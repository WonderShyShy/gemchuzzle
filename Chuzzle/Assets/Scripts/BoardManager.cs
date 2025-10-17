using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 棋盘管理器 - 负责生成和管理6x6的游戏棋盘
/// </summary>
public class BoardManager : MonoBehaviour
{
    [Header("棋盘设置")]
    [SerializeField] private int rows = 6; // 行数
    [SerializeField] private int columns = 6; // 列数
    [SerializeField] private float gemSpacing = 1.2f; // 宝石间距
    [SerializeField] private float gemScale = 0.15f; // 宝石缩放比例
    [SerializeField] private Vector2 boardOffset = new Vector2(-3f, -3f); // 棋盘偏移（用于居中）

    // 公开属性，供InputController访问
    public float GemSpacing => gemSpacing;

    [Header("宝石预制体")]
    [SerializeField] private GameObject bluePrefab;
    [SerializeField] private GameObject greenPrefab;
    [SerializeField] private GameObject orangePrefab;
    [SerializeField] private GameObject pinkPrefab;
    [SerializeField] private GameObject redPrefab;
    [SerializeField] private GameObject whitePrefab;
    [SerializeField] private GameObject yellowPrefab;

    // 宝石数组 - 存储所有宝石的引用
    private Gem[,] gems;

    // 宝石预制体字典
    private Dictionary<GemType, GameObject> gemPrefabs;

    void Start()
    {
        InitializePrefabDictionary();
        GenerateBoard();
    }

    /// <summary>
    /// 初始化预制体字典
    /// </summary>
    private void InitializePrefabDictionary()
    {
        gemPrefabs = new Dictionary<GemType, GameObject>
        {
            { GemType.Blue, bluePrefab },
            { GemType.Green, greenPrefab },
            { GemType.Orange, orangePrefab },
            { GemType.Pink, pinkPrefab },
            { GemType.Red, redPrefab },
            { GemType.White, whitePrefab },
            { GemType.Yellow, yellowPrefab }
        };
    }

    /// <summary>
    /// 生成6x6棋盘
    /// </summary>
    private void GenerateBoard()
    {
        gems = new Gem[rows, columns];

        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                // 随机选择一个宝石类型
                GemType gemType = GetRandomGemType();

                // 计算宝石的世界坐标
                Vector3 position = GetWorldPosition(row, col);

                // 实例化宝石
                GameObject gemObject = Instantiate(GetPrefabForType(gemType), position, Quaternion.identity);
                gemObject.transform.parent = this.transform; // 设置为棋盘的子对象
                gemObject.transform.localScale = Vector3.one * gemScale; // 设置宝石缩放
                gemObject.name = $"Gem_{row}_{col}_{gemType}"; // 设置名称方便调试

                // 添加Gem组件（如果预制体上没有）
                Gem gem = gemObject.GetComponent<Gem>();
                if (gem == null)
                {
                    gem = gemObject.AddComponent<Gem>();
                }

                // 设置宝石属性
                gem.gemType = gemType;
                gem.SetPosition(row, col, position);

                // 存储到数组中
                gems[row, col] = gem;
            }
        }

        Debug.Log($"棋盘生成完成！{rows}x{columns} 共 {rows * columns} 个宝石");
    }

    /// <summary>
    /// 获取随机宝石类型
    /// </summary>
    private GemType GetRandomGemType()
    {
        // 随机返回7种宝石类型之一
        int randomIndex = Random.Range(0, 7);
        return (GemType)randomIndex;
    }

    /// <summary>
    /// 根据类型获取对应的预制体
    /// </summary>
    private GameObject GetPrefabForType(GemType type)
    {
        if (gemPrefabs.ContainsKey(type))
        {
            return gemPrefabs[type];
        }

        Debug.LogError($"找不到类型为 {type} 的预制体！");
        return bluePrefab; // 默认返回蓝色
    }

    /// <summary>
    /// 根据行列计算世界坐标
    /// </summary>
    private Vector3 GetWorldPosition(int row, int col)
    {
        float x = boardOffset.x + col * gemSpacing;
        float y = boardOffset.y + row * gemSpacing;
        return new Vector3(x, y, 0);
    }

    /// <summary>
    /// 获取指定位置的宝石
    /// </summary>
    public Gem GetGem(int row, int col)
    {
        if (row >= 0 && row < rows && col >= 0 && col < columns)
        {
            return gems[row, col];
        }
        return null;
    }

    /// <summary>
    /// 检查是否有任何宝石正在移动或有视觉偏移
    /// </summary>
    public bool IsAnyGemMoving()
    {
        for (int row = 0; row < rows; row++)
        {
            for (int col = 0; col < columns; col++)
            {
                if (gems[row, col] != null && (gems[row, col].IsMoving() || gems[row, col].HasVisualOffset()))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 应用整行视觉偏移（带影子宝石）
    /// </summary>
    public void ApplyRowVisualOffset(int row, float offsetX)
    {
        if (row < 0 || row >= rows) return;

        float rowWidth = columns * gemSpacing;

        for (int col = 0; col < columns; col++)
        {
            Gem gem = gems[row, col];
            if (gem == null) continue;

            Vector3 basePos = GetWorldPosition(row, col);
            Vector3 offset = new Vector3(offsetX, 0, 0);
            
            // 原宝石应用偏移
            gem.UpdateBasePosition(basePos);
            gem.ApplyVisualOffset(offset);
            
            // 计算当前显示位置
            Vector3 displayPos = basePos + offset;
            
            // 一旦开始拖动，立即在另一边创建影子（循环带效果）
            if (offsetX > 0)
            {
                // 向右拖动：在左边创建影子
                Vector3 shadowPos = displayPos - new Vector3(rowWidth, 0, 0);
                gem.CreateOrUpdateShadow(shadowPos);
            }
            else if (offsetX < 0)
            {
                // 向左拖动：在右边创建影子
                Vector3 shadowPos = displayPos + new Vector3(rowWidth, 0, 0);
                gem.CreateOrUpdateShadow(shadowPos);
            }
        }
    }

    /// <summary>
    /// 应用整列视觉偏移（带影子宝石）
    /// </summary>
    public void ApplyColumnVisualOffset(int col, float offsetY)
    {
        if (col < 0 || col >= columns) return;

        float columnHeight = rows * gemSpacing;

        for (int row = 0; row < rows; row++)
        {
            Gem gem = gems[row, col];
            if (gem == null) continue;

            Vector3 basePos = GetWorldPosition(row, col);
            Vector3 offset = new Vector3(0, offsetY, 0);
            
            // 原宝石应用偏移
            gem.UpdateBasePosition(basePos);
            gem.ApplyVisualOffset(offset);
            
            // 计算当前显示位置
            Vector3 displayPos = basePos + offset;
            
            // 一旦开始拖动，立即在另一边创建影子（循环带效果）
            if (offsetY > 0)
            {
                // 向上拖动：在下边创建影子
                Vector3 shadowPos = displayPos - new Vector3(0, columnHeight, 0);
                gem.CreateOrUpdateShadow(shadowPos);
            }
            else if (offsetY < 0)
            {
                // 向下拖动：在上边创建影子
                Vector3 shadowPos = displayPos + new Vector3(0, columnHeight, 0);
                gem.CreateOrUpdateShadow(shadowPos);
            }
        }
    }

    /// <summary>
    /// 重置整行的视觉偏移
    /// </summary>
    public void ResetRowVisualOffset(int row)
    {
        if (row < 0 || row >= rows) return;

        for (int col = 0; col < columns; col++)
        {
            if (gems[row, col] != null)
            {
                gems[row, col].ResetVisualOffset();
            }
        }
    }

    /// <summary>
    /// 重置整列的视觉偏移
    /// </summary>
    public void ResetColumnVisualOffset(int col)
    {
        if (col < 0 || col >= columns) return;

        for (int row = 0; row < rows; row++)
        {
            if (gems[row, col] != null)
            {
                gems[row, col].ResetVisualOffset();
            }
        }
    }

    /// <summary>
    /// 整行向左移动（循环）
    /// </summary>
    public void ShiftRowLeft(int row)
    {
        if (row < 0 || row >= rows) return;

        // 1. 保存最左边的宝石
        Gem firstGem = gems[row, 0];
        Vector3 targetPos = GetWorldPosition(row, columns - 1);

        // 2. 其他宝石向左移动一格
        for (int col = 0; col < columns - 1; col++)
        {
            gems[row, col] = gems[row, col + 1];
            gems[row, col].MoveTo(row, col, GetWorldPosition(row, col));
        }

        // 3. 最左边的宝石循环到最右边
        gems[row, columns - 1] = firstGem;
        firstGem.MoveTo(row, columns - 1, targetPos);
    }

    /// <summary>
    /// 整行向右移动（循环）
    /// </summary>
    public void ShiftRowRight(int row)
    {
        if (row < 0 || row >= rows) return;

        // 1. 保存最右边的宝石
        Gem lastGem = gems[row, columns - 1];
        Vector3 targetPos = GetWorldPosition(row, 0);

        // 2. 其他宝石向右移动一格
        for (int col = columns - 1; col > 0; col--)
        {
            gems[row, col] = gems[row, col - 1];
            gems[row, col].MoveTo(row, col, GetWorldPosition(row, col));
        }

        // 3. 最右边的宝石循环到最左边
        gems[row, 0] = lastGem;
        lastGem.MoveTo(row, 0, targetPos);
    }

    /// <summary>
    /// 整列向上移动（循环）
    /// </summary>
    public void ShiftColumnUp(int col)
    {
        if (col < 0 || col >= columns) return;

        // 1. 保存最上边的宝石
        Gem topGem = gems[rows - 1, col];
        Vector3 targetPos = GetWorldPosition(0, col);

        // 2. 其他宝石向上移动一格
        for (int row = rows - 1; row > 0; row--)
        {
            gems[row, col] = gems[row - 1, col];
            gems[row, col].MoveTo(row, col, GetWorldPosition(row, col));
        }

        // 3. 最上边的宝石循环到最下边
        gems[0, col] = topGem;
        topGem.MoveTo(0, col, targetPos);
    }

    /// <summary>
    /// 整列向下移动（循环）
    /// </summary>
    public void ShiftColumnDown(int col)
    {
        if (col < 0 || col >= columns) return;

        // 1. 保存最下边的宝石
        Gem bottomGem = gems[0, col];
        Vector3 targetPos = GetWorldPosition(rows - 1, col);

        // 2. 其他宝石向下移动一格
        for (int row = 0; row < rows - 1; row++)
        {
            gems[row, col] = gems[row + 1, col];
            gems[row, col].MoveTo(row, col, GetWorldPosition(row, col));
        }

        // 3. 最下边的宝石循环到最上边
        gems[rows - 1, col] = bottomGem;
        bottomGem.MoveTo(rows - 1, col, targetPos);
    }

    /// <summary>
    /// 在编辑器中绘制网格辅助线
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;

        // 绘制网格
        for (int row = 0; row <= rows; row++)
        {
            Vector3 start = GetWorldPosition(row, 0) - new Vector3(gemSpacing * 0.5f, gemSpacing * 0.5f, 0);
            Vector3 end = GetWorldPosition(row, columns - 1) + new Vector3(gemSpacing * 0.5f, -gemSpacing * 0.5f, 0);
            Gizmos.DrawLine(start, end);
        }

        for (int col = 0; col <= columns; col++)
        {
            Vector3 start = GetWorldPosition(0, col) - new Vector3(gemSpacing * 0.5f, gemSpacing * 0.5f, 0);
            Vector3 end = GetWorldPosition(rows - 1, col) + new Vector3(-gemSpacing * 0.5f, gemSpacing * 0.5f, 0);
            Gizmos.DrawLine(start, end);
        }
    }
}

