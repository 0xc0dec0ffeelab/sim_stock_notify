using Dapper;
using Npgsql;
using stock_backend.Models;
using System.Data;
using System.Data.Common;

namespace stock_backend.Repository
{
    public class ChatRepository : IChatRepository
    {
        private readonly NpgsqlConnection _connection;
        public ChatRepository(NpgsqlConnection connection) 
        {
            _connection = connection;
        }

        private async Task EnsureConnectionOpenAsync()
        {
            if (_connection.State != ConnectionState.Open)
                await _connection.OpenAsync();
        }

        public async Task InitializeAsync()
        {
            var createTableSql = @"

                CREATE TABLE IF NOT EXISTS chat_rooms (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    name TEXT NOT NULL,
                    chat_type TEXT NOT NULL,
                    created_by UUID NOT NULL,
                    created_at TIMESTAMPTZ NOT NULL DEFAULT now()
                );

                CREATE TABLE IF NOT EXISTS chat_messages (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    room_id UUID REFERENCES chat_rooms(id),
                    sender_id UUID,
                    content TEXT,
                    sent_at TIMESTAMPTZ NOT NULL DEFAULT now()
                );

                CREATE TABLE IF NOT EXISTS chat_users (
                    user_id UUID,
                    room_id UUID REFERENCES chat_rooms(id),
                    joined_at TIMESTAMPTZ NOT NULL DEFAULT now(),
                    PRIMARY KEY (user_id, room_id)
                );

            ";

            await EnsureConnectionOpenAsync();
            await _connection.ExecuteAsync(createTableSql);
        }

        public async Task<IEnumerable<ChatMessage>> GetMessagesByRoomIdAsync(Guid roomId)
        {
            const string sql = @"
                SELECT
                    id,
                    room_id as roomId,
                    sender_id as senderId,
                    content,
                    sent_at as sentAt  
                FROM chat_messages
                WHERE room_Id = @RoomId
                ORDER BY sent_at
                DESC LIMIT 50";
            return await _connection.QueryAsync<ChatMessage>(sql, new { RoomId = roomId });
        }

        public async Task<IEnumerable<ChatRoom>> GetRoomsByUserIdAsync(Guid userId)
        {
            const string sql = @"
                SELECT 
                    cr.id,
                    cr.name,
                    cr.chat_type as chatType,
                    cr.created_by as createdBy,
                    cr.created_at as createdAt
                FROM chat_rooms cr
                JOIN chat_users cu ON cr.id = cu.room_id
                WHERE cu.userId = @UserId";
            return await _connection.QueryAsync<ChatRoom>(sql, new { UserId = userId });
        }

        public async Task CreateMessageAsync(ChatMessage chatMessage)
        {
            const string sql = @"
            INSERT INTO chat_messages (id, room_Id, sender_id, content, sent_at)
            VALUES (@Id, @RoomId, @SenderId, @Content, @SentAt)";
            await _connection.ExecuteAsync(sql, chatMessage);
        }

        public async Task CreateRoomAsync(ChatRoom room)
        {
            const string sql = @"
            INSERT INTO chat_rooms (id, name, chat_type, created_by, created_at)
            VALUES (@Id, @Name, @ChatType, @CreatedBy, @CreatedAt)";
            await _connection.ExecuteAsync(sql, room);
        }

        public async Task AddUserToChat()
        {

        }

    }
}
