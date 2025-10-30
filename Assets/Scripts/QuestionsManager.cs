using System.Collections.Generic;
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

    /// <summary>
    /// History of asked questions, keyed by "category_difficulty"
    /// to prevent repetition within a session.
    /// </summary>
    private readonly Dictionary<string, HashSet<int>> questionHistory = new Dictionary<string, HashSet<int>>();

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

        LoadQuestionsFromLocale();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Asks a random question for the given category with random difficulty.
    /// </summary>
    /// <param name="category">Category of the question.</param>
    /// <param name="onAnswered">Callback invoked with true/false when answered.</param>
    public void AskRandomQuestion(string category, System.Action<bool> onAnswered)
    {
        string difficulty = ChooseRandomDifficulty();
        Question selected = GetNonRepeatedQuestion(category, difficulty);

        if (selected == null)
        {
            Debug.LogWarning($"No questions available for {category} [{difficulty}]");
            onAnswered?.Invoke(false);
            return;
        }

        Debug.Log($"Selected question ({category}, {difficulty}): {selected.enunciado}");
        UIManager.Instance.ShowQuestion(selected, difficulty, onAnswered);
    }

    /// <summary>
    /// Asks a random "wedge" (Quesito) question for a category.
    /// Uses a different difficulty distribution favoring harder questions.
    /// </summary>
    public void AskRandomWedgeQuestion(string category, System.Action<bool> onAnswered)
    {
        string difficulty = ChooseWedgeDifficulty();
        Question selected = GetNonRepeatedQuestion(category, difficulty);

        if (selected == null)
        {
            Debug.LogWarning($"No questions available for {category} [{difficulty}]");
            onAnswered?.Invoke(false);
            return;
        }

        Debug.Log($"Selected wedge question ({category}, {difficulty}): {selected.enunciado}");
        UIManager.Instance.ShowQuestion(selected, difficulty, onAnswered);
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
            Debug.LogError($"❌ Could not find {path}.json in Resources.");
            return;
        }

        QuestionBatch batch = JsonUtility.FromJson<QuestionBatch>(jsonFile.text);
        allQuestions = batch.questions;

        // Extract unique categories
        availableCategories = allQuestions
            .Select(q => q.categoria)
            .Distinct()
            .ToList();

        Debug.Log($"✅ Loaded {allQuestions.Length} questions across {availableCategories.Count} categories.");
    }

    /// <summary>
    /// Retrieves a random question that hasn't been asked yet for the given category and difficulty.
    /// </summary>
    private Question GetNonRepeatedQuestion(string category, string difficulty)
    {
        string key = $"{category}_{difficulty}";
        if (!questionHistory.ContainsKey(key))
            questionHistory[key] = new HashSet<int>();

        // Filter questions matching category & difficulty
        var filtered = allQuestions
            .Select((q, i) => new { Question = q, Index = i })
            .Where(x => x.Question.categoria == category && x.Question.dificultad == difficulty)
            .ToList();

        // Remove already asked
        var available = filtered
            .Where(x => !questionHistory[key].Contains(x.Index))
            .ToList();

        // If all used, reset history for this group
        if (available.Count == 0)
        {
            Debug.Log($"⚠️ Exhausted questions for {key}. Resetting its history.");
            questionHistory[key].Clear();
            available = filtered;
        }

        if (available.Count == 0) return null; // No questions at all

        // Pick random available question
        var selected = available[Random.Range(0, available.Count)];
        questionHistory[key].Add(selected.Index);
        return selected.Question;
    }

    /// <summary>
    /// Random difficulty distribution for normal questions.
    /// </summary>
    private string ChooseRandomDifficulty()
    {
        float r = Random.value;
        if (r < 0.5f) return "facil";
        if (r < 0.9f) return "media";
        return "dificil";
    }

    /// <summary>
    /// Random difficulty distribution for wedge questions (harder).
    /// </summary>
    private string ChooseWedgeDifficulty()
    {
        return Random.value < 0.7f ? "dificil" : "media";
    }

    #endregion
}
