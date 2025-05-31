using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using Npgsql;
using StackExchange.Redis;
using stock_backend.Models;
using stock_backend.Repository;
using System.Data;
using System.Text.Json;

namespace stock_backend.Controllers
{
    [ApiController]
    [Route("api/stock")]
    public class StockController : ControllerBase
    {
        //private readonly IDatabase _redis;
        //private readonly IDistributedCache _cache;
        //private readonly IDbConnection _db;
        private readonly NpgsqlConnection _db;
        //private readonly IConnectionMultiplexer _redis;
        private readonly IRedisRepository _redis;

        private readonly ILogger<StockController> _logger;

        public StockController(ILogger<StockController> logger, NpgsqlConnection db, IRedisRepository redis)
        {
            _logger = logger;
            _db = db;
            _redis = redis;
        }

        /// <summary>
        /// 查詢單一 symbol 某日資料 /api/stocks/AAPL/2025-05-01
        /// 查詢 symbol 某段區間資料  /api/stocks/AAPL?start=2025-04-01&end=2025-05-01
        /// 查詢 symbol 某一個 timestamp（秒級）  /api/stocks/AAPL/timestamp/1714893600
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <param name="page"></param>
        /// <param name="pageSize"></param>
        /// <returns>
        /// {
        //  "symbol": "AAPL",
        //  "timestamp": 1714893600,
        //  "date": "2025-05-05",
        //  "open": 184.52,
        //  "high": 186.34,
        //  "low": 183.77,
        //  "close": 185.91,
        //  "volume": 11032000
        //}
        /// </returns>
        [HttpGet("{symbol}")]
        public async Task<IActionResult> GetRange(string symbol, [FromQuery] DateTime start, [FromQuery] DateTime end, [FromQuery] int page = 1, [FromQuery] int pageSize = 100)
        {
            //string cacheKey = $"stock:{symbol}:{from}:{to}:{timestamp}";
            string cacheKey = $"range:{symbol}:{start:yyyyMMdd}:{end:yyyyMMdd}:{page}:{pageSize}";
            var cached = await _redis.DB.StringGetAsync(cacheKey);
            if (cached.IsNullOrEmpty == false)
                //return Content(cached, "application/json");
                return Ok(cached);

            // 分頁查詢
            int offset = (page - 1) * pageSize;
            var sql = @"SELECT * FROM stock_prices 
                    WHERE symbol = @Symbol AND date BETWEEN @Start AND @End 
                    ORDER BY timestamp LIMIT @PageSize OFFSET @Offset";

            var data = await _db.QueryAsync<StockPrice>(sql, new { Symbol = symbol, Start = start, End = end, PageSize = pageSize, Offset = offset });
            var json = JsonSerializer.Serialize(data);
            await _redis.DB.StringSetAsync(
                cacheKey,
                json,
                expiry: TimeSpan.FromMinutes(5));

            //return Content(json, "application/json");
            return Ok(cached);
        }

        [HttpPost("subscribe")]
        public async Task<IActionResult> Subscribe([FromQuery] string userId, [FromQuery] string symbol)
        {
            const string insertSql = @"
                INSERT INTO user_symbol_subscriptions (user_id, symbol) 
                VALUES (@UserId, @Symbol) 
                ON CONFLICT DO NOTHING;";

            await _db.ExecuteAsync(insertSql, new { UserId = userId, Symbol = symbol });
            return Ok(new { message = $"Subscribed to {symbol}" });
        }

        [HttpGet("subscribe/list")]
        public async Task<IActionResult> GetUserSubscriptions([FromQuery] string userId)
        {
            const string selectSql = @"
            SELECT symbol 
            FROM user_symbol_subscriptions 
            WHERE user_id = @UserId;";

            var symbols = await _db.QueryAsync<string>(selectSql, new { UserId = userId });
            return Ok(symbols);
        }

        [HttpPost("unsubscribe")]
        public async Task<IActionResult> Unsubscribe([FromQuery] string userId, [FromQuery] string symbol)
        {
            const string deleteSql = @"
            DELETE FROM user_symbol_subscriptions 
            WHERE user_id = @UserId AND symbol = @Symbol;";

            await _db.ExecuteAsync(deleteSql, new { UserId = userId, Symbol = symbol });
            return Ok(new { message = $"Unsubscribed from {symbol}" });
        }

        //[HttpGet("{symbol}")]
        //public async Task<IActionResult> GetPrice(string symbol)
        //{
        //    var key = $"price:{symbol.ToUpper()}";
        //    var cached = await _redis.StringGetAsync(key);
        //    if (!cached.HasValue)
        //    {
        //        return NotFound(new { message = "Price not available." });
        //    }

        //    var price = JsonSerializer.Deserialize<PriceData>(cached!);
        //    return Ok(price);
        //}

    }
}
