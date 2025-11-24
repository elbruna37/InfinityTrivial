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

    [Header("Color asignado a este selector")]
    public QuesitoColor color;

    private List<string> categories;
    private int index;

    // Categorías tomadas por todos los selectores
    public static HashSet<string> categoriesSelected = new HashSet<string>();

    private string actualCategory => categories[index];

    private void Start()
    {
        categories = QuestionsManager.Instance.availableCategories;

        // Buscar una categoría libre para empezar
        index = FindFirstFreeIndex();

        categoriesSelected.Add(actualCategory);
        UpdateLabel();

        upButton.onClick.AddListener(() => ChangeCategory(+1));
        downButton.onClick.AddListener(() => ChangeCategory(-1));
    }

    private int FindFirstFreeIndex()
    {
        for (int i = 0; i < categories.Count; i++)
            if (!categoriesSelected.Contains(categories[i]))
                return i;

        Debug.LogError("No hay categorías libres para asignar.");
        return 0;
    }

    private void ChangeCategory(int dir)
    {
        // Liberar categoría previa
        categoriesSelected.Remove(actualCategory);

        int startIndex = index;

        do
        {
            index = (index + dir + categories.Count) % categories.Count;

            // Si damos la vuelta y todo está ocupado
            if (index == startIndex)
                break;

        } while (categoriesSelected.Contains(categories[index]));

        // Seleccionar nueva
        categoriesSelected.Add(actualCategory);

        UpdateLabel();

        GameManager.Instance.SetCategoryForColor(color, actualCategory);
    }

    private void UpdateLabel()
    {
        label.text = actualCategory;
    }
}
