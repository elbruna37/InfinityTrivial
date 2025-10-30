using System;
using UnityEngine;

/// <summary>
/// Handles dice physics, value detection, and bounce interactions.
/// Invokes a callback when the dice has stopped rolling.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class Dice : MonoBehaviour
{
    [Header("Bounce Settings")]
    [Tooltip("Impulse applied when the dice hits a wall.")]
    public float bounceForce = 2f;

    [Header("Destruction Settings")]
    [Tooltip("Time before automatically destroying the dice.")]
    public float destroyDelay = 10f;

    private Rigidbody rb;
    private bool hasStopped = false;

    /// <summary>
    /// Current value displayed by the dice.
    /// </summary>
    public int currentValue { get; private set; } = 0;

    /// <summary>
    /// Event triggered when the dice stops rolling.
    /// </summary>
    public Action<int> onDiceStopped;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        DetectStopped();

        HandleDestruction();
    }

    #region Detection

    /// <summary>
    /// Checks if the dice has stopped moving and detects its value.
    /// </summary>
    private void DetectStopped()
    {
        if (!hasStopped && rb.IsSleeping())
        {
            hasStopped = true;
            DetectValue();
        }
    }

    /// <summary>
    /// Detects the value of the dice based on the top-facing collider.
    /// Assumes the collider's name is the number of the face.
    /// </summary>
    private void DetectValue()
    {
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, 2f))
        {
            if (int.TryParse(hit.collider.gameObject.name, out int value))
            {
                currentValue = value;
                Debug.Log($"Dice shows: {currentValue}");

                onDiceStopped?.Invoke(currentValue);
            }
            else
            {
                Debug.LogWarning("Dice face collider name is not a number: " + hit.collider.gameObject.name);
            }
        }
        else
        {
            Debug.LogWarning("No collider detected above the dice to read its value.");
        }
    }

    #endregion

    #region Physics

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Wall"))
        {
            Vector3 bounceDir = collision.contacts[0].normal;
            rb.AddForce(bounceDir * bounceForce, ForceMode.Impulse);

            Debug.Log($"Dice bounced off {collision.gameObject.name}");
        }
    }

    #endregion

    #region Destruction

    /// <summary>
    /// Handles automatic destruction of the dice if flagged by TurnManager.
    /// </summary>
    private void HandleDestruction()
    {
        if (TurnManager.Instance != null && TurnManager.Instance.canDestroy)
        {
            Destroy(gameObject, destroyDelay);
            TurnManager.Instance.canDestroy = false;
        }
    }

    #endregion
}
