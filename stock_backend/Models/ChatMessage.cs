namespace stock_backend.Models
{
    public class ChatMessage
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public Guid SenderId { get; set; }
        public string Content { get; set; } = string.Empty;
        public DateTimeOffset SentAt { get; set; }
    }
}
