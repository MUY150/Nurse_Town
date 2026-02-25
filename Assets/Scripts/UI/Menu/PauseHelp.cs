using UnityEngine;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// 暂停帮助菜单控制器，管理暂停时显示的帮助界面
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
    public class PauseHelp : MonoBehaviour
    {
        [SerializeField] private GameObject pauseMenu;
        [SerializeField] private GameObject helpMenu;
        [SerializeField] private Button backButton;

        // 私有字段：存储Canvas组件引用
        private Canvas _pauseMenuCanvas;
        private Canvas _helpCanvas;
        private Canvas _backCanvas;
        
        /// <summary>
        /// Unity生命周期方法：初始化时调用
        /// </summary>
        void Start()
        {
            // GetComponent方法：获取Canvas组件
            _pauseMenuCanvas = pauseMenu.GetComponent<Canvas>();
            _helpCanvas = helpMenu.GetComponent<Canvas>();
            
            // 注册返回按钮事件：使用Menu工具类注册点击事件
            Menu.ButtonAction(backButton, Back);
        }
        
        /// <summary>
        /// 返回到暂停菜单
        /// </summary>
        public void Back()
        {
            // 隐藏帮助菜单（这里直接使用SetActive而不是Canvas切换）
            helpMenu.SetActive(false);
        }
        
    }
}
