using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuGameOver : MonoBehaviour
{
    [SerializeField] private GameObject menuGameOver;
    [SerializeField] private string escenaMenuPrincipal = "MenuPrincipal";
    [SerializeField] private bool pausarTiempo = true;

    private VidaJugador vidaJugador;
    private bool activo;

    private void Awake()
    {
        if (menuGameOver != null) menuGameOver.SetActive(false);

        // Cursor siempre visible y libre
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        Time.timeScale = 1f;
    }

    private void OnEnable()
    {
        var playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) return;

        vidaJugador = playerGO.GetComponent<VidaJugador>();
        if (vidaJugador == null) return;

        vidaJugador.MuerteJugador += ActivarMenu;
    }

    private void OnDisable()
    {
        if (vidaJugador != null)
            vidaJugador.MuerteJugador -= ActivarMenu;
    }

    private void ActivarMenu(object sender, EventArgs e)
    {
        if (activo) return;
        activo = true;

        if (menuGameOver != null) menuGameOver.SetActive(true);
        if (pausarTiempo) Time.timeScale = 0f;

        // No tocar el cursor: se mantiene visible y unlocked.
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Reiniciar()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void MenuPrincipal()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(escenaMenuPrincipal);
    }

    public void Salir()
    {
        Application.Quit();
    }
}
