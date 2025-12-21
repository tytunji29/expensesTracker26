using expensesTracker26.Grpc;
using expensesTracker26.Application.Requests;
using expensesTracker26.Domain;
using Grpc.Core;
using Microsoft.EntityFrameworkCore;

public class FinanceGrpcService : FinanceServiceGrpc.FinanceServiceGrpcBase
{
    private readonly IFinanceService _financeService;

    public FinanceGrpcService(IFinanceService financeService)
    {
        _financeService = financeService;
    }

    // ===================== Income =====================
    public override async Task<IncomeList> GetIncomeSources(
        Empty request,
        ServerCallContext context)
    {
        var result = await _financeService.GetIncomeSourcesAsync();

        var response = new IncomeList();
        if (result.Status && result.Data is List<IncomeSource> incomes)
        {
            response.Items.AddRange(incomes.Select(i => new Income
            {
                Id = i.Id,
                Name = i.Name,
                Amount = (double)i.Amount
            }));
        }

        return response;
    }

    public override async Task<Income> AddIncome(
        IncomeRequest request,
        ServerCallContext context)
    {
        await _financeService.AddIncomeSource(new IncomeSourceRequest
        {
            Name = request.Name,
            Amount = (decimal)request.Amount
        });

        // Return the created object (optionally with Id)
        return new Income
        {
            Name = request.Name,
            Amount = request.Amount
        };
    }

    // ===================== Expenses =====================
    public override async Task<ExpenseList> GetExpenses(
        Empty request,
        ServerCallContext context)
    {
        var result = await _financeService.GetExpensesAsync();

        var response = new ExpenseList();
        if (result.Status && result.Data is List<expensesTracker26.Domain.Expense> expenses)
        {
            response.Items.AddRange(expenses.Select(e => new expensesTracker26.Grpc.Expense
            {
                Id = e.Id,
                Name = e.Name,
                Amount = (double)e.Amount
            }));
        }

        return response;
    }

    public override async Task<expensesTracker26.Grpc.Expense> AddExpense(
        expensesTracker26.Grpc.ExpenseRequest request,
        ServerCallContext context)
    {
        await _financeService.AddExpense(new expensesTracker26.Application.Requests.ExpenseRequest
        {
            Name = request.Name,
            Amount = (decimal)request.Amount
        });

        return new expensesTracker26.Grpc.Expense
        {
            Name = request.Name,
            Amount = request.Amount
        };
    }

    // ===================== Bills =====================
    public override async Task<Empty> LinkIncomeToExpense(
        expensesTracker26.Grpc.BillsHolderRequest request,
        ServerCallContext context)
    {
        await _financeService.LinkIncomeToExpense(new expensesTracker26.Application.Requests.BillsHolderRequest
        {
            IncomeSourceId = request.IncomeSourceId,
            ExpenseId = request.ExpenseId,
            MonthId = request.MonthId,
            Year = request.YearId,
            IsPaid = request.IsPaid
        });
        return new Empty();
    }

    public override async Task<BillsHolderList> GetBillsHolders(
        Empty request,
        ServerCallContext context)
    {
        var result = await _financeService.GetPaidBillsForTheMonth(0, 0); // Fetch all if month/year 0

        var response = new BillsHolderList();
        if (result.Status && result.Data is List<expensesTracker26.Domain.BillsHolder> bills)
        {
            response.Items.AddRange(bills.Select(b => new expensesTracker26.Grpc.BillsHolder
            {
                Id = b.Id,
                IncomeSourceId = b.IncomeSourceId,
                ExpenseId = b.ExpenseId,
                MonthId = b.MonthId,
                YearId = b.YearId,
                IsPaid = b.IsPaid
            }));
        }

        return response;
    }

    public override async Task<BillsHolderList> GetBillsHoldersByMonthId(
        MonthRequest request,
        ServerCallContext context)
    {
        var result = await _financeService.GetPaidBillsForTheMonth(request.MonthId, 0);

        var response = new BillsHolderList();
        if (result.Status && result.Data is List<expensesTracker26.Domain.BillsHolder> bills)
        {
            response.Items.AddRange(bills.Select(b => new expensesTracker26.Grpc.BillsHolder
            {
                Id = b.Id,
                IncomeSourceId = b.IncomeSourceId,
                ExpenseId = b.ExpenseId,
                MonthId = b.MonthId,
                YearId = b.YearId,
                IsPaid = b.IsPaid
            }));
        }

        return response;
    }

    public override async Task<BillsHolderList> GetBillsHoldersByMonthAndYear(
        MonthYearRequest request,
        ServerCallContext context)
    {
        var result = await _financeService.GetPaidBillsForTheMonth(request.MonthId, request.YearId);

        var response = new BillsHolderList();
        if (result.Status && result.Data is List<expensesTracker26.Domain.BillsHolder> bills)
        {
            response.Items.AddRange(bills.Select(b => new expensesTracker26.Grpc.BillsHolder
            {
                Id = b.Id,
                IncomeSourceId = b.IncomeSourceId,
                ExpenseId = b.ExpenseId,
                MonthId = b.MonthId,
                YearId = b.YearId,
                IsPaid = b.IsPaid
            }));
        }

        return response;
    }
}
