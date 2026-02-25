using System.Globalization;
using expensesTracker26.Application.Requests;
using expensesTracker26.Domain;
using expensesTracker26.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

public interface IFinanceService
{
    Task EditBillsHolder(BillsHolderRequest b, int id);
    Task FlagIsPaid(int id, bool isPaid);
    Task<ReturnObject> GetUnpaidBills();
    Task<ReturnObject> GetPaidBillsForTheMonth(int monthId, int yearId);
    Task<ReturnObject> GetAllBillsForTheMonth();
    Task<ReturnObject> GetUnPaidBillsForTheMonth(int monthId, int yearId);
    Task AddIncomeSource(IncomeSourceRequest income);
    Task UpdateIncomeSource(UpdateById<IncomeSourceRequest> income);
    Task<ReturnObject> GetIncomeSourcesAsync();
    Task<ReturnObject> GetIncomeSourcesBalanceAsync();
    Task<ReturnObject> AddBillsHolder(int sourceid, List<BillsHolderRequest> billsHolders);
    Task AddIncomeSourceForTheMonth(IncomeSourceForTheMonthRequest income);
    Task<ReturnObject> GetTotalBalanceAsync();
    Task FlagIsPaid(List<int> ids);
    Task<ReturnObject> GetInvestmentHolders();
    Task<ReturnObject> GetTotalInvestment(InvestmentHolderRequest investmentHolderRequest);
}

public class FinanceService : IFinanceService
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public FinanceService(AppDbContext db, IHttpContextAccessor httpContextAccessor)
    {
        _db = db;
        _httpContextAccessor = httpContextAccessor;
    }

    private int UserId =>
        _httpContextAccessor.HttpContext?.User?.GetUserId()
        ?? throw new InvalidOperationException("No HttpContext available");

    public IReadOnlyCollection<int> GetUserWithAdmin => new[] { 0, UserId };

    public async Task AddIncomeSource(IncomeSourceRequest income)
    {
        _db.IncomeSources.Add(
            new IncomeSource
            {
                Name = income.Name,
                Amount = income.Amount,
                CreatedBy = UserId,
            }
        );
        await _db.SaveChangesAsync();
    }

    public async Task AddIncomeSourceForTheMonth(IncomeSourceForTheMonthRequest income)
    {
        var incomeSource = await _db.IncomeSources.FirstOrDefaultAsync(i =>
            i.Id == income.Id && i.CreatedBy == UserId && i.IsDeleted == false
        );
        if (incomeSource == null)
            throw new Exception("Income source not found");
        if (incomeSource.Amount <= 0)
            throw new Exception("Income source amount must be greater than zero");

        _db.IncomeSourcesForTheMonth.Add(
            new IncomeSourceForTheMonth
            {
                IncomeSourceId = income.Id,
                Year = DateTime.Now.Year,
                Month = DateTime.Now.Month,
                CreatedBy = UserId,
            }
        );
        await _db.SaveChangesAsync();
    }

    public async Task UpdateIncomeSource(UpdateById<IncomeSourceRequest> income)
    {
        await _db
            .IncomeSources.Where(e => e.CreatedBy == UserId && e.Id == income.Id)
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(e => e.Name, income.Data.Name)
                    .SetProperty(e => e.Amount, income.Data.Amount)
                    .SetProperty(e => e.UpdatedAt, DateTime.UtcNow)
            );
    }

    public async Task<ReturnObject> AddBillsHolder(
        int sourceId,
        List<BillsHolderRequest> billsHolders
    )
    {
        if (billsHolders == null || !billsHolders.Any())
            throw new Exception("Bills list cannot be empty.");

        int month = DateTime.UtcNow.Month;
        int year = DateTime.UtcNow.Year;

        var incomeSources = await _db
            .IncomeSourcesForTheMonth.Include(i => i.IncomeSource)
            .Where(i => i.Month == month && i.Year == year && i.CreatedBy == UserId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();

        if (!incomeSources.Any())
            throw new Exception(
                "No income sources found for the current month. Please add an income source."
            );

        var expensesBySource = await _db
            .BillsHolders.Where(b =>
                b.MonthId == month && b.YearId == year && b.CreatedBy == UserId
            )
            .GroupBy(b => b.IncomeSourceId)
            .Select(g => new { IncomeSourceId = g.Key, TotalExpense = g.Sum(x => x.ExpenseAmount) })
            .ToDictionaryAsync(x => x.IncomeSourceId, x => x.TotalExpense);

        var remainingBalances = incomeSources.ToDictionary(
            i => i.IncomeSourceId,
            i =>
                i.IncomeSource.Amount
                - (expensesBySource.ContainsKey(i.Id) ? expensesBySource[i.Id] : 0)
        );

        var billsToInsert = new List<BillsHolder>();

        foreach (var bill in billsHolders)
        {
            if (sourceId != 0)
            {
                // Use specified source
                if (remainingBalances[sourceId] < bill.ExpenseAmount)
                    throw new Exception($"Insufficient balance for '{bill.ExpenseName}'.");

                remainingBalances[sourceId] -= bill.ExpenseAmount;
                billsToInsert.Add(CreateBill(bill, sourceId, month, year));
            }
            else
            {
                // Auto-allocate
                var allocated = false;
                foreach (var income in incomeSources)
                {
                    if (remainingBalances[income.Id] >= bill.ExpenseAmount)
                    {
                        remainingBalances[income.Id] -= bill.ExpenseAmount;
                        billsToInsert.Add(CreateBill(bill, income.Id, month, year));
                        allocated = true;
                        break;
                    }
                }

                if (!allocated)
                    throw new Exception($"Insufficient income to cover '{bill.ExpenseName}'.");
            }
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.BillsHolders.AddRange(billsToInsert);
        await _db.SaveChangesAsync();
        await transaction.CommitAsync();

        var totals = await _db
            .Users.Where(u => u.Id == UserId)
            .Select(u => new
            {
                TotalExpenses = _db.BillsHolders.Where(b =>
                        b.CreatedBy == UserId && b.MonthId == month && b.YearId == year
                    )
                    .Sum(b => (decimal?)b.ExpenseAmount)
                    ?? 0,
                TotalIncomes = _db.IncomeSourcesForTheMonth.Where(i =>
                        i.CreatedBy == UserId && i.Month == month && i.Year == year
                    )
                    .Sum(i => (decimal?)i.IncomeSource.Amount)
                    ?? 0,
            })
            .AsNoTracking()
            .FirstAsync();

        return new ReturnObject
        {
            Status = true,
            Message = "Bills added successfully",
            Data = new
            {
                totals.TotalExpenses,
                totals.TotalIncomes,
                TotalBalance = totals.TotalIncomes - totals.TotalExpenses,
            },
        };
    }

    // Helper method for readability
    private BillsHolder CreateBill(
        BillsHolderRequest request,
        int incomeSourceId,
        int month,
        int year
    )
    {
        return new BillsHolder
        {
            ExpenseName = request.ExpenseName,
            ExpenseAmount = request.ExpenseAmount,
            IncomeSourceId = incomeSourceId,
            MonthId = month,
            YearId = year,
            IsPaid = request.IsPaid,
            CreatedBy = UserId,
        };
    }

    public async Task EditBillsHolder(BillsHolderRequest b, int id)
    {
        await _db
            .BillsHolders.Where(o => o.Id == id && o.CreatedBy == UserId)
            .ExecuteUpdateAsync(setters =>
                setters
                    // .SetProperty(o => o.IncomeSourceId, b.IncomeSourceId)
                    //.SetProperty(o => o.ExpenseId, b.ExpenseId)
                    //.SetProperty(o => o.MonthId, b.MonthId)
                    //.SetProperty(o => o.YearId, b.Year)
                    .SetProperty(o => o.IsPaid, b.IsPaid)
                    .SetProperty(o => o.UpdatedAt, DateTime.UtcNow)
            );
    }

    public async Task FlagIsPaid(int id, bool isPaid)
    {
        await _db
            .BillsHolders.Where(o => o.Id == id && o.CreatedBy == UserId)
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(o => o.IsPaid, isPaid)
                    .SetProperty(o => o.UpdatedAt, DateTime.UtcNow)
            );
    }

    public async Task FlagIsPaid(List<int> ids)
    {
        await _db
            .BillsHolders.Where(o => ids.Contains(o.Id) && o.CreatedBy == UserId)
            .ExecuteUpdateAsync(setters =>
                setters
                    .SetProperty(o => o.IsPaid, true)
                    .SetProperty(o => o.UpdatedAt, DateTime.UtcNow)
            );
    }

    public async Task<ReturnObject> GetTotalInvestment(
        InvestmentHolderRequest investmentHolderRequest
    )
    {
        DateTime lastDayOfYear = new DateTime(
            DateTime.UtcNow.Year,
            12,
            31,
            23,
            59,
            59,
            DateTimeKind.Utc
        );
        var result = new ReturnObject();
        var billFilter = CalculateTotalInvestments(
            investmentHolderRequest.StartDate,
            investmentHolderRequest.EndDate ?? lastDayOfYear,
            investmentHolderRequest.Amount
        );
        var endDate = investmentHolderRequest.EndDate ?? lastDayOfYear;

        _db.InvestmentHolders.Add(
            new InvestmentHolder
            {
                PrincipalAmount = investmentHolderRequest.Amount,
                TotalAmountInvested = billFilter.Item1,
                StartMonth = investmentHolderRequest.StartDate.ToUniversalTime(),
                EndMonth = endDate.ToUniversalTime(),
                Year = DateTime.UtcNow.Year,
                CreatedBy = 0,
            }
        );
        await _db.SaveChangesAsync();

        result.Status = true;
        result.Data = new { TotalInvestment = billFilter.Item1, TotalRemaining = billFilter.Item2 };

        return result;
    }

    public async Task<ReturnObject> GetInvestmentHolders()
    {
        var result = new ReturnObject();
        var investments = await GetInvestmentQuery(GetUserWithAdmin).ToListAsync();
        result.Status = true;
        result.Data = investments;
        return result;
    }
    public async Task<ReturnObject> GetIncomeSourcesAsync()
    {
        var result = new ReturnObject();
        var incomes = await GetIncomeQuery(GetUserWithAdmin).ToListAsync();
        result.Status = true;
        result.Data = incomes;
        return result;
    }

    public async Task<ReturnObject> GetTotalBalanceAsync()
    {
        int currentMonth = DateTime.UtcNow.Month;
        int currentYear = DateTime.UtcNow.Year;

        // 1. Get all income sources for the month
        var incomeSources = await _db
            .IncomeSourcesForTheMonth.Include(i => i.IncomeSource)
            .Where(i => i.Month == currentMonth && i.Year == currentYear && i.CreatedBy == UserId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();
        var expensesByIncomeSource = await _db
            .BillsHolders.Where(b =>
                b.MonthId == currentMonth && b.YearId == currentYear && b.CreatedBy == UserId
            )
            .GroupBy(b => b.IncomeSourceId)
            .Select(g => new { IncomeSourceId = g.Key, TotalExpense = g.Sum(x => x.ExpenseAmount) })
            .ToListAsync();

        // 3. Build remaining balance tracker
        var remainingBalances = incomeSources.ToDictionary(
            i => i.Id,
            i =>
            {
                var spent =
                    expensesByIncomeSource
                        .FirstOrDefault(x => x.IncomeSourceId == i.Id)
                        ?.TotalExpense
                    ?? 0;

                return i.IncomeSource.Amount - spent;
            }
        );
        return new ReturnObject { Status = true, Data = remainingBalances.Values.Sum() };
    }

    public async Task<ReturnObject> GetPaidBillsForTheMonth(int monthId, int yearId)
    {
        var result = new ReturnObject();

        var billfilter = GetBillsQuery(GetUserWithAdmin)
            .Where(b => b.Paid && b.MonthId == monthId && b.Year == yearId)
            .AsQueryable();
        var bills = billfilter.ToList();
        result.Status = true;
        result.Data = bills;

        return result;
    }

    public async Task<ReturnObject> GetUnPaidBillsForTheMonth(int monthId, int yearId)
    {
        var result = new ReturnObject();

        var billsFilter = GetBillsQuery(GetUserWithAdmin)
            .Where(b => !b.Paid && b.MonthId == monthId && b.Year == yearId)
            .AsQueryable();
        var bills = billsFilter.OrderBy(o => o.PaymentDate).ToList();
        result.Status = true;
        result.Data = bills;

        return result;
    }

    public async Task<ReturnObject> GetAllBillsForTheMonth()
    {
        var currentMonth = DateTime.UtcNow.Month;
        var currentYear = DateTime.UtcNow.Year;
        var result = new ReturnObject();

        var billsFilter = GetBillsQuery(GetUserWithAdmin)
            .Where(b => b.MonthId == currentMonth && b.Year == currentYear)
            .AsQueryable();
        var bills = billsFilter.ToList();
        result.Status = true;
        result.Data = bills;

        return result;
    }

    public async Task<ReturnObject> GetUnpaidBills()
    {
        var result = new ReturnObject();
        var billFilter = GetBillsQuery(GetUserWithAdmin).Where(b => !b.Paid).AsQueryable();
        var bills = billFilter.ToList();
        result.Status = true;
        result.Data = bills;

        return result;
    }

    public async Task<ReturnObject> GetIncomeSourcesBalanceAsync()
    {
        var balances = await (
            from i in _db.IncomeSourcesForTheMonth
            join a in _db.IncomeSources on i.IncomeSourceId equals a.Id
            join b in _db.BillsHolders on i.Id equals b.IncomeSourceId into bills
            select new
            {
                i.Id,
                a.Name,
                TotalBills = bills.Sum(x => (decimal?)x.ExpenseAmount) ?? 0,
                Balance = a.Amount - (bills.Sum(x => (decimal?)x.ExpenseAmount) ?? 0),
            }
        ).ToListAsync();
        return new ReturnObject
        {
            Data = balances,
            Message = "Balance Found Successfully",
            Status = true,
        };
    }

    private IQueryable<IncomeSource> GetIncomeQuery(IReadOnlyCollection<int> userId)
    {
        return _db.IncomeSources.Where(i => userId.Contains(i.CreatedBy)).AsQueryable();
    }

    private IQueryable<BillResponse> GetBillsQuery(IReadOnlyCollection<int> userId)
    {
        return _db
            .BillsHolders.Where(o => userId.Contains(o.CreatedBy))
            .Include(b => b.IncomeSource)
            // .Include(b => b.Expense)
            .AsEnumerable()
            .OrderBy(o => o.IncomeSource.PaymentDate)
            .Select(b => new BillResponse
            {
                Id = b.Id,
                MonthId = b.MonthId,
                ExpenseAmount = b.ExpenseAmount,
                IncomeSourceName = b.IncomeSource.Name,
                ExpenseName = b.ExpenseName,
                PaymentDate = b.IncomeSource.PaymentDate,
                Month = GetMonthName(b.MonthId),
                Year = b.YearId,
                Paid = b.IsPaid,
            })
            .AsQueryable();
    }

    private string GetMonthName(int monthId)
    {
        if (monthId < 1 || monthId > 12)
            return "Unknown";

        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthId);
    }

    ///  Investment  methods
    private IQueryable<InvestmentHolder> GetInvestmentQuery(IReadOnlyCollection<int> userId)
    {
        return _db.InvestmentHolders.Where(i => userId.Contains(i.CreatedBy)).AsQueryable();
    }

    private (decimal, decimal) CalculateTotalInvestments(
        DateTime startDate,
        DateTime endDate,
        Decimal amount
    )
    {
        var interest = InterestCalculator.CalculateSpecialInterestWithReinvestment(
            amount,
            startDate,
            endDate
        );
        return interest;
    }
}
