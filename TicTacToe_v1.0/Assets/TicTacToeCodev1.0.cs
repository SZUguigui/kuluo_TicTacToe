using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TicTacToe_v1_0 : MonoBehaviour
{
    // �Ѿ�����ı������ֲ���
    private int[,] board = new int[3, 3];
    public Button[] buttons;
    public Text gameText;
    private bool gameOver;
    private int playerTurn = 1; // 1 for player, 2 for AI
    
    // ���ʤ����¼����
    public Text playerWinsText;
    public Text aiWinsText;
    private int playerWins = 0;
    private int aiWins = 0;

    //��ӷ�Ӧ״ָ̬ʾ����
    public Text ElementsReaction;
    
    // ���AI��ص�˽�б���
    private Player aiPlayer = Player.O;
    private Player humanPlayer = Player.X;

    // �����Ч��ر���
    public AudioClip winSound;
    public AudioClip loseSound;
    private AudioSource audioSource;

    // ���ö��
    private enum Player { None, X, O }
    public enum ElementType { None, Fire, Water, Grass, Earth, Ice }

    // ��Ӹ���״̬ö��
    public enum CellState
    {
        Normal,       // ����״̬
        Burning,      // ȼ��״̬
        Pot,          // �չ�״̬
        WildGrass,    // ���״̬
        Frozen,        // ����״̬

    }

    // ��Ӹ�����������ÿ�����ӵ�״̬
    [System.Serializable]
    public class GridCell
    {
        public ElementType element; // ����Ԫ������
        public CellState state;     // ����״̬
        public int owner;           // 0=����, 1=���, 2=AI
    }

    // �����Ϸ����
    private GridCell[,] gridCells = new GridCell[3, 3];
    private ElementType currentPlayerElement; // ��ǰ�����������
    private ElementType currentAIElement;     // ��ǰAI��������
    public Image playerElementIndicator;      // �������ָʾ��
    public Image aiElementIndicator;          // AI����ָʾ��

    // �����ɫӳ��
    public Color fireColor = Color.red;
    public Color waterColor = Color.blue;
    public Color grassColor = Color.green;
    public Color earthColor = new Color(0.6f, 0.4f, 0.2f);
    public Color iceColor = Color.cyan;

    //���������������������������������������������������������������������������������������������������������������������������������

    void Start()
    {
        InitializeBoard();
        UpdateButtonTexts();
        gameText.text = "Your Turn (X)";
        UpdateWinsText(); // ��ʼ��ʤ����ʾ

        //��ʼ����ƵԴ
        audioSource = gameObject.AddComponent<AudioSource>();
    }

    void InitializeBoard()
    {
        // ��ʼ������
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

        // ��������������
        List<Vector2Int> positions = new List<Vector2Int>();
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                positions.Add(new Vector2Int(i, j));
            }
        }

        // �������λ��
        positions = positions.OrderBy(x => Random.value).ToList();

        // ���������� (1��)
        gridCells[positions[0].x, positions[0].y].element = ElementType.Earth;

        // ��������� (1��)
        gridCells[positions[1].x, positions[1].y].element = ElementType.Ice;

        // ��������� (2��)
        gridCells[positions[2].x, positions[2].y].element = ElementType.Grass;
        gridCells[positions[3].x, positions[3].y].element = ElementType.Grass;

        // ������ӱ���None

        gameOver = false;

        // ���������Һ�AI�ĳ�ʼ��������
        RandomizePlayerElement();
        RandomizeAIElement();

        // ����UI��ʾ
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

        // �������Ƿ��ѱ�ռ��
        if (gridCells[row, col].owner != 0) return;

        // ����Ԫ�ط�Ӧ
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

            // �л��غϲ����������
            playerTurn = 2;
            RandomizePlayerElement();
            RandomizeAIElement();
            UpdateElementIndicators();
            gameText.text = "AI's Turn";

            // AI�ƶ�
            StartCoroutine(AIMoveWithDelay());
        }
    }

    bool ProcessElementReaction(int row, int col, ElementType element, bool isPlayer)
    {
        GridCell cell = gridCells[row, col];
        bool success = true;

        // ���״̬����
        if (cell.state == CellState.Burning && element != ElementType.Water)
        {
            // ȼ��״ֻ̬����ˮ��������
            return false;
        }
        else if (cell.state == CellState.WildGrass && element != ElementType.Fire)
        {
            // ���״ֻ̬���»���������
            return false;
        }
        

        // ����Ԫ�ط�Ӧ
        if (element == ElementType.Fire)
        {
            switch (cell.element)
            {
                case ElementType.Grass:
                    // �� + �� = ȼ��״̬
                    cell.state = CellState.Burning;
                    cell.element = ElementType.None;
                    cell.owner = 0; // ��ռ�ݸ���
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Burning!";
                    break;

                case ElementType.Ice:
                    // �� + �� = ԭ��״̬
                    cell.state = CellState.Normal;
                    cell.element = ElementType.None;
                    cell.owner = 0;
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Melting!";
                    break;

                case ElementType.Earth:
                    // �� + �� = �չ�״̬����������
                    cell.state = CellState.Pot;
                    cell.element = ElementType.None;
                    cell.owner = isPlayer ? 1 : 2;
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Pot!";
                    break;

                default:
                    // �������ֱ��ռ��
                    cell.owner = isPlayer ? 1 : 2;
                    break;
            }
            switch(cell.state)
            {
                case CellState.WildGrass:
                    cell.element = ElementType.Earth;
                    cell.state = CellState.Normal;
                    cell.owner = 0; // ��ռ�ݸ���
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
                    // ˮ + �� = ���״̬
                    cell.state = CellState.WildGrass;
                    cell.element = ElementType.None;
                    cell.owner = 0; // ��ռ�ݸ���
                    if (ElementsReaction != null)
                        ElementsReaction.text = "WildGrass!";
                    break;

                case ElementType.Ice:
                    // ˮ + �� = ����״̬��ռ�ݸ���
                    cell.state = CellState.Frozen;
                    cell.element = ElementType.None;
                    cell.owner = isPlayer ? 1 : 2;
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Frozen!";
                    break;

                case ElementType.Earth:
                    // ˮ + �� = ��Ϊ������
                    cell.element = ElementType.Grass;
                    cell.owner = 0; // ��ռ�ݸ���
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Growing!";
                    break;

                default:
                    // �������ֱ��ռ��
                    cell.owner = isPlayer ? 1 : 2;
                    break;
            }
            switch (cell.state)
            {
                case CellState.Burning:
                    cell.state = CellState.Normal;
                    cell.element = ElementType.Earth;
                    cell.owner = 0; // ��ռ�ݸ���
                    if (ElementsReaction != null)
                        ElementsReaction.text = "Extinguish!";
                    break;
            }
        }

        // ���¸���Ԫ�أ������ռ��״̬��
        if (cell.owner != 0)
        {
            cell.element = element;
        }

        return success;
    }

    IEnumerator AIMoveWithDelay()
    {
        // ���һ���ӳ٣���AI���ƶ�����������Ȼ
        yield return new WaitForSeconds(0.5f);
        MakeAIMove();
    }

    void MakeAIMove()
    {
        if (gameOver || playerTurn != 2) return;

        // ��AI�����ѡ��һ�����ø���
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
            // û�п��ø��ӣ�ƽ��
            gameText.text = "Draw!";
            gameOver = true;
            return;
        }

        // ���ѡ��һ������
        Vector2Int selectedCell = availableCells[Random.Range(0, availableCells.Count)];

        // ����Ԫ�ط�Ӧ
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

            // �л��غϲ����������
            playerTurn = 1;
            RandomizePlayerElement();
            RandomizeAIElement();
            UpdateElementIndicators();
            gameText.text = "Your Turn";
        }
        else
        {
            // �����Ӧʧ�ܣ����³���
            MakeAIMove();
        }
    }

    // AI�㷨ʵ��
    private int EvaluateBoard(Player[,] board)
    {
        // ���������
        for (int row = 0; row < 3; row++)
        {
            if (board[row, 0] != Player.None &&
                board[row, 0] == board[row, 1] &&
                board[row, 1] == board[row, 2])
            {
                return board[row, 0] == aiPlayer ? 10 : -10;
            }
        }

        // ���������
        for (int col = 0; col < 3; col++)
        {
            if (board[0, col] != Player.None &&
                board[0, col] == board[1, col] &&
                board[1, col] == board[2, col])
            {
                return board[0, col] == aiPlayer ? 10 : -10;
            }
        }

        // ���Խ���
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

        return 0; // ƽ�ֻ�δ����
    }

    // ����Ƿ��п�λ
    private bool HasEmptyCells(Player[,] board)
    {
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                if (board[i, j] == Player.None)
                    return true;
        return false;
    }

    // Minimax�㷨ʵ��
    private int Minimax(Player[,] board, int depth, bool isMaximizing, int alpha, int beta)
    {
        int score = EvaluateBoard(board);

        // ���AIӮ��
        if (score == 10)
            return score - depth;

        // ������Ӯ��
        if (score == -10)
            return score + depth;

        // ���û�п�λ��ƽ��
        if (!HasEmptyCells(board))
            return 0;

        // �����AI�Ļغϣ������ң�
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

                        // Alpha-Beta��֦
                        if (beta <= alpha)
                            break;
                    }
                }
            }
            return best;
        }
        // �������ҵĻغϣ���С����ң�
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

                        // Alpha-Beta��֦
                        if (beta <= alpha)
                            break;
                    }
                }
            }
            return best;
        }
    }

    // �ҵ�����ƶ�
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
        // �����
        for (int i = 0; i < 3; i++)
        {
            if (gridCells[i, 0].owner == player &&
                gridCells[i, 1].owner == player &&
                gridCells[i, 2].owner == player)
                return true;
        }

        // �����
        for (int i = 0; i < 3; i++)
        {
            if (gridCells[0, i].owner == player &&
                gridCells[1, i].owner == player &&
                gridCells[2, i].owner == player)
                return true;
        }

        // ���Խ���
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
                // ������κθ���û�б�ռ���Ҳ�������״̬������Ϸδ����
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

                // ���ð�ť�ı�
                if (cell.owner == 1)
                    buttons[index].GetComponentInChildren<Text>().text = "X";
                else if (cell.owner == 2)
                    buttons[index].GetComponentInChildren<Text>().text = "O";
                else
                    buttons[index].GetComponentInChildren<Text>().text = "";

                // ���ð�ť��ɫ����Ԫ������
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

                // ����״̬������ɫ
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
    
    // ����ʤ����ʾ
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