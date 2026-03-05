using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


/// <summary>
/// Manages player turns, dice rolling, question handling, and win conditions.
/// </summary>
public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public event Action<int, PlayerPiece> OnPlayerRegistered;

    [Header("References")]
    public DiceSpawner diceSpawner;            
    public BoardManager boardManager;

   [Header("Player Pieces")]
    private readonly List<PlayerPiece> playerPieces = new List<PlayerPiece>();
    private int[] wedgesPerPlayer;
    private Dictionary<int, List<QuesitoColor>> wedgesByPlayer = new();

    [Header("Control")]
    public int currentPlayerIndex = 0;
    public bool isPause = false;
    public bool isWedgeQuestion = false;
    bool tapCancelled;

    //true = PC controller; false = Movil Controller
    private bool controller = true;
    private float touchStartTime;
    private bool touchActive;
    private bool pauseTriggered;

    private bool gameEnded = false;
    public bool canDestroy = false;

    [Header("Localization")]
    [SerializeField] private LocalizedString turnText;
    [SerializeField] private LocalizedString rollAgainText;
    [SerializeField] private LocalizedString wedgeWinText;
    [SerializeField] private LocalizedString rerollBoxText;
    [SerializeField] private LocalizedString winText;

    [SerializeField] private LocalizedString selectWedgeText;
    [SerializeField] private LocalizedString wedgeStolenText;
    [SerializeField] private LocalizedString duelStartText;
    [SerializeField] private LocalizedString couldntStolenText;
    [SerializeField] private LocalizedString heistFailedText;

    [SerializeField] private LocalizedString teamGreen;
    [SerializeField] private LocalizedString teamBlue;
    [SerializeField] private LocalizedString teamRed;
    [SerializeField] private LocalizedString teamYellow;

    private StringVariable teamNameVar;
    private StringVariable teamColorVar;

    private StringVariable attackerVar;
    private StringVariable attackerColorVar;

    private StringVariable defenderVar;
    private StringVariable defenderColorVar;

    private StringVariable wedgeVar;
    private StringVariable wedgeColorVar;


    [Header("UI")]
    public TMP_Text infoText;
    private Tween blinkTween;
    public GameObject pausePanel;
    public GameObject loadingPanel;
    public GameObject[] playerWedgeContainers;
    [SerializeField] private Button pauseBtn;


   [Header("Camera Reference")]
    [SerializeField] private GameObject mainCamera;

    [Header("Steal Duel Logic")]
    public bool isDuelActive = false;
    private int duelAttacker = 0;
    private int duelDefender = 0;
    private int duelCurrent;
    private QuesitoColor duelColor;
    

    private string[] teamNames;
    private readonly string[] teamColors = { "green", "blue", "red", "yellow" };


    private readonly string[] wedgeColors = { "#4C92CB", "#E54781", "#C67731", "#EDCB34", "#86B363", "#8F62BC" };  // blue, pink, orange, yellow, green, purple
    int wedgeColorsIndex;
    string wedgeCategory;

    #region Unity Lifecycle
    /// <summary>
    /// Singleton initialization.
    /// </summary>
    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (GameManager.Instance.IsLoadingGame && GameSaveManager.Instance.LoadedSaveData != null)
        {
            currentPlayerIndex = GameSaveManager.Instance.LoadedSaveData.currentPlayerIndex;
            Debug.Log($"[TurnManager] Restored turn index early: {currentPlayerIndex}");
        }
    }

    /// <summary>
    /// Initializes teams and starts the first turn.
    /// </summary>
    private void Start()
    {
        pauseBtn.interactable = false;

        teamNames = new string[]
        {
            teamGreen.GetLocalizedString(),
            teamBlue.GetLocalizedString(),
            teamRed.GetLocalizedString(),
            teamYellow.GetLocalizedString()
        };

        wedgesPerPlayer = new int[GameManager.Instance.MaxPlayers];

        gameEnded = false;

        if (GameManager.Instance.IsLoadingGame && GameSaveManager.Instance.LoadedSaveData != null)
        {
            var save = GameSaveManager.Instance.LoadedSaveData;

            if (save.wedgesByPlayer != null)
            {
                SetWedgesByPlayer(save.wedgesByPlayer);
                Debug.Log("[TurnManager] Wedges restored.");
            }
        }

        GameManager.Instance.SetLoadingGame(false);

        loadingPanel.SetActive(false);

        MusicManager.Instance.PlayMusic("backgroundMusic");

        StartTurn();
    }

    #endregion

    #region Turn Logic

    /// <summary>
    /// Starts a new turn for the current player and updates localized UI text.
    /// </summary>
    private void StartTurn()
    {
        if (gameEnded) return;

        InitialaizeLocation();

        StartCoroutine(SetLocalizedText(turnText));

        WaitForPlayer(controller,() =>
        {
            diceSpawner.SpawnAndThrowDice(OnDiceResult);
        });
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
    public void RegisterPlayer(PlayerPiece piece, int index)
    {
        while (playerPieces.Count <= index)
            playerPieces.Add(null);

        playerPieces[index] = piece;

        OnPlayerRegistered?.Invoke(index, piece);
    }

    public bool TryGetPlayer(int index, out PlayerPiece piece)
    {
        piece = null;
        if (index < 0 || index >= playerPieces.Count) return false;
        piece = playerPieces[index];
        return piece != null;
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
                HandleStartNode(landedNode);
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
        Debug.Log("Normal node, fetching question...");
        isWedgeQuestion = false;
        string category = GameManager.Instance.GetCategoryForColor(ConvertNodeColor(node.nodeColor));

        StartCoroutine(PlaySoundDelayed(1,1, () =>
        {
            GameManager.Instance.PlayQuestionSound();
        }));

        QuestionsManager.Instance.AskRandomQuestion(category,1,0.5f,0.9f, correct =>
        {
            if (correct)
            {
                StartCoroutine(SetLocalizedText(rollAgainText));
                StartTurn();
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
        Debug.Log($"Wedge node, category {node.nodeColor}");
        isWedgeQuestion = true;
        string category = GameManager.Instance.GetCategoryForColor(ConvertNodeColor(node.nodeColor));
       
        StartCoroutine(PlaySoundDelayed(1,1, () =>
        {
            GameManager.Instance.PlayWedgeQuestionSound();
        }));

        QuestionsManager.Instance.AskRandomQuestion(category,1,0,0.3f, correct =>
        {
            if (correct)
            {
                StartCoroutine(SetLocalizedText(wedgeWinText));
                ActivateWedgeForPlayer(currentPlayerIndex, ConvertNodeColor(node.nodeColor));
                StartTurn();
            }
            else
            {
                NextTurn();
            }
        });
    }

    private void HandleStartNode(BoardNode node)
    {
        Debug.Log("Start node, choosing player...");

        int attacker = currentPlayerIndex;

        List<int> candidates = new List<int>();
        for (int i = 0; i < playerPieces.Count; i++)
        {
            if (i == attacker) continue;
            if (wedgesByPlayer.ContainsKey(i) && wedgesByPlayer[i].Count > 0)
                candidates.Add(i);
        }

        // Si nadie tiene quesitos → pierde turno
        if (candidates.Count == 0)
        {
            Debug.Log("No opponents have wedges, skip turn.");
            StartCoroutine(SetLocalizedText(couldntStolenText));

            WaitForPlayer(controller,() =>
            {
                NextTurn();
            });
            return;
        }

        StartCoroutine(PlaySoundDelayed(0,1, () =>
        {
            MusicManager.Instance.PlayMusic("stealMusic");
        }));

        SetupWedgeButtonsForSteal(attacker, candidates);

        int defender = candidates[0];
        
    }

    /// <summary>
    /// Handles logic when landing on a reroll node.
    /// </summary>
    private void HandleRerollNode()
    {
        StartCoroutine(SetLocalizedText(rerollBoxText));

        WaitForPlayer(controller,() =>
        {
            diceSpawner.SpawnAndThrowDice(OnDiceResult);
        });
    }

    #endregion

    #region Steal GameMode Methods

    private void SetupWedgeButtonsForSteal(int attacker, List<int> candidates)
    {
        StartCoroutine(SetLocalizedText(selectWedgeText));

        foreach (int defenderIndex in candidates)
        {
            Transform panel = playerWedgeContainers[defenderIndex].transform;

            foreach (Transform child in panel)
            {
                // Ignore texts or containers
                if (child.name == "Almacen" || child.name.Contains("Text"))
                    continue;

                Button b = child.GetComponent<Button>();
                if (b == null)
                    b = child.gameObject.AddComponent<Button>();

                // Only visible buttons (the player owns this wedge)
                if (child.gameObject.activeSelf)
                    b.interactable = true;       
                else
                    b.interactable = false;     

                b.onClick.RemoveAllListeners();

                QuesitoColor color = ConvertNameToColor(child.name);

                b.onClick.AddListener(() =>
                {
                    Debug.Log($"Steal selected defender = {defenderIndex}, color = {color}");

                    int defender = defenderIndex;

                    wedgeColorsIndex = (int)color;

                    // Deactivate ALL buttons when one is clicked
                    foreach (var container in playerWedgeContainers)
                    {
                        foreach (Transform child in container.transform)
                        {
                            Button b = child.GetComponent<Button>();
                            if (b != null)
                                b.interactable = false;
                        }
                    }

                    BeginStealDuel(attacker, defender, color);
                });
            }
        }
    }

    public void BeginStealDuel(int attacker, int defender, QuesitoColor color)
    {

        duelAttacker = attacker;
        duelDefender = defender;

        duelColor = color;
        string category = GameManager.Instance.GetCategoryForColor(color);
        wedgeCategory = category;

        Debug.Log($"Starting STEAL DUEL = attacker: {attacker}, defender: {defender}, color: {category}");

        duelCurrent = attacker;
        isDuelActive = true;

        InitialaizeStealLocation();

        StartCoroutine(SetLocalizedText(duelStartText));

        WaitForPlayer(controller,() =>
        {
            ContinueStealDuel();
        });
    }

    private void ContinueStealDuel()
    {
        if (!isDuelActive)
            return;

        Debug.Log($"STEAL TURN Player {duelCurrent}");
        string category = GameManager.Instance.GetCategoryForColor(duelColor);

        QuestionsManager.Instance.AskRandomQuestion(category,2,0,0.1f, (correct) =>
        {
            OnStealAnswerReceived(correct);
        });
    }

    private void OnStealAnswerReceived(bool isCorrect)
    {
        if (!isCorrect)
        {
            Debug.Log($"STEAL DUEL Player {duelCurrent} FAILED!");

            if (duelCurrent == duelAttacker)
            {
                Debug.Log("Attacker failed, turn ends.");
                StartCoroutine(SetLocalizedText(heistFailedText));
                isDuelActive = false;
                
                WaitForPlayer(controller,() =>
                {
                    NextTurn();
                });
            }
            else
            {
                Debug.Log("Defender failed, attacker steals the wedge!");
                StartCoroutine(SetLocalizedText(wedgeStolenText));
                GiveWedgeToAttacker(duelAttacker, duelDefender, duelColor);
                isDuelActive = false;

                WaitForPlayer(controller,() =>
                {
                    StartTurn();
                });
            }

            MusicManager.Instance.ResumeMusic(1);
            
            return;
        }

        Debug.Log($"STEAL DUEL Player {duelCurrent} CORRECT!");

        duelCurrent = (duelCurrent == duelAttacker) ? duelDefender : duelAttacker;

        ContinueStealDuel();
    }




    private QuesitoColor ConvertNameToColor(string name)
    {
        if (name.Contains("Azul")) return QuesitoColor.Azul;
        if (name.Contains("Rosa")) return QuesitoColor.Rosa;
        if (name.Contains("Amarillo")) return QuesitoColor.Amarillo; 
        if (name.Contains("Naranja")) return QuesitoColor.Naranja;
        if (name.Contains("Verde")) return QuesitoColor.Verde;
        if (name.Contains("Morado")) return QuesitoColor.Morado;

        Debug.LogWarning("Color no reconocido: " + name);
        return QuesitoColor.Azul;
    }

    #endregion

    #region Wedge Activation & Victory

    /// <summary>
    /// Activates a wedge for a player and checks win condition.
    /// </summary>
    private void ActivateWedgeForPlayer(int playerIndex, QuesitoColor color)
    {
        GameObject playerObj = playerWedgeContainers[playerIndex];

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
            Debug.LogWarning($"Could not find wedge {wedgeName} inside {playerObj}");
            return;
        }

        if (wedge.gameObject.activeSelf)
        {
            Debug.Log($"Wedge {wedgeName} already active for {playerObj}");
            return;
        }

        wedge.gameObject.SetActive(true);
        wedgesPerPlayer[playerIndex]++;

        if (!wedgesByPlayer.ContainsKey(playerIndex))
            wedgesByPlayer[playerIndex] = new List<QuesitoColor>();

        if (!wedgesByPlayer[playerIndex].Contains(color))
            wedgesByPlayer[playerIndex].Add(color);

        Debug.Log($"Team {teamNames[currentPlayerIndex]} has {wedgesPerPlayer[playerIndex]} wedges.");

        if (wedgesPerPlayer[playerIndex] >= 6)
        {
            gameEnded = true;
            StartCoroutine(SetLocalizedText(winText));

            StartCoroutine(WaitForClickToReturnToMenu());
        }
    }

    public void DeactivateWedgeForPlayer(int playerIndex, QuesitoColor color)
    {
        Transform panel = playerWedgeContainers[playerIndex].transform;

        foreach (Transform child in panel)
        {
            if (child.name.Contains(color.ToString()))
            {
                child.gameObject.SetActive(false);
                break;
            }
        }

        wedgesPerPlayer[playerIndex]--;
    }

    private void GiveWedgeToAttacker(int attacker, int defender, QuesitoColor color)
    {
        Debug.Log($"TRANSFER {defender} to {attacker} | Color = {color}");

        // Quitar al defensor
        if (wedgesByPlayer.ContainsKey(defender))
            wedgesByPlayer[defender].Remove(color);

        // Dar al atacante
        if (!wedgesByPlayer.ContainsKey(attacker))
            wedgesByPlayer[attacker] = new List<QuesitoColor>();

        wedgesByPlayer[attacker].Add(color);

        ActivateWedgeForPlayer(attacker, color);
        DeactivateWedgeForPlayer(defender, color);
    }

    #endregion

    #region Localization & UI

    private void InitialaizeLocation()
    {
        teamNameVar = new StringVariable { Value = teamNames[currentPlayerIndex] };
        teamColorVar = new StringVariable { Value = teamColors[currentPlayerIndex] };


        AddLocalizationVariables(turnText);
        AddLocalizationVariables(rollAgainText);
        AddLocalizationVariables(wedgeWinText);
        AddLocalizationVariables(rerollBoxText);
        AddLocalizationVariables(winText);
        AddLocalizationVariables(wedgeStolenText);
        AddLocalizationVariables(heistFailedText);
    }

    /// <summary>
    /// Adds team name and color variables to a LocalizedString.
    /// </summary>
    private void AddLocalizationVariables(LocalizedString localized)
    {
        localized.Add("teamName", teamNameVar);
        localized.Add("teamColor", teamColorVar);
  
    }

    private void InitialaizeStealLocation()
    {
        attackerVar = new StringVariable { Value = teamNames[duelAttacker] };
        attackerColorVar = new StringVariable { Value = teamColors[duelAttacker] };
        defenderVar = new StringVariable { Value = teamNames[duelDefender] };
        defenderColorVar = new StringVariable { Value = teamColors[duelDefender] };

        duelStartText.Add("attackerName", attackerVar);
        duelStartText.Add("attackerColor", attackerColorVar);

        duelStartText.Add("defenderName", defenderVar);
        duelStartText.Add("defenderColor", defenderColorVar);

        wedgeVar = new StringVariable { Value = wedgeCategory };
        wedgeColorVar = new StringVariable { Value = wedgeColors[wedgeColorsIndex] };

        duelStartText.Add("category", wedgeVar);
        duelStartText.Add("categoryColor", wedgeColorVar);
    }

    /// <summary>
    /// Updates the info text UI with localized content and blinking effect.
    /// </summary>
    public void UpdateInfoText(string newText)
    { 
        blinkTween?.Kill();
        infoText.alpha = 1f;

        infoText.text = newText;

        if (string.IsNullOrEmpty(newText))
        {
            infoText.DOFade(1f, 0.2f);
            return;
        }

        blinkTween = DOTween.Sequence()
            .AppendInterval(1.5f).Append(infoText.DOFade(0f, 0.5f)).AppendInterval(0.5f)                 
            .Append(infoText.DOFade(1f, 0.5f)).SetLoops(-1).SetEase(Ease.Linear);
    }

    /// <summary>
    /// Coroutine to localize text and apply it to the UI.
    /// </summary>
    private IEnumerator SetLocalizedText(LocalizedString localized)
    {
        yield return LocalizationSettings.InitializationOperation;
        string result = localized.GetLocalizedString();
        UpdateInfoText(result);

        yield return new WaitForSeconds(1);
    }

    #endregion

    #region SaveGame

    public Dictionary<int, List<QuesitoColor>> GetWedgesByPlayer()
    {
        return wedgesByPlayer;
    }

    public void SetWedgesByPlayer(Dictionary<int, List<QuesitoColor>> loadedData)
    {
        wedgesByPlayer = loadedData;

        // Reactivar visualmente los quesitos en cada jugador
        foreach (var kvp in wedgesByPlayer)
        {
            int playerIndex = kvp.Key;
            foreach (var color in kvp.Value)
            {
                Debug.Log($"Activating wedge {color} for player {playerIndex}");
                ActivateWedgeForPlayer(playerIndex, color);
            }
        }
    }

    public Dictionary<int, string> GetPlayerPositions()
    {
        Dictionary<int, string> positions = new Dictionary<int, string>();

        for (int i = 0; i < playerPieces.Count; i++)
        {
            if (playerPieces[i].currentNode != null)
                positions[i] = playerPieces[i].currentNode.nodeID; 
        }

        return positions;
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

    private IEnumerator PlaySoundDelayed (float timedelayed, int slot,Action callback)
    {
        yield return new WaitForSeconds (timedelayed);

        MusicManager.Instance.PauseMusic(slot);

        yield return new WaitForSeconds (1);

        callback?.Invoke();
    }

    #endregion

    #region INPUT HANDLERS

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

    public void WaitForPlayer(bool controller, Action callback)
    {
        tapCancelled = false;
        if (controller) { StartCoroutine(WaitForClickCoroutine(callback)); }
        else { StartCoroutine(WaitForTapCoroutine(callback)); }
    }

    private IEnumerator WaitForClickCoroutine(Action callback)
    {

        while (!Input.GetMouseButtonDown(0))
        {
            HandleEscapePause();
            if (tapCancelled)
                yield break;
            yield return null;
        }

        UpdateInfoText("");

        yield return new WaitForSeconds(0.3f);

        callback?.Invoke();
    }

    private IEnumerator WaitForTapCoroutine(Action callback)
    {
        pauseBtn.interactable = true;
        
        while (!IsShortTap())
        {
            if (tapCancelled)
                yield break;
            yield return null;
        }

        UpdateInfoText("");

        yield return new WaitForSeconds(0.3f);

        callback?.Invoke();
    }

    void HandleEscapePause()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ActivatePause();
        }
    }

    bool IsShortTap()
    {
        if (Input.touchCount == 0)
            return false;

        Touch touch = Input.GetTouch(0);

        switch (touch.phase)
        {
            case TouchPhase.Began:
                touchStartTime = Time.time;
                pauseTriggered = false;
                touchActive = true;
                break;

            case TouchPhase.Stationary:
            case TouchPhase.Moved:
                if (touchActive && !pauseTriggered)
                {
                    if (Time.time - touchStartTime >= 1f)
                    {
                        pauseTriggered = true;
                        ActivatePause();
                    }
                }
                break;

            case TouchPhase.Ended:
                if (!touchActive)
                    return false;

                touchActive = false;

                if (pauseTriggered)
                    return false;

                float duration = Time.time - touchStartTime;

                if (duration < 1f)
                {
                    return true;
                }
                break;

            case TouchPhase.Canceled:
                touchActive = false;
                pauseTriggered = false;
                break;
        }

        return false;
    }

    #endregion

    #region PAUSE

    void ActivatePause()
    {
        tapCancelled = true;
        UpdateInfoText("");
        ShowPausePanel();
    }

    #endregion

    #region PauseLogic

    public void ShowPausePanel()
    {
        pausePanel.SetActive(true);
    }

    public void OnExitSavePressed()
    {
        GameManager.Instance.PlayClickSound();
        MusicManager.Instance.StopMusic();
        GameSaveManager.Instance.SaveGame();

        Destroy(QuestionsManager.Instance.gameObject);
        Destroy(BoardManager.Instance.gameObject);

        GameManager.Instance.AnimateCameraAfterSceneLoad(mainCamera, new Vector3(0, 8, -10.7f), Quaternion.Euler(48.968f, 0f, 0f), "Menu");
    }

    public void OnContinuePressed()
    {
        GameManager.Instance.PlayClickSound();

        pausePanel.SetActive(false);

        StartTurn();
    }

    #endregion
}
