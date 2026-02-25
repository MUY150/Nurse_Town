using UnityEngine;
using UnityEngine.UI;
using UI.Menu;
using Unity.VisualScripting;

namespace UI.Menu
{
    /// <summary>
    /// 主菜单控制器，管理游戏主菜单的显示和交互
    /// </summary>
    /// <remarks>
    /// C#特性说明：
    /// - MonoBehaviour：Unity脚本基类
    /// - 命名空间（namespace）：代码组织结构
    /// - Unity生命周期方法：Start()
    /// - [SerializeField]特性：序列化字段，在Inspector中可编辑
    /// - Unity UI组件：Button、Canvas、GameObject
    /// - GetComponent方法：获取组件
    /// - 条件编译：#if UNITY_EDITOR
    /// - 预处理器指令：#else、#endif
    /// - Unity API：Application.Quit()、UnityEditor.EditorApplication.isPlaying
    /// - 私有字段：私有成员变量
    /// </remarks>
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject credits;
        [SerializeField] private GameObject loadingScreen;
        [SerializeField] private Button startGameButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        // 私有字段：存储Canvas组件引用
        private Canvas _mainMenuCanvas;
        private Canvas _startGameCanvas;
        private Canvas _creditsCanvas;
        private Canvas _quitCanvas;
        private Canvas _loadingScreenCanvas;
    
        /// <summary>
        /// Unity生命周期方法：初始化时调用
        /// </summary>
        void Start()
        {
            // GetComponent方法：获取Canvas组件
            _mainMenuCanvas = mainMenu.GetComponent<Canvas>();
            _creditsCanvas = credits.GetComponent<Canvas>();
            _loadingScreenCanvas = loadingScreen.GetComponent<Canvas>();
            
            // 初始状态：隐藏其他菜单
            _creditsCanvas.enabled = false; 
            _loadingScreenCanvas.enabled = false;
            
            // 注册按钮事件：使用Menu工具类注册点击事件
            Menu.ButtonAction(startGameButton, NewGame);
            Menu.ButtonAction(creditsButton, Credits);
            Menu.ButtonAction(quitButton, Quit);
        }
    
        /// <summary>
        /// 开始新游戏
        /// </summary>
        private void NewGame()
        {
            // 菜单切换：从主菜单切换到开始游戏界面
            Menu.CanvasTransition(_mainMenuCanvas, _startGameCanvas);
        }
        
        /// <summary>
        /// 显示制作人员名单
        /// </summary>
        private void Credits()
        {
            // 菜单切换：从主菜单切换到制作人员名单
            Menu.CanvasTransition(_mainMenuCanvas, _creditsCanvas);
        }

        /// <summary>
        /// 退出游戏
        /// </summary>
        private void Quit()
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
