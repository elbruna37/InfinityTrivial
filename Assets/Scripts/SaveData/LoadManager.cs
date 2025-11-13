using DG.Tweening;
using System;
using System.IO.IsolatedStorage;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Loads saved game data and applies stored category-color mappings 
/// before entering the game scene.
/// </summary>

public class LoadManager : MonoBehaviour
{
    #region Inspector References

    [Header("Managers")]
    [SerializeField] private QuestionsManager questionsManager;

    [Header("UI Elements")]
    [SerializeField] private GameObject canvas;
    [SerializeField] private GameObject confirmButtons;
    [SerializeField] private GameObject categorySaves;

    [Header("Camera")]
    [SerializeField] private GameObject cameraObject;

    [Header("Animation")]
    [SerializeField] private float delayBetweenWedges = 0.5f;
    [SerializeField] private float flightDuration = 1.5f;    
    [SerializeField] private float descendDuration = 0.3f;  
    [SerializeField] private float arcHeight = 1.5f;           

    private Vector3[] _originalWedgePositions;
    private Quaternion[] _originalWedgeRotations;

    public GameObject [] wedges;
    public TMP_Text[] wedgeTexts; 

    private static readonly Vector3[] _basePositions = new Vector3[]
    {
        new Vector3(-3.2f, 0.15f, 8.53f),  // Rosa
        new Vector3(-2.9f, 0.15f, 8.0f),   // Azul
        new Vector3(-3.2f, 0.15f, 7.5f),   // Verde
        new Vector3(-3.786f, 0.15f, 7.5f), // Amarillo
        new Vector3(-4.1f, 0.15f, 8.0f),   // Morado
        new Vector3(-3.784f, 0.15f, 8.528f) // Naranja
    };

    #endregion

    /// <summary>
    /// Loading the last saved game, color-coded category mapping, maxPlayer, and the history of questions used.
    /// </summary>
    void Start()
    {
        _originalWedgePositions = new Vector3[wedges.Length];
        _originalWedgeRotations = new Quaternion[wedges.Length];

        GameSaveManager.Instance.LoadCategories();

    }


    #region Confirm / Back Buttons

    public void ConfirmCategories()
    {
        GameManager.Instance.PlayClickSound();

        canvas.SetActive(false);

        Sequence wedgeSeq = AnimateWedges();

        wedgeSeq.OnComplete(() =>
        {
            Sequence camSequence = DOTween.Sequence();
            camSequence.Append(cameraObject.transform.DOMove(new Vector3(0, 8.7f, 0), 1f).SetEase(Ease.InOutQuad));
            camSequence.Join(cameraObject.transform.DORotate(new Vector3(90, 360, 0), 1f, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));
            camSequence.OnComplete(() => {
                SceneManager.LoadScene("Game");
                SceneManager.sceneLoaded += OnGameSceneLoaded;
            });
        });
    }

    private void OnGameSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game")
        {
            SceneManager.sceneLoaded -= OnGameSceneLoaded;
            //GameSaveManager.Instance.LoadGame();
        }
    }

    public void BackToMenu()
    {
        GameManager.Instance.PlayClickSound();

        confirmButtons.SetActive(true);
        categorySaves.SetActive(false);
    }

    public void CancelBackMenu()
    {
        GameManager.Instance.PlayClickSound();

        confirmButtons.SetActive(false);
        categorySaves.SetActive(true);
    }

    public void ConfirmBackMenu()
    {
        GameManager.Instance.PlayClickSound();

        ReturnAllWedgesToOriginalPositions();

        confirmButtons.SetActive(false);

        GameManager.Instance.MoveObjectToPoint(cameraObject, new Vector3(0, 8, -10.7f), Quaternion.Euler(48.968f, 0f, 0f), "Menu");
  
    }

    #endregion

    #region Wedge Animation

    private Sequence AnimateWedges()
    {
        Sequence allSeq = DOTween.Sequence();

        for (int i = 0; i < Mathf.Min(6, _basePositions.Length); i++)
        {
            _originalWedgePositions[i] = wedges[i].transform.position;
            _originalWedgeRotations[i] = wedges[i].transform.rotation;

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
            Vector3 center = new Vector3(-3.5f,0.226f,8); 
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

            if (i < wedgeTexts.Length && wedgeTexts[i] != null)
            {
                TMP_Text text = wedgeTexts[i];
                seq.AppendCallback(() =>
                {
                    text.DOFade(0f, 0.3f).SetEase(Ease.OutQuad);
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

    /// <summary>
    /// Returns a quesito to its original position and rotation with a smooth parabolic motion.
    /// </summary>
    public Tween ReturnAllWedgesToOriginalPositions()
    {

        Sequence fullSeq = DOTween.Sequence();
        float duration = 1f;
        Ease moveEase = Ease.InOutQuad;

        for (int i = 0; i < wedges.Length; i++)
        {
            GameObject wedge = wedges[i];
            if (wedge == null) continue;

            Vector3 currentPos = wedge.transform.position;
            Quaternion currentRot = wedge.transform.rotation;

            Vector3 basePos = _basePositions[i];
            Vector3 targetPos = _originalWedgePositions[i];
            Quaternion targetRot = _originalWedgeRotations[i];

            Tween wedgeTween;

            // Si el wedge está lejos, simplemente lo mueve directo
            if (Vector3.Distance(currentPos, basePos) > 0.3f)
            {
                Sequence seq = DOTween.Sequence();
                seq.Join(wedge.transform.DOMove(targetPos, duration).SetEase(moveEase));
                seq.Join(wedge.transform.DORotateQuaternion(targetRot, duration).SetEase(moveEase));
                wedgeTween = seq;
            }
            else
            {
                // Movimiento parabólico + rotación
                Sequence parabolicSeq = DOTween.Sequence();
                Vector3 liftPos = currentPos + Vector3.up * 1.5f;
                parabolicSeq.Append(wedge.transform.DOMove(liftPos, 0.35f).SetEase(Ease.OutQuad));

                float arcHeight = Mathf.Max(2f, Vector3.Distance(currentPos, targetPos) * 0.1f);
                Vector3 midPoint = Vector3.Lerp(liftPos, targetPos, 0.5f);
                midPoint.y += arcHeight;

                float jumpDuration = 1.0f;

                parabolicSeq.Append(wedge.transform.DOPath(
                    new Vector3[] { liftPos, midPoint, targetPos },
                    jumpDuration,
                    PathType.CatmullRom
                ).SetEase(Ease.InOutSine));

                parabolicSeq.Join(wedge.transform.DORotateQuaternion(targetRot, jumpDuration + 0.3f)
                                             .SetEase(Ease.InOutSine));

                wedgeTween = parabolicSeq;
            }

            // Añadimos cada tween al Sequence principal para que se ejecuten a la vez
            fullSeq.Join(wedgeTween);
        }

        return fullSeq;
    }

    #endregion
}
