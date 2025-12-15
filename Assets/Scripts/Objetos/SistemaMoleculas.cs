using TMPro;
using UnityEngine;

public class SistemaMoleculas : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI textoCantidadMoleculas;

    [SerializeField] private int cantidadMoleculas;

    private void Start()
    {
        ActualizarTexto();
    }

    private void OnEnable()
    {
        Moleculas.MoleculaRecolectada += SumarMoleculas;
    }
    private void OnDisable()
    {
        Moleculas.MoleculaRecolectada -= SumarMoleculas;
    }

    private void SumarMoleculas()
    {
        cantidadMoleculas += 1;
        ActualizarTexto();
    }

    private void ActualizarTexto()
    {
        textoCantidadMoleculas.text = cantidadMoleculas.ToString();
    }
}
