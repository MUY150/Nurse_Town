using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家视角控制器，处理第一人称视角的旋转
/// </summary>
/// <remarks>
/// C#特性说明：
/// - MonoBehaviour：Unity脚本基类
/// - Camera：Unity摄像机组件
/// - Quaternion：Unity四元数，用于旋转
/// - Vector3：Unity三维向量结构
/// - Vector2：Unity二维向量结构
/// - transform.Rotate()：旋转游戏对象
/// - transform.localRotation：设置本地旋转
/// - Quaternion.Euler()：从欧拉角创建四元数
/// - Mathf.Clamp()：限制数值在指定范围内
/// - Time.deltaTime：帧时间增量
/// </remarks>
public class PlayerLook : MonoBehaviour
{
    public Camera cam;
    private float xRotation = 0f;
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    /// <summary>
    /// 处理视角输入
    /// </summary>
    /// <param name="input">输入向量</param>
    public void ProcessLook(Vector2 input)
    {
        float mouseX = input.x;
        float mouseY = input.y;

        xRotation -= (mouseY * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -80f, 80f);

        cam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.Rotate(Vector3.up * (mouseX * Time.deltaTime) * xSensitivity);
    }
}
