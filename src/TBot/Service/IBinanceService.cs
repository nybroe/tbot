using Binance.Net.Objects;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace TBot.Service
{
    public interface IBinanceService
    {
        Task<BinancePlacedOrder> CreateTestOrderAsync();
        Task<BinancePlacedOrder> CreateOrderAsync(OrderSide side, decimal? quantity);
        Task<decimal?> GetPriceAsync();
        Task<List<BinanceOrder>> GetOpenOrdersAsync();
        Task<BinanceAccountInfo> GetAccountInfoAsync();
        Task<BinanceOrder> GetOrderAsync(long orderId);
    }
}
