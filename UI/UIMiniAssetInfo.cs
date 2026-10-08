using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIMiniAssetInfo : UIItemBase, UserObserver.IListener, UIObserver.IListener
{
    [SerializeField] private Transform rootTransform = null;

    private Dictionary<int, UIAssetCount> assetCounts = new Dictionary<int, UIAssetCount>();
    private Stack<E_UIEvent> eventStack = new Stack<E_UIEvent>();

    //private static int[] defaultAssetButtonIndexs = new int[]
    //{
    //    99102
    //};

    private void OnEnable()
    {
        UserGameData.Get().SubscribeObserver(E_UserMessageGroup.Bag, this);
        UserGameData.Get().SubscribeObserver(E_UserMessageGroup.Asset, this);

        UIManager.Instance.SubscribeObserver(this);
    }

    private void OnDisable()
    {
        UserGameData.Get().UnsubscribeObserver(E_UserMessageGroup.Asset, this);
        UserGameData.Get().UnsubscribeObserver(E_UserMessageGroup.Bag, this);

        UIManager.Instance.UnSubscribeObserver(this);
    }
    public void Release()
    {
        assetCounts.DestroyGameObjects();
    }
 
	public void OnClick_Cash()
	{
        if (UserGameData.Get().GetContentsOpenValue(eContentsOpenType.STORE) == false)
        {
            UserGameData.Get().LockClickAction(eContentsOpenType.STORE);
            return;
        }

        var popup = PopupManager.Instance.TryCreatePopup<PopupShop>();

        popup.GoToPearlStore();
    }

    void ObserverPattern<UserGameData, UserParameter>.IListener.OnEvent(UserGameData owner, UserParameter param)
    {
        switch (param.type)
        {
            case E_UserMessage.Asset_ChangeGold:
                {
                    UpdateAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.GOLD));
                }
                break;
            case E_UserMessage.Asset_ChangePearl:
                {
                    UpdateAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.PEARL));
                }
                break;
            case E_UserMessage.Bag_AddItem:
            case E_UserMessage.Bag_ChangeCount:
            case E_UserMessage.Bag_RemoveItem:
                {
                    int itemIndex = param.GetValue<int>();
                    UpdateAssetCount(itemIndex);
                }
                break;
        }
    }

    void UIObserver.IListener.OnEvent(UIManager owner, UIEventMessage param)
    {
        showAssetInfoByUIEvent(param, out bool isOpenEvent);

        if (isOpenEvent)
        {
            eventStack.Push(param.eventType);
        }
    }

    public void PopOnEvent(E_UIEvent offEventType)
    {
        E_UIEvent onEventType = E_UIEvent.None;
        switch (offEventType)
        {
            // HeroGrowth
            case E_UIEvent.OffHeroAbilityTab:
                {
                    onEventType = E_UIEvent.OnHeroAbilityTab;
                }
                break;

            case E_UIEvent.OffHeroBreakTab:
                {
                    onEventType = E_UIEvent.OnHeroBreakTab;
                }
                break;

            case E_UIEvent.OffHeroSpecificTab:
                {
                    onEventType = E_UIEvent.OnHeroSpecificTab;
                }
                break;

            case E_UIEvent.OffHeroRelicTab:
                {
                    onEventType = E_UIEvent.OnHeroRelicTab;
                }
                break;

            case E_UIEvent.CloseHeroGrowth:
                {
                    onEventType = E_UIEvent.OpenHeroGrowth;
                }
                break;

            case E_UIEvent.CloseGacha:
                {
                    onEventType = E_UIEvent.OpenGacha;
                }
                break;

            case E_UIEvent.CloseShop:
                {
                    onEventType = E_UIEvent.OpenShop;
                }
                break;

            case E_UIEvent.CloseAwaken:
                {
                    onEventType = E_UIEvent.OpenAwaken;
                }
                break;

            case E_UIEvent.ClosePet:
                {
                    onEventType = E_UIEvent.OpenPet;
                }
                break;
        }

        if (eventStack.Count <= 0)
        {
            return;
        }

        var currentEventType = eventStack.Peek();
        if (onEventType != currentEventType)
        {
            return;
        }

        eventStack.Pop();
    }
    private void setAssetCount(int assetIndex, bool isOn)
    {
        var assetItemData = TableManager.Instance.ItemInfoData.Find(assetIndex);

        var assetCount = UIManager.Instance.CreateUIItem<UIAssetCount>(E_UIChildType.AssetCountText, rootTransform);
        assetCount.Set(assetItemData);

        assetCounts.Add(assetItemData.Index, assetCount);

        ShowAssetCount(assetItemData.Index, isOn);

        //일단 1개라서 예외처리지만 후에 고민해 보아야할 문제
        if (assetItemData.itemType == ITEM_TYPE.PEARL)
        {
            assetCount.SetPlusButton(OnClick_Cash);
        }
    }

    public void UpdateAssetCount(int assetIndex)
    {
        if (assetCounts.TryGetValue(assetIndex, out UIAssetCount assetCount) == false)
        {
            return;
        }

        if (assetCount.IsActive())
        {
            assetCount.UpdateCountText();
        }
    }

    public void DeactiveAll()
    {
        foreach(var assetCount in assetCounts.Values)
        {
            _gameObjectExtensions.SetActive(assetCount, false);
        }
    }

    public void ShowAssetCount(int assetIndex, bool isOn)
    {
        if (assetCounts.TryGetValue(assetIndex, out UIAssetCount assetCount) == false)
        {
            setAssetCount(assetIndex, isOn);
        }
        else
        {
            Util.SetActiveObject(assetCount.gameObject, isOn);
            if (assetCount.IsActive())
            {
                UpdateAssetCount(assetIndex);
            }
        }
    }

    private void showAssetInfoByUIEvent(UIEventMessage param, out bool isOpenEvent)
    {
        switch (param.eventType)
        {
            case E_UIEvent.OffHeroAbilityTab:
            case E_UIEvent.OffHeroBreakTab:
            case E_UIEvent.OffHeroSpecificTab:
            case E_UIEvent.OffHeroRelicTab:
            case E_UIEvent.CloseHeroGrowth:
                {
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.SOUL_ESSENCE), false);
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.BREAKTHROUGH), false);

                    PopOnEvent(param.eventType);

                    isOpenEvent = false;
                }
                break;

            case E_UIEvent.OnHeroAbilityTab:
            case E_UIEvent.OnHeroSpecificTab:
                {
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.SOUL_ESSENCE), false);
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.BREAKTHROUGH), false);

                    isOpenEvent = true;
                }
                break;

            case E_UIEvent.OnHeroBreakTab:
                {
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.SOUL_ESSENCE), false);
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.BREAKTHROUGH), true);

                    UpdateAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.BREAKTHROUGH));

                    isOpenEvent = true;
                }
                break;

            case E_UIEvent.OnHeroRelicTab:
                {
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.SOUL_ESSENCE), true);
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.BREAKTHROUGH), false);

                    UpdateAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.SOUL_ESSENCE));

                    isOpenEvent = true;
                }
                break;

            case E_UIEvent.CloseGacha:
                {
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.PEARL), true);
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.GACHA_TICKET_EQUIP), false);
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.GACHA_TICKET_SKILL), false);
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.GACHA_TICKET_AVATAR), false);
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.AVATAR_GACHA_COIN), false);

                    UpdateAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.PEARL));

                    PopOnEvent(param.eventType);

                    isOpenEvent = false;
                }
                break;

            case E_UIEvent.CloseShop:
                {
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.PEARL), true);
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.MILEAGE), false);

                    UpdateAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.PEARL));

                    PopOnEvent(param.eventType);

                    isOpenEvent = false;
                }
                break;

            // Awaken
            case E_UIEvent.OpenAwaken:
                {
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.AWAKEN_JEM), true);

                    isOpenEvent = true;
                }
                break;

            case E_UIEvent.CloseAwaken:
                {
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.AWAKEN_JEM), false);
                    PopOnEvent(param.eventType);

                    isOpenEvent = false;
                }
                break;

            case E_UIEvent.OpenPet:
                {
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.PET_CANDY), true);

                    isOpenEvent = true;
                }
                break;

            case E_UIEvent.ClosePet:
                {
                    ShowAssetCount(StaticItemInfo.GetIndex(ITEM_TYPE.PET_CANDY), false);
                    PopOnEvent(param.eventType);

                    isOpenEvent = false;
                }
                break;

            default:
                {
                    isOpenEvent = false;
                }
                break;
        }
    }
}
