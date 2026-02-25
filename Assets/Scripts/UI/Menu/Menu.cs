using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Menu
{
    /// <summary>
    /// 菜单基础工具类，提供菜单系统的通用功能
    /// </summary>
    /// <remarks>
    /// C#特性说明：
    /// - 静态类（static class）：不能实例化，只包含静态成员
    /// - 命名空间（namespace）：代码组织结构
    /// - Unity事件（UnityAction）：Unity专用事件类型
    /// - Unity UI组件（Button、Canvas）：Unity UI系统
    /// - 异常处理：try-catch块
    /// - 空条件运算符：?. 避免空引用异常
    /// - 字符串连接：使用+操作符连接字符串
    /// - 方法名访问：Method.Name获取方法名称
    /// </remarks>
    public static class Menu
    {
        /// <summary>
        /// 为按钮注册点击事件处理函数
        /// </summary>
        /// <param name="button">要注册的按钮组件</param>
        /// <param name="functionName">要执行的事件处理函数</param>
        public static void ButtonAction(Button button, UnityAction functionName)
        {
            try
            {
                // Unity事件系统：AddListener添加事件监听器
                button.onClick.AddListener(functionName);
            }
            catch
            {
                // 异常处理：捕获并记录错误信息
                Debug.LogError("\tRegisterFunc with button "
                               + button.name
                               + "\n\tand function "
                               + functionName.Method.Name
                               + "\n\thas failed by "
                               + (button == null ? "null button" : "add listener"));
            }
        }

        /// <summary>
        /// 在两个画布之间切换显示
        /// </summary>
        /// <param name="currentCanvas">当前显示的画布</param>
        /// <param name="nextCanvas">要切换到的画布</param>
        public static void CanvasTransition(Canvas currentCanvas, Canvas nextCanvas)
        {
            // 隐藏当前画布，显示目标画布
            currentCanvas.enabled = false;
            nextCanvas.enabled = true;
        }
        
        
    }
}
