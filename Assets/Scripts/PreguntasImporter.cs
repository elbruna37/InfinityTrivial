using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PreguntasImporter : MonoBehaviour
{
    [Header("UI Input")]
    public TMP_InputField inputJson;
    public TMP_Text resumenText;

    private string filePath;

    void Awake()
    {
        filePath = Path.Combine(Application.persistentDataPath, "preguntas.json");

        // Si no existe, lo inicializamos desde Resources
        if (!File.Exists(filePath))
        {
            TextAsset baseFile = Resources.Load<TextAsset>("preguntas");
            if (baseFile != null)
                File.WriteAllText(filePath, baseFile.text);
        }

        MostrarResumen();
    }

    [System.Serializable]
    private class PreguntaArrayWrapper
    {
        public Pregunta[] preguntas;
    }

    public void ImportarPreguntas()
    {
        string raw = inputJson.text.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            Debug.LogWarning("⚠️ No se ha pegado ningún JSON");
            return;
        }

        // 1. Cargar JSON actual
        string filePath = Path.Combine(Application.persistentDataPath, "preguntas.json");
        string existingJson = File.ReadAllText(filePath);
        PreguntaLote lote = JsonUtility.FromJson<PreguntaLote>(existingJson);
        List<Pregunta> lista = new List<Pregunta>(lote.questions);

        // 2. Envolver si el usuario pega un array
        if (raw.StartsWith("["))
        {
            raw = "{\"preguntas\":" + raw + "}";
        }

        PreguntaArrayWrapper wrapper = JsonUtility.FromJson<PreguntaArrayWrapper>(raw);
        if (wrapper != null && wrapper.preguntas != null)
        {
            lista.AddRange(wrapper.preguntas);
            Debug.Log($"✅ Importadas {wrapper.preguntas.Length} nuevas preguntas");
        }
        else
        {
            Debug.LogError("⚠️ El JSON pegado no es válido o no tiene preguntas");
            return;
        }

        // 3. Guardar JSON final
        lote.questions = lista.ToArray();
        string nuevoJson = JsonUtility.ToJson(lote, true);
        File.WriteAllText(filePath, nuevoJson);

        Debug.Log($"📘 Total de preguntas ahora: {lote.questions.Length}");

        inputJson.text = "";
    }

    public void BorrarPreguntas()
    {
        // Confirmación opcional (por ejemplo, podrías mostrar un popup en UI antes de ejecutar esto)
        Debug.Log("⚠️ Restaurando preguntas originales...");

        // Sobrescribir con el JSON base de Resources
        TextAsset jsonFile = Resources.Load<TextAsset>("preguntas");
        if (jsonFile != null)
        {
            File.WriteAllText(filePath, jsonFile.text);

            inputJson.text = "";

            // Actualizar resumen
            MostrarResumen();
        }
        else
        {
            Debug.LogError("No se encontró preguntas.json en Resources");
        }
    }

    private void MostrarResumen()
    {
        if (!File.Exists(filePath))
        {
            resumenText.text = "⚠️ No se encontró preguntas.json";
            return;
        }

        string json = File.ReadAllText(filePath);
        if (string.IsNullOrWhiteSpace(json))
        {
            resumenText.text = "⚠️ El archivo está vacío";
            return;
        }

        try
        {
            PreguntaLote lote = JsonUtility.FromJson<PreguntaLote>(json);
            if (lote == null || lote.questions == null)
            {
                resumenText.text = "⚠️ Error al leer preguntas";
                return;
            }

            var resumen = new Dictionary<string, (int facil, int media, int dificil)>();

            foreach (var p in lote.questions)
            {
                if (!resumen.ContainsKey(p.categoria))
                    resumen[p.categoria] = (0, 0, 0);

                var entry = resumen[p.categoria];
                switch (p.dificultad.ToLower())
                {
                    case "facil": entry.facil++; break;
                    case "media": entry.media++; break;
                    case "dificil": entry.dificil++; break;
                }
                resumen[p.categoria] = entry;
            }

            // Construir texto
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($" Resumen ({lote.questions.Length} preguntas):\n");
            foreach (var kvp in resumen)
            {
                sb.AppendLine($"• {kvp.Key} → Fácil: {kvp.Value.facil}, Media: {kvp.Value.media}, Difícil: {kvp.Value.dificil}");
            }

            resumenText.text = sb.ToString();
        }
        catch (System.Exception e)
        {
            resumenText.text = "⚠️ Error leyendo JSON: " + e.Message;
        }
    }

    public void VolverAlMenu()
    {
        Destroy(GameManager.Instance.gameObject);

        SceneManager.LoadScene("Menu");
    }
}
