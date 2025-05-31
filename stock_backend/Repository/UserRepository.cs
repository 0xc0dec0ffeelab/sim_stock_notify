using Dapper;
using Npgsql;
using stock_backend.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stock_backend.Repository
{
    public class UserRepository : IUserRepository
    {
        private readonly NpgsqlConnection _connection;
        public UserRepository(NpgsqlConnection connection)
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
                CREATE EXTENSION IF NOT EXISTS pgcrypto;

                CREATE TABLE IF NOT EXISTS users (
                    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
                    username TEXT NOT NULL UNIQUE,
                    password_hash TEXT NOT NULL,
                    refresh_token TEXT,
                    refresh_token_expiry_time TIMESTAMPTZ,
                    created_at TIMESTAMPTZ DEFAULT now()
                );


            ";

            await EnsureConnectionOpenAsync();
            await _connection.ExecuteAsync(createTableSql);
        }
         
        
        public async Task CreateUserAsync(StockUser user)
        {
            const string sql = @"
            INSERT INTO users (id, username, password_hash)
            VALUES (@Id, @Username, @PasswordHash)";

            await _connection.ExecuteAsync(sql, user);
        }

        public async Task<StockUser?> GetUserByUsernameAsync(string username)
        {
            const string sql = @"
            SELECT 
                id, 
                username, 
                password_hash AS passwordHash 
            FROM users 
            WHERE username = @Username";

            return await _connection.QueryFirstOrDefaultAsync<StockUser>(sql, new { Username = username });
        }

        public async Task<StockUser?> GetRefreshTokenExpiryTimeByUsernameAsync(string refreshToken)
        {
            const string sql = @"
            SELECT 
                id, 
                username,
                refresh_token_expiry_time AS refreshTokenExpiryTime
            FROM users 
            WHERE refresh_token = @RefreshToken";

            return await _connection.QueryFirstOrDefaultAsync<StockUser>(sql, new { RefreshToken = refreshToken });
        }

        public async Task UpdateRefreshTokenAsync(StockUser user)
        {
            const string sql = @"
                UPDATE users 
                SET 
                    refresh_token = @RefreshToken,
                    refresh_token_expiry_time = @RefreshTokenExpiryTime
                WHERE id = @Id;
            ";

            await _connection.ExecuteAsync(sql, new
            {
                user.RefreshToken,
                user.RefreshTokenExpiryTime,
                user.Id
            });
        }

    }
}
