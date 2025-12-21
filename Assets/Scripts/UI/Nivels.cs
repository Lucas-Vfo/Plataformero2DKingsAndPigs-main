using UnityEngine;
using UnityEngine.SceneManagement;

public class Nivels : MonoBehaviour
{
    [SerializeField] private int numeroEscena;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            SceneManager.LoadScene(numeroEscena);
        }
    }
}
