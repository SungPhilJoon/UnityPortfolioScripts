using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

public class PopupInventory : BasePopup, UserObserver.IListener
{
    class InvenCompare : IComparer<ItemClass>
    {
        public int Compare(ItemClass lH, ItemClass rH)
        {
            return lH.GetData().invenSort.CompareTo(rH.GetData().invenSort);
        }
    }

    [SerializeField] Transform itemContents;
    [SerializeField] Toggle BoxOpenTypeToggle;
    [SerializeField] Toggle ItemTypeToggle;

    private List<UIInventorySlot> slotList = new List<UIInventorySlot>();
    private UIItemPool<UIInventorySlot> slotPool = null;

    private List<ItemClass> sortedList = new List<ItemClass>();
    private UserBag userBag = null;
    private E_ItemCategory selectCategory = default(E_ItemCategory);

    private InvenCompare invenCompare = new InvenCompare();

    private Action<ItemInfoData> slotClickCallback = null;

    public override E_UILayers GetLayer()
    {
        return E_UILayers.FrontGame;
    }

    public override bool HasDimmed()
    {
        return true;
    }
    public override Action GetDimmedCallback()
    {
        return OnClickClose;
    }
    protected override void OnCreate()
    {
        UserGameData.Get().SubscribeObserver(E_UserMessageGroup.Bag, this);

        slotPool = new UIItemPool<UIInventorySlot>(E_UIChildType.InventorySlot, itemContents);
        userBag = UserGameData.Get().GetUserData<UserBag>();

        BoxOpenTypeToggle.onValueChanged.AddListener((bool ison) =>
        {
            if (ison)
                OnClickOpenBoxToggle(ison);
        });
        ItemTypeToggle.onValueChanged.AddListener((bool ison) =>
        {
            if (ison)
                OnClickItemToggle(ison);
        });

        BoxOpenTypeToggle.onValueChanged.Invoke(true);

        RefreshUI();
    }

    protected override void OnUIDestroy()
    {
        slotPool.FreeCollection(slotList);
        slotPool.Release();
        userBag = null;

        UserGameData.Get().UnsubscribeObserver(E_UserMessageGroup.Bag, this);

        base.OnUIDestroy();
    }

    public void RefreshUI()
    {
        slotPool.FreeCollection(slotList);

        var bagItems = userBag.GetBagItems();

        sortedList.Clear();

        foreach (var item in bagItems.Values)
        {
            if(item.GetData().invenSort > 0
            && item.GetData().categoryType == selectCategory
            && item.Count != 0)
            {
                sortedList.Add(item);
            }
        }

        sortedList.Sort(invenCompare);
        foreach (var item in sortedList)
        {
            var slot = slotPool.Alloc();

            slot.SetData(item.GetData(), slotClickCallback);

            slot.UpdateCount(item.Count);

            slotList.Add(slot);
        }
    }

    public void OnClickOpenBoxToggle(bool ison)
    {
        if (ison)
        {
            selectCategory = E_ItemCategory.Consumable;
            slotClickCallback = clickBoxSlotCallback;

            RefreshUI();
        }

    }

    public void OnClickItemToggle(bool ison)
    {
        if (ison)
        {
            selectCategory = E_ItemCategory.Ingredients;
            slotClickCallback = clickIngredientsSlotCallback;

            RefreshUI();
        }
    }

    private void clickBoxSlotCallback(ItemInfoData itemInfo)
    {
        var boxInfo = TableManager.Instance.BoxInfoData.Find(itemInfo.itemValue);
        if (boxInfo.BoxType == eBoxRewardType.SELECT)
        {
            var popupBoxOpen = PopupManager.Instance.CreatePopup<PopupSelectBoxOpen>();
            popupBoxOpen.Set(itemInfo, boxInfo);
        }
        else
        {
            var popupBoxOpen = PopupManager.Instance.CreatePopup<PopupBoxOpen>();
            popupBoxOpen.Set(new UILogicBoxOpenCounter(itemInfo, boxInfo));
        }
    }

    private void clickIngredientsSlotCallback(ItemInfoData itemInfo)
    {
        var popupDetailTooltip = PopupManager.Instance.CreatePopup<PopupInventoryDetailTooltip>();
        popupDetailTooltip.Set(itemInfo);
    }

    public override void OnClickOk()
    {
        OnClickClose();
    }

    private void updateStackCount(int itemIndex)
    {
        var count = userBag.GetItemCount(itemIndex);

        foreach(var slot in slotList)
        {
            if(itemIndex == slot.GetItemIndex())
            {
                slot.UpdateCount(count);

                break;
            }
        }
    }
    void UserObserver.IListener.OnEvent(UserGameData owner, UserParameter param)
    {
        int itemIndex = param.GetValue<int>();

        if (param.type == E_UserMessage.Bag_ChangeCount && userBag.GetItemCount(itemIndex) > 0)
        {
            updateStackCount(itemIndex);
        }
        else
        {
            RefreshUI();
        }
    }
}
