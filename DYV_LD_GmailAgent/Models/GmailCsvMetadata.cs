
namespace DYV_Linked_Document_Management.Models
{
    public class GmailCsvMetadata
    {
        public string Rfc822MessageId { get; set; }
        public string GmailMessageId { get; set; }
        public string FileName { get; set; }
        public string Account { get; set; }
        public string Labels { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string To { get; set; }
        public string CC { get; set; }
        public string BCC { get; set; }
        public string DateSent { get; set; }
        public string DateReceived { get; set; }
        public string SubjectAtStart { get; set; }
        public string SubjectAtEnd { get; set; }
        public string DateFirstMessageSent { get; set; }
        public string DateLastMessageSent { get; set; }
        public string DateFirstMessageReceived { get; set; }
        public string DateLastMessageReceived { get; set; }
        public string ThreadedMessageCount { get; set; }
        public string Identifier { get; set; }
        public string FileLinkedDocument { get; set; }
    }
    public class GmailCsvDriveLink
    {
        public string Account { get; set; }
        public string Rfc822MessageId { get; set; }
        public string GmailMessageId { get; set; }
        public string DriveUrl { get; set; }
        public string DriveItemId { get; set; }
        public string Identifier { get; set; }
        public string FileLinkedDocument { get; set; }
    }
}
