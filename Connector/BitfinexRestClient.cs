using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using HyperQuantTestTask.Interfaces;
using HyperQuantTestTask.Models;

namespace BitfinexConnector
{
    public class BitfinexRestClient : IRestConnector
    {
        private readonly HttpClient _httpClient;
        private const string Url = "https://api-pub.bitfinex.com/v2/";

        public BitfinexRestClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<IEnumerable<Trade>> GetNewTradesAsync(string pair, int maxCount)
        {

            var url = $"{Url}trades/t{pair}/hist?limit={maxCount}";
            var response = await _httpClient.GetStringAsync(url);
            var trades = JsonSerializer.Deserialize<JsonElement>(response);
            var result = new List<Trade>();
            if (trades.ValueKind == JsonValueKind.Array)
            {
                foreach (var trade in trades.EnumerateArray())
                {
                    var data = trade.EnumerateArray().ToArray();

                    result.Add(new Trade
                    {
                        Pair = pair,
                        Id = data[0].ToString(),
                        Time = DateTimeOffset.FromUnixTimeMilliseconds(data[1].GetInt64()),
                        Amount = data[2].GetDecimal(),
                        Price = data[3].GetDecimal(),
                        Side = data[2].GetDecimal() > 0 ? "buy" : "sell"
                    });
                }
            }
            return result;
        }

        public async Task<IEnumerable<Candle>> GetCandleSeriesAsync(string pair, int periodInSec, DateTimeOffset? from, DateTimeOffset? to = null, long? count = 0)
        {
            string timeFrame = periodInSec switch
            {
                60 => "1m",
                300 => "5m",
                900 => "15m",
                3600 => "1h",
                86400 => "1D",
                _ => "1m"
            };

            var url = $"{Url}candles/trade:{timeFrame}:t{pair}/hist?limit={count}";
            var response = await _httpClient.GetStringAsync(url);
            var candles = JsonSerializer.Deserialize<JsonElement>(response);

            var result = new List<Candle>();
            if (candles.ValueKind == JsonValueKind.Array)
            {
                foreach (var data in candles.EnumerateArray())
                {
                    result.Add(new Candle
                    {
                        Pair = pair,
                        OpenTime = DateTimeOffset.FromUnixTimeMilliseconds(data[0].GetInt64()),
                        OpenPrice = data[1].GetDecimal(),
                        HighPrice = data[2].GetDecimal(),
                        LowPrice = data[3].GetDecimal(),
                        ClosePrice = data[4].GetDecimal(),
                        TotalVolume = data[5].GetDecimal()
                    });
                }
            }
            return result;
        }
    }
}
