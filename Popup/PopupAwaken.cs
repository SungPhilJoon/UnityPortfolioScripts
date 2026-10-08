using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupAwaken : BasePopup, UserObserver.IListener
{
    [SerializeField] private Transform[] slotPosContainer = null;
    [SerializeField] private Toggle[] presetToggles = null;
    [SerializeField] private Toggle[] awakenGradeToggles = null;
    [SerializeField] private GameObject[] awakenSystemOpenCover = null;
    [SerializeField] private TMPro.TMP_Text gauntletName = null;

    [SerializeField] private GameObject awakenButton = null;
    [SerializeField] private GameObject autoAwakenButton = null;
    [SerializeField] private GameObject awakenButtonCover = null;
    [SerializeField] private GameObject autoAwakenButtonCover = null;
    [SerializeField] private TMPro.TMP_Text awakenBtnText = null;
    [SerializeField] private TMPro.TMP_Text awakenBtnCoverText = null;
    [SerializeField] private Transform randomStatInfoPos = null;

    [SerializeField] private GameObject awakenButtonsPanel = null;
    [SerializeField] private GameObject duringAutoAwakenPanel = null;
    [SerializeField] private GameObject duringAutoAwakenActivePanel = null;

    [SerializeField] private Transform previewPos = null;
    [SerializeField] private Vector3 defaultPos = new Vector3(1f, 0f, -1f);
    [SerializeField] private Vector3 defaultRot = new Vector3(0f, 45f, -45f);

    [SerializeField] private float slotAwakenParticlePlayTime = 0.3f;

    private UserAwaken userAwaken = null;
    private List<UIAwakenSlot> awakenSlots = new List<UIAwakenSlot>();
    private int awakenCount => (awakenData.slotCount - userAwaken.GetLockCount(selectGrade) - userAwaken.GetSystemLockCount(awakenData));

    private UIPreview uiPreview = null;
    private Transform previewObjectTr = null;
    private PreviewWorld previewWorld = null;
    private UIRandomStatInfo randomStatInfo = null;

    private CharAwaken awakenData = null;
    private ITEM_GRADE selectGrade = ITEM_GRADE.NONE;

    private UpdateEnumerator updater = new UpdateEnumerator();

    private int currentPreset = 0;

    private bool isDuringAutoAwaken = false;

    protected override void OnUnityAwake()
    {
        base.OnUnityAwake();

        userAwaken = UserGameData.Get().GetUserData<UserAwaken>();
        UserGameData.Get().SubscribeObserver(E_UserMessageGroup.Bag, this);

        int maxPresetCount = TableManager.Instance.CommonData.Find("AwakenPresetCount").Default().GetInt();

        for (int i = 1; i <= maxPresetCount; i++)
        {
            AddPresetToggleListener(i);
        }

        var awakenDatas = TableManager.Instance.CharAwaken.GetValueCollections();
        foreach (var awakenData in awakenDatas)
        {
            AddGradeToggleListener(awakenData.grade);
            SetAwakenSystemOpenCoverActive(awakenData.grade, awakenData.awakenSystemOpenType);

            UIManager.RedDot().InstallRedDot(E_RedDotType.Awaken_Grade, awakenGradeToggles[(int)awakenData.grade].gameObject, E_RedDotAnchor.RightTop, awakenData.grade);
        }

        for (int i = 0; i < slotPosContainer.Length; i++)
        {
            var slot = UIManager.Instance.CreateUIItem<UIAwakenSlot>(E_UIChildType.AwakenSlot, slotPosContainer[i]);
            slot.Initialize(slotLockListener);

            awakenSlots.Add(slot);
        }

        UIManager.Instance.SendEvent(E_UIEvent.OpenAwaken);
    }

    protected override void OnCreate()
    {
        base.OnCreate();

        previewObjectTr = createAwakenGauntlet(ITEM_GRADE.NORMAL);
        previewWorld = UIManager.Instance.CreatePreviewWorld(previewObjectTr, defaultPos, defaultRot);

        uiPreview = UIManager.Instance.CreateUIItem<UIPreview>(E_UIChildType.Preview, previewPos);
        uiPreview.Initialze(previewObjectTr, previewWorld.GetObjectParentTransform(), previewWorld.ShowRenderTexture(), null, null, null);

        var currentToggle = presetToggles[userAwaken.GetCurrentPreset() - 1];
        if (currentToggle.isOn)
        {
            currentToggle.onValueChanged.Invoke(true);
        }
        else
        {
            currentToggle.isOn = true;
        }

        userAwaken = UserGameData.Get().GetUserData<UserAwaken>();
    }

    protected override void OnUnityDestroy()
    {
        userAwaken = null;

        base.OnUnityDestroy();
    }
    public override void Update()
    {
        updater.Update();
    }

    public override E_UILayers GetLayer()
    {
        return E_UILayers.FrontGame;
    }

    public void OnClick_Awaken()
    {
        clickRandomOptionLogic(awakenClickEvent);
    }

    private void awakenClickEvent()
    {
        if (userAwaken.CanAwaken(awakenData) == false || awakenCount <= 0)
        {
            OnClick_AwakenCover();

            return;
        }

        userAwaken.SetRandomOption(awakenData.randomOptionGroupId, awakenCount);
        updater.Push(playParticle());

        SoundManager.Instance.PlaySound(SoundStaticID.AwakenRandomOption.GetKey(), E_SoundType.UI);
    }

    private IEnumerator playParticle()
    {
        refreshUI();

        int slotCount = 0;
        UIAwakenSlot checkSlot = null;
        for (int i = 0; i < awakenData.slotActivate.Length; i++)
        {
            if (awakenData.slotActivate[i] == true)
            {
                int currentSlotCount = slotCount++;
                if (UserGameData.Get().GetContentsOpenValue(awakenData.slotSystemOpenType[currentSlotCount]) == false
                    || userAwaken.GetAwakenInfo(selectGrade, currentSlotCount).GetIsLock() == true)
                {
                    continue;
                }

                checkSlot = awakenSlots[i];
                checkSlot.ShowAwakenParticle();
            }
        }

        if (checkSlot == null)
        {
            yield break;
        }

        float checkTime = 0f;
        while (slotAwakenParticlePlayTime > checkTime)
        {
            checkTime += Time.deltaTime;
            yield return Yielders.EndOfFrame;
        }

        allOffParticle();

        yield return Yielders.EndOfFrame;
    }

    public void OnClick_AutoAwaken()
    {
        clickRandomOptionLogic(autoAwakenClickEvent);
    }

    private void autoAwakenClickEvent()
    {
        if (userAwaken.CanAwaken(awakenData) == false)
        {
            OnClick_StopAutoAwaken();

            return;
        }

        if (isDuringAutoAwaken == false)
        {
            isDuringAutoAwaken = true;
        }

        Util.SetActiveObject(duringAutoAwakenPanel, true);
        Util.SetActiveObject(awakenButtonsPanel, false);
        Util.SetActiveObject(duringAutoAwakenActivePanel, true);

        updater.Push(executeAutoAwaken(autoAwakenClickEvent, stopAutoAwakenClickEvent));
        updater.Push(playParticle());

        SoundManager.Instance.PlaySound(SoundStaticID.AwakenRandomOption.GetKey(), E_SoundType.UI);
    }

    private void clickRandomOptionLogic(System.Action clickEvent)
    {
        bool askExecuteAwaken = userAwaken.AskExecuteAwaken(selectGrade);

        if (askExecuteAwaken)
        {
            PopupManager.Instance.CreatePopup<PopupCommonMessage>(_popup =>
            {
                _popup.Init(1, string.Empty, LocalizeManager.Instance.GetTXT("STR_MSG_AWAKEN_ERROR3"), LocalizeManager.Instance.GetTXT("STR_UI_CONFIRM"), LocalizeManager.Instance.GetTXT("STR_UI_CANCEL"), confirm =>
                {
                    if (confirm)
                    {
                        clickEvent.Invoke();

                        UIToastFacade.ShowCombatPower();
                    }
                });
            });
        }
        else
        {
            clickEvent.Invoke();
        }
    }

    private IEnumerator executeAutoAwaken(System.Action continuousCallback, System.Action stopCallback)
    {
        int randomGroupID = TableManager.Instance.CharAwaken.Find(selectGrade).randomOptionGroupId;
        bool isContinuous = userAwaken.SetAutoRandomOption(randomGroupID, awakenCount);

        if (isContinuous)
        {
            continuousCallback.Invoke();
        }
        else
        {
            if (userAwaken.IsStopAutoAwaken(selectGrade))
            {
                PopupManager.Instance.CreatePopup<PopupCommonMessage>(_popup =>
                {
                    _popup.Init(0, string.Empty, LocalizeManager.Instance.GetTXT("STR_UI_AUTOAWAKEN_SUCCESS"), LocalizeManager.Instance.GetTXT("STR_UI_CONFIRM"), LocalizeManager.Instance.GetTXT("STR_UI_CANCEL"), null);
                });
            }

            stopCallback.Invoke();
        }

        yield return Yielders.EndOfFrame;
    }

    public void OnClick_StopAutoAwaken()
    {
        stopAutoAwakenClickEvent();
    }

    private void stopAutoAwakenClickEvent()
    {
        Util.SetActiveObject(duringAutoAwakenPanel, false);
        Util.SetActiveObject(awakenButtonsPanel, true);
        Util.SetActiveObject(duringAutoAwakenActivePanel, false);

        if (userAwaken.CanAwaken(awakenData) == false)
        {
            OnClick_AwakenCover();
        }

        userAwaken.StopAutoAwaken();
        updater.ClearAllEnumerators();

        if (isDuringAutoAwaken == true)
        {
            isDuringAutoAwaken = false;
        }

        refreshUI();

        UIToastFacade.ShowCombatPower();
    }

    public void OnClick_ShowRandomDataPopup()
    {
        randomStatInfo = UIManager.Instance.CreateUIItem<UIRandomStatInfo>(E_UIChildType.RandomStatInfo, randomStatInfoPos);
        randomStatInfo.Set(awakenData.randomOptionGroupId, () => randomStatInfo = null);

        UIUtil.Stretch(randomStatInfo.rectTransform);
    }

    public void OnClick_AwakenCover()
    {
        if (awakenCount <= 0)
        {
            UIToastFacade.ShowSimple("STR_MSG_AWAKEN_ERROR2");

            return;
        }

        if (userAwaken.CanAwaken(awakenData) == false)
        {
            UIToastFacade.ShowSimple("STR_MSG_AWAKEN_ERROR1");

            return;
        }
    }

    protected override void OnUIDestroy()
    {
        randomStatInfo = null;

        UIManager.Instance.SendEvent(E_UIEvent.CloseAwaken);

        UIManager.RedDot().UnInstallAllRedDots(E_RedDotType.Awaken_Grade);

        userAwaken.StopAutoAwaken();
        updater.ClearAllEnumerators();

        uiPreview.DestroyGameObject();
        uiPreview = null;

        Destroy(previewObjectTr.gameObject);
        previewObjectTr = null;

        UIManager.Instance.ReleasePreviewWorld();

        UserGameData.Get().UnsubscribeObserver(E_UserMessageGroup.Bag, this);

        base.OnUIDestroy();
    }

    private void setSlot()
    {
        if (awakenSlots.Count <= 0)
        {
            Debugger.Error($"AwakenSlots is Empty");
        }

        for (int i = 0; i < awakenData.slotActivate.Length; i++)
        {
            bool activate = awakenData.slotActivate[i];
            Util.SetActiveObject(awakenSlots[i].gameObject, activate);
        }

        refreshUI();
    }

    private void setPanel(ITEM_GRADE awakenGrade)
    {
        allOffParticle();
        if (selectGrade == awakenGrade && currentPreset == userAwaken.GetCurrentPreset())
        {
            return;
        }

        setUIData(awakenGrade);
        var gauntletTr = createAwakenGauntlet(selectGrade);

        Destroy(previewObjectTr.gameObject);
        previewObjectTr = null;
        previewObjectTr = gauntletTr;
        previewWorld.Initialize(previewObjectTr, defaultPos, defaultRot);
        uiPreview.Initialze(previewObjectTr, previewWorld.GetObjectParentTransform(), previewWorld.ShowRenderTexture(), null, null, null);

        setSlot();
        setGauntletName(awakenData.ui3DPrefabName);
    }

    private void setUIData(ITEM_GRADE selectGrade)
    {
        if (this.selectGrade == selectGrade)
        {
            return;
        }

        this.selectGrade = selectGrade;
        awakenData = TableManager.Instance.CharAwaken.Find(selectGrade);

        int activeIndex = 0;
        int slotCount = awakenData.slotCount;
        for (int i = 0; i < awakenSlots.Count; i++)
        {
            if (awakenData.slotActivate[i] == true)
            {
                awakenSlots[i].SetSlotNum(activeIndex);
                activeIndex++;
            }

            if (slotCount <= activeIndex)
            {
                break;
            }
        }
    }

    private void refreshUI()
    {
        int activeIndex = 0;
        for (int i = 0; i < awakenData.slotActivate.Length; i++)
        {
            if (awakenData.slotActivate[i] == true)
            {
                var systemOpenType = awakenData.slotSystemOpenType[activeIndex];
                awakenSlots[i].SetSlot(userAwaken.GetAwakenInfo(selectGrade, activeIndex), systemOpenType);

                activeIndex++;
            }

            if (activeIndex >= awakenData.slotCount)
            {
                break;
            }
        }

        setButtonState();
    }

    private void allOffParticle()
    {
        for (int i = 0; i < awakenSlots.Count; i++)
        {
            awakenSlots[i].OffAwakenParticle();
        }
    }

    private void slotLockListener(int slotNum, bool isOn)
    {
        userAwaken.SetSlotLock(selectGrade, slotNum, isOn);
        setButtonState();
    }

    private void AddGradeToggleListener(ITEM_GRADE awakenGrade)
    {
        var gradeToggle = awakenGradeToggles[(int)awakenGrade];

        gradeToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                setPanel(awakenGrade);

                ConfigManager.Instance.SetEnum(E_GameConfig.AwakenSelectGrade, awakenGrade);
            }
        });
    }

    private void AddPresetToggleListener(int preset)
    {
        var presetToggle = presetToggles[preset - 1];

        presetToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
            {
                if (isDuringAutoAwaken)
                {
                    stopAutoAwakenClickEvent();
                }

                userAwaken.ChangePreset(preset);
                var selectGrade = getSelectGrade();
                var defaultToggle = awakenGradeToggles[(int)selectGrade];
                if (defaultToggle.isOn == true)
                {
                    defaultToggle.onValueChanged.Invoke(true);
                }
                else
                {
                    defaultToggle.isOn = true;
                }

                currentPreset = preset;

                UIToastFacade.ShowCombatPower();
            }
        });
    }

    private void SetAwakenSystemOpenCoverActive(ITEM_GRADE awakenGrade, eContentsOpenType openType)
    {
        Util.SetActiveObject(awakenSystemOpenCover[(int)awakenGrade], !UserGameData.Get().GetContentsOpenValue(openType));
    }

    private Transform createAwakenGauntlet(ITEM_GRADE selectGrade)
    {
        var awakenData = TableManager.Instance.CharAwaken.Find(selectGrade);
        var gauntletTr = ResourceManager.Instance.Instantiate<Transform>(awakenData.ui3DPrefabPath, E_DataType.UIITEM);

        int scale = awakenData.ui3DPrefabScale;
        gauntletTr.localScale = Vector3.one * scale;

        return gauntletTr;
    }

    private void setButtonState()
    {
        long count = awakenData.costValue * (userAwaken.GetLockCount(selectGrade) + 1);

        if (userAwaken.CanAwaken(awakenData))
        {
            awakenBtnText.text = StringUtil.Format(LocalizeManager.Instance.GetTXT("STR_UI_AWAKEN_AWAKENBTN"), GameTextUtil.GetUnitText(count, UnitTextType.Resource));

            Util.SetActiveObject(awakenButton, true);
            Util.SetActiveObject(autoAwakenButton, true);
            Util.SetActiveObject(awakenButtonCover, false);
            Util.SetActiveObject(autoAwakenButtonCover, false);
        }
        else
        {
            awakenBtnCoverText.text = StringUtil.Format(LocalizeManager.Instance.GetTXT("STR_UI_AWAKEN_AWAKENBTN"), GameTextUtil.GetUnitText(count, UnitTextType.Resource));

            long useItemCount = UserGameData.Get().GetUserData<UserBag>().GetItemCount(awakenData.useItemId);
            if (useItemCount >= count)
            {
                awakenBtnCoverText.color = Color.white;
            }
            else
            {
                awakenBtnCoverText.color = Color.red;
            }

            Util.SetActiveObject(awakenButton, false);
            Util.SetActiveObject(autoAwakenButton, false);
            Util.SetActiveObject(awakenButtonCover, true);
            Util.SetActiveObject(autoAwakenButtonCover, true);
        }
    }

    private void setGauntletName(string gauntletName)
    {
        this.gauntletName.text = LocalizeManager.Instance.GetTXT(gauntletName);
    }

    public void OnEvent(UserGameData owner, UserParameter param)
    {
        switch (param.type)
        {
            case E_UserMessage.Bag_ChangeCount:
            case E_UserMessage.Bag_UseItem:
            case E_UserMessage.Bag_AddItem:
            case E_UserMessage.Bag_RemoveItem:
                {
                    int itemIndex = param.GetValue<int>();
                    if (itemIndex == awakenData.useItemId)
                    {
                        setButtonState();
                    }
                }
                break;
        }
    }

    private ITEM_GRADE getSelectGrade()
    {
        var selectGrade = ConfigManager.Instance.GetEnum<ITEM_GRADE>(E_GameConfig.AwakenSelectGrade);
        var selectAwakenData = TableManager.Instance.CharAwaken.Find(selectGrade);
        if (!UserGameData.Get().GetContentsOpenValue(selectAwakenData.awakenSystemOpenType))
        {
            selectGrade = ITEM_GRADE.NORMAL;
        }

        return selectGrade;
    }

    public void ButtonDownCallback()
    {
    }

    public void ButtonUpCallback()
    {
        UIToastFacade.ShowCombatPower();
    }
}
