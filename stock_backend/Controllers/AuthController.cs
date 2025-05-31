using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using stock_backend.Models;
using stock_backend.Repository;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Sockets;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Linq;

namespace stock_backend.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly IUserRepository _userRepository;

        private readonly PasswordHasher<string> _hasher = new();

        public AuthController(IUserRepository userRepository, IConfiguration configuration)
        {
            _configuration = configuration;
            _userRepository = userRepository;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(LoginRequest reqest)
        {
            if (string.IsNullOrWhiteSpace(reqest.Username) || string.IsNullOrWhiteSpace(reqest.Password))
                return BadRequest("帳號和密碼必填"); 

            var existing = await _userRepository.GetUserByUsernameAsync(reqest.Username);

            if (existing != null)
                return BadRequest("重複註冊");

            var hash = HashPassword(reqest.Password);
            
            var user = new StockUser
            {
                Id = Guid.NewGuid(),
                Username = reqest.Username,
                PasswordHash = hash
            };

            await _userRepository.CreateUserAsync(user);

            return Ok("註冊成功");
        }

        [HttpGet("containerinfo")]
        public async Task<IActionResult> GetContainerInfo()
        {
            var hostName = Dns.GetHostName();
            var containerIP = Dns.GetHostEntry(hostName)
                    .AddressList
                    .FirstOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork)?
                    .ToString();
            //var containerId = Environment.MachineName; // Docker container ID

            return Ok(new {hostName, containerIP});
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequest req)
        {
            var user = await _userRepository.GetUserByUsernameAsync(req.Username);

            if (user == null || VerifyPassword(user.PasswordHash, req.Password) == false)
                return Unauthorized(new { message = "帳號尚未註冊或是帳號密碼有誤", token= "" });
            // Generate JWT (you can store it in cookie if needed)
            var jwt = GenerateJwt(user);
            var refreshToken = GenerateRefreshToken();

            // update refreshToken
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTimeOffset.UtcNow.AddDays(7);
            await _userRepository.UpdateRefreshTokenAsync(user);

            return Ok(new { token = jwt, refreshToken, message = "" });
        }

        [HttpPost("logout")]
        public IActionResult Logout()
        {
            Response.Cookies.Delete("jwt");
            Response.Cookies.Delete("refresh");

            return Ok("已登出");
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh()
        {
            if (Request.Cookies.TryGetValue("refresh", out var refreshToken) == false)
                return Unauthorized(new { message = "refresh token 不存在", token = "" });
           
            if (string.IsNullOrWhiteSpace(refreshToken))
                return Unauthorized(new { message = "refresh token 不存在", token = "" });

            var user = await _userRepository.GetRefreshTokenExpiryTimeByUsernameAsync(refreshToken);

            if (user is null)
                return Unauthorized(new { message = "帳戶不存在", token = "" });

            if (user.RefreshTokenExpiryTime < DateTimeOffset.UtcNow)
                return Unauthorized(new { message = "refresh token 已過期，請重新登入", token = "" });

            var jwt = GenerateJwt(user);

            return Ok(new { token = jwt, message = "" });
        }

        [HttpGet("test")]
        public IActionResult GetSecureData()
        {
            var userName = User?.Identity?.Name; // 可從 claims 取出用戶資訊
            //var userId = User?.FindFirst("UserId")?.Value;
            return Ok($"Hello {userName}, this is a protected API");
        }

        private string GenerateRefreshToken()
        {
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            return Convert.ToBase64String(randomBytes);
        }

        private string GenerateJwt(StockUser user)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Username), // 代表這個 token 屬於誰
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // 這個 token 一個唯一識別碼
                new Claim(ClaimTypes.Name, user.Username), //Name 欄位，之後可以用 User.Identity.Name 取得這個值s
                new Claim("UserId", user.Id.ToString()) // 自訂的 Claim，這裡加上使用者的 Id
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256); // 簽名憑證


            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                //expires: DateTime.UtcNow.AddSeconds(10),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }


        // 從過期的 token 中解析出原本的 claims
        private ClaimsPrincipal GetPrincipalFromExpiredToken(string token)
        {
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateIssuerSigningKey = true,
                ValidateLifetime = false, // 不驗證過期時間

                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:SecretKey"]!))
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out SecurityToken securityToken);

            // 確保 token 是 JWT，沒被竄改
            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                throw new SecurityTokenException("Invalid token");

            return principal;
        }

        private string HashPassword(string password)
        {
            return _hasher.HashPassword(string.Empty, password);
        }

        private bool VerifyPassword(string hash, string input)
        {
            return _hasher.VerifyHashedPassword(string.Empty, hash, input) == PasswordVerificationResult.Success;
        }
    }
}
