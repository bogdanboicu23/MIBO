# MIBO Finance Data Service

Mock financial API that provides banking and financial data following DummyJSON's API structure patterns.

## Running the Service

```bash
cd src/MIBO.FinanceDataService
dotnet run
```

The service will be available at `http://localhost:5100` with Swagger UI documentation.

## API Endpoints

### Accounts
- `GET /api/accounts` - Get all accounts with pagination
- `GET /api/accounts/{id}` - Get account by ID
- `GET /api/accounts/user/{userId}` - Get accounts by user ID
- `GET /api/accounts/number/{accountNumber}` - Get account by account number

### Transactions
- `GET /api/transactions` - Get all transactions with filtering and pagination
- `GET /api/transactions/{id}` - Get transaction by ID
- `GET /api/transactions/user/{userId}` - Get transactions by user ID
- `GET /api/transactions/account/{accountId}` - Get transactions by account ID
- `GET /api/transactions/search?q={query}` - Search transactions

### Expenses
- `GET /api/expenses` - Get all expenses with filtering and pagination
- `GET /api/expenses/{id}` - Get expense by ID
- `GET /api/expenses/user/{userId}` - Get expenses by user ID
- `GET /api/expenses/categories` - Get available expense categories

### Budgets
- `GET /api/budgets` - Get all budgets with filtering
- `GET /api/budgets/{id}` - Get budget by ID
- `GET /api/budgets/user/{userId}` - Get budgets by user ID

### Summary
- `GET /api/summary/user/{userId}` - Get complete financial summary
- `GET /api/summary/user/{userId}/balance` - Get balance summary
- `GET /api/summary/user/{userId}/expenses` - Get expenses summary

## Query Parameters

### Pagination
- `skip` - Number of items to skip (default: 0)
- `limit` - Number of items to return (default: 30)

### Filtering
- `userId` - Filter by user ID
- `accountId` - Filter by account ID (transactions)
- `type` - Filter by type (transactions: debit/credit/transfer)
- `category` - Filter by category
- `startDate` - Filter by start date
- `endDate` - Filter by end date
- `isActive` - Filter active/inactive (budgets)
- `period` - Filter by period (budgets: daily/weekly/monthly/yearly)

### Sorting
- `sortBy` - Field to sort by
- `order` - Sort order (asc/desc)

## Response Format

Paginated responses follow DummyJSON structure:
```json
{
  "items": [...],
  "total": 100,
  "skip": 0,
  "limit": 30
}
```

## Mock Data

The service generates mock data for 10 users with:
- 1-3 bank accounts per user
- 10-50 transactions per account
- Expenses linked to debit transactions
- 3-7 budgets per user

All data is generated using the Bogus library for realistic mock data.