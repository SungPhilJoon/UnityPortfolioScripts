using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class PopupPet : BasePopup, UserObserver.IListener, IPointerClickHandler
{
    [System.Serializable]
    struct PetElementalSlot
    {
        public eStatType statType;
        public GameObject elementalSlotObject;
        public TMPro.TMP_Text valueText;
    }

    [SerializeField] private PetElementalSlot[] petElementalSlots = null;

    [SerializeField] private List<UIShareEquipSlot> equipSlots = null;
    [SerializeField] private ScrollRect petSlotPos = null;
    [SerializeField] private TMPro.TMP_Text haveCountText = null;

    [SerializeField] private ScrollRect totalStatScroll = null;

    [SerializeField] private UIPetDetailInfoPanel petDetailPanel = null;

    [SerializeField] private float titleTextSpace = 0f;

    [SerializeField] private UIPetComposePanel petComposePanel = null;

    private UserPet userPet = null;

    private List<UIShareSlot> petSlots = new List<UIShareSlot>();

    private List<UIPetTotalStatTitle> totalStatTitleTextList = new List<UIPetTotalStatTitle>();
    private List<UIPetTotalStatValueText> totalStatValueTextList = new List<UIPetTotalStatValueText>();
    private List<UISeperator> statSeperatorList = new List<UISeperator>();

    private int selectIndex = 0;
    private int selectEquipSlotIndex = -1;
    private UIShareSlot selectSlot = null;

    private UIItemPool<UIPetTotalStatTitle> totalStatTitlePool = null;
    private UIItemPool<UIPetTotalStatValueText> totalStatValuePool = null;
    private UIItemPool<UISeperator> statSeperatorPool = null;

    private bool isReadyEquip = false;


    protected override void OnUnityAwake()
    {
        base.OnUnityAwake();

        userPet = UserGameData.Get().GetUserData<UserPet>();
        UserGameData.Get().SubscribeObserver(E_UserMessageGroup.Pet, this);
    }

    public override E_UILayers GetLayer()
    {
        return E_UILayers.FrontGame;
    }
    public override bool HasDimmed()
    {
        return true;
    }
    public override bool IsLowSoundVolume()
    {
        return true;
    }

    protected override void OnCreate()
    {
        base.OnCreate();

        UIManager.Instance.SendEvent(E_UIEvent.OpenPet);

        totalStatTitlePool = new UIItemPool<UIPetTotalStatTitle>(E_UIChildType.PetTotalStatTitle, totalStatScroll.content);
        totalStatValuePool = new UIItemPool<UIPetTotalStatValueText>(E_UIChildType.PetTotalStatValue, totalStatScroll.content);
        statSeperatorPool = new UIItemPool<UISeperator>(E_UIChildType.Separator, totalStatScroll.content);

        CreateSlots();

        initStatTextList();
        updateElementalSlots();
    }

    public void CreateSlots()
    {
        int totalCount = 0;
        int haveCount = 0;
        var keys = TableManager.Instance.PetInfoData.GetKeyCollections();
        foreach (var key in keys)
        {
            var petInfo = userPet.GetGrowthInfo(key);
            var petSlot = UIManager.Instance.CreateUIItem<UIShareSlot>(E_UIChildType.ShareInfoSlot, petSlotPos.content);

            petSlot.Initialize(petInfo, petSlotCallback);
            petSlot.SetSubImage(true);
            petSlot.ReInit();
            petSlot.SetStars(TableManager.Instance.PetGradeData.Find(petInfo.GetGrade()).GetMaxGrade());

            UIManager.RedDot().InstallRedDot(E_RedDotType.Pet_New, petSlot.gameObject, E_RedDotAnchor.RightTop, key, true);

            totalCount++;
            if (petInfo.HasInfo())
            {
                haveCount++;
            }

            petSlots.Add(petSlot);
        }

        updateSlotsEquipObject();

        haveCountText.text = $"{haveCount.ToString()}/{totalCount.ToString()}";

        for (int i = 0; i < equipSlots.Count; i++)
        {
            int slotNum = i;
            void clickSlotCallback()
            {
                this.clickSlotCallback(slotNum);
            }

            void equipSlotClickCallback(IGrowthInfo growthInfo)
            {
                this.equipSlotClickCallback(growthInfo, slotNum);
            }

            void unEquipCallback(IGrowthInfo growthInfo)
            {
                this.unEquipCallback(growthInfo, slotNum);
            }

            var equipPet = userPet.GetEquipPet(i);
            equipSlots[i].Initialize(equipPet, clickSlotCallback, equipSlotClickCallback, unEquipCallback);
        }

    }

    private void equipCallback()
    {
        if (isReadyEquip == false)
        {
            ReadyEquip();
        }
        else
        {
            if (selectEquipSlotIndex >= 0)
            {
                equipSlotClickCallback(userPet.GetGrowthInfo(selectIndex), selectEquipSlotIndex);

                selectEquipSlotIndex = -1;
            }
            else
            {
                UnReadyEquip();
            }
        }
    }

    private void closeCallback()
    {
        selectSlot?.SetSelect(false);

        selectSlot = null;
        selectIndex = -1;

        if (isReadyEquip)
        {
            UnReadyEquip();
        }
    }

    private void petSlotCallback(UIShareSlot selectSlot, IGrowthInfo growthInfo)
    {
        if (selectIndex == growthInfo.GetIndex())
        {
            return;
        }

        this.selectSlot?.SetSelect(false);

        this.selectIndex = growthInfo.GetIndex();
        this.selectSlot = selectSlot;

        this.selectSlot.SetSelect(true);

        userPet.SelectPet(this.selectIndex);

        petDetailPanel.Set(growthInfo, equipCallback, unReadyEquipCallback, closeCallback);

        if (isReadyEquip && selectEquipSlotIndex >= 0)
        {
            userPet.EquipPet(selectIndex, selectEquipSlotIndex);
            selectEquipSlotIndex = -1;

            updateElementalSlots();
            updateTotalStatValueTextList();

            SoundManager.Instance.RelaySound(SoundStaticID.PetEquip.GetKey(), E_SoundType.UI);
        }

        PopupManager.Instance.ClosePopup<PopupPetRandomOption>();

        UIManager.NewIcon().RemoveNewObject(ITEM_TYPE.PET, selectIndex);

        UIManager.Instance.SendEvent(E_UIEvent.ConfirmPetSlot);
    }

    private void clickSlotCallback(int slot)
    {
        selectEquipSlotIndex = slot;

        if (isReadyEquip == false)
        {
            ReadyEquip(slot);
        }
        else
        {
            if (selectIndex > 0)
            {
                userPet.EquipPet(selectIndex, slot);

                updateElementalSlots();
                updateTotalStatValueTextList();

                selectEquipSlotIndex = -1;

                SoundManager.Instance.RelaySound(SoundStaticID.PetEquip.GetKey(), E_SoundType.UI);
            }
            else
            {
                UnReadyEquip();
            }
        }
    }

    private void equipSlotClickCallback(IGrowthInfo growthInfo, int slot)
    {
        if (isReadyEquip == false)
        {
            selectEquipSlotIndex = slot;
            if (growthInfo == null)
            {
                equipCallback();
                return;
            }

            for (int i = 0; i < petSlots.Count; i++)
            {
                if (petSlots[i].FindSlot(growthInfo.GetIndex()))
                {
                    petSlots[i].OnClick_Slot();
                    break;
                }
            }

            this.selectIndex = growthInfo.GetIndex();
            userPet.SelectPet(this.selectIndex);

            ReadyEquip(slot);
        }
        else
        {
            if (selectEquipSlotIndex >= 0)
            {
                var equipPetInfo = userPet.GetEquipPet(selectEquipSlotIndex);
                if (equipPetInfo == null)
                {
                    if (selectEquipSlotIndex != slot)
                    {
                        UnReadyEquip();
                        return;
                    }
                }
            }

            userPet.EquipPet(this.selectIndex, slot);
            selectEquipSlotIndex = -1;

            updateElementalSlots();
            updateTotalStatValueTextList();

            UnReadyEquip();

            SoundManager.Instance.RelaySound(SoundStaticID.PetEquip.GetKey(), E_SoundType.UI);
        }
    }

    private void unEquipCallback(IGrowthInfo growthInfo, int slot)
    {
        userPet.UnEquipPet(slot);

        selectEquipSlotIndex = -1;

        UnReadyEquip();

        updateElementalSlots();
        updateTotalStatValueTextList();
    }

    private void ReadyEquip(int slotIndex)
    {
        isReadyEquip = true;

        equipSlots[slotIndex].ReadyEquip(true);
    }

    private void ReadyEquip()
    {
        isReadyEquip = true;

        for (int i = 0; i < equipSlots.Count; i++)
        {
            equipSlots[i].ReadyEquip(false);
        }
    }

    private void UnReadyEquip()
    {
        isReadyEquip = false;

        for (int i = 0; i < equipSlots.Count; i++)
        {
            equipSlots[i].UnReadyEquip();
        }
    }

    private void updateElementalSlots()
    {
        for (int i = 0; i < petElementalSlots.Length; i++)
        {
            petElementalSlots[i].elementalSlotObject.SetActive(false);
        }

        for (int i = 0; i < petElementalSlots.Length; i++)
        {
            eStatType statType = petElementalSlots[i].statType;
            float statValue = userPet.GetEquipStatTotalValue(statType);
            if (statValue <= 0)
            {
                petElementalSlots[i].elementalSlotObject.SetActive(false);
                continue;
            }

            petElementalSlots[i].elementalSlotObject.SetActive(true);
            petElementalSlots[i].valueText.text = UIStatBinder.ConvertTextPlus(statType, statValue);
        }
    }

    private void initStatTextList()
    {
        var equipTitleText = totalStatTitlePool.Alloc();
        equipTitleText.SetTitle(LocalizeManager.Instance.GetTXT("STR_UI_PET_TOTAL_STAT2"));

        totalStatTitleTextList.Add(equipTitleText);

        var equipStatEnumerator = userPet.GetEquipStatTotalValues();
        while (equipStatEnumerator.MoveNext())
        {
            var equipStat = equipStatEnumerator.Current;

            eStatType statType = equipStat.Key;
            float statValue = equipStat.Value;

            var valueText = totalStatValuePool.Alloc();
            valueText.SetText(statType, statValue);

            totalStatValueTextList.Add(valueText);
        }

        var statSeperator = statSeperatorPool.Alloc();
        statSeperator.Set(RectTransform.Axis.Vertical, titleTextSpace);

        statSeperatorList.Add(statSeperator);

        var randomTitleText = totalStatTitlePool.Alloc();
        randomTitleText.SetTitle(LocalizeManager.Instance.GetTXT("STR_UI_PET_TOTAL_STAT3"));

        totalStatTitleTextList.Add(randomTitleText);

        var randomStatEnumerator = userPet.GetRandomStatTotalValues();
        while (randomStatEnumerator.MoveNext())
        {
            var randomStat = randomStatEnumerator.Current;

            eStatType statType = randomStat.Key;
            float statValue = randomStat.Value;

            var valueText = totalStatValuePool.Alloc();
            valueText.SetText(statType, statValue);

            totalStatValueTextList.Add(valueText);
        }
    }

    private void updateTotalStatValueTextList()
    {
        totalStatTitlePool.FreeCollection(totalStatTitleTextList);
        totalStatValuePool.FreeCollection(totalStatValueTextList);
        statSeperatorPool.FreeCollection(statSeperatorList);

        initStatTextList();
    }

    private void updateSlotsEquipObject()
    {
        int index = 0;
        var enumerator = TableManager.Instance.PetInfoData.GetValueCollections().GetEnumerator();

        while (enumerator.MoveNext())
        {
            var isEquip = userPet.IsEquip(enumerator.Current.Index, out _);
            petSlots[index].SetEquipObjectActive(isEquip);

            index++;
        }
    }

    protected override void OnUIDestroy()
    {
        base.OnUIDestroy();

        var values = TableManager.Instance.PetInfoData.GetValueCollections();
        foreach (var value in values)
        {
            if (userPet.HasData(value.Index) == false)
            {
                continue;
            }

            UIManager.NewIcon().RemoveNewObject(ITEM_TYPE.PET, value.Index);
        }

        UIManager.Instance.SendEvent(E_UIEvent.ClosePet);

        equipSlots.DestroyGameObjects();
        equipSlots.Clear();

        if (petDetailPanel.gameObject.activeInHierarchy)
        {
            petDetailPanel.ClosePanel();
        }

        totalStatTitlePool.FreeCollection(totalStatTitleTextList);
        totalStatValuePool.FreeCollection(totalStatValueTextList);
        statSeperatorPool.FreeCollection(statSeperatorList);

        totalStatTitlePool.Release();
        totalStatValuePool.Release();
        statSeperatorPool.Release();

        petComposePanel.Release();

        UserGameData.Get().UnsubscribeObserver(E_UserMessageGroup.Pet, this);
        UIManager.RedDot().UnInstallAllRedDots(E_RedDotType.Pet_New);

    }

    public void OnClick_OpenCompose()
    {
        petComposePanel.gameObject.SetActive(true);
        petComposePanel.OpenPanel();
    }

    public override void OnClickClose()
    {
        PopupManager.Instance.ClosePopup<PopupPetRandomOption>();
        PopupManager.Instance.ClosePopup<PopupPetSkillInfoTooltip>();

        base.OnClickClose();
    }

    void ObserverPattern<UserGameData, UserParameter>.IListener.OnEvent(UserGameData owner, UserParameter param)
    {
        switch (param.type)
        {
            case E_UserMessage.Pet_UpdateSlot:
                {
                    for (int i = 0; i < equipSlots.Count; i++)
                    {
                        var equipPetInfo = userPet.GetEquipPet(i);
                        equipSlots[i].SetInfo(equipPetInfo);
                    }

                    UnReadyEquip();

                    UIToastFacade.ShowCombatPower();

                    updateSlotsEquipObject();
                }
                break;

            case E_UserMessage.Pet_Add:
            case E_UserMessage.Pet_Decrease:
                {
                    int petIndex = param.GetValue<int>();

                    for (int i = 0; i < petSlots.Count; i++)
                    {
                        if (petSlots[i].FindSlot(petIndex))
                        {
                            petSlots[i].ReInit();
                            petSlots[i].SetStars(TableManager.Instance.PetGradeData.Find(userPet.GetGrowthInfo(petIndex).GetGrade()).GetMaxGrade());
                            break;
                        }
                    }
                }
                break;

            case E_UserMessage.Pet_Promotion:
                {
                    int petIndex = param.GetValue<int>();
                    var petInfo = userPet.GetGrowthInfo(petIndex);
                    var petGradeData = TableManager.Instance.PetGradeData.Find(petInfo.GetGrade());

                    selectSlot.ReInit();
                    selectSlot.SetStars(petGradeData.GetMaxGrade());

                    updateElementalSlots();
                    updateTotalStatValueTextList();
                }
                break;

            case E_UserMessage.Pet_RandomOption:
                {
                    updateElementalSlots();
                    updateTotalStatValueTextList();
                }
                break;
        }
    }

    private void unReadyEquipCallback()
    {
        if (isReadyEquip)
        {
            UnReadyEquip();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isReadyEquip)
        {
            if (eventData.pointerEnter == null
            || eventData.pointerEnter.GetComponent<UIShareSlot>() == null
            || eventData.pointerEnter.GetComponent<UIShareEquipSlot>() == null)
            {
                UnReadyEquip();
            }
        }
    }
}
