using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace stock_backend.Hubs
{
    public class HubUserIdProvider : IUserIdProvider
    {
        public string? GetUserId(HubConnectionContext connection)
        {
            return connection.User?.FindFirst("UserId")?.Value;
        }
    }
}
