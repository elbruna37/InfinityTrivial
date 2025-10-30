using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Handles spawning and throwing dice with random position, force, and rotation.
/// Also manages a timeout in case the dice gets stuck.
/// </summary>
public class DiceSpawner : MonoBehaviour
{
    [Header("Dice Prefab")]
    [Tooltip("Prefab of the dice to spawn.")]
    public GameObject dicePrefab;

    [Header("Spawn Area")]
    [Tooltip("Center point of the spawn area.")]
    public Vector3 center = new Vector3(0.5f, 2f, -10);
    [Tooltip("Size of the spawn area (width x 0 x length).")]
    public Vector3 size = new Vector3(6.3f, 0, 6.7f);

    [Header("Throw Force")]
    public float minForce = 5f;
    public float maxForce = 10f;

    [Header("Torque (Rotation)")]
    public float minTorque = 5f;
    public float maxTorque = 15f;

    private float maxWaitTime = 8f;
    private Coroutine timeoutRoutine;

    #region Public Methods

    /// <summary>
    /// Spawns a dice at a random position and throws it with random force and torque.
    /// Invokes the callback when the dice stops rolling.
    /// </summary>
    /// <param name="onResult">Callback invoked with the dice value.</param>
    public void SpawnAndThrowDice(Action<int> onResult)
    {
        Vector3 spawnPos = GetRandomPosition();
        GameObject dice = Instantiate(dicePrefab, spawnPos, UnityEngine.Random.rotation);

        GameManager.Instance.PlayDiceSound();

        Rigidbody rb = dice.GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Dice prefab must have a Rigidbody.");
            Destroy(dice);
            return;
        }

        SetupDiceCallback(dice, onResult);
        ApplyRandomForceAndTorque(rb);

        // Start timeout
        if (timeoutRoutine != null) StopCoroutine(timeoutRoutine);
        timeoutRoutine = StartCoroutine(TimeoutDice(dice, onResult));
    }

    #endregion

    #region Private Methods

    /// <summary>
    /// Returns a random position within the defined spawn area.
    /// </summary>
    private Vector3 GetRandomPosition()
    {
        return center + new Vector3(
            UnityEngine.Random.Range(-size.x / 2, size.x / 2),
            1.5f,
            UnityEngine.Random.Range(-size.z / 2, size.z / 2)
        );
    }

    /// <summary>
    /// Assigns the onDiceStopped callback to the Dice script.
    /// </summary>
    private void SetupDiceCallback(GameObject dice, Action<int> onResult)
    {
        Dice diceScript = dice.GetComponent<Dice>();
        if (diceScript != null)
        {
            diceScript.onDiceStopped = (value) =>
            {
                if (timeoutRoutine != null) StopCoroutine(timeoutRoutine);
                onResult?.Invoke(value);
            };
        }
    }

    /// <summary>
    /// Applies random force and torque to a Rigidbody for throwing the dice.
    /// </summary>
    private void ApplyRandomForceAndTorque(Rigidbody rb)
    {
        // Random direction in XZ plane
        Vector3 randomDirection = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            0f,
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized;

        float randomForce = UnityEngine.Random.Range(minForce, maxForce);
        rb.AddForce(randomDirection * randomForce, ForceMode.Impulse);

        // Random torque
        Vector3 randomTorque = new Vector3(
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f),
            UnityEngine.Random.Range(-1f, 1f)
        ).normalized * UnityEngine.Random.Range(minTorque, maxTorque);

        rb.AddTorque(randomTorque, ForceMode.Impulse);
    }

    /// <summary>
    /// Coroutine that destroys the dice after a timeout and respawns it.
    /// </summary>
    private IEnumerator TimeoutDice(GameObject dice, Action<int> onResult)
    {
        yield return new WaitForSeconds(maxWaitTime);

        if (dice != null) Destroy(dice);

        Debug.LogWarning("⚠️ Dice timeout reached. Retrying...");
        SpawnAndThrowDice(onResult);
    }

    #endregion
}
