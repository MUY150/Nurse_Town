using UnityEngine;
using UnityEngine.UI;
using TMPro;
using NurseTown.Core.Interfaces;

namespace NurseTown.Core.Effects
{
    /// <summary>
    /// 血压效果 - 管理血液视觉效果和血压测量功能
    /// 实现了 IEffect 接口，可以被 EffectSystem 统一管理
    /// </summary>
    public class BloodPressureEffect : MonoBehaviour, IEffect
    {
        [Header("UI 组件")]
        [SerializeField] private Image bloodImage;
        [SerializeField] private TextMeshProUGUI bloodPressureText;
        [SerializeField] private TextMeshProUGUI promptText;

        [Header("效果配置")]
        [SerializeField] private string effectId = "blood_pressure";
        [SerializeField] private bool autoRegister = true;
        [SerializeField] private string promptMessage = "Press F to measure blood pressure";
        [SerializeField] private string measuredText = "Patient blood pressure: 150 mmHg";
        [SerializeField] private string unknownText = "Patient blood pressure: Unknown";

        [Header("动画")]
        [SerializeField] private Animator targetAnimator;
        [SerializeField] private string measurementAnimationTrigger = "after_BP";

        // 状态
        private bool _isActive = false;
        private bool _canMeasure = false;

        public string EffectId => effectId;
        public bool IsActive => _isActive;
        public bool CanMeasure => _canMeasure;

        void Start()
        {
            // 自动查找组件
            FindComponents();

            // 初始化 UI 状态
            SetBloodVisibility(false);
            UpdateBloodPressureText(unknownText);

            // 自动注册到效果系统
            if (autoRegister && EffectSystem.Instance != null)
            {
                EffectSystem.Instance.RegisterEffect(this);
            }
        }

        void Update()
        {
            // 检查输入（仅在效果激活时）
            if (_isActive && _canMeasure)
            {
                if (GameInputStateMachine.Instance != null &&
                    GameInputStateMachine.Instance.IsUIActive())
                {
                    return;
                }

                if (Input.GetKeyDown(KeyCode.F))
                {
                    MeasureBloodPressure();
                }
            }
        }

        /// <summary>
        /// 查找必要的组件
        /// </summary>
        private void FindComponents()
        {
            // 查找血液图像
            if (bloodImage == null)
            {
                bloodImage = GetComponent<Image>();
            }

            // 查找提示文本
            if (promptText == null)
            {
                var promptObj = GameObject.Find("bloodText");
                if (promptObj != null)
                {
                    promptText = promptObj.GetComponent<TextMeshProUGUI>();
                }
            }

            // 查找血压文本
            if (bloodPressureText == null)
            {
                var bpObj = GameObject.Find("BloodPressureVal");
                if (bpObj != null)
                {
                    bloodPressureText = bpObj.GetComponent<TextMeshProUGUI>();
                }
            }

            // 查找动画器
            if (targetAnimator == null)
            {
                var sittingObj = GameObject.Find("Sitting");
                if (sittingObj != null)
                {
                    targetAnimator = sittingObj.GetComponent<Animator>();
                }
            }
        }

        /// <summary>
        /// 触发效果 - 显示血液效果
        /// </summary>
        public void Trigger()
        {
            SetBloodVisibility(true);
            Debug.Log($"[BloodPressureEffect] Effect triggered: {effectId}");
        }

        /// <summary>
        /// 停止效果 - 隐藏血液效果
        /// </summary>
        public void Stop()
        {
            SetBloodVisibility(false);
            Debug.Log($"[BloodPressureEffect] Effect stopped: {effectId}");
        }

        /// <summary>
        /// 设置血液效果可见性
        /// </summary>
        public void SetBloodVisibility(bool visible)
        {
            _isActive = visible;
            _canMeasure = visible;

            if (bloodImage != null)
            {
                bloodImage.enabled = visible;
            }

            if (promptText != null)
            {
                promptText.enabled = visible;
                if (visible)
                {
                    promptText.text = promptMessage;
                }
            }

            Debug.Log($"[BloodPressureEffect] Blood visibility set to: {visible}");
        }

        /// <summary>
        /// 测量血压
        /// </summary>
        public void MeasureBloodPressure()
        {
            if (!_canMeasure) return;

            // 隐藏血液效果和提示
            SetBloodVisibility(false);

            // 更新血压文本
            UpdateBloodPressureText(measuredText);

            // 触发测量动画
            if (targetAnimator != null && !string.IsNullOrEmpty(measurementAnimationTrigger))
            {
                targetAnimator.SetTrigger(measurementAnimationTrigger);
                Debug.Log($"[BloodPressureEffect] Animation triggered: {measurementAnimationTrigger}");
            }
            else
            {
                Debug.LogWarning("[BloodPressureEffect] Animator not found or trigger name empty");
            }

            _canMeasure = false;

            Debug.Log("[BloodPressureEffect] Blood pressure measured");
        }

        /// <summary>
        /// 更新血压文本
        /// </summary>
        private void UpdateBloodPressureText(string text)
        {
            if (bloodPressureText != null)
            {
                bloodPressureText.text = text;
            }
            else
            {
                Debug.LogWarning("[BloodPressureEffect] Blood pressure text component not found");
            }
        }

        /// <summary>
        /// 设置血液透明度
        /// </summary>
        public void SetAlpha(float alpha)
        {
            if (bloodImage != null)
            {
                Color color = bloodImage.color;
                color.a = Mathf.Clamp01(alpha);
                bloodImage.color = color;
            }
        }

        /// <summary>
        /// 重置效果到初始状态
        /// </summary>
        public void Reset()
        {
            Stop();
            UpdateBloodPressureText(unknownText);
        }

        void OnDestroy()
        {
            // 从效果系统注销
            if (EffectSystem.Instance != null)
            {
                EffectSystem.Instance.UnregisterEffect(effectId);
            }
        }
    }
}
