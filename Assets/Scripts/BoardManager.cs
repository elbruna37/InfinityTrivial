using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;
    public BoardNode startNode;

    private void Awake() => Instance = this;

    private BoardNode _chosenNode = null;
    private bool _awaitingChoice = false;
    private readonly HashSet<BoardNode> _validChoices = new HashSet<BoardNode>();

    // ----------------- Movimiento -----------------

    public IEnumerator MoveRoutine(PlayerPiece piece, int steps)
    {
        // Obtener nodos alcanzables
        List<BoardNode> reachable = GetReachableNodes(piece.currentNode, steps);
        if (reachable.Count == 0)
        {
            Debug.LogWarning("No hay nodos alcanzables.");
            yield break;
        }

        // Iluminar opciones
        foreach (var node in reachable)
            node.Highlight(true);

        // Esperar elección
        yield return StartCoroutine(ChooseNextNode(reachable));

        // Desiluminar
        foreach (var node in reachable)
            node.Highlight(false);

        if (_chosenNode == null)
        {
            Debug.LogWarning("No se eligió destino.");
            yield break;
        }

        // Calcular camino hasta el destino
        List<BoardNode> path = GetPath(piece.currentNode, _chosenNode, steps);
        if (path == null)
        {
            Debug.LogWarning("No se pudo encontrar camino.");
            yield break;
        }

        // Crear secuencia DoTween
        Sequence moveSeq = DOTween.Sequence();
        float moveDuration = 0.5f;

        foreach (var stepNode in path)
        { 
            moveSeq.Append(
                piece.transform.DOMove(stepNode.transform.position, moveDuration)
                .SetEase(Ease.InOutQuad)
                .OnStart(() => GameManager.Instance.AudioFicha())
                .OnComplete(() => piece.currentNode = stepNode)
            );
        }

        // Esperar a que termine la secuencia
        bool finished = false;
        moveSeq.OnComplete(() => finished = true);

        yield return new WaitUntil(() => finished);
    }

    // ----------------- Eleccion de Nodo -----------------
    private IEnumerator ChooseNextNode(List<BoardNode> options)
    {
        _awaitingChoice = true;
        _chosenNode = null;
        _validChoices.Clear();
        foreach (var n in options)
            _validChoices.Add(n);

        yield return new WaitUntil(() => _chosenNode != null);

        _awaitingChoice = false;
        _validChoices.Clear();
    }

    public void NotifyNodeClicked(BoardNode node)
    {
        if (_awaitingChoice && node != null && _validChoices.Contains(node))
        {
            _chosenNode = node;
        }
    }

    // ----------------- Calculo de Nodo -----------------
    private List<BoardNode> GetReachableNodes(BoardNode start, int steps)
    {
        List<BoardNode> result = new List<BoardNode>();
        Queue<(BoardNode node, int remaining)> queue = new Queue<(BoardNode, int)>();
        HashSet<BoardNode> visited = new HashSet<BoardNode>();

        queue.Enqueue((start, steps));
        visited.Add(start);

        while (queue.Count > 0)
        {
            var (current, remaining) = queue.Dequeue();

            if (remaining == 0)
            {
                if (current != start)
                    result.Add(current);
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

    // ----------------- Camino hasta destino -----------------

    private List<BoardNode> GetPath(BoardNode start, BoardNode end, int steps)
    {
        Queue<(BoardNode node, List<BoardNode> path)> queue = new Queue<(BoardNode, List<BoardNode>)>();
        queue.Enqueue((start, new List<BoardNode>()));

        while (queue.Count > 0)
        {
            var (current, path) = queue.Dequeue();

            if (path.Count > steps)
                continue;

            if (current == end && path.Count == steps)
                return path;

            foreach (var neighbor in current.neighbors)
            {
                if (!path.Contains(neighbor))
                {
                    List<BoardNode> newPath = new List<BoardNode>(path) { neighbor };
                    queue.Enqueue((neighbor, newPath));
                }
            }
        }

        return null;
    }
}
