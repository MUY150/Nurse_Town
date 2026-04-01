using UnityEngine;
using System;
using System.Collections.Generic;
using NurseTown.Core.Interfaces;

namespace NurseTown.Core.Effects
{
    /// <summary>
    /// 效果类型枚举
    /// </summary>
    public enum EffectType
    {
        Visual,
        Audio,
        Particle,
        PostProcessing,
        Combined
    }

    /// <summary>
    /// 效果配置基类
    /// </summary>
    [Serializable]
    public class EffectConfig
    {
        public string effectId;
        public string effectName;
        public EffectType effectType;
        public float duration = -1f; // -1 表示永久
        public bool autoStop = false;
        public float autoStopDelay = 0f;
    }

    /// <summary>
    /// 效果系统 - 统一管理游戏中所有效果
    /// </summary>
    public class EffectSystem : MonoBehaviour
    {
        [Header("效果配置")]
        [SerializeField] private List<EffectConfig> effectConfigs = new List<EffectConfig>();

        private Dictionary<string, IEffect> _effects = new Dictionary<string, IEffect>();
        private Dictionary<string, EffectConfig> _effectConfigs = new Dictionary<string, EffectConfig>();

        public static EffectSystem Instance { get; private set; }

        public event Action<string> OnEffectTriggered;
        public event Action<string> OnEffectStopped;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeEffectConfigs();
        }

        /// <summary>
        /// 初始化效果配置
        /// </summary>
        private void InitializeEffectConfigs()
        {
            _effectConfigs.Clear();
            foreach (var config in effectConfigs)
            {
                if (!string.IsNullOrEmpty(config.effectId))
                {
                    _effectConfigs[config.effectId] = config;
                }
            }
        }

        /// <summary>
        /// 注册效果
        /// </summary>
        public void RegisterEffect(IEffect effect)
        {
            if (effect == null || string.IsNullOrEmpty(effect.EffectId))
            {
                Debug.LogWarning("[EffectSystem] Cannot register effect with null or empty ID");
                return;
            }

            _effects[effect.EffectId] = effect;
            Debug.Log($"[EffectSystem] Registered effect: {effect.EffectId}");
        }

        /// <summary>
        /// 注销效果
        /// </summary>
        public void UnregisterEffect(string effectId)
        {
            if (_effects.ContainsKey(effectId))
            {
                _effects.Remove(effectId);
                Debug.Log($"[EffectSystem] Unregistered effect: {effectId}");
            }
        }

        /// <summary>
        /// 触发效果
        /// </summary>
        public bool TriggerEffect(string effectId)
        {
            if (!_effects.TryGetValue(effectId, out var effect))
            {
                Debug.LogWarning($"[EffectSystem] Effect not found: {effectId}");
                return false;
            }

            effect.Trigger();
            OnEffectTriggered?.Invoke(effectId);

            // 检查是否需要自动停止
            if (_effectConfigs.TryGetValue(effectId, out var config) && config.autoStop)
            {
                if (config.duration > 0)
                {
                    Invoke(nameof(StopEffectDelayed), config.duration);
                }
                else if (config.autoStopDelay > 0)
                {
                    Invoke(nameof(StopEffectDelayed), config.autoStopDelay);
                }
            }

            Debug.Log($"[EffectSystem] Triggered effect: {effectId}");
            return true;
        }

        /// <summary>
        /// 停止效果
        /// </summary>
        public bool StopEffect(string effectId)
        {
            if (!_effects.TryGetValue(effectId, out var effect))
            {
                Debug.LogWarning($"[EffectSystem] Effect not found: {effectId}");
                return false;
            }

            effect.Stop();
            OnEffectStopped?.Invoke(effectId);
            Debug.Log($"[EffectSystem] Stopped effect: {effectId}");
            return true;
        }

        /// <summary>
        /// 延迟停止效果（用于 Invoke）
        /// </summary>
        private void StopEffectDelayed(string effectId)
        {
            StopEffect(effectId);
        }

        /// <summary>
        /// 检查效果是否存在
        /// </summary>
        public bool HasEffect(string effectId)
        {
            return _effects.ContainsKey(effectId);
        }

        /// <summary>
        /// 获取效果
        /// </summary>
        public IEffect GetEffect(string effectId)
        {
            _effects.TryGetValue(effectId, out var effect);
            return effect;
        }

        /// <summary>
        /// 获取所有已注册的效果ID
        /// </summary>
        public string[] GetAllEffectIds()
        {
            var ids = new string[_effects.Count];
            _effects.Keys.CopyTo(ids, 0);
            return ids;
        }

        /// <summary>
        /// 停止所有效果
        /// </summary>
        public void StopAllEffects()
        {
            foreach (var effect in _effects.Values)
            {
                effect.Stop();
            }
            Debug.Log("[EffectSystem] All effects stopped");
        }

        /// <summary>
        /// 添加效果配置
        /// </summary>
        public void AddEffectConfig(EffectConfig config)
        {
            if (config == null || string.IsNullOrEmpty(config.effectId))
            {
                Debug.LogWarning("[EffectSystem] Cannot add effect config with null or empty ID");
                return;
            }

            _effectConfigs[config.effectId] = config;
            if (!effectConfigs.Contains(config))
            {
                effectConfigs.Add(config);
            }
        }

        /// <summary>
        /// 获取效果配置
        /// </summary>
        public EffectConfig GetEffectConfig(string effectId)
        {
            _effectConfigs.TryGetValue(effectId, out var config);
            return config;
        }

        void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
