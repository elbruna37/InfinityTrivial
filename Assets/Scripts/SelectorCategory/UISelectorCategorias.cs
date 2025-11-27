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
/// Handles dropdown selection, 3D animations, confirm/back buttons, and camera movement.
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
    [SerializeField] private GameObject backButton;
    [SerializeField] private LocalizedString defaultCategoryText;

    [Header("Camera")]
    [SerializeField] private GameObject cameraObject;

    #endregion

    #region Private State

    private List<string> _originalCategories;
    private Dictionary<TMP_Dropdown, string> _selectionMap;
    private int _currentIndex = 0;

    private Tween _spinTween;

    private Vector3[] _originalQuesitoPositions;
    private Quaternion[] _originalQuesitoRotations;

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

        Sequence camSequence = DOTween.Sequence();
        camSequence.Append(cameraObject.transform.DOMove(new Vector3(0, 8.7f, 0), 1f).SetEase(Ease.InOutQuad));
        camSequence.Join(cameraObject.transform.DORotate(new Vector3(90, 360, 0), 1f, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));
        camSequence.OnComplete(() => SceneManager.LoadScene("Game"));
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

    #region Wedge Animations

    /// <summary>
    /// Animates a quesito rising + spinning + dropdown activation.
    /// </summary>
    private void AnimateWedge(GameObject quesito, TMP_Dropdown dropdown, int index)
    {
        if (quesito == null) return;

        Vector3 startPos = quesito.transform.position;
        Vector3 loopPos = startPos + Vector3.up * 1.5f;

        dropdown.gameObject.SetActive(false);

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(0.5f);
        seq.Append(quesito.transform.DOMove(loopPos, 1f).SetEase(Ease.OutQuad));
        seq.Join(quesito.transform.DORotate(new Vector3(0, 0, 90), 1f));

        seq.OnComplete(() =>
        {
            quesito.transform.DORotateQuaternion(Quaternion.Euler(0, 0, 90), 0f);
            _spinTween = quesito.transform.DORotate(new Vector3(0, 360, 0), 4f, RotateMode.WorldAxisAdd)
                                  .SetEase(Ease.Linear)
                                  .SetLoops(-1, LoopType.Restart);

            dropdown.gameObject.SetActive(true);
            if (_currentIndex > 0) backButton.SetActive(true);
        });

        bool waiting = true;
        dropdown.onValueChanged.AddListener((val) =>
        {
            if (val != 0 && waiting)
            {
                waiting = false;
                _spinTween.Kill();
            }
        });
    }

    /// <summary>
    /// Returns a quesito to its original position and rotation with a smooth parabolic motion.
    /// </summary>
    private Tween ReturnWedgeToOriginalPosition(GameObject quesito, int index)
    {
        if (quesito == null) return null;
        if (_originalQuesitoPositions == null || _originalQuesitoPositions.Length <= index) return null;

        Vector3 currentPos = quesito.transform.position;
        Quaternion currentRot = quesito.transform.rotation;

        Vector3 basePos = _basePositions[index];
        Vector3 targetPos = _originalQuesitoPositions[index];
        Quaternion targetRot = _originalQuesitoRotations[index];

        float duration = 1.2f;
        Ease moveEase = Ease.InOutQuad;

        if (Vector3.Distance(currentPos, basePos) > 0.3f)
        {
            Sequence seq = DOTween.Sequence();
            seq.Join(quesito.transform.DOMove(targetPos, duration).SetEase(moveEase));
            seq.Join(quesito.transform.DORotateQuaternion(targetRot, duration).SetEase(moveEase));
            return seq;
        }

        Sequence parabolicSeq = DOTween.Sequence();
        Vector3 liftPos = currentPos + Vector3.up * 1.5f;
        parabolicSeq.Append(quesito.transform.DOMove(liftPos, 0.35f).SetEase(Ease.OutQuad));

        float arcHeight = Mathf.Max(2f, Vector3.Distance(currentPos, targetPos) * 0.1f);
        Vector3 midPoint = Vector3.Lerp(liftPos, targetPos, 0.5f);
        midPoint.y += arcHeight;

        float jumpDuration = 1.0f;

        parabolicSeq.Append(quesito.transform.DOPath(new Vector3[] { liftPos, midPoint, targetPos }, jumpDuration, PathType.CatmullRom)
                                     .SetEase(Ease.InOutSine));

        parabolicSeq.Join(quesito.transform.DORotateQuaternion(targetRot, jumpDuration + 0.3f).SetEase(Ease.InOutSine));

        return parabolicSeq;
    }

    #endregion
}
