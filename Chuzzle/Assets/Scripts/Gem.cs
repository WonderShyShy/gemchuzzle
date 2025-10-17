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
    
    // 多米诺回弹动画
    private bool isDominoAnimating = false; // 是否正在多米诺动画
    private float dominoDelay = 0f; // 多米诺延迟
    private float dominoTimer = 0f; // 多米诺计时器
    private float dominoDuration = 0.2f; // 多米诺动画持续时间
    private Vector3 dominoStartOffset; // 动画起始偏移
    
    // 影子宝石（用于循环显示）
    private GameObject shadowGem; // 影子宝石对象
    private SpriteRenderer shadowRenderer; // 影子的渲染器
    private Vector3 shadowStartPos; // 影子起始位置（用于动画）

    void Start()
    {
        basePosition = transform.position;
        targetPosition = transform.position;
    }

    void Update()
    {
        // 多米诺回弹动画优先级最高
        if (isDominoAnimating)
        {
            // 等待延迟
            if (dominoDelay > 0)
            {
                dominoDelay -= Time.deltaTime;
                return;
            }
            
            // 更新计时器
            dominoTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(dominoTimer / dominoDuration);
            
            // 使用EaseOut曲线
            float easedProgress = 1 - Mathf.Pow(1 - progress, 3);
            
            // 插值偏移：从起始偏移回到0
            visualOffset = Vector3.Lerp(dominoStartOffset, Vector3.zero, easedProgress);
            transform.position = basePosition + visualOffset;
            
            // 动画完成
            if (progress >= 1f)
            {
                isDominoAnimating = false;
                hasVisualOffset = false;
                visualOffset = Vector3.zero;
                transform.position = basePosition;
                // 注意：影子由AnimateShadow协程单独处理
            }
        }
        // 如果有视觉偏移，应用偏移
        else if (hasVisualOffset)
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
    /// 重置视觉偏移（立即）
    /// </summary>
    public void ResetVisualOffset()
    {
        visualOffset = Vector3.zero;
        hasVisualOffset = false;
        isDominoAnimating = false;
        transform.position = basePosition;
        
        // 同时销毁影子
        DestroyShadow();
    }

    /// <summary>
    /// 开始多米诺回弹动画
    /// </summary>
    public void StartDominoAnimation(float delay = 0f, float shadowDelay = 0f)
    {
        // 保存当前状态
        dominoStartOffset = visualOffset;
        dominoDelay = delay;
        dominoTimer = 0f;
        isDominoAnimating = true;
        hasVisualOffset = false; // 不再接受新的拖动偏移
        
        // 保存影子的起始位置（如果有影子）
        if (shadowGem != null)
        {
            shadowStartPos = shadowGem.transform.position - visualOffset;
            // 影子使用单独的延迟
            StartCoroutine(AnimateShadow(shadowDelay));
        }
    }

    /// <summary>
    /// 影子的独立动画（可以有不同的延迟）
    /// </summary>
    private System.Collections.IEnumerator AnimateShadow(float shadowDelay)
    {
        // 等待影子的延迟
        if (shadowDelay > 0)
        {
            yield return new WaitForSeconds(shadowDelay);
        }
        
        float shadowTimer = 0f;
        float shadowDuration = dominoDuration;
        
        while (shadowTimer < shadowDuration && shadowGem != null)
        {
            shadowTimer += Time.deltaTime;
            float progress = Mathf.Clamp01(shadowTimer / shadowDuration);
            float easedProgress = 1 - Mathf.Pow(1 - progress, 3);
            
            Vector3 shadowCurrentOffset = Vector3.Lerp(dominoStartOffset, Vector3.zero, easedProgress);
            shadowGem.transform.position = shadowStartPos + shadowCurrentOffset;
            
            yield return null;
        }
        
        // 影子动画完成，销毁影子
        if (shadowGem != null)
        {
            Destroy(shadowGem);
            shadowGem = null;
        }
    }

    /// <summary>
    /// 检查是否正在多米诺动画
    /// </summary>
    public bool IsDominoAnimating()
    {
        return isDominoAnimating;
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

