using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

/// <summary>
/// Manages all question logic — loading, filtering by category/difficulty,
/// preventing repetition, and delegating question display to UIManager.
/// </summary>
public class QuestionsManager : MonoBehaviour
{
    public static QuestionsManager Instance { get; private set; }

    [Header("Available Questions")]
    public Question[] allQuestions;
    public List<string> availableCategories;

    private string historyFilePath;

    /// <summary>
    /// History of asked questions, keyed by "category_difficulty"
    /// to prevent repetition within a session.
    /// </summary>
    public readonly Dictionary<string, HashSet<string>> questionHistory = new Dictionary<string, HashSet<string>>();

    #region Unity Lifecycle

    /// <summary>
    /// Singleton initialization and question data loading from localized JSON file.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Persist across scenes
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        historyFilePath = Path.Combine(Application.persistentDataPath,"historial_importadas.json");

        LoadQuestionsFromLocale();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Asks a random question for the given category with random difficulty.
    /// </summary>
    /// <param name="category">Category of the question.</param>
    /// <param name="onAnswered">Callback invoked with true/false when answered.</param>
    public void AskRandomQuestion(string category,int slot,float easy, float medium, System.Action<bool> onAnswered)
    {
        string normalizedCategory = NormalizeCategory(category);
        string difficulty = ChooseRandomDifficulty(easy,medium);

        Question selected = GetNonRepeatedQuestion(normalizedCategory, difficulty);

        if (selected == null)
        {
            Debug.LogWarning($"No questions available for {category} [{difficulty}]");
            onAnswered?.Invoke(false);
            return;
        }

        Debug.Log($"Selected question ({category}, {difficulty}): {selected.enunciado}");
        UIManager.Instance.ShowQuestion(selected, difficulty, onAnswered,slot);
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// Loads question data from a localized JSON file in Resources.
    /// </summary>
    private void LoadQuestionsFromLocale()
    {
        string code = LocalizationSettings.SelectedLocale.Identifier.Code;
        string path = $"questions_{code}";

        TextAsset jsonFile = Resources.Load<TextAsset>(path);
        if (jsonFile == null)
        {
            Debug.LogError($"Could not find {path}.json in Resources.");
            return;
        }

        QuestionBatch batch = JsonUtility.FromJson<QuestionBatch>(jsonFile.text);
        List<Question> loadedQuestions = new List<Question>();

        if (batch?.questions != null)
            loadedQuestions.AddRange(batch.questions);

        if (File.Exists(historyFilePath))
        {
            string historyJson = File.ReadAllText(historyFilePath);
            QuestionArrayWrapper historyWrapper = JsonUtility.FromJson<QuestionArrayWrapper>(historyJson);

            if (historyWrapper?.preguntas != null && historyWrapper.preguntas.Length > 0)
            {
                var groupedByCategory = historyWrapper.preguntas
                    .Where(q => !string.IsNullOrEmpty(q.categoria))
                    .GroupBy(q => q.categoria);

                foreach (var categoryGroup in groupedByCategory)
                {
                    int faciles = categoryGroup.Count(q =>
                    NormalizeDifficulty(q.dificultad) == "facil");

                    int medias = categoryGroup.Count(q =>
                        NormalizeDifficulty(q.dificultad) == "media");

                    int dificiles = categoryGroup.Count(q =>
                        NormalizeDifficulty(q.dificultad) == "dificil");

                    if (faciles >= 30 && medias >= 30 && dificiles >= 30)
                    {
                        loadedQuestions.AddRange(categoryGroup);
                    }
                }
            }
        }

        allQuestions = loadedQuestions.ToArray();

        // Extract unique categories
        availableCategories = allQuestions
            .Where(q => !string.IsNullOrEmpty(q.categoria))
            .Select(q => q.categoria)
            .Distinct()
            .ToList();

        Debug.Log($"Loaded {allQuestions.Length} questions across {availableCategories.Count} categories.");
    }

    /// <summary>
    /// Retrieves a random question that hasn't been asked yet for the given category and difficulty.
    /// </summary>
    private Question GetNonRepeatedQuestion(string category, string difficulty)
    {
        if (string.IsNullOrEmpty(category) || string.IsNullOrEmpty(difficulty)) 
            return null;

        string normalizedCategory = category.Trim().ToLower();
        string normalizedDifficulty = NormalizeDifficulty(difficulty);

        string key = $"{normalizedCategory}_{normalizedDifficulty}";

        if (!questionHistory.ContainsKey(key)) questionHistory[key] = new HashSet<string>();

        // Filter questions matching category & difficulty
        var filtered = allQuestions
       .Where(q =>
           !string.IsNullOrEmpty(q.categoria) &&
           NormalizeDifficulty(q.dificultad) == normalizedDifficulty &&
           q.categoria.Trim().ToLower() == normalizedCategory
       ).ToList();

        if (filtered.Count == 0)
            return null;

        // Remove already asked
        var available = filtered
        .Where(q => !questionHistory[key].Contains(GetQuestionUniqueId(q)))
        .ToList(); ;

        // If all used, reset history for this group
        if (available.Count == 0)
        {
            Debug.Log($"Exhausted questions for {key}. Resetting its history.");
            questionHistory[key].Clear();
            available = filtered;
        }

        if (available.Count == 0) return null; // No questions at all

        // Pick random available question
        var selected = available[UnityEngine.Random.Range(0, available.Count)];
        questionHistory[key].Add(GetQuestionUniqueId(selected));

        return selected;
    }

    /// <summary>
    /// Generates a unique ID for a question based on its content.
    /// This prevents duplicates even if the indexes change.
    /// </summary>
    private string GetQuestionUniqueId(Question question)
    {
        return $"{question.enunciado?.Trim().ToLower()}_{question.categoria?.Trim().ToLower()}_{NormalizeDifficulty(question.dificultad)}";
    }

    /// <summary>
    /// Random difficulty distribution for questions.
    /// </summary>
    private string ChooseRandomDifficulty(float easy, float medium)
    {
        float r = UnityEngine.Random.value;
        if (r < easy) return "facil";
        if (r >= easy && r < medium) return "media";
        else return "dificil";
    }

    private string NormalizeCategory(string category)
    {
        return string.IsNullOrEmpty(category)
            ? ""
            : category.Trim().ToLower();
    }

    private string NormalizeDifficulty(string d)
    {
        return d
            .ToLower()
            .Replace("á", "a")
            .Replace("é", "e")
            .Replace("í", "i");
    }

    #endregion
}
