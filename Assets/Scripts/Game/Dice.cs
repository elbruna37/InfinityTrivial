using System;
using UnityEngine;

public class Dice : MonoBehaviour
{
    [Header("Fuerza de rebote")]
    public float bounceForce = 2f;

    private Rigidbody rb;
    private bool hasStopped = false;
    public int currentValue = 0;

    [Header("Tiempo antes de destruir el dado")]
    public float destroyDelay = 10f;

    public Action<int> onDiceStopped;
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        // Detectar cuando el dado se ha detenido
        if (!hasStopped && rb.IsSleeping())
        {
            hasStopped = true;
            DetectValue();
        }

        // Destruir el dado
        if (TurnManager.Instance.canDestroy) { Destroy(gameObject); TurnManager.Instance.canDestroy = false; }
    }

    void DetectValue()
    {
        // Raycast hacia arriba
        if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit hit, 2f))
        {
            // El collider debería tener un nombre con el número de la cara
            int value;
            if (int.TryParse(hit.collider.gameObject.name, out value))
            {
                currentValue = value;
                Debug.Log("El dado muestra: " + currentValue);

                // Avisamos al callback
                onDiceStopped?.Invoke(currentValue);
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        // Verificar si la colisión es con una pared
        if (collision.gameObject.CompareTag("Wall"))
        {
            // Dirección contraria al impacto
            Vector3 bounceDir = collision.contacts[0].normal;

            // Aplicar un pequeño impulso
            rb.AddForce(bounceDir * bounceForce, ForceMode.Impulse);

            Debug.Log($"Dado rebotó contra {collision.gameObject.name}");
        }
    }
}
