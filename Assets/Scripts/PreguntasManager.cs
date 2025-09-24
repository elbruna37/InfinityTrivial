using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PreguntasManager : MonoBehaviour
{
    public static PreguntasManager Instance;

    [Header("Preguntas disponibles")]
    public Pregunta[] todasLasPreguntas;
    public List<string> categoriasDisponibles;

    // Historial → clave "categoria_dificultad"
    private Dictionary<string, HashSet<int>> historialPreguntas = new Dictionary<string, HashSet<int>>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Se mantiene entre escenas
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // Cargar JSON de Resources/preguntas.json
        TextAsset jsonFile = Resources.Load<TextAsset>("preguntas");
        if (jsonFile != null)
        {
            PreguntaLote lote = JsonUtility.FromJson<PreguntaLote>(jsonFile.text);
            todasLasPreguntas = lote.questions;

            // Extraer categorías únicas
            categoriasDisponibles = todasLasPreguntas
                .Select(p => p.categoria)
                .Distinct()
                .ToList();

            Debug.Log($"Cargadas {todasLasPreguntas.Length} preguntas en {categoriasDisponibles.Count} categorías.");
        }
        else
        {
            Debug.LogError("No se encontró preguntas.json en Resources");
        }
    }

    // Devuelve preguntas filtradas por categoría
    public void AskRandomQuestion(string categoria, System.Action<bool> onAnswered)
    {
        string dificultad = ElegirDificultadAleatoria();
        Pregunta seleccionada = GetPreguntaNoRepetida(categoria, dificultad);

        if (seleccionada == null)
        {
            Debug.LogWarning($"No hay preguntas para {categoria} en dificultad {dificultad}");
            onAnswered?.Invoke(false);
            return;
        }

        Debug.Log($"Pregunta seleccionada ({categoria}, {dificultad}): {seleccionada.enunciado}");

        // Delegamos en el UIManager
        UIManager.Instance.MostrarPregunta(seleccionada, onAnswered);
    }

    public void AskRandomQuesitoQuestion(string categoria, System.Action<bool> onAnswered)
    {
        string dificultad = ElegirDificultadQuesito();
        Pregunta seleccionada = GetPreguntaNoRepetida(categoria, dificultad);

        if (seleccionada == null)
        {
            Debug.LogWarning($"No hay preguntas para {categoria} en dificultad {dificultad}");
            onAnswered?.Invoke(false);
            return;
        }

        Debug.Log($"Pregunta seleccionada ({categoria}, {dificultad}): {seleccionada.enunciado}");

        // Delegamos en el UIManager
        UIManager.Instance.MostrarPregunta(seleccionada, onAnswered);
    }

    private Pregunta GetPreguntaNoRepetida(string categoria, string dificultad)
    {
        string key = $"{categoria}_{dificultad}";

        if (!historialPreguntas.ContainsKey(key))
            historialPreguntas[key] = new HashSet<int>();

        // Filtrar con índices
        var preguntasFiltradas = todasLasPreguntas
            .Select((p, i) => new { Pregunta = p, Index = i })
            .Where(x => x.Pregunta.categoria == categoria && x.Pregunta.dificultad == dificultad)
            .ToList();

        // Excluir ya usadas
        var disponibles = preguntasFiltradas
            .Where(x => !historialPreguntas[key].Contains(x.Index))
            .ToList();

        if (disponibles.Count == 0)
        {
            Debug.Log($"⚠️ Se agotaron preguntas de {key}. Reiniciando historial de ese grupo.");
            historialPreguntas[key].Clear();
            disponibles = preguntasFiltradas; // reset pool
        }

        if (disponibles.Count == 0) return null; // seguridad

        var seleccion = disponibles[Random.Range(0, disponibles.Count)];
        historialPreguntas[key].Add(seleccion.Index);

        return seleccion.Pregunta;
    }

    private string ElegirDificultadAleatoria()
    {
        float r = Random.value;
        if (r < 0.5f) return "facil";
        else if (r < 0.9f) return "media";
        else return "dificil";
    }

    private string ElegirDificultadQuesito()
    {
        float r = Random.value;
        if (r < 0.7f) return "dificil";
        else return "media";
    }
}

