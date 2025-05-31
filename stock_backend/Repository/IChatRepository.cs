using stock_backend.Models;

namespace stock_backend.Repository
{
    public interface IChatRepository
    {
        Task InitializeAsync();
        Task<IEnumerable<ChatMessage>> GetMessagesByRoomIdAsync(Guid roomId);
        Task<IEnumerable<ChatRoom>> GetRoomsByUserIdAsync(Guid userId);
        Task CreateMessageAsync(ChatMessage chatMessage);
        Task CreateRoomAsync(ChatRoom room);

    }
}
