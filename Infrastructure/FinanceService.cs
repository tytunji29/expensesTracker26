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
    Task UpdateExpense(UpdateById<ExpenseRequest> expense);
    Task<ReturnObject> GetPaidBillsForTheMonth(int monthId, int yearId);
    Task<ReturnObject> GetUnPaidBillsForTheMonth(int monthId, int yearId);
    Task AddIncomeSource(IncomeSourceRequest income);
    Task UpdateIncomeSource(UpdateById<IncomeSourceRequest> income);
    Task AddExpense(ExpenseRequest expense);
    Task AddExpense(List<ExpenseRequest> expenses);
    Task AddBillsHolder(BillsHolderRequest billsHolder);
    Task<ReturnObject> GetIncomeSourcesAsync();
    Task<ReturnObject> GetExpensesAsync();
    Task LinkIncomeToExpense(BillsHolderRequest billsHolder);
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
    public async Task UpdateExpense(UpdateById<ExpenseRequest> expense)
    {
        await _db.Expenses
    .Where(e => e.CreatedBy == UserId && e.Id == expense.Id)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(e => e.Name, expense.Data.Name)
        .SetProperty(e => e.Amount, expense.Data.Amount)
        .SetProperty(e => e.UpdatedAt, DateTime.UtcNow)
    );
    }
    public async Task AddExpense(List<ExpenseRequest> expenses)
    {
        var expenseEntities = expenses
       .Select(e => new Expense
       {
           Name = e.Name,
           Amount = e.Amount,
           CreatedBy = UserId
       })
       .ToList();

        await _db.Expenses.AddRangeAsync(expenseEntities);
        await _db.SaveChangesAsync();
    }
    public async Task AddExpense(ExpenseRequest expense)
    {
        _db.Expenses.Add(new Expense
        {
            Name = expense.Name,
            Amount = expense.Amount,
            CreatedBy = UserId
        });
        await _db.SaveChangesAsync();
    }
    public async Task AddBillsHolder(BillsHolderRequest billsHolder)
    {
        _db.BillsHolders.Add(new BillsHolder
        {
            IncomeSourceId = billsHolder.IncomeSourceId,
            ExpenseId = billsHolder.ExpenseId,
            MonthId = billsHolder.MonthId,
            YearId = billsHolder.Year,
            IsPaid = billsHolder.IsPaid,
            CreatedBy = UserId
        });
        await _db.SaveChangesAsync();
    }
    public async Task EditBillsHolder(BillsHolderRequest b, int id)
    {
        await _db.BillsHolders
            .Where(o => o.Id == id && o.CreatedBy == UserId)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(o => o.IncomeSourceId, b.IncomeSourceId)
                .SetProperty(o => o.ExpenseId, b.ExpenseId)
                .SetProperty(o => o.MonthId, b.MonthId)
                .SetProperty(o => o.YearId, b.Year)
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

    public async Task<ReturnObject> GetExpensesAsync()
    {
        var result = new ReturnObject();
        var expenses = await GetExpenseQuery(GetUserWithAdmin).ToListAsync();
        result.Status = true;
        result.Data = expenses;

        return result;
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


    public async Task LinkIncomeToExpense(BillsHolderRequest billsHolder)
    {
        var link = new BillsHolder
        {
            IncomeSourceId = billsHolder.IncomeSourceId,
            ExpenseId = billsHolder.ExpenseId,
            MonthId = billsHolder.MonthId,
            YearId = billsHolder.Year,
            IsPaid = billsHolder.IsPaid,
            CreatedBy = UserId
        };

        _db.BillsHolders.Add(link);
        await _db.SaveChangesAsync();
    }
    private IQueryable<IncomeSource> GetIncomeQuery(IReadOnlyCollection<int> userId)
    {
        return _db.IncomeSources.Where(i => userId.Contains(i.CreatedBy)).AsQueryable();
    }
    private IQueryable<Expense> GetExpenseQuery(IReadOnlyCollection<int> userId)
    {
        return _db.Expenses.Where(e => userId.Contains(e.CreatedBy)).AsQueryable();
    }
    private IQueryable<BillResponse> GetBillsQuery(IReadOnlyCollection<int> userId)
    {
        return _db.BillsHolders
            .Where(o => userId.Contains(o.CreatedBy))
            .Include(b => b.IncomeSource)
            .Include(b => b.Expense)
             .AsEnumerable()
            .Select(b => new BillResponse
            {
                Id = b.Id,
                MonthId = b.MonthId,
                IncomeSourceName = b.IncomeSource.Name,
                ExpenseName = b.Expense.Name,
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
