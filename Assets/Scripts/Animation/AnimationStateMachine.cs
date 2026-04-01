using UnityEngine;
using System;
using System.Collections.Generic;

namespace NurseTown.Core.Animation
{
    /// <summary>
    /// 动画状态类型
    /// </summary>
    public enum AnimationStateType
    {
        Idle,
        Talking,
        Emotion,
        Action,
        Custom
    }

    /// <summary>
    /// 动画状态配置
    /// </summary>
    [Serializable]
    public class AnimationStateConfig
    {
        public string stateName;
        public AnimationStateType stateType;
        public string animationTrigger;
        public float transitionDuration = 0.25f;
        public bool canInterrupt = true;
        public AnimationStateType[] allowedTransitions;
        public Action onEnterState;
        public Action onExitState;
    }

    /// <summary>
    /// 配置驱动的动画状态机
    /// 统一管理角色动画状态转换
    /// </summary>
    public class AnimationStateMachine : MonoBehaviour
    {
        [Header("动画配置")]
        [SerializeField] private AnimationConfig animationConfig;
        [SerializeField] private List<AnimationStateConfig> stateConfigs = new List<AnimationStateConfig>();

        [Header("当前状态")]
        [SerializeField] private string currentStateName = "Idle";
        [SerializeField] private AnimationStateType currentStateType = AnimationStateType.Idle;

        private ICharacterAnimation _characterAnimation;
        private Dictionary<string, AnimationStateConfig> _stateMap;
        private AnimationStateConfig _currentState;

        public static AnimationStateMachine Instance { get; private set; }

        public string CurrentStateName => currentStateName;
        public AnimationStateType CurrentStateType => currentStateType;
        public AnimationConfig Config => animationConfig;

        public event Action<string, string> OnStateChanged;
        public event Action<AnimationStateConfig> OnStateEntered;
        public event Action<AnimationStateConfig> OnStateExited;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializeStateMap();
        }

        /// <summary>
        /// 初始化状态映射
        /// </summary>
        private void InitializeStateMap()
        {
            _stateMap = new Dictionary<string, AnimationStateConfig>();
            foreach (var config in stateConfigs)
            {
                if (!string.IsNullOrEmpty(config.stateName))
                {
                    _stateMap[config.stateName] = config;
                }
            }
        }

        /// <summary>
        /// 设置角色动画控制器
        /// </summary>
        public void SetCharacterAnimation(ICharacterAnimation characterAnimation)
        {
            _characterAnimation = characterAnimation;
            Debug.Log($"[AnimationStateMachine] Character animation controller set");
        }

        /// <summary>
        /// 设置动画配置
        /// </summary>
        public void SetAnimationConfig(AnimationConfig config)
        {
            animationConfig = config;
            Debug.Log($"[AnimationStateMachine] Animation config set: {config?.characterType}");
        }

        /// <summary>
        /// 注册动画状态
        /// </summary>
        public void RegisterState(AnimationStateConfig stateConfig)
        {
            if (_stateMap == null)
            {
                InitializeStateMap();
            }

            _stateMap[stateConfig.stateName] = stateConfig;
            if (!stateConfigs.Contains(stateConfig))
            {
                stateConfigs.Add(stateConfig);
            }
        }

        /// <summary>
        /// 切换到指定状态
        /// </summary>
        public bool TransitionTo(string stateName)
        {
            if (_stateMap == null || !_stateMap.TryGetValue(stateName, out var targetState))
            {
                Debug.LogWarning($"[AnimationStateMachine] State not found: {stateName}");
                return false;
            }

            // 检查是否允许从当前状态转换
            if (_currentState != null && !_currentState.canInterrupt)
            {
                if (_currentState.allowedTransitions != null)
                {
                    bool allowed = false;
                    foreach (var allowedType in _currentState.allowedTransitions)
                    {
                        if (allowedType == targetState.stateType)
                        {
                            allowed = true;
                            break;
                        }
                    }
                    if (!allowed)
                    {
                        Debug.LogWarning($"[AnimationStateMachine] Transition from {_currentState.stateName} to {stateName} not allowed");
                        return false;
                    }
                }
            }

            // 执行状态转换
            ExecuteTransition(targetState);
            return true;
        }

        /// <summary>
        /// 执行状态转换
        /// </summary>
        private void ExecuteTransition(AnimationStateConfig newState)
        {
            string previousState = currentStateName;

            // 退出当前状态
            if (_currentState != null)
            {
                _currentState.onExitState?.Invoke();
                OnStateExited?.Invoke(_currentState);
            }

            // 更新状态
            _currentState = newState;
            currentStateName = newState.stateName;
            currentStateType = newState.stateType;

            // 播放动画
            if (_characterAnimation != null && !string.IsNullOrEmpty(newState.animationTrigger))
            {
                _characterAnimation.PlayAnimation(newState.animationTrigger);
            }

            // 进入新状态
            newState.onEnterState?.Invoke();
            OnStateEntered?.Invoke(newState);
            OnStateChanged?.Invoke(previousState, currentStateName);

            Debug.Log($"[AnimationStateMachine] Transitioned from {previousState} to {currentStateName}");
        }

        /// <summary>
        /// 根据情绪代码播放动画
        /// </summary>
        public void PlayByEmotionCode(int emotionCode)
        {
            if (animationConfig == null)
            {
                Debug.LogWarning("[AnimationStateMachine] No animation config set");
                return;
            }

            var mapping = animationConfig.GetMappingByEmotionCode(emotionCode);
            if (mapping == null)
            {
                Debug.LogWarning($"[AnimationStateMachine] No mapping found for emotion code: {emotionCode}");
                return;
            }

            // 查找或创建对应的状态
            string stateName = $"Emotion_{emotionCode}";
            if (!_stateMap.ContainsKey(stateName))
            {
                var emotionState = new AnimationStateConfig
                {
                    stateName = stateName,
                    stateType = AnimationStateType.Emotion,
                    animationTrigger = mapping.triggerName,
                    canInterrupt = true
                };
                RegisterState(emotionState);
            }

            TransitionTo(stateName);

            // 触发血液效果（如果需要）
            if (mapping.triggerBloodEffect)
            {
                _characterAnimation?.TriggerBloodEffect();
            }
        }

        /// <summary>
        /// 根据动画名称播放
        /// </summary>
        public void PlayByName(string animationName)
        {
            if (animationConfig == null)
            {
                Debug.LogWarning("[AnimationStateMachine] No animation config set");
                return;
            }

            string triggerName = animationConfig.GetTriggerByName(animationName);
            if (string.IsNullOrEmpty(triggerName))
            {
                Debug.LogWarning($"[AnimationStateMachine] No trigger found for animation: {animationName}");
                return;
            }

            // 查找或创建对应的状态
            string stateName = $"Action_{animationName}";
            if (!_stateMap.ContainsKey(stateName))
            {
                var actionState = new AnimationStateConfig
                {
                    stateName = stateName,
                    stateType = AnimationStateType.Action,
                    animationTrigger = triggerName,
                    canInterrupt = true
                };
                RegisterState(actionState);
            }

            TransitionTo(stateName);
        }

        /// <summary>
        /// 播放空闲动画
        /// </summary>
        public void PlayIdle()
        {
            TransitionTo("Idle");
        }

        /// <summary>
        /// 播放说话动画
        /// </summary>
        public void PlayTalking()
        {
            TransitionTo("Talking");
        }

        /// <summary>
        /// 检查当前是否处于指定状态
        /// </summary>
        public bool IsInState(string stateName)
        {
            return currentStateName == stateName;
        }

        /// <summary>
        /// 检查当前是否处于指定状态类型
        /// </summary>
        public bool IsInStateType(AnimationStateType stateType)
        {
            return currentStateType == stateType;
        }

        /// <summary>
        /// 获取当前状态配置
        /// </summary>
        public AnimationStateConfig GetCurrentStateConfig()
        {
            return _currentState;
        }

        /// <summary>
        /// 获取所有注册的状态名称
        /// </summary>
        public string[] GetAllStateNames()
        {
            if (_stateMap == null) return new string[0];
            var names = new string[_stateMap.Count];
            _stateMap.Keys.CopyTo(names, 0);
            return names;
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
