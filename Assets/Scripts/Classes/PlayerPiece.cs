using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPiece : MonoBehaviour
{
    public BoardNode currentNode;

    /// <summary>
    /// Instantly moves the player to the specified node (used for loading saves).
    /// </summary>
    /// <param name="node">Target board node.</param>
    public void MoveToNodeInstant(BoardNode node)
    {
        if (node == null)
        {
            Debug.LogWarning("MoveToNodeInstant called with a null node.");
            return;
        }

        // Liberar el nodo actual (si había uno)
        if (currentNode != null)
        {
            currentNode.ReleaseNode();
        }

        // Asignar y ocupar el nuevo nodo
        currentNode = node;
        transform.position = node.transform.position;
        node.OccupyNode();

        Debug.Log($"Moved player {gameObject.name} to node {node.name}");
    }
}

