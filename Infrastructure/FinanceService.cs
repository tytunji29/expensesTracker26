using expensesTracker26.Application.Requests;
using expensesTracker26.Domain;
using expensesTracker26.Infrastructure;
using Microsoft.EntityFrameworkCore;

public interface IFinanceService
{
    Task<ReturnObject> GetUnpaidBills();
    Task<ReturnObject> GetPaidBillsForTheMonth(int monthId, int yearId);
    Task<ReturnObject> GetUnPaidBillsForTheMonth(int monthId, int yearId);
    Task AddIncomeSource(IncomeSourceRequest income);
    Task AddExpense(ExpenseRequest expense);
    Task AddBillsHolder(BillsHolderRequest billsHolder);
    Task<ReturnObject> GetIncomeSourcesAsync();
    Task<ReturnObject> GetExpensesAsync();
    Task LinkIncomeToExpense(BillsHolderRequest billsHolder);
}

public class FinanceService : IFinanceService
{
    private readonly AppDbContext _db;

    public FinanceService(AppDbContext db)
    {
        _db = db;
    }
    //do the add methods 
    public async Task AddIncomeSource(IncomeSourceRequest income)
    {
        _db.IncomeSources.Add(new IncomeSource
        {
            Name = income.Name,
            Amount = income.Amount
        });
        await _db.SaveChangesAsync();
    }

    public async Task AddExpense(ExpenseRequest expense)
    {
        _db.Expenses.Add(new Expense
        {
            Name = expense.Name,
            Amount = expense.Amount
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
            IsPaid = billsHolder.IsPaid
        });
        await _db.SaveChangesAsync();
    }
    public async Task<ReturnObject> GetIncomeSourcesAsync()
    {
        var result = new ReturnObject();
        try
        {
            var incomes = await GetIncomeQuery().ToListAsync();
            result.Status = true;
            result.Data = incomes;
        }
        catch (Exception ex)
        {
            result.Status = false;
            result.Message = ex.Message;
        }
        return result;
    }

    public async Task<ReturnObject> GetExpensesAsync()
    {
        var result = new ReturnObject();
        try
        {
            var expenses = await GetExpenseQuery().ToListAsync();
            result.Status = true;
            result.Data = expenses;
        }
        catch (Exception ex)
        {
            result.Status = false;
            result.Message = ex.Message;
        }
        return result;
    }

    public async Task<ReturnObject> GetPaidBillsForTheMonth(int monthId, int yearId)
    {
        var result = new ReturnObject();
        try
        {
            var bills = await GetBillsQuery()
                .Where(b => b.IsPaid && b.MonthId == monthId && b.YearId == yearId)
                .ToListAsync();

            result.Status = true;
            result.Data = bills;
        }
        catch (Exception ex)
        {
            result.Status = false;
            result.Message = ex.Message;
        }
        return result;
    }

    public async Task<ReturnObject> GetUnPaidBillsForTheMonth(int monthId, int yearId)
    {
        var result = new ReturnObject();
        try
        {
            var bills = await GetBillsQuery()
                .Where(b => !b.IsPaid && b.MonthId == monthId && b.YearId == yearId)
                .ToListAsync();

            result.Status = true;
            result.Data = bills;
        }
        catch (Exception ex)
        {
            result.Status = false;
            result.Message = ex.Message;
        }
        return result;
    }
    public async Task<ReturnObject> GetUnpaidBills()
    {
        var result = new ReturnObject();
        try
        {
            var bills = await GetBillsQuery()
                .Where(b => !b.IsPaid)
                .ToListAsync();

            result.Status = true;
            result.Data = bills;
        }
        catch (Exception ex)
        {
            result.Status = false;
            result.Message = ex.Message;
        }
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
            IsPaid = billsHolder.IsPaid
        };

        _db.BillsHolders.Add(link);
        await _db.SaveChangesAsync();
    }
    private IQueryable<IncomeSource> GetIncomeQuery()
    {
        return _db.IncomeSources.AsQueryable();
    }
    private IQueryable<Expense> GetExpenseQuery()
    {
        return _db.Expenses.AsQueryable();
    }
    private IQueryable<BillsHolder> GetBillsQuery()
    {
        return _db.BillsHolders
            .Include(b => b.IncomeSource)
            .Include(b => b.Expense)
            .AsQueryable();
    }

}
