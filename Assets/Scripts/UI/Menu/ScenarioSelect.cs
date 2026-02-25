using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// 场景选择控制器，管理多个游戏场景的选择和描述显示
    /// </summary>
    /// <remarks>
    /// C#特性说明：
    /// - MonoBehaviour：Unity脚本基类
    /// - 命名空间（namespace）：代码组织结构
    /// - Unity生命周期方法：Start()
    /// - 公共字段：public成员变量
    /// - PlayerPrefs：Unity持久化存储
    /// - Unity UI组件：Canvas、Button
    /// - Unity事件（UnityAction）：Unity专用事件类型
    /// - 委托（delegate）：定义事件处理器类型
    /// - 匿名函数：Lambda表达式 (参数) => { 方法体 }
    /// - Unity UI系统：Canvas切换
    /// - [SerializeField]特性：序列化字段（可能在其他地方使用）
    /// </remarks>
    public class ScenarioSelect : MonoBehaviour
    {
        // 公共字段：存储各个Canvas组件
        public Canvas welcomeCanvas;
        public Canvas scenarioSelectCanvas;
        public Canvas scenario1DescriptionCanvas;
        public Canvas scenario2DescriptionCanvas;
        public Canvas scenario3DescriptionCanvas;
        public Canvas scenario4DescriptionCanvas;
        public Canvas scenario5DescriptionCanvas;
        public Canvas loadingScreenCanvas;
        
        // 公共字段：存储场景选择按钮
        public Button scenario1Button;
        public Button scenario2Button;
        public Button scenario3Button;
        public Button scenario4Button;
        public Button scenario5Button;
        public Button nextButton;
        public Button backButton;
        public Button finishButton;
        
        /// <summary>
        /// Unity生命周期方法：初始化时调用
        /// </summary>
        void Start()
        {
            // 检查玩家是否已经访问过场景1
            // PlayerPrefs：Unity持久化存储系统，GetInt获取整数类型的保存值
            if (PlayerPrefs.GetInt("Scene" + 1, 0) == 1) // 检查是否已访问
            {
                // 如果已访问，显示场景选择界面
                welcomeCanvas.enabled = false;
                scenarioSelectCanvas.enabled = true;
            }
            else
            {
                // 如果未访问，显示欢迎界面
                welcomeCanvas.enabled = true;
                scenarioSelectCanvas.enabled = false;
            }
            
            // 初始状态：隐藏所有场景描述界面
            scenario1DescriptionCanvas.enabled = false;
            scenario2DescriptionCanvas.enabled = false;
            scenario3DescriptionCanvas.enabled = false;
            scenario4DescriptionCanvas.enabled = false;
            scenario5DescriptionCanvas.enabled = false;
            loadingScreenCanvas.enabled = false;
            
            // 注册按钮事件：使用Menu工具类注册点击事件
            Menu.ButtonAction(nextButton, NextWindow);
            Menu.ButtonAction(backButton, BackWindow);
            Menu.ButtonAction(finishButton, Finish);
            
            // 使用匿名函数（Lambda表达式）注册场景按钮事件
            // 匿名函数：() => { } 表示无参数的Lambda表达式
            Menu.ButtonAction(scenario1Button, ShowDescription(scenario1DescriptionCanvas) );
            Menu.ButtonAction(scenario2Button, ShowDescription(scenario2DescriptionCanvas) );
            Menu.ButtonAction(scenario3Button, ShowDescription(scenario3DescriptionCanvas) );
            Menu.ButtonAction(scenario4Button, ShowDescription(scenario4DescriptionCanvas) );
            Menu.ButtonAction(scenario5Button, ShowDescription(scenario5DescriptionCanvas) );
        }
        
        /// <summary>
        /// 切换到下一个窗口（从欢迎界面到场景选择界面）
        /// </summary>
        private void NextWindow()
        {
            // 菜单切换：从欢迎界面到场景选择界面
            Menu.CanvasTransition(welcomeCanvas, scenarioSelectCanvas);
        }
        
        /// <summary>
        /// 返回到上一个窗口（从场景选择界面到欢迎界面）
        /// </summary>
        private void BackWindow()
        {
            // 菜单切换：从场景选择界面到欢迎界面
            Menu.CanvasTransition(scenarioSelectCanvas, welcomeCanvas);
        }
        
        /// <summary>
        /// 完成场景选择操作
        /// </summary>
        private void Finish()
        {
            // 这里通常会加载下一个场景或执行某些操作
            Debug.Log("Finish button clicked. Implement your logic here.");
            // 例如，加载新场景：
            // LoadingManager.Instance.LoadScene("YourNextSceneName");
        }

        /// <summary>
        /// 显示场景描述信息
        /// </summary>
        /// <param name="descriptionCanvas">要显示的描述Canvas</param>
        /// <returns>UnityAction委托，用于按钮点击事件</returns>
        private UnityAction ShowDescription(Canvas descriptionCanvas)
        {
            // 匿名函数（Lambda表达式）：创建并返回事件处理器
            return () => 
            {
                // 这个Lambda函数将在按钮被点击时调用
                Menu.CanvasTransition(scenarioSelectCanvas, descriptionCanvas);
            };
        }

    }
}
