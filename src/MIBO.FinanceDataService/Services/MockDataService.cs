using Bogus;
using MIBO.FinanceDataService.Models;

namespace MIBO.FinanceDataService.Services;

public class MockDataService
{
    private readonly List<Account> _accounts = new();
    private readonly List<Transaction> _transactions = new();
    private readonly List<Expense> _expenses = new();
    private readonly List<Budget> _budgets = new();
    private readonly Faker _faker = new();

    private readonly string[] _accountTypes = ["checking", "savings", "credit", "investment"];
    private readonly string[] _transactionTypes = ["debit", "credit", "transfer"];
    private readonly string[] _transactionCategories = ["food", "transport", "shopping", "entertainment", "bills", "healthcare", "education", "other"];
    private readonly string[] _expenseCategories = ["groceries", "dining", "transport", "shopping", "entertainment", "utilities", "rent", "healthcare", "education", "subscriptions", "other"];
    private readonly string[] _banks = ["Chase Bank", "Bank of America", "Wells Fargo", "Citibank", "Capital One", "TD Bank", "PNC Bank", "US Bank", "MAIB", "VictoriaBank", "OTP Bank", "Moldinconbank"];
    private readonly string[] _merchants = ["Amazon", "Walmart", "Target", "Starbucks", "McDonald's", "Uber", "Netflix", "Spotify", "Apple", "Google", "Shell", "Exxon", "Linella", ""];
    private readonly string[] _budgetPeriods = ["daily", "weekly", "monthly", "yearly"];

    public MockDataService()
    {
        GenerateMockData();
    }

    private void GenerateMockData()
    {
        // Generate accounts for 10 users
        for (int userId = 1; userId <= 10; userId++)
        {
            var numAccounts = _faker.Random.Int(1, 3);
            for (int i = 0; i < numAccounts; i++)
            {
                var account = new Account
                {
                    Id = _accounts.Count + 1,
                    UserId = userId,
                    AccountNumber = _faker.Finance.Account(12),
                    AccountName = $"{_faker.Random.ArrayElement(_accountTypes)} Account",
                    Type = _faker.Random.ArrayElement(_accountTypes),
                    Balance = _faker.Random.Decimal(100, 50000),
                    AvailableBalance = _faker.Random.Decimal(100, 50000),
                    Currency = "USD",
                    Bank = _faker.Random.ArrayElement(_banks),
                    Iban = _faker.Finance.Iban(),
                    Swift = _faker.Finance.Bic(),
                    IsActive = _faker.Random.Bool(0.9f),
                    CreatedAt = _faker.Date.Past(3),
                    UpdatedAt = _faker.Date.Recent(30)
                };
                _accounts.Add(account);

                // Generate transactions for each account with realistic date distribution
                var numTransactions = _faker.Random.Int(50, 150); // More transactions for better analytics
                for (int j = 0; j < numTransactions; j++)
                {
                    var transactionType = _faker.Random.ArrayElement(_transactionTypes);
                    var amount = _faker.Random.Decimal(5, 1000);

                    // Generate more realistic date distribution
                    // 60% in last 30 days, 30% in last 60 days, 10% in last 90 days
                    DateTime transactionDate;
                    var dateRandom = _faker.Random.Double();
                    if (dateRandom < 0.6)
                        transactionDate = _faker.Date.Recent(30);
                    else if (dateRandom < 0.9)
                        transactionDate = _faker.Date.Recent(60);
                    else
                        transactionDate = _faker.Date.Recent(90);

                    // Adjust category and amount based on type
                    string category = transactionType == "credit"
                        ? _faker.Random.ArrayElement(new[] { "salary", "refund", "transfer", "other" })
                        : _faker.Random.ArrayElement(_transactionCategories);

                    var transaction = new Transaction
                    {
                        Id = _transactions.Count + 1,
                        AccountId = account.Id,
                        UserId = userId,
                        Type = transactionType,
                        Amount = amount,
                        Currency = "USD",
                        Description = _faker.Commerce.ProductName(),
                        Category = category,
                        Merchant = _faker.Random.ArrayElement(_merchants),
                        Date = transactionDate,
                        Status = _faker.Random.ArrayElement(new[] { "completed", "pending", "completed", "completed" }),
                        BalanceAfter = account.Balance + (transactionType == "credit" ? amount : -amount),
                        Reference = _faker.Random.AlphaNumeric(10).ToUpper(),
                        Metadata = new Dictionary<string, object>
                        {
                            ["location"] = _faker.Address.City(),
                            ["device"] = _faker.Random.ArrayElement(new[] { "mobile", "web", "atm" })
                        }
                    };
                    _transactions.Add(transaction);

                    // Create expense for debit transactions
                    if (transactionType == "debit" && _faker.Random.Bool(0.8f))
                    {
                        // Map transaction category to expense category more intelligently
                        string expenseCategory = transaction.Category switch
                        {
                            "food" => _faker.Random.ArrayElement(new[] { "groceries", "dining" }),
                            "transport" => "transport",
                            "shopping" => "shopping",
                            "entertainment" => "entertainment",
                            "bills" => _faker.Random.ArrayElement(new[] { "utilities", "rent", "subscriptions" }),
                            "healthcare" => "healthcare",
                            "education" => "education",
                            _ => "other"
                        };

                        var expense = new Expense
                        {
                            Id = _expenses.Count + 1,
                            UserId = userId,
                            TransactionId = transaction.Id,
                            Category = expenseCategory,
                            Amount = amount,
                            Currency = "USD",
                            Description = transaction.Description,
                            Date = transactionDate,
                            Vendor = transaction.Merchant,
                            IsRecurring = _faker.Random.Bool(0.2f),
                            Tags = _faker.Random.WordsArray(0, 3),
                            Notes = _faker.Random.Bool(0.3f) ? _faker.Lorem.Sentence() : ""
                        };
                        _expenses.Add(expense);
                    }
                }
            }

            // Generate budgets for each user
            var numBudgets = _faker.Random.Int(3, 7);
            for (int i = 0; i < numBudgets; i++)
            {
                var budget = new Budget
                {
                    Id = _budgets.Count + 1,
                    UserId = userId,
                    Name = $"{_faker.Random.ArrayElement(_expenseCategories)} Budget",
                    Category = _faker.Random.ArrayElement(_expenseCategories),
                    Limit = _faker.Random.Decimal(100, 2000),
                    Spent = _faker.Random.Decimal(50, 1500),
                    Period = _faker.Random.ArrayElement(_budgetPeriods),
                    StartDate = _faker.Date.Recent(30),
                    EndDate = _faker.Date.Future(1),
                    IsActive = _faker.Random.Bool(0.8f)
                };
                _budgets.Add(budget);
            }
        }
    }

    public List<Account> GetAccounts() => _accounts;
    public List<Transaction> GetTransactions() => _transactions;
    public List<Expense> GetExpenses() => _expenses;
    public List<Budget> GetBudgets() => _budgets;

    public Account? GetAccountById(int id) => _accounts.FirstOrDefault(a => a.Id == id);
    public Transaction? GetTransactionById(int id) => _transactions.FirstOrDefault(t => t.Id == id);
    public Expense? GetExpenseById(int id) => _expenses.FirstOrDefault(e => e.Id == id);
    public Budget? GetBudgetById(int id) => _budgets.FirstOrDefault(b => b.Id == id);

    public List<Account> GetAccountsByUserId(int userId) => _accounts.Where(a => a.UserId == userId).ToList();
    public List<Transaction> GetTransactionsByUserId(int userId) => _transactions.Where(t => t.UserId == userId).ToList();
    public List<Expense> GetExpensesByUserId(int userId) => _expenses.Where(e => e.UserId == userId).ToList();
    public List<Budget> GetBudgetsByUserId(int userId) => _budgets.Where(b => b.UserId == userId).ToList();
}