using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardNode : MonoBehaviour
{

    public List<BoardNode> neighbors;


    public enum NodeType { Normal, Quesito, VolverATirar, Start }
    public NodeType nodeType = NodeType.Normal;


    public enum NodeColor { Ninguno, Blue, Pink, Yellow, Green, Orange, Purple }
    public NodeColor nodeColor = NodeColor.Ninguno;

    public bool IsIntersection => neighbors.Count > 2;

    [Header("Highlight")]
    public Color highlightColor = Color.yellow;
    private Color originalColor;

    private void Awake()
    {
        if (TryGetComponent<Renderer>(out Renderer rend))
            originalColor = rend.material.color;
    }

    public void Highlight(bool active)
    {
        if (TryGetComponent<Renderer>(out Renderer rend))
            rend.material.color = active ? highlightColor : originalColor;
    }

    private void OnMouseDown()
    {
        BoardManager.Instance.NotifyNodeClicked(this);
    }
}
