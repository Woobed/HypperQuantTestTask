using System.Net;
using System.Net.WebSockets;
using BitfinexConnector;
using HyperQuantTestTask.Connector;
using Microsoft.AspNetCore.Mvc;

namespace HyperQuantTestTask.Controllers
{
    public class AccountController : Controller
    {

        private AccountService _accountService;

        private BitfinexRestClient _bifinexRestClient = new BitfinexRestClient(new HttpClient());
        private BitfinexWSClient _bifinexWSClient = new BitfinexWSClient(new ClientWebSocket());

        
        public AccountController(AccountService accountService)
        {
            _accountService = accountService;
        }

        public async Task<ActionResult> Index()
        {
            var balanceDataGrid = await _accountService.GetPortfolioBalanceAsync();

            // проверка BitfinexRestClient

            var trades = await _bifinexRestClient.GetNewTradesAsync("BTCUSD", 2);
            var candles = await _bifinexRestClient.GetCandleSeriesAsync("BTCUSD", 60, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, 5);

            //

            // проверка BitfinexWSClient
            _bifinexWSClient.NewBuyTrade += trade =>
            {
                Console.WriteLine("New Buy Trade:");
                Console.WriteLine($"Id: {trade.Id}");
                Console.WriteLine($"Pair: {trade.Pair}");
                Console.WriteLine($"Price: {trade.Price}");
                Console.WriteLine($"Amount: {trade.Amount}");
                Console.WriteLine($"Side: {trade.Side}");
                Console.WriteLine($"Time: {trade.Time}");
                Console.WriteLine();
            };

            _bifinexWSClient.NewSellTrade += trade =>
            {
                Console.WriteLine("New Sell Trade:");
                Console.WriteLine($"Id: {trade.Id}");
                Console.WriteLine($"Pair: {trade.Pair}");
                Console.WriteLine($"Price: {trade.Price}");
                Console.WriteLine($"Amount: {trade.Amount}");
                Console.WriteLine($"Side: {trade.Side}");
                Console.WriteLine($"Time: {trade.Time}");
                Console.WriteLine();
            };

            _bifinexWSClient.CandleSeriesProcessing += candle =>
            {
                Console.WriteLine("New Candle:");
                Console.WriteLine($"Pair: {candle.Pair}");
                Console.WriteLine($"OpenTime: {candle.OpenTime}");
                Console.WriteLine($"OpenPrice: {candle.OpenPrice}");
                Console.WriteLine($"HighPrice: {candle.HighPrice}");
                Console.WriteLine($"LowPrice: {candle.LowPrice}");
                Console.WriteLine($"ClosePrice: {candle.ClosePrice}");
                Console.WriteLine($"TotalVolume: {candle.TotalVolume}");
                Console.WriteLine();
            };

            
            await _bifinexWSClient.ConnectAsync();

            
            _bifinexWSClient.SubscribeTrades("BTCUSD");
            _bifinexWSClient.SubscribeCandles("BTCUSD", 60, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow, 5);

           
            await Task.Delay(TimeSpan.FromSeconds(30));

            // Отписка и отключение
            _bifinexWSClient.UnsubscribeTrades("BTCUSD");
            _bifinexWSClient.UnsubscribeCandles("BTCUSD");
            await _bifinexWSClient.DisconnectAsync();
            //

            return View(balanceDataGrid);
        }


    }
}
