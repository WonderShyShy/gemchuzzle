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

