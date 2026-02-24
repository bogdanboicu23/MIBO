# MIBO Integration Examples

## How MIBO Can Use the Financial Service API

### Example 1: "Give me all my expenses for the last month, structured by category in a pie chart"

**MIBO would call:**
```
GET /api/analytics/expenses/user/{userId}/categories?period=lastmonth
```

**Response provides:**
```json
{
  "userId": 1,
  "startDate": "2024-01-01",
  "endDate": "2024-01-31",
  "totalAmount": 3456.78,
  "currency": "USD",
  "categories": [
    {
      "category": "groceries",
      "amount": 890.50,
      "percentage": 25.7,
      "count": 23,
      "color": "#4CAF50"
    },
    {
      "category": "dining",
      "amount": 650.25,
      "percentage": 18.8,
      "count": 15,
      "color": "#FF9800"
    }
    // ... more categories
  ],
  "chartData": {
    "labels": ["groceries", "dining", "transport", ...],
    "values": [890.50, 650.25, 450.00, ...],
    "percentages": [25.7, 18.8, 13.0, ...],
    "colors": ["#4CAF50", "#FF9800", "#2196F3", ...]
  }
}
```

✅ **Ready for pie chart visualization** - includes labels, values, percentages, and colors

### Example 2: "Show me my spending trends over the last 3 months"

**MIBO would call:**
```
GET /api/analytics/expenses/user/{userId}/timeseries?period=last90days
```

**Response provides:**
```json
{
  "userId": 1,
  "startDate": "2023-11-01",
  "endDate": "2024-01-31",
  "timeSeries": [
    {"date": "2023-11-01", "amount": 145.50, "count": 3},
    {"date": "2023-11-02", "amount": 89.25, "count": 2},
    // ... daily data
  ],
  "chartData": {
    "labels": ["2023-11-01", "2023-11-02", ...],
    "values": [145.50, 89.25, ...]
  }
}
```

✅ **Ready for line/bar chart visualization**

### Example 3: "What's my current balance across all accounts?"

**MIBO would call:**
```
GET /api/summary/user/{userId}/balance
```

**Response provides:**
```json
{
  "userId": 1,
  "totalBalance": 45678.90,
  "currency": "USD",
  "accounts": {
    "Checking Account": 12345.67,
    "Savings Account": 30000.00,
    "Credit Account": 3333.23
  }
}
```

### Example 4: "List my transactions from last week"

**MIBO would call:**
```
GET /api/transactions/user/{userId}?period=lastweek
```

Or with date range:
```
GET /api/transactions/user/{userId}?startDate=2024-01-22&endDate=2024-01-28
```

### Example 5: "How much did I spend on dining this month?"

**MIBO would call:**
```
GET /api/expenses/user/{userId}?category=dining&period=thismonth
```

### Example 6: "Show me expense statistics"

**MIBO would call:**
```
GET /api/analytics/expenses/user/{userId}/statistics?period=thismonth
```

**Response provides:**
```json
{
  "average": 45.67,
  "median": 35.00,
  "min": 5.00,
  "max": 450.00,
  "mostExpensiveCategory": "rent",
  "mostFrequentCategory": "groceries",
  "totalTransactions": 156
}
```

## Supported Period Keywords

The API supports these natural language period keywords:
- `today`, `yesterday`
- `thisweek`, `lastweek`
- `thismonth`, `lastmonth`
- `last30days`, `last90days`
- `thisyear`, `lastyear`

## Key Features for MIBO

1. **Chart-Ready Data**: Responses include `chartData` sections with pre-formatted arrays for direct visualization
2. **Color Coding**: Each category includes hex colors for consistent chart rendering
3. **Percentages Calculated**: All percentage calculations are done server-side
4. **Flexible Date Filtering**: Support for both date ranges and period keywords
5. **Aggregated Analytics**: Pre-calculated statistics, summaries, and breakdowns
6. **Pagination**: All list endpoints support pagination for large datasets

## Natural Language to API Mapping

| User Says | API Call |
|-----------|----------|
| "expenses last month" | `/api/expenses/user/{id}?period=lastmonth` |
| "spending by category" | `/api/analytics/expenses/user/{id}/categories` |
| "balance" | `/api/summary/user/{id}/balance` |
| "transactions today" | `/api/transactions/user/{id}?period=today` |
| "budget status" | `/api/budgets/user/{id}?isActive=true` |
| "expense trends" | `/api/analytics/expenses/user/{id}/timeseries` |

This structure allows MIBO to easily translate natural language queries into API calls and receive data formatted for immediate presentation or visualization.