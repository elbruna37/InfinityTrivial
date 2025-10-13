using DG.Tweening;
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
    public RectTransform upRespuestasPanel;
    public RectTransform downRespuestasPanel;
    public Image fondo;
    private Color colorOriginal;

    [Header("Indicador de Dificultad")]
    public Image[] dificultadIcons;

    [Header("Temporizador")]
    public Image rellenoTemporizador;
    public Transform agujaTemporizador;
    public TMP_Text contadorTMP;
    public float duracion = 30f;
    public float rotacionInicial = 0f;
    public float rotacionFinal = -360f;
    private Tween rotTween;
    private Tween fillTween;

    [Header("Textos")]
    public TMP_Text enunciadoTMP;
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

    [Header("DoTween")]
    Sequence upDownPanelsSeq = DOTween.Sequence();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        var categorias = GameManager.Instance.GetTodasLasCategorias();
        ActualizarCategorias(categorias);
        colorOriginal = fondo.color;
    }

    public void MostrarPregunta(Pregunta pregunta, string dificultad, Action<bool> onAnswered)
    {
        preguntaActual = pregunta;
        callbackRespuesta = onAnswered;

        MostrarDificultad(dificultad);

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(FlujoPregunta());
    }

    private void MostrarDificultad(string dificultad)
    {
        // Oculta todos los iconos primero
        foreach (var icon in dificultadIcons)
            icon.gameObject.SetActive(false);

        int cantidadVisible = 0;

        switch (dificultad.ToLower())
        {
            case "facil":
                cantidadVisible = 1;
                break;
            case "media":
                cantidadVisible = 2;
                break;
            case "dificil":
                cantidadVisible = 3;
                break;
        }

        for (int i = 0; i < cantidadVisible && i < dificultadIcons.Length; i++)
            dificultadIcons[i].gameObject.SetActive(true);
    }

    private IEnumerator FlujoPregunta()
    {
        quesitoPanel.SetActive(false);
        // 🔹 Mostrar solo enunciado 30s

        fondo.color = colorOriginal;
        panelPregunta.SetActive(true);
        panelRespuestas.SetActive(false);
        enunciadoTMP.fontSize = 70;
        enunciadoTMP.text = preguntaActual.enunciado;
        TurnManager.Instance.canDestroy = true;

        yield return new WaitForSeconds(6f);

        // 🔹 Reducir tamaño Texto Pregunta
        float elapsed = 0f;
        float startSize = enunciadoTMP.fontSize;

        Temporizador();
        while (elapsed < 0.3f)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / 0.3f);
            enunciadoTMP.fontSize = Mathf.Lerp(startSize, 50, t);
            yield return null;
        }

        enunciadoTMP.fontSize = 50;

        // 🔹 Mostrar opciones
        panelRespuestas.SetActive(true);
        
        upDownPanelsSeq.Append(upRespuestasPanel.DOAnchorPosY(upRespuestasPanel.anchoredPosition.y - 483, 0.3f).SetEase(Ease.OutQuad));
        upDownPanelsSeq.Join(downRespuestasPanel.DOAnchorPosY(downRespuestasPanel.anchoredPosition.y + 483, 0.3f).SetEase(Ease.OutQuad));

        PrepararOpciones();

        float tiempo = 30f;
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
        rotTween.Kill(); fillTween.Kill();

        bool tiempoAgotado = indice == -1;
        bool correcta = !tiempoAgotado && indice == preguntaActual.indiceCorrecta;

        // Bloquear todos los botones al seleccionar una opción
        foreach (var btn in botonesOpciones)
            btn.interactable = false;

        if (tiempoAgotado)
        {
            GameManager.Instance.AudioFallo();
            ParpadearFondo(Color.red);
            if (timerCoroutine != null) StopCoroutine(timerCoroutine);
            timerCoroutine = StartCoroutine(CerrarConRetraso(false));
            return;
        }

        // Feedback
        if (correcta)
            //textInformation.text = "Respuesta correcta";
            GameManager.Instance.AudioAcierto();

        else
            //textInformation.text = "Respuesta incorrecta → pierde turno";
            GameManager.Instance.AudioFallo();

        ParpadearFondo(correcta ? Color.green : Color.red);

        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
        timerCoroutine = StartCoroutine(CerrarConRetraso(correcta));
    }

    private void ParpadearFondo(Color colorObjetivo)
    {
        int repeticionesParpadeo = 3;
        float duracionParpadeo = 0.5f;

        // Secuencia de parpadeo
        Sequence seq = DOTween.Sequence();

        for (int i = 0; i < repeticionesParpadeo; i++)
        {
            seq.Append(fondo.DOColor(colorObjetivo, duracionParpadeo / (2 * repeticionesParpadeo)))
               .Append(fondo.DOColor(colorOriginal, duracionParpadeo / (2 * repeticionesParpadeo)));
        }

        // Asegurar que al final quede en su color original
        seq.OnComplete(() => fondo.color = colorObjetivo);
    }

    public void Temporizador()
    {
        //resetear estados
        DOTween.Kill(agujaTemporizador);
        DOTween.Kill(rellenoTemporizador);

        GameManager.Instance.AudioTemporizador();

        agujaTemporizador.rotation = Quaternion.Euler(0f, 0f, rotacionInicial);
        rellenoTemporizador.fillAmount = 0f;
        contadorTMP.text = Mathf.CeilToInt(duracion).ToString();

        // Tween de rotación (aguja)
        rotTween = agujaTemporizador
            .DORotate(new Vector3(0f, 0f, rotacionFinal), duracion, RotateMode.FastBeyond360)
            .SetEase(Ease.Linear).OnComplete(() =>
            {
                OnOpcionSeleccionada(-1);
            });

        // Tween del fillAmount (relleno)
        fillTween = DOTween.To(() => rellenoTemporizador.fillAmount, x => rellenoTemporizador.fillAmount = x, 1f, duracion)
            .SetEase(Ease.Linear);
    }

    private IEnumerator CerrarConRetraso(bool correcta)
    {
        yield return new WaitForSeconds(2f);
        OnRespuestaFinalizada(correcta);
    }

    private void OnRespuestaFinalizada(bool correcta)
    {
        upDownPanelsSeq.Append(upRespuestasPanel.DOAnchorPosY(upRespuestasPanel.anchoredPosition.y + 483, 0.3f).SetEase(Ease.OutQuad));
        upDownPanelsSeq.Join(downRespuestasPanel.DOAnchorPosY(downRespuestasPanel.anchoredPosition.y - 483, 0.3f).SetEase(Ease.OutQuad));

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

