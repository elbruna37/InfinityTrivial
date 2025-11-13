using System.Collections;
using UnityEngine;

/// <summary>
/// Handles spawning player pieces at the start of the game and initializing UI indicators.
/// </summary>
public class PlayerSpawner : MonoBehaviour
{
    [Header("Player Prefabs")]
    [Tooltip("Assign your player prefabs here.")]
    public GameObject[] playerPrefabs;

    [Header("Spawn Points")]
    [Tooltip("Initial positions on the board for each player.")]
    public Transform[] spawnPoints;

    [Header("UI Panels (Quesitos)")]
    [Tooltip("Panels showing player indicators.")]
    public GameObject[] quesitosPanel;

    /// <summary>
    /// Spawns all players at the start of the game and registers them in the TurnManager.
    /// If the game is loaded, activate the flag to move the player prefabs to their saved positions.
    /// </summary>
    void Start()
    {
        int numPlayers = Mathf.Clamp(GameManager.Instance.MaxPlayers, 0, Mathf.Min(playerPrefabs.Length, spawnPoints.Length));

        for (int i = 0; i < numPlayers; i++)
        {
            SpawnPlayer(i);
        }

        for (int i = 0; i < numPlayers; i++)
        {
            if (i < quesitosPanel.Length)
                quesitosPanel[i].SetActive(true);
        }

        if (GameManager.Instance.IsLoadingGame)
            StartCoroutine(LoadAfterSpawn());
    }

    /// <summary>
    /// Instantiates a player prefab at its spawn point and initializes its PlayerPiece.
    /// </summary>
    /// <param name="index">Index of the player to spawn.</param>
    private void SpawnPlayer(int index)
    {
        GameObject prefab = playerPrefabs[index];
        Transform spawn = spawnPoints[index];

        GameObject player = Instantiate(prefab, spawn.position, spawn.rotation);
        player.name = $"Player_{index + 1}";

        PlayerPiece piece = player.GetComponent<PlayerPiece>();
        if (piece != null)
        {
            piece.currentNode = BoardManager.Instance.startNode; // assign starting node
            BoardManager.Instance.startNode.OccupyNode(); // increment pieces in node instead of assigning directly
        }

        TurnManager.Instance.RegisterPlayer(piece, index);
    }

    private IEnumerator LoadAfterSpawn()
    {
        // Esperar un frame para garantizar que todo esté inicializado
        yield return null;

        GameSaveManager.Instance.LoadGame();
        GameManager.Instance.SetLoadingGame(false);
    }
}
