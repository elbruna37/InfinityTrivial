using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// Handles the main menu UI logic, including button animations, camera transitions, 
/// and scene navigation.
/// </summary>
public class UIMenuManager : MonoBehaviour
{
    [Header("Rotation Settings")]
    [SerializeField] private float rotationSpeed = 10f;
    private bool canRotate = true;

    [Header("Menu Panels")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private GameObject maxPlayersPanel;
    [SerializeField] private GameObject confirmExitPanel;
    [SerializeField] private GameObject confirmDeletePanel;

    [Header("UI Elements")]
    [SerializeField] private Image logoImage;
    [SerializeField] private GameObject leftButtons;
    [SerializeField] private GameObject playButton;
    [SerializeField] private GameObject continueButton;

    [Header("Camera Reference")]
    [SerializeField] private GameObject mainCamera;

    #region Unity Lifecycle

    /// <summary>
    /// Initializes UI state and triggers the opening animation sequence.
    /// </summary>
    private void Start()
    {
        canRotate = true;
        mainMenuPanel.SetActive(true);
        PlayMenuEntranceAnimation();
    }

    /// <summary>
    /// Rotates the menu UI slowly for visual appeal.
    /// </summary>
    private void Update()
    {
        if (canRotate)
        {
            transform.Rotate(0f, rotationSpeed * Time.deltaTime, 0f);
        }
    }

    #endregion

    #region UI Animation

    /// <summary>
    /// Plays the entrance animation for the menu using DOTween sequences.
    /// </summary>
    private void PlayMenuEntranceAnimation()
    {
        string saveFilePath = Path.Combine(Application.persistentDataPath, "savegame.json");

        Sequence entranceSequence = DOTween.Sequence();

        entranceSequence.Append(leftButtons.transform.DOLocalMoveX(0f, 0.5f).SetEase(Ease.OutBounce));

        if (File.Exists(saveFilePath))
        {
            continueButton.SetActive(true);
            entranceSequence.Join(playButton.transform.DOLocalMoveY(358f, 0.5f).SetEase(Ease.OutBounce));
        }
        else { entranceSequence.Join(playButton.transform.DOLocalMoveY(219f, 0.5f).SetEase(Ease.OutBounce)); }
        
        entranceSequence.Join(logoImage.transform.DOScale(5f, 0.5f).SetEase(Ease.OutBounce));
    }

    #endregion

    #region Button Callbacks

    /// <summary>
    /// Called when "Start" is pressed. Opens the player selection menu.
    /// </summary>
    public void OnStartPressed()
    {
        GameManager.Instance.PlayClickSound();

        mainMenuPanel.SetActive(false);
        maxPlayersPanel.SetActive(true);
    }

    /// <summary>
    /// Called when "Continue" is pressed. Move the camera to load scene.
    /// </summary>
    public void OnContinuePressed()
    {
        GameManager.Instance.PlayClickSound();

        mainMenuPanel.SetActive(false);

        GameManager.Instance.MoveObjectToPoint(mainCamera,new Vector3(1.42f, 2.84f, 12.31f), Quaternion.Euler(36.512f, 192.58f, 0f), "QuestionsContinue");
    }

    /// <summary>
    /// Called when "Delete" is pressed. Delete the last saved game.
    /// </summary>
    public void OnDeletePressed()
    {
        GameManager.Instance.PlayClickSound();

        confirmDeletePanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    public void OnCancelDeletePressed()
    {
        confirmDeletePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void OnConfirmDeletePressed()
    {
        GameSaveManager.Instance.DeleteSave();

        continueButton.SetActive(false);
        playButton.transform.DOLocalMoveY(219f, 0.5f).SetEase(Ease.OutBounce);

        confirmDeletePanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Called when "Options" is pressed. Moves the camera to the options scene.
    /// </summary>
    public void OnOptionsPressed()
    {
        GameManager.Instance.PlayClickSound();
        mainMenuPanel.SetActive(false);

        GameManager.Instance.MoveObjectToPoint(mainCamera,new Vector3(13.97f, 2.21f, -1.25f),Quaternion.Euler(36.151f, -72.055f, 0f),"Options");
    }

    /// <summary>
    /// Called when "Questions" is pressed. Moves the camera to the question importer scene.
    /// </summary>
    public void OnQuestionsPressed()
    {
        GameManager.Instance.PlayClickSound();
        mainMenuPanel.SetActive(false);

        GameManager.Instance.MoveObjectToPoint(
            mainCamera,
            new Vector3(13.97f, 2.21f, -1.25f),
            Quaternion.Euler(36.151f, -72.055f, 0f),
            "PreguntasImporter"
        );
    }

    /// <summary>
    /// Called when "Quit" is pressed. Opens confirmation panel.
    /// </summary>
    public void OnQuitPressed()
    {
        GameManager.Instance.PlayClickSound();

        confirmExitPanel.SetActive(true);
        mainMenuPanel.SetActive(false);
    }

    /// <summary>
    /// Called when "No" is pressed in the quit confirmation. Returns to main menu.
    /// </summary>
    public void OnCancelQuitPressed()
    {
        GameManager.Instance.PlayClickSound();

        confirmExitPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Called when "Yes" is pressed in the quit confirmation. Exits the game.
    /// </summary>
    public void OnConfirmQuitPressed()
    {
        GameManager.Instance.PlayClickSound();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    /// <summary>
    /// Called when "Back" is pressed in the player selection menu. Returns to main menu.
    /// </summary>
    public void OnBackPressed()
    {
        GameManager.Instance.PlayClickSound();

        maxPlayersPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    /// <summary>
    /// Sets max players to 2 and transitions to the Questions scene.
    /// </summary>
    public void OnSelect2Players() => HandlePlayerSelection(2);

    /// <summary>
    /// Sets max players to 3 and transitions to the Questions scene.
    /// </summary>
    public void OnSelect3Players() => HandlePlayerSelection(3);

    /// <summary>
    /// Sets max players to 4 and transitions to the Questions scene.
    /// </summary>
    public void OnSelect4Players() => HandlePlayerSelection(4);

    #endregion

    #region Private Helpers

    /// <summary>
    /// Handles common logic for selecting number of players.
    /// </summary>
    private void HandlePlayerSelection(int maxPlayers)
    {
        GameManager.Instance.PlayClickSound();

        GameManager.Instance.MaxPlayers = maxPlayers;

        maxPlayersPanel.SetActive(false);
        canRotate = false;

        GameManager.Instance.MoveObjectToPoint(
            mainCamera,
            new Vector3(1.42f, 2.84f, 12.31f),
            Quaternion.Euler(36.512f, 192.58f, 0f),
            "Questions"
        );
    }

    #endregion
}
