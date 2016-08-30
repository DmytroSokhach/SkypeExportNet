namespace SkypeExportNet.Exporter.DatabaseModel
{
    public class SkypeMessage
    {
        public SkypeMessageType Type { get; set; }
        public SendingStatus SendingStatus { get; set; }
        public int ChatMessageStatus { get; set; }
        public string Author { get; set; }
        public string FromDispName { get; set; }
        public string BodyXML { get; set; }
        public long Timestamp { get; set; }
        public long EditedTimestamp { get; set; }
        public byte[] GUID { get; set; }
        public int ConvoId { get; set; }
        public string Identities { get; set; }
    }
}
