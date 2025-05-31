using Microsoft.AspNetCore.SignalR;
using System.Text.RegularExpressions;

namespace stock_backend.WS
{
    public class StockHub : Hub<IStockHub>
    {
        public override async Task OnConnectedAsync()
        {
            await base.OnConnectedAsync();
        }
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            await base.OnDisconnectedAsync(exception);
        }

        public async Task BroadcastMessage(string message)
        {
            await Clients.All.ReceiveMessage(message);
        }
        public async Task Subscribe(string symbol)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, symbol);
        }

        public async Task Unsubscribe(string symbol)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, symbol);
        }

        //資料推給所有訂閱了該 symbol 的用戶
        public static async Task PushToSymbol(IHubContext<StockHub> hub, string symbol, object data)
        {
            await hub.Clients.Group(symbol).SendAsync("ReceiveStockUpdate", data);
        }
    }
}
