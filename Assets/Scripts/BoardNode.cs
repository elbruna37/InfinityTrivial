using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardNode : MonoBehaviour
{

    public List<BoardNode> neighbors;

    public List<PlayerPiece> actualPieces = new List<PlayerPiece>();

    public enum NodeType { Normal, Quesito, VolverATirar, Start }
    public NodeType nodeType = NodeType.Normal;


    public enum NodeColor { Ninguno, Blue, Pink, Yellow, Green, Orange, Purple }
    public NodeColor nodeColor = NodeColor.Ninguno;

    public bool IsIntersection => neighbors.Count > 2;
    public int piecesInNode = 0;

    [Header("Highlight")]
    public Renderer highlightRenderer;
    private Color baseColor;
    public Color highlightColor = new Color(1f, 1f, 0f, 0.5f); // amarillo translúcido

    private void Awake()
    {

        if (highlightRenderer != null)
            baseColor = GetColorFromNode(nodeColor);

        highlightColor = baseColor * 1.5f;
        highlightColor.a = 0.5f;

        Highlight(false);
    }

    public void OcupaNodo()
    {
        piecesInNode++;
    }


    public void LiberaNodo()
    {
        piecesInNode--;
    }

    public void Highlight(bool active)
    {

        highlightRenderer.gameObject.SetActive(active);

        highlightRenderer.enabled = active;

        highlightRenderer.material.DOKill(); // detener tweens anteriores

        if (active)
        {
            // Hace que pulse suavemente el alfa
            highlightRenderer.material
                .DOColor(highlightColor, 0.8f)
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        else
        {
            // Volver al color base y detener el loop
            highlightRenderer.material.color = baseColor;
        }
    }

    private Color GetColorFromNode(NodeColor color)
    {
        switch (color)
        {
            case NodeColor.Pink: return new Color(1f, 0.5f, 0.7f);       // Rosa claro
            case NodeColor.Blue: return new Color(0.4f, 0.6f, 1f);       // Azul suave
            case NodeColor.Green: return new Color(0.5f, 1f, 0.5f);      // Verde brillante
            case NodeColor.Yellow: return new Color(1f, 1f, 0.5f);     // Amarillo claro
            case NodeColor.Orange: return new Color(0.9f, 0.45f, 0.1f); // Naranja más oscuro
            case NodeColor.Purple: return new Color(0.7f, 0.5f, 1f);     // Morado suave
            // Default o Quesito → tono grisáceo oscuro y poco brillante
            default: return new Color(0.2f, 0.2f, 0.2f, 0.5f);
        }
    }

    private void OnMouseDown()
    {
        BoardManager.Instance.NotifyNodeClicked(this);
    }
}
