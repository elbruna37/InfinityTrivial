using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

public class LocaleController : MonoBehaviour
{
    public static LocaleController Instance;

    private void Awake()
    {
        // Patrón Singleton básico
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 🔒 No se destruye entre escenas
            StartCoroutine(LoadSavedLocale());
        }
        else
        {
            Destroy(gameObject); // Evita duplicados
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
        // Espera hasta que el sistema de localization se inicialice
        yield return LocalizationSettings.InitializationOperation;

        // Busca un Locale cuyo Identifier.Code comience por el code que pasamos ("es" o "en")
        var locales = LocalizationSettings.AvailableLocales.Locales;
        var locale = locales.FirstOrDefault(l => l.Identifier.Code.StartsWith(code));

        if (locale != null)
        {
            LocalizationSettings.SelectedLocale = locale;
            Debug.Log("Locale cambiado a: " + locale.LocaleName + " (" + locale.Identifier.Code + ")");
        }
        else
        {
            Debug.LogWarning("Locale no encontrado para: " + code);
        }
    }
}

