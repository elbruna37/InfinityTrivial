using DG.Tweening;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIOptionsManager : MonoBehaviour
{
    public GameObject tarjeta;
    public GameObject camara;

    [Header("Referencias UI")]
    public TMP_Dropdown languageDropdown;
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown qualityDropdown;
    public Slider volumeSlider;
    public Toggle vibrationToggle;
    public Button applyButton;

    private Resolution[] resolutions;
    private int selectedResolutionIndex;
    private string selectedLanguage;
    private int selectedQuality;
    private float selectedVolume;
    private bool vibrationEnabled;

    [Header("Animacion")]
    Vector3 startPos;
    Quaternion startRot;

    void Start()
    {
        // --- Idiomas ---
        languageDropdown.ClearOptions();
        languageDropdown.AddOptions(new System.Collections.Generic.List<string> { "Español", "English" });

        // --- Resoluciones comunes 16:9 ---
        resolutionDropdown.ClearOptions();
        string[] commonRes = { "1920x1080", "1600x900", "1366x768", "1280x720" };
        resolutionDropdown.AddOptions(new System.Collections.Generic.List<string>(commonRes));

        // --- Calidad ---
        qualityDropdown.ClearOptions();
        qualityDropdown.AddOptions(new System.Collections.Generic.List<string> { "Baja", "Media", "Alta" });

        // Cargar valores guardados
        LoadSettings();

        // Asignar listener al botón
        applyButton.onClick.AddListener(ApplySettings);

        AnimateTarjeta();
    }

    private void LoadSettings()
    {
        selectedLanguage = PlayerPrefs.GetString("language", "Español");
        selectedResolutionIndex = PlayerPrefs.GetInt("resolution", 0);
        selectedQuality = PlayerPrefs.GetInt("quality", 1);
        selectedVolume = PlayerPrefs.GetFloat("volume", 1f);
        vibrationEnabled = PlayerPrefs.GetInt("vibration", 1) == 1;

        // Reflejar en UI
        languageDropdown.value = selectedLanguage == "English" ? 1 : 0;
        resolutionDropdown.value = selectedResolutionIndex;
        qualityDropdown.value = selectedQuality;
        volumeSlider.value = selectedVolume;
        vibrationToggle.isOn = vibrationEnabled;
    }

    public void ApplySettings()
    {
        GameManager.Instance.AudioClick();

        // Idioma
        selectedLanguage = languageDropdown.value == 0 ? "Español" : "English";
        PlayerPrefs.SetString("language", selectedLanguage);
        string code = selectedLanguage == "Español" ? "es" : "en";
        LocaleController.Instance.SetLocaleCode(code);

        // Resolución
        selectedResolutionIndex = resolutionDropdown.value;
        string res = resolutionDropdown.options[selectedResolutionIndex].text;
        string[] dims = res.Split('x');
        int width = int.Parse(dims[0]);
        int height = int.Parse(dims[1]);
        Screen.SetResolution(width, height, FullScreenMode.FullScreenWindow);
        PlayerPrefs.SetInt("resolution", selectedResolutionIndex);
        Debug.Log("Resolución cambiada a: " + res);

        // Calidad
        selectedQuality = qualityDropdown.value;
        QualitySettings.SetQualityLevel(selectedQuality);
        PlayerPrefs.SetInt("quality", selectedQuality);
        Debug.Log("Calidad: " + qualityDropdown.options[selectedQuality].text);

        // Volumen
        selectedVolume = volumeSlider.value;
        AudioListener.volume = selectedVolume;
        PlayerPrefs.SetFloat("volume", selectedVolume);
        Debug.Log("Volumen: " + selectedVolume);

        // Vibración
        vibrationEnabled = vibrationToggle.isOn;
        PlayerPrefs.SetInt("vibration", vibrationEnabled ? 1 : 0);
        Debug.Log("Vibración: " + (vibrationEnabled ? "Activada" : "Desactivada"));

        PlayerPrefs.Save();

        AnimateTarjetaAgain();
    }

    private void AnimateTarjeta()
    {
        startPos = tarjeta.transform.position;
        startRot = tarjeta.transform.rotation;
        Vector3 loopPos = startPos + Vector3.up * 1.5f;

        Sequence tarjetaSeq = DOTween.Sequence();

        //tarjetaSeq.AppendInterval(0f);

        tarjetaSeq.Append(tarjeta.transform.DOMove(loopPos, 1f).SetEase(Ease.OutQuad));  //Suma 1.5 a la altura del objeto
        tarjetaSeq.Insert(tarjetaSeq.Duration() - 1f + 0.3f, tarjeta.transform.DORotateQuaternion(Quaternion.Euler(-208.997f, 107.955f, -90.02499f), 1.5f)).SetEase(Ease.InOutQuad);  //0.3 segundos depués rota la tarjeta
        tarjetaSeq.Insert(tarjetaSeq.Duration() - 2.2f + 0.7f, tarjeta.transform.DOMove(new Vector3(12.84f, 1.24f, -0.74f), 2f).SetEase(Ease.OutQuad));     //Y en el segundo 0.7 hace el movimiento hacia la camara
    }

    private void AnimateTarjetaAgain()
    {
        Sequence tarjetaSeq = DOTween.Sequence();

        //tarjetaSeq.AppendInterval(0.5f);

        tarjetaSeq.Append(tarjeta.transform.DORotateQuaternion(startRot, 0.5f).SetEase(Ease.InOutQuad));
        tarjetaSeq.Append(tarjeta.transform.DOMove(startPos, 1f).SetEase(Ease.OutQuad));

        GameManager.Instance.MoveCamToPoint(camara, new Vector3(0, 8, -10.7f), Quaternion.Euler(48.968f, 0f, 0f), "Menu");
    }
}
