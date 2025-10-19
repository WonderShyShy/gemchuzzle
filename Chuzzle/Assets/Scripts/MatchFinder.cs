using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 匹配检测系统 - 使用连通区域算法检测任意形状的匹配
/// </summary>
public class MatchFinder
{
    private BoardManager board;
    private int minMatchCount = 3; // 最少匹配数量

    public MatchFinder(BoardManager board)
    {
        this.board = board;
    }

    // ==================== 完整连通检测（游戏中使用） ====================

    /// <summary>
    /// 找出所有匹配组（使用Flood Fill算法，支持任意形状）
    /// </summary>
    public List<List<Gem>> FindAllMatchGroups()
    {
        HashSet<Gem> processedGems = new HashSet<Gem>();
        List<List<Gem>> matchGroups = new List<List<Gem>>();

        // 遍历整个棋盘
        for (int row = 0; row < board.Rows; row++)
        {
            for (int col = 0; col < board.Columns; col++)
            {
                Gem gem = board.GetGem(row, col);
                if (gem == null) continue;

                // 已经处理过了
                if (processedGems.Contains(gem)) continue;

                // 使用Flood Fill找出连通区域
                List<Gem> connected = FindConnectedGems(row, col);

                // 如果连通区域 >= 3，就是一个匹配组
                if (connected.Count >= minMatchCount)
                {
                    matchGroups.Add(connected);
                    Debug.Log($"发现匹配组：{connected.Count}个{gem.gemType}宝石");
                }

                // 标记所有连通的宝石为已处理
                foreach (var g in connected)
                {
                    processedGems.Add(g);
                }
            }
        }

        return matchGroups;
    }

    /// <summary>
    /// 使用Flood Fill算法找出指定位置的所有连通宝石
    /// </summary>
    public List<Gem> FindConnectedGems(int startRow, int startCol)
    {
        Gem startGem = board.GetGem(startRow, startCol);
        if (startGem == null) return new List<Gem>();

        GemType targetType = startGem.gemType;
        List<Gem> connected = new List<Gem>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
        Queue<Vector2Int> toCheck = new Queue<Vector2Int>();

        // 从起点开始
        Vector2Int startPos = new Vector2Int(startRow, startCol);
        toCheck.Enqueue(startPos);
        visited.Add(startPos);

        // Flood Fill算法
        while (toCheck.Count > 0)
        {
            Vector2Int current = toCheck.Dequeue();
            Gem currentGem = board.GetGem(current.x, current.y);
            
            if (currentGem != null)
            {
                connected.Add(currentGem);
            }

            // 检查四个方向（上下左右）
            Vector2Int[] directions = {
                new Vector2Int(-1, 0),  // 上
                new Vector2Int(1, 0),   // 下
                new Vector2Int(0, -1),  // 左
                new Vector2Int(0, 1)    // 右
            };

            foreach (var dir in directions)
            {
                int newRow = current.x + dir.x;
                int newCol = current.y + dir.y;
                Vector2Int newPos = new Vector2Int(newRow, newCol);

                // 边界检查
                if (newRow < 0 || newRow >= board.Rows ||
                    newCol < 0 || newCol >= board.Columns)
                    continue;

                // 已访问过
                if (visited.Contains(newPos))
                    continue;

                // 获取邻居宝石
                Gem neighborGem = board.GetGem(newRow, newCol);
                if (neighborGem == null)
                    continue;

                // 颜色相同才加入连通区域
                if (neighborGem.gemType == targetType)
                {
                    toCheck.Enqueue(newPos);
                    visited.Add(newPos);
                }
            }
        }

        return connected;
    }

    /// <summary>
    /// 检查指定位置是否有匹配
    /// </summary>
    public bool HasMatchAt(int row, int col)
    {
        var connected = FindConnectedGems(row, col);
        return connected.Count >= minMatchCount;
    }

    // ==================== 开局生成检测（快速规则） ====================

    /// <summary>
    /// 获取不会形成匹配的安全类型（用于开局生成）
    /// 使用快速规则：只检查横向和纵向的2个相邻
    /// </summary>
    public GemType GetSafeTypeForGeneration(int row, int col)
    {
        HashSet<GemType> forbidden = new HashSet<GemType>();

        // 规则1：检查左边2个
        if (col >= 2)
        {
            Gem left1 = board.GetGem(row, col - 1);
            Gem left2 = board.GetGem(row, col - 2);

            if (left1 != null && left2 != null && left1.gemType == left2.gemType)
            {
                forbidden.Add(left1.gemType);
            }
        }

        // 规则2：检查上边2个
        if (row >= 2)
        {
            Gem up1 = board.GetGem(row - 1, col);
            Gem up2 = board.GetGem(row - 2, col);

            if (up1 != null && up2 != null && up1.gemType == up2.gemType)
            {
                forbidden.Add(up1.gemType);
            }
        }

        // 规则3：检查L型（左1个+上1个相同）
        if (col >= 1 && row >= 1)
        {
            Gem left = board.GetGem(row, col - 1);
            Gem up = board.GetGem(row - 1, col);

            if (left != null && up != null && left.gemType == up.gemType)
            {
                GemType cornerType = left.gemType;

                // 检查是否会形成3个L型
                // 情况A：左边还有1个相同
                if (col >= 2)
                {
                    Gem left2 = board.GetGem(row, col - 2);
                    if (left2 != null && left2.gemType == cornerType)
                    {
                        forbidden.Add(cornerType);
                    }
                }

                // 情况B：上边还有1个相同
                if (row >= 2)
                {
                    Gem up2 = board.GetGem(row - 2, col);
                    if (up2 != null && up2.gemType == cornerType)
                    {
                        forbidden.Add(cornerType);
                    }
                }

                // 情况C：左上角有相同（形成2x2的一部分）
                if (col >= 2 && row >= 1)
                {
                    Gem leftUp = board.GetGem(row - 1, col - 2);
                    if (leftUp != null && leftUp.gemType == cornerType)
                    {
                        forbidden.Add(cornerType);
                    }
                }

                if (col >= 1 && row >= 2)
                {
                    Gem upLeft = board.GetGem(row - 2, col - 1);
                    if (upLeft != null && upLeft.gemType == cornerType)
                    {
                        forbidden.Add(cornerType);
                    }
                }
            }
        }

        // 从非禁用的颜色中选择
        List<GemType> allTypes = System.Enum.GetValues(typeof(GemType)).Cast<GemType>().ToList();
        List<GemType> available = allTypes.Except(forbidden).ToList();

        // 如果都被禁用（极端情况），返回随机颜色
        if (available.Count == 0)
        {
            Debug.LogWarning($"位置({row},{col})所有颜色都被禁用，返回随机颜色");
            available = allTypes;
        }

        return available[Random.Range(0, available.Count)];
    }

    /// <summary>
    /// 完整检测：判断放置某个类型后是否会形成匹配（准确但较慢）
    /// </summary>
    public bool WouldCreateMatch(int row, int col, GemType type)
    {
        // 临时保存原来的宝石
        Gem originalGem = board.GetGem(row, col);
        
        // 创建临时宝石来测试
        GameObject tempObj = new GameObject("TempGem");
        Gem tempGem = tempObj.AddComponent<Gem>();
        tempGem.gemType = type;
        tempGem.row = row;
        tempGem.column = col;

        // 临时替换
        board.SetGemForTest(row, col, tempGem);

        // 检测连通区域
        List<Gem> connected = FindConnectedGems(row, col);
        bool wouldMatch = connected.Count >= minMatchCount;

        // 恢复原状
        board.SetGemForTest(row, col, originalGem);
        Object.Destroy(tempObj);

        return wouldMatch;
    }

    // ==================== 调试辅助 ====================

    /// <summary>
    /// 可视化显示所有匹配
    /// </summary>
    public void DebugShowAllMatches()
    {
        var matchGroups = FindAllMatchGroups();
        
        if (matchGroups.Count == 0)
        {
            Debug.Log("没有发现匹配");
        }
        else
        {
            Debug.Log($"发现 {matchGroups.Count} 个匹配组：");
            for (int i = 0; i < matchGroups.Count; i++)
            {
                var group = matchGroups[i];
                string positions = string.Join(", ", group.Select(g => $"({g.row},{g.column})"));
                Debug.Log($"  匹配组{i + 1}: {group.Count}个{group[0].gemType} - 位置: {positions}");
            }
        }
    }
}


