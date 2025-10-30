using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles the options/settings UI, including language, resolution, quality, volume, vibration,
/// and card animations for entering and exiting the menu.
/// </summary>
public class UIOptionsManager : MonoBehaviour
{
    #region Inspector References

    [Header("3D Card & Camera")]
    [SerializeField] private GameObject card;
    [SerializeField] private GameObject cameraObject;

    [Header("UI Elements")]
    [SerializeField] private TMP_Dropdown languageDropdown;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown qualityDropdown;
    [SerializeField] private Slider volumeSlider;
    [SerializeField] private Toggle vibrationToggle;
    [SerializeField] private Button applyButton;

    #endregion

    #region Private State

    private Vector3 startPosition;
    private Quaternion startRotation;

    private int selectedResolutionIndex;
    private string selectedLanguage;
    private int selectedQuality;
    private float selectedVolume;
    private bool vibrationEnabled;

    #endregion

    #region Unity Lifecycle

    /// <summary>
    /// Initializes dropdowns, loads saved settings, sets apply button listener, and plays entrance animation.
    /// </summary>
    private void Start()
    {
        InitializeDropdowns();
        LoadSettings();
        applyButton.onClick.AddListener(ApplySettings);
        AnimateCardEntrance();
    }

    #endregion

    #region Initialization

    /// <summary>
    /// Populates the language, resolution, and quality dropdowns with options.
    /// </summary>
    private void InitializeDropdowns()
    {
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new System.Collections.Generic.List<string> { "Español", "English" });

        resolutionDropdown.ClearOptions();
        resolutionDropdown.AddOptions(new System.Collections.Generic.List<string> { "1920x1080", "1600x900", "1366x768", "1280x720" });

        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "Baja", "Media", "Alta" });
    }

    /// <summary>
    /// Loads saved player settings and updates the UI accordingly.
    /// </summary>
    private void LoadSettings()
    {
        selectedLanguage = PlayerPrefs.GetString("language", "Español");
        selectedResolutionIndex = PlayerPrefs.GetInt("resolution", 0);
        selectedQuality = PlayerPrefs.GetInt("quality", 1);
        selectedVolume = PlayerPrefs.GetFloat("volume", 1f);
        vibrationEnabled = PlayerPrefs.GetInt("vibration", 1) == 1;

        languageDropdown.value = selectedLanguage == "English" ? 1 : 0;
        resolutionDropdown.value = selectedResolutionIndex;
        qualityDropdown.value = selectedQuality;
        volumeSlider.value = selectedVolume;
        vibrationToggle.isOn = vibrationEnabled;
    }

    #endregion

    #region Apply Settings

    /// <summary>
    /// Saves all settings selected in the UI, applies them immediately, and animates card exit.
    /// </summary>
    public void ApplySettings()
    {
        GameManager.Instance.PlayClickSound();

        ApplyLanguage();
        ApplyResolution();
        ApplyQuality();
        ApplyVolume();
        ApplyVibration();

        PlayerPrefs.Save();
        AnimateCardExit();
    }

    private void ApplyLanguage()
    {
        selectedLanguage = languageDropdown.value == 0 ? "Español" : "English";
        PlayerPrefs.SetString("language", selectedLanguage);
        string code = selectedLanguage == "Español" ? "es" : "en";
        LocaleController.Instance.SetLocaleCode(code);
    }

    private void ApplyResolution()
    {
        selectedResolutionIndex = resolutionDropdown.value;
        string resText = resolutionDropdown.options[selectedResolutionIndex].text;
        string[] dims = resText.Split('x');
        int width = int.Parse(dims[0]);
        int height = int.Parse(dims[1]);

        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
        PlayerPrefs.SetInt("resolution", selectedResolutionIndex);

        Debug.Log($"Resolution set to: {resText}");
    }

    private void ApplyQuality()
    {
        selectedQuality = qualityDropdown.value;
        QualitySettings.SetQualityLevel(selectedQuality);
        PlayerPrefs.SetInt("quality", selectedQuality);

        Debug.Log($"Quality set to: {qualityDropdown.options[selectedQuality].text}");
    }

    private void ApplyVolume()
    {
        selectedVolume = volumeSlider.value;
        AudioListener.volume = selectedVolume;
        PlayerPrefs.SetFloat("volume", selectedVolume);

        Debug.Log($"Volume set to: {selectedVolume}");
    }

    private void ApplyVibration()
    {
        vibrationEnabled = vibrationToggle.isOn;
        PlayerPrefs.SetInt("vibration", vibrationEnabled ? 1 : 0);

        Debug.Log($"Vibration: {(vibrationEnabled ? "Enabled" : "Disabled")}");
    }

    #endregion

    #region Card Animations

    /// <summary>
    /// Plays the entrance animation of the card when opening the options menu.
    /// </summary>
    private void AnimateCardEntrance()
    {
        startPosition = card.transform.position;
        startRotation = card.transform.rotation;

        Vector3 loopPos = startPosition + Vector3.up * 1.5f;
        Sequence seq = DOTween.Sequence();

        seq.Append(card.transform.DOMove(loopPos, 1f).SetEase(Ease.OutQuad));
        seq.Insert(seq.Duration() - 1f + 0.3f,
            card.transform.DORotateQuaternion(Quaternion.Euler(-208.997f, 107.955f, -90.02499f), 1.5f)
                .SetEase(Ease.InOutQuad));
        seq.Insert(seq.Duration() - 2.2f + 0.7f,
            card.transform.DOMove(new Vector3(12.84f, 1.24f, -0.74f), 2f).SetEase(Ease.OutQuad));
    }

    /// <summary>
    /// Animates the card returning to its original position and rotation when exiting the menu.
    /// Also moves the camera back to the main menu.
    /// </summary>
    private void AnimateCardExit()
    {
        Sequence seq = DOTween.Sequence();
        seq.Append(card.transform.DORotateQuaternion(startRotation, 0.5f).SetEase(Ease.InOutQuad));
        seq.Append(card.transform.DOMove(startPosition, 1f).SetEase(Ease.OutQuad));

        GameManager.Instance.MoveObjectToPoint(cameraObject, new Vector3(0, 8, -10.7f), Quaternion.Euler(48.968f, 0f, 0f), "Menu");
    }

    #endregion
}
