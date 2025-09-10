using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CreativeTicTacToe_v1_1 : MonoBehaviour
{
    // 棋子类型枚举
    public enum PieceType { None, Guard, Heavy, Archer }

    // 棋盘状态
    private PieceType[,] board = new PieceType[3, 3];
    private int[,] boardOwners = new int[3, 3]; // 0=空, 1=玩家, 2=AI

    public Button[] buttons;
    public Text gameText;
    private bool gameOver;
    private int playerTurn = 1; // 1 for player, 2 for AI

    // 棋子计数
    public Text playerGuardText;
    public Text playerHeavyText;
    public Text playerArcherText;
    public Text aiGuardText;
    public Text aiHeavyText;
    public Text aiArcherText;

    private int playerGuards = 3;
    private int playerHeavies = 3;
    private int playerArchers = 3;

    private int aiGuards = 3;
    private int aiHeavies = 3;
    private int aiArchers = 3;

    // 颜色定义
    public Color guardColor = Color.blue;
    public Color heavyColor = Color.red;
    public Color archerColor = Color.green;
    public Color emptyColor = Color.white;
    public Color AIColor = Color.red;
    public Color PlayerColor = Color.blue;

    // 当前选择的棋子类型
    private PieceType selectedPieceType = PieceType.None;

    // 音效相关变量
    public AudioClip winSound;
    public AudioClip loseSound;
    private AudioSource audioSource;

    // 胜场记录
    private int playerWins = 0;
    private int aiWins = 0;
    public Text playerWinsText;
    public Text aiWinsText;

    void Start()
    {
        InitializeBoard();
        UpdateButtonAppearance();
        UpdatePieceCounts();
        UpdateWinsText(); // 初始化胜场显示
        gameText.text = "Your Turn - Select a Piece";

        // 初始化音频源
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void InitializeBoard()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                board[i, j] = PieceType.None;
                boardOwners[i, j] = 0;
            }
        }
        gameOver = false;

    }

    // 更新胜场显示
    void UpdateWinsText()
    {
        if (playerWinsText != null)
            playerWinsText.text = ""+playerWins;

        if (aiWinsText != null)
            aiWinsText.text = ""+aiWins;
    }

    // 玩家选择棋子类型
    public void SelectGuard()
    {
        if (playerTurn != 1 || gameOver || playerGuards <= 0) return;
        selectedPieceType = PieceType.Guard;
        gameText.text = "Your Turn - Place Guard";
    }

    public void SelectHeavy()
    {
        if (playerTurn != 1 || gameOver || playerHeavies <= 0) return;
        selectedPieceType = PieceType.Heavy;
        gameText.text = "Your Turn - Place Heavy";
    }

    public void SelectArcher()
    {
        if (playerTurn != 1 || gameOver || playerArchers <= 0) return;
        selectedPieceType = PieceType.Archer;
        gameText.text = "Your Turn - Place Archer";
    }

    public void OnButtonClick(int index)
    {
        if (gameOver || playerTurn != 1 || selectedPieceType == PieceType.None) return;

        int row = index / 3;
        int col = index % 3;

        // 检查格子是否已被占据
        if (boardOwners[row, col] != 0)
        {
            // 检查是否可以攻击（克制关系）
            if (CanAttack(selectedPieceType, board[row, col]))
            {
                // 攻击成功，占领格子
                board[row, col] = selectedPieceType;
                boardOwners[row, col] = 1; // 玩家占领
                UsePlayerPiece(selectedPieceType);
                UpdateButtonAppearance();

                if (CheckWin(1))
                {
                    gameText.text = "You Win!";
                    playerWins++; // 增加玩家胜场
                    UpdateWinsText(); // 更新胜场显示

                    // 播放胜利音效
                    if (winSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(winSound);
                    }

                    gameOver = true;
                    return;
                }

                // 切换到AI回合
                playerTurn = 2;
                selectedPieceType = PieceType.None;
                gameText.text = "AI's Turn";

                // AI移动
                StartCoroutine(AIMoveWithDelay());
            }
            else
            {
                gameText.text = "Cannot attack this piece! Select another piece.";
            }
            return;
        }

        // 如果是空位，直接放置棋子
        board[row, col] = selectedPieceType;
        boardOwners[row, col] = 1; // 玩家占领
        UsePlayerPiece(selectedPieceType);
        UpdateButtonAppearance();

        if (CheckWin(1))
        {
            gameText.text = "You Win!";
            playerWins++; // 增加玩家胜场
            UpdateWinsText(); // 更新胜场显示
            // 播放胜利音效
            if (winSound != null && audioSource != null)
            {
                audioSource.PlayOneShot(winSound);
            }
            gameOver = true;
            return;
        }

        if (CheckDraw())
        {
            gameText.text = "Draw!";
            gameOver = true;
            return;
        }

        // 切换到AI回合
        playerTurn = 2;
        selectedPieceType = PieceType.None;
        gameText.text = "AI's Turn";

        // AI移动
        StartCoroutine(AIMoveWithDelay());
    }

    IEnumerator AIMoveWithDelay()
    {
        // 添加一点延迟，让AI的移动看起来更自然
        yield return new WaitForSeconds(0.5f);
        MakeAIMove();
    }

    void MakeAIMove()
    {
        if (gameOver || playerTurn != 2) return;

        // AI策略：优先尝试连线，然后随机选择，最后才攻击
        Vector3Int bestMove = FindBestMove();

        if (bestMove.x != -1)
        {
            int row = bestMove.x;
            int col = bestMove.y;
            PieceType aiPieceType = bestMove.z == 0 ? PieceType.Guard :
                                   (bestMove.z == 1 ? PieceType.Heavy : PieceType.Archer);

            // 检查是否有该类型的棋子
            if ((aiPieceType == PieceType.Guard && aiGuards > 0) ||
                (aiPieceType == PieceType.Heavy && aiHeavies > 0) ||
                (aiPieceType == PieceType.Archer && aiArchers > 0))
            {
                // 如果是攻击
                if (boardOwners[row, col] != 0)
                {
                    board[row, col] = aiPieceType;
                    boardOwners[row, col] = 2; // AI占领
                    UseAIPiece(aiPieceType);
                }
                else
                {
                    // 如果是放置
                    board[row, col] = aiPieceType;
                    boardOwners[row, col] = 2; // AI占领
                    UseAIPiece(aiPieceType);
                }

                UpdateButtonAppearance();

                if (CheckWin(2))
                {
                    gameText.text = "AI Wins!";
                    aiWins++; // 增加AI胜场
                    UpdateWinsText(); // 更新胜场显示

                    // 播放失败音效
                    if (loseSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(loseSound);
                    }

                    gameOver = true;
                    return;
                }

                if (CheckDraw())
                {
                    gameText.text = "Draw!";
                    gameOver = true;
                    return;
                }
            }
        }

        // 切换回玩家回合
        playerTurn = 1;
        gameText.text = "Your Turn - Select a Piece";
    }

    // 检查克制关系
    bool CanAttack(PieceType attacker, PieceType defender)
    {
        // 近卫克制重装
        if (attacker == PieceType.Guard && defender == PieceType.Heavy)
            return true;

        // 重装克制弓箭手
        if (attacker == PieceType.Heavy && defender == PieceType.Archer)
            return true;

        // 弓箭手克制近卫
        if (attacker == PieceType.Archer && defender == PieceType.Guard)
            return true;

        return false;
    }

    // AI找到最佳移动
    // AI找到最佳移动
    Vector3Int FindBestMove()
    {
        // 1. 优先尝试完成连线
        for (int type = 0; type < 3; type++)
        {
            PieceType pieceType = type == 0 ? PieceType.Guard :
                                 (type == 1 ? PieceType.Heavy : PieceType.Archer);

            // 检查是否有该类型的棋子
            if ((pieceType == PieceType.Guard && aiGuards <= 0) ||
                (pieceType == PieceType.Heavy && aiHeavies <= 0) ||
                (pieceType == PieceType.Archer && aiArchers <= 0))
                continue;

            // 检查所有空位
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (boardOwners[i, j] == 0)
                    {
                        // 模拟放置棋子
                        boardOwners[i, j] = 2;

                        // 检查是否会赢
                        if (CheckWin(2))
                        {
                            boardOwners[i, j] = 0; // 恢复
                            return new Vector3Int(i, j, type);
                        }

                        boardOwners[i, j] = 0; // 恢复
                    }
                }
            }
        }

        // 2. 随机选择一个空位和棋子类型
        List<Vector2Int> emptyCells = new List<Vector2Int>();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (boardOwners[i, j] == 0)
                {
                    emptyCells.Add(new Vector2Int(i, j));
                }
            }
        }

        if (emptyCells.Count > 0)
        {
            Vector2Int randomCell = emptyCells[Random.Range(0, emptyCells.Count)];

            // 随机选择一个可用的棋子类型
            List<int> availableTypes = new List<int>();
            if (aiGuards > 0) availableTypes.Add(0);
            if (aiHeavies > 0) availableTypes.Add(1);
            if (aiArchers > 0) availableTypes.Add(2);

            if (availableTypes.Count > 0)
            {
                int randomType = availableTypes[Random.Range(0, availableTypes.Count)];
                return new Vector3Int(randomCell.x, randomCell.y, randomType);
            }
        }

        // 3. 最后尝试攻击玩家棋子
        for (int type = 0; type < 3; type++)
        {
            PieceType pieceType = type == 0 ? PieceType.Guard :
                                 (type == 1 ? PieceType.Heavy : PieceType.Archer);

            // 检查是否有该类型的棋子
            if ((pieceType == PieceType.Guard && aiGuards <= 0) ||
                (pieceType == PieceType.Heavy && aiHeavies <= 0) ||
                (pieceType == PieceType.Archer && aiArchers <= 0))
                continue;

            // 检查所有玩家棋子
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (boardOwners[i, j] == 1 && CanAttack(pieceType, board[i, j]))
                    {
                        return new Vector3Int(i, j, type);
                    }
                }
            }
        }

        // 如果没有可用移动，返回无效值
        return new Vector3Int(-1, -1, -1);
    }

    bool CheckWin(int player)
    {
        // 检查所有行
        for (int i = 0; i < 3; i++)
        {
            if (boardOwners[i, 0] == player &&
                boardOwners[i, 1] == player &&
                boardOwners[i, 2] == player)
                return true;
        }

        // 检查所有列
        for (int i = 0; i < 3; i++)
        {
            if (boardOwners[0, i] == player &&
                boardOwners[1, i] == player &&
                boardOwners[2, i] == player)
                return true;
        }

        // 检查对角线
        if (boardOwners[0, 0] == player &&
            boardOwners[1, 1] == player &&
            boardOwners[2, 2] == player)
            return true;

        if (boardOwners[0, 2] == player &&
            boardOwners[1, 1] == player &&
            boardOwners[2, 0] == player)
            return true;

        return false;
    }

    bool CheckDraw()
    {
        // 检查是否所有格子都被占据
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (boardOwners[i, j] == 0)
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

                // 默认设置为空状态
                Color buttonColor = emptyColor;
                string buttonText = "";

                // 检查格子所有者并设置相应颜色
                if (boardOwners[i, j] == 1) // 玩家占领
                {
                    buttonColor = new Color(0, 0, 1, 0.5f); ; // 玩家颜色为蓝色

                    // 根据棋子类型设置文本
                    switch (board[i, j])
                    {
                        case PieceType.Guard:
                            buttonText = "G";
                            break;
                        case PieceType.Heavy:
                            buttonText = "H";
                            break;
                        case PieceType.Archer:
                            buttonText = "A";
                            break;
                    }
                }
                else if (boardOwners[i, j] == 2) // AI占领
                {
                    buttonColor = new Color(1, 0, 0, 0.5f); // AI颜色为红色

                    // 根据棋子类型设置文本
                    switch (board[i, j])
                    {
                        case PieceType.Guard:
                            buttonText = "G";
                            break;
                        case PieceType.Heavy:
                            buttonText = "H";
                            break;
                        case PieceType.Archer:
                            buttonText = "A";
                            break;
                    }
                }
                else // 空格子
                {
                    buttonColor = emptyColor;
                    buttonText = "";
                }

                // 应用颜色和文本
                buttons[index].image.color = buttonColor;
                buttons[index].GetComponentInChildren<Text>().text = buttonText;
            }
        }
    }

    void UpdatePieceCounts()
    {
        if (playerGuardText != null)
            playerGuardText.text = $"{playerGuards}";

        if (playerHeavyText != null)
            playerHeavyText.text = $"{playerHeavies}";

        if (playerArcherText != null)
            playerArcherText.text = $"{playerArchers}";

        if (aiGuardText != null)
            aiGuardText.text = $"{aiGuards}";

        if (aiHeavyText != null)
            aiHeavyText.text = $"{aiHeavies}";

        if (aiArcherText != null)
            aiArcherText.text = $"{aiArchers}";
    }

    void UsePlayerPiece(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Guard:
                playerGuards--;
                break;
            case PieceType.Heavy:
                playerHeavies--;
                break;
            case PieceType.Archer:
                playerArchers--;
                break;
        }
        UpdatePieceCounts();
    }

    void UseAIPiece(PieceType pieceType)
    {
        switch (pieceType)
        {
            case PieceType.Guard:
                aiGuards--;
                break;
            case PieceType.Heavy:
                aiHeavies--;
                break;
            case PieceType.Archer:
                aiArchers--;
                break;
        }
        UpdatePieceCounts();
    }

    public void NewGame()
    {
        // 重置胜场
        playerWins = 0;
        aiWins = 0;
        UpdateWinsText(); // 更新胜场显示

        // 重置游戏
        ResetGame();
    }
    public void ResetGame()
    {
        InitializeBoard();

        // 重置棋子计数
        playerGuards = 3;
        playerHeavies = 3;
        playerArchers = 3;
        aiGuards = 3;
        aiHeavies = 3;
        aiArchers = 3;

        UpdateButtonAppearance();
        UpdatePieceCounts();
        gameText.text = "Your Turn - Select a Piece";
        playerTurn = 1;
        selectedPieceType = PieceType.None;
    }
}