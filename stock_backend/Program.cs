/*
 

 在 PriceFeederService 中將資料 Publish 到 Kafka 主題 ??? 模擬訂閱者 ??
將最新一天資料透過 Kafka 推送給訂閱者


https://cdnjs.cloudflare.com/ajax/libs/microsoft-signalr/8.0.0/signalr.min.js

 SignalR 加入即時價格推播能力
 Redis TTL 控制，並且自動更新 Refresh ?

資料來源為大量虛擬產生之股價歷史資料
提供 Symbol / 日期區間 / 指定時間戳的查詢 API

 Redis Backplane


docker compose --env-file docker-compose-config.txt up --build -d
docker compose up -d
docker compose down

docker compose down -v
-v 刪除volume

docker compose up --scale stock.backend=3 -d
docker compose -f docker-compose.yaml up --scale stock.backend=3 -d

redis pubsub 如何設計讓 user 可以跨chat server 溝通
以及 針對因離線而未能傳送的訊息如何存取


redis pub/sub topic

私聊
chat:user:{user_id}
訂閱: 加入 => SUBSCRIBE chat:user:user1
取消訂閱: 離線(斷線) => UNSUBSCRIBE chat:user:user1、
如果是換到另一台chatserver 也需要自動取消訂閱這一台的chatserver訂閱 ??

                                  18T10:00:00Z",
  "message": "Hello!",
  "room_id": "optional"
}

// 每人一頻道 + metadata 篩選私聊對象，效能最佳。 ??

// 一開始設定多少 topic ， 如何從各自users 的 chat server 訂閱各自需要 topic ??

// 斷線重連的 訂閱處理、topic 如果都沒訂閱?

// 線上狀態
// SET chat:online:userB true EX 60

// 一上線馬上同步 離線訊息?

// redis pub/sub 訊息順序性


*/
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using StackExchange.Redis;
using stock_backend.Helpers;
using stock_backend.Hubs;
using stock_backend.Repository;
using stock_backend.Services;
using stock_backend.WS;
using System.Data;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IConfiguration>(builder.Configuration);
builder.Services.AddControllers().AddJsonOptions(options =>options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
// Add services to the container.

//builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect("localhost:6379"));
//builder.Services.AddStackExchangeRedisCache(options =>
//{
//    options.Configuration = "localhost:6379";
//});

builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = builder.Configuration.GetConnectionString("Redis")!;
    var options = ConfigurationOptions.Parse(configuration);

    //var logger = sp.GetRequiredService<ILogger<Program>>();

    options.AbortOnConnectFail = false;
    //options.ReconnectRetryPolicy = new ExponentialRetry(5000); // 5 秒指數退避重連策略

    var connection = ConnectionMultiplexer.Connect(options);
    connection.ConnectionFailed += (sender, e) =>
    {
        Console.WriteLine($"Redis connection failed: {e.Exception?.Message}");
    };
    // 嘗試重連事件
    connection.ConnectionRestored += (_, e) =>
    {
        //logger.LogInformation("Redis connection restored: {FailureType}", e.FailureType);
        Console.WriteLine($"Redis connection restored: {e.FailureType}");
    };

    if (connection.IsConnected == false)
    {
        Console.WriteLine("Warning: Redis is not connected.");
    }
    // 內部錯誤事件
    connection.ErrorMessage += (_, e) =>
    {
        Console.WriteLine($"Redis error message: {e.Message}");
        //logger.LogError("Redis error message: {Message}", e.Message);
    };

    // 診斷訊息
    connection.InternalError += (_, e) =>
    {
        Console.WriteLine($"Redis internal error: {e.Origin}: {e.Exception}");
        //logger.LogError(e.Exception, "Redis internal error: {Origin}", e.Origin);
    };

    return connection;
});

builder.Services.AddScoped(sp =>
{
    return new NpgsqlConnection(builder.Configuration.GetConnectionString("TimescaleDB"));
});

builder.Services.AddSingleton<IRedisRepository, RedisRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IChatRepository, ChatRepository>();
builder.Services.AddSingleton<IRedisPubSubRepository>(sp =>
{
    var connection = sp.GetRequiredService<IConnectionMultiplexer>();
    var channelPrefix = "chat";
    return new RedisPubSubRepository(connection, channelPrefix);
});

//builder.Services.AddSingleton<IRedisPubSubRepository>(sp =>
//{
//    var connection = sp.GetRequiredService<IConnectionMultiplexer>();
//    var channelPrefix = "alert";
//    return new RedisPubSubRepository(connection, channelPrefix);
//});



builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

}).AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(builder.Configuration.GetValue<string>("Jwt:SecretKey")!)),
        ValidIssuer = builder.Configuration.GetValue<string>("Jwt:Issuer"),
        ValidAudience = builder.Configuration.GetValue<string>("Jwt:Audience"),
        ClockSkew = TimeSpan.Zero
    };

    // get jwt from cookie
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            if (context.Request.Cookies.ContainsKey("jwt"))
            {
                context.Token = context.Request.Cookies["jwt"];
            }
            return Task.CompletedTask;
        },
        OnAuthenticationFailed = context =>
        {

            var path = context.Request.Path.Value!;

            if (   path.StartsWith("/api/auth/containerinfo") ||
                   path.StartsWith("/api/auth/login") ||
                   path.StartsWith("/api/auth/logout") ||
                   path.StartsWith("/api/auth/refresh") ||
                   path.StartsWith("/api/auth/register"))
            {
                // 不做任何處理，放行
                context.NoResult();  // 防止預設錯誤處理
                return Task.CompletedTask;
            }


            if (context.Exception is SecurityTokenExpiredException)
            {
                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";
                var json = JsonSerializer.Serialize(new { message = "Token 已過期，請重新登入" }, JsonDefaults.UnsafeOption);
                return context.Response.WriteAsync(json);
            }
            return Task.CompletedTask;
        },
        OnChallenge = context =>
        {
            if (!context.Response.HasStarted)
            {
                // 其他認證失敗，例如 Token 無效
                context.HandleResponse();

                context.Response.StatusCode = 401;
                context.Response.ContentType = "application/json";

                var json = JsonSerializer.Serialize(new { message = "您尚未授權，請先登入" }, JsonDefaults.UnsafeOption);
                return context.Response.WriteAsync(json);
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

builder.Services.AddControllers().AddJsonOptions(options => options.JsonSerializerOptions.Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping);
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
// todo add cookie test api
builder.Services.AddSwaggerGen();

// 加入 SignalR 服務
builder.Services.AddSignalR().AddHubOptions<ChatHub>(options =>
{
    //options.HandshakeTimeout = TimeSpan.FromSeconds(15);
    //options.KeepAliveInterval = TimeSpan.FromSeconds(15);
    //options.ClientTimeoutInterval = TimeSpan.FromSeconds(30);
}).AddMessagePackProtocol();

builder.Services.AddSingleton<IUserIdProvider, HubUserIdProvider>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:8080") // 前端位置
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});


// compression
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<GzipCompressionProvider>();
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
        new[] { "application/json" });
});
builder.Services.Configure<GzipCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest;
});


var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var userRepo = scope.ServiceProvider.GetRequiredService<IUserRepository>();
    var chatRepo = scope.ServiceProvider.GetRequiredService<IChatRepository>();
    await userRepo.InitializeAsync();  // 執行 CREATE TABLE IF NOT EXISTS
    await chatRepo.InitializeAsync();  // 執行 CREATE TABLE IF NOT EXISTS
}

app.UseRouting();
app.UseCors();

// Configure the HTTP request pipeline.
// if (app.Environment.IsDevelopment())
// {
app.UseSwagger();
app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "Realtime API V1"));

// }

//app.UseHttpsRedirection();
app.UseAuthentication();  // 啟用身份驗證
//app.UseAuthorization(); // 啟用授權

app.MapControllers();

//app.MapHub<StockHub>("/ws/stocks");
app.MapHub<ChatHub>("/chatHub", options => 
{
    options.Transports = HttpTransportType.WebSockets;

    //伺服器關閉之後，如果用戶端在此時間間隔內無法關閉，連線就會終止。
    //options.WebSockets.CloseTimeout = TimeSpan.FromSeconds(5);

    //options.CloseOnAuthenticationExpiration = false;
});

app.Run();
