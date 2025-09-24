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

    void Start()
    {
        originalCategories = new List<string>(manager.categoriasDisponibles);
        selectionMap = new Dictionary<TMP_Dropdown, string>();

        foreach (var map in dropdownColorMaps)
        {
            TMP_Dropdown dd = map.dropdown;
            selectionMap[dd] = null;

            var opciones = BuildOptionsFor(dd);
            dd.ClearOptions();
            dd.AddOptions(opciones);
            dd.SetValueWithoutNotify(0);

            TMP_Dropdown local = dd;
            dd.onValueChanged.AddListener((int value) => OnDropdownValueChanged(local, value));
        }

        UpdateConfirmButton();
    }

    private void OnDropdownValueChanged(TMP_Dropdown dd, int value)
    {
        string nueva = value == 0 ? null : dd.options[value].text;
        string anterior = selectionMap[dd];

        if (anterior == nueva) return;

        selectionMap[dd] = nueva;

        // Bloquear dropdown al elegir
        dd.interactable = string.IsNullOrEmpty(nueva) ? true : false;

        RefreshAllDropdowns();
        UpdateConfirmButton(); // Actualizamos el estado del botón
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

    private void RefreshAllDropdowns()
    {
        foreach (var dd in selectionMap.Keys.ToList())
        {
            string seleccionActual = selectionMap[dd];
            var opciones = BuildOptionsFor(dd);

            dd.ClearOptions();
            dd.AddOptions(opciones);

            if (!string.IsNullOrEmpty(seleccionActual))
            {
                int idx = dd.options.FindIndex(o => o.text == seleccionActual);
                if (idx != -1)
                {
                    dd.SetValueWithoutNotify(idx);
                    dd.interactable = false;
                }
                else
                {
                    selectionMap[dd] = null;
                    dd.SetValueWithoutNotify(0);
                    dd.interactable = true;
                }
            }
            else
            {
                dd.SetValueWithoutNotify(0);
                dd.interactable = true;
            }
        }
    }

    private void UpdateConfirmButton()
    {
        // El botón solo es interactuable si todos los dropdowns tienen selección
        botonConfirmar.interactable = selectionMap.Values.All(v => !string.IsNullOrEmpty(v));
    }

    public void ConfirmarCategorias()
    {

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
        Destroy(GameManager.Instance.gameObject);
        Destroy(PreguntasManager.Instance.gameObject);

        SceneManager.LoadScene("Menu");
    }
}
