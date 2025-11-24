using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

[RequireComponent(typeof(TMP_Dropdown))]
public class LocalizedDropdown : MonoBehaviour
{
    [SerializeField] private LocalizedStringTable stringTable;
    [SerializeField] private List<string> entryKeys = new List<string>();

    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
        LocalizationSettings.SelectedLocaleChanged += OnLocaleChanged;
    }

    private void Start()
    {
        UpdateOptions();
    }

    private void OnDestroy()
    {
        LocalizationSettings.SelectedLocaleChanged -= OnLocaleChanged;
    }

    private void OnLocaleChanged(Locale locale)
    {
        UpdateOptions();
    }

    private void UpdateOptions()
    {
        StartCoroutine(UpdateOptionsCoroutine());
    }

    private IEnumerator UpdateOptionsCoroutine()
    {
        var tableOp = LocalizationSettings.StringDatabase.GetTableAsync(stringTable.TableReference);
        yield return tableOp;

        if (tableOp.Result == null)
        {
            Debug.LogWarning("Location table not found: " + stringTable.TableReference);
            yield break;
        }

        var table = tableOp.Result;
        dropdown.options.Clear();

        foreach (string key in entryKeys)
        {
            var entry = table.GetEntry(key);
            string value = entry != null ? entry.GetLocalizedString() : key;
            dropdown.options.Add(new TMP_Dropdown.OptionData(value));
        }

        dropdown.RefreshShownValue();
    }
}

