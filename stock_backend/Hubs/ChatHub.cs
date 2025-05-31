using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using stock_backend.Models;
using stock_backend.Repository;
using System.Net.Sockets;
using System.Net;

namespace stock_backend.WS
{
    public class ChatHub : Hub
    {
        readonly IRedisRepository _redis;
        readonly IChatRepository _chatRepository;
        public ChatHub(IRedisRepository redis, IChatRepository chatRepository)
        {
            _redis = redis;
            _chatRepository = chatRepository;
        }
        //Redis 的 Hash/Set

        //public async Task SendMessageAsync(string userId, string message)
        //{
        //    await Clients.User(userId).SendAsync("ReceiveMessage", message);
        //}

        //public async Task<string> CreateRoomAsync(string roomName)
        //{
        //    var userId = Context.UserIdentifier;
        //    if (userId == null) return string.Empty;

        //    Guid roomId = Guid.NewGuid();
        //    // DB 建立聊天室
        //    ChatRoom chatRoom = new()
        //    {
        //        Id = roomId,
        //        Name = roomName,
        //        CreatedBy = Guid.Parse(userId),
        //        CreatedAt = DateTimeOffset.UtcNow
        //    };
        //    await _chatRepository.CreateRoomAsync(chatRoom);
        //    // 聊天加入建立者
        //    return roomId.ToString();
        //}

        public async Task JoinRoomAsync(string roomId)
        {   
            //var userId = Context.User?.FindFirst("UserId")?.Value;
            var userId = Context.UserIdentifier;
            if (userId == null) return;

            await Groups.AddToGroupAsync(Context.ConnectionId, roomId);
            await _redis.DB.SetAddAsync($"room:{roomId}:users", Context.ConnectionId);
            await _redis.DB.SetAddAsync($"user:{userId}:rooms", roomId);

            // chat_rooms
            // chat_users
            // lua script

            await Clients.Group(roomId).SendAsync("UserJoined", userId);
        }


        public async Task LeaveRoomAsync(string roomId)
        {
            var userId = Context.UserIdentifier;
            if (userId == null) return;

            //if (Rooms.TryGetValue(roomName, out var members) && members.Contains(Context.ConnectionId))
            //{
            //    members.Remove(Context.ConnectionId);
            //    await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomName);
            //    await Clients.Group(roomName).SendAsync("UserLeft", Context.ConnectionId);
            //}

            // chat_rooms
            // chat_users
            // lua script

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, roomId);
            await _redis.DB.SetRemoveAsync($"room:{roomId}:users", Context.ConnectionId);
            await _redis.DB.SetRemoveAsync($"user:{userId}:rooms", roomId);

            await Clients.Group(roomId).SendAsync("UserLeft", userId);
        }

        public async Task DeleteRoomAsync(string roomId)
        {

            var userIds = await _redis.DB.SetMembersAsync($"room:{roomId}:users");

            foreach (var connectionId in userIds)
            {
                var userId = await _redis.DB.StringGetAsync($"connection:{connectionId}:user");
                if (userId.IsNullOrEmpty == false)
                {
                    await _redis.DB.SetRemoveAsync($"user:{userId}:rooms", roomId);
                }
            }

            await _redis.DB.KeyDeleteAsync($"room:{roomId}:users");
            // DB 刪除聊天室相關資料
            //    刪除聊天室連線
            //await _redis.KeyDeleteAsync($"room:{roomId}:info");
            //await _chatRepository.DeleteRoomAsync(Guid.Parse(roomId));
        }

        public async Task SendMessageToRoomAsync(string roomId, ChatMessage chatMessage)
        {
            //await Clients.Group(roomName).SendAsync("ReceiveMessage", Context.ConnectionId, message);
            var userId = Context.UserIdentifier;
            if (userId == null) return;

            await _chatRepository.CreateMessageAsync(chatMessage);
            await Clients.Group(roomId).SendAsync("ReceiveMessage", chatMessage.Content);

        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = await _redis.DB.StringGetAsync($"connection:{Context.ConnectionId}:user");

            // 如果離線
            // 主動刪除 Group 連線 
            if (userId.IsNullOrEmpty == false)
            {
                await _redis.DB.SetRemoveAsync($"user:{userId}:connections", Context.ConnectionId);
                await _redis.DB.KeyDeleteAsync($"connection:{Context.ConnectionId}:user");

                // 誰離線
                // 刪除連線
                // 刪除訂閱

            }

            await base.OnDisconnectedAsync(exception);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            if (userId == null)
            {
                Context.Abort();
                return;
            }
            Console.WriteLine($"User connected: {userId}");

            if (userId != null)
            {
                await _redis.DB.SetAddAsync($"user:{userId}:connections", Context.ConnectionId);
                await _redis.DB.StringSetAsync($"connection:{Context.ConnectionId}:user", userId);
            }

            var hostName = Dns.GetHostName();
            var containerIP = Dns.GetHostEntry(hostName)
                    .AddressList
                    .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)?
                    .ToString();
            await Clients.Caller.SendAsync("ContainerChanged", new { hostName, containerIP });

            await base.OnConnectedAsync();
        }
    }
}
