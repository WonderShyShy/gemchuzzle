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

    private Vector3 basePosition; // 基础位置（逻辑位置）
    private Vector3 visualOffset = Vector3.zero; // 视觉偏移（拖动时使用）
    private Vector3 targetPosition; // 目标位置
    private bool isMoving = false; // 是否正在移动
    private bool hasVisualOffset = false; // 是否有视觉偏移
    
    // 影子宝石（用于循环显示）
    private GameObject shadowGem; // 影子宝石对象
    private SpriteRenderer shadowRenderer; // 影子的渲染器

    void Start()
    {
        basePosition = transform.position;
        targetPosition = transform.position;
    }

    void Update()
    {
        // 如果有视觉偏移，应用偏移
        if (hasVisualOffset)
        {
            transform.position = basePosition + visualOffset;
        }
        // 否则平滑移动到目标位置
        else if (isMoving)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, moveSpeed * Time.deltaTime);
            
            // 到达目标位置
            if (Vector3.Distance(transform.position, targetPosition) < 0.01f)
            {
                transform.position = targetPosition;
                basePosition = targetPosition;
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
        basePosition = worldPosition;
        targetPosition = worldPosition;
        isMoving = false;
        hasVisualOffset = false;
        visualOffset = Vector3.zero;
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
        hasVisualOffset = false;
        visualOffset = Vector3.zero;
    }

    /// <summary>
    /// 应用视觉偏移（拖动时使用，不改变逻辑位置）
    /// </summary>
    public void ApplyVisualOffset(Vector3 offset)
    {
        visualOffset = offset;
        hasVisualOffset = true;
    }

    /// <summary>
    /// 创建或更新影子宝石
    /// </summary>
    public void CreateOrUpdateShadow(Vector3 shadowPosition)
    {
        // 如果影子不存在，创建它
        if (shadowGem == null)
        {
            // 创建影子GameObject
            shadowGem = new GameObject($"Shadow_{gameObject.name}");
            shadowGem.transform.parent = transform.parent; // 和原宝石同一个父对象
            
            // 复制SpriteRenderer
            SpriteRenderer originalRenderer = GetComponent<SpriteRenderer>();
            shadowRenderer = shadowGem.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = originalRenderer.sprite;
            shadowRenderer.material = originalRenderer.material;
            shadowRenderer.color = originalRenderer.color;
            shadowRenderer.sortingLayerID = originalRenderer.sortingLayerID;
            shadowRenderer.sortingOrder = originalRenderer.sortingOrder - 1; // 稍微低一点，在原宝石下面
            
            // 设置缩放（和原宝石一样）
            shadowGem.transform.localScale = transform.localScale;
            
            // 影子稍微透明一点（可选）
            Color shadowColor = shadowRenderer.color;
            shadowColor.a = 0.8f;
            shadowRenderer.color = shadowColor;
        }
        
        // 更新影子位置
        shadowGem.transform.position = shadowPosition;
        shadowGem.SetActive(true);
    }

    /// <summary>
    /// 销毁影子宝石
    /// </summary>
    public void DestroyShadow()
    {
        if (shadowGem != null)
        {
            Destroy(shadowGem);
            shadowGem = null;
            shadowRenderer = null;
        }
    }

    /// <summary>
    /// 检查是否有影子
    /// </summary>
    public bool HasShadow()
    {
        return shadowGem != null && shadowGem.activeSelf;
    }

    /// <summary>
    /// 重置视觉偏移
    /// </summary>
    public void ResetVisualOffset()
    {
        visualOffset = Vector3.zero;
        hasVisualOffset = false;
        transform.position = basePosition;
        
        // 同时销毁影子
        DestroyShadow();
    }

    /// <summary>
    /// 获取基础位置
    /// </summary>
    public Vector3 GetBasePosition()
    {
        return basePosition;
    }

    /// <summary>
    /// 更新基础位置（用于包裹计算）
    /// </summary>
    public void UpdateBasePosition(Vector3 newBasePosition)
    {
        basePosition = newBasePosition;
    }

    /// <summary>
    /// 检查是否正在移动
    /// </summary>
    public bool IsMoving()
    {
        return isMoving;
    }

    /// <summary>
    /// 检查是否有视觉偏移
    /// </summary>
    public bool HasVisualOffset()
    {
        return hasVisualOffset;
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

