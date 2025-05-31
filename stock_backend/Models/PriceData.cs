namespace stock_backend.Models
{
    public class PriceData
    {
        public string Symbol { get; set; }
        public decimal BestBid { get; set; }
        public decimal BestAsk { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
