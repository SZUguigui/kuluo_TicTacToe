using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class InnovativeTicTacToe_v1_2 : MonoBehaviour
{
    // 游戏状态枚举
    public enum GameState { PlayerTurn, AITurn, AttackPhase, GameOver }

    // 棋盘状态
    private int[,] board = new int[3, 3]; // 0=空, 1=玩家, 2=AI
    public Button[] buttons;
    public Text gameText;
    private GameState currentState = GameState.PlayerTurn;

    // 攻击判定相关变量
    public GameObject attackPanel;
    public Image outerCircle;
    public Image innerCircle;
    private Vector2Int attackTarget;
    private float outerRadius;
    private float innerRadius;
    private float shrinkSpeed = 50f;
    private bool isAttacking = false;

    // 音效
    public AudioClip attackSuccessSound;
    public AudioClip attackFailSound;
    private AudioSource audioSource;

    // 添加材质引用
    public Material circleRingMaterial;

    // 添加胜场记录变量
    public Text playerWinsText;
    public Text aiWinsText;
    private int playerWins = 0;
    private int aiWins = 0;

    void Start()
    {
        InitializeBoard();
        UpdateButtonAppearance();
        UpdateWinsText();
        gameText.text = "Your Turn (X)";

        // 初始化音频源
        audioSource = gameObject.AddComponent<AudioSource>();

        // 隐藏攻击面板
        if (attackPanel != null)
            attackPanel.SetActive(false);
    }

    void InitializeBoard()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                board[i, j] = 0;
            }
        }
    }

    public void OnButtonClick(int index)
    {
        if (currentState != GameState.PlayerTurn && currentState != GameState.AttackPhase) return;

        int row = index / 3;
        int col = index % 3;

        // 处理攻击判定阶段
        if (currentState == GameState.AttackPhase)
        {
            // 只有在攻击面板激活时才处理攻击判定
            if (attackPanel != null && attackPanel.activeSelf)
            {
                HandleAttackResult();
            }
            return;
        }

        // 正常下棋阶段
        if (board[row, col] == 0)
        {
            // 玩家下棋
            board[row, col] = 1;
            UpdateButtonAppearance();

            if (CheckWin(1))
            {
                gameText.text = "You Win!";
                playerWins++;
                UpdateWinsText();
                currentState = GameState.GameOver;
                return;
            }

            if (CheckDraw())
            {
                gameText.text = "Draw!";
                currentState = GameState.GameOver;
                return;
            }

            // 切换到AI回合
            currentState = GameState.AITurn;
            gameText.text = "AI's Turn (O)";

            // AI移动
            StartCoroutine(AIMoveWithDelay());
        }
        else if (board[row, col] == 2) // 尝试攻击AI的棋子
        {
            StartAttackPhase(row, col);
        }
    }

    void StartAttackPhase(int row, int col)
    {
        attackTarget = new Vector2Int(row, col);
        currentState = GameState.AttackPhase;
        gameText.text = "Attack! Click to judge";

        if (attackPanel != null)
        {
            attackPanel.SetActive(true);

            RectTransform buttonRect = buttons[row * 3 + col].GetComponent<RectTransform>();
            attackPanel.GetComponent<RectTransform>().position = buttonRect.position;

            // 使用Shader控制圆环效果
            if (circleRingMaterial != null)
            {
                // 创建材质实例以避免共享材质问题
                outerCircle.material = new Material(circleRingMaterial);
                innerCircle.material = new Material(circleRingMaterial);

                // 设置初始半径
                outerCircle.material.SetFloat("_OuterRadius", 0.7f);
                outerCircle.material.SetFloat("_InnerRadius", 0.5f);
                innerCircle.material.SetFloat("_OuterRadius", 0.5f);
                innerCircle.material.SetFloat("_InnerRadius", 0.3f);
            }

            // 重置动画参数
            outerRadius = 0.7f;
            innerRadius = 0.5f;
            isAttacking = true;
        }
    }

    void Update()
    {
        if (isAttacking)
        {
            // 更新半径值 - 确保内外圈都缩小
            outerRadius -= shrinkSpeed * Time.deltaTime / 100f;
            innerRadius = innerRadius; // 内圈保持为外圈的一半

            // 更新Shader参数
            if (outerCircle.material != null)
            {
                outerCircle.material.SetFloat("_OuterRadius", outerRadius);
                outerCircle.material.SetFloat("_InnerRadius", outerRadius * 0.7f);
            }

            if (innerCircle.material != null)
            {
                innerCircle.material.SetFloat("_OuterRadius", innerRadius);
                innerCircle.material.SetFloat("_InnerRadius", innerRadius * 0.7f);
            }

            // 如果光圈缩小到很小，自动判定失败
            if (outerRadius <= 0.1f)
            {
                HandleAttackResult(false);
            }
        }
    }

    void HandleAttackResult()
    {
        // 计算两个光圈的半径差
        float radiusDifference = Mathf.Abs(outerRadius - innerRadius);
        float threshold = innerRadius * 0.5f;

        // 判定是否成功
        bool success = radiusDifference >= threshold;
        HandleAttackResult(success); // 调用带参数版本
    }

    void HandleAttackResult(bool success)
    {
        isAttacking = false;

        // 隐藏攻击面板
        if (attackPanel != null)
            attackPanel.SetActive(false);

        if (success)
        {
            // 攻击成功，占领格子
            board[attackTarget.x, attackTarget.y] = 1;
            UpdateButtonAppearance();

            // 播放成功音效
            if (attackSuccessSound != null)
                audioSource.PlayOneShot(attackSuccessSound);

            gameText.text = "Attack Success!";

            if (CheckWin(1))
            {
                gameText.text = "You Win!";
                playerWins++;
                UpdateWinsText();
                currentState = GameState.GameOver;
                return;
            }


            // 切换到AI回合
            currentState = GameState.AITurn;
            gameText.text = "AI's Turn (O)";

            // AI移动
            StartCoroutine(AIMoveWithDelay());
        }
        else
        {
            // 攻击失败
            // 播放失败音效
            if (attackFailSound != null)
                audioSource.PlayOneShot(attackFailSound);

            gameText.text = "Attack Failed! AI's Turn";

            // 切换到AI回合
            currentState = GameState.AITurn;

            // AI移动
            StartCoroutine(AIMoveWithDelay());
        }
    }

    IEnumerator AIMoveWithDelay()
    {
        // 添加一点延迟，让AI的移动看起来更自然
        yield return new WaitForSeconds(0.5f);
        MakeAIMove();
    }

    void MakeAIMove()
    {
        if (currentState != GameState.AITurn) return;

        Vector2Int? bestMove = FindBestMove();

        if (bestMove.HasValue)
        {
            Vector2Int selectedCell = bestMove.Value;
            board[selectedCell.x, selectedCell.y] = 2;
            UpdateButtonAppearance();

            if (CheckWin(2))
            {
                gameText.text = "AI Wins!";
                aiWins++;
                UpdateWinsText();
                currentState = GameState.GameOver;
                return;
            }

            if (CheckDraw())
            {
                gameText.text = "Draw!";
                currentState = GameState.GameOver;
                return;
            }

            // 切换回玩家回合
            currentState = GameState.PlayerTurn;
            gameText.text = "Your Turn (X)";
        }
        else
        {
            // 没有找到有效移动（应该是平局状态）
            gameText.text = "Draw!";
            currentState = GameState.GameOver;
        }
    }

    // AI智能决策算法
    Vector2Int? FindBestMove()
    {
        // 1. 检查AI是否可以立即获胜
        Vector2Int? winMove = FindWinningMove(2);
        if (winMove.HasValue)
            return winMove.Value;

        // 2. 阻止玩家立即获胜
        Vector2Int? blockMove = FindWinningMove(1);
        if (blockMove.HasValue)
            return blockMove.Value;

        // 3. 优先占据中心位置（如果可用）
        if (board[1, 1] == 0)
            return new Vector2Int(1, 1);

        // 4. 占据角落位置
        List<Vector2Int> corners = new List<Vector2Int>()
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, 2),
            new Vector2Int(2, 0),
            new Vector2Int(2, 2)
        };

        // 随机选择可用的角落
        List<Vector2Int> availableCorners = corners.Where(pos => board[pos.x, pos.y] == 0).ToList();
        if (availableCorners.Count > 0)
        {
            return availableCorners[Random.Range(0, availableCorners.Count)];
        }

        // 5. 占据边缘位置
        List<Vector2Int> edges = new List<Vector2Int>()
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, 2),
            new Vector2Int(2, 1)
        };

        // 随机选择可用的边缘
        List<Vector2Int> availableEdges = edges.Where(pos => board[pos.x, pos.y] == 0).ToList();
        if (availableEdges.Count > 0)
        {
            return availableEdges[Random.Range(0, availableEdges.Count)];
        }

        // 6. 没有可用位置（平局）
        return null;
    }

    // 寻找获胜位置（适用于任何玩家）
    Vector2Int? FindWinningMove(int player)
    {
        // 检查所有行
        for (int row = 0; row < 3; row++)
        {
            int count = 0;
            Vector2Int? emptyPos = null;

            for (int col = 0; col < 3; col++)
            {
                if (board[row, col] == player)
                    count++;
                else if (board[row, col] == 0)
                    emptyPos = new Vector2Int(row, col);
            }

            if (count == 2 && emptyPos.HasValue)
                return emptyPos.Value;
        }

        // 检查所有列
        for (int col = 0; col < 3; col++)
        {
            int count = 0;
            Vector2Int? emptyPos = null;

            for (int row = 0; row < 3; row++)
            {
                if (board[row, col] == player)
                    count++;
                else if (board[row, col] == 0)
                    emptyPos = new Vector2Int(row, col);
            }

            if (count == 2 && emptyPos.HasValue)
                return emptyPos.Value;
        }

        // 检查主对角线
        int diag1Count = 0;
        Vector2Int? diag1Empty = null;
        for (int i = 0; i < 3; i++)
        {
            if (board[i, i] == player)
                diag1Count++;
            else if (board[i, i] == 0)
                diag1Empty = new Vector2Int(i, i);
        }
        if (diag1Count == 2 && diag1Empty.HasValue)
            return diag1Empty.Value;

        // 检查副对角线
        int diag2Count = 0;
        Vector2Int? diag2Empty = null;
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 2 - i] == player)
                diag2Count++;
            else if (board[i, 2 - i] == 0)
                diag2Empty = new Vector2Int(i, 2 - i);
        }
        if (diag2Count == 2 && diag2Empty.HasValue)
            return diag2Empty.Value;

        return null;
    }

    bool CheckWin(int player)
    {
        // 检查行
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] == player && board[i, 1] == player && board[i, 2] == player)
                return true;
        }

        // 检查列
        for (int i = 0; i < 3; i++)
        {
            if (board[0, i] == player && board[1, i] == player && board[2, i] == player)
                return true;
        }

        // 检查对角线
        if (board[0, 0] == player && board[1, 1] == player && board[2, 2] == player)
            return true;

        if (board[0, 2] == player && board[1, 1] == player && board[2, 0] == player)
            return true;

        return false;
    }

    bool CheckDraw()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == 0)
                    return false;
            }
        }
        return true;
    }

    void UpdateButtonAppearance()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int index = i * 3 + j;

                if (board[i, j] == 1)
                    buttons[index].GetComponentInChildren<Text>().text = "X";
                else if (board[i, j] == 2)
                    buttons[index].GetComponentInChildren<Text>().text = "O";
                else
                    buttons[index].GetComponentInChildren<Text>().text = "";
            }
        }
    }

    // 更新胜场显示
    void UpdateWinsText()
    {
        if (playerWinsText != null)
            playerWinsText.text = "" + playerWins;

        if (aiWinsText != null)
            aiWinsText.text = "" + aiWins;
    }

    public void ResetGame()
    {
        InitializeBoard();
        UpdateButtonAppearance();
        gameText.text = "Your Turn (X)";
        currentState = GameState.PlayerTurn;

        // 确保攻击面板隐藏
        if (attackPanel != null)
            attackPanel.SetActive(false);

        isAttacking = false;
    }

    public void NewGame()
    {
        InitializeBoard();
        UpdateButtonAppearance();
        gameText.text = "Your Turn (X)";
        currentState = GameState.PlayerTurn;
        playerWins = 0;
        aiWins = 0;
        UpdateWinsText();

        // 确保攻击面板隐藏
        if (attackPanel != null)
            attackPanel.SetActive(false);

        isAttacking = false;
    }
}