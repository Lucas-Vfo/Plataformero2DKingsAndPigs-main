using UnityEngine;

public class MenuCrafting : MonoBehaviour
{
    [SerializeField] private UIInventarioCrafting uiInventario;
    [SerializeField] private GameObject menuCrafting;

    private bool menuAbierto;

    private void Awake()
    {
        // Asegura que parte cerrado
        menuCrafting.SetActive(false);
        menuAbierto = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            if (menuAbierto) Cerrar();
            else Abrir();
        }
    }

    private void Abrir()
    {
        menuAbierto = true;
        menuCrafting.SetActive(true);

        // Refresca textos apenas se abre (muestra lo recolectado “en background”)
        if (uiInventario != null)
            uiInventario.RefreshAll();
    }

    private void Cerrar()
    {
        menuAbierto = false;
        menuCrafting.SetActive(false);
    }
}