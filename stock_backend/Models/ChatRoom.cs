namespace stock_backend.Models
{
    public class ChatRoom
    {
        public Guid Id { get; set; }
        public Guid CreatedBy { get; set; }
        public string Name { get; set; } = string.Empty;
        /// <summary>
        /// "private"
        /// "group"
        /// </summary>
        public string ChatType { get; set; } = "private";
        public DateTimeOffset CreatedAt { get; set; }
    }
}
