using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles saving and loading of game data to and from a JSON file.
/// Works with Unity’s official Newtonsoft JSON package.
/// </summary>
public class GameSaveManager : MonoBehaviour
{
    public static GameSaveManager Instance { get; private set; }

    public GameSaveData LoadedSaveData { get; private set; }

    private string saveFilePath;

    private void Awake()
    {

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");
    }

    /// <summary>
    /// Saves all relevant game data (players, board state, questions, etc.) to disk.
    /// </summary>
    public void SaveGame()
    {
        try
        {
            GameSaveData saveData = new GameSaveData
            {
                maxPlayer = GameManager.Instance.MaxPlayers,
                selectedCategories = GameManager.Instance.GetAllCategories(),
                currentPlayerIndex = TurnManager.Instance.currentPlayerIndex,
                wedgesByPlayer = TurnManager.Instance.GetWedgesByPlayer(),
                playerPositions = TurnManager.Instance.GetPlayerPositions(),
                usedQuestionsByCategory = new Dictionary<string, List<int>>()
            };

            // Convert question history to serializable format
            foreach (var pair in QuestionsManager.Instance.questionHistory)
                saveData.usedQuestionsByCategory[pair.Key] = new List<int>(pair.Value);

            // Serialize and write to file
            string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
            File.WriteAllText(saveFilePath, json);

            Debug.Log($"Game saved successfully to: {saveFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error saving game: {ex.Message}");
        }
    }

    /// <summary>
    /// Loads the basic data (categories, used questions, player count).
    /// Should be called in the category-loading scene.
    /// </summary>
    public void LoadCategories()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning("No save file found to load categories.");
            return;
        }

        try
        {
            GameManager.Instance.SetLoadingGame(true);

            string json = File.ReadAllText(saveFilePath);
            LoadedSaveData = JsonConvert.DeserializeObject<GameSaveData>(json);

            // Restore core data before gameplay
            GameManager.Instance.MaxPlayers = LoadedSaveData.maxPlayer;

            if (LoadedSaveData.selectedCategories != null)
            {
                foreach (var kvp in LoadedSaveData.selectedCategories)
                    GameManager.Instance.SetCategoryForColor(kvp.Key, kvp.Value);
            }

            if (LoadedSaveData.usedQuestionsByCategory != null)
            {
                QuestionsManager.Instance.questionHistory.Clear();
                foreach (var pair in LoadedSaveData.usedQuestionsByCategory)
                    QuestionsManager.Instance.questionHistory[pair.Key] = new HashSet<int>(pair.Value);
            }

            Debug.Log("Category data and question history loaded successfully.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error loading categories: {ex.Message}");
        }
    }


    /// <summary>
    /// Loads the saved game data from JSON file and stores it in memory.
    /// Other systems (GameManager, TurnManager, BoardNode) will later use these data
    /// once they are fully initialized.
    /// </summary>
    public void LoadGame()
    {
        if (!File.Exists(saveFilePath))
        {
            Debug.LogWarning($"No save file found at path: {saveFilePath}");
            LoadedSaveData = null;
            return;
        }

        try
        {
            string json = File.ReadAllText(saveFilePath);
            LoadedSaveData = JsonConvert.DeserializeObject<GameSaveData>(json);

            if (LoadedSaveData == null)
            {
                Debug.LogWarning("⚠Failed to parse save file (LoadedSaveData is null).");
                return;
            }

            // --- TURN DATA (just stored, not applied yet) ---
            // Positions and wedges will be applied by TurnManager & BoardNode when ready.
            if (LoadedSaveData.playerPositions != null)
                Debug.Log($"Loaded {LoadedSaveData.playerPositions.Count} player positions from save file.");

            Debug.Log($"Game data successfully loaded into memory from: {saveFilePath}");
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error loading game file: {ex.Message}\n{ex.StackTrace}");
            LoadedSaveData = null;
        }
    }

    /// <summary>
    /// Deletes the save file if it exists (for debugging or new games).
    /// </summary>
    public void DeleteSave()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
            Debug.Log("Save file deleted.");
        }
    }
}
