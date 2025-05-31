using stock_backend.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stock_backend.Repository
{
    public interface IUserRepository
    {
        Task InitializeAsync();
        Task CreateUserAsync(StockUser user);
        Task<StockUser?> GetUserByUsernameAsync(string username);
        Task UpdateRefreshTokenAsync(StockUser user);
        Task<StockUser?> GetRefreshTokenExpiryTimeByUsernameAsync(string refreshToken);
    }
}
