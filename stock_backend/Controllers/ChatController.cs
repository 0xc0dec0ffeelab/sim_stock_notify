using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using StackExchange.Redis;
using stock_backend.Models;
using stock_backend.Repository;
using stock_backend.WS;
using System.Data;

namespace stock_backend.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IRedisRepository _redis;
        private readonly IChatRepository _chatRepository;
        private readonly IHubContext<ChatHub> _hubContext;

        public ChatController(IRedisRepository redis, IChatRepository chatRepository, IHubContext<ChatHub> hubContext)
        {
            _redis = redis;
            _chatRepository = chatRepository;
            _hubContext = hubContext;
        }

        // todo show all rooms to join

        /// <summary>
        /// 取得所有聊天室
        /// chat_type = private、group
        /// </summary>
        /// <returns></returns>
        [HttpGet("rooms")]
        public async Task<IActionResult> GetMyRooms()
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (userId == null)
                return BadRequest("帳戶未註冊");

            // from DB
            // cache Redis ?

            var roomIds = await _redis.DB.SetMembersAsync($"user:{userId}:rooms");
            var result = new List<object>();

            foreach (var roomId in roomIds)
            {
                var roomInfo = await _redis.DB.HashGetAllAsync($"room:{roomId}:info");
                result.Add(new
                {
                    RoomId = roomId.ToString(),
                    Name = roomInfo.FirstOrDefault(e => e.Name == "name").Value.ToString()
                });
            }

            return Ok(result);
        }

        /// <summary>
        /// 取得聊天室所有訊息
        /// </summary>
        /// <param name="roomId"></param>
        /// <returns></returns>
        [HttpGet("history/{roomId}")]
        public async Task<IActionResult> GetHistory(Guid roomId)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (userId == null)
                return BadRequest("帳戶未註冊");

            var messages = await _chatRepository.GetMessagesByRoomIdAsync(roomId);

            return Ok(messages);
        }

        [HttpPost("room")]
        public async Task<IActionResult> CreateRoom(CreateRoomRequest request)
        {
            var userId = User.FindFirst("UserId")?.Value;
            if (userId == null)
                return BadRequest("帳戶未註冊");

            if (string.IsNullOrWhiteSpace(request.RoomName) || string.IsNullOrWhiteSpace(request.ChatType)) 
                return BadRequest("聊天室名稱不可為空 或是 聊天室類型不能為空");

            // DB
            var roomId = Guid.NewGuid();
            ChatRoom chatRoom = new()
            {
                Id = roomId,
                Name = request.RoomName,
                ChatType = request.ChatType,
                CreatedBy = Guid.Parse(userId),
                CreatedAt = DateTimeOffset.UtcNow
            };
            await _chatRepository.CreateRoomAsync(chatRoom);

            // 需要更新 redis 聊天室 資訊?

            // Redis publish 訊息至所有 server
            // await _hubContext.Clients.All.SendAsync("ReceiveMessage", message.User, message.Content);

            return Ok(new { RoomId = roomId });
        }
    }
}
