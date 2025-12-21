namespace expensesTracker26.Domain;

public class BillsHolder : BaseEntity
{
    public int Id { get; set; }

    public int IncomeSourceId { get; set; }
    public int ExpenseId { get; set; }

    public int MonthId { get; set; }
    public int YearId { get; set; }
    public bool IsPaid { get; set; }
    public IncomeSource? IncomeSource { get; set; }
    public Expense? Expense { get; set; }
}
