using UnityEngine;

/// <summary>
/// 输入控制器 - 处理玩家的拖动输入
/// </summary>
public class InputController : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private Camera mainCamera;

    [Header("拖动设置")]
    [SerializeField] private float dragThreshold = 0.2f; // 拖动阈值（避免误触）
    [SerializeField] private float moveThreshold = 0.6f; // 移动阈值（需要拖动超过半格才移动）

    // 拖动状态
    private bool isDragging = false;
    private Vector3 startMousePos;
    private Vector3 currentMousePos;
    private Gem selectedGem;
    private DragDirection dragDirection = DragDirection.None;
    
    // 拖动偏移量（用于视觉反馈）
    private int currentOffset = 0; // 当前已经移动了多少格

    void Start()
    {
        // 自动查找摄像机
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        // 自动查找BoardManager
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        // 开始拖动
        if (Input.GetMouseButtonDown(0) && !isDragging)
        {
            StartDrag();
        }

        // 拖动中
        if (Input.GetMouseButton(0) && isDragging)
        {
            UpdateDrag();
        }

        // 结束拖动
        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDrag();
        }
    }

    /// <summary>
    /// 开始拖动
    /// </summary>
    private void StartDrag()
    {
        // 检查是否有宝石在移动
        if (boardManager.IsAnyGemMoving())
        {
            return;
        }

        // 将屏幕坐标转换为世界坐标
        startMousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        startMousePos.z = 0;

        // 射线检测获取点击的宝石
        Gem hitGem = GetGemAtPosition(startMousePos);
        
        if (hitGem != null)
        {
            selectedGem = hitGem;
            isDragging = true;
            dragDirection = DragDirection.None;
            currentOffset = 0;
            
            Debug.Log($"开始拖动宝石: {hitGem.name} at ({hitGem.row}, {hitGem.column})");
        }
    }

    /// <summary>
    /// 更新拖动
    /// </summary>
    private void UpdateDrag()
    {
        if (selectedGem == null) return;

        // 获取当前鼠标位置
        currentMousePos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        currentMousePos.z = 0;

        // 计算拖动向量
        Vector3 dragVector = currentMousePos - startMousePos;

        // 如果还没确定方向，先判断方向
        if (dragDirection == DragDirection.None)
        {
            if (dragVector.magnitude > dragThreshold)
            {
                // 判断是水平还是垂直
                if (Mathf.Abs(dragVector.x) > Mathf.Abs(dragVector.y))
                {
                    // 水平拖动
                    dragDirection = dragVector.x > 0 ? DragDirection.Right : DragDirection.Left;
                    Debug.Log($"拖动方向: {dragDirection}, 移动第 {selectedGem.row} 行");
                }
                else
                {
                    // 垂直拖动
                    dragDirection = dragVector.y > 0 ? DragDirection.Up : DragDirection.Down;
                    Debug.Log($"拖动方向: {dragDirection}, 移动第 {selectedGem.column} 列");
                }
            }
        }
        else
        {
            // 已经确定方向，检查是否需要移动
            float dragDistance = 0;
            
            switch (dragDirection)
            {
                case DragDirection.Left:
                case DragDirection.Right:
                    dragDistance = dragVector.x;
                    break;
                case DragDirection.Up:
                case DragDirection.Down:
                    dragDistance = dragVector.y;
                    break;
            }

            // 计算应该移动多少格
            int targetOffset = Mathf.FloorToInt(Mathf.Abs(dragDistance) / boardManager.GemSpacing);
            
            // 如果拖动距离超过阈值，执行移动
            if (targetOffset > currentOffset && Mathf.Abs(dragDistance) > moveThreshold)
            {
                PerformMove();
                currentOffset = targetOffset;
                
                // 更新起始位置，为下一次移动做准备
                startMousePos = currentMousePos;
            }
        }
    }

    /// <summary>
    /// 结束拖动
    /// </summary>
    private void EndDrag()
    {
        Debug.Log($"结束拖动，方向: {dragDirection}, 移动了 {currentOffset} 格");
        
        isDragging = false;
        selectedGem = null;
        dragDirection = DragDirection.None;
        currentOffset = 0;
    }

    /// <summary>
    /// 执行移动
    /// </summary>
    private void PerformMove()
    {
        if (selectedGem == null) return;

        switch (dragDirection)
        {
            case DragDirection.Left:
                boardManager.ShiftRowLeft(selectedGem.row);
                Debug.Log($"向左移动第 {selectedGem.row} 行");
                break;
            case DragDirection.Right:
                boardManager.ShiftRowRight(selectedGem.row);
                Debug.Log($"向右移动第 {selectedGem.row} 行");
                break;
            case DragDirection.Up:
                boardManager.ShiftColumnUp(selectedGem.column);
                Debug.Log($"向上移动第 {selectedGem.column} 列");
                break;
            case DragDirection.Down:
                boardManager.ShiftColumnDown(selectedGem.column);
                Debug.Log($"向下移动第 {selectedGem.column} 列");
                break;
        }
    }

    /// <summary>
    /// 通过射线检测获取指定位置的宝石
    /// </summary>
    private Gem GetGemAtPosition(Vector3 worldPosition)
    {
        RaycastHit2D hit = Physics2D.Raycast(worldPosition, Vector2.zero);
        
        if (hit.collider != null)
        {
            Gem gem = hit.collider.GetComponent<Gem>();
            return gem;
        }
        
        return null;
    }

    /// <summary>
    /// 可视化调试信息
    /// </summary>
    void OnDrawGizmos()
    {
        if (isDragging && selectedGem != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(selectedGem.transform.position, 0.3f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawLine(startMousePos, currentMousePos);
        }
    }
}

/// <summary>
/// 拖动方向枚举
/// </summary>
public enum DragDirection
{
    None,
    Left,
    Right,
    Up,
    Down
}

