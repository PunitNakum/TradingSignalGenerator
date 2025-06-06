using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace TradingSignalConsoleApp
{
    public class Program
    {
        public static List<TradeSignal> trades = new List<TradeSignal>();
        static Dictionary<string, decimal> marketPrices = new Dictionary<string, decimal>();
        static Random random = new Random();
        static async Task Main(string[] args)
        {
            // Creating background task to simulate random market prices and monitor trades
            _ = Task.Run(FetchLivePrices);
            _ = Task.Run(MonitorTrades);

            HttpListener listener = new HttpListener();
            listener.Prefixes.Add("http://+:5001/api/signal/");
            listener.Prefixes.Add("http://+:5001/api/trades/");
            listener.Start();
            Console.WriteLine($" Time : {DateTime.Now} :: Listening on http://localhost:5001");

            while (true)
            {
                var context = await listener.GetContextAsync();
                var request = context.Request;
                var response = context.Response;

                if (request.HttpMethod == "POST" && request.Url.AbsolutePath == "/api/signal/")
                {
                    using var reader = new System.IO.StreamReader(request.InputStream);
                    var body = await reader.ReadToEndAsync();
                    var signal = JsonSerializer.Deserialize<TradeSignal>(body);

                    bool hasOpenTrade = trades.Any(t => t.Symbol == signal.Symbol && t.Status == "OPEN");

                    if (hasOpenTrade)
                    {
                        byte[] errorBuffer = Encoding.UTF8.GetBytes($"Time : {DateTime.Now} :: Trade already open for this symbol.");
                        response.StatusCode = 400;
                        response.ContentLength64 = errorBuffer.Length;
                        await response.OutputStream.WriteAsync(errorBuffer, 0, errorBuffer.Length);
                        response.OutputStream.Close();
                        continue;
                    }
                    trades.Add(signal);

                    if (!marketPrices.ContainsKey(signal.Symbol))
                        marketPrices[signal.Symbol] = signal.EntryPrice;

                    Console.WriteLine($"Time : {DateTime.Now} :: Signal: {signal.Symbol} {signal.Side} @ {signal.EntryPrice}");

                    byte[] buffer = Encoding.UTF8.GetBytes("Signal received");
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                else if (request.HttpMethod == "GET" && request.Url.AbsolutePath == "/api/trades/")
                {
                    var json = JsonSerializer.Serialize(trades, new JsonSerializerOptions { WriteIndented = true });
                    byte[] buffer = Encoding.UTF8.GetBytes(json);
                    response.ContentLength64 = buffer.Length;
                    await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                }
                else
                {
                    response.StatusCode = 404;
                }

                response.OutputStream.Close();
            }
        }


        /// <summary>
        /// Simulates random market prices for trading symbols.
        /// </summary>
        /// <returns></returns>
        static async Task SimulatePrices()
        {
            while (true)
            {
                foreach (var symbol in marketPrices.Keys)
                {
                    var current = marketPrices[symbol];
                    var change = (decimal)(random.NextDouble() * 100 - 50);
                    marketPrices[symbol] = Math.Round(current + change, 2);
                    Console.WriteLine($"Time : {DateTime.Now} :: {symbol} = {marketPrices[symbol]}");
                }
                await Task.Delay(5000);
            }
        }

        #region Fetch live data from binance broker
        static async Task FetchLivePrices()
        {
            using HttpClient client = new HttpClient();

            while (true)
            {
                string binanceSymbol = "BTCUSDT"; // here we can create a list of symbols to fetch prices
                string url = $"https://api.binance.com/api/v3/ticker/price?symbol={binanceSymbol}";

                try
                {
                    var response = await client.GetStringAsync(url);
                    var result = JsonSerializer.Deserialize<BinancePrice>(response);
                    if (decimal.TryParse(result.Price, out decimal livePrice))
                    {
                        marketPrices[binanceSymbol] = Math.Round(livePrice, 2);
                        Console.WriteLine($"{binanceSymbol} = {marketPrices[binanceSymbol]}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching price for {binanceSymbol}: {ex.Message}");
                }

                await Task.Delay(5000);
            }
        }

        public class BinancePrice
        {
            [JsonPropertyName("symbol")]
            public string Symbol { get; set; }

            [JsonPropertyName("price")]
            public string Price { get; set; }
        }

        #endregion


        /// <summary>
        /// Monitors trades and checks if stop loss or target is hit.
        /// </summary>
        /// <returns></returns>
        static async Task MonitorTrades()
        {
            while (true)
            {
                foreach (var trade in trades)
                {
                    if (trade.Status != "OPEN") continue;

                    var currentPrice = marketPrices[trade.Symbol];

                    if (trade.Side == TradeSide.Buy)
                    {
                        if (currentPrice <= trade.StopLoss)
                        {
                            trade.Status = "STOP_LOSS_HIT";
                            Console.WriteLine($"Time : {DateTime.Now} :: SL Hit: {trade.Symbol} at {currentPrice}");
                        }
                        else if (currentPrice >= trade.Target)
                        {
                            trade.Status = "TARGET_HIT";
                            Console.WriteLine($"Time : {DateTime.Now} :: Target Hit: {trade.Symbol} at {currentPrice}");
                        }
                    }
                    else if (trade.Side == TradeSide.Sell)
                    {
                        if (currentPrice >= trade.StopLoss)
                        {
                            trade.Status = "STOP_LOSS_HIT";
                            Console.WriteLine($"Time : {DateTime.Now} :: SL Hit: {trade.Symbol} at {currentPrice}");
                        }
                        else if (currentPrice <= trade.Target)
                        {
                            trade.Status = "TARGET_HIT";
                            Console.WriteLine($"Time : {DateTime.Now} :: Target Hit: {trade.Symbol} at {currentPrice}");
                        }
                    }
                }

                await Task.Delay(5000);
            }
        }

        #region TradingSignal
        /// <summary>
        /// Trading Signal class representing a trading signal which we'll receive from the postman API.
        /// </summary>
        public class TradeSignal
        {
            [JsonPropertyName("symbol")]
            public string Symbol { get; set; }
            [JsonConverter(typeof(JsonStringEnumConverter))]
            public TradeSide Side { get; set; }
            [JsonPropertyName("entryPrice")]
            public decimal EntryPrice { get; set; }
            [JsonPropertyName("stopLoss")]
            public decimal StopLoss { get; set; }
            [JsonPropertyName("target")]
            public decimal Target { get; set; }
            public string Status { get; set; } = "OPEN";
        }

        public enum TradeSide
        {
            Buy,
            Sell
        }
        #endregion
    }
}