using System;
using System.Collections;
using UnityEngine;

public class DiceSpawner : MonoBehaviour
{
    [Header("Prefab del dado")]
    public GameObject dicePrefab;

    [Header("Margen de spawn")]
    public Vector3 center = new Vector3(0.5f, 2f, -10); // centro del área
    public Vector3 size = new Vector3(6.3f, 0, 6.7f); // ancho/largo del área donde puede aparecer

    [Header("Fuerza de lanzamiento")]
    public float minForce = 5f;
    public float maxForce = 10f;

    [Header("Fuerza de torque (rotación)")]
    public float minTorque = 5f;
    public float maxTorque = 15f;

    float maxWaitTime = 8;
    private Coroutine timeoutRoutine;

    public void SpawnAndThrowDice(Action<int> onResult)
    {
        // 1. Posición aleatoria dentro del área
        Vector3 randomPos = center + new Vector3(
            UnityEngine.Random.Range(-size.x / 2, size.x / 2),
            1.5f,
            UnityEngine.Random.Range(-size.z / 2, size.z / 2)
        );

        // 2. Instanciar el dado
        GameObject dice = Instantiate(dicePrefab, randomPos, UnityEngine.Random.rotation);

        Rigidbody rb = dice.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("El prefab del dado necesita un Rigidbody.");
            return;
        }

        Dice diceScript = dice.GetComponent<Dice>();
        if (diceScript != null)
        {
            diceScript.onDiceStopped = (value) =>
            {
                // Si ya llegó un resultado, cancelar timeout
                if (timeoutRoutine != null) StopCoroutine(timeoutRoutine);
                onResult?.Invoke(value);
            };
        }

        // 3. Dirección y magnitud de fuerza aleatoria
        Vector3 randomDirection = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            0f, // empuje hacia arriba un poco
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized;

        float randomForce = UnityEngine.Random.Range(minForce, maxForce);
        rb.AddForce(randomDirection * randomForce, ForceMode.Impulse);

        // 4. Torque (rotación aleatoria)
        Vector3 randomTorque = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized * UnityEngine.Random.Range(minTorque, maxTorque);

        rb.AddTorque(randomTorque, ForceMode.Impulse);

        // 5. Iniciar timeout
        if (timeoutRoutine != null) StopCoroutine(timeoutRoutine);
        timeoutRoutine = StartCoroutine(TimeoutDice(dice, onResult));
    }

    private IEnumerator TimeoutDice(GameObject dice, Action<int> onResult)
    {
        yield return new WaitForSeconds(maxWaitTime);

        if (dice != null) Destroy(dice);

        Debug.LogWarning("⚠️ Timeout del dado, volviendo a tirar...");
        SpawnAndThrowDice(onResult);
    }
}

