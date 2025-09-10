using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TicTacToe_v1_0 : MonoBehaviour
{
    // 已经定义的变量保持不变
    private int[,] board = new int[3, 3];
    public Button[] buttons;
    public Text gameText;
    private bool gameOver;
    private int playerTurn = 1; // 1 for player, 2 for AI
    
    // 添加胜场记录变量
    public Text playerWinsText;
    public Text aiWinsText;
    private int playerWins = 0;
    private int aiWins = 0;

    //添加反应状态指示变量
    public Text ElementsReaction;
    
    // 添加AI相关的私有变量
    private Player aiPlayer = Player.O;
    private Player humanPlayer = Player.X;

    // 添加音效相关变量
    public AudioClip winSound;
    public AudioClip loseSound;
    private AudioSource audioSource;

    // 玩家枚举
    private enum Player { None, X, O }
    public enum ElementType { None, Fire, Water, Grass, Earth, Ice }

    // 添加格子状态枚举
    public enum CellState
    {
        Normal,       // 正常状态
        Burning,      // 燃烧状态
        Pot,          // 陶罐状态
        WildGrass,    // 狂草状态
        Frozen,        // 冻结状态

    }

    // 添加格子类来管理每个格子的状态
    [System.Serializable]
    public class GridCell
    {
        public ElementType element; // 格子元素属性
        public CellState state;     // 格子状态
        public int owner;           // 0=无主, 1=玩家, 2=AI
    }

    // 添加游戏变量
    private GridCell[,] gridCells = new GridCell[3, 3];
    private ElementType currentPlayerElement; // 当前玩家棋子属性
    private ElementType currentAIElement;     // 当前AI棋子属性
    public Image playerElementIndicator;      // 玩家属性指示器
    public Image aiElementIndicator;          // AI属性指示器

    // 添加颜色映射
    public Color fireColor = Color.red;
    public Color waterColor = Color.blue;
    public Color grassColor = Color.green;
    public Color earthColor = new Color(0.6f, 0.4f, 0.2f);
    public Color iceColor = Color.cyan;

    //—————————————————————————————变量定义结束——————————————————————————————

    void Start()
    {
        InitializeBoard();
        UpdateButtonTexts();
        gameText.text = "Your Turn (X)";
        UpdateWinsText(); // 初始化胜场显示

        //初始化音频源
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void InitializeBoard()
    {
        // 初始化格子
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                gridCells[i, j] = new GridCell();
                gridCells[i, j].element = ElementType.None;
                gridCells[i, j].state = CellState.Normal;
                gridCells[i, j].owner = 0;
            }
        }

        // 随机分配格子属性
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                positions.Add(new Vector2Int(i, j));
            }
        }

        // 随机打乱位置
        positions = positions.OrderBy(x => Random.value).ToList();

        // 分配土属性 (1个)
        gridCells[positions[0].x, positions[0].y].element = ElementType.Earth;

        // 分配冰属性 (1个)
        gridCells[positions[1].x, positions[1].y].element = ElementType.Ice;

        // 分配草属性 (2个)
        gridCells[positions[2].x, positions[2].y].element = ElementType.Grass;
        gridCells[positions[3].x, positions[3].y].element = ElementType.Grass;

        // 其余格子保持None

        gameOver = false;

        // 随机分配玩家和AI的初始棋子属性
        RandomizePlayerElement();
        RandomizeAIElement();

        // 更新UI显示
        UpdateButtonAppearance();
        UpdateElementIndicators();
    }

    void RandomizePlayerElement()
    {
        currentPlayerElement = Random.Range(0, 2) == 0 ? ElementType.Fire : ElementType.Water;
    }

    void RandomizeAIElement()
    {
        currentAIElement = Random.Range(0, 2) == 0 ? ElementType.Fire : ElementType.Water;
    }

    void UpdateElementIndicators()
    {
        if (playerElementIndicator != null)
        {
            playerElementIndicator.color = currentPlayerElement == ElementType.Fire ? fireColor : waterColor;
        }

        if (aiElementIndicator != null)
        {
            aiElementIndicator.color = currentAIElement == ElementType.Fire ? fireColor : waterColor;
        }
    }

    public void OnButtonClick(int index)
    {
        if (gameOver || playerTurn != 1) return;

        int row = index / 3;
        int col = index % 3;

        // 检查格子是否已被占据
        if (gridCells[row, col].owner != 0) return;

        // 处理元素反应
        bool reactionSuccess = ProcessElementReaction(row, col, currentPlayerElement, true);

        if (reactionSuccess)
        {
            UpdateButtonAppearance();

            if (CheckWin(1))
            {
                gameText.text = "You Win!";
                playerWins++;
                UpdateWinsText();
                gameOver = true;
                return;
            }

            if (CheckDraw())
            {
                gameText.text = "Draw!";
                gameOver = true;
                return;
            }

            // 切换回合并随机化属性
            playerTurn = 2;
            RandomizePlayerElement();
            RandomizeAIElement();
            UpdateElementIndicators();
            gameText.text = "AI's Turn";

            // AI移动
            StartCoroutine(AIMoveWithDelay());
        }
    }

    bool ProcessElementReaction(int row, int col, ElementType element, bool isPlayer)
    {
        GridCell cell = gridCells[row, col];
        bool success = true;

        // 检查状态限制
        if (cell.state == CellState.Burning && element != ElementType.Water)
        {
            // 燃烧状态只能下水属性棋子
            return false;
        }
        else if (cell.state == CellState.WildGrass && element != ElementType.Fire)
        {
            // 狂草状态只能下火属性棋子
            return false;
        }
        

        // 处理元素反应
        if (element == ElementType.Fire)
        {
            switch (cell.element)
            {
                case ElementType.Grass:
                    // 火 + 草 = 燃烧状态
                    cell.state = CellState.Burning;
                    cell.element = ElementType.None;
                    cell.owner = 0; // 不占据格子
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Burning!";
                    break;

                case ElementType.Ice:
                    // 火 + 冰 = 原初状态
                    cell.state = CellState.Normal;
                    cell.element = ElementType.None;
                    cell.owner = 0;
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Melting!";
                    break;

                case ElementType.Earth:
                    // 火 + 土 = 陶罐状态，锁定格子
                    cell.state = CellState.Pot;
                    cell.element = ElementType.None;
                    cell.owner = isPlayer ? 1 : 2;
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Pot!";
                    break;

                default:
                    // 其他情况直接占据
                    cell.owner = isPlayer ? 1 : 2;
                    break;
            }
            switch(cell.state)
            {
                case CellState.WildGrass:
                    cell.element = ElementType.Earth;
                    cell.state = CellState.Normal;
                    cell.owner = 0; // 不占据格子
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Ash!";
                    break;
            }
                

        }
        else if (element == ElementType.Water)
        {
            switch (cell.element)
            {
                case ElementType.Grass:
                    // 水 + 草 = 狂草状态
                    cell.state = CellState.WildGrass;
                    cell.element = ElementType.None;
                    cell.owner = 0; // 不占据格子
                    if (ElementsReaction != null)
                        ElementsReaction.text = "WildGrass!";
                    break;

                case ElementType.Ice:
                    // 水 + 冰 = 冻结状态，占据格子
                    cell.state = CellState.Frozen;
                    cell.element = ElementType.None;
                    cell.owner = isPlayer ? 1 : 2;
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Frozen!";
                    break;

                case ElementType.Earth:
                    // 水 + 土 = 变为草属性
                    cell.element = ElementType.Grass;
                    cell.owner = 0; // 不占据格子
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Growing!";
                    break;

                default:
                    // 其他情况直接占据
                    cell.owner = isPlayer ? 1 : 2;
                    break;
            }
            switch (cell.state)
            {
                case CellState.Burning:
                    cell.state = CellState.Normal;
                    cell.element = ElementType.Earth;
                    cell.owner = 0; // 不占据格子
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Extinguish!";
                    break;
            }
        }

        // 更新格子元素（如果是占据状态）
        if (cell.owner != 0)
        {
            cell.element = element;
        }

        return success;
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

        // 简单AI：随机选择一个可用格子
        List<Vector2Int> availableCells = new List<Vector2Int>();

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (gridCells[i, j].owner == 0)
                {
                    availableCells.Add(new Vector2Int(i, j));
                }
            }
        }

        if (availableCells.Count == 0)
        {
            // 没有可用格子，平局
            gameText.text = "Draw!";
            gameOver = true;
            return;
        }

        // 随机选择一个格子
        Vector2Int selectedCell = availableCells[Random.Range(0, availableCells.Count)];

        // 处理元素反应
        bool reactionSuccess = ProcessElementReaction(selectedCell.x, selectedCell.y, currentAIElement, false);

        if (reactionSuccess)
        {
            UpdateButtonAppearance();

            if (CheckWin(2))
            {
                gameText.text = "AI Wins!";
                aiWins++;
                UpdateWinsText();
                gameOver = true;
                return;
            }

            if (CheckDraw())
            {
                gameText.text = "Draw!";
                gameOver = true;
                return;
            }

            // 切换回合并随机化属性
            playerTurn = 1;
            RandomizePlayerElement();
            RandomizeAIElement();
            UpdateElementIndicators();
            gameText.text = "Your Turn";
        }
        else
        {
            // 如果反应失败，重新尝试
            MakeAIMove();
        }
    }

    // AI算法实现
    private int EvaluateBoard(Player[,] board)
    {
        // 检查所有行
        for (int row = 0; row < 3; row++)
        {
            if (board[row, 0] != Player.None &&
                board[row, 0] == board[row, 1] &&
                board[row, 1] == board[row, 2])
            {
                return board[row, 0] == aiPlayer ? 10 : -10;
            }
        }

        // 检查所有列
        for (int col = 0; col < 3; col++)
        {
            if (board[0, col] != Player.None &&
                board[0, col] == board[1, col] &&
                board[1, col] == board[2, col])
            {
                return board[0, col] == aiPlayer ? 10 : -10;
            }
        }

        // 检查对角线
        if (board[0, 0] != Player.None &&
            board[0, 0] == board[1, 1] &&
            board[1, 1] == board[2, 2])
        {
            return board[0, 0] == aiPlayer ? 10 : -10;
        }

        if (board[0, 2] != Player.None &&
            board[0, 2] == board[1, 1] &&
            board[1, 1] == board[2, 0])
        {
            return board[0, 2] == aiPlayer ? 10 : -10;
        }

        return 0; // 平局或未结束
    }

    // 检查是否还有空位
    private bool HasEmptyCells(Player[,] board)
    {
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                if (board[i, j] == Player.None)
                    return true;
        return false;
    }

    // Minimax算法实现
    private int Minimax(Player[,] board, int depth, bool isMaximizing, int alpha, int beta)
    {
        int score = EvaluateBoard(board);

        // 如果AI赢了
        if (score == 10)
            return score - depth;

        // 如果玩家赢了
        if (score == -10)
            return score + depth;

        // 如果没有空位，平局
        if (!HasEmptyCells(board))
            return 0;

        // 如果是AI的回合（最大化玩家）
        if (isMaximizing)
        {
            int best = int.MinValue;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == Player.None)
                    {
                        board[i, j] = aiPlayer;
                        int value = Minimax(board, depth + 1, false, alpha, beta);
                        board[i, j] = Player.None;

                        best = Mathf.Max(best, value);
                        alpha = Mathf.Max(alpha, best);

                        // Alpha-Beta剪枝
                        if (beta <= alpha)
                            break;
                    }
                }
            }
            return best;
        }
        // 如果是玩家的回合（最小化玩家）
        else
        {
            int best = int.MaxValue;

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (board[i, j] == Player.None)
                    {
                        board[i, j] = humanPlayer;
                        int value = Minimax(board, depth + 1, true, alpha, beta);
                        board[i, j] = Player.None;

                        best = Mathf.Min(best, value);
                        beta = Mathf.Min(beta, best);

                        // Alpha-Beta剪枝
                        if (beta <= alpha)
                            break;
                    }
                }
            }
            return best;
        }
    }

    // 找到最佳移动
    private Vector2Int FindBestMove(Player[,] board)
    {
        int bestVal = int.MinValue;
        Vector2Int bestMove = new Vector2Int(-1, -1);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == Player.None)
                {
                    board[i, j] = aiPlayer;
                    int moveVal = Minimax(board, 0, false, int.MinValue, int.MaxValue);
                    board[i, j] = Player.None;

                    if (moveVal > bestVal)
                    {
                        bestMove = new Vector2Int(i, j);
                        bestVal = moveVal;
                    }
                }
            }
        }

        return bestMove;
    }

    bool CheckWin(int player)
    {
        // 检查行
        for (int i = 0; i < 3; i++)
        {
            if (gridCells[i, 0].owner == player &&
                gridCells[i, 1].owner == player &&
                gridCells[i, 2].owner == player)
                return true;
        }

        // 检查列
        for (int i = 0; i < 3; i++)
        {
            if (gridCells[0, i].owner == player &&
                gridCells[1, i].owner == player &&
                gridCells[2, i].owner == player)
                return true;
        }

        // 检查对角线
        if (gridCells[0, 0].owner == player &&
            gridCells[1, 1].owner == player &&
            gridCells[2, 2].owner == player)
            return true;

        if (gridCells[0, 2].owner == player &&
            gridCells[1, 1].owner == player &&
            gridCells[2, 0].owner == player)
            return true;

        return false;
    }

    bool CheckDraw()
    {
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                // 如果有任何格子没有被占据且不是限制状态，则游戏未结束
                if (gridCells[i, j].owner == 0 &&
                    gridCells[i, j].state != CellState.Burning &&
                    gridCells[i, j].state != CellState.WildGrass)
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
                GridCell cell = gridCells[i, j];

                // 设置按钮文本
                if (cell.owner == 1)
                    buttons[index].GetComponentInChildren<Text>().text = "X";
                else if (cell.owner == 2)
                    buttons[index].GetComponentInChildren<Text>().text = "O";
                else
                    buttons[index].GetComponentInChildren<Text>().text = "";

                // 设置按钮颜色基于元素属性
                Color cellColor = Color.white;

                switch (cell.element)
                {
                    case ElementType.Fire:
                        cellColor = fireColor;
                        break;
                    case ElementType.Water:
                        cellColor = waterColor;
                        break;
                    case ElementType.Grass:
                        cellColor = grassColor;
                        break;
                    case ElementType.Earth:
                        cellColor = earthColor;
                        break;
                    case ElementType.Ice:
                        cellColor = iceColor;
                        break;
                }

                // 根据状态调整颜色
                if (cell.state == CellState.Burning)
                    cellColor = Color.Lerp(cellColor, Color.red, 0.5f);
                else if (cell.state == CellState.WildGrass)
                    cellColor = Color.Lerp(cellColor, Color.green, 0.9f);
                else if (cell.state == CellState.Frozen)
                    cellColor = Color.Lerp(cellColor, Color.cyan, 0.5f);
                else if (cell.state == CellState.Pot)
                    cellColor = Color.Lerp(cellColor, Color.gray, 0.5f);

                buttons[index].image.color = cellColor;
            }
        }
    }

    void UpdateButtonTexts()
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
        gameText.text = "Your Turn";
        playerTurn = 1;
    }

    public void NewGame()
    {
        playerWins = 0;
        aiWins = 0;
        UpdateWinsText();
        ResetGame();
    }
}