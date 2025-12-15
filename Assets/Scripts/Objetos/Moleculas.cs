using System;
using UnityEngine;

public class Moleculas : MonoBehaviour
{
    public static Action MoleculaRecolectada;

    [SerializeField] private int valorCantidad;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            Recolectar();
        }
    }

    private void Recolectar()
    {
        MoleculaRecolectada?.Invoke();
        Destroy(gameObject);
    }
}
