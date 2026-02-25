using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家交互控制器，处理玩家与场景对象的交互
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - [SerializeField]序列化特性：让私有字段在Inspector中可编辑
/// - LayerMask：Unity层级掩码，用于射线检测
/// - GetComponent()：获取对象上的组件
/// - Ray：Unity射线结构，用于射线检测
/// - RaycastHit：Unity射线碰撞信息结构
/// - Physics.Raycast()：物理射线检测
/// - Debug.DrawRay()：在场景视图中绘制射线
/// - 泛型：GetComponent<T>()
/// </remarks>
public class PlayerInteract : MonoBehaviour
{
    private Camera cam;
    [SerializeField] private float distance = 3f;
    [SerializeField] private LayerMask mask;
    private InputManger inputManager;
    
    void Start()
    {
        cam = GetComponent<PlayerLook>().cam;
        inputManager = GetComponent<InputManger>();
    }

    void Update()
    {
        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        Debug.DrawRay(ray.origin, ray.direction * distance);
        RaycastHit hitInfo;
        if (Physics.Raycast(ray, out hitInfo, distance, mask))
        {
            if (hitInfo.collider.GetComponent<Interactable>() != null)
            {
                Interactable interactable = hitInfo.collider.GetComponent<Interactable>();
                // Debug.Log(hitInfo.collider.GetComponent<Interactable>().promptMessage);
                // playerUI.UpdateText(interactable.promptMessage);
                // if (inputManager.onFoot.Interact.triggered)
                // {
                //     interactable.BaseInteract();
                // }
            }
        }
    }
}
