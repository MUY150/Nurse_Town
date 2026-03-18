using Newtonsoft.Json.Linq;

public interface ITool
{
    string Name { get; }
    string Description { get; }
    JObject ParametersSchema { get; }
    ToolResult Execute(JObject parameters);
}

public class ToolResult
{
    public bool Success { get; set; }
    public string Message { get; set; }
    public JObject Data { get; set; }
    
    public static ToolResult SuccessResult(string message = null, JObject data = null)
    {
        return new ToolResult { Success = true, Message = message, Data = data };
    }
    
    public static ToolResult ErrorResult(string message)
    {
        return new ToolResult { Success = false, Message = message };
    }
}

public class ToolCallEventArgs
{
    public string ToolName { get; set; }
    public JObject Parameters { get; set; }
    public ToolResult Result { get; set; }
}
