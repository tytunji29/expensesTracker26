
namespace expensesTracker26.Domain;

public class InvestmentHolder : BaseEntity
{
    public int Id { get; set; }
    public required decimal PrincipalAmount { get; set; }
    public required decimal TotalAmountInvested { get; set; }
    public required int Year { get; set; }
    public required DateTime StartMonth { get; set; }
    public required DateTime EndMonth { get; set; }
}
