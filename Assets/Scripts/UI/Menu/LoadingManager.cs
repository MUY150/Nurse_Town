using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI.Menu
{
    /// <summary>
    /// 加载管理器，管理游戏场景的加载过程
    /// </summary>
    /// <remarks>
    /// C#特性说明：
    /// - MonoBehaviour：Unity脚本基类
    /// - 命名空间（namespace）：代码组织结构
    /// - 单例模式（Singleton）：使用静态Instance字段确保全局唯一
    /// - Unity生命周期方法：Awake()
    /// - [SerializeField]特性：序列化字段，在Inspector中可编辑
    /// - 协程（Coroutine）：使用IEnumerator和yield return实现异步操作
    /// - Unity场景管理：SceneManager.LoadScene()
    /// - GameObject.Find()：查找场景中的游戏对象
    /// - DontDestroyOnLoad()：场景切换时不销毁对象
    /// - 静态属性：Instance属性
    /// </remarks>
    public class LoadingManager : MonoBehaviour
    {
        // 单例模式：静态实例，确保全局唯一
        public static LoadingManager Instance;
        [SerializeField] private Canvas _loadingScreen;

        /// <summary>
        /// Unity生命周期方法：对象创建时调用
        /// </summary>
        public void Awake()
        {
            // 单例模式：确保只有一个LoadingManager实例
            if (Instance == null)
            {
                Instance = this;
                // 场景切换时不销毁此对象
                DontDestroyOnLoad(gameObject);
            }

            // 查找加载屏幕Canvas组件
            // GameObject.Find()：在场景中查找指定名称的游戏对象
            _loadingScreen = GameObject.Find("Loading Screen").GetComponent<Canvas>();
            // 确保加载屏幕初始状态为隐藏
            _loadingScreen.enabled = false;
        }

        /// <summary>
        /// 加载指定场景
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        public void LoadScene(string sceneName)
        {
            Debug.Log("Loading scene: " + sceneName);
            // Unity场景管理：加载指定场景
            SceneManager.LoadScene(sceneName);
            // 注意：加载屏幕功能被注释掉了
        }

        /// <summary>
        /// 加载屏幕协程（当前被注释）
        /// </summary>
        /// <param name="sceneName">要加载的场景名称</param>
        /// <returns>IEnumerator，用于协程</returns>
        public IEnumerator LoadingScreen(string sceneName)
        {
            Debug.Log("Starting loading coroutine for scene: " + sceneName);
            // 协程：等待指定时间
            yield return new WaitForSeconds(2);
            Debug.Log("Loading scene after delay: " + sceneName);
            // 加载场景
            SceneManager.LoadScene(sceneName);
        }
    }
}
