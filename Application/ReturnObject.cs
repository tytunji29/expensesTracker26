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
