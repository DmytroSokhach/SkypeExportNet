namespace SkypeExportNet.Exporter.DatabaseModel
{
    public enum SkypeMessageType
    {
        PersonAddedToConference = 10,
        PersonLeftConference = 13,
        CallStart = 30,
        CallEnd = 39,
        Emote = 60,
        Text = 61,
        FileTransfer = 68,
        CloudFileTransfer = 201
    }
}
