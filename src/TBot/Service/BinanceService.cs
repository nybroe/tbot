using Binance.Net;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Logging;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace TBot.Service
{
    public class BinanceService: IBinanceService
    {
        private readonly ILogger<PingService> _logger;

        public BinanceService(ILogger<PingService> logger)
        {
            _logger = logger;

            BinanceClient.SetDefaultOptions(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials("[YOUR_BINANCE_CLINTID]", "[YOUR_BINANCE_CLIENT_SECRET]"),
                LogVerbosity = LogVerbosity.Debug,
                LogWriters = new List<TextWriter> { Console.Out }
            });

            var host = Dns.GetHostEntry(Dns.GetHostName());
        }

        public async Task<BinancePlacedOrder> CreateTestOrderAsync()
        {
            using (var client = new BinanceClient())
            {
                var btcPrice = await client.GetPriceAsync("BTCUSDT");

                if (btcPrice.ResponseStatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"Retrieving price failed with: {btcPrice.Error.Message}");
                    return null;
                }

                var testOrderResult = await client.PlaceTestOrderAsync("BTCUSDT", OrderSide.Buy, OrderType.Limit, 1, price: btcPrice.Data.Price, timeInForce: TimeInForce.GoodTillCancel);

                if (testOrderResult.ResponseStatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"Creating TEST order failed with: {testOrderResult.Error.Message}");
                    return null;
                }

                return testOrderResult.Data;
            }
        }

        public async Task<BinancePlacedOrder> CreateOrderAsync(OrderSide side, decimal? quantity)
        {
            using (var client = new BinanceClient())
            {
                var btcPrice = await client.GetPriceAsync("BTCUSDT");
                if (btcPrice == null)
                {
                    return null;
                }

                var roundedQuantity = Decimal.Round(quantity.Value, 6);
                var orderResult = await client.PlaceOrderAsync("BTCUSDT", side, OrderType.Limit, roundedQuantity, price: btcPrice.Data.Price, timeInForce: TimeInForce.GoodTillCancel);

                if (orderResult.ResponseStatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"Creating order failed with: {orderResult.Error.Message}");
                    return null;
                }

                return orderResult.Data;
            }
        }

        public async Task<decimal?> GetPriceAsync()
        {
            using (var client = new BinanceClient())
            {
                var btcPrice = await client.GetPriceAsync("BTCUSDT");

                if (btcPrice.ResponseStatusCode != System.Net.HttpStatusCode.OK) {
                    _logger.LogError($"Retrieving price failed with: {btcPrice.Error.Message}");
                    return null;
                }

                return btcPrice?.Data?.Price;
            }
        }

        public async Task<List<BinanceOrder>> GetOpenOrdersAsync()
        {
            using (var client = new BinanceClient())
            {
                var openOrders = await client.GetOpenOrdersAsync("BTCUSDT");

                if (openOrders.ResponseStatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"Retrieving open orders failed with: {openOrders.Error.Message}");
                    return null;
                }

                return openOrders != null && openOrders.Data != null ? openOrders.Data.ToList() : null;
            }
        }

        public async Task<BinanceOrder> GetOrderAsync(long orderId)
        {
            using (var client = new BinanceClient())
            {
                var order = await client.GetOrderAsync("BTCUSDT", orderId);

                if (order.ResponseStatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"Retrieving order failed with: {order.Error.Message}");
                    return null;
                }

                return order.Data ?? null;
            }
        }

        public async Task<BinanceAccountInfo> GetAccountInfoAsync()
        {
            using (var client = new BinanceClient())
            {
                var accountInfo = await client.GetAccountInfoAsync();

                if (accountInfo.ResponseStatusCode != System.Net.HttpStatusCode.OK)
                {
                    _logger.LogError($"Retrieving account info failed with: {accountInfo.Error.Message}");
                    return null;
                }

                return accountInfo.Data ?? null;
            }
        }
    }
}
