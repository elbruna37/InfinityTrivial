using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public struct DropdownColorMap
{
    public QuesitoColor color;
    public TMP_Dropdown dropdown;
}

public class UISelectorCategorias : MonoBehaviour
{
    public PreguntasManager manager;
    public DropdownColorMap[] dropdownColorMaps;
    public Button botonConfirmar;

    private List<string> originalCategories;
    private Dictionary<TMP_Dropdown, string> selectionMap;

    private int currentIndex = 0; // índice del dropdown que está activo

    void Start()
    {
        originalCategories = new List<string>(manager.categoriasDisponibles);
        selectionMap = new Dictionary<TMP_Dropdown, string>();

        // Inicializar todos como ocultos excepto el primero
        for (int i = 0; i < dropdownColorMaps.Length; i++)
        {
            TMP_Dropdown dd = dropdownColorMaps[i].dropdown;
            dd.gameObject.SetActive(i == 0); // solo el primero activo
            selectionMap[dd] = null;

            if (i == 0) // solo configuro el primero al inicio
            {
                var opciones = BuildOptionsFor(dd);
                dd.ClearOptions();
                dd.AddOptions(opciones);
                dd.SetValueWithoutNotify(0);

                TMP_Dropdown local = dd;
                dd.onValueChanged.AddListener((int value) => OnDropdownValueChanged(local, value));
            }
        }

        UpdateConfirmButton();
    }

    private void OnDropdownValueChanged(TMP_Dropdown dd, int value)
    {
        string nueva = value == 0 ? null : dd.options[value].text;
        if (nueva == null) return;

        selectionMap[dd] = nueva;

        // Bloquear dropdown actual
        dd.interactable = false;
        dd.gameObject.SetActive(false);

        // Avanzar al siguiente dropdown si existe
        currentIndex++;
        if (currentIndex < dropdownColorMaps.Length)
        {
            TMP_Dropdown siguiente = dropdownColorMaps[currentIndex].dropdown;
            siguiente.gameObject.SetActive(true);

            var opciones = BuildOptionsFor(siguiente);
            siguiente.ClearOptions();
            siguiente.AddOptions(opciones);
            siguiente.SetValueWithoutNotify(0);

            TMP_Dropdown local = siguiente;
            siguiente.onValueChanged.AddListener((int v) => OnDropdownValueChanged(local, v));
        }

        UpdateConfirmButton();
    }

    private List<string> BuildOptionsFor(TMP_Dropdown dd)
    {
        HashSet<string> bloqueadas = new HashSet<string>(
            selectionMap.Where(kv => kv.Key != dd && !string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Value)
        );

        List<string> disponibles = originalCategories.Where(c => !bloqueadas.Contains(c)).ToList();

        List<string> opciones = new List<string> { "Selecciona una categoría" };
        opciones.AddRange(disponibles);
        return opciones;
    }

    private void UpdateConfirmButton()
    {
        // El botón solo es interactuable si todos los dropdowns tienen selección
        bool allSelected = selectionMap.Values.All(v => !string.IsNullOrEmpty(v));
        botonConfirmar.interactable = allSelected;

        TMP_Text buttonText = botonConfirmar.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.color = allSelected ? Color.white : Color.gray;
        }
    }

    public void ConfirmarCategorias()
    {
        GameManager.Instance.AudioClick();

        foreach (var map in dropdownColorMaps)
        {
            string categoria = selectionMap[map.dropdown];
            if (!string.IsNullOrEmpty(categoria))
            {
                GameManager.Instance.SetCategoriaParaColor(map.color, categoria);
            }
        }

        SceneManager.LoadScene("Game");
    }

    public void VolverAlMenu()
    {
        GameManager.Instance.AudioClick();

        Destroy(GameManager.Instance.gameObject);
        Destroy(PreguntasManager.Instance.gameObject);

        SceneManager.LoadScene("Menu");
    }
}
