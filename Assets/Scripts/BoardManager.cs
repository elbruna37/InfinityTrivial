using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager : MonoBehaviour
{
    public static BoardManager Instance;
    public BoardNode startNode;

    public GameObject tablero;

    private void Awake() => Instance = this;

    private BoardNode _chosenNode = null;
    private bool _awaitingChoice = false;
    private readonly HashSet<BoardNode> _validChoices = new HashSet<BoardNode>();

    // ----------------- Movimiento -----------------

    public IEnumerator MoveRoutine(PlayerPiece piece, int steps)
    {
        BoardNode node = piece.currentNode;

        BoardNode previousNode = null; // nodo de donde venimos

        while (steps > 0)
        {
            BoardNode nextNode = null;

            List<BoardNode> options = new List<BoardNode>();

            if (node.neighbors.Count > 1) // intersección
            {
                // filtrar nodo anterior para no volver atrás
                foreach (var n in node.neighbors)
                    if (n != previousNode)
                        options.Add(n);

                // iluminar opciones
                foreach (var n in options) n.Highlight(true);

                // esperar elección
                yield return StartCoroutine(ChooseNextNode(options));

                nextNode = _chosenNode;
                _chosenNode = null;

                // desiluminar
                foreach (var n in options) n.Highlight(false);

                if (nextNode == null)
                {
                    Debug.LogWarning("No se eligió ningún nodo. Cancelando movimiento.");
                    yield break;
                }
            }
            else if (node.neighbors.Count == 1) // camino único
            {
                nextNode = node.neighbors[0];
            }
            else
            {
                Debug.LogWarning("Nodo sin vecinos. ¿Falta conectar el tablero?");
                yield break;
            }

            // animar movimiento
            yield return StartCoroutine(MoveToPosition(piece.transform, nextNode.transform.position, 0.35f));

            // actualizar referencias
            previousNode = node;
            node = nextNode;
            piece.currentNode = node;
            steps--;
        }
    }

    private IEnumerator MoveToPosition(Transform obj, Vector3 target, float duration)
    {
        Vector3 start = obj.position;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            obj.position = Vector3.Lerp(start, target, t);
            yield return null;
        }
    }

    // ----------------- Elección de nodo -----------------
    private IEnumerator ChooseNextNode(List<BoardNode> options)
    {
        _awaitingChoice = true;
        _chosenNode = null;
        _validChoices.Clear();
        foreach (var n in options) _validChoices.Add(n);

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

    public void SacudirTablero()
    {
        StartCoroutine(Sacudir(0.5f,0.2f));
    }

    public IEnumerator Sacudir(float duration, float magnitude)
    {
        Vector3 originalPos = tablero.transform.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;

            tablero.transform.localPosition = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);

            elapsed += Time.deltaTime;
            yield return null;
        }

        tablero.transform.localPosition = originalPos;
    }
}
