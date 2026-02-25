using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// 开始游戏菜单控制器，管理开始游戏界面的显示和返回
    /// </summary>
    /// <remarks>
    /// C#特性说明：
    /// - MonoBehaviour：Unity脚本基类
    /// - 命名空间（namespace）：代码组织结构
    /// - Unity生命周期方法：Start()
    /// - [SerializeField]特性：序列化字段，在Inspector中可编辑
    /// - Unity UI组件：Canvas、GameObject、Button
    /// - GetComponent方法：获取组件
    /// - Unity UI系统：Canvas切换
    /// </remarks>
    public class StartGame
    {
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject startGame;
        [SerializeField] private Button backButton;

        // 私有字段：存储Canvas组件引用
        private Canvas _mainMenuCanvas;
        private Canvas _creditsCanvas;
        private Canvas _backCanvas;
        
        /// <summary>
        /// Unity生命周期方法：初始化时调用
        /// </summary>
        void Start()
        {
            // GetComponent方法：获取Canvas组件
            _mainMenuCanvas = mainMenu.GetComponent<Canvas>();
            _creditsCanvas = startGame.GetComponent<Canvas>();
             // 确保开始游戏画布初始状态为禁用
        }
        
        /// <summary>
        /// 返回主菜单
        /// </summary>
        public void Back()
        {
            // 菜单切换：从开始游戏界面返回主菜单
            Menu.CanvasTransition(_creditsCanvas, _mainMenuCanvas);
        }
        
    }
}
