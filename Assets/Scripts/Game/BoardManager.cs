using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;
    public BoardNode startNode;

    [SerializeField] private List<BoardNode> allNodes = new List<BoardNode>();
    private Dictionary<string, BoardNode> nodeDictionary;
    private Dictionary<string, BoardNode> nodeLookup = new Dictionary<string, BoardNode>();

    private BoardNode _chosenNode = null;
    private bool _awaitingChoice = false;
    private readonly HashSet<BoardNode> _validChoices = new HashSet<BoardNode>();

    Color[] playerColors = { Color.green, Color.blue, Color.red, Color.yellow };

    private bool nodesInitialized = false;

    public bool AreNodesInitialized() => nodesInitialized;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        nodeDictionary = new Dictionary<string, BoardNode>();
        foreach (var node in allNodes)
        {
            if (node != null && !string.IsNullOrEmpty(node.nodeID))
            {
                if (!nodeDictionary.ContainsKey(node.nodeID))
                    nodeDictionary.Add(node.nodeID, node);
                else
                    Debug.LogWarning($"Duplicate node ID detected: {node.nodeID}");
            }
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeNodes();
    }



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
        HighlightNodes(reachable, true);

        // Esperar elección
        yield return StartCoroutine(ChooseNextNode(reachable));

        // Desiluminar
        HighlightNodes(reachable, false);

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

        Sequence moveSeq = DOTween.Sequence();
        float moveDuration = 0.5f;
        bool alreadyRepositioned = false;

        // Guardar nodo original
        BoardNode startNode = piece.currentNode;

        for (int i = 0; i < path.Count; i++)
        {
            var stepNode = path[i];
            bool isLast = (i == path.Count - 1);

            // Si no es la última y está ocupada, buscamos dónde debemos aterrizar:
            if (!isLast && stepNode.PiecesInNode > 0)
            {
                int jumpToIndex = -1;
                // Buscar el primer nodo vacio o el destino (último nodo)
                for (int j = i + 1; j < path.Count; j++)
                {
                    if (path[j].PiecesInNode == 0 || j == path.Count - 1)
                    {
                        jumpToIndex = j;
                        break;
                    }
                }

                if (jumpToIndex != -1 && jumpToIndex != i)
                {
                    var jumpTarget = path[jumpToIndex];
                    int casillasSaltadas = jumpToIndex - i;

                    // Calcular altura y duración dinámicas
                    float jumpHeight = 2.5f + casillasSaltadas * 1f;
                    float jumpDuration = 0.5f + casillasSaltadas * 0.25f;
                    // Si el nodo destino está ocupado, calcula offsets para que la ficha entrante aterrice en su offset
                    bool destinoOcupado = jumpTarget.actualPieces != null && jumpTarget.actualPieces.Count > 0;
                    Vector3 incomingPos = jumpTarget.transform.position;
                    Vector3[] offsets = null;
                    int incomingIndex = 0;

                    if (destinoOcupado)
                    {
                        offsets = GetOffsets(jumpTarget);
                        int existingCount = jumpTarget.actualPieces.Count;
                        incomingIndex = Mathf.Min(existingCount, offsets.Length - 1);
                        incomingPos = jumpTarget.transform.position + offsets[incomingIndex];
                    }

                    // Salto directamente al incomingPos (offset si estaba ocupado, centro si estaba vacío)
                    moveSeq.Append(
                        piece.transform
                             .DOJump(incomingPos, jumpHeight, 1, jumpDuration)
                             .SetEase(Ease.OutQuad)
                             .OnStart(() => GameManager.Instance.PlayJumpSound())
                    );

                    // Si el destino está ocupado, mueve las piezas existentes a sus offsets al mismo tiempo (Join),
                    // de modo que cuando la incoming aterrice, ya haya sitio.
                    if (destinoOcupado && offsets != null)
                    {
                        int existingCount = jumpTarget.actualPieces.Count;
                        float otherDuration = Mathf.Max(0.15f, jumpDuration * 0.9f);

                        for (int k = 0; k < existingCount && k < offsets.Length; k++)
                        {
                            var other = jumpTarget.actualPieces[k];
                            if (other == null) continue;

                            Vector3 target = jumpTarget.transform.position + offsets[k];

                            // join para que ocurra concurrentemente con la llegada de la incoming
                            moveSeq.Join(
                                other.transform
                                     .DOMove(target, otherDuration)
                                     .SetEase(Ease.OutQuad)
                            );
                        }
                    }

                    // Actualizar nodo actual al llegar
                    moveSeq.AppendCallback(() =>
                    {
                        piece.currentNode = jumpTarget;
                    });

                    // Avanzamos el índice para que el próximo ciclo continue desde después del jumpTarget.
                    i = jumpToIndex;
                    continue;
                }
            }

            if (isLast)
            {
                var destino = stepNode;
                bool occupied = destino.actualPieces != null && destino.actualPieces.Count > 0;

                if (occupied)
                {
                    // Obtener offsets y decidir posiciones
                    Vector3[] offsets = GetOffsets(destino);
                    int existingCount = destino.actualPieces.Count;
                    int incomingIndex = Mathf.Min(existingCount, offsets.Length - 1);
                    float finalDuration = Mathf.Max(0.25f, moveDuration);

                    Vector3 incomingPos = destino.transform.position + offsets[incomingIndex];

                    // Primero: movemos la pieza entrante directamente a su offset (Append)
                    moveSeq.Append(
                        piece.transform
                            .DOMove(incomingPos, finalDuration)
                            .SetEase(Ease.InOutQuad)
                            .OnStart(() => GameManager.Instance.PlayTokenSound())
                    );

                    // Hacemos que las piezas existentes se desplacen a sus offsets al mismo tiempo (Join)
                    for (int k = 0; k < existingCount && k < offsets.Length; k++)
                    {
                        var other = destino.actualPieces[k];
                        if (other == null) continue;

                        Vector3 target = destino.transform.position + offsets[k];
                        // join para que ocurra concurrentemente con la llegada de la incoming
                        moveSeq.Join(
                            other.transform
                                 .DOMove(target, finalDuration)
                                 .SetEase(Ease.OutQuad)
                        );
                    }

                    moveSeq.AppendCallback(() =>
                    {
                        piece.currentNode = destino;
                    });

                    // Indicamos que ya hicimos la recolocación visualmente
                    alreadyRepositioned = true;
                }
                else
                {
                    // Destino vacío → movimiento normal al centro
                    moveSeq.Append(
                        piece.transform
                            .DOMove(destino.transform.position, moveDuration)
                            .SetEase(Ease.InOutQuad)
                            .OnStart(() => GameManager.Instance.PlayTokenSound())
                    );

                    moveSeq.AppendCallback(() =>
                    {
                        piece.currentNode = destino;
                    });
                }

                // ya procesamos el último, salir del for
                continue;
            }

            // Si llegamos aquí: movimiento normal hacia stepNode
            moveSeq.Append(
                piece.transform
                    .DOMove(stepNode.transform.position, moveDuration)
                    .SetEase(Ease.InOutQuad)
                    .OnStart(() => GameManager.Instance.PlayTokenSound())
            );

            moveSeq.AppendCallback(() =>
            {
                piece.currentNode = stepNode;
            });
        }

        // Esperar a que termine la secuencia
        bool finished = false;
        moveSeq.OnComplete(() => finished = true);
        yield return new WaitUntil(() => finished);

        // Liberar nodo original
        RemovePieceFromNode(startNode, piece);

        // Ocupar nodo final y añadir la pieza
        var finalNode = piece.currentNode;
        finalNode.OccupyNode();
        finalNode.actualPieces.Add(piece);

        // Si no hubo recolocación en la secuencia, la hacemos ahora
        if (!alreadyRepositioned)
        {
            ReubicarPiezasEnNodo(finalNode);
        }
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
            //TurnManager.Instance.canDestroy=true;
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

    // ----------------- Reubicación -----------------

    private void ReubicarPiezasEnNodo(BoardNode nodo, PlayerPiece incomingPiece = null)
    {
        if (nodo == null || nodo.actualPieces == null) return;

        var piezas = nodo.actualPieces;

        // Si hay una pieza que todavía no se añadió oficialmente, la incluimos temporalmente
        if (incomingPiece != null && !piezas.Contains(incomingPiece))
            piezas.Add(incomingPiece);

        if (piezas.Count == 0) return;

        if (piezas.Count == 1)
        {
            VerificarRecentrado(nodo);
            return;
        }

        Vector3[] offsets = GetOffsets(nodo);

        for (int i = 0; i < piezas.Count && i < offsets.Length; i++)
        {
            var p = piezas[i];
            if (p == null) continue;

            Vector3 destino = nodo.transform.position + offsets[i];

            // Si es la pieza entrante, que vaya directamente a su offset
            float duracion = (p == incomingPiece) ? 0.1f : 0.25f;

            p.transform.DOKill();
            p.transform
                .DOMove(destino, duracion)
                .SetEase(Ease.OutQuad);
        }
    }

    private void VerificarRecentrado(BoardNode nodo)
    {
        if (nodo == null || nodo.actualPieces == null) return;

        var piezas = nodo.actualPieces;
        if (piezas.Count != 1) return;

        var pieza = piezas[0];
        if (pieza == null) return;

        Vector3 destino = nodo.transform.position;

        pieza.transform.DOKill();
        pieza.transform
            .DOMove(destino, 0.3f)
            .SetEase(Ease.OutQuad);

        Debug.Log($"[BoardManager] Recentrando pieza solitaria en nodo {nodo.name}");
    }

    private Vector3[] GetOffsets(BoardNode nodo)
    {
        float tamaño = 0.05f;
        var col = nodo.GetComponent<Collider>();
        if (col != null)
            tamaño = Mathf.Min(col.bounds.size.x, col.bounds.size.z) * 0.2f;

        return new Vector3[]
        {
        new Vector3(-tamaño * 1.5f, 0f, -tamaño * 1.2f),
        new Vector3( tamaño * 1.5f, 0f, -tamaño * 1.2f),
        new Vector3(-tamaño * 1.5f, 0f,  tamaño * 1.2f),
        new Vector3( tamaño * 1.5f, 0f,  tamaño * 1.2f)
        };
    }

    private void RemovePieceFromNode(BoardNode nodo, PlayerPiece piece)
    {
        if (nodo == null || piece == null || nodo.actualPieces == null) return;

        nodo.actualPieces.Remove(piece);
        nodo.ReleaseNode();

        // Verificar si quedó una sola pieza → recentrarla
        if (nodo.actualPieces.Count == 1)
            VerificarRecentrado(nodo);
    }

    private void HighlightNodes(IEnumerable<BoardNode> nodes, bool active)
    {
        Color color = playerColors[TurnManager.Instance.currentPlayerIndex];
        foreach (var node in nodes)
        {
            node.SetHighlight(active, color);
        }
    }

    /// <summary>
    /// Initializes all nodes and builds a lookup dictionary for quick access.
    /// </summary>
    private void InitializeNodes()
    {
        nodeLookup.Clear();

        foreach (var node in allNodes)
        {
            if (node == null) continue;

            // If the node doesn't have an ID, assign one automatically.
            if (string.IsNullOrEmpty(node.nodeID))
                node.nodeID = node.gameObject.name;

            // Avoid duplicates and add to lookup.
            if (!nodeLookup.ContainsKey(node.nodeID))
                nodeLookup.Add(node.nodeID, node);
            else
                Debug.LogWarning($"Duplicate node ID detected: {node.nodeID}");
        }

        nodesInitialized = true;
    }

    /// <summary>
    /// Returns the BoardNode corresponding to a given nodeID.
    /// </summary>
    public BoardNode GetNodeByID(string id)
    {
        if (nodeDictionary.TryGetValue(id, out var node))
            return node;

        Debug.LogWarning($"No node found with ID: {id}");
        return null;
    }
}