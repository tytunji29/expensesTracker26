using System.Collections.Generic;

namespace expensesTracker26.Domain;

public class IncomeSource
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public decimal Amount { get; set; }

    public ICollection<BillsHolder> BillsHolders { get; set; } = new List<BillsHolder>();
}
