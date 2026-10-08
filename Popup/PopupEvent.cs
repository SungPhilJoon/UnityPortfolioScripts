using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using Extensions;
using System;

public class PopupEvent : BasePopup, UserObserver.IListener
{
    [SerializeField] private TMPro.TMP_Text eventTitleText = null;
    [SerializeField] private TMPro.TMP_Text eventInfoText = null;
    [SerializeField] private TMPro.TMP_Text eventLastTimeText = null;
    [SerializeField] private GameObject eventLastTimePanel = null;
    [SerializeField] private Image bannerImage = null;
    [SerializeField] private Transform eventPanelPos = null;
    [SerializeField] private Transform eventTogglePos = null;
    [SerializeField] private ToggleGroup toggleGroup = null;

    [SerializeField] private Slider progressSlider = null;
    [SerializeField] private TMPro.TMP_Text progressPercentText = null;

    [SerializeField] private GameObject finalRewardSlotObject = null;
    [SerializeField] private PopupItemBox finalRewardSlot = null;
    [SerializeField] private GameObject finalRewardCover = null;

    private UserGameEvent userEvent = null;
    private int selectEventIndex = 0;
    private UIEventPanel currentEventPanelItem = null;
    private List<UIEventToggle> eventToggles = new List<UIEventToggle>();

    private float checkSecond = 0f;

    public override E_UILayers GetLayer()
    {
        return E_UILayers.FrontGame;
    }
    public override bool HasDimmed()
    {
        return true;
    }

    public override UIDimmed.E_Alpha GetDimmedAlpha()
    {
        return UIDimmed.E_Alpha.Zero;
    }
    public override Action GetDimmedCallback()
    {
        return OnClickClose;
    }

    protected override void OnCreate()
    {
        userEvent = UserGameData.Get().GetUserData<UserGameEvent>();
        var eventDataContainer = userEvent.GetEventDataContainer();
        foreach (var eventData in eventDataContainer)
        {
            var eventToggle = UIManager.Instance.CreateUIItem<UIEventToggle>(E_UIChildType.GameEventToggle, eventTogglePos);
            eventToggle.Set(toggleGroup, eventData.Key, toggleCallback);
            eventToggle.SetToggleText(LocalizeManager.Instance.GetTXT(TableManager.Instance.EventData.Find(eventData.Key).eventNameId));
            UIManager.RedDot().InstallRedDot(E_RedDotType.Event_GameEvent, eventToggle.gameObject, E_RedDotAnchor.RightTop, eventData.Key);
            eventToggles.Add(eventToggle);
        }

        currentEventPanelItem = UIManager.Instance.CreateUIItem<UIEventPanel>(E_UIChildType.GameEventPanel, eventPanelPos);
        currentEventPanelItem.Init();

        eventToggles[0].ToggleValueChanged(true);

        UIManager.Instance.SendEvent(E_UIEvent.OpenEvent);

        UserGameData.Get().SubscribeObserver(E_UserMessageGroup.Event, this);
    }

    public override void OnClickOk()
    {
        OnClickClose();
    }

    protected override void OnUIDestroy()
    {
        UIManager.Instance.SendEvent(E_UIEvent.CloseEvent);

        UIManager.RedDot().UnInstallAllRedDots(E_RedDotType.Event_GameEvent);

        foreach (var eventToggle in eventToggles)
        {
            eventToggle.DestroyGameObject();
        }

        currentEventPanelItem?.DestroyGameObject();
        currentEventPanelItem = null;

        UserGameData.Get().UnsubscribeObserver(E_UserMessageGroup.Event, this);

        base.OnUIDestroy();
    }

    private void toggleCallback(int eventIndex, bool isOn)
    {
        if (selectEventIndex == eventIndex)
        {
            return;
        }

        selectEventIndex = eventIndex;
        userEvent.SelectEventData(eventIndex);

        currentEventPanelItem.Set(userEvent);

        UIUtil.Stretch(currentEventPanelItem.rectTransform);

        var selectEventData = userEvent.GetSelectEventData();
        eventTitleText.text = LocalizeManager.Instance.GetTXT(selectEventData.eventNameId);
        eventInfoText.text = LocalizeManager.Instance.GetTXT(selectEventData.eventDescId);
        bannerImage.sprite = UIIconLoader.LoadEventIcon(selectEventData.eventBanner);
        Util.SetActiveObject(progressSlider.gameObject, selectEventData.progressType);

        bool isProgressType = selectEventData.progressType;
        Util.SetActiveObject(finalRewardSlotObject, isProgressType);
        if (isProgressType)
        {
            int clearCount = 0;
            int maxCount = 0;

            for (int i = 0; i < selectEventData.MissionGroupId.Length; i++)
            {
                var eventInfoDic = userEvent.GetGameEventInfoList(selectEventData.MissionGroupId[i]);
                foreach (var eventInfo in eventInfoDic.Values)
                {
                    maxCount++;
                    if (eventInfo.GetRewardState() == true)
                    {
                        clearCount++;
                    }
                }
            }

            progressSlider.minValue = 0f;
            progressSlider.maxValue = maxCount;
            progressSlider.value = clearCount;

            progressPercentText.text = $"{(int)(progressSlider.normalizedValue * 100)}%";

            UIItemBoxFacade.BindItemBox<PopupItemBox>(selectEventData.finalReward[0], finalRewardSlot);
            Util.SetActiveObject(finalRewardCover, userEvent.IsGetFinalReward());
        }

        bool isLimitType = selectEventData.eventType == eTypeEvent.LIMIT;
        Util.SetActiveObject(eventLastTimePanel, isLimitType);
    }

    public void OnEvent(UserGameData owner, UserParameter param)
    {
        switch (param.type)
        {
            case E_UserMessage.Event_GetReward:
                {
                    var index = param.GetValue<int>();
                    var selectEventData = userEvent.GetSelectEventData();
                    if (selectEventData.progressType)
                    {
                        for (int i = 0; i < selectEventData.MissionGroupId.Length; i++)
                        {
                            var eventInfoDic = userEvent.GetGameEventInfoList(selectEventData.MissionGroupId[i]);
                            if (eventInfoDic.ContainsKey(index) == false)
                            {
                                continue;
                            }

                            progressSlider.value += 1;
                            break;
                        }

                        progressPercentText.text = $"{(int)(progressSlider.normalizedValue * 100)}%";
                    }
                }
                break;
        }
    }

    private void updateEventEndTimeText()
    {
        if (userEvent.GetSelectEventData().eventType == eTypeEvent.LIMIT)
        {
            TimeSpan remainTime = userEvent.GetRemainEventTime(selectEventIndex);

            if (remainTime.Seconds != checkSecond)
            {
                checkSecond = remainTime.Seconds;
                UITimeBinder.BindTimeSpan(remainTime, eventLastTimeText, "STR_UI_DUNGEON_TIMELEFT");
            }
        }
    }

    public override void Update()
    {
        base.Update();

        updateEventEndTimeText();
    }

    public void OnClick_GetFinalReward()
    {
        userEvent.GetFinalReward();

        if (userEvent.IsGetFinalReward() == false)
        {
            finalRewardSlot.OnClick_Slot();

            return;
        }

        finalRewardCover.SetActive(true);
    }
}
