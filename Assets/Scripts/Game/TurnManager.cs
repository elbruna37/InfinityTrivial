using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.SmartFormat.PersistentVariables;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    public PreguntasManager preguntasManager;

    [Header("Referencias")]
    public DiceSpawner diceSpawner;      // Tiene el método SpawnAndThrowDice
    public BoardManager boardManager;    // Para mover al jugador después (más tarde)

    [Header("Fichas de jugadores")]
    List<PlayerPiece> playerPieces = new List<PlayerPiece>();
    private int[] quesitosPorJugador;

    public int currentPlayerIndex = 0;
    private bool isWaitingForClick = false;
    private bool gameEnded = false;
    public bool canDestroy = false;

    [Header("Localizacion")]
    [SerializeField] private LocalizedString turnText;
    [SerializeField] private LocalizedString rollAgain;
    [SerializeField] private LocalizedString wedgeWin;
    [SerializeField] private LocalizedString rerollBox;
    [SerializeField] private LocalizedString winText;

    public LocalizedString teamGreen;
    public LocalizedString teamBlue;
    public LocalizedString teamRed;
    public LocalizedString teamYellow;

    private StringVariable teamNameVar;
    private StringVariable teamColorVar;


    public TMP_Text textInformation;
    private float velocidadParpadeo = 2f;
    private Tween tweenParpadeo;

    string[] equipos;
    string[] color = { "green", "blue", "red", "yellow" };

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

    }

    private void Start()
    {
        equipos = new string[]
        {
            teamGreen.GetLocalizedString(),
            teamBlue.GetLocalizedString(),
            teamRed.GetLocalizedString(),
            teamYellow.GetLocalizedString()
        };

        quesitosPorJugador = new int[GameManager.Instance.maxPlayer];
        gameEnded = false;
        StartTurn();
    }

    private void StartTurn()
    {
        if (gameEnded) return;

        teamNameVar = new StringVariable { Value = equipos[currentPlayerIndex] };
        teamColorVar = new StringVariable { Value = color[currentPlayerIndex] };

        turnText.Add("teamName", teamNameVar);
        turnText.Add("teamColor", teamColorVar);

        rollAgain.Add("teamName", teamNameVar);
        rollAgain.Add("teamColor", teamColorVar);

        wedgeWin.Add("teamName", teamNameVar);
        wedgeWin.Add("teamColor", teamColorVar);

        rerollBox.Add("teamName", teamNameVar);
        rerollBox.Add("teamColor", teamColorVar);

        winText.Add("teamName", teamNameVar);
        winText.Add("teamColor", teamColorVar);


        StartCoroutine(SetTextFromLocalized(turnText));

        isWaitingForClick = true;   // ahora esperamos a que el jugador haga click
    }

    private void Update()
    {
        if (gameEnded) return;

        if (isWaitingForClick && (Input.GetMouseButtonDown(0) || Input.touchCount > 0)) // clic izquierdo
        {
            isWaitingForClick = false;
            ActualizarTexto("");
            diceSpawner.SpawnAndThrowDice(OnDiceResult);
        }
    }

    public void RegisterPlayer(PlayerPiece piece)
    {
        if (playerPieces == null)
            playerPieces = new List<PlayerPiece>();

        playerPieces.Add(piece);
    }

    // Callback que recibe el resultado del dado
    private void OnDiceResult(int result)
    {
        Debug.Log($"Jugador {currentPlayerIndex + 1} sacó un {result}");


        //mover la ficha del jugador actual
        if (playerPieces.Count > currentPlayerIndex)
        {
            PlayerPiece currentPiece = playerPieces[currentPlayerIndex];
            StartCoroutine(MoveAndWait(currentPiece, result));
        }
    }

    // Coroutine para esperar a que termine el movimiento
    private IEnumerator MoveAndWait(PlayerPiece piece, int steps)
    {

        yield return boardManager.MoveRoutine(piece, steps); 
        
        BoardNode landedNode = piece.currentNode;
        if (landedNode != null)
        {
            Debug.Log($"El jugador {piece.name} cayó en {landedNode.name} ({landedNode.nodeType}, {landedNode.nodeColor})");

            switch (landedNode.nodeType)
            {
                case BoardNode.NodeType.Normal:
                    Debug.Log("Casilla normal → buscando pregunta...");
                    string categoria = GameManager.Instance.GetCategoriaParaColor(ConvertNodeColorToQuesitoColor(landedNode.nodeColor));
                    
                    PreguntasManager.Instance.AskRandomQuestion(categoria, (bool correcta) =>
                    {
                        if (correcta)
                        {
                            StartCoroutine(SetTextFromLocalized(rollAgain));
                            isWaitingForClick = true;
                        }
                        if(!correcta)
                        {
                            NextTurn();
                        }
                    });

                    break;

                case BoardNode.NodeType.Quesito:
                    Debug.Log($"Casilla de Quesito → preguntar categoría {landedNode.nodeColor}");
                    categoria = GameManager.Instance.GetCategoriaParaColor(ConvertNodeColorToQuesitoColor(landedNode.nodeColor));

                    PreguntasManager.Instance.AskRandomQuesitoQuestion(categoria, (bool correcta) =>
                    {
                        if (correcta)
                        {
                            StartCoroutine(SetTextFromLocalized(wedgeWin));
                            ActivarQuesitoParaJugador(currentPlayerIndex, ConvertNodeColorToQuesitoColor(landedNode.nodeColor));
                            isWaitingForClick = true;
                        }
                        if(!correcta)
                        {
                            NextTurn();
                        }
                    });

                    break;

                case BoardNode.NodeType.VolverATirar:
                    canDestroy = true;
                    StartCoroutine(SetTextFromLocalized(rerollBox));
                    isWaitingForClick = true;
                    break;

                case BoardNode.NodeType.Start:
                    canDestroy= true;
                    NextTurn();
                    break;
            }
        }
    }

    private void NextTurn()
    {
        currentPlayerIndex++;

        if (currentPlayerIndex >= GameManager.Instance.maxPlayer)
            currentPlayerIndex = 0; // volver al jugador 1

        StartTurn();
    }

    private QuesitoColor ConvertNodeColorToQuesitoColor(BoardNode.NodeColor nodeColor)
    {
        switch (nodeColor)
        {
            case BoardNode.NodeColor.Blue: return QuesitoColor.Azul;
            case BoardNode.NodeColor.Pink: return QuesitoColor.Rosa;
            case BoardNode.NodeColor.Yellow: return QuesitoColor.Amarillo;
            case BoardNode.NodeColor.Green: return QuesitoColor.Verde;
            case BoardNode.NodeColor.Orange: return QuesitoColor.Naranja;
            case BoardNode.NodeColor.Purple: return QuesitoColor.Morado;
            default: throw new System.ArgumentException($"Color de nodo no soportado: {nodeColor}");
        }
    }

    private void ActivarQuesitoParaJugador(int playerIndex, QuesitoColor color)
    {
        // Construimos el nombre del objeto padre del jugador
        string jugadorName = $"QuesitoPlayer{playerIndex + 1}";
        GameObject jugador = GameObject.Find(jugadorName);
        if (jugador == null)
        {
            Debug.LogWarning($"No se encontró el objeto {jugadorName}");
            return;
        }

        // Construimos el nombre del quesito a activar según el color
        string quesitoName = color switch
        {
            QuesitoColor.Azul => "Quesito Azul",
            QuesitoColor.Rosa => "Quesito Rosa",
            QuesitoColor.Amarillo => "Quesito Amarillo",
            QuesitoColor.Verde => "Quesito Verde",
            QuesitoColor.Naranja => "Quesito Naranja",
            QuesitoColor.Morado => "Quesito Morado",
            _ => ""
        };

        if (string.IsNullOrEmpty(quesitoName)) return;

        // Buscar el quesito dentro del jugador
        Transform quesito = jugador.transform.Find(quesitoName);
        if (quesito == null)
        {
            Debug.LogWarning($"No se encontró el quesito {quesitoName} dentro de {jugadorName}");
            return;
        }

        if (!quesito.gameObject.activeSelf)
        {
            quesito.gameObject.SetActive(true);

            // --- Comprobación de victoria ---
            
            quesitosPorJugador[playerIndex]++;
                
            
            Debug.Log($"El equipo {equipos[currentPlayerIndex]} tiene {quesitosPorJugador[playerIndex]} puntos");


            if (quesitosPorJugador[playerIndex] >= 6)
            {
                gameEnded = true;
                StartCoroutine(SetTextFromLocalized(winText));
                isWaitingForClick = false;
                StartCoroutine(WaitForClickToReturnMenu());
            }
        }
        else
        {
            Debug.Log($"El quesito {quesitoName} ya estaba activado para {jugadorName}. No se suma punto.");
        }
    }

    private IEnumerator SetTextFromLocalized(LocalizedString localized)
    {
        // Espera la inicialización del sistema si hace falta
        yield return LocalizationSettings.InitializationOperation;

        string s = localized.GetLocalizedString();
        ActualizarTexto(s);
    }

    public void ActualizarTexto(string nuevoTexto)
    {
        textInformation.text = nuevoTexto;

        // Detenemos cualquier tween anterior
        tweenParpadeo?.Kill();

        if (string.IsNullOrEmpty(nuevoTexto))
        {
            // Si no hay texto, se asegura de que esté completamente visible
            textInformation.DOFade(1f, 0.2f);
            return;
        }

        // Crear el parpadeo
        tweenParpadeo = textInformation
            .DOFade(0f, velocidadParpadeo / 3f)
            .SetLoops(-1, LoopType.Yoyo)
            .SetEase(Ease.InOutSine);
    }
    private IEnumerator WaitForClickToReturnMenu()
    {

        while (!(Input.GetMouseButtonDown(0) || Input.touchCount > 0))
        {
            yield return null;
        }

        Destroy(GameManager.Instance.gameObject);
        Destroy(PreguntasManager.Instance.gameObject);
        Destroy(UIManager.Instance.gameObject);

        SceneManager.LoadScene("Menu");
    }

}


