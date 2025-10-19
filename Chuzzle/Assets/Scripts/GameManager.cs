using UnityEngine;

/// <summary>
/// 游戏管理器 - 管理游戏整体状态和流程
/// </summary>
public class GameManager : MonoBehaviour
{
    [Header("引用")]
    [SerializeField] private BoardManager boardManager;
    [SerializeField] private InputController inputController;

    [Header("游戏状态")]
    [SerializeField] private GameState currentState = GameState.Idle;

    void Start()
    {
        // 自动查找组件
        if (boardManager == null)
        {
            boardManager = FindObjectOfType<BoardManager>();
        }

        if (inputController == null)
        {
            inputController = FindObjectOfType<InputController>();
        }

        // 初始化游戏
        currentState = GameState.Playing;
        Debug.Log("游戏开始！");
    }

    /// <summary>
    /// 获取当前游戏状态
    /// </summary>
    public GameState GetCurrentState()
    {
        return currentState;
    }

    /// <summary>
    /// 设置游戏状态
    /// </summary>
    public void SetGameState(GameState newState)
    {
        currentState = newState;
        Debug.Log($"游戏状态切换: {currentState} -> {newState}");
    }

    /// <summary>
    /// 检查是否可以进行输入
    /// </summary>
    public bool CanInput()
    {
        return currentState == GameState.Playing;
    }
}

/// <summary>
/// 游戏状态枚举
/// </summary>
public enum GameState
{
    Idle,          // 空闲
    Playing,       // 游戏中（可以输入）
    Moving,        // 宝石移动中
    Matching,      // 匹配检测中
    Eliminating,   // 消除中
    Refilling,     // 填充中
    GameOver       // 游戏结束
}


