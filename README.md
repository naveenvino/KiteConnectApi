# Kite Connect .Net library
The official .Net client for communicating with [Kite Connect API](https://kite.trade) is kept in https://github.com/zerodha/dotnetkiteconnect.git refer that and build our application.

if you have doubts go through kite api .net doc or refer kite office .net githug https://github.com/zerodha/dotnetkiteconnect.git .

Kite Connect is a set of REST-like APIs that expose many capabilities required to build a complete investment and trading platform. Execute orders in real time, manage user portfolio, stream live market data (WebSockets), and more, with the simple HTTP API collection.

[Zerodha Technology Pvt. Ltd.](http://zerodha.com) &copy; 2023. Licensed under the [MIT License](/license/).

## Documentation

* [.Net Library](https://kite.trade/docs/kiteconnectdotnet/)
* [HTTP API](https://kite.trade/docs/connect/)

## Install Client Library


### Using NuGet

Execute in **Tools** &raquo; **NuGet Package Manager** &raquo; **Package Manager Console**

```
Install-Package Tech.Zerodha.KiteConnect
```
### Using .Net CLI

```
dotnet add package Tech.Zerodha.KiteConnect
```

## Getting started
```csharp
// Import library
using KiteConnect;

// Initialize Kiteconnect using apiKey. Enabling Debug will give logs of requests and responses
Kite kite = new Kite(MyAPIKey, Debug: true);

// Collect login url to authenticate user. Load this URL in browser or WebView. 
// After successful authentication this will redirect to your redirect url with request token.
kite.GetLoginURL();

// Collect tokens and user details using the request token
User user = kite.GenerateSession(RequestToken, MySecret);

// Persist these tokens in database or settings
string MyAccessToken = user.AccessToken;
string MyPublicToken = user.PublicToken;

// Initialize Kite APIs with access token
kite.SetAccessToken(MyAccessToken);

// Set session expiry callback. Method can be separate function also.
kite.SetSessionExpiryHook(() => Console.WriteLine("Need to login again"));

// Example call for functions like "PlaceOrder" that returns Dictionary
Dictionary<string, dynamic> response = kite.PlaceOrder(
    Exchange: Constants.EXCHANGE_CDS,
    TradingSymbol: "USDINR17AUGFUT",
    TransactionType: Constants.TRANSACTION_TYPE_SELL,
    Quantity: 1,
    Price: 64.0000m,
    OrderType: Constants.ORDER_TYPE_MARKET,
    Product: Constants.PRODUCT_MIS
);
Console.WriteLine("Order Id: " + response["data"]["order_id"]);

// Example call for functions like "GetHoldings" that returns a data structure
List<Holding> holdings = kite.GetHoldings();
Console.WriteLine(holdings[0].AveragePrice);

```
For more examples, take a look at [Program.cs](https://github.com/zerodhatech/dotnetkiteconnect/blob/kite3/KiteConnectSample/Program.cs) of **KiteConnect Sample** project in this repository.

## WebSocket live streaming data

This library uses Events to get ticks. These events are non blocking and can be used without additional threads. Create event handlers and attach it to Ticker instance as shown in the example below.

```csharp
/* 
To get live price use KiteTicker websocket connection. 
It is recommended to use only one websocket connection at any point of time and make sure you stop connection, 
once user goes out of app.
*/

// Create a new Ticker instance
Ticker ticker = new Ticker(MyAPIKey, MyAccessToken);

// Add handlers to events
ticker.OnTick += onTick;
ticker.OnOrderUpdate += OnOrderUpdate;
ticker.OnReconnect += onReconnect;
ticker.OnNoReconnect += oNoReconnect;
ticker.OnError += onError;
ticker.OnClose += onClose;
ticker.OnConnect += onConnect;

// Engage reconnection mechanism and connect to ticker
ticker.EnableReconnect(Interval: 5,Retries: 50);
ticker.Connect();

// Subscribing to NIFTY50 and setting mode to LTP
ticker.Subscribe(Tokens: new UInt32[] { 256265 });
ticker.SetMode(Tokens: new UInt32[] { 256265 }, Mode: Constants.MODE_LTP);

// Example onTick handler
private static void onTick(Tick TickData)
{
    Console.WriteLine("LTP: " + TickData.LastPrice);
}

private static void OnOrderUpdate(Order OrderData)
{
    Console.WriteLine("OrderUpdate " + Utils.JsonSerialize(OrderData));
}

// Disconnect ticker before closing the application
ticker.Close();
```

For more details about different mode of quotes and subscribing for them, take a look at **KiteConnect Sample** project in this repository and [Kite Connect HTTP API documentation](https://kite.trade/docs/connect/v3).
