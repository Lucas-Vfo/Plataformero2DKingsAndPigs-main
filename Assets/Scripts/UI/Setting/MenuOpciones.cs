using UnityEngine;
using UnityEngine.Audio;

public class MenuOpciones : MonoBehaviour
{
    [SerializeField] private AudioMixer audioMixer;

    public void CambioVolumen(float volumen)
    {
        audioMixer.SetFloat("Volumen", volumen);
    }
}
