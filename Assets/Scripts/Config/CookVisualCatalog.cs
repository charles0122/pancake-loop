using UnityEngine;

[CreateAssetMenu(menuName = "ByteLoop/Cook Visual Catalog", fileName = "CookVisualCatalog")]
public class CookVisualCatalog : ScriptableObject
{
    [Header("锅内制作")]
    public Sprite crepe;
    public Sprite pancakeDone;
    public Sprite resultGood;
    public Sprite resultBad;
    public Sprite ladle;
    public Sprite brushSauce;
    public Sprite brushPepper;
    public Sprite eggSplat;

    [Header("订单图标")]
    public Sprite iconEgg;
    public Sprite iconPepper;
    public Sprite iconSauce;
    public Sprite iconSausage;
    public Sprite iconSesame;
    public Sprite iconCoriander;
    public Sprite iconOnion;
    public Sprite iconLettuce;
    public Sprite iconFlour;
    public Sprite iconMeat;
    public Sprite iconOil;
    public Sprite iconCrisp;

    public Sprite GetIcon(MaterialType type)
    {
        switch (type)
        {
            case MaterialType.Egg: return iconEgg;
            case MaterialType.Pepper: return iconPepper;
            case MaterialType.Sauce: return iconSauce;
            case MaterialType.Sausage: return iconSausage;
            case MaterialType.Sesame: return iconSesame;
            case MaterialType.Coriander: return iconCoriander;
            case MaterialType.Onion: return iconOnion;
            case MaterialType.RemaineLettuce: return iconLettuce;
            case MaterialType.Flour: return iconFlour;
            case MaterialType.Meat: return iconMeat;
            case MaterialType.Oil: return iconOil;
            case MaterialType.Crisp: return iconCrisp;
            default: return null;
        }
    }
}
