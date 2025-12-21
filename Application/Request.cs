namespace expensesTracker26.Application.Requests;

public class IncomeSourceRequest
{
    public required string Name { get; set; }
    public decimal Amount { get; set; }
}
public class ExpenseRequest
{
    public required string Name { get; set; }
    public decimal Amount { get; set; }
}

public class BillsHolderRequest
{
    public required int IncomeSourceId { get; set; }
    public required int ExpenseId { get; set; }
    public required int MonthId { get; set; }
    public required int Year { get; set; }
    public required bool IsPaid { get; set; }
}