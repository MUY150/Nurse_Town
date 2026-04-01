using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;
using NurseTown.Core.Animation;
using NurseTown.Core.Effects;

public class ActTool : IDynamicTool
{
    public string Name => "act";

    public string Description => "Play a character animation to express emotions or actions. Use this to visually enhance your response.";

    private ToolContext _context;

    public JObject ParametersSchema => GetDynamicSchema();

    public JObject GetDynamicSchema()
    {
        var animations = GetAvailableAnimations();
        var descriptions = GetAnimationDescriptions();

        var properties = new JObject();
        foreach (var anim in animations)
        {
            string animDescription = descriptions.TryGetValue(anim, out var desc)
                ? $"{desc}. Use this animation to match your emotional expression."
                : "Play this animation to express your emotions.";

            properties[anim] = new JObject
            {
                ["type"] = "string",
                ["description"] = animDescription
            };
        }

        return new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject
            {
                ["animation"] = new JObject
                {
                    ["type"] = "string",
                    ["enum"] = new JArray(animations),
                    ["description"] = "Character animation to play. Choose the animation that best matches your current emotional state and the conversation context."
                }
            },
            ["required"] = new JArray("animation")
        };
    }

    private Dictionary<string, string> GetAnimationDescriptions()
    {
        // 优先使用 AnimationStateMachine 的配置
        if (AnimationStateMachine.Instance?.Config?.animationDescriptions != null)
        {
            return AnimationStateMachine.Instance.Config.animationDescriptions;
        }

        if (_context?.AnimationConfig?.animationDescriptions != null)
        {
            return _context.AnimationConfig.animationDescriptions;
        }

        if (AnimationService.Instance?.CurrentConfig?.animationDescriptions != null)
        {
            return AnimationService.Instance.CurrentConfig.animationDescriptions;
        }

        return new Dictionary<string, string>();
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

            bool animationPlayed = false;

            // 优先使用新的 AnimationStateMachine
            if (AnimationStateMachine.Instance != null)
            {
                AnimationStateMachine.Instance.PlayByName(animation);
                Debug.Log($"[ActTool] Animation via AnimationStateMachine: {animation}");
                animationPlayed = true;
            }
            // 回退到旧的 AnimationController
            else if (_context?.AnimationController != null)
            {
                _context.AnimationController.PlayByName(animation);
                Debug.Log($"[ActTool] Animation via AnimationController: {animation}");
                animationPlayed = true;
            }
            else if (AnimationService.Instance != null)
            {
                AnimationService.Instance.PlayAnimation(animation);
                Debug.Log($"[ActTool] Animation via AnimationService: {animation}");
                animationPlayed = true;
            }

            if (!animationPlayed)
            {
                Debug.LogWarning("[ActTool] No animation controller available");
                return ToolResult.ErrorResult("No animation controller available");
            }

            // 触发相关效果（如血液效果）
            TriggerRelatedEffects(animation);

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

    /// <summary>
    /// 触发与动画相关的效果
    /// </summary>
    private void TriggerRelatedEffects(string animationName)
    {
        // 使用 EffectSystem 触发相关效果
        if (EffectSystem.Instance != null)
        {
            // 根据动画名称触发对应的效果
            string effectId = GetEffectIdForAnimation(animationName);
            if (!string.IsNullOrEmpty(effectId))
            {
                EffectSystem.Instance.TriggerEffect(effectId);
                Debug.Log($"[ActTool] Triggered effect: {effectId} for animation: {animationName}");
            }
        }

        // 回退：使用旧的血液效果触发机制
        if (AnimationStateMachine.Instance?.Config != null)
        {
            string triggerName = AnimationStateMachine.Instance.Config.GetTriggerByName(animationName);
            if (!string.IsNullOrEmpty(triggerName))
            {
                var mapping = AnimationStateMachine.Instance.Config.GetMappingByTriggerName(triggerName);
                if (mapping != null && mapping.triggerBloodEffect)
                {
                    // 尝试通过 ICharacterAnimation 接口触发血液效果
                    _context?.AnimationController?.TriggerBloodEffect();
                }
            }
        }
    }

    /// <summary>
    /// 根据动画名称获取对应的效果ID
    /// </summary>
    private string GetEffectIdForAnimation(string animationName)
    {
        // 可以在这里添加动画到效果的映射逻辑
        // 例如：pain 动画对应 blood_effect 效果
        switch (animationName.ToLower())
        {
            case "pain":
            case "writhing_pain":
            case "blood_pressure":
                return "blood_effect";
            default:
                return null;
        }
    }

    private string[] GetAvailableAnimations()
    {
        // 优先使用 AnimationStateMachine 的配置
        if (AnimationStateMachine.Instance?.Config?.namedAnimations != null)
        {
            var keys = new string[AnimationStateMachine.Instance.Config.namedAnimations.Count];
            AnimationStateMachine.Instance.Config.namedAnimations.Keys.CopyTo(keys, 0);
            return keys;
        }

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
