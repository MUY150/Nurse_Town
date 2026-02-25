using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// 制作人员名单控制器，管理制作人员名单界面的显示和返回
    /// </summary>
    /// <remarks>
    /// C#特性说明：
    /// - MonoBehaviour：Unity脚本基类
    /// - 命名空间（namespace）：代码组织结构
    /// - Unity生命周期方法：Start()
    /// - [SerializeField]特性：序列化字段，在Inspector中可编辑
    /// - Unity UI组件：Canvas、GameObject、Button
    /// - GetComponent方法：获取组件
    /// - Unity UI系统：GameObject.SetActive()控制显示状态
    /// - 私有字段：私有成员变量
    /// </remarks>
    public class Credits : MonoBehaviour
    {
        [SerializeField] private GameObject mainMenu;
        [SerializeField] private GameObject credits;
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
            _creditsCanvas = credits.GetComponent<Canvas>();
            
            // 注册返回按钮事件：使用Menu工具类注册点击事件
            Menu.ButtonAction(backButton, Back);
        }
        
        /// <summary>
        /// 返回到主菜单
        /// </summary>
        public void Back()
        {
            // 隐藏制作人员名单界面（这里直接使用SetActive而不是Canvas切换）
            credits.SetActive(false);
        }
        
    }
}
