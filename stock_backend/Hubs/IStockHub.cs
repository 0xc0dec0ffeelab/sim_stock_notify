namespace stock_backend.WS
{
    public interface IStockHub
    {
        Task ReceiveMessage(string message);
    }
}
