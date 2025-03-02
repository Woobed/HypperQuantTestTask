using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using HyperQuantTestTask.Interfaces;
using HyperQuantTestTask.Models;

namespace HyperQuantTestTask.Connector
{
    public class BitfinexWSClient : IWSConnector
    {
        private ClientWebSocket _webSocket = new ClientWebSocket();
        private readonly CancellationTokenSource _cts = new();
        private readonly Uri _uri = new("wss://api-pub.bitfinex.com/ws/2");
        private readonly Dictionary<int, string> _subscriptions = new();

        public event Action<Trade> NewBuyTrade;
        public event Action<Trade> NewSellTrade;
        public event Action<Candle> CandleSeriesProcessing;

        public async Task ConnectAsync()
        {

            if (_webSocket != null)
            {
                await DisconnectAsync();
            }

            _webSocket = new ClientWebSocket();
            await _webSocket.ConnectAsync(_uri, CancellationToken.None);
            _ = Task.Run(ReceiveMessagesAsync);
        }
        public async Task DisconnectAsync()
        {
            if (_webSocket != null && (_webSocket.State == WebSocketState.Open || _webSocket.State == WebSocketState.CloseReceived))
            {
                await _webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
            }
            _webSocket?.Dispose();
        }

        public async void SubscribeTrades(string pair, int maxCount = 100)
        {
            string message = $"{{\"event\":\"subscribe\", \"channel\":\"trades\", \"symbol\":\"t{pair.ToUpper()}\"}}";
            await SendMessageAsync(message);
        }
        public async void UnsubscribeTrades(string pair)
        {
            string message = $"{{\"event\":\"unsubscribe\", \"channel\":\"trades\", \"symbol\":\"t{pair.ToUpper()}\"}}";
            await SendMessageAsync(message);
        }

        public async void SubscribeCandles(string pair, int periodInSec, DateTimeOffset? from = null, DateTimeOffset? to = null, long? count = 0)
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

            string message = $"{{\"event\":\"subscribe\", \"channel\":\"candles\", \"key\":\"trade:{timeFrame}:t{pair.ToUpper()}\"}}";
            await SendMessageAsync(message);
        }

        public async void UnsubscribeCandles(string pair)
        {
            string message = $"{{\"event\":\"unsubscribe\", \"channel\":\"candles\", \"symbol\":\"t{pair.ToUpper()}\"}}";
            await SendMessageAsync(message);
        }

        private async Task SendMessageAsync(string message)
        {
            if (_webSocket.State == WebSocketState.Open)
            {
                var buffer = Encoding.UTF8.GetBytes(message);
                await _webSocket.SendAsync(new ArraySegment<byte>(buffer), WebSocketMessageType.Text, true, _cts.Token);
            }
        }

        private async Task ReceiveMessagesAsync()
        {
            var buffer = new byte[8192];

            while (_webSocket.State == WebSocketState.Open)
            {
                var result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    string jsonMessage = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    ProcessMessage(jsonMessage);
                }
            }
        }

        private void ProcessMessage(string message)
        {
            try
            {
                if (message.StartsWith("["))
                {
                    var elements = JsonSerializer.Deserialize<object[]>(message);
                    if (elements == null || elements.Length < 2) return;

                    if (elements[0] is int channelId && _subscriptions.TryGetValue(channelId, out string pair))
                    {
                        string channelType = elements[1]?.ToString();

                        if (channelType == "tu")
                        {
                            var tradeData = JsonSerializer.Deserialize<decimal[]>(elements[2].ToString());

                            if (tradeData != null && tradeData.Length >= 4)
                            {
                                var trade = new Trade
                                {
                                    Id = tradeData[0].ToString(),
                                    Pair = pair,
                                    Price = tradeData[3],
                                    Amount = Math.Abs(tradeData[2]),
                                    Side = tradeData[2] > 0 ? "buy" : "sell",
                                    Time = DateTimeOffset.FromUnixTimeMilliseconds((long)tradeData[1])
                                };

                                if (trade.Side == "buy")
                                    NewBuyTrade?.Invoke(trade);
                                else
                                    NewSellTrade?.Invoke(trade);
                            }
                        }

                        if (channelType == "candles")
                        {
                            var candleData = JsonSerializer.Deserialize<decimal[]>(elements[2].ToString());

                            if (candleData != null && candleData.Length >= 6)
                            {
                                var candle = new Candle
                                {
                                    Pair = pair,
                                    OpenTime = DateTimeOffset.FromUnixTimeMilliseconds((long)candleData[0]),
                                    OpenPrice = candleData[1],
                                    HighPrice = candleData[2],
                                    LowPrice = candleData[3],
                                    ClosePrice = candleData[4],
                                    TotalVolume = candleData[5],
                                    TotalPrice = candleData.Length > 6 ? candleData[6] : 0
                                };

                                CandleSeriesProcessing?.Invoke(candle);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке сообщения: {ex.Message}");
            }
        }
    }
}
