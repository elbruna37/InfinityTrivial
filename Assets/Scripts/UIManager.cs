using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Manages question UI flow: entrance card motion, displaying question text,
/// showing answer options, handling the countdown timer, feedback (color blink),
/// and returning the card state while invoking the answer callback.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    #region Inspector References

    [Header("Panels")]
    [SerializeField] private GameObject questionPanel;
    [SerializeField] private GameObject answersPanel;
    [SerializeField] private GameObject wedgePanel;
    [SerializeField] private RectTransform answersTopPanel;
    [SerializeField] private RectTransform answersBottomPanel;
    [SerializeField] private Image backgroundImage;

    [Header("Difficulty Indicator")]
    [SerializeField] private Image[] difficultyIcons;

    [Header("Timer")]
    [SerializeField] private Image timerFill;
    [SerializeField] private Transform timerNeedle;
    [SerializeField] private float questionDuration = 30f;
    [SerializeField] private float needleStartRotation = 0f;
    [SerializeField] private float needleEndRotation = -360f;

    [Header("Question Text")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private RectTransform questionTextRect;

    [Header("Answer Options")]
    [SerializeField] private Button[] optionButtons;
    [SerializeField] private TMP_Text[] optionTexts;

    [Header("Category Texts (Board)")]
    [SerializeField] private TMP_Text categoryBlueText;
    [SerializeField] private TMP_Text categoryYellowText;
    [SerializeField] private TMP_Text categoryOrangeText;
    [SerializeField] private TMP_Text categoryPinkText;
    [SerializeField] private TMP_Text categoryPurpleText;
    [SerializeField] private TMP_Text categoryGreenText;

    [Header("Card Motion")]
    [SerializeField] private Transform cardTransform;

    #endregion

    #region Private State

    private Color _originalBackgroundColor;
    private Question _currentQuestion;
    private Action<bool> _answerCallback;
    private Coroutine _questionCoroutine;

    private Vector3 _cardStartPosition;
    private Quaternion _cardStartRotation;

    // Tweens and sequences
    private Tween _needleTween;
    private Tween _fillTween;
    private Sequence _cardSequence;
    private Sequence _panelsSequence;
    private Sequence _cardReturnSequence;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Registers singleton instance.
    /// </summary>
    private void Awake()
    {
        Instance = this;
    }

    /// <summary>
    /// Captures initial card transform and loads category labels from GameManager.
    /// </summary>
    private void Start()
    {
        _cardStartPosition = cardTransform.position;
        _cardStartRotation = cardTransform.rotation;
        _originalBackgroundColor = backgroundImage.color;

        var categories = GameManager.Instance.GetAllCategories();
        UpdateCategoryTexts(categories);
    }

    #endregion

    #region Public API

    /// <summary>
    /// Starts the UI sequence to show a question and handle user answer.
    /// Side effects:
    ///  - Begins card animation.
    ///  - Starts timer and shows answer options after initial delay.
    ///  - Invokes onAnswered callback with true/false when flow completes.
    /// </summary>
    /// <param name="question">Question data to display.</param>
    /// <param name="difficulty">Difficulty string ("facil"/"media"/"dificil").</param>
    /// <param name="onAnswered">Callback to call when answer is processed.</param>
    public void ShowQuestion(Question question, string difficulty, Action<bool> onAnswered)
    {
        _currentQuestion = question;
        _answerCallback = onAnswered;

        DisplayDifficulty(difficulty);

        if (_questionCoroutine != null)
            StopCoroutine(_questionCoroutine);

        _questionCoroutine = StartCoroutine(QuestionFlowCoroutine());
    }

    #endregion

    #region Difficulty

    /// <summary>
    /// Shows a number of difficulty icons according to difficulty string.
    /// </summary>
    private void DisplayDifficulty(string difficulty)
    {
        // Hide all icons first
        for (int i = 0; i < difficultyIcons.Length; i++)
            difficultyIcons[i].gameObject.SetActive(false);

        int toShow = difficulty?.ToLower() switch
        {
            "facil" => 1,
            "media" => 2,
            "dificil" => 3,
            _ => 0
        };

        for (int i = 0; i < toShow && i < difficultyIcons.Length; i++)
            difficultyIcons[i].gameObject.SetActive(true);
    }

    #endregion

    #region Question Flow (Coroutine broken into steps)

    /// <summary>
    /// Orchestrates the full question UI flow:
    ///  1) Play card motion
    ///  2) Show the question text (big) for a short time
    ///  3) Shrink text and reveal answer panels
    ///  4) Start countdown and wait for selection / timeout
    /// If no selection occurs, treats as wrong answer.
    /// </summary>
    private IEnumerator QuestionFlowCoroutine()
    {
        PlayCardMotion();

        // Wait for the card to animate upward
        yield return new WaitForSeconds(5f);

        // Show question text panel
        EnterQuestionTextPhase();

        // Wait a bit to let user read big text
        yield return new WaitForSeconds(5f);

        // Animate size reduction of the question text smoothly (0.5s)
        yield return AnimateQuestionTextResize(70, 40, 0.5f);

        // Reveal answers and prepare buttons
        ShowAnswersPanel();

        // Start countdown timer and block until timeout or selection handled by OnOptionSelected
        float remaining = questionDuration;
        StartTimerAnimation(questionDuration);

        while (remaining > 0f)
        {
            yield return new WaitForSeconds(1f);
            remaining -= 1f;
        }

        // Time out → treat as wrong selection
        OnOptionSelected(-1);
    }

    /// <summary>
    /// Enters the visual phase where only the question text is visible.
    /// Sets UI state accordingly and populates the text field.
    /// </summary>
    private void EnterQuestionTextPhase()
    {
        wedgePanel.SetActive(false);

        backgroundImage.color = _originalBackgroundColor;
        questionPanel.SetActive(true);
        answersPanel.SetActive(false);

        questionText.fontSize = 70;
        questionText.text = _currentQuestion.enunciado;
        TurnManager.Instance.canDestroy = true;

        // pop-in scale for the question text rect
        questionTextRect.localScale = Vector3.zero;
        questionTextRect.DOScale(Vector3.one, 0.7f).SetEase(Ease.OutBack);
    }

    /// <summary>
    /// Smoothly animates the question font size from startSize to endSize over durationSeconds.
    /// </summary>
    private IEnumerator AnimateQuestionTextResize(float startSize, float endSize, float durationSeconds)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / durationSeconds;
            float eased = Mathf.SmoothStep(0f, 1f, t);
            questionText.fontSize = Mathf.RoundToInt(Mathf.Lerp(startSize, endSize, eased));
            yield return null;
        }

        questionText.fontSize = Mathf.RoundToInt(endSize);
    }

    /// <summary>
    /// Reveals the answer panels with a sliding animation and sets up option buttons.
    /// </summary>
    private void ShowAnswersPanel()
    {
        answersPanel.SetActive(true);

        // Build or reuse sequence to move top & bottom panels
        _panelsSequence?.Kill();
        _panelsSequence = DOTween.Sequence();
        _panelsSequence.Append(answersTopPanel.DOAnchorPosY(answersTopPanel.anchoredPosition.y - 483f, 0.3f).SetEase(Ease.OutQuad));
        _panelsSequence.Join(answersBottomPanel.DOAnchorPosY(answersBottomPanel.anchoredPosition.y + 483f, 0.3f).SetEase(Ease.OutQuad));
        _panelsSequence.Play();

        PrepareOptionButtons();
    }

    #endregion

    #region Option Handling

    /// <summary>
    /// Prepares option buttons with text, listeners and sets them interactable.
    /// </summary>
    private void PrepareOptionButtons()
    {
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (i < _currentQuestion.opciones.Length)
            {
                optionTexts[i].text = _currentQuestion.opciones[i];
                int index = i; // capture
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(index));
                optionButtons[i].interactable = true;
            }
            else
            {
                optionTexts[i].text = string.Empty;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].interactable = false;
            }
        }
    }

    /// <summary>
    /// Handles both player selection and timeout (-1).
    /// It:
    ///  - Kills timer tweens
    ///  - Disables all option buttons
    ///  - Plays correct/wrong feedback
    ///  - Starts the close-with-delay coroutine to wrap up the UI and call callback
    /// </summary>
    /// <param name="index">Index of selected option, or -1 on timeout.</param>
    private void OnOptionSelected(int index)
    {
        _needleTween?.Kill();
        _fillTween?.Kill();

        bool timedOut = index == -1;
        bool correct = !timedOut && index == _currentQuestion.indiceCorrecta;

        // Disable all buttons immediately
        foreach (var btn in optionButtons)
            btn.interactable = false;

        if (timedOut)
        {
            GameManager.Instance.PlayWrongSound();
            BlinkBackground(Color.red);
            if (_questionCoroutine != null) StopCoroutine(_questionCoroutine);
            _questionCoroutine = StartCoroutine(CloseWithDelay(false));
            return;
        }

        // Play feedback sounds
        if (correct) GameManager.Instance.PlayCorrectSound();
        else GameManager.Instance.PlayWrongSound();

        BlinkBackground(correct ? Color.green : Color.red);

        if (_questionCoroutine != null) StopCoroutine(_questionCoroutine);
        _questionCoroutine = StartCoroutine(CloseWithDelay(correct));
    }

    #endregion

    #region Visual Feedback & Timer

    /// <summary>
    /// Blinks the background between the original color and target color a few times.
    /// Final color is set back to the original when complete.
    /// </summary>
    private void BlinkBackground(Color targetColor)
    {
        int blinkRepeats = 3;
        float blinkDuration = 0.5f;

        Sequence seq = DOTween.Sequence();
        for (int i = 0; i < blinkRepeats; i++)
        {
            seq.Append(backgroundImage.DOColor(targetColor, blinkDuration / (2 * blinkRepeats)))
               .Append(backgroundImage.DOColor(_originalBackgroundColor, blinkDuration / (2 * blinkRepeats)));
        }

        // Ensure background returns to original color at the end
        seq.OnComplete(() => backgroundImage.color = _originalBackgroundColor);
    }

    /// <summary>
    /// Starts the needle rotation and fillAmount DOTween animations for the countdown.
    /// On completion calls OnOptionSelected(-1) to treat as timeout.
    /// </summary>
    /// <param name="durationSeconds">Duration for the countdown.</param>
    private void StartTimerAnimation(float durationSeconds)
    {
        // Kill any existing tweens on the UI elements
        DOTween.Kill(timerNeedle);
        DOTween.Kill(timerFill);

        GameManager.Instance.PlayTimerSound();

        timerNeedle.rotation = Quaternion.Euler(0f, 0f, needleStartRotation);
        timerFill.fillAmount = 0f;

        _needleTween = timerNeedle
            .DORotate(new Vector3(0f, 0f, needleEndRotation), durationSeconds, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear)
            .OnComplete(() => OnOptionSelected(-1));

        _fillTween = DOTween.To(() => timerFill.fillAmount, x => timerFill.fillAmount = x, 1f, durationSeconds)
            .SetEase(Ease.Linear);
    }

    #endregion

    #region Closing / Return Card Motion

    /// <summary>
    /// Waits a short delay to let feedback settle, then closes the question UI,
    /// plays the card return animation, and invokes the answer callback.
    /// </summary>
    private IEnumerator CloseWithDelay(bool wasCorrect)
    {
        yield return new WaitForSeconds(2f);
        FinalizeAnswer(wasCorrect);
    }

    /// <summary>
    /// Finalizes the answer UI: hides panels, plays card return animation and
    /// calls the answer callback when the card motion finishes.
    /// </summary>
    private void FinalizeAnswer(bool wasCorrect)
    {
        // Move panels back
        _panelsSequence?.Kill();
        _panelsSequence = DOTween.Sequence();
        _panelsSequence.Append(answersTopPanel.DOAnchorPosY(answersTopPanel.anchoredPosition.y + 483f, 0.3f).SetEase(Ease.OutQuad));
        _panelsSequence.Join(answersBottomPanel.DOAnchorPosY(answersBottomPanel.anchoredPosition.y - 483f, 0.3f).SetEase(Ease.OutQuad));
        _panelsSequence.Play();

        questionPanel.SetActive(false);
        answersPanel.SetActive(false);
        wedgePanel.SetActive(true);

        // Card return sequence
        _cardReturnSequence?.Kill();
        _cardReturnSequence = DOTween.Sequence();

        _cardReturnSequence.Append(cardTransform.DORotate(new Vector3(-90f, 90f, 0f), 0.5f, RotateMode.FastBeyond360).SetEase(Ease.OutBack));
        _cardReturnSequence.AppendInterval(0.25f);
        _cardReturnSequence.Append(cardTransform.DOMoveY(30f, 0.5f).SetEase(Ease.OutCubic));
        _cardReturnSequence.Append(cardTransform.DOMoveX(-10f, 0.5f).SetEase(Ease.OutCubic).OnComplete(() =>
        {
            // Reset transform to original state
            cardTransform.position = _cardStartPosition;
            cardTransform.rotation = _cardStartRotation;

            // Invoke callback
            _answerCallback?.Invoke(wasCorrect);
        }));
    }

    #endregion

    #region Card Motion (Entrance)

    /// <summary>
    /// Plays the long card entrance motion used before showing the question.
    /// This method creates and plays a DOTween sequence; it does not block.
    /// </summary>
    private void PlayCardMotion()
    {
        // Kill previous tweens/sequences
        cardTransform.DOKill();
        _cardSequence?.Kill();

        Vector3 initialEuler = cardTransform.eulerAngles;
        Vector3 midPos = new Vector3(0f, 14f, -10f);
        Vector3 highPos = new Vector3(0f, 50.31f, -10f);
        Vector3 finalRotation = new Vector3(90f, 90f, 0f);

        _cardSequence = DOTween.Sequence();

        // Step 1: move to mid and spin Z
        _cardSequence.Append(cardTransform.DOMove(midPos, 1.5f).SetEase(Ease.InOutCubic));
        _cardSequence.Join(cardTransform.DORotate(new Vector3(initialEuler.x, initialEuler.y, initialEuler.z + 720f), 1.5f, RotateMode.FastBeyond360).SetEase(Ease.InOutSine));
        _cardSequence.AppendInterval(0.25f);

        // Step 2: move higher and continue rotation
        _cardSequence.Append(cardTransform.DOMove(highPos, 2f).SetEase(Ease.OutCubic));
        _cardSequence.Join(cardTransform.DORotate(new Vector3(initialEuler.x + 1080f, initialEuler.y, initialEuler.z), 2f, RotateMode.FastBeyond360).SetEase(Ease.OutCubic));
        _cardSequence.AppendInterval(0.25f);

        // Step 3: final rotation
        _cardSequence.Append(cardTransform.DORotate(finalRotation, 0.5f).SetEase(Ease.OutBack));

        _cardSequence.Play();
    }

    #endregion

    #region Category Texts

    /// <summary>
    /// Updates the category labels placed on the board using the provided dictionary.
    /// </summary>
    public void UpdateCategoryTexts(Dictionary<QuesitoColor, string> categories)
    {
        if (categories.TryGetValue(QuesitoColor.Azul, out string blue))
            categoryBlueText.text = blue;

        if (categories.TryGetValue(QuesitoColor.Amarillo, out string yellow))
            categoryYellowText.text = yellow;

        if (categories.TryGetValue(QuesitoColor.Naranja, out string orange))
            categoryOrangeText.text = orange;

        if (categories.TryGetValue(QuesitoColor.Rosa, out string pink))
            categoryPinkText.text = pink;

        if (categories.TryGetValue(QuesitoColor.Morado, out string purple))
            categoryPurpleText.text = purple;

        if (categories.TryGetValue(QuesitoColor.Verde, out string green))
            categoryGreenText.text = green;
    }

    #endregion
}
