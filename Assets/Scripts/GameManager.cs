using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;


    [Header("Datos de partida")]
    // Mapa color → categoría
    private Dictionary<QuesitoColor, string> categoriasPorColor = new Dictionary<QuesitoColor, string>();

    public GameObject menuButtons;
    public GameObject maxPlayerMenu;

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

    public void StartPressed()
    {
        menuButtons.SetActive(false);

        maxPlayerMenu.SetActive(true);
    }

    public void OptionsPressed()
    {

    }
    public void QuitPressed()
    {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }

    public void BackPressed()
    {
        maxPlayerMenu.SetActive(false);

        menuButtons.SetActive(true);
    }

    public void PlayerMax2()
    {
        maxPlayer = 2;

        SceneManager.LoadScene("Questions");
    }

    public void PlayerMax3()
    {
        maxPlayer = 3;

        SceneManager.LoadScene("Questions");
    }

    public void PlayerMax4()
    {
        maxPlayer = 4;

        SceneManager.LoadScene("Questions");
    }


}
