using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

[System.Serializable]
public struct DropdownColorMap
{
    public QuesitoColor color;
    public TMP_Dropdown dropdown;
    public GameObject quesitoModel;
}

public class UISelectorCategorias : MonoBehaviour
{
    public PreguntasManager manager;
    public DropdownColorMap[] dropdownColorMaps;
    public Button botonConfirmar;
    public GameObject canvas;

    private List<string> originalCategories;
    private Dictionary<TMP_Dropdown, string> selectionMap;

    private int currentIndex = 0;

    Tween spin;

    private static readonly Vector3[] basePositions = new Vector3[]
{
    new Vector3(-3.2f, 0f, 8.53f),  // Rosa
    new Vector3(-2.9f, 0f, 8.0f),   // Azul
    new Vector3(-3.2f, 0f, 7.5f),   // Verde
    new Vector3(-3.78f, 0f, 7.5f),  // Amarillo
    new Vector3(-4.1f, 0f, 8.0f),   // Morado
    new Vector3(-3.78f, 0f, 8.53f)  // Naranja
};

    public GameObject camara;

    void Start()
    {
        canvas.SetActive(true);

        currentIndex = 0;

        originalCategories = new List<string>(manager.categoriasDisponibles);
        selectionMap = new Dictionary<TMP_Dropdown, string>();

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
        if (nueva == null) return;

        selectionMap[dd] = nueva;

        dd.interactable = false;
        dd.gameObject.SetActive(false);

        currentIndex++;
        if (currentIndex < dropdownColorMaps.Length)
        {
            TMP_Dropdown siguiente = dropdownColorMaps[currentIndex].dropdown;
            siguiente.gameObject.SetActive(true);

            var opciones = BuildOptionsFor(siguiente);
            siguiente.ClearOptions();
            siguiente.AddOptions(opciones);
            siguiente.SetValueWithoutNotify(0);

            TMP_Dropdown local = siguiente;
            siguiente.onValueChanged.AddListener((int v) => OnDropdownValueChanged(local, v));

            AnimateQuesito(dropdownColorMaps[currentIndex].quesitoModel, dropdownColorMaps[currentIndex].dropdown, currentIndex);
        }

        UpdateConfirmButton();
    }

    private List<string> BuildOptionsFor(TMP_Dropdown dd)
    {
        HashSet<string> bloqueadas = new HashSet<string>(
            selectionMap.Where(kv => kv.Key != dd && !string.IsNullOrEmpty(kv.Value)).Select(kv => kv.Value)
        );

        List<string> disponibles = originalCategories.Where(c => !bloqueadas.Contains(c)).ToList();

        List<string> opciones = new List<string> { "Selecciona una categoría" };
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

        Destroy(GameManager.Instance.gameObject);
        Destroy(PreguntasManager.Instance.gameObject);

        SceneManager.LoadScene("Menu");
    }

    // 🔹 Animación con DOTween
    private void AnimateQuesito(GameObject quesito, TMP_Dropdown dropdown, int index)
    {
        if (quesito == null) return;


        Vector3 startPos = quesito.transform.position;
        Vector3 loopPos = startPos + Vector3.up * 1.5f;

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
}
