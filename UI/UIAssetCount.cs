using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIAssetCount : UIItemBase
{
    [SerializeField] private TMPro.TMP_Text countText = null;
    [SerializeField] private Image assetImage = null;
    [SerializeField] private GameObject plusObj = null;

    private ItemInfoData assetItemData = null;

    private void Start()
    {
        UpdateCountText();
    }

    public void Set(ItemInfoData assetItemData)
    {
        this.assetItemData = assetItemData;
        var assetSprite = UIIconLoader.LoadItemSprite(assetItemData.icon);
        if (assetSprite == null)
        {
            Debugger.Error($"Resource Null. Asset Name : {assetItemData.icon}");
        }

        setAssetImage(this.assetItemData);
    }

    public void UpdateCountText()
    {
        var userGameData = UserGameData.Get();

        if (assetItemData == null)
        {
            Debugger.Error($"This Asset Count Prefab Not Set. {GetComponentsInParent<BasePopup>()[0].gameObject.name}");
        }

        switch (assetItemData.itemType)
        {
            case ITEM_TYPE.GOLD:
                {
                    setCountText(userGameData.GetGold());
                }
                break;

            case ITEM_TYPE.PEARL:
                {
                    setCountText(userGameData.GetPearl());
                }
                break;

            default:
                {
                    var itemClass = userGameData.GetBagItem(assetItemData.Index);
                    setCountText(itemClass?.Count ?? 0);
                }
                break;
        }
    }

    public void SetPlusButton(System.Action clickCallback)
    {
        Util.SetActiveObject(plusObj, true);

        var plusBtn = gameObject.AddComponent<Button>();
        plusBtn.onClick.AddListener(() =>
        {
            clickCallback?.Invoke();
        });
    }

    private void setCountText(long count)
    {
        countText.text = GameTextUtil.GetGoldText(count);
    }

    private void setAssetImage(ItemInfoData assetItemData)
    {
        var assetSprite = UIIconLoader.LoadItemSprite(assetItemData.icon);
        if (assetSprite == null)
        {
            Debugger.Error($"Resource Null. Asset Name : {assetItemData.icon}");
        }

        this.assetImage.sprite = assetSprite;
    }
}
