using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HyperQuantTestTask.Interfaces;
using HyperQuantTestTask.Models;

namespace BitfinexConnector
{
    public class RestConnector : IRestConnector
    {
        private readonly HttpClient _httpClient;
        private const string Url = "https://api-pub.bitfinex.com/v2/";

        public RestConnector(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {
            var url = $"{Url}trades/t{pair}/hist?limit={maxCount}";
            var response = await _httpClient.GetStringAsync(url);
            var trades = JsonSerializer.Deserialize<List<List<object>>>(response);
            var result = new List<Trade>();

            foreach (var data in trades)
            {
                result.Add(new Trade
                {
                    Pair = pair,
                    Price = Convert.ToDecimal(data[3]),
                    Amount = Convert.ToDecimal(data[2]),
                    Side = Convert.ToDecimal(data[2]) > 0 ? "buy" : "sell",
                    Time = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(data[1])),
                    Id = data[0].ToString()
                });
            }
            return result;
        }

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            var url = $"{Url}candles/trade:{periodInSec}m:t{pair}/hist?limit={count}";
            var response = await _httpClient.GetStringAsync(url);
            var candles = JsonSerializer.Deserialize<List<List<object>>>(response);

            var result = new List<Candle>();

            foreach (var data in candles)
            {
                result.Add(new Candle
                {
                    Pair = pair,
                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(Convert.ToInt64(data[0])),
                    OpenPrice = Convert.ToDecimal(data[1]),
                    HighPrice = Convert.ToDecimal(data[2]),
                    LowPrice = Convert.ToDecimal(data[3]),
                    ClosePrice = Convert.ToDecimal(data[4]),
                    TotalVolume = Convert.ToDecimal(data[5])
                });
            }
            return result;
        }
    }
}
