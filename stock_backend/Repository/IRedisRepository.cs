using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace stock_backend.Repository
{
    public interface IRedisRepository
    {
        IDatabase DB { get; }
        //Task PublishAsync(string channel, string message);
    }
}
