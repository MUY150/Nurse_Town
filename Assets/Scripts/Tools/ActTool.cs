using Newtonsoft.Json.Linq;
using UnityEngine;

public class ActTool : IDynamicTool
{
    public string Name => "act";
    
    public string Description => "Play a character animation to express emotions or actions. Use this to visually enhance your response.";

    private ToolContext _context;

    public JObject ParametersSchema => GetDynamicSchema();

    public JObject GetDynamicSchema()
    {
        var animations = GetAvailableAnimations();
        return new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["animation"] = new JObject
                {
                    ["type"] = "string",
                    ["enum"] = new JArray(animations),
                    ["description"] = "Character animation to play. Choose based on your emotional state and the conversation context."
                }
            },
            ["required"] = new JArray("animation")
        };
    }

    public void SetContext(ToolContext context)
    {
        _context = context;
    }

    public ToolResult Execute(JObject parameters)
    {
        try
        {
            string animation = parameters["animation"]?.ToString();
            
            if (string.IsNullOrEmpty(animation))
            {
                return ToolResult.ErrorResult("Animation parameter is required");
            }

            if (_context?.AnimationController != null)
            {
                _context.AnimationController.PlayByName(animation);
                Debug.Log($"[ActTool] Animation: {animation}");
            }
            else if (AnimationService.Instance != null)
            {
                AnimationService.Instance.PlayAnimation(animation);
                Debug.Log($"[ActTool] Animation via Service: {animation}");
            }
            else
            {
                Debug.LogWarning("[ActTool] No animation controller available");
                return ToolResult.ErrorResult("No animation controller available");
            }

            var animEvent = new AnimationExecutedEvent
            {
                Timestamp = System.DateTime.Now,
                AnimationName = animation
            };
            LlmEventBus.Publish(animEvent);

            return ToolResult.SuccessResult($"Played animation: {animation}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[ActTool] Error: {e.Message}");
            return ToolResult.ErrorResult(e.Message);
        }
    }

    private string[] GetAvailableAnimations()
    {
        if (_context?.AnimationConfig?.namedAnimations != null)
        {
            var keys = new string[_context.AnimationConfig.namedAnimations.Count];
            _context.AnimationConfig.namedAnimations.Keys.CopyTo(keys, 0);
            return keys;
        }

        if (AnimationService.Instance?.CurrentConfig?.namedAnimations != null)
        {
            var keys = new string[AnimationService.Instance.CurrentConfig.namedAnimations.Count];
            AnimationService.Instance.CurrentConfig.namedAnimations.Keys.CopyTo(keys, 0);
            return keys;
        }

        return new string[] { "idle", "pain", "happy", "sad", "shrug", "head_nod", "head_shake" };
    }
}
