using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Pregunta
{
    public string categoria;
    public string enunciado;
    public string[] opciones;
    public int indiceCorrecta;
    public string dificultad;
}

[System.Serializable]
public class PreguntaLote
{
    public Pregunta[] questions;
}

public enum QuesitoColor
{
    Azul,
    Rosa,
    Amarillo,
    Verde,
    Naranja,
    Morado
}
