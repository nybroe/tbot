using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TBot.Models;

namespace TBot.Service
{
    public interface IStateService
    {
        Task<StateModel> GetAsync();
        Task<List<OrderModel>> GetTradesAsync();
    }
}
