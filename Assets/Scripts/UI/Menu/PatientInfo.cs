using UnityEngine;

namespace UI.Menu
{
    /// <summary>
    /// 病人信息控制器，管理病人信息界面的显示和切换
    /// </summary>
    /// <remarks>
    /// C#特性说明：
    /// - MonoBehaviour：Unity脚本基类
    /// - 命名空间（namespace）：代码组织结构
    /// - Unity生命周期方法：Update()
    /// - 公共字段：public成员变量
    /// - 输入检测：Input.GetKeyDown()检测按键输入
    /// - GameObject.activeSelf：检测游戏对象是否激活
    /// - GameObject.SetActive()：设置游戏对象激活状态
    /// </remarks>
    public class PatientInfo : MonoBehaviour
    {
        // 公共字段：存储病人信息UI组件
        public GameObject patientInfoUI;
    
        /// <summary>
        /// Unity生命周期方法：每帧调用
        /// </summary>
        void Update()
        {
            // 输入检测：检测I键按下
            if (Input.GetKeyDown(KeyCode.I))
            {
                // 切换病人信息显示状态
                ToggleInfo();
            }
        }
    
        /// <summary>
        /// 切换病人信息显示状态
        /// </summary>
        public void ToggleInfo()
        {
            // 检查当前显示状态
            if (patientInfoUI.activeSelf)
            {
                // 如果当前显示，则隐藏病人信息UI
                patientInfoUI.SetActive(false);
            }
            else
            {
                // 如果当前隐藏，则显示病人信息UI
                patientInfoUI.SetActive(true);
            }
        }
    }
}
