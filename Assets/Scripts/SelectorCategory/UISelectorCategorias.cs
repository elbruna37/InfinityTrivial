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
/// Maps a QuesitoColor to its Dropdown and 3D model in the scene.
/// </summary>
[Serializable]
public struct DropdownColorMap
{
    public QuesitoColor color;
    public TMP_Dropdown dropdown;
    public GameObject quesitoModel;
}

/// <summary>
/// Manages the UI for selecting categories for each colored wedge (quesito).
/// Handles dropdown selection, 3D animations, confirm/back buttons, and camera movement.
/// </summary>
public class UISelectorCategorias : MonoBehaviour
{
    #region Inspector References

    [Header("Managers")]
    [SerializeField] private QuestionsManager questionsManager;

    [Header("Dropdowns & Wedges")]
    [SerializeField] private DropdownColorMap[] dropdownColorMaps;

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

        _currentIndex = 0;

        _originalCategories = new List<string>(questionsManager.availableCategories);
        _selectionMap = new Dictionary<TMP_Dropdown, string>();

        StoreOriginalWedgeTransforms();

        InitializeDropdowns();

        AnimateWedge(dropdownColorMaps[0].quesitoModel, dropdownColorMaps[0].dropdown, 0);

        UpdateConfirmButton();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Saves original positions and rotations of all quesito models at scene start.
    /// </summary>
    private void StoreOriginalWedgeTransforms()
    {
        _originalQuesitoPositions = new Vector3[dropdownColorMaps.Length];
        _originalQuesitoRotations = new Quaternion[dropdownColorMaps.Length];

        for (int i = 0; i < dropdownColorMaps.Length; i++)
        {
            var quesito = dropdownColorMaps[i].quesitoModel;
            if (quesito != null)
            {
                _originalQuesitoPositions[i] = quesito.transform.position;
                _originalQuesitoRotations[i] = quesito.transform.rotation;
            }
        }
    }

    /// <summary>
    /// Activates the first dropdown, builds its options and sets listener.
    /// </summary>
    private void InitializeDropdowns()
    {
        for (int i = 0; i < dropdownColorMaps.Length; i++)
        {
            TMP_Dropdown dropdown = dropdownColorMaps[i].dropdown;
            dropdown.gameObject.SetActive(i == 0);
            _selectionMap[dropdown] = null;

            if (i == 0)
            {
                SetupDropdown(dropdown);
            }
        }
    }

    /// <summary>
    /// Prepares a dropdown with available options and registers OnValueChanged listener.
    /// </summary>
    private void SetupDropdown(TMP_Dropdown dropdown)
    {
        List<string> options = BuildOptionsFor(dropdown);
        dropdown.ClearOptions();
        dropdown.AddOptions(options);
        dropdown.SetValueWithoutNotify(0);

        TMP_Dropdown local = dropdown;
        dropdown.onValueChanged.RemoveAllListeners();
        dropdown.onValueChanged.AddListener((int value) => OnDropdownValueChanged(local, value));
    }

    #endregion

    #region Dropdown Selection Handling

    private void OnDropdownValueChanged(TMP_Dropdown dropdown, int value)
    {
        string selectedCategory = value == 0 ? null : dropdown.options[value].text;
        if (selectedCategory == null) return;

        backButton.SetActive(false);

        _selectionMap[dropdown] = selectedCategory;

        dropdown.gameObject.SetActive(false);

        if (_spinTween != null) _spinTween.Kill();

        MoveWedgeToBase(_currentIndex, () =>
        {
            _currentIndex++;

            if (_currentIndex < dropdownColorMaps.Length)
            {
                var nextMap = dropdownColorMaps[_currentIndex];
                TMP_Dropdown nextDropdown = nextMap.dropdown;
                nextDropdown.gameObject.SetActive(true);

                SetupDropdown(nextDropdown);

                AnimateWedge(nextMap.quesitoModel, nextDropdown, _currentIndex);
            }

            UpdateConfirmButton();
        });
    }

    #endregion

    #region Dropdown Options

    /// <summary>
    /// Builds the available category options for a dropdown excluding already selected ones.
    /// </summary>
    private List<string> BuildOptionsFor(TMP_Dropdown dropdown)
    {
        HashSet<string> blocked = new HashSet<string>(
            _selectionMap.Where(kv => kv.Key != dropdown && !string.IsNullOrEmpty(kv.Value))
                         .Select(kv => kv.Value)
        );

        List<string> available = _originalCategories.Where(c => !blocked.Contains(c)).ToList();

        List<string> options = new List<string> { defaultCategoryText.GetLocalizedString() };
        options.AddRange(available);

        return options;
    }

    /// <summary>
    /// Updates the confirm button interactable state and text color.
    /// </summary>
    private void UpdateConfirmButton()
    {
        bool allSelected = _selectionMap.Values.All(v => !string.IsNullOrEmpty(v));
        confirmButton.interactable = allSelected;

        TMP_Text text = confirmButton.GetComponentInChildren<TMP_Text>();
        if (text != null)
            text.color = allSelected ? Color.white : Color.gray;
    }

    #endregion

    #region Confirm / Back Buttons

    public void ConfirmCategories()
    {
        GameManager.Instance.PlayClickSound();

        canvas.SetActive(false);

        foreach (var map in dropdownColorMaps)
        {
            string category = _selectionMap[map.dropdown];
            if (!string.IsNullOrEmpty(category))
                GameManager.Instance.SetCategoryForColor(map.color, category);
        }

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

        for (int i = 0; i < dropdownColorMaps.Length; i++)
        {
            var map = dropdownColorMaps[i];
            GameObject quesito = map.quesitoModel;
            if (quesito == null) continue;

            DOTween.Kill(quesito.transform, complete: false);

            Tween t = ReturnWedgeToOriginalPosition(quesito, i);
            if (t != null) returnSeq.Join(t);
        }

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

    #region Undo Selection

    public void UndoSelection()
    {
        GameManager.Instance.PlayClickSound();

        if (_currentIndex <= 0) return;

        backButton.SetActive(false);

        // Return current quesito to original
        if (_currentIndex < dropdownColorMaps.Length)
        {
            var currentMap = dropdownColorMaps[_currentIndex];
            GameObject currentQuesito = currentMap.quesitoModel;
            if (currentQuesito != null)
            {
                DOTween.Kill(currentQuesito.transform, complete: false);
                currentQuesito.transform.DOMove(_originalQuesitoPositions[_currentIndex], 0.6f).SetEase(Ease.InOutQuad);
                currentQuesito.transform.DORotateQuaternion(_originalQuesitoRotations[_currentIndex], 0.6f).SetEase(Ease.InOutQuad);
            }

            currentMap.dropdown.gameObject.SetActive(false);
        }

        _currentIndex--;

        var prevMap = dropdownColorMaps[_currentIndex];
        TMP_Dropdown prevDropdown = prevMap.dropdown;
        GameObject prevQuesito = prevMap.quesitoModel;

        _selectionMap[prevDropdown] = null;

        SetupDropdown(prevDropdown);

        if (prevQuesito != null)
        {
            prevDropdown.gameObject.SetActive(false);
            DOTween.Kill(prevQuesito.transform, complete: false);

            Vector3 loopPos = _originalQuesitoPositions[_currentIndex] + Vector3.up * 1.5f;

            Sequence seq = DOTween.Sequence();
            seq.Append(prevQuesito.transform.DOMoveY(prevQuesito.transform.position.y + 1f, 0.4f).SetEase(Ease.OutQuad));
            seq.Append(prevQuesito.transform.DOMove(loopPos, 1f).SetEase(Ease.InOutQuad));
            seq.Join(prevQuesito.transform.DORotateQuaternion(Quaternion.Euler(0, 0, 90), 1f).SetEase(Ease.InOutQuad));

            seq.OnComplete(() =>
            {
                _spinTween = prevQuesito.transform.DORotate(new Vector3(0, 360, 0), 4f, RotateMode.WorldAxisAdd)
                                         .SetEase(Ease.Linear)
                                         .SetLoops(-1, LoopType.Restart);

                prevDropdown.gameObject.SetActive(true);
                if (_currentIndex > 0) backButton.SetActive(true);
            });
        }

        UpdateConfirmButton();
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

                MoveWedgeToBase(index, null);
            }
        });
    }

    /// <summary>
    /// Moves the quesito to its base circular position with rotation animation.
    /// Calls onComplete when animation finishes.
    /// </summary>
    private void MoveWedgeToBase(int index, Action onComplete)
    {
        GameObject quesito = dropdownColorMaps[index].quesitoModel;
        if (quesito == null) return;

        Vector3 basePos = _basePositions[index];
        basePos.y = quesito.transform.position.y;
        Quaternion baseRot = Quaternion.Euler(270, 0, 120 + (index * 60));

        quesito.transform.DOMove(basePos, 1.5f).SetEase(Ease.InOutQuad);
        quesito.transform.DORotateQuaternion(baseRot, 1.5f).SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                Vector3 finalPos = new Vector3(basePos.x, 0.18f, basePos.z);
                quesito.transform.DOMove(finalPos, 1f).SetEase(Ease.InOutQuad)
                    .OnComplete(() => onComplete?.Invoke());
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
