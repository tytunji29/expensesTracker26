namespace expensesTracker26.Application.Requests;

public class UpdateById<TRequest>
{
    public int Id { get; set; }
    public  TRequest Data { get; set; }
}


// public interface IUpdateById
// {
//     int Id { get; set; }
// }
// public class UpdateIncomeSourceRequest : IncomeSourceRequest, IUpdateById
// {
//     public int Id { get; set; }
// }
public class IncomeSourceForTheMonthRequest
{
    public int Id { get; set; }
}
public class IncomeSourceRequest
{
    public  string Name { get; set; }
    public decimal Amount { get; set; }
}
public class ExpenseRequest
{
    public  string Name { get; set; }
    public decimal Amount { get; set; }
}

public class BillsHolderRequest
{
    public  decimal ExpenseAmount { get; set; }
    public  string ExpenseName { get; set; }
    public  bool IsPaid { get; set; }
}

public class InvestmentHolderRequest
{
    public  decimal Amount { get; set; }
    public  DateTime StartDate { get; set; }
    public DateTime? EndDate { get; set; }
}
public class AppUserRequest
{
    public  string Email { get; set; }
    public  string Password { get; set; }
}