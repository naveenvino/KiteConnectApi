# KiteConnectApi User Manual

## 1. Introduction

The KiteConnectApi is a comprehensive application designed to interact with the Kite Connect API for various trading and analysis functionalities. It provides features for:

*   **Authentication:** Secure access to the API.
*   **Backtesting:** Evaluate trading strategies against historical data.
*   **Notifications:** Receive alerts and updates via different channels (Email, SMS, Telegram).
*   **Order Management:** Place and manage trading orders.
*   **Portfolio Management:** Monitor and analyze your trading portfolio.
*   **Quotes:** Retrieve real-time market data.
*   **Screener:** Filter and identify trading opportunities based on predefined criteria.
*   **Strategy Management:** Define, configure, and execute trading strategies.
*   **Real-time Trading:** Execute trades based on strategies and market conditions.

## 2. Getting Started

This section guides you through setting up and running the KiteConnectApi application.

### 2.1. Prerequisites

Before you begin, ensure you have the following installed:

*   **.NET SDK:** The application is built with .NET. You can download it from the official Microsoft website.
*   **SQL Server LocalDB:** Used as the default database.
*   **Kite Connect API Key and Secret:** Obtain these from your Kite Connect developer account.

### 2.2. Configuration

The `appsettings.json` file contains crucial configuration settings.

*   **`ConnectionStrings:DefaultConnection`**: Configure your database connection string. The default uses SQL Server LocalDB.
    ```json
    "ConnectionStrings": {
        "DefaultConnection": "Server=(localdb)\mssqllocaldb;Database=KiteConnectApiDb;Trusted_Connection=True;MultipleActiveResultSets=true"
    }
    ```
*   **`UseSimulatedServices`**: Set to `true` for simulated trading (no real trades are placed) or `false` for live trading.
    ```json
    "UseSimulatedServices": true
    ```
*   **`KiteConnect`**: Your Kite Connect API Key and Secret should be set as environment variables. The application reads them as `KiteConnect__ApiKey` and `KiteConnect__ApiSecret`.
    ```json
    "KiteConnect": {
        "AccessToken": "YOUR_ACCESS_TOKEN_HERE" // This is typically obtained via the login flow, not set directly here.
    }
    ```
    *Note: The `AccessToken` is usually obtained dynamically after a successful login flow and not directly configured in `appsettings.json`.* 
*   **`Jwt:Enabled`**: Set to `true` to enable JWT authentication for API endpoints. If `false`, authentication is disabled.
    ```json
    "Jwt": {
        "Enabled": false,
        "Key": "THIS_IS_A_VERY_STRONG_SECRET_KEY_AND_SHOULD_BE_KEPT_CONFIDENTIAL_AND_LONG_ENOUGH",
        "Issuer": "KiteConnectApi",
        "Audience": "KiteConnectApiUsers",
        "ExpireDays": 7
    }
    ```
    If `Jwt:Enabled` is `true`, ensure you set a strong `Key`.
*   **`NiftyOptionStrategy`**: Configuration for the Nifty option trading strategy.
*   **`RiskParameters`**: Define your risk management parameters.
*   **`Notification`**: Configure email notification settings.

### 2.3. Database Migrations

After configuring the connection string, apply the database migrations:

1.  Open your terminal or command prompt in the project root directory (`D:\New folder\KiteConnectApi`).
2.  Run the following command:
    ```bash
    dotnet ef database update
    ```
    This will create the necessary database schema.

### 2.4. Running the Application

To start the application:

1.  Open your terminal or command prompt in the project root directory (`D:\New folder\KiteConnectApi`).
2.  Run the following command:
    ```bash
    dotnet run
    ```
    The application will start, and you will see output indicating the URLs it's listening on (e.g., `https://localhost:7000`).

### 2.5. Accessing the API

Once the application is running, you can access the API documentation and test endpoints using Swagger UI:

*   Open your web browser and navigate to `https://localhost:7000/swagger` (replace `7000` with the actual port if different).

    This interface allows you to explore available API endpoints, understand their parameters, and send requests directly from the browser.

## 3. Functionalities and Testing

This section details each API functionality and provides step-by-step instructions on how to test them.

### 3.1. AuthController

**Purpose:** Handles user authentication and generates JWT tokens for authorized access to other API endpoints.

**Endpoints:**

*   **`POST /api/v1/Auth/login`**
    *   **Description:** Authenticates a user with a username and password. If successful, returns a JWT token.
    *   **Request Body:**
        ```json
        {
            "username": "string",
            "password": "string"
        }
        ```
    *   **Example Request (using `curl`):**
        ```bash
        curl -X POST "https://localhost:7000/api/v1/Auth/login" \
             -H "Content-Type: application/json" \
             -d "{ \"username\": \"admin\", \"password\": \"password\" }"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9..."
        }
        ```
    *   **Expected Response (Unauthorized):**
        ```
        HTTP/1.1 401 Unauthorized
        ```

**Testing:**

1.  **Enable JWT (if not already):** In `appsettings.json`, set `"Jwt:Enabled": true`.
2.  **Start the application:** Run `dotnet run` from the project root.
3.  **Access Swagger UI:** Open `https://localhost:7000/swagger` in your browser.
4.  **Locate AuthController:** Expand the `Auth` section.
5.  **Try `POST /api/v1/Auth/login`:**
    *   Click "Try it out".
    *   Enter `admin` for username and `password` for password.
    *   Click "Execute".
    *   Verify that you receive a JWT token in the response.
6.  **Test unauthorized access:**
    *   Repeat step 5 with incorrect credentials (e.g., `username: "wrong", password: "123"`)
    *   Verify that you receive a `401 Unauthorized` response.

### 3.2. BacktestingController

**Purpose:** Allows users to run backtests on trading strategies using historical data.

**Endpoints:**

*   **`POST /api/Backtesting`**
    *   **Description:** Initiates a backtest for a given symbol, date range, and interval.
    *   **Query Parameters:**
        *   `symbol` (string, required): The trading symbol (e.g., "NIFTY").
        *   `fromDate` (string, required): Start date for backtesting (e.g., "2023-01-01").
        *   `toDate` (string, required): End date for backtesting (e.g., "2023-01-31").
        *   `interval` (string, required): Data interval (e.g., "day", "minute").
    *   **Example Request (using `curl`):**
        ```bash
        curl -X POST "https://localhost:7000/api/Backtesting?symbol=NIFTY&fromDate=2023-01-01&toDate=2023-01-31&interval=day" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "initialCapital": 100000.0,
            "finalCapital": 105000.0,
            "totalProfitLoss": 5000.0,
            "profitLossPercentage": 5.0,
            "totalTrades": 10,
            "winningTrades": 7,
            "losingTrades": 3,
            "winRate": 70.0,
            "maxDrawdown": 2.5,
            "trades": [
                // ... array of trade details
            ]
        }
        ```
    *   **Expected Response (Bad Request):**
        ```
        HTTP/1.1 400 Bad Request
        Symbol, fromDate, toDate, and interval are required.
        ```

**Testing:**

1.  **Start the application:** Run `dotnet run` from the project root.
2.  **Access Swagger UI:** Open `https://localhost:7000/swagger` in your browser.
3.  **Locate BacktestingController:** Expand the `Backtesting` section.
4.  **Try `POST /api/Backtesting`:**
    *   Click "Try it out".
    *   Enter sample values for `symbol` (e.g., `NIFTY`), `fromDate` (e.g., `2023-01-01`), `toDate` (e.g., `2023-01-31`), and `interval` (e.g., `day`).
    *   Click "Execute".
    *   Verify that you receive a `200 OK` response with backtest results.
5.  **Test with missing parameters:**
    *   Repeat step 4, but leave one or more required parameters empty.
    *   Verify that you receive a `400 Bad Request` response.

### 3.3. NotificationController

**Purpose:** Manages user notification preferences across different channels (Email, SMS, Telegram).

**Endpoints:**

*   **`GET /api/Notification`**
    *   **Description:** Retrieves all notification preferences.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Notification" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        [
            {
                "id": "string",
                "userId": "string",
                "channel": "Email",
                "value": "user@example.com",
                "isActive": true
            }
        ]
        ```

*   **`GET /api/Notification/{id}`**
    *   **Description:** Retrieves a specific notification preference by its ID.
    *   **Path Parameters:**
        *   `id` (string, required): The ID of the notification preference.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Notification/your-preference-id" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "id": "string",
            "userId": "string",
            "channel": "Email",
            "value": "user@example.com",
            "isActive": true
        }
        ```
    *   **Expected Response (Not Found):**
        ```
        HTTP/1.1 404 Not Found
        ```

*   **`POST /api/Notification`**
    *   **Description:** Adds a new notification preference.
    *   **Request Body:**
        ```json
        {
            "userId": "string",
            "channel": "Email", // or "Sms", "Telegram"
            "value": "string",  // email address, phone number, or Telegram chat ID
            "isActive": true
        }
        ```
    *   **Example Request (using `curl`):**
        ```bash
        curl -X POST "https://localhost:7000/api/Notification" \
             -H "Content-Type: application/json" \
             -d "{ \"userId\": \"user123\", \"channel\": \"Email\", \"value\": \"test@example.com\", \"isActive\": true }"
        ```
    *   **Expected Response (Success - 201 Created):**
        ```json
        {
            "id": "string",
            "userId": "user123",
            "channel": "Email",
            "value": "test@example.com",
            "isActive": true
        }
        ```

*   **`PUT /api/Notification/{id}`**
    *   **Description:** Updates an existing notification preference.
    *   **Path Parameters:**
        *   `id` (string, required): The ID of the notification preference to update.
    *   **Request Body:**
        ```json
        {
            "id": "string",      // Must match the ID in the URL
            "userId": "string",
            "channel": "Email",
            "value": "string",
            "isActive": true
        }
        ```
    *   **Example Request (using `curl`):**
        ```bash
        curl -X PUT "https://localhost:7000/api/Notification/your-preference-id" \
             -H "Content-Type: application/json" \
             -d "{ \"id\": \"your-preference-id\", \"userId\": \"user123\", \"channel\": \"Email\", \"value\": \"updated@example.com\", \"isActive\": false }"
        ```
    *   **Expected Response (Success - 204 No Content):**
        ```
        HTTP/1.1 204 No Content
        ```
    *   **Expected Response (Bad Request):**
        ```
        HTTP/1.1 400 Bad Request
        Notification preference ID mismatch.
        ```
    *   **Expected Response (Not Found):**
        ```
        HTTP/1.1 404 Not Found
        ```

*   **`DELETE /api/Notification/{id}`**
    *   **Description:** Deletes a notification preference by its ID.
    *   **Path Parameters:**
        *   `id` (string, required): The ID of the notification preference to delete.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X DELETE "https://localhost:7000/api/Notification/your-preference-id" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success - 204 No Content):**
        ```
        HTTP/1.1 204 No Content
        ```
    *   **Expected Response (Not Found):**
        ```
        HTTP/1.1 404 Not Found
        ```

**Testing:**

1.  **Start the application:** Run `dotnet run` from the project root.
2.  **Access Swagger UI:** Open `https://localhost:7000/swagger` in your browser.
3.  **Locate NotificationController:** Expand the `Notification` section.

4.  **Test `POST /api/Notification` (Add a preference):**
    *   Click "Try it out".
    *   Provide a sample `userId`, `channel` (e.g., "Email"), `value` (e.g., "test@example.com"), and `isActive` (e.g., `true`).
    *   Click "Execute".
    *   Note the `id` from the successful `201 Created` response. This ID will be used for subsequent tests.

5.  **Test `GET /api/Notification` (Get all preferences):**
    *   Click "Try it out".
    *   Click "Execute".
    *   Verify that the preference you just added is listed in the response.

6.  **Test `GET /api/Notification/{id}` (Get a specific preference):**
    *   Click "Try it out".
    *   Enter the `id` obtained from step 4.
    *   Click "Execute".
    *   Verify that the correct preference details are returned.
    *   Test with a non-existent ID to verify a `404 Not Found` response.

7.  **Test `PUT /api/Notification/{id}` (Update a preference):**
    *   Click "Try it out".
    *   Enter the `id` obtained from step 4.
    *   Modify the `value` or `isActive` field in the request body (e.g., change `value` to "updated@example.com" and `isActive` to `false`). Ensure the `id` in the body matches the path parameter.
    *   Click "Execute".
    *   Verify a `204 No Content` response.
    *   Use `GET /api/Notification/{id}` to confirm the update.
    *   Test with a mismatched ID in the URL and body to verify a `400 Bad Request`.
    *   Test with a non-existent ID to verify a `404 Not Found` response.

8.  **Test `DELETE /api/Notification/{id}` (Delete a preference):**
    *   Click "Try it out".
    *   Enter the `id` obtained from the `POST` request.
    *   Click "Execute".
    *   Verify a `204 No Content` response.
    *   Use `GET /api/Notification/{id}` with the same ID to confirm deletion (should return `404 Not Found`).

### 3.4. OrdersController

**Purpose:** Manages trading orders, including placing, modifying, canceling, and retrieving order history.

**Endpoints:**

*   **`GET /api/Orders`**
    *   **Description:** Retrieves a list of all active orders from Kite Connect.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Orders" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        [
            {
                "order_id": "string",
                "exchange": "string",
                "trading_symbol": "string",
                "transaction_type": "string",
                "quantity": 0,
                "price": 0.0,
                "status": "string"
                // ... other order details
            }
        ]
        ```

*   **`GET /api/Orders/history`**
    *   **Description:** Retrieves the historical record of all orders stored in the application's database.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Orders/history" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        [
            {
                "id": "string",
                "orderId": "string",
                "tradingSymbol": "string",
                "exchange": "string",
                "transactionType": "string",
                "quantity": 0,
                "price": 0.0,
                "orderType": "string",
                "product": "string",
                "status": "string",
                "orderTimestamp": "2023-07-08T10:00:00Z"
                // ... other historical order details
            }
        ]
        ```

*   **`POST /api/Orders`**
    *   **Description:** Places a new order.
    *   **Request Body:**
        ```json
        {
            "exchange": "string",       // e.g., "NSE", "NFO"
            "tradingSymbol": "string",  // e.g., "NIFTY23JULFUT"
            "transactionType": "string",// e.g., "BUY", "SELL"
            "quantity": 0,
            "product": "string",        // e.g., "MIS", "CNC"
            "orderType": "string",      // e.g., "MARKET", "LIMIT"
            "price": 0.0                // Required for LIMIT orders
        }
        ```
    *   **Example Request (using `curl`):**
        ```bash
        curl -X POST "https://localhost:7000/api/Orders" \
             -H "Content-Type: application/json" \
             -d "{ \"exchange\": \"NSE\", \"tradingSymbol\": \"RELIANCE\", \"transactionType\": \"BUY\", \"quantity\": 1, \"product\": \"CNC\", \"orderType\": \"MARKET\" }"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "order_id": "string",
            "status": "string" // e.g., "COMPLETE", "OPEN"
            // ... other order placement details
        }
        ```

*   **`PUT /api/Orders/{orderId}`**
    *   **Description:** Modifies an existing order.
    *   **Path Parameters:**
        *   `orderId` (string, required): The ID of the order to modify.
    *   **Request Body:**
        ```json
        {
            "quantity": 0,          // Optional: New quantity
            "price": 0.0,           // Optional: New price (for LIMIT orders)
            "orderType": "string"   // Optional: New order type (e.g., "LIMIT")
        }
        ```
    *   **Example Request (using `curl`):**
        ```bash
        curl -X PUT "https://localhost:7000/api/Orders/your-order-id" \
             -H "Content-Type: application/json" \
             -d "{ \"quantity\": 2, \"price\": 1500.50, \"orderType\": \"LIMIT\" }"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "order_id": "string",
            "status": "string" // e.g., "COMPLETE", "OPEN"
            // ... other order modification details
        }
        ```

*   **`DELETE /api/Orders/{orderId}`**
    *   **Description:** Cancels an existing order.
    *   **Path Parameters:**
        *   `orderId` (string, required): The ID of the order to cancel.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X DELETE "https://localhost:7000/api/Orders/your-order-id" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "order_id": "string",
            "status": "string" // e.g., "CANCELLED"
            // ... other order cancellation details
        }
        ```

**Testing:**

1.  **Start the application:** Run `dotnet run` from the project root.
2.  **Access Swagger UI:** Open `https://localhost:7000/swagger` in your browser.
3.  **Locate OrdersController:** Expand the `Orders` section.

4.  **Test `POST /api/Orders` (Place a new order):**
    *   Click "Try it out".
    *   Provide valid parameters for `exchange`, `tradingSymbol`, `transactionType`, `quantity`, `product`, and `orderType`. For a `LIMIT` order, also provide `price`.
    *   Click "Execute".
    *   Note the `order_id` from the successful response. This will be used for modify and cancel tests.

5.  **Test `GET /api/Orders` (Get active orders):**
    *   Click "Try it out".
    *   Click "Execute".
    *   Verify that the order you just placed (if still active) is listed.

6.  **Test `GET /api/Orders/history` (Get order history):**
    *   Click "Try it out".
    *   Click "Execute".
    *   Verify that the order you placed is present in the historical list.

7.  **Test `PUT /api/Orders/{orderId}` (Modify an order):**
    *   Click "Try it out".
    *   Enter the `orderId` obtained from the `POST` request.
    *   Modify `quantity`, `price`, or `orderType` in the request body.
    *   Click "Execute".
    *   Verify a successful response.
    *   Use `GET /api/Orders` to confirm the modification (if the order is still active).

8.  **Test `DELETE /api/Orders/{orderId}` (Cancel an order):**
    *   Click "Try it out".
    *   Enter the `orderId` obtained from the `POST` request.
    *   Click "Execute".
    *   Verify a `204 No Content` response.
    *   Use `GET /api/Orders` to confirm the order is no longer active or its status is "CANCELLED".
    *   Use `GET /api/Orders/history` to confirm the order's status is updated to "CANCELLED" in the history.

### 3.5. PortfolioController

**Purpose:** Manages portfolio-related functionalities, including retrieving holdings, positions, and calculating overall Profit & Loss (P&L).

**Endpoints:**

*   **`GET /Portfolio/holdings`**
    *   **Description:** Retrieves the user's current stock holdings.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/Portfolio/holdings" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        [
            {
                "tradingsymbol": "string",
                "exchange": "string",
                "isin": "string",
                "t1_quantity": 0,
                "realised_quantity": 0,
                "quantity": 0,
                "authorised_quantity": 0,
                "product": "string",
                "average_price": 0.0,
                "last_price": 0.0,
                "close_price": 0.0,
                "pnl": 0.0,
                "day_change": 0.0,
                "day_change_percentage": 0.0
            }
        ]
        ```

*   **`GET /Portfolio/positions`**
    *   **Description:** Retrieves the user's current open positions.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/Portfolio/positions" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        [
            {
                "tradingsymbol": "string",
                "exchange": "string",
                "product": "string",
                "quantity": 0,
                "average_price": 0.0,
                "last_price": 0.0,
                "realised": 0.0,
                "unrealised": 0.0,
                "buy_quantity": 0,
                "sell_quantity": 0,
                "buy_price": 0.0,
                "sell_price": 0.0,
                "buy_value": 0.0,
                "sell_value": 0.0,
                "day_buy_quantity": 0,
                "day_sell_quantity": 0,
                "day_buy_price": 0.0,
                "day_sell_price": 0.0,
                "day_buy_value": 0.0,
                "day_sell_value": 0.0
            }
        ]
        ```

*   **`GET /Portfolio/pnl`**
    *   **Description:** Calculates and returns the total realized, unrealized, and overall Profit & Loss from both holdings and positions.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/Portfolio/pnl" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "totalRealizedPnl": 0.0,
            "totalUnrealizedPnl": 0.0,
            "overallPnl": 0.0
        }
        ```

**Testing:**

1.  **Start the application:** Run `dotnet run` from the project root.
2.  **Access Swagger UI:** Open `https://localhost:7000/swagger` in your browser.
3.  **Locate PortfolioController:** Expand the `Portfolio` section.

4.  **Test `GET /Portfolio/holdings`:**
    *   Click "Try it out".
    *   Click "Execute".
    *   Verify that you receive a `200 OK` response with your holdings data. (Note: This will only return data if you have actual holdings in your Kite Connect account).

5.  **Test `GET /Portfolio/positions`:**
    *   Click "Try it out".
    *   Click "Execute".
    *   Verify that you receive a `200 OK` response with your open positions data. (Note: This will only return data if you have actual open positions in your Kite Connect account).

6.  **Test `GET /Portfolio/pnl`:**
    *   Click "Try it out".
    *   Click "Execute".
    *   Verify that you receive a `200 OK` response with the calculated P&L. The values will depend on your actual holdings and positions.

### 3.6. QuotesController

**Purpose:** Provides real-time and historical market data for specified instruments.

**Endpoints:**

*   **`GET /api/Quotes`**
    *   **Description:** Retrieves real-time quotes for a list of instruments.
    *   **Query Parameters:**
        *   `instruments` (array of string, required): A comma-separated list of instrument tokens or exchange-traded symbols (e.g., `NSE:INFY,BSE:RELIANCE`).
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Quotes?instruments=NSE:INFY,BSE:RELIANCE" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):
        ```json
        {
            "NSE:INFY": {
                "instrument_token": 12345,
                "last_price": 1500.50,
                "ohlc": {
                    "open": 1490.0,
                    "high": 1510.0,
                    "low": 1485.0,
                    "close": 1500.0
                },
                "change": 10.50,
                "net_change": 0.70,
                "volume": 1000000
                // ... other quote details
            },
            "BSE:RELIANCE": {
                // ... quote details for Reliance
            }
        }
        ```
    *   **Expected Response (Bad Request):**
        ```
        HTTP/1.1 400 Bad Request
        Instrument list cannot be empty.
        ```

*   **`GET /api/Quotes/historical`**
    *   **Description:** Retrieves historical data for a single instrument within a specified date range and interval.
    *   **Query Parameters:**
        *   `instrumentToken` (string, required): The instrument token for which to retrieve historical data.
        *   `from` (string, required): Start date for historical data (e.g., "2023-01-01").
        *   `to` (string, required): End date for historical data (e.g., "2023-01-31").
        *   `interval` (string, required): Data interval (e.g., "day", "minute").
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Quotes/historical?instrumentToken=12345&from=2023-01-01&to=2023-01-31&interval=day" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        [
            {
                "date": "2023-01-01T00:00:00Z",
                "open": 100.0,
                "high": 105.0,
                "low": 99.0,
                "close": 103.0,
                "volume": 100000
            }
            // ... array of historical data points
        ]
        ```
    *   **Expected Response (Bad Request):**
        ```
        HTTP/1.1 400 Bad Request
        Instrument token is required.
        ```

**Testing:**

1.  **Start the application:** Run `dotnet run` from the project root.
2.  **Access Swagger UI:** Open `https://localhost:7000/swagger` in your browser.
3.  **Locate QuotesController:** Expand the `Quotes` section.

4.  **Test `GET /api/Quotes` (Get real-time quotes):**
    *   Click "Try it out".
    *   Enter a comma-separated list of instruments (e.g., `NSE:INFY,BSE:RELIANCE`). You might need to find valid instrument tokens or symbols from Kite Connect documentation or a live feed.
    *   Click "Execute".
    *   Verify that you receive a `200 OK` response with quote data.
    *   Test with an empty `instruments` list to verify a `400 Bad Request` response.

5.  **Test `GET /api/Quotes/historical` (Get historical data):**
    *   Click "Try it out".
    *   Enter a valid `instrumentToken` (e.g., `12345`), `from` date (e.g., `2023-01-01`), `to` date (e.g., `2023-01-31`), and `interval` (e.g., `day`).
    *   Click "Execute".
    *   Verify that you receive a `200 OK` response with historical data.
    *   Test with an empty `instrumentToken` to verify a `400 Bad Request` response.

### 3.7. ScreenerController

**Purpose:** Manages screener criteria and runs market screening based on defined criteria.

**Endpoints:**

*   **`GET /api/Screener`**
    *   **Description:** Retrieves all saved screener criteria.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Screener" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        [
            {
                "id": "string",
                "name": "string",
                "minPrice": 0.0,
                "maxPrice": 0.0,
                "minVolume": 0,
                "maxVolume": 0,
                "minChange": 0.0,
                "maxChange": 0.0
                // ... other criteria fields
            }
        ]
        ```

*   **`GET /api/Screener/{id}`**
    *   **Description:** Retrieves a specific screener criteria by its ID.
    *   **Path Parameters:**
        *   `id` (string, required): The ID of the screener criteria.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Screener/your-criteria-id" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "id": "string",
            "name": "string",
            "minPrice": 0.0,
            "maxPrice": 0.0,
            "minVolume": 0,
            "maxVolume": 0,
            "minChange": 0.0,
            "maxChange": 0.0
            // ... other criteria fields
        }
        ```
    *   **Expected Response (Not Found):**
        ```
        HTTP/1.1 404 Not Found
        ```

*   **`POST /api/Screener`**
    *   **Description:** Adds a new screener criteria.
    *   **Request Body:**
        ```json
        {
            "name": "string",
            "minPrice": 0.0,
            "maxPrice": 0.0,
            "minVolume": 0,
            "maxVolume": 0,
            "minChange": 0.0,
            "maxChange": 0.0
            // ... other criteria fields
        }
        ```
    *   **Example Request (using `curl`):**
        ```bash
        curl -X POST "https://localhost:7000/api/Screener" \
             -H "Content-Type: application/json" \
             -d "{ \"name\": \"High Volume Stocks\", \"minVolume\": 1000000, \"minChange\": 0.05 }"
        ```
    *   **Expected Response (Success - 201 Created):n        ```json
        {
            "id": "string",
            "name": "High Volume Stocks",
            "minVolume": 1000000,
            "minChange": 0.05
            // ... other criteria fields
        }
        ```

*   **`PUT /api/Screener/{id}`**
    *   **Description:** Updates an existing screener criteria.
    *   **Path Parameters:**
        *   `id` (string, required): The ID of the screener criteria to update.
    *   **Request Body:**
        ```json
        {
            "id": "string",      // Must match the ID in the URL
            "name": "string",
            "minPrice": 0.0,
            "maxPrice": 0.0,
            "minVolume": 0,
            "maxVolume": 0,
            "minChange": 0.0,
            "maxChange": 0.0
            // ... other criteria fields
        }
        ```
    *   **Example Request (using `curl`):**
        ```bash
        curl -X PUT "https://localhost:7000/api/Screener/your-criteria-id" \
             -H "Content-Type: application/json" \
             -d "{ \"id\": \"your-criteria-id\", \"name\": \"High Volume Stocks Updated\", \"minVolume\": 2000000 }"
        ```
    *   **Expected Response (Success - 204 No Content):**
        ```
        HTTP/1.1 204 No Content
        ```
    *   **Expected Response (Bad Request):**
        ```
        HTTP/1.1 400 Bad Request
        Screener criteria ID mismatch.
        ```
    *   **Expected Response (Not Found):**
        ```
        HTTP/1.1 404 Not Found
        ```

*   **`DELETE /api/Screener/{id}`**
    *   **Description:** Deletes a screener criteria by its ID.
    *   **Path Parameters:**
        *   `id` (string, required): The ID of the screener criteria to delete.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X DELETE "https://localhost:7000/api/Screener/your-criteria-id" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success - 204 No Content):**
        ```
        HTTP/1.1 204 No Content
        ```
    *   **Expected Response (Not Found):**
        ```
        HTTP/1.1 404 Not Found
        ```

*   **`POST /api/Screener/run/{id}`**
    *   **Description:** Runs the market screener based on the specified criteria ID.
    *   **Path Parameters:**
        *   `id` (string, required): The ID of the screener criteria to run.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X POST "https://localhost:7000/api/Screener/run/your-criteria-id" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        [
            {
                "instrument_token": 12345,
                "exchange_token": 1,
                "tradingsymbol": "INFY",
                "name": "Infosys",
                "last_price": 1500.50,
                "ohlc": {
                    "open": 1490.0,
                    "high": 1510.0,
                    "low": 1485.0,
                    "close": 1500.0
                },
                "change": 10.50,
                "net_change": 0.70,
                "volume": 1000000
                // ... other instrument details that match the criteria
            }
        ]
        ```
    *   **Expected Response (Not Found):**
        ```
        HTTP/1.1 404 Not Found
        Screener criteria not found.
        ```

**Testing:**

1.  **Start the application:** Run `dotnet run` from the project root.
2.  **Access Swagger UI:** Open `https://localhost:7000/swagger` in your browser.
3.  **Locate ScreenerController:** Expand the `Screener` section.

4.  **Test `POST /api/Screener` (Add a screener criteria):**
    *   Click "Try it out".
    *   Provide a sample `name` (e.g., "My First Screener"), and some criteria like `minVolume` (e.g., `100000`).
    *   Click "Execute".
    *   Note the `id` from the successful `201 Created` response. This ID will be used for subsequent tests.

5.  **Test `GET /api/Screener` (Get all screener criteria):**
    *   Click "Try it out".
    *   Click "Execute".
    *   Verify that the criteria you just added is listed in the response.

6.  **Test `GET /api/Screener/{id}` (Get a specific screener criteria):**
    *   Click "Try it out".
    *   Enter the `id` obtained from step 4.
    *   Click "Execute".
    *   Verify that the correct criteria details are returned.
    *   Test with a non-existent ID to verify a `404 Not Found` response.

7.  **Test `PUT /api/Screener/{id}` (Update a screener criteria):**
    *   Click "Try it out".
    *   Enter the `id` obtained from step 4.
    *   Modify the `name` or other criteria fields in the request body. Ensure the `id` in the body matches the path parameter.
    *   Click "Execute".
    *   Verify a `204 No Content` response.
    *   Use `GET /api/Screener/{id}` to confirm the update.
    *   Test with a mismatched ID in the URL and body to verify a `400 Bad Request`.
    *   Test with a non-existent ID to verify a `404 Not Found` response.

8.  **Test `POST /api/Screener/run/{id}` (Run screener):**
    *   Click "Try it out".
    *   Enter the `id` obtained from step 4.
    *   Click "Execute".
    *   Verify that you receive a `200 OK` response with a list of instruments matching the criteria.
    *   Test with a non-existent ID to verify a `404 Not Found` response.

9.  **Test `DELETE /api/Screener/{id}` (Delete a screener criteria):**
    *   Click "Try it out".
    *   Enter the `id` obtained from the `POST` request.
    *   Click "Execute".
    *   Verify a `204 No Content` response.
    *   Use `GET /api/Screener/{id}` with the same ID to confirm deletion (should return `404 Not Found`).

### 3.8. StrategyController

**Purpose:** Manages trading strategies, including retrieving, adding, updating, and deleting strategy configurations.

**Endpoints:**

*   **`GET /api/Strategy/total-allocated-capital`**
    *   **Description:** Retrieves the total capital allocated across all active strategies.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Strategy/total-allocated-capital" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        100000.0
        ```

*   **`GET /api/Strategy`**
    *   **Description:** Retrieves all active strategy configurations.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Strategy" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        [
            {
                "id": "string",
                "name": "string",
                "isActive": true,
                "allocatedCapital": 0.0,
                "riskParameters": {
                    "maxDailyLossPercentage": 0.0,
                    "maxPerTradeLossPercentage": 0.0,
                    "maxOpenPositions": 0,
                    "totalCapital": 0.0
                }
                // ... other strategy configuration fields
            }
        ]
        ```

*   **`GET /api/Strategy/{id}`**
    *   **Description:** Retrieves a specific strategy configuration by its ID.
    *   **Path Parameters:**
        *   `id` (string, required): The ID of the strategy configuration.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X GET "https://localhost:7000/api/Strategy/your-strategy-id" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "id": "string",
            "name": "string",
            "isActive": true,
            "allocatedCapital": 0.0,
            "riskParameters": {
                "maxDailyLossPercentage": 0.0,
                "maxPerTradeLossPercentage": 0.0,
                "maxOpenPositions": 0,
                "totalCapital": 0.0
            }
            // ... other strategy configuration fields
        }
        ```
    *   **Expected Response (Not Found):**
        ```
        HTTP/1.1 404 Not Found
        ```

*   **`POST /api/Strategy`**
    *   **Description:** Adds a new strategy configuration.
    *   **Request Body:**
        ```json
        {
            "name": "string",
            "isActive": true,
            "allocatedCapital": 0.0,
            "riskParameters": {
                "maxDailyLossPercentage": 0.0,
                "maxPerTradeLossPercentage": 0.0,
                "maxOpenPositions": 0,
                "totalCapital": 0.0
            }
            // ... other strategy configuration fields
        }
        ```
    *   **Example Request (using `curl`):**
        ```bash
        curl -X POST "https://localhost:7000/api/Strategy" \
             -H "Content-Type: application/json" \
             -d "{ \"name\": \"My New Strategy\", \"isActive\": true, \"allocatedCapital\": 50000.0 }"
        ```
    *   **Expected Response (Success - 201 Created):**
        ```json
        {
            "id": "string",
            "name": "My New Strategy",
            "isActive": true,
            "allocatedCapital": 50000.0
            // ... other strategy configuration fields
        }
        ```

*   **`PUT /api/Strategy/{id}`**
    *   **Description:** Updates an existing strategy configuration.
    *   **Path Parameters:**
        *   `id` (string, required): The ID of the strategy configuration to update.
    *   **Request Body:**
        ```json
        {
            "id": "string",      // Must match the ID in the URL
            "name": "string",
            "isActive": true,
            "allocatedCapital": 0.0,
            "riskParameters": {
                "maxDailyLossPercentage": 0.0,
                "maxPerTradeLossPercentage": 0.0,
                "maxOpenPositions": 0,
                "totalCapital": 0.0
            }
            // ... other strategy configuration fields
        }
        ```
    *   **Example Request (using `curl`):**
        ```bash
        curl -X PUT "https://localhost:7000/api/Strategy/your-strategy-id" \
             -H "Content-Type: application/json" \
             -d "{ \"id\": \"your-strategy-id\", \"name\": \"Updated Strategy Name\", \"isActive\": false }"
        ```
    *   **Expected Response (Success - 204 No Content):**
        ```
        HTTP/1.1 204 No Content
        ```
    *   **Expected Response (Bad Request):**
        ```
        HTTP/1.1 400 Bad Request
        Strategy ID mismatch.
        ```
    *   **Expected Response (Not Found):**
        ```
        HTTP/1.1 404 Not Found
        ```

*   **`DELETE /api/Strategy/{id}`**
    *   **Description:** Deletes a strategy configuration by its ID.
    *   **Path Parameters:**
        *   `id` (string, required): The ID of the strategy configuration to delete.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X DELETE "https://localhost:7000/api/Strategy/your-strategy-id" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success - 204 No Content):**
        ```
        HTTP/1.1 204 No Content
        ```
    *   **Expected Response (Not Found):**
        ```
        HTTP/1.1 404 Not Found
        ```

**Testing:**

1.  **Start the application:** Run `dotnet run` from the project root.
2.  **Access Swagger UI:** Open `https://localhost:7000/swagger` in your browser.
3.  **Locate StrategyController:** Expand the `Strategy` section.

4.  **Test `POST /api/Strategy` (Add a strategy configuration):**
    *   Click "Try it out".
    *   Provide a sample `name` (e.g., "My Test Strategy"), `isActive` (e.g., `true`), and `allocatedCapital` (e.g., `25000.0`). You can also include `riskParameters`.
    *   Click "Execute".
    *   Note the `id` from the successful `201 Created` response. This ID will be used for subsequent tests.

5.  **Test `GET /api/Strategy` (Get all active strategies):**
    *   Click "Try it out".
    *   Click "Execute".
    *   Verify that the strategy you just added is listed in the response.

6.  **Test `GET /api/Strategy/total-allocated-capital` (Get total allocated capital):**
    *   Click "Try it out".
    *   Click "Execute".
    *   Verify that the response reflects the sum of `allocatedCapital` for all active strategies.

7.  **Test `GET /api/Strategy/{id}` (Get a specific strategy configuration):**
    *   Click "Try it out".
    *   Enter the `id` obtained from step 4.
    *   Click "Execute".
    *   Verify that the correct strategy details are returned.
    *   Test with a non-existent ID to verify a `404 Not Found` response.

8.  **Test `PUT /api/Strategy/{id}` (Update a strategy configuration):**
    *   Click "Try it out".
    *   Enter the `id` obtained from step 4.
    *   Modify the `name`, `isActive`, or `allocatedCapital` fields in the request body. Ensure the `id` in the body matches the path parameter.
    *   Click "Execute".
    *   Verify a `204 No Content` response.
    *   Use `GET /api/Strategy/{id}` to confirm the update.
    *   Test with a mismatched ID in the URL and body to verify a `400 Bad Request`.
    *   Test with a non-existent ID to verify a `404 Not Found` response.

9.  **Test `DELETE /api/Strategy/{id}` (Delete a strategy configuration):**
    *   Click "Try it out".
    *   Enter the `id` obtained from the `POST` request.
    *   Click "Execute".
    *   Verify a `204 No Content` response.
    *   Use `GET /api/Strategy/{id}` with the same ID to confirm deletion (should return `404 Not Found`).

### 3.9. TradingController

**Purpose:** Handles incoming TradingView alerts and provides an endpoint for manually exiting all positions.

**Endpoints:**

*   **`POST /api/Trading/alert`**
    *   **Description:** Processes an incoming alert from TradingView. This endpoint is typically configured as a webhook in TradingView.
    *   **Request Body:**
        ```json
        {
            "symbol": "string",
            "exchange": "string",
            "transactionType": "string", // e.g., "BUY", "SELL"
            "quantity": 0,
            "orderType": "string",       // e.g., "MARKET", "LIMIT"
            "price": 0.0,                // Optional, for LIMIT orders
            "product": "string",         // e.g., "MIS", "CNC"
            "strategyName": "string",    // Name of the strategy triggering the alert
            "alertMessage": "string"     // Raw alert message from TradingView
        }
        ```
    *   **Example Request (using `curl`):**
        ```bash
        curl -X POST "https://localhost:7000/api/Trading/alert" \
             -H "Content-Type: application/json" \
             -d "{ \"symbol\": \"NIFTY\", \"exchange\": \"NFO\", \"transactionType\": \"BUY\", \"quantity\": 50, \"orderType\": \"MARKET\", \"strategyName\": \"MyOptionStrategy\", \"alertMessage\": \"NIFTY BUY Signal\" }"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "status": "AlertProcessed"
        }
        ```

*   **`POST /api/Trading/exit-all`**
    *   **Description:** Manually exits all open positions. This can be used as a panic button or for end-of-day square-off.
    *   **Example Request (using `curl`):**
        ```bash
        curl -X POST "https://localhost:7000/api/Trading/exit-all" \
             -H "accept: */*"
        ```
    *   **Expected Response (Success):**
        ```json
        {
            "status": "Exit all command processed."
        }
        ```

**Testing:**

1.  **Start the application:** Run `dotnet run` from the project root.
2.  **Access Swagger UI:** Open `https://localhost:7000/swagger` in your browser.
3.  **Locate TradingController:** Expand the `Trading` section.

4.  **Test `POST /api/Trading/alert` (Handle TradingView Alert):**
    *   Click "Try it out".
    *   Provide a sample `TradingViewAlert` JSON body. Ensure the `symbol`, `exchange`, `transactionType`, `quantity`, `orderType`, and `strategyName` are relevant.
    *   Click "Execute".
    *   Verify that you receive a `200 OK` response with `"status": "AlertProcessed"`.
    *   *Note: For a real test, you would configure a webhook in TradingView to send alerts to this endpoint.*

5.  **Test `POST /api/Trading/exit-all` (Exit All Positions):**
    *   Click "Try it out".
    *   Click "Execute".
    *   Verify that you receive a `200 OK` response with `"status": "Exit all command processed."`.
    *   *Note: This action will attempt to square off all open positions. Use with caution in a live trading environment.*

---