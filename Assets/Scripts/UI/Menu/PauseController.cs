using UnityEngine;

namespace UI.Menu 
{
    /// <summary>
    /// 暂停控制器，管理游戏的暂停状态和暂停菜单显示
    /// </summary>
    /// <remarks>
    /// C#特性说明：
    /// - MonoBehaviour：Unity脚本基类
    /// - 命名空间（namespace）：代码组织结构
    /// - Unity生命周期方法：Update()
    /// - 公共字段：public成员变量
    /// - 输入检测：Input.GetKeyDown()检测按键输入
    /// - Time.timeScale：控制游戏时间缩放
    /// - GameObject.activeSelf：检测游戏对象是否激活
    /// - GameObject.SetActive()：设置游戏对象激活状态
    /// - 条件编译：#if UNITY_EDITOR
    /// - Unity场景管理：SceneManager.LoadScene()
    /// - Unity API：Application.Quit()、UnityEditor.EditorApplication.isPlaying
    /// </remarks>
    public class PauseController : MonoBehaviour
    {
        // 公共字段：存储暂停菜单UI组件
        public GameObject PauseMenuUI;
        public GameObject HelpMenu;
        public GameObject CreditsMenu;

        /// <summary>
        /// Unity生命周期方法：每帧调用
        /// </summary>
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                var stateMachine = GameInputStateMachine.Instance;
                if (stateMachine != null && stateMachine.CurrentState != GameInputState.Gameplay)
                {
                    return;
                }
                TogglePause();
            }
        }

        /// <summary>
        /// 切换暂停状态
        /// </summary>
        public void TogglePause()
        {
            // 检查当前时间缩放：如果为1表示游戏正在运行
            if (Time.timeScale == 1)
            {
                // 暂停游戏
                Time.timeScale = 0;
                // 显示暂停菜单UI
                PauseMenuUI.SetActive(true);
            }
            else
            {
                // 恢复游戏
                Time.timeScale = 1;
                // 隐藏暂停菜单UI
                PauseMenuUI.SetActive(false);
            }
        }
        
        /// <summary>
        /// 切换帮助菜单显示
        /// </summary>
        public void ToggleHelpCanvas()
        {
            Debug.Log("Toggling Help Canvas");
            // 检查帮助菜单是否激活
            if (HelpMenu.activeSelf)
            {
                // 如果帮助菜单激活，则显示暂停菜单，隐藏帮助菜单
                PauseMenuUI.SetActive(true);
                HelpMenu.SetActive(false);
            }
            else
            {
                // 如果帮助菜单未激活，则隐藏暂停菜单，显示帮助菜单
                PauseMenuUI.SetActive(false);
                HelpMenu.SetActive(true);
            }
        }
        
        /// <summary>
        /// 切换制作人员名单菜单显示
        /// </summary>
        public void ToggleCreditsCanvas()
        {
            Debug.Log("Toggling Credits Canvas");
            // 检查制作人员名单菜单是否激活
            if (CreditsMenu.activeSelf)
            {
                // 如果制作人员名单菜单激活，则显示暂停菜单，隐藏制作人员名单菜单
                PauseMenuUI.SetActive(true);
                CreditsMenu.SetActive(false);
            }
            else
            {
                // 如果制作人员名单菜单未激活，则隐藏暂停菜单，显示制作人员名单菜单
                PauseMenuUI.SetActive(false);
                CreditsMenu.SetActive(true);
            }
        }
        
        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void ReturnToMainMenu()
        {
            // 恢复时间缩放
            Time.timeScale = 1;
            // 加载主菜单场景
            UnityEngine.SceneManagement.SceneManager.LoadScene("Scenes/Menu/Start");
        }
        
        /// <summary>
        /// 退出游戏
        /// </summary>
        public void QuitGame()
        {
            // 条件编译：根据不同平台执行不同的退出逻辑
            #if UNITY_EDITOR
            {
                // 编辑器模式：停止播放
                UnityEditor.EditorApplication.isPlaying = false;
            }
            #else 
		    {
			    // 发布版本：退出应用程序
			    Application.Quit();
		    }
            #endif
        }
    }
}
