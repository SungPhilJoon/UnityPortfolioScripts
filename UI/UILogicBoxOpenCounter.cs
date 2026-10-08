// 원래는 namespace GT.UILogic 이런식으로 네임스페이스로 하는게 좋습니다.
// Logic 을 UI단에서 같이 바인딩하는게 좋은데 이미 구조가 많이 가있어서
// 별도로 바인딩을 합니다.
public class UILogicBoxOpenCounter : IUILogicOpenCounter
{
    private ItemInfoData boxItemInfo = null;
    private BoxInfoData boxInfo = null;
    private BoxListData boxListData = null;

    public UILogicBoxOpenCounter(ItemInfoData itemInfo, BoxInfoData boxInfoData)
    {
        boxItemInfo = itemInfo;
        boxInfo = boxInfoData;
        boxListData = TableManager.Instance.BoxListData.Find(boxInfo.ItemListID);
    }
    void IUILogic.DestroyPanel()
    {
    }

    int IUILogicOpenCounter.GetLimitCount()
    {
        return boxItemInfo.openMaxCount;
    }

    string IUILogicOpenCounter.GetContext()
    {
        return LocalizeManager.Instance.GetTXT(boxItemInfo.tooltipID);
    }

    string IUILogicOpenCounter.GetTitle()
    {
        return LocalizeManager.Instance.GetTXT(boxItemInfo.nameTextID);
    }
    ItemInfoData IUILogicOpenCounter.GetItemInfo()
    {
        return boxItemInfo;
    }
    long IUILogicOpenCounter.GetItemCount()
    {
        return getCurrentCount();
    }
    long IUILogicOpenCounter.GetShowItemCount()
    {
        return getCurrentCount();
    }
    bool IUILogicOpenCounter.IsMaxCountSetting()
    {
        return boxItemInfo.openDefaultMax;
    }

    bool IUILogicOpenCounter.IsVisibleItemList()
    {
        return boxItemInfo.visibleItemList;
    }

    IGameAsset[] IUILogicOpenCounter.GetVisibleItemList()
    {
        return boxListData.boxAssets;
    }

    bool IUILogicOpenCounter.Confirm(int count)
    {
        var currentCount = getCurrentCount();
        if (count > currentCount)
        {
            return false;
        }

        GameBox.StartMerge();
        boxListData.boxAssets.MergeGameAssets(boxInfo.BoxType, count);
        GameAsset[] gameAssets = GameBox.EndMerge();

        UserFacade.Asset.AddItem(gameAssets);

        UIRewardFacade.ShowResultPage(gameAssets, "STR_UI_ITEM_BOXOPEN_REWARD");

        UserFacade.Asset.Decrease(boxItemInfo.itemType, boxItemInfo.Index, count);

        UserGameData.Get().CheckCondition(E_ConditionType.Box_Open, count: count);
        UserGameData.Get().CheckCondition(E_ConditionType.Box_Open_CheckID, checkValue: boxItemInfo.Index, count: count);
        UserGameData.Get().CheckCondition(E_ConditionType.Box_Open_All, count: count);

        UserGameData.Get().SendEvent(E_UserMessage.Bag_UseItem, boxItemInfo.Index);

        if (currentCount - count > 0)
        {
            return true;
        }

        return false;
    }

    private long getCurrentCount()
    {
        var boxItemClass = UserGameData.Get().GetBagItem(boxItemInfo.Index);

        return boxItemClass?.Count ?? 0;
    }

    bool IUILogicOpenCounter.IsDisableCountSlider()
    {
        return boxItemInfo.openMaxCount == 1;
    }
}
