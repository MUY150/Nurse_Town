using Newtonsoft.Json.Linq;

public interface IDynamicTool : ITool
{
    JObject GetDynamicSchema();
    void SetContext(ToolContext context);
}

public class ToolContext
{
    public string CharacterId { get; set; }
    public AnimationConfig AnimationConfig { get; set; }
    public ICharacterAnimation AnimationController { get; set; }
}
