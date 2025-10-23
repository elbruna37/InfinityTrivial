using DG.Tweening;
using Newtonsoft.Json.Bson;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class UIMenuManager : MonoBehaviour
{
    [SerializeField] private float velocidad = 50f;   

    [Header("Botones")]
    public GameObject menuButtons;
    public GameObject maxPlayerMenu;
    public GameObject confirmButtons;
    bool canRotate = true;

    [Header("UI")]
    public Image logo;
    public GameObject leftButtons;
    public GameObject playButton;

    [SerializeField] private GameObject camara;

    private void Start()
    {
        canRotate = true;

        menuButtons.SetActive(true);

        EmergenceMenu();
    }

    void Update()
    {
        if (canRotate)
        {
            transform.Rotate(0f, velocidad * Time.deltaTime, 0f);
        }
    }

    void EmergenceMenu()
    {
        Sequence emergence = DOTween.Sequence();

        emergence.Append(leftButtons.transform.DOLocalMoveX(0, 0.5f).SetEase(Ease.OutBounce));
        emergence.Join(playButton.transform.DOLocalMoveY(-349, 0.5f).SetEase(Ease.OutBounce));
        emergence.Join(logo.transform.DOScale(5, 0.5f).SetEase(Ease.OutBounce));

    }

    public void StartPressed()
    {
        GameManager.Instance.AudioClick();

        menuButtons.SetActive(false);

        maxPlayerMenu.SetActive(true);

    }

    public void OptionsPressed()
    {
        GameManager.Instance.AudioClick();
        menuButtons.SetActive(false);

        GameManager.Instance.MoveCamToPoint(camara, new Vector3(13.97f, 2.21f, -1.25f), Quaternion.Euler(36.151f, -72.055f, 0f), "Options");
    }

    public void QuestionsPressed()
    {
        GameManager.Instance.AudioClick();
        menuButtons.SetActive(false);

        GameManager.Instance.MoveCamToPoint(camara, new Vector3(13.97f, 2.21f, -1.25f), Quaternion.Euler(36.151f, -72.055f, 0f), "PreguntasImporter");
    }

    public void QuitPressed()
    {
        GameManager.Instance.AudioClick();

        confirmButtons.SetActive(true);
        menuButtons.SetActive(false);
    }

    public void NoPressed()
    {
        GameManager.Instance.AudioClick();

        confirmButtons.SetActive(false);
        menuButtons.SetActive(true);
    }

    public void ExitPressed()
    {
        GameManager.Instance.AudioClick();

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }

    public void BackPressed()
    {
        GameManager.Instance.AudioClick();

        maxPlayerMenu.SetActive(false);

        menuButtons.SetActive(true);
    }

    public void PlayerMax2()
    {
        GameManager.Instance.AudioClick();

        GameManager.Instance.maxPlayer = 2;

        maxPlayerMenu.SetActive(false);

        canRotate = false;

        GameManager.Instance.MoveCamToPoint(camara, new Vector3(1.42f, 2.84f, 12.31f), Quaternion.Euler(36.512f, 192.58f, 0f), "Questions");
    }

    public void PlayerMax3()
    {
        GameManager.Instance.AudioClick();

        GameManager.Instance.maxPlayer = 3;

        maxPlayerMenu.SetActive(false);

        canRotate = false;

        GameManager.Instance.MoveCamToPoint(camara, new Vector3(1.42f, 2.84f, 12.31f), Quaternion.Euler(36.512f, 192.58f, 0f), "Questions");
    }

    public void PlayerMax4()
    {
        

        GameManager.Instance.maxPlayer = 4;

        maxPlayerMenu.SetActive(false);

        canRotate = false;

        GameManager.Instance.MoveCamToPoint(camara, new Vector3(1.42f, 2.84f, 12.31f), Quaternion.Euler(36.512f, 192.58f, 0f), "Questions");
    }
}
