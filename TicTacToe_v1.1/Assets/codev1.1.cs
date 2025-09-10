using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class CreativeTicTacToe_v1_1 : MonoBehaviour
{
    // ��������ö��
    public enum PieceType { None, Guard, Heavy, Archer }

    // ����״̬
    private PieceType[,] board = new PieceType[3, 3];
    private int[,] boardOwners = new int[3, 3]; // 0=��, 1=���, 2=AI

    public Button[] buttons;
    public Text gameText;
    private bool gameOver;
    private int playerTurn = 1; // 1 for player, 2 for AI

    // ���Ӽ���
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

    // ��ɫ����
    public Color guardColor = Color.blue;
    public Color heavyColor = Color.red;
    public Color archerColor = Color.green;
    public Color emptyColor = Color.white;
    public Color AIColor = Color.red;
    public Color PlayerColor = Color.blue;

    // ��ǰѡ�����������
    private PieceType selectedPieceType = PieceType.None;

    // ��Ч��ر���
    public AudioClip winSound;
    public AudioClip loseSound;
    private AudioSource audioSource;

    // ʤ����¼
    private int playerWins = 0;
    private int aiWins = 0;
    public Text playerWinsText;
    public Text aiWinsText;

    void Start()
    {
        InitializeBoard();
        UpdateButtonAppearance();
        UpdatePieceCounts();
        UpdateWinsText(); // ��ʼ��ʤ����ʾ
        gameText.text = "Your Turn - Select a Piece";

        // ��ʼ����ƵԴ
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

    // ����ʤ����ʾ
    void UpdateWinsText()
    {
        if (playerWinsText != null)
            playerWinsText.text = ""+playerWins;

        if (aiWinsText != null)
            aiWinsText.text = ""+aiWins;
    }

    // ���ѡ����������
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

        // �������Ƿ��ѱ�ռ��
        if (boardOwners[row, col] != 0)
        {
            // ����Ƿ���Թ��������ƹ�ϵ��
            if (CanAttack(selectedPieceType, board[row, col]))
            {
                // �����ɹ���ռ�����
                board[row, col] = selectedPieceType;
                boardOwners[row, col] = 1; // ���ռ��
                UsePlayerPiece(selectedPieceType);
                UpdateButtonAppearance();

                if (CheckWin(1))
                {
                    gameText.text = "You Win!";
                    playerWins++; // �������ʤ��
                    UpdateWinsText(); // ����ʤ����ʾ

                    // ����ʤ����Ч
                    if (winSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(winSound);
                    }

                    gameOver = true;
                    return;
                }

                // �л���AI�غ�
                playerTurn = 2;
                selectedPieceType = PieceType.None;
                gameText.text = "AI's Turn";

                // AI�ƶ�
                StartCoroutine(AIMoveWithDelay());
            }
            else
            {
                gameText.text = "Cannot attack this piece! Select another piece.";
            }
            return;
        }

        // ����ǿ�λ��ֱ�ӷ�������
        board[row, col] = selectedPieceType;
        boardOwners[row, col] = 1; // ���ռ��
        UsePlayerPiece(selectedPieceType);
        UpdateButtonAppearance();

        if (CheckWin(1))
        {
            gameText.text = "You Win!";
            playerWins++; // �������ʤ��
            UpdateWinsText(); // ����ʤ����ʾ
            // ����ʤ����Ч
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

        // �л���AI�غ�
        playerTurn = 2;
        selectedPieceType = PieceType.None;
        gameText.text = "AI's Turn";

        // AI�ƶ�
        StartCoroutine(AIMoveWithDelay());
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

        // AI���ԣ����ȳ������ߣ�Ȼ�����ѡ�����Ź���
        Vector3Int bestMove = FindBestMove();

        if (bestMove.x != -1)
        {
            int row = bestMove.x;
            int col = bestMove.y;
            PieceType aiPieceType = bestMove.z == 0 ? PieceType.Guard :
                                   (bestMove.z == 1 ? PieceType.Heavy : PieceType.Archer);

            // ����Ƿ��и����͵�����
            if ((aiPieceType == PieceType.Guard && aiGuards > 0) ||
                (aiPieceType == PieceType.Heavy && aiHeavies > 0) ||
                (aiPieceType == PieceType.Archer && aiArchers > 0))
            {
                // ����ǹ���
                if (boardOwners[row, col] != 0)
                {
                    board[row, col] = aiPieceType;
                    boardOwners[row, col] = 2; // AIռ��
                    UseAIPiece(aiPieceType);
                }
                else
                {
                    // ����Ƿ���
                    board[row, col] = aiPieceType;
                    boardOwners[row, col] = 2; // AIռ��
                    UseAIPiece(aiPieceType);
                }

                UpdateButtonAppearance();

                if (CheckWin(2))
                {
                    gameText.text = "AI Wins!";
                    aiWins++; // ����AIʤ��
                    UpdateWinsText(); // ����ʤ����ʾ

                    // ����ʧ����Ч
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

        // �л�����һغ�
        playerTurn = 1;
        gameText.text = "Your Turn - Select a Piece";
    }

    // �����ƹ�ϵ
    bool CanAttack(PieceType attacker, PieceType defender)
    {
        // ����������װ
        if (attacker == PieceType.Guard && defender == PieceType.Heavy)
            return true;

        // ��װ���ƹ�����
        if (attacker == PieceType.Heavy && defender == PieceType.Archer)
            return true;

        // �����ֿ��ƽ���
        if (attacker == PieceType.Archer && defender == PieceType.Guard)
            return true;

        return false;
    }

    // AI�ҵ�����ƶ�
    // AI�ҵ�����ƶ�
    Vector3Int FindBestMove()
    {
        // 1. ���ȳ����������
        for (int type = 0; type < 3; type++)
        {
            PieceType pieceType = type == 0 ? PieceType.Guard :
                                 (type == 1 ? PieceType.Heavy : PieceType.Archer);

            // ����Ƿ��и����͵�����
            if ((pieceType == PieceType.Guard && aiGuards <= 0) ||
                (pieceType == PieceType.Heavy && aiHeavies <= 0) ||
                (pieceType == PieceType.Archer && aiArchers <= 0))
                continue;

            // ������п�λ
            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    if (boardOwners[i, j] == 0)
                    {
                        // ģ���������
                        boardOwners[i, j] = 2;

                        // ����Ƿ��Ӯ
                        if (CheckWin(2))
                        {
                            boardOwners[i, j] = 0; // �ָ�
                            return new Vector3Int(i, j, type);
                        }

                        boardOwners[i, j] = 0; // �ָ�
                    }
                }
            }
        }

        // 2. ���ѡ��һ����λ����������
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

            // ���ѡ��һ�����õ���������
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

        // 3. ����Թ����������
        for (int type = 0; type < 3; type++)
        {
            PieceType pieceType = type == 0 ? PieceType.Guard :
                                 (type == 1 ? PieceType.Heavy : PieceType.Archer);

            // ����Ƿ��и����͵�����
            if ((pieceType == PieceType.Guard && aiGuards <= 0) ||
                (pieceType == PieceType.Heavy && aiHeavies <= 0) ||
                (pieceType == PieceType.Archer && aiArchers <= 0))
                continue;

            // ��������������
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

        // ���û�п����ƶ���������Чֵ
        return new Vector3Int(-1, -1, -1);
    }

    bool CheckWin(int player)
    {
        // ���������
        for (int i = 0; i < 3; i++)
        {
            if (boardOwners[i, 0] == player &&
                boardOwners[i, 1] == player &&
                boardOwners[i, 2] == player)
                return true;
        }

        // ���������
        for (int i = 0; i < 3; i++)
        {
            if (boardOwners[0, i] == player &&
                boardOwners[1, i] == player &&
                boardOwners[2, i] == player)
                return true;
        }

        // ���Խ���
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
        // ����Ƿ����и��Ӷ���ռ��
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

                // Ĭ������Ϊ��״̬
                Color buttonColor = emptyColor;
                string buttonText = "";

                // �����������߲�������Ӧ��ɫ
                if (boardOwners[i, j] == 1) // ���ռ��
                {
                    buttonColor = new Color(0, 0, 1, 0.5f); ; // �����ɫΪ��ɫ

                    // �����������������ı�
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
                else if (boardOwners[i, j] == 2) // AIռ��
                {
                    buttonColor = new Color(1, 0, 0, 0.5f); // AI��ɫΪ��ɫ

                    // �����������������ı�
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
                else // �ո���
                {
                    buttonColor = emptyColor;
                    buttonText = "";
                }

                // Ӧ����ɫ���ı�
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
        // ����ʤ��
        playerWins = 0;
        aiWins = 0;
        UpdateWinsText(); // ����ʤ����ʾ

        // ������Ϸ
        ResetGame();
    }
    public void ResetGame()
    {
        InitializeBoard();

        // �������Ӽ���
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