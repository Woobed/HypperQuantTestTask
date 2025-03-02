using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace HyperQuantTestTask.Connector
{
    public class AccountService
    {
        private readonly HttpClient _httpClient;
        private readonly Dictionary<string, decimal> _balances = new()
        {
            {"BTC", 1},
            {"XRP", 15000},
            {"XMR", 50},
            {"DSH", 30} //Bitfinex используют тикер DSH а не DASH
        };

        public AccountService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<Dictionary<string, decimal>> GetPortfolioBalanceAsync()
        {
            var rates = await GetExchangeRatesAsync();
            var convertedBalances = new Dictionary<string, decimal>();

            foreach (var currency in _balances.Keys)
            {
                convertedBalances[currency] = _balances.Sum(b => b.Value * GetRate(rates, b.Key, currency));
            }

            return convertedBalances;
        }

        private async Task<Dictionary<string, decimal>> GetExchangeRatesAsync()
        {
            var response = await _httpClient.GetStringAsync("https://api.bitfinex.com/v2/tickers?symbols=tBTCUSD,tXRPUSD,tXMRUSD,tDSHUSD");
            var data = JsonSerializer.Deserialize<List<List<JsonElement>>>(response);

            var rates = data.ToDictionary(
                item => item[0].ToString().Substring(1, item[0].ToString().Length - 4),
                item => (decimal)item[7].GetDouble()
            );
            
            return rates;
        }

        private decimal GetRate(Dictionary<string, decimal> rates, string from, string to)
        {
            if (from == to) return 1;
            if (to == "USDT" && rates.ContainsKey(from)) return rates[from];
            if (from == "USDT" && rates.ContainsKey(to)) return 1 / rates[to];
            if (rates.ContainsKey(from) && rates.ContainsKey(to)) return rates[from] / rates[to];

            return 0;
        }
    }
}
