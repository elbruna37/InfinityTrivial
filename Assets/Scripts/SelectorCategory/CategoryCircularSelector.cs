using DG.Tweening;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryCircularSelector : MonoBehaviour
{
    public TextMeshProUGUI label;
    public Button upButton;
    public Button downButton;
    public CanvasGroup CircularSelector;

    [Header("Color assigned to this selector")]
    public QuesitoColor color;

    private List<string> categories;
    private int index;

    public static HashSet<string> categoriesSelected = new HashSet<string>();

    private string actualCategory => categories[index];

    private void Start()
    {
        CircularSelector.DOFade(1f, 0.3f).SetEase(Ease.OutQuad);

        categories = QuestionsManager.Instance.availableCategories;

        index = FindFirstFreeIndex();

        categoriesSelected.Add(actualCategory);
        UpdateLabel();
        GameManager.Instance.SetCategoryForColor(color, actualCategory);

        upButton.onClick.AddListener(() => ChangeCategory(+1));
        downButton.onClick.AddListener(() => ChangeCategory(-1));
    }

    private int FindFirstFreeIndex()
    {
        for (int i = 0; i < categories.Count; i++)
            if (!categoriesSelected.Contains(categories[i]))
                return i;

        Debug.LogError("There are no free categories to assign.");
        return 0;
    }

    private void ChangeCategory(int dir)
    {
        categoriesSelected.Remove(actualCategory);

        int startIndex = index;

        do
        {
            index = (index + dir + categories.Count) % categories.Count;

            if (index == startIndex)
                break;

        } while (categoriesSelected.Contains(categories[index]));

        categoriesSelected.Add(actualCategory);

        UpdateLabel();

        GameManager.Instance.SetCategoryForColor(color, actualCategory);
    }

    private void UpdateLabel()
    {
        label.text = actualCategory;
    }
}
