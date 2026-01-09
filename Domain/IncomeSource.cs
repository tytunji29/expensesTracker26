using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace expensesTracker26.Domain;

public class IncomeSource : BaseEntity
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public decimal Amount { get; set; }
    public int PaymentDate { get; set; }

    [JsonIgnore]

    public ICollection<BillsHolder> BillsHolders { get; set; } = new List<BillsHolder>();
}
