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
    Task<ReturnObject> GetUnPaidBillsForTheMonth(int monthId, int yearId);
    Task AddIncomeSource(IncomeSourceRequest income);
    Task UpdateIncomeSource(UpdateById<IncomeSourceRequest> income);

    Task AddBillsHolder(BillsHolderRequest billsHolder);
    Task<ReturnObject> GetIncomeSourcesAsync();
    Task AddBillsHolder(List<BillsHolderRequest> billsHolders);
    Task AddIncomeSourceForTheMonth(IncomeSourceForTheMonthRequest income);
    Task<ReturnObject> GetTotalBalanceAsync();
    //Task LinkIncomeToExpense(BillsHolderRequest billsHolder);
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

    private int UserId => _httpContextAccessor.HttpContext?.User?.GetUserId()
     ?? throw new InvalidOperationException("No HttpContext available");

    public IReadOnlyCollection<int> GetUserWithAdmin =>
          new[] { 0, UserId };

    public async Task AddIncomeSource(IncomeSourceRequest income)
    {
        _db.IncomeSources.Add(new IncomeSource
        {
            Name = income.Name,
            Amount = income.Amount,
            CreatedBy = UserId

        });
        await _db.SaveChangesAsync();
    }
    public async Task AddIncomeSourceForTheMonth(IncomeSourceForTheMonthRequest income)
    {
        var incomeSource = await _db.IncomeSources
            .FirstOrDefaultAsync(i => i.Id == income.Id && i.CreatedBy == UserId && i.IsDeleted == false);
        if (incomeSource == null)
            throw new Exception("Income source not found");
        if (incomeSource.Amount <= 0)
            throw new Exception("Income source amount must be greater than zero");

        _db.IncomeSourcesForTheMonth.Add(new IncomeSourceForTheMonth
        {
            IncomeSourceId = income.Id,
            Year = DateTime.Now.Year,
            Month = DateTime.Now.Month,
            CreatedBy = UserId
        });
        await _db.SaveChangesAsync();
    }
    public async Task UpdateIncomeSource(UpdateById<IncomeSourceRequest> income)
    {
        await _db.IncomeSources
    .Where(e => e.CreatedBy == UserId && e.Id == income.Id)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(e => e.Name, income.Data.Name)
        .SetProperty(e => e.Amount, income.Data.Amount)
        .SetProperty(e => e.UpdatedAt, DateTime.UtcNow)
    );
    }


    public async Task AddBillsHolder(BillsHolderRequest billsHolder)
    {
        int currentMonth = DateTime.UtcNow.Month;
        int currentYear = DateTime.UtcNow.Year;

        // 1. Get all income sources for the month
        var incomeSourcesForTheMonth = await _db.IncomeSourcesForTheMonth
            .Where(i =>
                i.Month == currentMonth &&
                i.Year == currentYear &&
                i.CreatedBy == UserId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();

        if (!incomeSourcesForTheMonth.Any())
            throw new Exception("No income sources found for the current month. Please add an income source.");

        // 2. Get total expenses already allocated per income source
        var expensesByIncomeSource = await _db.BillsHolders
            .Where(b =>
                b.MonthId == currentMonth &&
                b.YearId == currentYear &&
                b.CreatedBy == UserId)
            .GroupBy(b => b.IncomeSourceId)
            .Select(g => new
            {
                IncomeSourceId = g.Key,
                TotalExpense = g.Sum(x => x.ExpenseAmount)
            })
            .ToListAsync();

        // 3. Try to find an income source that can cover this bill
        IncomeSourceForTheMonth? selectedIncomeSource = null;

        foreach (var incomeSource in incomeSourcesForTheMonth)
        {
            var alreadySpent = expensesByIncomeSource
                .FirstOrDefault(x => x.IncomeSourceId == incomeSource.Id)?
                .TotalExpense ?? 0;

            var remainingAmount = incomeSource.IncomeSource.Amount - alreadySpent;

            if (remainingAmount >= billsHolder.ExpenseAmount)
            {
                selectedIncomeSource = incomeSource;
                break;
            }
        }

        if (selectedIncomeSource == null)
            throw new Exception("No income source has sufficient balance to cover this bill.");

        // 4. Add bill holder
        _db.BillsHolders.Add(new BillsHolder
        {
            ExpenseAmount = billsHolder.ExpenseAmount,
            ExpenseName = billsHolder.ExpenseName,
            IncomeSourceId = selectedIncomeSource.Id,
            MonthId = currentMonth,
            YearId = currentYear,
            IsPaid = billsHolder.IsPaid,
            CreatedBy = UserId
        });

        await _db.SaveChangesAsync();
    }

    public async Task AddBillsHolder(List<BillsHolderRequest> billsHolders)
    {
        if (billsHolders == null || !billsHolders.Any())
            throw new Exception("Bills list cannot be empty.");

        int currentMonth = DateTime.UtcNow.Month;
        int currentYear = DateTime.UtcNow.Year;

        using var transaction = await _db.Database.BeginTransactionAsync();

        // 1. Get all income sources for the month
        var incomeSources = await _db.IncomeSourcesForTheMonth
    .Include(i => i.IncomeSource)
            .Where(i =>
                i.Month == currentMonth &&
                i.Year == currentYear &&
                i.CreatedBy == UserId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();

        if (!incomeSources.Any())
            throw new Exception("No income sources found for the current month. Please add an income source.");

        // 2. Get already allocated expenses per income source
        var expensesByIncomeSource = await _db.BillsHolders
            .Where(b =>
                b.MonthId == currentMonth &&
                b.YearId == currentYear &&
                b.CreatedBy == UserId)
            .GroupBy(b => b.IncomeSourceId)
            .Select(g => new
            {
                IncomeSourceId = g.Key,
                TotalExpense = g.Sum(x => x.ExpenseAmount)
            })
            .ToListAsync();

        // 3. Build remaining balance tracker
        var remainingBalances = incomeSources.ToDictionary(
            i => i.Id,
            i =>
            {
                var spent = expensesByIncomeSource
                    .FirstOrDefault(x => x.IncomeSourceId == i.Id)?
                    .TotalExpense ?? 0;

                return i.IncomeSource.Amount - spent;
            });

        var billsToInsert = new List<BillsHolder>();

        // 4. Allocate each bill
        foreach (var bill in billsHolders)
        {
            IncomeSourceForTheMonth? selectedIncomeSource = null;

            foreach (var incomeSource in incomeSources)
            {
                var remaining = remainingBalances[incomeSource.Id];

                if (remaining >= bill.ExpenseAmount)
                {
                    selectedIncomeSource = incomeSource;
                    remainingBalances[incomeSource.Id] -= bill.ExpenseAmount;
                    break;
                }
            }

            if (selectedIncomeSource == null)
                throw new Exception(
                    $"Insufficient income to cover expense '{bill.ExpenseName}' ({bill.ExpenseAmount}).");

            billsToInsert.Add(new BillsHolder
            {
                ExpenseName = bill.ExpenseName,
                ExpenseAmount = bill.ExpenseAmount,
                IncomeSourceId = selectedIncomeSource.Id,
                MonthId = currentMonth,
                YearId = currentYear,
                IsPaid = bill.IsPaid,
                CreatedBy = UserId
            });
        }

        // 5. Save once
        _db.BillsHolders.AddRange(billsToInsert);
        await _db.SaveChangesAsync();

        await transaction.CommitAsync();
    }



    public async Task EditBillsHolder(BillsHolderRequest b, int id)
    {
        await _db.BillsHolders
            .Where(o => o.Id == id && o.CreatedBy == UserId)
            .ExecuteUpdateAsync(setters => setters
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
        await _db.BillsHolders
            .Where(o => o.Id == id && o.CreatedBy == UserId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.IsPaid, isPaid)
                .SetProperty(o => o.UpdatedAt, DateTime.UtcNow)
            );
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
        var incomeSources = await _db.IncomeSourcesForTheMonth
    .Include(i => i.IncomeSource)
            .Where(i =>
                i.Month == currentMonth &&
                i.Year == currentYear &&
                i.CreatedBy == UserId)
            .OrderBy(i => i.CreatedAt)
            .ToListAsync();
        var expensesByIncomeSource = await _db.BillsHolders
        .Where(b =>
            b.MonthId == currentMonth &&
            b.YearId == currentYear &&
            b.CreatedBy == UserId)
        .GroupBy(b => b.IncomeSourceId)
        .Select(g => new
        {
            IncomeSourceId = g.Key,
            TotalExpense = g.Sum(x => x.ExpenseAmount)
        })
        .ToListAsync();

        // 3. Build remaining balance tracker
        var remainingBalances = incomeSources.ToDictionary(
            i => i.Id,
            i =>
            {
                var spent = expensesByIncomeSource
                    .FirstOrDefault(x => x.IncomeSourceId == i.Id)?
                    .TotalExpense ?? 0;

                return i.IncomeSource.Amount - spent;
            });
        return new ReturnObject
        {
            Status = true,
            Data = remainingBalances.Values.Sum()
        };

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
        var bills = billsFilter.ToList();
        result.Status = true;
        result.Data = bills;

        return result;
    }
    public async Task<ReturnObject> GetUnpaidBills()
    {
        var result = new ReturnObject();
        var billFilter = GetBillsQuery(GetUserWithAdmin)
            .Where(b => !b.Paid).AsQueryable();
        var bills = billFilter.ToList();
        result.Status = true;
        result.Data = bills;

        return result;
    }

    private IQueryable<IncomeSource> GetIncomeQuery(IReadOnlyCollection<int> userId)
    {
        return _db.IncomeSources.Where(i => userId.Contains(i.CreatedBy)).AsQueryable();
    }
    private IQueryable<BillResponse> GetBillsQuery(IReadOnlyCollection<int> userId)
    {
        return _db.BillsHolders
            .Where(o => userId.Contains(o.CreatedBy))
            .Include(b => b.IncomeSource)
             // .Include(b => b.Expense)
             .AsEnumerable()
            .Select(b => new BillResponse
            {
                Id = b.Id,
                MonthId = b.MonthId,
                ExpenseAmount = b.ExpenseAmount,
                IncomeSourceName = b.IncomeSource.Name,
                ExpenseName = b.ExpenseName,
                Month = GetMonthName(b.MonthId),
                Year = b.YearId,
                Paid = b.IsPaid
            })
            .AsQueryable();
    }
    private string GetMonthName(int monthId)
    {
        if (monthId < 1 || monthId > 12)
            return "Unknown";

        return CultureInfo.CurrentCulture.DateTimeFormat.GetMonthName(monthId);
    }


}
