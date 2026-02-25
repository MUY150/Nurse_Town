using UnityEngine;
using TMPro;

/// <summary>
/// 血液文本控制器，管理血压测量相关文本的显示和隐藏
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - Unity UI组件：TextMeshProUGUI
/// - [SerializeField]特性：序列化字段，在Inspector中可编辑
/// - Unity生命周期方法：Start()、Update()
/// - GameObject.Find()：查找场景中的游戏对象
/// - GetComponent方法：获取组件
/// - Unity API：Debug.Log()、Debug.LogError()
/// - 布尔变量：控制文本显示状态
/// - 条件判断：if语句和else分支
/// - 异常检查：null检查防止空引用异常
/// - TextMeshPro组件：高质量文本渲染组件
/// </remarks>
public class BloodTextController : MonoBehaviour
{
    // 私有字段：存储血压文本组件
    private TextMeshProUGUI pressF_Text;
    
    // [SerializeField]特性：序列化字段，在Inspector中可编辑
    [SerializeField] private bool showBlood = false;
    
    /// <summary>
    /// Unity生命周期方法：初始化时调用
    /// </summary>
    void Start()
    {
        // 查找血压文本组件
        // GameObject.Find()：在场景中查找指定名称的游戏对象
        // TextMeshProUGUI：TextMeshPro的UI文本组件
        pressF_Text = GameObject.Find("bloodText").GetComponent<TextMeshProUGUI>();
        
        // 异常检查：验证文本组件是否找到
        if (pressF_Text == null)
        {
            // 异常检查：记录错误日志
            Debug.LogError("PressF Text component not found!");
        }
        else
        {
            // 设置初始文本内容和启用状态
            pressF_Text.text = "Press F to measure blood pressure";
            pressF_Text.enabled = showBlood;
        }
    }

    /// <summary>
    /// Unity生命周期方法：每帧调用
    /// </summary>
    void Update()
    {
        // 检查文本组件是否存在
        if (pressF_Text != null)
        {
            // 根据showBlood状态更新文本显示
            pressF_Text.enabled = showBlood;
        }
    }

    /// <summary>
    /// 设置血液文本显示状态
    /// </summary>
    /// <param name="show">是否显示血液文本</param>
    public void SetBloodTextVisibility(bool show)
    {
        // 更新显示状态
        showBlood = show;
        Debug.Log("Showing blood text called: " + showBlood);
    }
}
