using System;
using SkypeExportNet.Exporter.DatabaseModel;

namespace SkypeExportNet.Exporter.ReportModel
{
    public class MessageReport
    {
        public SkypeMessageType Type { get; set; }
        public SendingStatus SendingStatus { get; set; }
        public int ChatMessageStatus { get; set; }
        public string Author { get; set; }
        public string FromDispName { get; set; }
        public string BodyXML { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
