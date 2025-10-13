using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Datos de partida")]
    // Mapa color → categoría
    private Dictionary<QuesitoColor, string> categoriasPorColor = new Dictionary<QuesitoColor, string>();
    public int maxPlayer = 0;

    [Header("Sonido")]
    [SerializeField] private AudioClip click;
    [SerializeField] private AudioClip acierto;
    [SerializeField] private AudioClip fallo;
    [SerializeField] private AudioClip temporizador;

    private AudioSource audioSource;

    

    void Awake()
    {
        Application.targetFrameRate = 60;

        // patrón Singleton
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            DontDestroyOnLoad(this.gameObject);
        }

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = false;

    }
    public void SetCategoriaParaColor(QuesitoColor color, string categoria)
    {
        categoriasPorColor[color] = categoria;
        Debug.Log($"Asignada categoría '{categoria}' al color {color}");
    }
    public string GetCategoriaParaColor(QuesitoColor color)
    {
        if (categoriasPorColor.TryGetValue(color, out var categoria))
            return categoria;

        Debug.LogWarning($"No hay categoría asignada al color {color}");
        return null;
    }

    public Dictionary<QuesitoColor, string> GetTodasLasCategorias()
    {
        return new Dictionary<QuesitoColor, string>(categoriasPorColor);
    }

    //Metedos para AudioClips

    public void AudioClick()
    {
        audioSource.clip = click;
        audioSource.Play();
    }

    public void AudioAcierto()
    {
        audioSource.clip = acierto;
        audioSource.Play();
    }

    public void AudioFallo()
    {
        audioSource.clip = fallo;
        audioSource.Play();
    }

    public void AudioTemporizador()
    {
        audioSource.clip = temporizador;
        audioSource.Play();
    }

    public void MoveCamToPoint(GameObject obj, Vector3 targetPos, Quaternion targetRot, string scene)
    {
        float moveDuration = 1.5f;
        float parabolaHeight = 3f;

        Vector3 startPos = obj.transform.position;
        Quaternion startRot = obj.transform.rotation;

        //Espera inicial
        DOVirtual.DelayedCall(0f, () =>
        {
            // 2️⃣ Creamos la secuencia DOTween
            Sequence seq = DOTween.Sequence();

            // Tween "virtual" para controlar el parámetro t (0→1)
            float t = 0f;
            seq.Append(
                DOVirtual.Float(0f, 1f, moveDuration, value =>
                {
                    t = value;

                    // Interpolación lineal + parábola
                    Vector3 linearPos = Vector3.Lerp(startPos, targetPos, t);
                    float parabola = 4f * parabolaHeight * t * (1 - t);
                    linearPos.y += parabola;
                    obj.transform.position = linearPos;

                    // Rotación
                    obj.transform.rotation = Quaternion.Slerp(startRot, targetRot, t);
                })
                .SetEase(Ease.InOutQuad)
            );

            // 3️⃣ Al terminar, fijamos la posición final y cargamos la escena
            seq.OnComplete(() =>
            {
                obj.transform.position = targetPos;
                obj.transform.rotation = targetRot;
                SceneManager.LoadScene(scene);
            });
        });
    }
}
