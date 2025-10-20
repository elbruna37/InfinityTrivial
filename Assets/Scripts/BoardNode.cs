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
    private bool materialInstantiated = false;

    private void Awake()
    {
        if (highlightRenderer != null)
        {
            // Instanciar material para que no comparta con otros nodos
            if (!materialInstantiated)
            {
                highlightRenderer.material = new Material(highlightRenderer.material);
                materialInstantiated = true;
            }

            baseColor = highlightRenderer.material.color;
            Highlight(Color.white, false); // desactivar highlight al inicio
        }
    }

    public void OcupaNodo() => piecesInNode++;
    public void LiberaNodo() => piecesInNode--;

    // Highlight basado en el color del jugador actual
    public void Highlight(Color playerColor, bool active)
    {
        if (highlightRenderer == null) return;

        highlightRenderer.gameObject.SetActive(active);
        highlightRenderer.enabled = active;
        highlightRenderer.material.DOKill(); // detener cualquier tween anterior

        if (active)
        {
            Color transparentColor = new Color(playerColor.r, playerColor.g, playerColor.b, 0f);

            highlightRenderer.material
                .DOColor(transparentColor, 0.8f)
                .From(new Color(playerColor.r, playerColor.g, playerColor.b, 0.5f)) // desde alpha visible
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
        }
        else
        {
            // Restaurar color base
            highlightRenderer.material.color = baseColor;
        }
    }

    private void OnMouseDown()
    {
        BoardManager.Instance.NotifyNodeClicked(this);
    }
}
