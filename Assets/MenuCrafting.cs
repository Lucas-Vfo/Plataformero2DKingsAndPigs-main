using UnityEngine;

public class MenuCrafting : MonoBehaviour
{
    [SerializeField] private GameObject botonCrafting;

    [SerializeField] private GameObject menuCrafting;

    private bool juegoPausado = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            if (juegoPausado)
            {
                Reanudar();
            }
            else
            {
                Pausa();
            }
        }
    }
    public void Pausa()
    {
        juegoPausado = true;
        botonCrafting.SetActive(false);
        menuCrafting.SetActive(true);
    }

    public void Reanudar()
    {
        juegoPausado = false;
        botonCrafting.SetActive(true);
        menuCrafting.SetActive(false);
    }
}
