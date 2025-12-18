using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class BondHitbox : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image img;
    public AtomBond Bond { get; private set; }

    private CraftingBondBoardSingle board;

    private void Awake()
    {
        if (img == null) img = GetComponent<Image>();
        board = GetComponentInParent<CraftingBondBoardSingle>();
        SetAlpha(0f);
    }

    public void Setup(AtomBond bond)
    {
        Bond = bond;
        RefreshTransform();
    }

    public void RefreshTransform()
    {
        if (Bond?.a == null || Bond?.b == null) return;

        var a = (RectTransform)Bond.a.transform;
        var b = (RectTransform)Bond.b.transform;

        Vector3 p1 = a.position;
        Vector3 p2 = b.position;

        var rt = (RectTransform)transform;
        rt.position = (p1 + p2) * 0.5f;

        Vector3 dir = (p2 - p1);
        float dist = dir.magnitude;

        var aPointRt = (RectTransform)Bond.aPoint.transform;
        float thickness = aPointRt.rect.width;     // igual al bondpoint

        rt.sizeDelta = new Vector2(dist, thickness);
        float ang = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rt.rotation = Quaternion.Euler(0, 0, ang);
    }

    public void OnPointerEnter(PointerEventData eventData) => SetAlpha(0.15f);
    public void OnPointerExit(PointerEventData eventData) => SetAlpha(0f);

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.clickCount == 2 && Bond != null)
            board?.BreakBond(Bond);
    }

    private void SetAlpha(float a)
    {
        var c = img.color;
        c.a = a;
        img.color = c;
    }
}
