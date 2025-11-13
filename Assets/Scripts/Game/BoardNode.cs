using DG.Tweening;
using System;
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

    private HashSet<int> appliedPlayerIndices = new HashSet<int>();

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

        TryApplySavedPositionForExistingPlayers();
    }

    private void OnMouseDown()
    {
        BoardManager.Instance.NotifyNodeClicked(this);
    }
    private void OnEnable()
    {
        // Subscribe to player registration to handle late-registered players
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPlayerRegistered += OnPlayerRegistered;
    }

    private void OnDisable()
    {
        if (TurnManager.Instance != null)
            TurnManager.Instance.OnPlayerRegistered -= OnPlayerRegistered;
    }

    private void TryApplySavedPositionForExistingPlayers()
    {
        var save = GameSaveManager.Instance?.LoadedSaveData;
        if (save == null || save.playerPositions == null) return;

        // Recorremos los pares playerIndex -> nodeID y aplicamos si coinciden con este nodeID
        foreach (var kvp in save.playerPositions)
        {
            int playerIndex = kvp.Key;
            string savedNodeID = kvp.Value;
            if (string.Equals(savedNodeID, nodeID, StringComparison.Ordinal))
            {
                // Si ese player ya está registrado, lo movemos
                if (TurnManager.Instance.TryGetPlayer(playerIndex, out var piece) && piece != null)
                {
                    if (!appliedPlayerIndices.Contains(playerIndex))
                    {
                        piece.MoveToNodeInstant(this);
                        appliedPlayerIndices.Add(playerIndex);
                        Debug.Log($"[BoardNode] Applied saved position for player {playerIndex} to node {nodeID}");
                    }
                }
                // si no está registrado, esperar al evento OnPlayerRegistered
            }
        }
    }

    // Handler del evento cuando un player se registra más tarde
    private void OnPlayerRegistered(int playerIndex, PlayerPiece piece)
    {
        var save = GameSaveManager.Instance?.LoadedSaveData;
        if (save == null || save.playerPositions == null) return;

        if (save.playerPositions.TryGetValue(playerIndex, out var savedNodeID))
        {
            if (string.Equals(savedNodeID, nodeID, StringComparison.Ordinal))
            {
                if (!appliedPlayerIndices.Contains(playerIndex))
                {
                    piece.MoveToNodeInstant(this);
                    appliedPlayerIndices.Add(playerIndex);
                    Debug.Log($"[BoardNode] Late-applied saved position for player {playerIndex} to node {nodeID}");
                }
            }
        }
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
