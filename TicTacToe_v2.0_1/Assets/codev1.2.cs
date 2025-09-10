using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class InnovativeTicTacToe_v1_2 : MonoBehaviour
{
    // ��Ϸ״̬ö��
    public enum GameState { PlayerTurn, AITurn, AttackPhase, GameOver }

    // ����״̬
    private int[,] board = new int[3, 3]; // 0=��, 1=���, 2=AI
    public Button[] buttons;
    public Text gameText;
    private GameState currentState = GameState.PlayerTurn;

    // �����ж���ر���
    public GameObject attackPanel;
    public Image outerCircle;
    public Image innerCircle;
    private Vector2Int attackTarget;
    private float outerRadius;
    private float innerRadius;
    private float shrinkSpeed = 50f;
    private bool isAttacking = false;

    // ��Ч
    public AudioClip attackSuccessSound;
    public AudioClip attackFailSound;
    private AudioSource audioSource;

    // ��Ӳ�������
    public Material circleRingMaterial;

    // ���ʤ����¼����
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

        // ��ʼ����ƵԴ
        audioSource = gameObject.AddComponent<AudioSource>();

        // ���ع������
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

        // �������ж��׶�
        if (currentState == GameState.AttackPhase)
        {
            // ֻ���ڹ�����弤��ʱ�Ŵ������ж�
            if (attackPanel != null && attackPanel.activeSelf)
            {
                HandleAttackResult();
            }
            return;
        }

        // ��������׶�
        if (board[row, col] == 0)
        {
            // �������
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

            // �л���AI�غ�
            currentState = GameState.AITurn;
            gameText.text = "AI's Turn (O)";

            // AI�ƶ�
            StartCoroutine(AIMoveWithDelay());
        }
        else if (board[row, col] == 2) // ���Թ���AI������
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

            // ʹ��Shader����Բ��Ч��
            if (circleRingMaterial != null)
            {
                // ��������ʵ���Ա��⹲���������
                outerCircle.material = new Material(circleRingMaterial);
                innerCircle.material = new Material(circleRingMaterial);

                // ���ó�ʼ�뾶
                outerCircle.material.SetFloat("_OuterRadius", 0.7f);
                outerCircle.material.SetFloat("_InnerRadius", 0.5f);
                innerCircle.material.SetFloat("_OuterRadius", 0.5f);
                innerCircle.material.SetFloat("_InnerRadius", 0.3f);
            }

            // ���ö�������
            outerRadius = 0.7f;
            innerRadius = 0.5f;
            isAttacking = true;
        }
    }

    void Update()
    {
        if (isAttacking)
        {
            // ���°뾶ֵ - ȷ������Ȧ����С
            outerRadius -= shrinkSpeed * Time.deltaTime / 100f;
            innerRadius = innerRadius; // ��Ȧ����Ϊ��Ȧ��һ��

            // ����Shader����
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

            // �����Ȧ��С����С���Զ��ж�ʧ��
            if (outerRadius <= 0.1f)
            {
                HandleAttackResult(false);
            }
        }
    }

    void HandleAttackResult()
    {
        // ����������Ȧ�İ뾶��
        float radiusDifference = Mathf.Abs(outerRadius - innerRadius);
        float threshold = innerRadius * 0.5f;

        // �ж��Ƿ�ɹ�
        bool success = radiusDifference >= threshold;
        HandleAttackResult(success); // ���ô������汾
    }

    void HandleAttackResult(bool success)
    {
        isAttacking = false;

        // ���ع������
        if (attackPanel != null)
            attackPanel.SetActive(false);

        if (success)
        {
            // �����ɹ���ռ�����
            board[attackTarget.x, attackTarget.y] = 1;
            UpdateButtonAppearance();

            // ���ųɹ���Ч
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


            // �л���AI�غ�
            currentState = GameState.AITurn;
            gameText.text = "AI's Turn (O)";

            // AI�ƶ�
            StartCoroutine(AIMoveWithDelay());
        }
        else
        {
            // ����ʧ��
            // ����ʧ����Ч
            if (attackFailSound != null)
                audioSource.PlayOneShot(attackFailSound);

            gameText.text = "Attack Failed! AI's Turn";

            // �л���AI�غ�
            currentState = GameState.AITurn;

            // AI�ƶ�
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

            // �л�����һغ�
            currentState = GameState.PlayerTurn;
            gameText.text = "Your Turn (X)";
        }
        else
        {
            // û���ҵ���Ч�ƶ���Ӧ����ƽ��״̬��
            gameText.text = "Draw!";
            currentState = GameState.GameOver;
        }
    }

    // AI���ܾ����㷨
    Vector2Int? FindBestMove()
    {
        // 1. ���AI�Ƿ����������ʤ
        Vector2Int? winMove = FindWinningMove(2);
        if (winMove.HasValue)
            return winMove.Value;

        // 2. ��ֹ���������ʤ
        Vector2Int? blockMove = FindWinningMove(1);
        if (blockMove.HasValue)
            return blockMove.Value;

        // 3. ����ռ������λ�ã�������ã�
        if (board[1, 1] == 0)
            return new Vector2Int(1, 1);

        // 4. ռ�ݽ���λ��
        List<Vector2Int> corners = new List<Vector2Int>()
        {
            new Vector2Int(0, 0),
            new Vector2Int(0, 2),
            new Vector2Int(2, 0),
            new Vector2Int(2, 2)
        };

        // ���ѡ����õĽ���
        List<Vector2Int> availableCorners = corners.Where(pos => board[pos.x, pos.y] == 0).ToList();
        if (availableCorners.Count > 0)
        {
            return availableCorners[Random.Range(0, availableCorners.Count)];
        }

        // 5. ռ�ݱ�Եλ��
        List<Vector2Int> edges = new List<Vector2Int>()
        {
            new Vector2Int(0, 1),
            new Vector2Int(1, 0),
            new Vector2Int(1, 2),
            new Vector2Int(2, 1)
        };

        // ���ѡ����õı�Ե
        List<Vector2Int> availableEdges = edges.Where(pos => board[pos.x, pos.y] == 0).ToList();
        if (availableEdges.Count > 0)
        {
            return availableEdges[Random.Range(0, availableEdges.Count)];
        }

        // 6. û�п���λ�ã�ƽ�֣�
        return null;
    }

    // Ѱ�һ�ʤλ�ã��������κ���ң�
    Vector2Int? FindWinningMove(int player)
    {
        // ���������
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

        // ���������
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

        // ������Խ���
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

        // ��鸱�Խ���
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
        // �����
        for (int i = 0; i < 3; i++)
        {
            if (board[i, 0] == player && board[i, 1] == player && board[i, 2] == player)
                return true;
        }

        // �����
        for (int i = 0; i < 3; i++)
        {
            if (board[0, i] == player && board[1, i] == player && board[2, i] == player)
                return true;
        }

        // ���Խ���
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
        gameText.text = "Your Turn (X)";
        currentState = GameState.PlayerTurn;

        // ȷ�������������
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

        // ȷ�������������
        if (attackPanel != null)
            attackPanel.SetActive(false);

        isAttacking = false;
    }
}