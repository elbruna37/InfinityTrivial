using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.SceneManagement;

/// <summary>
/// Manages player turns, dice rolling, question handling, and win conditions.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    [Header("References")]
    public DiceSpawner diceSpawner;             // Spawns and throws dice
    public BoardManager boardManager;           // Moves player pieces

    [Header("Player Pieces")]
    private readonly List<PlayerPiece> playerPieces = new List<PlayerPiece>();
    private int[] wedgesPerPlayer;

    public int currentPlayerIndex = 0;
    private bool isWaitingForClick = false;
    private bool gameEnded = false;
    public bool canDestroy = false;

    [Header("Localization")]
    [SerializeField] private LocalizedString turnText;
    [SerializeField] private LocalizedString rollAgainText;
    [SerializeField] private LocalizedString wedgeWinText;
    [SerializeField] private LocalizedString rerollBoxText;
    [SerializeField] private LocalizedString winText;

    [SerializeField] private LocalizedString teamGreen;
    [SerializeField] private LocalizedString teamBlue;
    [SerializeField] private LocalizedString teamRed;
    [SerializeField] private LocalizedString teamYellow;

    private StringVariable teamNameVar;
    private StringVariable teamColorVar;

    [Header("UI")]
    public TMP_Text infoText;
    private float blinkSpeed = 2f;
    private Tween blinkTween;

    private string[] teamNames;
    private readonly string[] teamColors = { "green", "blue", "red", "yellow" };

    #region Unity Lifecycle

    /// <summary>
    /// Singleton initialization.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Initializes teams and starts the first turn.
    /// </summary>
    private void Start()
    {
        teamNames = new string[]
        {
            teamGreen.GetLocalizedString(),
            teamBlue.GetLocalizedString(),
            teamRed.GetLocalizedString(),
            teamYellow.GetLocalizedString()
        };

        wedgesPerPlayer = new int[GameManager.Instance.MaxPlayers];
        gameEnded = false;

        StartTurn();
    }

    /// <summary>
    /// Waits for player click to roll dice.
    /// </summary>
    private void Update()
    {
        if (gameEnded) return;

        if (isWaitingForClick && (Input.GetMouseButtonDown(0) || Input.touchCount > 0))
        {
            isWaitingForClick = false;
            UpdateInfoText("");
            diceSpawner.SpawnAndThrowDice(OnDiceResult);
        }
    }

    #endregion

    #region Turn Logic

    /// <summary>
    /// Starts a new turn for the current player and updates localized UI text.
    /// </summary>
    private void StartTurn()
    {
        if (gameEnded) return;

        teamNameVar = new StringVariable { Value = teamNames[currentPlayerIndex] };
        teamColorVar = new StringVariable { Value = teamColors[currentPlayerIndex] };

        AddLocalizationVariables(turnText);
        AddLocalizationVariables(rollAgainText);
        AddLocalizationVariables(wedgeWinText);
        AddLocalizationVariables(rerollBoxText);
        AddLocalizationVariables(winText);

        StartCoroutine(SetLocalizedText(turnText));
        isWaitingForClick = true;
    }

    /// <summary>
    /// Advances to the next player's turn.
    /// </summary>
    private void NextTurn()
    {
        currentPlayerIndex++;

        if (currentPlayerIndex >= GameManager.Instance.MaxPlayers)
            currentPlayerIndex = 0;

        StartTurn();
    }

    #endregion

    #region Player Registration

    /// <summary>
    /// Registers a player piece to participate in the turn cycle.
    /// </summary>
    public void RegisterPlayer(PlayerPiece piece)
    {
        if (!playerPieces.Contains(piece))
            playerPieces.Add(piece);
    }

    #endregion

    #region Dice & Movement

    /// <summary>
    /// Callback triggered after dice roll, initiates player movement.
    /// </summary>
    private void OnDiceResult(int result)
    {
        Debug.Log($"Player {currentPlayerIndex + 1} rolled a {result}");

        if (playerPieces.Count > currentPlayerIndex)
        {
            PlayerPiece currentPiece = playerPieces[currentPlayerIndex];
            StartCoroutine(MoveAndHandleNode(currentPiece, result));
        }
    }

    /// <summary>
    /// Moves the player and handles board node behavior after landing.
    /// </summary>
    private IEnumerator MoveAndHandleNode(PlayerPiece piece, int steps)
    {
        yield return boardManager.MoveRoutine(piece, steps);

        BoardNode landedNode = piece.currentNode;
        if (landedNode == null) yield break;

        Debug.Log($"Player {piece.name} landed on {landedNode.name} ({landedNode.nodeType}, {landedNode.nodeColor})");

        switch (landedNode.nodeType)
        {
            case BoardNode.NodeType.Normal:
                HandleNormalNode(landedNode);
                break;

            case BoardNode.NodeType.Wedge:
                HandleWedgeNode(landedNode);
                break;

            case BoardNode.NodeType.Reroll:
                HandleRerollNode();
                break;

            case BoardNode.NodeType.Start:
                canDestroy = true;
                NextTurn();
                break;
        }
    }

    #endregion

    #region Node Handling

    /// <summary>
    /// Handles logic when landing on a normal node.
    /// </summary>
    private void HandleNormalNode(BoardNode node)
    {
        Debug.Log("Normal node → fetching question...");
        string category = GameManager.Instance.GetCategoryForColor(ConvertNodeColor(node.nodeColor));

        QuestionsManager.Instance.AskRandomQuestion(category, correct =>
        {
            if (correct)
            {
                StartCoroutine(SetLocalizedText(rollAgainText));
                isWaitingForClick = true;
            }
            else
            {
                NextTurn();
            }
        });
    }

    /// <summary>
    /// Handles logic when landing on a wedge node.
    /// </summary>
    private void HandleWedgeNode(BoardNode node)
    {
        Debug.Log($"Wedge node → category {node.nodeColor}");
        string category = GameManager.Instance.GetCategoryForColor(ConvertNodeColor(node.nodeColor));

        QuestionsManager.Instance.AskRandomWedgeQuestion(category, correct =>
        {
            if (correct)
            {
                StartCoroutine(SetLocalizedText(wedgeWinText));
                ActivateWedgeForPlayer(currentPlayerIndex, ConvertNodeColor(node.nodeColor));
                isWaitingForClick = true;
            }
            else
            {
                NextTurn();
            }
        });
    }

    /// <summary>
    /// Handles logic when landing on a reroll node.
    /// </summary>
    private void HandleRerollNode()
    {
        canDestroy = true;
        StartCoroutine(SetLocalizedText(rerollBoxText));
        isWaitingForClick = true;
    }

    #endregion

    #region Wedge Activation & Victory

    /// <summary>
    /// Activates a wedge for a player and checks win condition.
    /// </summary>
    private void ActivateWedgeForPlayer(int playerIndex, QuesitoColor color)
    {
        string playerObjectName = $"QuesitoPlayer{playerIndex + 1}";
        GameObject playerObj = GameObject.Find(playerObjectName);

        if (playerObj == null)
        {
            Debug.LogWarning($"Could not find object {playerObjectName}");
            return;
        }

        string wedgeName = color switch
        {
            QuesitoColor.Azul => "Quesito Azul",
            QuesitoColor.Rosa => "Quesito Rosa",
            QuesitoColor.Amarillo => "Quesito Amarillo",
            QuesitoColor.Verde => "Quesito Verde",
            QuesitoColor.Naranja => "Quesito Naranja",
            QuesitoColor.Morado => "Quesito Morado",
            _ => string.Empty
        };

        if (string.IsNullOrEmpty(wedgeName)) return;

        Transform wedge = playerObj.transform.Find(wedgeName);
        if (wedge == null)
        {
            Debug.LogWarning($"Could not find wedge {wedgeName} inside {playerObjectName}");
            return;
        }

        if (wedge.gameObject.activeSelf)
        {
            Debug.Log($"Wedge {wedgeName} already active for {playerObjectName}");
            return;
        }

        wedge.gameObject.SetActive(true);
        wedgesPerPlayer[playerIndex]++;

        Debug.Log($"Team {teamNames[currentPlayerIndex]} has {wedgesPerPlayer[playerIndex]} wedges.");

        if (wedgesPerPlayer[playerIndex] >= 6)
        {
            gameEnded = true;
            StartCoroutine(SetLocalizedText(winText));
            isWaitingForClick = false;
            StartCoroutine(WaitForClickToReturnToMenu());
        }
    }

    #endregion

    #region Localization & UI

    /// <summary>
    /// Adds team name and color variables to a LocalizedString.
    /// </summary>
    private void AddLocalizationVariables(LocalizedString localized)
    {
        localized.Add("teamName", teamNameVar);
        localized.Add("teamColor", teamColorVar);
    }

    /// <summary>
    /// Updates the info text UI with localized content and blinking effect.
    /// </summary>
    public void UpdateInfoText(string newText)
    {
        infoText.text = newText;
        blinkTween?.Kill();

        if (string.IsNullOrEmpty(newText))
        {
            infoText.DOFade(1f, 0.2f);
            return;
        }

        blinkTween = infoText
            .DOFade(0f, blinkSpeed / 3f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }

    /// <summary>
    /// Coroutine to localize text and apply it to the UI.
    /// </summary>
    private IEnumerator SetLocalizedText(LocalizedString localized)
    {
        yield return LocalizationSettings.InitializationOperation;
        string result = localized.GetLocalizedString();
        UpdateInfoText(result);
    }

    #endregion

    #region Utility

    /// <summary>
    /// Converts board node color to wedge color type.
    /// </summary>
    private QuesitoColor ConvertNodeColor(BoardNode.NodeColor nodeColor)
    {
        return nodeColor switch
        {
            BoardNode.NodeColor.Blue => QuesitoColor.Azul,
            BoardNode.NodeColor.Pink => QuesitoColor.Rosa,
            BoardNode.NodeColor.Yellow => QuesitoColor.Amarillo,
            BoardNode.NodeColor.Green => QuesitoColor.Verde,
            BoardNode.NodeColor.Orange => QuesitoColor.Naranja,
            BoardNode.NodeColor.Purple => QuesitoColor.Morado,
            _ => throw new System.ArgumentException($"Unsupported node color: {nodeColor}")
        };
    }

    /// <summary>
    /// Waits for a click and then returns to the main menu scene.
    /// </summary>
    private IEnumerator WaitForClickToReturnToMenu()
    {
        while (!(Input.GetMouseButtonDown(0) || Input.touchCount > 0))
            yield return null;

        Destroy(GameManager.Instance.gameObject);
        Destroy(QuestionsManager.Instance.gameObject);
        Destroy(UIManager.Instance.gameObject);

        SceneManager.LoadScene("Menu");
    }

    #endregion
}
