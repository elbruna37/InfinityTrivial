using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// Manages the game board, player piece movement, node selection, and piece positioning.
/// Works closely with BoardNode to handle node occupancy and highlight effects.
/// </summary>
public class BoardManager : MonoBehaviour
{
    #region Singleton

    public static BoardManager Instance;

    private void Awake() => Instance = this;

    #endregion

    #region Board References

    [Tooltip("Starting node for all pieces.")]
    public BoardNode startNode;

    private BoardNode _chosenNode = null;
    private bool _awaitingChoice = false;
    private readonly HashSet<BoardNode> _validChoices = new HashSet<BoardNode>();

    private readonly Color[] playerColors = { Color.green, Color.blue, Color.red, Color.yellow };

    #endregion

    #region Public Movement Methods

    /// <summary>
    /// Moves a player piece across the board along a valid path, handling jumps, collisions, and node occupancy.
    /// </summary>
    /// <param name="piece">Player piece to move.</param>
    /// <param name="steps">Number of steps to move.</param>
    public IEnumerator MoveRoutine(PlayerPiece piece, int steps)
    {
        if (piece == null || piece.currentNode == null) yield break;

        List<BoardNode> reachableNodes = GetReachableNodes(piece.currentNode, steps);
        if (reachableNodes.Count == 0)
        {
            Debug.LogWarning("No reachable nodes found.");
            yield break;
        }

        HighlightNodes(reachableNodes, true);
        yield return StartCoroutine(ChooseNextNode(reachableNodes));
        HighlightNodes(reachableNodes, false);

        if (_chosenNode == null)
        {
            Debug.LogWarning("No node was chosen.");
            yield break;
        }

        List<BoardNode> path = GetPath(piece.currentNode, _chosenNode, steps);
        if (path == null)
        {
            Debug.LogWarning("Path to destination not found.");
            yield break;
        }

        yield return StartCoroutine(AnimatePieceAlongPath(piece, path));

        // Occupy final node
        BoardNode finalNode = piece.currentNode;
        finalNode.OccupyNode();
        finalNode.actualPieces.Add(piece);
    }

    #endregion

    #region Node Selection

    /// <summary>
    /// Coroutine to wait for player selection of the next node.
    /// </summary>
    /// <param name="options">List of selectable nodes.</param>
    private IEnumerator ChooseNextNode(List<BoardNode> options)
    {
        _awaitingChoice = true;
        _chosenNode = null;
        _validChoices.Clear();

        foreach (var node in options)
            _validChoices.Add(node);

        yield return new WaitUntil(() => _chosenNode != null);

        _awaitingChoice = false;
        _validChoices.Clear();
    }

    /// <summary>
    /// Notifies the BoardManager that a node was clicked.
    /// </summary>
    /// <param name="node">Clicked node.</param>
    public void NotifyNodeClicked(BoardNode node)
    {
        if (_awaitingChoice && node != null && _validChoices.Contains(node))
        {
            _chosenNode = node;
        }
    }

    #endregion

    #region Node Calculations

    /// <summary>
    /// Returns all nodes reachable from a start node in a given number of steps.
    /// </summary>
    private List<BoardNode> GetReachableNodes(BoardNode start, int steps)
    {
        var result = new List<BoardNode>();
        var queue = new Queue<(BoardNode node, int remaining)>();
        var visited = new HashSet<BoardNode>();

        queue.Enqueue((start, steps));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, remaining) = queue.Dequeue();

            if (remaining == 0)
            {
                if (current != start) result.Add(current);
                continue;
            }

            foreach (var neighbor in current.neighbors)
            {
                if (!visited.Contains(neighbor))
                {
                    visited.Add(neighbor);
                    queue.Enqueue((neighbor, remaining - 1));
                }
            }
        }

        return result;
    }

    /// <summary>
    /// Returns a path from start to end node with an exact number of steps.
    /// </summary>
    private List<BoardNode> GetPath(BoardNode start, BoardNode end, int steps)
    {
        var queue = new Queue<(BoardNode node, List<BoardNode> path)>();
        queue.Enqueue((start, new List<BoardNode>()));

        while (queue.Count > 0)
        {
            var (current, path) = queue.Dequeue();

            if (path.Count > steps) continue;

            if (current == end && path.Count == steps) return path;

            foreach (var neighbor in current.neighbors)
            {
                if (!path.Contains(neighbor))
                {
                    var newPath = new List<BoardNode>(path) { neighbor };
                    queue.Enqueue((neighbor, newPath));
                }
            }
        }

        return null;
    }

    #endregion

    #region Piece Animation

    /// <summary>
    /// Animates a piece along a path, handling jumps, collisions, and offsets.
    /// </summary>
    private IEnumerator AnimatePieceAlongPath(PlayerPiece piece, List<BoardNode> path)
    {
        Sequence moveSeq = DOTween.Sequence();
        float moveDuration = 0.5f;
        bool alreadyRepositioned = false;
        BoardNode startNode = piece.currentNode;

        for (int i = 0; i < path.Count; i++)
        {
            BoardNode stepNode = path[i];
            bool isLast = (i == path.Count - 1);

            if (!isLast && stepNode.PiecesInNode > 0)
            {
                // Jump logic for occupied intermediate nodes
                int jumpIndex = FindNextAvailableNode(path, i);
                if (jumpIndex != -1 && jumpIndex != i)
                {
                    BoardNode jumpTarget = path[jumpIndex];
                    float jumpHeight = 2.5f + (jumpIndex - i);
                    float jumpDuration = 0.5f + (jumpIndex - i) * 0.25f;

                    Vector3 targetPos = GetTargetPositionWithOffsets(jumpTarget);
                    moveSeq.Append(piece.transform
                        .DOJump(targetPos, jumpHeight, 1, jumpDuration)
                        .SetEase(Ease.OutQuad)
                        .OnStart(() => GameManager.Instance.PlayJumpSound()));

                    moveSeq.AppendCallback(() => piece.currentNode = jumpTarget);
                    i = jumpIndex;
                    continue;
                }
            }

            Vector3 pos = isLast ? GetTargetPositionWithOffsets(stepNode) : stepNode.transform.position;
            moveSeq.Append(piece.transform
                .DOMove(pos, moveDuration)
                .SetEase(Ease.InOutQuad)
                .OnStart(() => GameManager.Instance.PlayTokenSound()));

            moveSeq.AppendCallback(() => piece.currentNode = stepNode);
        }

        bool finished = false;
        moveSeq.OnComplete(() => finished = true);
        yield return new WaitUntil(() => finished);

        RemovePieceFromNode(startNode, piece);
        if (!alreadyRepositioned)
            RepositionPiecesInNode(piece.currentNode);
    }

    #endregion

    #region Node Highlight Helpers

    private void HighlightNodes(IEnumerable<BoardNode> nodes, bool active)
    {
        Color color = playerColors[TurnManager.Instance.currentPlayerIndex];
        foreach (var node in nodes)
        {
            node.SetHighlight(active, color);
        }
    }

    #endregion

    #region Piece Positioning Helpers

    /// <summary>
    /// Repositions all pieces in a node to avoid overlaps.
    /// </summary>
    private void RepositionPiecesInNode(BoardNode node, PlayerPiece incomingPiece = null)
    {
        if (node == null || node.actualPieces == null) return;

        var pieces = node.actualPieces;

        if (incomingPiece != null && !pieces.Contains(incomingPiece))
            pieces.Add(incomingPiece);

        if (pieces.Count == 0) return;
        if (pieces.Count == 1)
        {
            RecenterSinglePiece(node);
            return;
        }

        Vector3[] offsets = GetOffsets(node);
        for (int i = 0; i < pieces.Count && i < offsets.Length; i++)
        {
            PlayerPiece p = pieces[i];
            if (p == null) continue;

            Vector3 target = node.transform.position + offsets[i];
            float duration = (p == incomingPiece) ? 0.1f : 0.25f;

            p.transform.DOKill();
            p.transform.DOMove(target, duration).SetEase(Ease.OutQuad);
        }
    }

    /// <summary>
    /// Recenters a single piece in a node.
    /// </summary>
    private void RecenterSinglePiece(BoardNode node)
    {
        if (node == null || node.actualPieces.Count != 1) return;

        PlayerPiece piece = node.actualPieces[0];
        piece.transform.DOKill();
        piece.transform.DOMove(node.transform.position, 0.3f).SetEase(Ease.OutQuad);

        Debug.Log($"[BoardManager] Recentering single piece on node {node.name}");
    }

    private Vector3 GetTargetPositionWithOffsets(BoardNode node)
    {
        if (node.actualPieces.Count == 0) return node.transform.position;

        Vector3[] offsets = GetOffsets(node);
        int index = Mathf.Min(node.actualPieces.Count, offsets.Length - 1);
        return node.transform.position + offsets[index];
    }

    private int FindNextAvailableNode(List<BoardNode> path, int startIndex)
    {
        for (int i = startIndex + 1; i < path.Count; i++)
        {
            if (path[i].PiecesInNode == 0 || i == path.Count - 1)
                return i;
        }
        return -1;
    }

    private Vector3[] GetOffsets(BoardNode node)
    {
        float size = 0.05f;
        var col = node.GetComponent<Collider>();
        if (col != null)
            size = Mathf.Min(col.bounds.size.x, col.bounds.size.z) * 0.2f;

        return new Vector3[]
        {
            new Vector3(-size * 1.5f,0f,-size*1.2f),
            new Vector3(size * 1.5f,0f,-size*1.2f),
            new Vector3(-size * 1.5f,0f,size*1.2f),
            new Vector3(size * 1.5f,0f,size*1.2f)
        };
    }

    private void RemovePieceFromNode(BoardNode node, PlayerPiece piece)
    {
        if (node == null || piece == null || node.actualPieces == null) return;

        node.actualPieces.Remove(piece);
        node.ReleaseNode();

        if (node.actualPieces.Count == 1)
            RecenterSinglePiece(node);
    }

    #endregion
}
