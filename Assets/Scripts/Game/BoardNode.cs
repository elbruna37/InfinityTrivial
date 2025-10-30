using DG.Tweening;
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

    private void OnMouseDown()
    {
        BoardManager.Instance.NotifyNodeClicked(this);
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
