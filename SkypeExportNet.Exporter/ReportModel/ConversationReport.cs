using System.Collections.Generic;

namespace SkypeExportNet.Exporter.ReportModel
{
    public class ConversationReport
    {
        public string Name;
        public List<MessageReport> Messages = new List<MessageReport>();
    }
}
