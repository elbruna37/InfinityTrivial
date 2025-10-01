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

    [Header("Sonido")]
    [SerializeField] private AudioClip click;
    private AudioSource audioSource;

    public int maxPlayer = 0;

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

}
