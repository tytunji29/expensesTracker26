namespace expensesTracker26.Application.Requests;

public class UpdateById<TRequest>
{
    public int Id { get; set; }
    public required TRequest Data { get; set; }
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
    public required decimal ExpenseAmount { get; set; }
    public required string ExpenseName { get; set; }
    public required bool IsPaid { get; set; }
}

public class AppUserRequest
{
    public required string Email { get; set; }
    public required string Password { get; set; }
}