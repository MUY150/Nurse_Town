using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 血液效果控制器，管理游戏中血液视觉效果的显示和控制
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - 私有字段：private成员变量
/// - Unity UI组件：Image、TextMeshProUGUI
/// - [SerializeField]特性：序列化字段，在Inspector中可编辑
/// - Unity生命周期方法：Start()、Update()
/// - 输入检测：Input.GetKeyDown()检测按键输入
/// - GameObject.Find()：查找场景中的游戏对象
/// - GetComponent方法：获取组件
/// - Animator动画系统：SetTrigger()触发动画
/// - 颜色操作：Color结构和alpha通道
/// - Unity API：Debug.Log()、Debug.LogError()
/// - 条件判断：if语句和else分支
/// - 布尔变量：控制血液显示状态
/// - 异常检查：null检查防止空引用异常
/// </remarks>
public class BloodEffectController : MonoBehaviour
{
    // 私有字段：存储血液UI组件
    private Image blood;
    
    // [SerializeField]特性：序列化字段，在Inspector中可编辑
    [SerializeField] private bool showBlood = false;
    [SerializeField] private TextMeshProUGUI bloodPressureText;
    
    // 私有字段：存储其他控制器引用
    private BloodTextController bloodTextController;
    private bool canMeasureBloodPressure = false;
    private Animator animator;
    
    /// <summary>
    /// Unity生命周期方法：初始化时调用
    /// </summary>
    void Start()
    {
        // 获取血液Image组件
        blood = GetComponent<Image>();
        // 根据showBlood设置初始显示状态
        blood.enabled = showBlood;
        
        // 查找BloodTextController组件
        // FindObjectOfType<T>()：查找场景中指定类型的组件
        bloodTextController = FindObjectOfType<BloodTextController>();
        
        // 查找特定的坐姿动画器
        // GameObject.Find()：在场景中查找指定名称的游戏对象
        GameObject sittingObject = GameObject.Find("Sitting");
        if (sittingObject != null)
        {
            // 获取Animator组件
            animator = sittingObject.GetComponent<Animator>();
            if (animator == null)
            {
                // 异常检查：记录错误日志
                Debug.LogError("Animator component not found on Sitting GameObject!");
            }
        }
        else
        {
            // 异常检查：记录错误日志
            Debug.LogError("Sitting GameObject found in scene!");
        }
        
        // 查找血压文本组件（当前被注释）
        //bloodPressureText = GameObject.Find("BloodPressureVal").GetComponent<TextMeshProUGUI>();
        if (bloodPressureText == null)
        {
            // 异常检查：记录错误日志
            Debug.LogError("Blood Pressure Text component not found!");
        }
        else
        {
            // 设置初始文本内容
            bloodPressureText.text = "Patient blood pressure: Unknown";
        }
    }

    /// <summary>
    /// Unity生命周期方法：每帧调用
    /// </summary>
    void Update()
    {
        blood.enabled = showBlood;
        
        if (GameInputStateMachine.Instance != null && 
            GameInputStateMachine.Instance.IsUIActive())
        {
            return;
        }
        
        if (canMeasureBloodPressure && Input.GetKeyDown(KeyCode.F))
        {
            MeasureBloodPressure();
        }
    }

    /// <summary>
    /// 设置血液显示状态
    /// </summary>
    /// <param name="show">是否显示血液效果</param>
    public void SetBloodVisibility(bool show)
    {
        // 更新显示状态
        showBlood = show;
        canMeasureBloodPressure = show;
        Debug.Log("Showing blood called: " + showBlood);
    }

    /// <summary>
    /// 执行血压测量操作
    /// </summary>
    private void MeasureBloodPressure()
    {
        // 隐藏血液效果和文本
        showBlood = false;
        bloodTextController.SetBloodTextVisibility(false);
        
        // 更新血压文本内容
        if (bloodPressureText != null)
        {
            // 设置血压数值
            bloodPressureText.text = "Patient blood pressure: 150 mmHg";
            Debug.Log("Blood pressure text updated");
        }
        else
        {
            // 异常检查：记录错误日志
            Debug.LogError("Blood Pressure Text is null when trying to measure!");
        }
        
        // 触发动画
        if (animator != null)
        {
            // Animator动画系统：SetTrigger()触发动画
            // animator.SetTrigger("after_blood_mea");
            animator.SetTrigger("after_BP");
            
            Debug.Log("Animation trigger set: after_blood_mea, after_BP");
        }
        else
        {
            // 异常检查：记录错误日志
            Debug.LogError("Animator not found!");
        }
        
        // 禁用测量功能
        canMeasureBloodPressure = false;
    }

    /// <summary>
    /// 设置血液透明度
    /// </summary>
    /// <param name="alpha">透明度值（0-1）</param>
    public void SetAlpha(float alpha)
    {
        // 颜色操作：获取当前颜色并修改alpha通道
        Color color = blood.color;
        color.a = alpha;
        blood.color = color;
    }
}
