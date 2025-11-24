using System;
using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocaleController : MonoBehaviour
{
    public static LocaleController Instance;
    public event Action OnLanguageChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            StartCoroutine(LoadSavedLocale());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator LoadSavedLocale()
    {
        yield return LocalizationSettings.InitializationOperation;

        string savedLang = PlayerPrefs.GetString("language", "Español");
        string code = savedLang == "Español" ? "es" : "en";
        SetLocaleCode(code);
    }
    public void SetLocaleCode(string code)
    {
        StartCoroutine(SetLocaleWhenReadyCoroutine(code));
    }

    IEnumerator SetLocaleWhenReadyCoroutine(string code)
    {
        yield return LocalizationSettings.InitializationOperation;

        var locales = LocalizationSettings.AvailableLocales.Locales;
        var locale = locales.FirstOrDefault(l => l.Identifier.Code.StartsWith(code));

        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
            Debug.Log("Locale change to: " + locale.LocaleName + " (" + locale.Identifier.Code + ")");

            OnLanguageChanged?.Invoke();
        }
        else
        {
            Debug.LogWarning("Locale not found for: " + code);
        }
    }
}

