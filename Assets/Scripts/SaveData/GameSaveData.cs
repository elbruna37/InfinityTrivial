using System;
using System.Collections.Generic;

/// <summary>
/// Represents the complete state of a Trivial game that can be saved and loaded.
/// </summary>
[Serializable]
public class GameSaveData
{

    public int maxPlayer;

    public Dictionary<QuesitoColor, string> selectedCategories;

    public Dictionary<int, string> playerPositions;

    public int currentPlayerIndex;

    public Dictionary<int, List<QuesitoColor>> wedgesByPlayer;

    public Dictionary<string, List<int>> usedQuestionsByCategory;

}

