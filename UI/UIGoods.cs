using UnityEngine;
using UnityEngine.UI;

public class UIGoods : UIItemBase
{
    [SerializeField] private Image image = null;
    [SerializeField] private TMPro.TMP_Text count = null;
    public void SetAsset(GameAsset asset)
    {
        var sprite = UIIconLoader.LoadItemSprite(asset.type);

        image.sprite = sprite;

        count.text = asset.count.ToString();
    }
}
