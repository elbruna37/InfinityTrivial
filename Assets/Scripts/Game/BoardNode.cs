using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Represents a single node on the game board.
/// Manages player pieces on it, node type/color, intersections, and highlight animations.
/// </summary>
public class BoardNode : MonoBehaviour
{
    #region Inspector Fields

    [Header("Neighbors")]
    [Tooltip("Adjacent nodes connected to this node.")]
    public List<BoardNode> neighbors = new List<BoardNode>();

    [Header("Node Pieces")]
    [Tooltip("Player pieces currently occupying this node.")]
    public List<PlayerPiece> actualPieces = new List<PlayerPiece>();

    [Header("Node Configuration")]
    public NodeType nodeType = NodeType.Normal;
    public NodeColor nodeColor = NodeColor.Ninguno;
    public string nodeID;

    //Avoid applying the same position several times to the same player
    private HashSet<int> appliedPlayerIndices = new HashSet<int>();
    private bool subscribedToTurnManager = false;

    [Header("Highlight")]
    [Tooltip("Renderer used for highlighting this node.")]
    public Renderer highlightRenderer;

    #endregion

    #region Properties

    /// <summary>
    /// Determines if the node is an intersection (connected to more than 2 neighbors).
    /// </summary>
    public bool IsIntersection => neighbors.Count > 2;

    /// <summary>
    /// Number of pieces currently on this node.
    /// </summary>
    public int PiecesInNode { get; private set; } = 0;

    #endregion

    #region Node Types and Colors

    public enum NodeType { Normal, Wedge, Reroll, Start }
    public enum NodeColor { Ninguno, Blue, Pink, Yellow, Green, Orange, Purple }

    #endregion

    #region Private Fields

    private Color baseColor;
    private bool materialInstantiated = false;

    #endregion

    #region Unity Callbacks

    private void Awake()
    {
        InitializeHighlight();
    }

    /// <summary>
    /// Attempts to apply saved player positions after startup.
    /// Useful if TurnManager already exists when the scene loads.
    /// </summary>
    private void Start()
    {
        TryApplySavedPositions();          
    }

    /// <summary>
    /// Attempts to subscribe to the TurnManager event when this component becomes enabled.
    /// </summary>
    private void OnEnable()
    {
        TrySubscribeToTurnManager();      
    }

    private void OnDisable()
    {
        UnsubscribeFromTurnManager();    
    }

    private void OnMouseDown()
    {
        BoardManager.Instance.NotifyNodeClicked(this);
    }
    #endregion

    #region Subscribe Methods

    /// <summary>
    /// Attempts to register for TurnManager events.
    /// If TurnManager is not yet available, starts a coroutine to wait for it.
    /// </summary>
    private void TrySubscribeToTurnManager()
    {
        if (subscribedToTurnManager) return;

        if (TurnManager.Instance != null)
            Subscribe();
        else
            StartCoroutine(WaitForTurnManagerAndSubscribe());
    }

    private IEnumerator WaitForTurnManagerAndSubscribe()
    {
        const float timeout = 5f;
        float elapsed = 0f;

        while (TurnManager.Instance == null && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (TurnManager.Instance != null)
        {
            Subscribe();
            TryApplySavedPositions();      
        }
        else
        {
            Debug.LogWarning($"[BoardNode:{nodeID}] Timeout waiting for TurnManager.");
        }
    }

    private void Subscribe()
    {
        TurnManager.Instance.OnPlayerRegistered += OnPlayerRegistered;
        subscribedToTurnManager = true;
        Debug.Log($"[BoardNode:{nodeID}] Subscribed to OnPlayerRegistered.");
    }

    private void UnsubscribeFromTurnManager()
    {
        if (!subscribedToTurnManager || TurnManager.Instance == null) return;

        TurnManager.Instance.OnPlayerRegistered -= OnPlayerRegistered;
        subscribedToTurnManager = false;

        Debug.Log($"[BoardNode:{nodeID}] Unsubscribed from OnPlayerRegistered.");
    }

    /// <summary>
    /// Attempts to apply all saved player positions that correspond to this node.
    /// Only applies positions if the TurnManager and player pieces exist.
    /// </summary>
    private void TryApplySavedPositions()
    {
        var save = GameSaveManager.Instance?.LoadedSaveData;
        if (save?.playerPositions == null) return;

        foreach (var (playerIndex, savedNodeID) in save.playerPositions)
        {
            if (!IsMySavedNode(savedNodeID)) continue;

            if (TurnManager.Instance != null &&
                TurnManager.Instance.TryGetPlayer(playerIndex, out var piece))
            {
                ApplySavedPosition(playerIndex, piece);
            }
        }
    }


    /// <summary>
    /// Checks if the saved node ID matches this board node.
    /// </summary>
    /// <param name="savedNodeID">The node ID stored in the save file.</param>
    /// <returns>True if it matches this node's ID, otherwise false.</returns>
    private bool IsMySavedNode(string savedNodeID) =>
    string.Equals(savedNodeID, nodeID, StringComparison.Ordinal);

    /// <summary>
    /// Instantly moves the player piece to this node and records that this save
    /// position has already been applied.
    /// </summary>
    private void ApplySavedPosition(int playerIndex, PlayerPiece piece)
    {
        if (piece == null || appliedPlayerIndices.Contains(playerIndex)) return;

        piece.MoveToNodeInstant(this);
        appliedPlayerIndices.Add(playerIndex);
        BoardManager.Instance.ReubicarPiezasEnNodo(this, piece);

        Debug.Log($"[BoardNode:{nodeID}] Applied saved position for player {playerIndex}");
    }

    /// <summary>
    /// Handles late player registrations by applying saved position
    /// if this node matches the player's saved node.
    /// </summary>
    /// <param name="playerIndex">The index of the registered player.</param>
    /// <param name="piece">The player's piece instance.</param>
    private void OnPlayerRegistered(int playerIndex, PlayerPiece piece)
    {
        var save = GameSaveManager.Instance?.LoadedSaveData;
        if (save?.playerPositions == null) return;

        if (!save.playerPositions.TryGetValue(playerIndex, out var savedNodeID)) return;
        if (!IsMySavedNode(savedNodeID)) return;

        ApplySavedPosition(playerIndex, piece);
    }

    #endregion

    #region Node Occupancy

    /// <summary>
    /// Increment the number of pieces occupying this node.
    /// </summary>
    public void OccupyNode() => PiecesInNode++;

    /// <summary>
    /// Decrement the number of pieces occupying this node.
    /// </summary>
    public void ReleaseNode() => PiecesInNode--;

    #endregion

    #region Highlight Management

    /// <summary>
    /// Initializes the highlight material and sets it to the default state.
    /// </summary>
    private void InitializeHighlight()
    {
        if (highlightRenderer == null) return;

        if (!materialInstantiated)
        {
            // Instantiate material to avoid sharing with other nodes
            highlightRenderer.material = new Material(highlightRenderer.material);
            materialInstantiated = true;
        }

        baseColor = highlightRenderer.material.color;
        SetHighlight(false, Color.white);
    }

    /// <summary>
    /// Enables or disables the highlight effect on the node.
    /// Uses a pulsing tween based on the player's color when active.
    /// </summary>
    /// <param name="active">Whether the highlight should be active.</param>
    /// <param name="playerColor">The color associated with the player.</param>
    public void SetHighlight(bool active, Color playerColor)
    {
        if (highlightRenderer == null) return;

        highlightRenderer.gameObject.SetActive(active);
        highlightRenderer.enabled = active;
        highlightRenderer.material.DOKill(); // Stop any existing tweens

        if (active)
        {
            Color transparentColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0f);

            highlightRenderer.material
                .DOColor(transparentColor, 0.8f)
                .From(new Color(playerColor.r, playerColor.g, playerColor.b, 0.5f)) // start visible
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        else
        {
            // Restore base color
            highlightRenderer.material.color = baseColor;
        }
    }

    #endregion
}
