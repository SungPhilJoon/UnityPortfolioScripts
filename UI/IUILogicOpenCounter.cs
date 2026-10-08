using System.Collections.Generic;
using UnityEngine;

public interface IUILogicOpenCounter : IUILogic
{
    int GetLimitCount();
    string GetTitle();
    string GetContext();
    bool Confirm(int count);
    ItemInfoData GetItemInfo();
    long GetItemCount();
    long GetShowItemCount();
    bool IsMaxCountSetting();
    bool IsVisibleItemList();
    IGameAsset[] GetVisibleItemList();
    bool IsDisableCountSlider();
}
