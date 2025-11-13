using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton GameManager that handles global game data, audio feedback, 
/// aspect ratio adjustment, load settings and camera transitions between scenes.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool IsLoadingGame { get; private set; } = false;

    [Header("Screen Settings")]
    private readonly float _targetAspect = 16f / 9f;
    private Camera _mainCamera;

    [Header("Game Data")]
    // Mapping from color to category
    private readonly Dictionary<QuesitoColor, string> _categoriesByColor = new Dictionary<QuesitoColor, string>();
    public int MaxPlayers { get; set; } = 0;

    [Header("Audio Clips")]
    [SerializeField] private AudioClip _clickClip;
    [SerializeField] private AudioClip _correctClip;
    [SerializeField] private AudioClip _wrongClip;
    [SerializeField] private AudioClip _timerClip;
    [SerializeField] private AudioClip _tokenClip;
    [SerializeField] private AudioClip _jumpClip;
    [SerializeField] private AudioClip _diceClip;

    private AudioSource _audioSource;

    #region Unity Lifecycle

    /// <summary>
    /// Initializes singleton instance, sets frame rate, ensures main camera reference,
    /// and subscribes to scene change events.
    /// </summary>
    private void Awake()
    {
        Application.targetFrameRate = 60;

        LoadPlayerSettings();

        FindMainCamera();
        SceneManager.activeSceneChanged += OnSceneChanged;
        UpdateAspectRatio();

        // Singleton pattern
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.loop = false;
    }

    /// <summary>
    /// Keeps aspect ratio updated if the camera changes or screen size adjusts.
    /// </summary>
    private void Update()
    {
        if (_mainCamera == null)
            FindMainCamera();
        else
            UpdateAspectRatio();
    }

    /// <summary>
    /// Public flag to indicate if it is a new game or a loaded one
    /// </summary>
    public void SetLoadingGame(bool value)
    {
        IsLoadingGame = value;
    }

    /// <summary>
    /// Unsubscribes from scene change events when destroyed.
    /// </summary>
    private void OnDestroy()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
    }

    #endregion

    #region Category Management

    /// <summary>
    /// Assigns a category string to a specific color key.
    /// </summary>
    public void SetCategoryForColor(QuesitoColor color, string category)
    {
        _categoriesByColor[color] = category;
        Debug.Log($"Assigned category '{category}' to color {color}");
    }

    /// <summary>
    /// Retrieves the category associated with a color, or null if not assigned.
    /// </summary>
    public string GetCategoryForColor(QuesitoColor color)
    {
        if (_categoriesByColor.TryGetValue(color, out var category))
            return category;

        Debug.LogWarning($"No category assigned to color {color}");
        return null;
    }

    /// <summary>
    /// Returns a copy of the current color-category mapping.
    /// </summary>
    public Dictionary<QuesitoColor, string> GetAllCategories()
    {
        return new Dictionary<QuesitoColor, string>(_categoriesByColor);
    }

    #endregion

    #region Audio Management

    /// <summary>Plays a UI click sound.</summary>
    public void PlayClickSound() => PlaySound(_clickClip);

    /// <summary>Plays a correct-answer sound.</summary>
    public void PlayCorrectSound() => PlaySound(_correctClip);

    /// <summary>Plays a wrong-answer sound.</summary>
    public void PlayWrongSound() => PlaySound(_wrongClip);

    /// <summary>Plays a timer ticking sound.</summary>
    public void PlayTimerSound() => PlaySound(_timerClip);

    /// <summary>Plays a token placement sound.</summary>
    public void PlayTokenSound() => PlaySound(_tokenClip);

    /// <summary>Plays a jump sound effect.</summary>
    public void PlayJumpSound() => PlaySound(_jumpClip);

    /// <summary>Plays a dice roll sound.</summary>
    public void PlayDiceSound() => PlaySound(_diceClip);

    /// <summary>
    /// Assigns a clip to the AudioSource and plays it once.
    /// </summary>
    private void PlaySound(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("Attempted to play a null AudioClip.");
            return;
        }

        _audioSource.clip = clip;
        _audioSource.Play();
    }

    #endregion

    #region Camera, Settings & Aspect Handling


    /// <summary>
    /// Load the settings saved in PlayerPrefs
    /// </summary>
    private void LoadPlayerSettings()
    {
        string savedLang = PlayerPrefs.GetString("language", "");
        string selectedLang = PlayerPrefs.GetString("language", "Español");
        string code;

        if (string.IsNullOrEmpty(savedLang))
        {
            // Detectar idioma del sistema
            string systemLang = Application.systemLanguage.ToString();
            Debug.Log($"System language detected: {systemLang}");

            switch (systemLang)
            {
                case "Spanish":
                case "SpanishLatinAmerica":
                    selectedLang = "Español";
                    code = "es";
                    break;
                case "English":
                    selectedLang = "English";
                    code = "en";
                    break;
                default:
                    selectedLang = "English";
                    code = "en";
                    break;
            }

            PlayerPrefs.SetString("language", selectedLang);
            PlayerPrefs.Save();
        }
        else
        {
            selectedLang = savedLang;
            code = selectedLang == "Español" ? "es" : "en";
        }

        if (LocaleController.Instance != null)
        {
            LocaleController.Instance.SetLocaleCode(code);
        }
        else
        {
            Debug.LogWarning("⚠️ LocaleController not found at startup. Language will be applied later.");
        }
        Debug.Log($"Language loaded: {selectedLang} ({code})");

        int quality = PlayerPrefs.GetInt("quality", 2);
        QualitySettings.SetQualityLevel(quality);

        float volume = PlayerPrefs.GetFloat("volume", 1f);
        AudioListener.volume = volume;
        
        int resIndex = PlayerPrefs.GetInt("resolution", 0);
        if (Screen.resolutions.Length > resIndex)
        {
            Resolution res = Screen.resolutions[resIndex];
            Screen.SetResolution(res.width, res.height, FullScreenMode.FullScreenWindow);
        }


        bool vibration = PlayerPrefs.GetInt("vibration", 1) == 1;

        Debug.Log("✅ Player settings loaded at startup");
    }

    /// <summary>
    /// Locates the main camera in the active scene and updates aspect ratio.
    /// </summary>
    private void FindMainCamera()
    {
        _mainCamera = Camera.main;

        if (_mainCamera == null)
        {
            Debug.LogWarning("⚠️ Main camera not found in the current scene.");
            return;
        }

        UpdateAspectRatio();
    }

    /// <summary>
    /// Adjusts the camera viewport to maintain a fixed 16:9 aspect ratio.
    /// </summary>
    private void UpdateAspectRatio()
    {
        if (_mainCamera == null) return;

        float windowAspect = (float)Screen.width / Screen.height;
        float scaleHeight = windowAspect / _targetAspect;

        Rect rect = _mainCamera.rect;

        if (scaleHeight < 1.0f)
        {
            rect.width = 1.0f;
            rect.height = scaleHeight;
            rect.x = 0;
            rect.y = (1.0f - scaleHeight) / 2.0f;
        }
        else
        {
            float scaleWidth = 1.0f / scaleHeight;
            rect.width = scaleWidth;
            rect.height = 1.0f;
            rect.x = (1.0f - scaleWidth) / 2.0f;
            rect.y = 0;
        }

        _mainCamera.rect = rect;
    }

    #endregion

    #region Scene Transition

    /// <summary>
    /// Moves an object along a parabolic path to a target position and rotation, 
    /// then loads a new scene upon completion.
    /// </summary>
    public void MoveObjectToPoint(GameObject obj, Vector3 targetPosition, Quaternion targetRotation, string targetScene)
    {
        const float moveDuration = 1.5f;
        const float parabolaHeight = 3f;

        Vector3 startPosition = obj.transform.position;
        Quaternion startRotation = obj.transform.rotation;

        DOVirtual.DelayedCall(0f, () =>
        {
            Sequence sequence = DOTween.Sequence();
            float t = 0f;

            sequence.Append(
                DOVirtual.Float(0f, 1f, moveDuration, value =>
                {
                    t = value;

                    // Parabolic interpolation
                    Vector3 linearPosition = Vector3.Lerp(startPosition, targetPosition, t);
                    float parabola = 4f * parabolaHeight * t * (1 - t);
                    linearPosition.y += parabola;
                    obj.transform.position = linearPosition;

                    obj.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, t);
                }).SetEase(Ease.InOutQuad)
            );

            sequence.OnComplete(() =>
            {
                obj.transform.position = targetPosition;
                obj.transform.rotation = targetRotation;
                SceneManager.LoadScene(targetScene);
            });
        });
    }

    /// <summary>
    /// Called when the active scene changes; re-finds the main camera.
    /// </summary>
    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        FindMainCamera();
    }

    #endregion
}
