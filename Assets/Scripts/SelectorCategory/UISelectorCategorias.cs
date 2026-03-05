using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Localization;


/// <summary>
/// Manages the UI for selecting categories for each colored wedge (quesito).
/// Handles 3D animations, confirm/back buttons, and camera movement.
/// </summary>
public class UISelectorCategorias : MonoBehaviour
{
    #region Inspector References

    [Header("Managers")]
    [SerializeField] private QuestionsManager questionsManager;

    [Header("UI Elements")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private Button confirmButton;
    [SerializeField] private GameObject confirmButtons;
    [SerializeField] private GameObject quesitoMenuPanel;
    [SerializeField] private LocalizedString defaultCategoryText;

    [Header("Camera")]
    [SerializeField] private GameObject cameraObject;

    [Header("Animation")]
    [SerializeField] private float delayBetweenWedges = 0.5f;
    [SerializeField] private float flightDuration = 1.5f;
    [SerializeField] private float descendDuration = 0.3f;
    [SerializeField] private float arcHeight = 1.5f;

    public GameObject[] wedges;
    public CanvasGroup[] wedgeGroup;

    private static readonly Vector3[] _basePositions = new Vector3[]
    {
        new Vector3(-3.2f, 0f, 8.53f),  // Rosa
        new Vector3(-2.9f, 0f, 8.0f),   // Azul
        new Vector3(-3.2f, 0f, 7.5f),   // Verde
        new Vector3(-3.786f, 0f, 7.5f), // Amarillo
        new Vector3(-4.1f, 0f, 8.0f),   // Morado
        new Vector3(-3.784f, 0f, 8.528f) // Naranja
    };

    #endregion

    #region Unity Lifecycle

    private void Start()
    {
        canvas.SetActive(true);
    }

    #endregion

    #region Confirm / Back Buttons

    public void ConfirmCategories()
    {
        GameManager.Instance.PlayClickSound();
        canvas.SetActive(false);

        Sequence camSequence = AnimateWedges();

        camSequence.Append(cameraObject.transform.DOMove(new Vector3(0, 8.7f, 0), 1f).SetEase(Ease.InOutQuad));
        MusicManager.Instance.StopMusic();
        camSequence.Join(cameraObject.transform.DORotate(new Vector3(90, 360, 0), 1f, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));
        camSequence.OnComplete(() =>
        {
            //Destroy(QuestionsManager.Instance.gameObject);
            SceneManager.LoadScene("Game");
        });
    }

    public void BackToMenu()
    {
        GameManager.Instance.PlayClickSound();

        confirmButtons.SetActive(true);
        quesitoMenuPanel.SetActive(false);
    }

    public void CancelBackMenu()
    {
        GameManager.Instance.PlayClickSound();

        confirmButtons.SetActive(false);
        quesitoMenuPanel.SetActive(true);
    }

    public void ConfirmBackMenu()
    {
        GameManager.Instance.PlayClickSound();

        confirmButtons.SetActive(false);

        FadeOutWedgeTexts();

        Sequence returnSeq = DOTween.Sequence();

        returnSeq.OnComplete(() =>
        {
            if (QuestionsManager.Instance != null)
                Destroy(QuestionsManager.Instance.gameObject);

            GameManager.Instance.MoveObjectToPoint(
                cameraObject,
                new Vector3(0, 8, -10.7f),
                Quaternion.Euler(48.968f, 0f, 0f),
                "Menu"
            );
        });
    }

    #endregion

    #region Wedge Animation

    private Sequence AnimateWedges()
    {
        Sequence allSeq = DOTween.Sequence();

        for (int i = 0; i < Mathf.Min(6, _basePositions.Length); i++)
        {

            float dynamicArcHeight = arcHeight + (i * 0.18f);
            float dynamicDescendDuration = descendDuration - (i * 0.05f);
            float dynamicFlightDuration = flightDuration - (i * 0.05f);


            GameObject wedgeGO = wedges[i];
            Transform wedge = wedgeGO.transform;
            Vector3 startPos = wedge.position;
            Vector3 basePos = _basePositions[i];
            Vector3 arrivalPos = new Vector3(basePos.x, basePos.y + dynamicArcHeight, basePos.z);
            Quaternion baseRot = Quaternion.Euler(270f, 0f, 120f + (i * 60f));

            // puntos de control para la parábola
            Vector3 p0 = startPos;
            Vector3 center = new Vector3(-3.5f, 0.226f, 8);
            Vector3 fromCenter = (startPos - center).normalized;
            float lateralOffset = 1f * i;

            Vector3 p1 = new Vector3(
                (startPos.x + arrivalPos.x) / 2f,
                Mathf.Max(startPos.y, arrivalPos.y) + dynamicArcHeight,
                (startPos.z + arrivalPos.z) / 2f
            ) + fromCenter * lateralOffset;
            Vector3 p2 = arrivalPos;

            // Secuencia para cada wedge
            Sequence seq = DOTween.Sequence();

            // delay incremental (0.5s * índice)
            seq.PrependInterval(i * delayBetweenWedges);

            if (i < wedgeGroup.Length && wedgeGroup[i] != null)
            {
                CanvasGroup group = wedgeGroup[i];
                seq.AppendCallback(() =>
                {
                    group.DOFade(0f, 0.3f).SetEase(Ease.OutQuad);
                });
            }

            // --- Movimiento parabólico ---
            Tween flightTween = DOTween.To(() => 0f, t =>
            {
                float u = 1f - t;
                Vector3 pos = u * u * p0 + 2f * u * t * p1 + t * t * p2;
                wedge.position = pos;
            }, 1f, dynamicFlightDuration).SetEase(Ease.OutQuad);

            seq.Append(flightTween);

            // --- Rotación simultánea ---
            Tween rotTween = wedge.DORotateQuaternion(baseRot, dynamicFlightDuration).SetEase(Ease.OutQuad);
            seq.Join(rotTween);

            // --- Descenso final ---
            seq.Append(wedge.DOMoveY(basePos.y, dynamicDescendDuration).SetEase(Ease.InOutQuad));

            allSeq.Join(seq);
        }

        return allSeq;
    }

    void FadeOutWedgeTexts()
    {
        for (int i = 0; i < wedgeGroup.Length; i++)
        {
            CanvasGroup group = wedgeGroup[i];
            group.DOFade(0f, 0.3f).SetEase(Ease.OutQuad);
        }
    }

    void FadeInWedgeTexts()
    {
        for (int i = 0; i < wedgeGroup.Length; i++)
        {
            CanvasGroup group = wedgeGroup[i];
            group.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);
        }
    }

    #endregion
}
