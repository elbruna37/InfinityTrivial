using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UIMenuManager : MonoBehaviour
{
    [SerializeField] private float velocidad = 50f;   

    [Header("Botones")]
    public GameObject menuButtons;
    public GameObject maxPlayerMenu;
    bool canRotate = true;

    [Header("Camera Motion")]
    [SerializeField] private float moveDuration = 2f; // Tiempo en segundos
    [SerializeField] private float parabolaHeight = 3f; // Altura máxima de la parábola

    [SerializeField] private GameObject camara;

    private void Start()
    {
        canRotate = true;
    }

    void Update()
    {
        if (canRotate)
        {
            transform.Rotate(0f, velocidad * Time.deltaTime, 0f);
        }
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

        SceneManager.LoadScene("PreguntasImporter");
    }
    public void QuitPressed()
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

        StartCoroutine(MoveToZero(camara));
    }

    public void PlayerMax3()
    {
        GameManager.Instance.AudioClick();

        GameManager.Instance.maxPlayer = 3;

        maxPlayerMenu.SetActive(false);

        canRotate = false;

        StartCoroutine(MoveToZero(camara));
    }

    public void PlayerMax4()
    {
        

        GameManager.Instance.maxPlayer = 4;

        maxPlayerMenu.SetActive(false);

        canRotate = false;

        StartCoroutine(MoveToZero(camara));
    }


    private IEnumerator MoveToZero(GameObject obj)
    {
        Vector3 startPos = obj.transform.position;
        Vector3 targetPos = new Vector3(1.42f, 2.84f, 12.31f);

        Quaternion startRot = obj.transform.rotation;
        Quaternion targetRot = Quaternion.Euler(36.512f, 192.58f, 0f);

        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);

            // Movimiento lineal base
            Vector3 linearPos = Vector3.Lerp(startPos, targetPos, t);

            // Offset parabólico en Y
            float parabola = 4f * parabolaHeight * t * (1 - t);
            linearPos.y += parabola;

            obj.transform.position = linearPos;

            // Rotación interpolada hacia la final
            obj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);

            // Cuando llega al 50%, lanza el fade
            /*if (!fadeStarted && t >= 0.5f)
            {
                fadeStarted = true;
                StartCoroutine(FadeToBlack());
            }
            */
            yield return null;
        }

        // Asegura que termine exacto en destino
        obj.transform.position = targetPos;
        obj.transform.rotation = targetRot;

        SceneManager.LoadScene("Questions");
    }
}
