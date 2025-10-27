using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

[System.Serializable]
public struct DropdownColorMap
{
    public QuesitoColor color;
    public TMP_Dropdown dropdown;
    public GameObject quesitoModel;
}

public class UISelectorCategorias : MonoBehaviour
{
    [Header("Referencias")]
    public PreguntasManager manager;

    [Header("Dropdowns")]
    public DropdownColorMap[] dropdownColorMaps;

    [Header("UI")]
    public GameObject canvas;
    public Button botonConfirmar;
    public GameObject confirmButtons;
    public GameObject selectorQuesitosMenu;
    public GameObject backButton;
    [SerializeField] private LocalizedString defaultCategoryText;

    [Header("Camara")]
    public GameObject camara;

    private List<string> originalCategories;
    private Dictionary<TMP_Dropdown, string> selectionMap;

    private int currentIndex = 0;

    Tween spin;

    private Vector3[] originalQuesitoPositions;
    private Quaternion[] originalQuesitoRotations;

    

    private static readonly Vector3[] basePositions = new Vector3[]
{
    new Vector3(-3.2f, 0f, 8.53f),  // Rosa
    new Vector3(-2.9f, 0f, 8.0f),   // Azul
    new Vector3(-3.2f, 0f, 7.5f),   // Verde
    new Vector3(-3.786f, 0f, 7.5f),  // Amarillo
    new Vector3(-4.1f, 0f, 8.0f),   // Morado
    new Vector3(-3.784f, 0f, 8.528f)  // Naranja
};

    

    void Start()
    {
        canvas.SetActive(true);

        currentIndex = 0;

        originalCategories = new List<string>(manager.categoriasDisponibles);
        selectionMap = new Dictionary<TMP_Dropdown, string>();

        originalQuesitoPositions = new Vector3[dropdownColorMaps.Length];
        originalQuesitoRotations = new Quaternion[dropdownColorMaps.Length];
        for (int i = 0; i < dropdownColorMaps.Length; i++)
        {
            var q = dropdownColorMaps[i].quesitoModel;
            if (q != null)
            {
                // Guardamos la posición/rotación tal y como están al inicio de la escena
                originalQuesitoPositions[i] = q.transform.position;
                originalQuesitoRotations[i] = q.transform.rotation;
            }
        }

        for (int i = 0; i < dropdownColorMaps.Length; i++)
        {
            TMP_Dropdown dd = dropdownColorMaps[i].dropdown;
            dd.gameObject.SetActive(i == 0);
            selectionMap[dd] = null;

            if (i == 0)
            {
                var opciones = BuildOptionsFor(dd);
                dd.ClearOptions();
                dd.AddOptions(opciones);
                dd.SetValueWithoutNotify(0);

                TMP_Dropdown local = dd;
                dd.onValueChanged.AddListener((int value) => OnDropdownValueChanged(local, value));
            }
        }

        AnimateQuesito(dropdownColorMaps[0].quesitoModel, dropdownColorMaps[0].dropdown, 0);

        UpdateConfirmButton();
    }

    private void OnDropdownValueChanged(TMP_Dropdown dd, int value)
    {
        string nueva = value == 0 ? null : dd.options[value].text;
        if (nueva == null)
        {
            return;
        }

        backButton.SetActive(false);

        selectionMap[dd] = nueva;

        // Desactivar dropdown seleccionado
        dd.gameObject.SetActive(false);
        if (spin != null) spin.Kill();

        // Mover el quesito seleccionado a la base
        GameObject quesito = dropdownColorMaps[currentIndex].quesitoModel;
        Vector3 basePos = basePositions[currentIndex];
        basePos.y = quesito.transform.position.y;
        Quaternion baseRot = Quaternion.Euler(270, 0, 120 + (currentIndex * 60));

        quesito.transform.DOMove(basePos, 1.5f).SetEase(Ease.InOutQuad);
        quesito.transform.DORotateQuaternion(baseRot, 1.5f).SetEase(Ease.InOutQuad)
            .OnComplete(() =>
            {
                Vector3 finalPos = new Vector3(basePos.x, 0.18f, basePos.z);
                quesito.transform.DOMove(finalPos, 1f).SetEase(Ease.InOutQuad);

                // Incrementar currentIndex y activar siguiente quesito
                currentIndex++;

                if (currentIndex < dropdownColorMaps.Length)
                {
                    var siguienteMap = dropdownColorMaps[currentIndex];
                    TMP_Dropdown siguiente = siguienteMap.dropdown;
                    siguiente.gameObject.SetActive(true);

                    var opciones = BuildOptionsFor(siguiente);
                    siguiente.ClearOptions();
                    siguiente.AddOptions(opciones);
                    siguiente.SetValueWithoutNotify(0);

                    TMP_Dropdown local = siguiente;
                    siguiente.onValueChanged.RemoveAllListeners();
                    siguiente.onValueChanged.AddListener((int v) => OnDropdownValueChanged(local, v));

                    AnimateQuesito(siguienteMap.quesitoModel, siguiente, currentIndex);
                }

                UpdateConfirmButton();
            });
    }

    private List<string> BuildOptionsFor(TMP_Dropdown dd)
    {
        HashSet<string> bloqueadas = new HashSet<string>(
            selectionMap.Where(kv => kv.Key != dd && !string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Value)
        );

        List<string> disponibles = originalCategories.Where(c => !bloqueadas.Contains(c)).ToList();

        string textoDefault = defaultCategoryText.GetLocalizedString();
        List<string> opciones = new List<string> { textoDefault };

        opciones.AddRange(disponibles);
        return opciones;
    }

    private void UpdateConfirmButton()
    {
        bool allSelected = selectionMap.Values.All(v => !string.IsNullOrEmpty(v));
        botonConfirmar.interactable = allSelected;

        TMP_Text buttonText = botonConfirmar.GetComponentInChildren<TMP_Text>();
        if (buttonText != null)
        {
            buttonText.color = allSelected ? Color.white : Color.gray;
        }
    }

    public void ConfirmarCategorias()
    {
        GameManager.Instance.AudioClick();

        canvas.SetActive(false);

        foreach (var map in dropdownColorMaps)
        {
            string categoria = selectionMap[map.dropdown];
            if (!string.IsNullOrEmpty(categoria))
            {
                GameManager.Instance.SetCategoriaParaColor(map.color, categoria);
            }
        }
        Sequence camMotion = DOTween.Sequence();

        camMotion.Append(camara.transform.DOMove(new Vector3(0, 8.7f, 0), 1f).SetEase(Ease.InOutQuad));
        camMotion.Join(camara.transform.DORotate(new Vector3(90, 360, 0), 1f, RotateMode.FastBeyond360).SetEase(Ease.InOutQuad));


        camMotion.OnComplete(() =>
        {
            SceneManager.LoadScene("Game");
        });
    }

    public void VolverAlMenu()
    {
        GameManager.Instance.AudioClick();

        confirmButtons.SetActive(true);

        selectorQuesitosMenu.SetActive(false);
    }

    public void cancelBackMenu()
    {
        GameManager.Instance.AudioClick();

        confirmButtons.SetActive(false);

        selectorQuesitosMenu.SetActive(true);
    }

    public void confirmBackMenu()
    {
        GameManager.Instance.AudioClick();
        confirmButtons.SetActive(false);

        Sequence returnSeq = DOTween.Sequence();

        for (int i = 0; i < dropdownColorMaps.Length; i++)
        {
            var map = dropdownColorMaps[i];
            GameObject quesito = map.quesitoModel;
            if (quesito == null) continue;

            // Matar tweens activos sobre el transform (spin, etc.)
            DOTween.Kill(quesito.transform, complete: false);

            Tween t = ReturnQuesitoToOriginalPosition(quesito, i);
            if (t != null)
            {
                returnSeq.Join(t); // <-- correcto: Join sobre la Sequence
            }
        }

        returnSeq.OnComplete(() =>
        {
            if (PreguntasManager.Instance != null)
                Destroy(PreguntasManager.Instance.gameObject);

            GameManager.Instance.MoveCamToPoint(
                camara,
                new Vector3(0, 8, -10.7f),
                Quaternion.Euler(48.968f, 0f, 0f),
                "Menu"
            );
        });
    }

    public void RetrocederSeleccion()
    {
        GameManager.Instance.AudioClick();

        if (currentIndex <= 0)
        {
            return;
        }

        // Bloquear el botón de back
        backButton.SetActive(false);

        // Devolver quesito actual a posición original si existe
        if (currentIndex < dropdownColorMaps.Length)
        {
            var actualMap = dropdownColorMaps[currentIndex];
            GameObject actualQuesito = actualMap.quesitoModel;
            if (actualQuesito != null)
            {
                DOTween.Kill(actualQuesito.transform, complete: false);
                actualQuesito.transform.DOMove(originalQuesitoPositions[currentIndex], 0.6f).SetEase(Ease.InOutQuad);
                actualQuesito.transform.DORotateQuaternion(originalQuesitoRotations[currentIndex], 0.6f).SetEase(Ease.InOutQuad);
            }
            actualMap.dropdown.gameObject.SetActive(false);
        }

        currentIndex--;

        var prevMap = dropdownColorMaps[currentIndex];
        var prevDropdown = prevMap.dropdown;
        var prevQuesito = prevMap.quesitoModel;

        // Restaurar selección anterior
        if (selectionMap.ContainsKey(prevDropdown))
            selectionMap[prevDropdown] = null;

        // Reconstruir opciones
        var opciones = BuildOptionsFor(prevDropdown);
        prevDropdown.ClearOptions();
        prevDropdown.AddOptions(opciones);
        prevDropdown.SetValueWithoutNotify(0);

        //prevDropdown.interactable = false;
        prevDropdown.gameObject.SetActive(false);

        // Solo animación visual, no afecta currentIndex
        if (prevQuesito != null)
        {
            // Desactivar momentáneamente el dropdown
            prevDropdown.gameObject.SetActive(false);

            // Matamos tweens activos
            DOTween.Kill(prevQuesito.transform, complete: false);

            // Definimos directamente la posición de "rotación" (la misma que usa AnimateQuesito)
            Vector3 loopPos = originalQuesitoPositions[currentIndex] + Vector3.up * 1.5f;

            Sequence seq = DOTween.Sequence();

            // Movimiento suave hacia la posición de spin
            seq.Append(prevQuesito.transform.DOMoveY(prevQuesito.transform.position.y + 1f, 0.4f).SetEase(Ease.OutQuad));

            seq.Append(prevQuesito.transform.DOMove(loopPos, 1f).SetEase(Ease.InOutQuad));
            seq.Join(prevQuesito.transform.DORotateQuaternion(Quaternion.Euler(0, 0, 90), 1f).SetEase(Ease.InOutQuad));

            seq.OnComplete(() =>
            {
                // Empieza a girar sin pasar por posición original
                spin = prevQuesito.transform.DORotate(
                    new Vector3(0, 360, 0),
                    4f,
                    RotateMode.WorldAxisAdd
                )
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Restart);

                prevDropdown.gameObject.SetActive(true);
                if (currentIndex > 0) backButton.SetActive(true);
            });
        }

        UpdateConfirmButton();
    }

    // 🔹 Animación con DOTween
    private void AnimateQuesito(GameObject quesito, TMP_Dropdown dropdown, int index)
    {
        if (quesito == null) return;

        Vector3 startPos = quesito.transform.position;
        Vector3 loopPos = startPos + Vector3.up * 1.5f;

        // 🔹 Desactivar dropdown mientras anima
        dropdown.gameObject.SetActive(false);

        // Secuencia de animación
        Sequence seq = DOTween.Sequence();

        seq.AppendInterval(0.5f);

        // 1. Subida + rotación inicial
        seq.Append(quesito.transform.DOMove(loopPos, 1f).SetEase(Ease.OutQuad));
        seq.Join(quesito.transform.DORotate(new Vector3(0, 0, 90), 1f));


        // 2. Empieza a girar mientras se espera selección
        seq.OnComplete(() =>
        {
            // Aseguramos que parte desde la orientación deseada
            quesito.transform.DORotateQuaternion(Quaternion.Euler(0, 0, 90), 0f);

            // Inicia rotación infinita alrededor de Y
            spin = quesito.transform.DORotate(
                new Vector3(0, 360, 0), // rotación completa
                4f,                     // duración
                RotateMode.WorldAxisAdd // rotación en espacio global
            )
            .SetEase(Ease.Linear)
            .SetLoops(-1, LoopType.Restart);

            // Habilitamos el dropdown cuando el spin comienza
            dropdown.gameObject.SetActive(true);

            if (currentIndex > 0) { backButton.SetActive(true); }    
        });

        bool waiting = true;
        dropdown.onValueChanged.AddListener((val) =>
        {
            if (val != 0 && waiting)
            {
                waiting = false;
                spin.Kill();

                // 3. Mover a base circular
                Vector3 basePos = basePositions[index];
                basePos.y = quesito.transform.position.y;

                // --- ANIMACIÓN ---
                Quaternion baseRot = Quaternion.Euler(270, 0, 120 + (index * 60));

                quesito.transform.DOMove(basePos, 1.5f).SetEase(Ease.InOutQuad);
                quesito.transform.DORotateQuaternion(baseRot, 1.5f).SetEase(Ease.InOutQuad)
                    .OnComplete(() =>
                    {
                        // Ajustar altura final
                        Vector3 finalPos = new Vector3(basePos.x, 0.18f, basePos.z);
                        quesito.transform.DOMove(finalPos, 1f).SetEase(Ease.InOutQuad);
                    });
            }
        });
    }

    // Devuelve un quesito a su posición y rotación original con una parábola suave.
    private Tween ReturnQuesitoToOriginalPosition(GameObject quesito, int index)
    {
        if (quesito == null) return null;

        if (originalQuesitoPositions == null || originalQuesitoPositions.Length <= index)
            return null;

        Vector3 currentPos = quesito.transform.position;
        Quaternion currentRot = quesito.transform.rotation;

        Vector3 basePos = basePositions[index];
        Vector3 targetPos = originalQuesitoPositions[index];
        Quaternion targetRot = originalQuesitoRotations[index];

        float moveDuration = 1.2f;
        Ease moveEase = Ease.InOutQuad;

        // Si NO está en la base → movimiento directo
        if (Vector3.Distance(currentPos, basePos) > 0.3f)
        {
            Sequence quickSeq = DOTween.Sequence();
            quickSeq.Join(quesito.transform.DOMove(targetPos, moveDuration).SetEase(moveEase));
            quickSeq.Join(quesito.transform.DORotateQuaternion(targetRot, moveDuration).SetEase(moveEase));
            return quickSeq;
        }

        // Movimiento parabólico
        Sequence seq = DOTween.Sequence();

        // Subida inicial más viva
        Vector3 liftPos = currentPos + Vector3.up * 1.5f;
        seq.Append(quesito.transform.DOMove(liftPos, 0.35f).SetEase(Ease.OutQuad));

        // Parabola más natural: tres puntos con arco más alto
        float arcHeight = Mathf.Max(2.0f, Vector3.Distance(currentPos, targetPos) * 0.1f);
        Vector3 midPoint = Vector3.Lerp(liftPos, targetPos, 0.5f);
        midPoint.y += arcHeight; // subida adicional

        float jumpDuration = 1.0f;

        // Movimiento en curva Catmull-Rom
        seq.Append(quesito.transform.DOPath(
            new Vector3[] { liftPos, midPoint, targetPos },
            jumpDuration,
            PathType.CatmullRom
        )
        .SetEase(Ease.InOutSine));

        // Rotación sincronizada con el salto
        seq.Join(quesito.transform.DORotateQuaternion(targetRot, jumpDuration + 0.3f).SetEase(Ease.InOutSine));

        return seq;
    }


}
