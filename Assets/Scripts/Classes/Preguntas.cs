using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public class Question
{
    public string categoria;
    public string enunciado;
    public string[] opciones;
    public int indiceCorrecta;
    public string dificultad;
}

[System.Serializable]
public class QuestionBatch
{
    public Question[] questions;
}

[Serializable]
public class QuestionArrayWrapper
{
    public Question[] preguntas;
}

public enum QuesitoColor
{
    Azul,
    Rosa,
    Naranja,
    Amarillo,
    Verde,
    Morado
}
