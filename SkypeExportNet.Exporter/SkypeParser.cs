using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Text;
using Dapper;
using Newtonsoft.Json;
using SkypeExportNet.Exporter.DatabaseModel;
using SkypeExportNet.Exporter.ReportModel;

namespace SkypeExportNet.Exporter
{
    public class SkypeParser : IDisposable
    {
        private SQLiteConnection connection;

        public SkypeParser(string dbPath)
        {
            connection = new SQLiteConnection("Data Source=" + dbPath);
        }

        public List<string> GetSkypeUsers()
        {
            string sql =
                "SELECT DISTINCT author AS skypename FROM Messages WHERE (type!=50 AND author IS NOT NULL AND author!='' AND author NOT GLOB '*[#$@]*') "
                    + "UNION "
                    + "SELECT DISTINCT dialog_partner AS skypename FROM Messages WHERE (type!=50 AND dialog_partner IS NOT NULL AND dialog_partner!='' AND dialog_partner NOT GLOB '*[#$@]*') "
                    + "ORDER BY skypename ASC";
            return connection.Query<string>(sql).ToList();
        }
        
        public void ExportUserHistory(string skypeId, string logPath, TimeReference timeReference)
        {
            AccountReport report = GetAccountReport(skypeId, timeReference);
            string reportJson = JsonConvert.SerializeObject(report);
            File.WriteAllText(logPath, reportJson, Encoding.UTF8);
        }

        public AccountReport GetAccountReport(string skypeId, TimeReference timeReference)
        {
            AccountReport result = new AccountReport
            {
                Name = skypeId
            };

            var conversationReport = GetPersonConversationReport(skypeId, timeReference);
            if (conversationReport != null)
            {
                result.Conversations.Add(conversationReport);
            }

            IList<int> conferenceIds = GetConferencesForSkypeId(skypeId);
            foreach (int conferenceId in conferenceIds)
            {
                conversationReport = GetConferenceReport(conferenceId, timeReference);
                if (conversationReport != null)
                {
                    result.Conversations.Add(conversationReport);
                }
            }
            return result;
        }

        private IList<int> GetConferencesForSkypeId(string skypeId)
        {
            return connection.Query<int>(
                "SELECT c.id FROM Conversations c INNER JOIN Participants p ON p.convo_id = c.id WHERE c.type = 2 AND p.identity = @Identity",
                new { Identity = skypeId }).ToList();
        }

        private ConversationReport GetPersonConversationReport(string skypeId, TimeReference timeReference)
        {
            return GetConversationReport(skypeId, 0, timeReference);
        }

        private ConversationReport GetConferenceReport(int conferenceId, TimeReference timeReference)
        {
            return GetConversationReport(null, conferenceId, timeReference);
        }

        private static DateTime UnixTimeStampToDateTime(long unixTimeStamp, TimeReference timeReference)
        {
            // Unix timestamp is seconds past epoch
            DateTime result = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            result = result.AddSeconds(unixTimeStamp);
            if (timeReference == TimeReference.Local)
            {
                result = result.ToLocalTime();
            }
            return result;
        }

        private ConversationReport GetConversationReport(string skypeId, int conferenceId, TimeReference timeReference)
        {
            bool isConference = skypeId == null;
            int conversationId = isConference ? conferenceId : 0;

            if (!isConference)
            {
                conversationId = connection.QueryFirstOrDefault<int>(
                    "SELECT id FROM Conversations WHERE (identity = @Identity AND type = 1) LIMIT 1",
                    new { Identity = skypeId });
                if (conversationId == 0)
                {
                    return null;
                }
            }

            ConversationReport result = new ConversationReport
            {
                Name = skypeId ?? "Conference"
            };

            IEnumerable<SkypeMessage> skypeMessages = connection.Query<SkypeMessage>(
                "SELECT type Type, sending_status SendingStatus, chatmsg_status ChatMessageStatus, author Author," +
                    " from_dispname FromDispName, body_xml BodyXML, timestamp Timestamp, edited_timestamp EditedTimestamp," +
                    " guid GUID, convo_id ConversationId, identities Identities" +
                    "   FROM Messages" +
                    "   WHERE ConversationId = @ConversationId" +
                    "   ORDER BY timestamp ASC",
                new { ConversationId = conversationId });

            foreach (SkypeMessage skypeMessage in skypeMessages)
            {
                result.Messages.Add(new MessageReport
                {
                    Type = skypeMessage.Type,
                    SendingStatus = skypeMessage.SendingStatus,
                    ChatMessageStatus = skypeMessage.ChatMessageStatus,
                    Author = skypeMessage.Author,
                    FromDispName = skypeMessage.FromDispName,
                    BodyXML = skypeMessage.BodyXML,
                    Timestamp = UnixTimeStampToDateTime(skypeMessage.Timestamp, timeReference)
                });
            }
            
            return result;
        }

        public void Dispose()
        {
            if (connection != null)
            {
                connection.Dispose();
                connection = null;
            }
        }
    }
}
