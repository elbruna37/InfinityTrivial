using DG.Tweening;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Manages importing trivia questions from JSON,
/// validates input, updates UI, handles card animations, and keeps import history.
/// </summary>
public class ImporterManager : MonoBehaviour
{
    #region Inspector References

    [Header("3D Card & Camera")]
    [SerializeField] private GameObject card;
    [SerializeField] private GameObject cameraObject;

    [Header("UI Panels")]
    [SerializeField] private CanvasGroup instructionsPanel;
    [SerializeField] private CanvasGroup importMenuPanel;
    [SerializeField] private CanvasGroup historyMenuPanel;

    [Header("UI / COPY")]
    [SerializeField] private TMP_InputField questionCountInput;
    [SerializeField] private TMP_InputField categoryInput;
    [SerializeField] private TMP_Dropdown difficultyDropdown;
    [SerializeField] private Button copyButton;

    [Header("UI / PASTE")]
    [SerializeField] private TMP_InputField jsonInput;

    [Header("UI / HISTORY")]
    [SerializeField] private Transform contentParent;
    [SerializeField] private GameObject itemPrefab;

    [Header("Localization")]
    [SerializeField] private LocalizedString difficultyEasyOption;
    [SerializeField] private LocalizedString difficultyMediumOption;
    [SerializeField] private LocalizedString difficultyHardOption;

    #endregion

    #region Private State

    private string questionsFilePath;
    private string historyFilePath;

    private Vector3 startCardPosition;
    private Quaternion startCardRotation;

    private string localizationText;

    [Serializable]
    private class QuestionArrayWrapper
    {
        public Question[] preguntas;
    }

    [Serializable]
    public class CategoryData
    {
        public string nombre;
        public int faciles;
        public int medias;
        public int dificiles;
    }

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes file paths, input fields, copy button, and plays initial card animation.
    /// </summary>
    private void Awake()
    {
        startCardPosition = card.transform.position;
        startCardRotation = card.transform.rotation;

        questionsFilePath = Path.Combine(Application.persistentDataPath, "preguntas.json");
        historyFilePath = Path.Combine(Application.persistentDataPath, "historial_importadas.json");

        InitializeFiles();
        SetupInputFields();
        SetupCopyButton();

        AnimateCardInitial();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Ensures the question JSON and history JSON exist.
    /// If not, initializes them from Resources or empty JSON.
    /// </summary>
    private void InitializeFiles()
    {
        if (!File.Exists(questionsFilePath))
        {
            TextAsset baseFile = Resources.Load<TextAsset>("preguntas");
            if (baseFile != null)
                File.WriteAllText(questionsFilePath, baseFile.text);
        }

        if (!File.Exists(historyFilePath))
        {
            File.WriteAllText(historyFilePath, "{\"preguntas\":[]}");
        }
    }

    /// <summary>
    /// Sets character limits and listeners for input validation.
    /// </summary>
    private void SetupInputFields()
    {
        if (questionCountInput != null)
        {
            questionCountInput.characterLimit = 2;
            questionCountInput.onValueChanged.AddListener(ValidateNumberInput);
        }

        if (categoryInput != null)
        {
            categoryInput.characterLimit = 20;
            categoryInput.onValueChanged.AddListener(ValidateTextInput);
        }
    }

    /// <summary>
    /// Assigns copy prompt functionality to the copy button.
    /// </summary>
    private void SetupCopyButton()
    {
        if (copyButton != null)
            copyButton.onClick.AddListener(CopyPromptToClipboard);
    }

    #endregion

    #region Input Validation

    /// <summary>
    /// Ensures that only numeric characters are entered in the question count input.
    /// </summary>
    /// <param name="value">The current input string.</param>
    private void ValidateNumberInput(string value)
    {
        string onlyNumbers = Regex.Replace(value, @"[^0-9]", "");
        if (onlyNumbers != value) questionCountInput.text = onlyNumbers;
    }

    /// <summary>
    /// Ensures that only letters and spaces are entered in the category input.
    /// </summary>
    /// <param name="value">The current input string.</param>
    private void ValidateTextInput(string value)
    {
        string onlyLetters = Regex.Replace(value, @"[^a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s]", "");
        if (onlyLetters != value) categoryInput.text = onlyLetters;
    }

    #endregion

    #region Copy Prompt

    /// <summary>
    /// Builds a JSON prompt based on user inputs and copies it to the system clipboard.
    /// </summary>
    private void CopyPromptToClipboard()
    {
        string questionCount = questionCountInput != null ? questionCountInput.text : "10";
        string category = categoryInput != null ? categoryInput.text : "General";

        string language = LocalizationSettings.SelectedLocale?.Identifier.CultureInfo.NativeName ?? "español";
        string difficulty = difficultyDropdown != null ? GetDifficultyText(difficultyDropdown.value) : "fácil";

        string prompt = $"Genera {questionCount} preguntas de trivial en {language} en formato JSON con el siguiente esquema:\n\n" +
                        $"categoria: \"{category}\".\n" +
                        $"enunciado: string con la pregunta.\n\n" +
                        $"opciones: array de 4 respuestas posibles.\n\n" +
                        $"indiceCorrecta: índice 0–3 correcto.\n\n" +
                        $"dificultad: \"{difficulty}\"\n" +
                        $"Devuelve un array JSON de objetos, sin explicaciones.";

        GUIUtility.systemCopyBuffer = prompt;
        Debug.Log("✅ Prompt copiado al portapapeles:\n" + prompt);
    }

    /// <summary>
    /// Returns the difficulty string based on dropdown index.
    /// </summary>
    /// <param name="dropdownIndex">The dropdown selected index.</param>
    /// <returns>The difficulty string.</returns>
    private string GetDifficultyText(int dropdownIndex)
    {
        return dropdownIndex switch
        {
            0 => "fácil",
            1 => "media",
            2 => "difícil",
            _ => "fácil"
        };
    }

    #endregion

    #region Import Questions

    /// <summary>
    /// Imports questions from the pasted JSON input and appends them to existing questions and history.
    /// </summary>
    public void ImportQuestions()
    {
        string rawJson = jsonInput.text.Trim();
        if (string.IsNullOrEmpty(rawJson))
        {
            Debug.LogWarning("No se ha pegado ningún JSON");
            return;
        }

        QuestionBatch existingBatch = LoadExistingQuestions();
        List<Question> questionsList = new List<Question>(existingBatch.questions);

        if (rawJson.StartsWith("["))
            rawJson = "{\"preguntas\":" + rawJson + "}";

        QuestionArrayWrapper newQuestionsWrapper = JsonUtility.FromJson<QuestionArrayWrapper>(rawJson);
        if (newQuestionsWrapper?.preguntas == null)
        {
            Debug.LogError("El JSON pegado no es válido o no tiene preguntas");
            return;
        }

        questionsList.AddRange(newQuestionsWrapper.preguntas);
        existingBatch.questions = questionsList.ToArray();

        File.WriteAllText(questionsFilePath, JsonUtility.ToJson(existingBatch, true));
        Debug.Log($"Total de preguntas ahora: {existingBatch.questions.Length}");

        jsonInput.text = "";

        UpdateImportHistory(newQuestionsWrapper.preguntas);
    }

    /// <summary>
    /// Loads the existing questions JSON file into a QuestionBatch object.
    /// </summary>
    /// <returns>The loaded QuestionBatch.</returns>
    private QuestionBatch LoadExistingQuestions()
    {
        string json = File.ReadAllText(questionsFilePath);
        return JsonUtility.FromJson<QuestionBatch>(json);
    }

    /// <summary>
    /// Updates the history JSON file with newly imported questions.
    /// </summary>
    /// <param name="newQuestions">The array of newly imported questions.</param>
    private void UpdateImportHistory(Question[] newQuestions)
    {
        string historyJson = File.ReadAllText(historyFilePath);
        QuestionArrayWrapper historyWrapper = JsonUtility.FromJson<QuestionArrayWrapper>(historyJson);

        List<Question> historyList = new List<Question>();
        if (historyWrapper?.preguntas != null) historyList.AddRange(historyWrapper.preguntas);

        historyList.AddRange(newQuestions);

        QuestionArrayWrapper updatedHistory = new QuestionArrayWrapper { preguntas = historyList.ToArray() };
        File.WriteAllText(historyFilePath, JsonUtility.ToJson(updatedHistory, true));
    }

    #endregion

    #region Category Processing

    /// <summary>
    /// Reads the history JSON and calculates counts of questions per difficulty per category.
    /// </summary>
    /// <returns>List of CategoryData containing counts.</returns>
    private List<CategoryData> GetCategories()
    {
        if (!File.Exists(historyFilePath)) return new List<CategoryData>();

        string json = File.ReadAllText(historyFilePath);
        QuestionArrayWrapper wrapper = JsonUtility.FromJson<QuestionArrayWrapper>(json);

        if (wrapper.preguntas == null || wrapper.preguntas.Length == 0) return new List<CategoryData>();

        Dictionary<string, CategoryData> categories = new Dictionary<string, CategoryData>();
        foreach (var q in wrapper.preguntas)
        {
            if (string.IsNullOrEmpty(q.categoria)) continue;

            if (!categories.ContainsKey(q.categoria))
                categories[q.categoria] = new CategoryData { nombre = q.categoria };

            switch (q.dificultad.ToLower())
            {
                case "fácil":
                case "facil": categories[q.categoria].faciles++; break;
                case "media": categories[q.categoria].medias++; break;
                case "difícil":
                case "dificil": categories[q.categoria].dificiles++; break;
            }
        }

        return new List<CategoryData>(categories.Values);
    }

    /// <summary>
    /// Populates the UI list with categories and their difficulty counts.
    /// </summary>
    /// <param name="categories">List of categories to display.</param>
    public void ShowCategories(List<CategoryData> categories)
    {
        foreach (Transform child in contentParent) Destroy(child.gameObject);

        foreach (var cat in categories)
        {
            GameObject item = Instantiate(itemPrefab, contentParent);
            TMP_Text[] texts = item.GetComponentsInChildren<TMP_Text>();

            texts[0].text = cat.nombre;

            localizationText = difficultyEasyOption.GetLocalizedString();
            texts[1].text = $"{localizationText}: {cat.faciles}";

            localizationText = difficultyMediumOption.GetLocalizedString();
            texts[2].text = $"{localizationText}: {cat.medias}";

            localizationText = difficultyHardOption.GetLocalizedString();
            texts[3].text = $"{localizationText}: {cat.dificiles}";
        }
    }

    #endregion

    #region Animations

    /// <summary>
    /// Plays the initial card animation sequence on scene start.
    /// </summary>
    private void AnimateCardInitial()
    {
        Sequence seq = DOTween.Sequence();
        Vector3 loopPos = startCardPosition + Vector3.up * 1.5f;

        seq.Append(card.transform.DOMove(loopPos, 1f).SetEase(Ease.OutQuad));
        seq.Insert(seq.Duration() - 1f + 0.3f, card.transform.DORotateQuaternion(
            Quaternion.Euler(-208.997f, 107.955f, -90.02499f), 1.5f).SetEase(Ease.InOutQuad));
        seq.Insert(seq.Duration() - 2.2f + 0.7f, card.transform.DOMove(new Vector3(12.84f, 1.24f, -0.74f), 2f)
            .SetEase(Ease.OutQuad));
    }

    /// <summary>
    /// Returns the card to its starting position and rotation with animation.
    /// </summary>
    private void AnimateCardReturn()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(card.transform.DORotateQuaternion(startCardRotation, 0.5f).SetEase(Ease.InOutQuad));
        seq.Append(card.transform.DOMove(startCardPosition, 1f).SetEase(Ease.OutQuad));

        GameManager.Instance.MoveObjectToPoint(cameraObject, new Vector3(0, 8, -10.7f), Quaternion.Euler(48.968f, 0f, 0f), "Menu");
    }

    #endregion

    #region UI Transitions

    /// <summary>
    /// Handles pressing accept on instructions, fades to import menu.
    /// </summary>
    public void AcceptPressed()
    {
        FadeOutCanvas(instructionsPanel, () => ShowCanvas(importMenuPanel));
    }

    /// <summary>
    /// Handles pressing history button, shows categories and fades to history panel.
    /// </summary>
    public void HistoryPressed()
    {
        List<CategoryData> categories = GetCategories();
        ShowCategories(categories);

        FadeOutCanvas(importMenuPanel, () => ShowCanvas(historyMenuPanel));
    }

    /// <summary>
    /// Handles pressing back button from history, returns to import menu.
    /// </summary>
    public void BackPressed()
    {
        FadeOutCanvas(historyMenuPanel, () => ShowCanvas(importMenuPanel));
    }

    /// <summary>
    /// Handles returning to main menu by animating the card back.
    /// </summary>
    public void BackMenu()
    {
        AnimateCardReturn();
    }

    /// <summary>
    /// Fades out a CanvasGroup and invokes a callback after completion.
    /// </summary>
    /// <param name="canvasGroup">The CanvasGroup to fade.</param>
    /// <param name="onComplete">Action to execute after fade.</param>
    private void FadeOutCanvas(CanvasGroup canvasGroup, Action onComplete)
    {
        canvasGroup.DOFade(0f, 0.6f).SetEase(Ease.InOutQuad).OnComplete(() =>
        {
            canvasGroup.gameObject.SetActive(false);
            onComplete?.Invoke();
        });
    }

    /// <summary>
    /// Shows a CanvasGroup by enabling it and fading in.
    /// </summary>
    /// <param name="canvasGroup">The CanvasGroup to show.</param>
    private void ShowCanvas(CanvasGroup canvasGroup)
    {
        canvasGroup.gameObject.SetActive(true);
        canvasGroup.alpha = 0f;
        canvasGroup.DOFade(1f, 0.6f).SetEase(Ease.OutQuad);
    }

    #endregion
}
