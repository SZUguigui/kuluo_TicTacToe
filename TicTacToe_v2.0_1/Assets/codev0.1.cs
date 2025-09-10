using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TicTacToe : MonoBehaviour
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
    
    // ���AI��ص�˽�б���
    private Player aiPlayer = Player.O;
    private Player humanPlayer = Player.X;

    // �����Ч��ر���
    public AudioClip winSound;
    public AudioClip loseSound;
    private AudioSource audioSource;

    // ���ö��
    private enum Player { None, X, O }
    
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
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                board[i, j] = 0;
            }
        }
        gameOver = false;
    }
    
    public void OnButtonClick(int index)
    {
        if (gameOver || playerTurn != 1) return;
        
        int row = index / 3;
        int col = index % 3;
        
        if (board[row, col] == 0)
        {
            board[row, col] = 1;
            UpdateButtonTexts();
            
            if (CheckWin(1))
            {
                gameText.text = "You Win!";
                playerWins++; // �������ʤ��
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
            
            playerTurn = 2; // AI's turn
            gameText.text = "AI's Turn (O)";
            
            // AI move after a short delay
            StartCoroutine(AIMoveWithDelay());
        }
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
        
        // ת������״̬ΪAI�㷨��Ҫ�ĸ�ʽ
        Player[,] aiBoard = new Player[3, 3];
        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                if (board[i, j] == 0) aiBoard[i, j] = Player.None;
                else if (board[i, j] == 1) aiBoard[i, j] = humanPlayer;
                else aiBoard[i, j] = aiPlayer;
            }
        }
        
        // ��ȡAI������ƶ�
        Vector2Int bestMove = FindBestMove(aiBoard);
        
        // ִ��AI�ƶ�
        board[bestMove.x, bestMove.y] = 2;
        UpdateButtonTexts();
        
        if (CheckWin(2))
        {
            gameText.text = "AI Wins!";
            aiWins++; // ����AIʤ��
            UpdateWinsText();

            // ����ʤ����Ч
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
        
        playerTurn = 1; // Player's turn
        gameText.text = "Your Turn (X)";
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
        // Check rows
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] == player && board[i, 1] == player && board[i, 2] == player)
                return true;
        }
        
        // Check columns
        for (int i = 0; i < 3; i++)
        {
            if (board[0, i] == player && board[1, i] == player && board[2, i] == player)
                return true;
        }
        
        // Check diagonals
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
        UpdateButtonTexts();
        gameText.text = "Your Turn (X)";
        playerTurn = 1;
    }
    
    // ����Ϸ�� - ������Ϸ������ʤ����
    public void NewGame()
    {
        ResetGame(); // ��������
        playerWins = 0; // �������ʤ��
        aiWins = 0; // ����AIʤ��
        UpdateWinsText(); // ����ʤ����ʾ
    }
}