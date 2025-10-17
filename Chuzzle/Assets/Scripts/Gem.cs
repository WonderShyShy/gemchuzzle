using UnityEngine;

/// <summary>
/// 宝石类 - 代表棋盘上的单个宝石
/// </summary>
public class Gem : MonoBehaviour
{
    [Header("宝石属性")]
    public GemType gemType; // 宝石类型（颜色）
    public int row; // 所在行
    public int column; // 所在列

    [Header("移动设置")]
    public float moveSpeed = 10f; // 移动速度

    private Vector3 targetPosition; // 目标位置
    private bool isMoving = false; // 是否正在移动

    void Start()
    {
        targetPosition = transform.position;
    }

    void Update()
    {
        // 平滑移动到目标位置
        if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            
            // 到达目标位置
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                isMoving = false;
            }
        }
    }

    /// <summary>
    /// 设置宝石位置（立即设置）
    /// </summary>
    public void SetPosition(int row, int column, Vector3 worldPosition)
    {
        this.row = row;
        this.column = column;
        transform.position = worldPosition;
        targetPosition = worldPosition;
        isMoving = false;
    }

    /// <summary>
    /// 移动到新位置（带动画）
    /// </summary>
    public void MoveTo(int newRow, int newColumn, Vector3 worldPosition)
    {
        this.row = newRow;
        this.column = newColumn;
        targetPosition = worldPosition;
        isMoving = true;
    }

    /// <summary>
    /// 检查是否正在移动
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }
}

/// <summary>
/// 宝石类型枚举
/// </summary>
public enum GemType
{
    Blue,    // 蓝色
    Green,   // 绿色
    Orange,  // 橙色
    Pink,    // 粉色
    Red,     // 红色
    White,   // 白色
    Yellow   // 黄色
}

