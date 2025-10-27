using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("Prefabs de jugadores")]
    public GameObject[] playerPrefabs; // arrastra aquí tus 4 prefabs en el inspector

    [Header("Puntos de spawn")]
    public Transform[] spawnPoints; // posiciones iniciales en el tablero

    [Header("UIQuesitos")]
    public GameObject[] quesitosPanel;


    void Start()
    {
        int numPlayers = GameManager.Instance.maxPlayer;

        for (int i = 0; i < numPlayers && i < playerPrefabs.Length && i < spawnPoints.Length; i++)
        {
            GameObject prefab = playerPrefabs[i];
            Transform spawn = spawnPoints[i];

            GameObject player = Instantiate(prefab, spawn.position, spawn.rotation);
            player.name = $"Player_{i + 1}";

            // Obtener componente PlayerPiece
            PlayerPiece piece = player.GetComponent<PlayerPiece>();
            if (piece != null)
            {
                piece.currentNode = BoardManager.Instance.startNode; // asignar nodo inicial
                BoardManager.Instance.startNode.piecesInNode = numPlayers;
            }

            // Registrar en TurnManager
            TurnManager.Instance.RegisterPlayer(piece);
        }

        for (int i = 0; i < numPlayers; i++)
        {
            // Activa solo los que están dentro del rango
            quesitosPanel[i].SetActive(true);
        }

    }
}

