// 원래는 namespace GT.UILogic 이런식으로 네임스페이스로 하는게 좋습니다.
// Logic 을 UI단에서 같이 바인딩하는게 좋은데 이미 구조가 많이 가있어서
// 별도로 바인딩을 합니다.
using UnityEngine;

public class UILogicGoodsOpenCounter : IUILogicOpenCounter
{
    private GoodsData goodsData = null;
    private GoodsUIData resourceData = null;
    private int limitCount = 0;
    private System.Action<int> buyCallback = null;
    public UILogicGoodsOpenCounter(GoodsData goodsData, GoodsUIData resourceData, int limitCount, System.Action<int> buyCallback)
    {
        this.goodsData = goodsData;
        this.limitCount = limitCount;
        this.buyCallback = buyCallback;
        this.resourceData = resourceData;
    }
    void IUILogic.DestroyPanel()
    {
    }

    int IUILogicOpenCounter.GetLimitCount()
    {
        return limitCount;
    }

    string IUILogicOpenCounter.GetContext()
    {
        return LocalizeManager.Instance.GetTXT(resourceData.productText);
    }

    string IUILogicOpenCounter.GetTitle()
    {
        return LocalizeManager.Instance.GetTXT(resourceData.titleText);
    }
    ItemInfoData IUILogicOpenCounter.GetItemInfo()
    {
        var firstAsset = goodsData.ItemList[0];

        return TableManager.Instance.ItemInfoData.Find(firstAsset.index);
    }

    long IUILogicOpenCounter.GetItemCount()
    {
        return limitCount;
    }
    long IUILogicOpenCounter.GetShowItemCount()
    {
        return goodsData.ItemList[0].count;
    }
    bool IUILogicOpenCounter.IsMaxCountSetting()
    {
        return false;
    }

    bool IUILogicOpenCounter.IsVisibleItemList()
    {
        return false;
    }

    IGameAsset[] IUILogicOpenCounter.GetVisibleItemList()
    {
        return null;
    }

    bool IUILogicOpenCounter.Confirm(int count)
    {
        buyCallback?.Invoke(count);

        return false;
    }

    bool IUILogicOpenCounter.IsDisableCountSlider()
    {
        return false;
    }
}
