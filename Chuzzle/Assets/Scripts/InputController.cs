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
    
    // 拖动偏移量
    private float totalDragOffset = 0f; // 总拖动偏移量（像素/单位）
    private int confirmedMoves = 0; // 已确认的移动次数

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
            totalDragOffset = 0f;
            confirmedMoves = 0;
            
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
            // 已经确定方向，实时更新视觉偏移
            float dragDistance = 0;
            
            switch (dragDirection)
            {
                case DragDirection.Left:
                    dragDistance = -dragVector.x; // 左拖是负的，转为正的偏移量
                    totalDragOffset = dragDistance;
                    boardManager.ApplyRowVisualOffset(selectedGem.row, -dragDistance); // 应用负偏移
                    break;
                case DragDirection.Right:
                    dragDistance = dragVector.x;
                    totalDragOffset = dragDistance;
                    boardManager.ApplyRowVisualOffset(selectedGem.row, dragDistance);
                    break;
                case DragDirection.Up:
                    dragDistance = dragVector.y;
                    totalDragOffset = dragDistance;
                    boardManager.ApplyColumnVisualOffset(selectedGem.column, dragDistance);
                    break;
                case DragDirection.Down:
                    dragDistance = -dragVector.y;
                    totalDragOffset = dragDistance;
                    boardManager.ApplyColumnVisualOffset(selectedGem.column, -dragDistance);
                    break;
            }
        }
    }

    /// <summary>
    /// 结束拖动
    /// </summary>
    private void EndDrag()
    {
        if (selectedGem == null)
        {
            isDragging = false;
            return;
        }

        // 计算应该移动多少格
        int movesToConfirm = Mathf.RoundToInt(totalDragOffset / boardManager.GemSpacing);
        
        Debug.Log($"结束拖动，总偏移: {totalDragOffset:F2}, 应该移动: {movesToConfirm} 格");

        // 不管是否确认移动，都先用多米诺动画回到基础位置
        // 然后再决定是否执行逻辑移动
        if (movesToConfirm > 0)
        {
            // 拖动距离足够，先多米诺动画，然后确认移动
            Debug.Log($"拖动距离足够，先多米诺回弹，然后移动 {movesToConfirm} 格");
            DominoBackAnimation();
            
            // 等多米诺动画完成后再执行逻辑移动
            StartCoroutine(PerformMoveAfterDomino(movesToConfirm));
        }
        else
        {
            // 拖动距离不够，只做多米诺回弹
            Debug.Log("拖动距离不足，多米诺回弹");
            DominoBackAnimation();
        }
        
        isDragging = false;
        selectedGem = null;
        dragDirection = DragDirection.None;
        totalDragOffset = 0f;
        confirmedMoves = 0;
    }

    /// <summary>
    /// 重置视觉偏移（立即）
    /// </summary>
    private void ResetVisualOffset()
    {
        if (selectedGem == null) return;

        switch (dragDirection)
        {
            case DragDirection.Left:
            case DragDirection.Right:
                boardManager.ResetRowVisualOffset(selectedGem.row);
                break;
            case DragDirection.Up:
            case DragDirection.Down:
                boardManager.ResetColumnVisualOffset(selectedGem.column);
                break;
        }
    }

    /// <summary>
    /// 多米诺回弹动画
    /// </summary>
    private void DominoBackAnimation()
    {
        if (selectedGem == null) return;

        switch (dragDirection)
        {
            case DragDirection.Left:
                boardManager.DominoBackRow(selectedGem.row, false); // 向左拖，从右边开始回弹
                Debug.Log("多米诺回弹：整行从右到左");
                break;
            case DragDirection.Right:
                boardManager.DominoBackRow(selectedGem.row, true); // 向右拖，从左边开始回弹
                Debug.Log("多米诺回弹：整行从左到右");
                break;
            case DragDirection.Up:
                boardManager.DominoBackColumn(selectedGem.column, true); // 向上拖，从下边开始回弹
                Debug.Log("多米诺回弹：整列从下到上");
                break;
            case DragDirection.Down:
                boardManager.DominoBackColumn(selectedGem.column, false); // 向下拖，从上边开始回弹
                Debug.Log("多米诺回弹：整列从上到下");
                break;
        }
    }

    /// <summary>
    /// 等待多米诺动画完成后执行移动
    /// </summary>
    private System.Collections.IEnumerator PerformMoveAfterDomino(int movesToConfirm)
    {
        // 等待多米诺动画完成
        // 总时长 = 最后一个宝石的延迟 + 动画持续时间
        float totalDuration = (boardManager.Columns - 1) * 0.05f + 0.2f + 0.1f; // 多加0.1秒缓冲
        yield return new WaitForSeconds(totalDuration);

        // 现在执行逻辑移动
        for (int i = 0; i < movesToConfirm; i++)
        {
            PerformMove();
        }
    }

    /// <summary>
    /// 执行移动（确认移动，更新逻辑位置）
    /// </summary>
    private void PerformMove()
    {
        // 执行实际的逻辑移动
        switch (dragDirection)
        {
            case DragDirection.Left:
                boardManager.ShiftRowLeft(selectedGem.row);
                Debug.Log($"确认向左移动第 {selectedGem.row} 行");
                break;
            case DragDirection.Right:
                boardManager.ShiftRowRight(selectedGem.row);
                Debug.Log($"确认向右移动第 {selectedGem.row} 行");
                break;
            case DragDirection.Up:
                boardManager.ShiftColumnUp(selectedGem.column);
                Debug.Log($"确认向上移动第 {selectedGem.column} 列");
                break;
            case DragDirection.Down:
                boardManager.ShiftColumnDown(selectedGem.column);
                Debug.Log($"确认向下移动第 {selectedGem.column} 列");
                break;
        }

        confirmedMoves++;
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

