using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    [Header("Referencias")]

    [Header("UI Panels")]
    public GameObject panelPregunta;
    public GameObject panelRespuestas;
    public GameObject quesitoPanel;

    [Header("Textos")]
    public TMP_Text enunciadoTMP;
    public TMP_Text contadorTMP;
    public TMP_Text textInformation;
    

    [Header("Opciones de respuesta")]
    public Button[] botonesOpciones;
    public TMP_Text[] textosOpciones;

    private Pregunta preguntaActual;
    private Action<bool> callbackRespuesta;
    private Coroutine timerCoroutine;

    [Header("Textos de categorías en el tablero")]
    public TMP_Text categoriaAzulTMP;
    public TMP_Text categoriaAmarilloTMP;
    public TMP_Text categoriaNaranjaTMP;
    public TMP_Text categoriaRosaTMP;
    public TMP_Text categoriaMoradoTMP;
    public TMP_Text categoriaVerdeTMP;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        var categorias = GameManager.Instance.GetTodasLasCategorias();
        ActualizarCategorias(categorias);
    }

    public void MostrarPregunta(Pregunta pregunta, Action<bool> onAnswered)
    {
        preguntaActual = pregunta;
        callbackRespuesta = onAnswered;

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(FlujoPregunta());
    }

    private IEnumerator FlujoPregunta()
    {
        quesitoPanel.SetActive(false);
        // 🔹 Mostrar solo enunciado 30s
        panelPregunta.SetActive(true);
        panelRespuestas.SetActive(false);
        enunciadoTMP.text = preguntaActual.enunciado;
        TurnManager.Instance.canDestroy = true;

        yield return new WaitForSeconds(6f);

        panelPregunta.SetActive(false);

        // 🔹 Mostrar opciones
        panelRespuestas.SetActive(true);
        PrepararOpciones();

        float tiempo = 15f;
        while (tiempo > 0f)
        {
            contadorTMP.text = Mathf.CeilToInt(tiempo).ToString();
            yield return new WaitForSeconds(1f);
            tiempo -= 1f;
        }

        OnRespuestaFinalizada(false);
    }

    private void PrepararOpciones()
    {
        for (int i = 0; i < botonesOpciones.Length; i++)
        {
            if (i < preguntaActual.opciones.Length)
            {
                textosOpciones[i].text = preguntaActual.opciones[i];
                int indice = i;
                botonesOpciones[i].onClick.RemoveAllListeners();
                botonesOpciones[i].onClick.AddListener(() => OnOpcionSeleccionada(indice));
                botonesOpciones[i].interactable = true;
            }
        }
    }

    private void OnOpcionSeleccionada(int indice)
    {
        bool correcta = indice == preguntaActual.indiceCorrecta;

        // Bloquear todos los botones al seleccionar una opción
        foreach (var btn in botonesOpciones)
            btn.interactable = false;

        // Feedback
        if (correcta)
            textInformation.text = "Respuesta correcta";
        else
            textInformation.text = "Respuesta incorrecta → pierde turno";

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(CerrarConRetraso(correcta));
    }

    private IEnumerator CerrarConRetraso(bool correcta)
    {
        yield return new WaitForSeconds(2f);
        OnRespuestaFinalizada(correcta);
    }

    private void OnRespuestaFinalizada(bool correcta)
    {
        panelPregunta.SetActive(false);
        panelRespuestas.SetActive(false);
        quesitoPanel.SetActive(true);
        textInformation.text = "";
        callbackRespuesta?.Invoke(correcta);
    }

    public void ActualizarCategorias(Dictionary<QuesitoColor, string> categorias)
    {
        if (categorias.TryGetValue(QuesitoColor.Azul, out string azul))
            categoriaAzulTMP.text = azul;

        if (categorias.TryGetValue(QuesitoColor.Amarillo, out string amarillo))
            categoriaAmarilloTMP.text = amarillo;

        if (categorias.TryGetValue(QuesitoColor.Naranja, out string naranja))
            categoriaNaranjaTMP.text = naranja;

        if (categorias.TryGetValue(QuesitoColor.Rosa, out string rosa))
            categoriaRosaTMP.text = rosa;

        if (categorias.TryGetValue(QuesitoColor.Morado, out string morado))
            categoriaMoradoTMP.text = morado;

        if (categorias.TryGetValue(QuesitoColor.Verde, out string verde))
            categoriaVerdeTMP.text = verde;
    }
}

