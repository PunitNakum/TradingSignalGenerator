# 📈 Trading Signal Console App

This is a .NET Core 8 console application that listens for trading signals via a REST API (e.g. from Postman or TradingView webhook), simulates price monitoring, and logs if stop-loss or target is hit. Market prices are fetched live from Binance for simulation purposes.

---

## 📦 Features

- ✅ Receive Buy/Sell trading signals via HTTP POST
- ✅ Track entry price, stop loss, and target
- ✅ Live price fetch (e.g. BTCUSDT from Binance)
- ✅ Background monitoring of open trades
- ✅ REST API to view current trades
- ✅ Dockerized for easy deployment

---

## 📁 Project Structure
```bash
TradingSignalConsoleApp/
├── Program.cs
├── TradingSignalConsoleApp.csproj
├── Dockerfile
└── README.md
```

---

## 🚀 How to Run Locally

### 🧰 Prerequisites

- [.NET 8 SDK]
- [Postman]

---
### 🔧 Build & Run

```bash
dotnet restore
dotnet build
dotnet run
```

### Console output should show:
```bash
Listening on http://localhost:5001
```

## API Usage
1. 🔁 Send Trade Signal (POST)
```bash
URL: http://localhost:5001/api/signal/
Method: POST
Headers: Content-Type: application/json
```
### Example Request Body
```bash
{
  "symbol": "BTCUSDT",
  "side": "Buy",
  "entryPrice": 68000,
  "stopLoss": 67000,
  "target": 70000
}
```

2. 📄 View All Trades (GET)
```bash
URL: http://localhost:5001/api/trades/
Method: GET
```
Returns a JSON array of current and closed trades.

# 🐳 Docker Instructions
## 🧰 Prerequisites
Docker Desktop

 ### Build Docker Image
 ```bash
 docker build -t trading-signal-app .
 ```
 ### Run Container
 ```bash
 docker run -d -p 5001:5001 --name trading-signal trading-signal-app
```
# ⚙️ Notes
Currently fetches only BTCUSDT from Binance.
Simulates one symbol, but can be extended to multiple.
Make sure port 5001 is open in Docker or any reverse proxy.

### Example Log Output
```bash
Time : 2025-06-06 12:30:01 :: BTCUSDT = 68032.5
Time : 2025-06-06 12:30:02 :: Signal: BTCUSDT Buy @ 68000
Time : 2025-06-06 12:30:08 :: Target Hit: BTCUSDT at 70012.3