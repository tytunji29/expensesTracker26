using System.Text.Json.Serialization;

public class ReturnObject
{
    [JsonIgnore]
    public string source { get; set; }
    public bool Status { get; set; }
    private string? _message;

    public string Message
    {
        get
        {
            if (!Status) return "An error occurred";
            if (!string.IsNullOrEmpty(_message)) return _message;
            return source == "post" ? "Record Added Successfully" : "Record fetched successfully";
        }
        set => _message = value;
    }

    public dynamic Data { get; set; }

    // Constructor
    public ReturnObject(string sourceType = "get")
    {
        source = sourceType;
    }
}


public class BillResponse
{
    public int Id { get; set; }

    public string IncomeSourceName { get; set; }
    public string ExpenseName { get; set; }
    public int MonthId { get; set; }
    public string Month { get; set; }
    public int Year { get; set; }
    public bool Paid { get; set; }
}
