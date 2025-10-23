using DG.Tweening;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ImporterManager : MonoBehaviour
{
    public GameObject tarjeta;
    public GameObject camara;

    [Header("Referencias UI")]
    public CanvasGroup instrucciones;
    public CanvasGroup menuImporter;
    public CanvasGroup menuHistorial;

    [Header("Referencias UI / COPIAR")]
    public TMP_InputField numeroPreguntasInput;
    public TMP_InputField categoriaInput;
    public TMP_Dropdown dificultadDropdown;
    public Button copiarButton;

    [Header("Referencias UI / PEGAR")]
    public TMP_InputField inputJson;

    [Header("Referencias UI / HISTORIAL")]
    public Transform contentParent; 
    public GameObject itemPrefab;

    private string filePath;
    private string historialPath;

    [Header("Animacion")]
    Vector3 startPos;
    Quaternion startRot;

    [System.Serializable]
    private class PreguntaArrayWrapper
    {
        public Pregunta[] preguntas;
    }

    [System.Serializable]
    public class CategoriaData
    {
        public string nombre;
        public int faciles;
        public int medias;
        public int dificiles;
    }

    void Awake()
    {
        startPos = tarjeta.transform.position;
        startRot = tarjeta.transform.rotation;
        

        filePath = Path.Combine(Application.persistentDataPath, "preguntas.json");

        // Si no existe, lo inicializamos desde Resources
        if (!File.Exists(filePath))
        {
            TextAsset baseFile = Resources.Load<TextAsset>("preguntas");
            if (baseFile != null)
                File.WriteAllText(filePath, baseFile.text);
        }

        historialPath = Path.Combine(Application.persistentDataPath, "historial_importadas.json");

        // Si no existe, creamos un JSON vacío
        if (!File.Exists(historialPath))
        {
            File.WriteAllText(historialPath, "{\"preguntas\":[]}");
        }

        if (numeroPreguntasInput != null)
        {
            numeroPreguntasInput.characterLimit = 2; // Máximo 2 cifras
            numeroPreguntasInput.onValueChanged.AddListener(ValidarNumero);
        }

        if (categoriaInput != null)
        {
            categoriaInput.characterLimit = 20; // Máximo 20 caracteres
            categoriaInput.onValueChanged.AddListener(ValidarTexto);
        }

        if (copiarButton != null)
            copiarButton.onClick.AddListener(CopiarPromptAlPortapapeles);

        

        AnimateTarjeta();
    }

    private void ValidarNumero(string value)
    {
        // Solo números
        string soloNumeros = Regex.Replace(value, @"[^0-9]", "");
        if (soloNumeros != value)
            numeroPreguntasInput.text = soloNumeros;
    }

    private void ValidarTexto(string value)
    {
        // Solo letras (mayúsculas o minúsculas) y espacios
        string soloLetras = Regex.Replace(value, @"[^a-zA-ZáéíóúÁÉÍÓÚüÜñÑ\s]", "");
        if (soloLetras != value)
            categoriaInput.text = soloLetras;
    }

    private void CopiarPromptAlPortapapeles()
    {
        // Obtenemos los valores de los campos
        string numeroPreguntas = numeroPreguntasInput != null ? numeroPreguntasInput.text : "10";
        string categoria = categoriaInput != null ? categoriaInput.text : "General";
        string idioma = PlayerPrefs.GetString("idioma", "español");

        string dificultad = "fácil";
        if (dificultadDropdown != null)
        {
            switch (dificultadDropdown.value)
            {
                case 0: dificultad = "fácil"; break;
                case 1: dificultad = "media"; break;
                case 2: dificultad = "difícil"; break;
            }
        }

        // Construimos el prompt
        string prompt = $"Genera {numeroPreguntas} preguntas de trivial en {idioma} en formato JSON con el siguiente esquema:\n\n" +
                        $"categoria: \"{categoria}\".\n" +
                        $"enunciado: string con la pregunta.\n\n" +
                        $"opciones: array de 4 respuestas posibles.\n\n" +
                        $"indiceCorrecta: índice 0–3 correcto.\n\n" +
                        $"dificultad: \"{dificultad}\"\n" +
                        $"Devuelve un array JSON de objetos, sin explicaciones.";

        // Copiamos al portapapeles
        GUIUtility.systemCopyBuffer = prompt;

        Debug.Log("✅ Prompt copiado al portapapeles:\n" + prompt);
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

        string historialJson = File.ReadAllText(historialPath);
        PreguntaArrayWrapper historialWrapper = JsonUtility.FromJson<PreguntaArrayWrapper>(historialJson);

        // Creamos una lista con las ya importadas
        List<Pregunta> historialLista = new List<Pregunta>();
        if (historialWrapper.preguntas != null)
            historialLista.AddRange(historialWrapper.preguntas);

        // Añadimos las nuevas
        historialLista.AddRange(wrapper.preguntas);

        // Guardamos el historial actualizado
        PreguntaArrayWrapper nuevoHistorial = new PreguntaArrayWrapper { preguntas = historialLista.ToArray() };
        string nuevoHistorialJson = JsonUtility.ToJson(nuevoHistorial, true);
        File.WriteAllText(historialPath, nuevoHistorialJson);
    }

    private List<CategoriaData> ObtenerCategorias()
    {
        if (!File.Exists(historialPath))
            return new List<CategoriaData>();

        string json = File.ReadAllText(historialPath);
        PreguntaArrayWrapper wrapper = JsonUtility.FromJson<PreguntaArrayWrapper>(json);

        if (wrapper.preguntas == null || wrapper.preguntas.Length == 0)
            return new List<CategoriaData>();

        Dictionary<string, CategoriaData> categorias = new Dictionary<string, CategoriaData>();

        foreach (var p in wrapper.preguntas)
        {
            if (string.IsNullOrEmpty(p.categoria)) continue;

            if (!categorias.ContainsKey(p.categoria))
                categorias[p.categoria] = new CategoriaData { nombre = p.categoria };

            switch (p.dificultad.ToLower())
            {
                case "fácil":
                case "facil":
                    categorias[p.categoria].faciles++;
                    break;
                case "media":
                    categorias[p.categoria].medias++;
                    break;
                case "difícil":
                case "dificil":
                    categorias[p.categoria].dificiles++;
                    break;
            }
        }

        return new List<CategoriaData>(categorias.Values);
    }

    public void MostrarCategorias(List<CategoriaData> categorias)
    {
        // Limpia los anteriores
        foreach (Transform child in contentParent)
            Destroy(child.gameObject);

        // Crea los nuevos
        foreach (var cat in categorias)
        {
            GameObject item = Instantiate(itemPrefab, contentParent);
            TMP_Text[] textos = item.GetComponentsInChildren<TMP_Text>();

            // Asumiendo orden: 0 = nombre, 1 = fácil, 2 = media, 3 = difícil
            textos[0].text = cat.nombre;
            textos[1].text = $"Fácil: {cat.faciles}";
            textos[2].text = $"Media: {cat.medias}";
            textos[3].text = $"Difícil: {cat.dificiles}";
        }
    }

    private void AnimateTarjeta()
    {
        Sequence tarjetaSeq = DOTween.Sequence();

        Vector3 loopPos = startPos + Vector3.up * 1.5f;

        tarjetaSeq.Append(tarjeta.transform.DOMove(loopPos, 1f).SetEase(Ease.OutQuad));  //Suma 1.5 a la altura del objeto
        tarjetaSeq.Insert(tarjetaSeq.Duration() - 1f + 0.3f, tarjeta.transform.DORotateQuaternion(Quaternion.Euler(-208.997f, 107.955f, -90.02499f), 1.5f)).SetEase(Ease.InOutQuad);  //0.3 segundos depués rota la tarjeta
        tarjetaSeq.Insert(tarjetaSeq.Duration() - 2.2f + 0.7f, tarjeta.transform.DOMove(new Vector3(12.84f, 1.24f, -0.74f), 2f).SetEase(Ease.OutQuad));     //Y en el segundo 0.7 hace el movimiento hacia la camara
    }

    private void AnimateTarjetaAgain()
    {
        Sequence tarjetaSeq = DOTween.Sequence();

        tarjetaSeq.Append(tarjeta.transform.DORotateQuaternion(startRot, 0.5f).SetEase(Ease.InOutQuad));
        tarjetaSeq.Append(tarjeta.transform.DOMove(startPos, 1f).SetEase(Ease.OutQuad));

        GameManager.Instance.MoveCamToPoint(camara, new Vector3(0, 8, -10.7f), Quaternion.Euler(48.968f, 0f, 0f), "Menu");
    }

    public void AceptarPressed()
    {
        // Desvanece las instrucciones
        instrucciones.DOFade(0f, 0.6f)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                instrucciones.gameObject.SetActive(false);

                // Activa el nuevo panel y lo hace aparecer
                menuImporter.gameObject.SetActive(true);
                menuImporter.alpha = 0f;
                menuImporter.DOFade(1f, 0.6f).SetEase(Ease.OutQuad);
            });
    }

    public void HistorialPressed()
    {
        List<CategoriaData> categorias = ObtenerCategorias();
        MostrarCategorias(categorias);

        // Desvanece las instrucciones
        menuImporter.DOFade(0f, 0.6f)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                menuImporter.gameObject.SetActive(false);

                // Activa el nuevo panel y lo hace aparecer
                menuHistorial.gameObject.SetActive(true);
                menuHistorial.alpha = 0f;
                menuHistorial.DOFade(1f, 0.6f).SetEase(Ease.OutQuad);
            });
    }

    public void VolverPressed()
    {
        menuHistorial.DOFade(0f, 0.6f)
            .SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                menuHistorial.gameObject.SetActive(false);

                // Activa el nuevo panel y lo hace aparecer
                menuImporter.gameObject.SetActive(true);
                menuImporter.alpha = 0f;
                menuImporter.DOFade(1f, 0.6f).SetEase(Ease.OutQuad);
            });
    }

    public void BackMenu()
    {
        AnimateTarjetaAgain();
    }
}
