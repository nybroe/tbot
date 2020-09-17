using Binance.Net.Objects;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TBot.Models;

namespace TBot.Service
{
    public class PingService : BackgroundService, IPingService
    {

        private readonly IGMailService _gmailService;
        private readonly IBinanceService _binanceService;
        private readonly IStorageService _storageService;
        private readonly IStateService _stateService;
        private readonly IHostingEnvironment _hostingEnv;
        private readonly ILogger<PingService> _logger;

        public PingService(
            IGMailService gmailService,
            IBinanceService binanceService,
            IHostingEnvironment hostingEnv,
            IStorageService storageService,
            IStateService stateService,
            ILogger<PingService> logger)
        {
            _gmailService = gmailService;
            _binanceService = binanceService;
            _storageService = storageService;
            _hostingEnv = hostingEnv;
            _stateService = stateService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogDebug($"PingService is starting.");

            stoppingToken.Register(() =>
            {
                _logger.LogDebug($" PingService background task is stopping.");
            });

            _logger.LogDebug($"PingService task doing background work.");

            await InitiateAsync(true);

            await Task.Delay(Timeout.Infinite, stoppingToken);
        }

        public async Task InitiateAsync(bool execute)
        {
            _logger.LogInformation($"Creating GMAIL credentials...");
            await _gmailService.CreateCredentialsAsync();

            if (!_gmailService.IsTokenValid())
            {
                _logger.LogError($"GMAIL token is not valid!");
            }

            _logger.LogInformation($"InitiateAsync is envoked...");

            var jsonEnvName = _hostingEnv.IsDevelopment() ? "test.json" : "prod.json";
            var stateExists = await _storageService.ExistsAsync("state", jsonEnvName);

            if (!stateExists)
            {
                var newState = new StateModel
                {
                    Active = execute,
                    CompletedOrders = null,
                    OpenOrders = null,
                    BTCPrice = 0,
                    LastUpdated = DateTimeOffset.Now
                };
                await _storageService.UploadAsync("state", jsonEnvName, JsonConvert.SerializeObject(newState));
            }

            while (true)
            {
                _logger.LogInformation($"Handling alerts and updating state...");

                try
                {
                    var state = await _stateService.GetAsync();
                    var price = await _binanceService.GetPriceAsync();

                    if (price == null)
                    {
                        _logger.LogError($"Unable to retrieve price from Binance. Price was null - bailing out...");
                        continue;
                    }

                    if (state != null && state.Active && price != null)
                    {
                        var messages = _gmailService.GetList();

                        List<OrderModel> orderedTrades = new List<OrderModel>();

                        var blobOrders = await _storageService.DownloadAsync("trades", jsonEnvName);
                        var orders = JsonConvert.DeserializeObject<List<OrderModel>>(blobOrders);

                        var blobShares = await _storageService.DownloadAsync("shares", jsonEnvName);
                        var shares = JsonConvert.DeserializeObject<List<ShareModel>>(blobShares);

                        var accountInfo = await _binanceService.GetAccountInfoAsync();
                        if (accountInfo == null)
                        {
                            _logger.LogError($"Unable to retrieve accountInfo from Binance. AccountInfo was null - bailing out...");
                            continue;
                        }

                        decimal? btcAmount = null;
                        decimal? usdtAmount = null;
                        decimal? portfolioValue = null;
                        if (accountInfo != null && accountInfo.Balances != null && accountInfo.Balances.Any())
                        {
                            _logger.LogInformation($"accountInfo: {JsonConvert.SerializeObject(accountInfo)}");

                            btcAmount = accountInfo.Balances?.SingleOrDefault(x => x.Asset == "BTC")?.Free;
                            usdtAmount = accountInfo.Balances?.SingleOrDefault(x => x.Asset == "USDT")?.Free;
                            portfolioValue = usdtAmount + (btcAmount * price);

                            _logger.LogInformation($"btcAmount: {btcAmount}");
                            _logger.LogInformation($"usdtAmount: {usdtAmount}");
                            _logger.LogInformation($"price: {price}");
                            _logger.LogInformation($"portfolioValue: {portfolioValue}");
                        }

                        var initialPortfolioValue = (decimal)800.0;

                        var alertMessages = messages.Select(x => _gmailService.GetMessageAlert(x.Id)).OrderBy(x => x.CreatedAt).ToList();
                        if (alertMessages != null && alertMessages.Any())
                        {
                            foreach (var alertMessage in alertMessages)
                            {
                                var existingTrade = orders.SingleOrDefault(x => x.MessageId == alertMessage.MessageId);
                                if (existingTrade != null)
                                {
                                    _logger.LogInformation($"Trade was already found - bailing out... Trade: {JsonConvert.SerializeObject(existingTrade)}");
                                    continue;
                                }

                                var previousTrade = (orders == null || !orders.Any()) ? null : orders.OrderByDescending(x => x.CreatedAt).ToList().FirstOrDefault();

                                if (alertMessage.Bear)
                                {
                                    if (alertMessage.Buy)
                                    {
                                        decimal rate = alertMessage.PercentageToTrade / (decimal)100;
                                        var usdToBuyWith = rate * usdtAmount;
                                        var quantity = usdToBuyWith / price;
                                        var trade = _hostingEnv.IsDevelopment() ? await _binanceService.CreateTestOrderAsync() : await _binanceService.CreateOrderAsync(OrderSide.Buy, quantity);
                                        var newTrade = CreateTradeOrder(alertMessage, trade, btcAmount, usdtAmount, price, quantity);
                                        if (newTrade == null)
                                        {
                                            continue;
                                        }
                                        orders.Add(newTrade);
                                    }

                                    if (alertMessage.Sell)
                                    {
                                        decimal rate = alertMessage.PercentageToTrade / (decimal)100;
                                        var quantity = rate * btcAmount;
                                        var trade = _hostingEnv.IsDevelopment() ? await _binanceService.CreateTestOrderAsync() : await _binanceService.CreateOrderAsync(OrderSide.Sell, quantity);
                                        var newTrade = CreateTradeOrder(alertMessage, trade, btcAmount, usdtAmount, price, quantity);
                                        if (newTrade == null)
                                        {
                                            continue;
                                        }
                                        orders.Add(newTrade);
                                    }
                                }

                                if (alertMessage.Bull)
                                {
                                    if (alertMessage.Sell)
                                    {
                                        decimal rate = alertMessage.PercentageToTrade / (decimal)100;
                                        var quantity = rate * btcAmount;
                                        var trade = _hostingEnv.IsDevelopment() ? await _binanceService.CreateTestOrderAsync() : await _binanceService.CreateOrderAsync(OrderSide.Sell, quantity);
                                        var newTrade = CreateTradeOrder(alertMessage, trade, btcAmount, usdtAmount, price, quantity);
                                        if (newTrade == null)
                                        {
                                            continue;
                                        }
                                        orders.Add(newTrade);
                                    }

                                    if (alertMessage.Buy)
                                    {
                                        decimal rate = alertMessage.PercentageToTrade / (decimal)100;
                                        var usdToBuyWith = rate * usdtAmount;
                                        var quantity = usdToBuyWith / price;
                                        var trade = _hostingEnv.IsDevelopment() ? await _binanceService.CreateTestOrderAsync() : await _binanceService.CreateOrderAsync(OrderSide.Buy, quantity);
                                        var newTrade = CreateTradeOrder(alertMessage, trade, btcAmount, usdtAmount, price, quantity);
                                        if (newTrade == null)
                                        {
                                            continue;
                                        }
                                        orders.Add(newTrade);
                                    }
                                }

                                _gmailService.MarkMessageAsUnread(alertMessage.MessageId);
                            }
                        }

                        var openOrders = await _binanceService.GetOpenOrdersAsync();
                        if (openOrders == null)
                        {
                            _logger.LogError($"Unable to retrieve openOrders from Binance. OpenOrders was null - bailing out...");
                            continue;
                        }

                        var updatedOrders = await UpdateCompletedOrdersAsync(orders);
                        var allOrders = updatedOrders.Concat(orders.Where(x => !x.Completed)).ToList();

                        foreach (var share in shares)
                        {
                            share.Value = portfolioValue == null ? null : portfolioValue * (share.Amount / (decimal)100);
                            share.Profit = (1 - (share.InitialValue / share.Value)) * 100;
                        }

                        var newState = new StateModel
                        {
                            Active = state.Active,
                            CompletedOrders = allOrders.Where(x => x.Completed).OrderByDescending(x => x.CreatedAt).ToList(),
                            OpenOrders = allOrders.Where(x => !x.Completed).OrderByDescending(x => x.CreatedAt).ToList(),
                            Shares = shares,
                            LastUpdated = DateTimeOffset.Now,
                            BTCAmount = btcAmount,
                            USDTAmount = usdtAmount,
                            PortfolioValue = portfolioValue,
                            BTCPrice = price,
                            BTCAmountValue = btcAmount * price,
                            Profit = portfolioValue == null ? null : (1 - (initialPortfolioValue / portfolioValue)) * 100,
                            InPercentage = portfolioValue == null || btcAmount == null ? null : ((btcAmount * price) / portfolioValue) * 100,
                            GmailTokenIsValid = _gmailService.IsTokenValid()
                        };

                        orderedTrades = allOrders.OrderByDescending(x => x.CreatedAt).ToList();
                        await _storageService.UploadAsync("shares", jsonEnvName, JsonConvert.SerializeObject(shares));
                        await _storageService.UploadAsync("trades", jsonEnvName, JsonConvert.SerializeObject(orderedTrades));
                        await _storageService.UploadAsync("state", jsonEnvName, JsonConvert.SerializeObject(newState));
                    }

                    _logger.LogInformation($"Alerts handled and state updated! Sleeping until next round..");
                    
                    Thread.Sleep(5*60*1000);
                }
                catch (Exception e)
                {
                    _logger.LogError($"PingService background task is stopping with an exception: {e.Message}, and innerException: {e.InnerException}, and stacktrace: {e.StackTrace}");
                }
            }
        }

        private async Task<List<OrderModel>> UpdateCompletedOrdersAsync(List<OrderModel> currentOrders)
        {
            if (currentOrders == null || !currentOrders.Any())
            {
                return currentOrders;
            }

            currentOrders = currentOrders.OrderByDescending(x => x.CreatedAt).ToList();
            var ordersToUpdate = currentOrders.Where(x => !x.Completed).ToList();
            for (int i = 0; i < ordersToUpdate.Count(); i++)
            {
                var order = await _binanceService.GetOrderAsync(ordersToUpdate[i].OrderId);
                if (order != null && order.Status == OrderStatus.Filled)
                {
                    var index = Array.FindIndex(currentOrders.ToArray(), row => row.OrderId == order.OrderId);
                    var previousTrade = index == currentOrders.Count() - 1 ? null : currentOrders[index + 1];
                    currentOrders[i].Completed = true;

                    var btcAmount = currentOrders[i].BTCAmount;
                    var usdtAmount = currentOrders[i].USDTAmount;

                    currentOrders[i].BTCAmount = order.Side == OrderSide.Buy ? currentOrders[i].BTCAmount + order.ExecutedQuantity : currentOrders[i].BTCAmount - order.ExecutedQuantity;
                    currentOrders[i].USDTAmount = order.Side == OrderSide.Buy ? currentOrders[i].USDTAmount - order.CummulativeQuoteQuantity : currentOrders[i].USDTAmount + order.CummulativeQuoteQuantity;
                    currentOrders[i].PortfolioValue = currentOrders[i].USDTAmount + (currentOrders[i].BTCAmount * currentOrders[i].BTCPrice);
                    currentOrders[i].Profit = previousTrade == null ? null : (1 - (previousTrade.PortfolioValue / currentOrders[i].PortfolioValue)) * 100;
                    currentOrders[i].Buy = order.Side == OrderSide.Buy;
                    currentOrders[i].Sell = order.Side == OrderSide.Sell;
                    currentOrders[i].BTCToTradeAmount = order.ExecutedQuantity;
                    currentOrders[i].BTCToTradeAmountValue = order.CummulativeQuoteQuantity;
                }
            }

            return currentOrders.Where(x => x.Completed).OrderByDescending(x => x.CreatedAt).ToList();
        }

        private OrderModel CreateTradeOrder(AlertMessage alertMessage, BinancePlacedOrder trade, decimal? btcAmount, decimal? usdtAmount, decimal? currentBtcPrice, decimal? quantity)
        {
            if (trade == null)
            {
                _logger.LogError($"Trade was null. Bailing out.");
            }

            var model = new OrderModel
            {
                Id = Guid.NewGuid(),
                CreatedAt = alertMessage.CreatedAt,
                OrderId = trade.OrderId,
                MessageId = alertMessage.MessageId,
                Completed = false,
                USDTAmount = usdtAmount,
                BTCAmount = btcAmount,
                BTCToTradeAmount = trade.ExecutedQuantity,
                BTCToTradeAmountValue = trade.CummulativeQuoteQuantity,
                BTCPrice = trade.Price == 0 ? currentBtcPrice : trade.Price,
                PercentageToTrade = alertMessage.PercentageToTrade,
                Buy = alertMessage.Buy,
                Sell = alertMessage.Sell
            };

            return model;
        }
    }
}
