namespace stock_backend.Models
{
    public class StockPrice
    {
        public int Id { get; set; }
        public string Symbol { get; set; } = "";
        public DateTime Timestamp { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public long Volume { get; set; }
    }
}
