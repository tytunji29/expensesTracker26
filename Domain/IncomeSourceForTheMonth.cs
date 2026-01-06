
using System.Text.Json.Serialization;

namespace expensesTracker26.Domain;

public class IncomeSourceForTheMonth : BaseEntity
{
    public int Id { get; set; }

    public required int IncomeSourceId { get; set; }

    public int Year { get; set; }
    public int Month { get; set; }
        [JsonIgnore]
    public IncomeSource IncomeSource { get; set; }

}
