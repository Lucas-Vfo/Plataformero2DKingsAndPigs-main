using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabilidadUI : MonoBehaviour
{
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text labelCantidad;
    [SerializeField] private GameObject highlight;

    private string skillId;

    public string SkillId => skillId;

    public void SetEmpty()
    {
        skillId = null;
        if (icon != null) icon.enabled = false;
        if (labelCantidad != null) labelCantidad.text = "";
        if (highlight != null) highlight.SetActive(false);
    }

    public void SetSkill(string id, Sprite sprite, int cantidad)
    {
        skillId = id;

        if (icon != null)
        {
            icon.enabled = true;
            icon.sprite = sprite;
            icon.preserveAspect = true;
        }

        if (labelCantidad != null)
            labelCantidad.text = (cantidad < 0) ? "" : Mathf.Max(0, cantidad).ToString();
    }

    public void SetSelected(bool selected)
    {
        if (highlight != null) highlight.SetActive(selected);
    }

    public void SetCantidad(int cantidad)
    {
        if (labelCantidad != null)
            labelCantidad.text = Mathf.Max(0, cantidad).ToString();
    }
}
