using UnityEngine;

public class Moleculas : MonoBehaviour
{
    [SerializeField] private Elemento elemento = Elemento.H;
    [SerializeField] private int valorCantidad = 1;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Debug.Log($"Moleculas trigger con: {collision.name} tag={collision.tag}");
        if (!collision.CompareTag("Player")) return;

        // Busca InventarioAtoms en el jugador (o en un GameManager)
        var inv = collision.GetComponentInParent<InventarioAtoms>();
        Debug.Log($"Inventario en Player: {(inv != null)}");
        if (inv != null)
        {
            inv.Add(elemento, valorCantidad);
            Debug.Log($"SUMADO: {elemento} +{valorCantidad} => ahora H={inv.Get(Elemento.H)}");
        }

        Destroy(gameObject);
    }
}