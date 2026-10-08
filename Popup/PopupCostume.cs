using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityNative.Toasts;
using EquipPrefabNames = System.Collections.Generic.Dictionary<E_CostumeItemQuality, string>;
using EquipPreviewIndexer = System.Collections.Generic.Dictionary<E_CostumeItemQuality, int?>;

public class PopupCostume : BasePopup, UserObserver.IListener
{
    [Serializable]
    public class PossessStatPage
    {
        [SerializeField] private GameObject statPageObject = null;
        [SerializeField] private RectTransform[] statGroupRTs = null;

        private Dictionary<int, UIItemPool<UICostumePossessStatText>> statTextPoolDic = new Dictionary<int, UIItemPool<UICostumePossessStatText>>();

        private Dictionary<int, List<UICostumePossessStatText>> possessTextDic = new Dictionary<int, List<UICostumePossessStatText>>();

        private UserCostume userCostume = null;

        public void Initialize(UserCostume userCostume)
        {
            this.userCostume = userCostume;

            for (int i = 0; i < statGroupRTs.Length; i++)
            {
                possessTextDic.Add(i, new List<UICostumePossessStatText>());
                statTextPoolDic.Add(i, new UIItemPool<UICostumePossessStatText>(E_UIChildType.CostumePossessStatText, statGroupRTs[i]));
            }
        }

        public void OpenPage()
        {
            updateText();
            statPageObject.SetActive(true);

            for (int i = 0; i < statGroupRTs.Length; i++)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(statGroupRTs[i]);
            }
        }

        public void ClosePage()
        {
            statPageObject.SetActive(false);
        }

        private void updateText()
        {
            foreach (var statTextPool in statTextPoolDic)
            {
                statTextPool.Value.FreeCollection(possessTextDic[statTextPool.Key]);
            }

            var statDatas = TableManager.Instance.EquipSortStat;
            var statDic = userCostume.GetPossessStatDic();
            foreach (var statData in statDatas)
            {
                var value = userCostume.GetPossessStatValue(statData.statType);
                if (value != null)
                {
                    var possessText = statTextPoolDic[statData.statGroup].Alloc();
                    possessText.SetStatTypeValue(statData.statType, (float)value.Value);

                    possessTextDic[statData.statGroup].Add(possessText);
                }
            }
        }

        public void Release()
        {
            foreach (var statTextPool in statTextPoolDic)
            {
                statTextPool.Value.FreeCollection(possessTextDic[statTextPool.Key]);
                statTextPool.Value.Release();
            }
        }
    }

    [Serializable]
    public class EquipSlot
    {
        [SerializeField] private ITEM_TYPE equipType = default(ITEM_TYPE);
        [SerializeField] private Image itemImage = null;
        [SerializeField] private Image gradeBackgroundImage = null;
        [SerializeField] private Image gradeFrameImage = null;
        [SerializeField] private GameObject disableSlotObj = null;

        public void Set(UserCostume userCostume, ITEM_TYPE equipType, E_CostumeItemQuality currentTabType)
        {
            if (this.equipType != equipType)
            {
                return;
            }

            bool isEquip = userCostume.IsEquip(currentTabType, out int equipCostumeIndex);
            if (isEquip == false)
            {
                disableSlotObj.SetActive(true);

                itemImage.gameObject.SetActive(false);
                gradeBackgroundImage.gameObject.SetActive(false);
                gradeFrameImage.gameObject.SetActive(false);
            }
            else
            {
                var info = userCostume.GetGrowthInfo(equipCostumeIndex);

                disableSlotObj.SetActive(false);

                itemImage.gameObject.SetActive(true);
                gradeBackgroundImage.gameObject.SetActive(true);
                gradeFrameImage.gameObject.SetActive(true);

                itemImage.sprite = UIIconLoader.LoadCostumeSprite(info.GetIconPath());
                gradeBackgroundImage.sprite = UIIconLoader.LoadGradeBackgroundSprite(info.GetData().grade);
                gradeFrameImage.sprite = UIIconLoader.LoadGradeFrameSprite(info.GetData().grade);
            }
        }
    }

    [Serializable]
    public class TabPackage
    {
        public Toggle Tab;
        public ITEM_TYPE TabType;
    }

    [Serializable]
    public class TabSubToggle
    {
        public ITEM_TYPE TabType;
        public Toggle Tab;
        public E_CostumeItemQuality qualityType;
    }

    [SerializeField] private PossessStatPage statPage = null;

    [SerializeField] private EquipSlot[] equipSlots = null;

    [SerializeField] private TabPackage[] ItemTabs = null;
    [SerializeField] private TabSubToggle[] ItemSubTabs = null;
    [SerializeField] private ToggleGroup ItemSubTabToggleGroup = null;
    [SerializeField] private RectTransform slotPos = null;

    [SerializeField] private Transform previewPos = null;

    [SerializeField] private Vector3 actorDummyPos = new Vector3(1f, 0.2f, -2f);
    [SerializeField] private Vector3 actorDummyRot = new Vector3(0f, 15f, 0f);

    private UserCostume userCostume = null;
    private ITEM_TYPE selectItemType = ITEM_TYPE.WEAPON;
    private Dictionary<ITEM_TYPE, E_CostumeItemQuality> currentSelectQualityTypes = new Dictionary<ITEM_TYPE, E_CostumeItemQuality>();

    private UIItemPool<UICostumeSlot> setStatableSlotPool = null;
    private UIItemPool<UICostumeSlot> nonSetStatableSlotPool = null;

    private List<UICostumeSlot> setableSlotList = new List<UICostumeSlot>();
    private List<UICostumeSlot> nonSetableSlotList = new List<UICostumeSlot>();

    private UIPreview uiPreview = null;
    private ActorUserDummy actorDummy = null;

    private Dictionary<ITEM_TYPE, EquipPrefabNames> previewDic = new Dictionary<ITEM_TYPE, EquipPrefabNames>();
    private Dictionary<ITEM_TYPE, EquipPreviewIndexer> previewIndexDic = new Dictionary<ITEM_TYPE, EquipPreviewIndexer>();

    private List<string> dummyAnimationNames = new List<string>();

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

    protected override void OnUnityAwake()
    {
        base.OnUnityAwake();

        userCostume = UserGameData.Get().GetUserData<UserCostume>();

        UserGameData.Get().SubscribeObserver(E_UserMessageGroup.Costume, this);

        selectItemType = ITEM_TYPE.WEAPON;

        currentSelectQualityTypes.Add(ITEM_TYPE.WEAPON, E_CostumeItemQuality.Weapon_Sword);
        currentSelectQualityTypes.Add(ITEM_TYPE.SHIELD, E_CostumeItemQuality.Shield);
        currentSelectQualityTypes.Add(ITEM_TYPE.AVATAR, E_CostumeItemQuality.Avatar);
        currentSelectQualityTypes.Add(ITEM_TYPE.ASPECT, E_CostumeItemQuality.Aspect);

        var animationNamesData = TableManager.Instance.CommonData.Find("EquipPreviewAnimationNames");
        for (int i = 0; i < animationNamesData.GetCount(); i++)
        {
            dummyAnimationNames.Add(animationNamesData.GetData(i).GetString());
        }

        initPreviewDic();
    }

    protected override void OnCreate()
    {
        setStatableSlotPool = new UIItemPool<UICostumeSlot>(E_UIChildType.SetStatableCostumeSlot, slotPos);
        nonSetStatableSlotPool = new UIItemPool<UICostumeSlot>(E_UIChildType.NonSetStatableCostumeSlot, slotPos);

        createActorDummy();
        initSelectToggle();
        initEquipSlots();

        statPage.Initialize(userCostume);
    }

    protected override void OnUIDestroy()
    {
        uiPreview.DestroyGameObject();
        uiPreview = null;

        actorDummy.DestroyGameObject();
        actorDummy = null;

        UIManager.Instance.ReleasePreviewWorld();

        Release();

        base.OnUIDestroy();
    }

    public void OnEventSelectToggle(Toggle toggle)
    {
        if (toggle.isOn)
        {
            var itemType = this.getToggle(toggle);

            selectItemType = itemType.TabType;

            selectSubToggle();
        }
    }
    public void OnEventSelectSubToggle(Toggle toggle)
    {
        if (toggle.isOn)
        {
            var subToggle = this.getSubToggle(toggle);

            currentSelectQualityTypes[selectItemType] = subToggle.qualityType;

            updateSlots();

            updateEquipSlots();

            setActorDummy();
        }
    }

    private void selectSubToggle()
    {
        ItemSubTabToggleGroup.allowSwitchOff = true;

        Toggle selectSubTab = null;
        foreach (var subTab in ItemSubTabs)
        {
            if (selectItemType != subTab.TabType)
            {
                subTab.Tab.SetActive(false);
            }
            else
            {
                subTab.Tab.SetActive(true);

                if (getCurrentSelectQualityType(selectItemType) == subTab.qualityType)
                {
                    selectSubTab = subTab.Tab;
                }
            }

            subTab.Tab.isOn = false;
        }

        if (null != selectSubTab)
        {
            selectSubTab.isOn = true;
        }

        ItemSubTabToggleGroup.allowSwitchOff = false;
    }

    private void initEquipSlots()
    {
        for (int i = 0; i < equipSlots.Length; i++)
        {
            var equipSlot = equipSlots[i];
            for (int j = 0; j < UserCostume.CostumeEquipTypes.Length; j++)
            {
                var equipType = UserCostume.CostumeEquipTypes[j];
                equipSlot.Set(userCostume, equipType, getCurrentSelectQualityType(equipType));
            }
        }
    }

    private void initSelectToggle()
    {
        selectItemType = ITEM_TYPE.WEAPON;
        currentSelectQualityTypes[selectItemType] = userCostume.GetCurrentEquipQualityType(selectItemType);

        OnEventSelectToggle(ItemTabs[0].Tab);
    }

    private void clickSlotCallback(CostumeInfo info, UICostumeSlot slot)
    {
        setPreviewDic(info);
        setPreviewIndexDic(info);

        for (int i = 0; i < setableSlotList.Count; i++)
        {
            setableSlotList[i].SetPreviewState(false);
            setableSlotList[i].UpdateSlot();
        }

        for (int i = 0; i < nonSetableSlotList.Count; i++)
        {
            nonSetableSlotList[i].SetPreviewState(false);
            nonSetableSlotList[i].UpdateSlot();
        }

        bool isEquipThisCostume = userCostume.IsEquip(info.GetData().qualityType, out int equipCostumeIndex) && equipCostumeIndex == info.GetIndex();
        if (isEquipThisCostume == false)
        {
            slot.SetPreviewState(true);
        }

        setActorDummy();
    }

    private void clickEquipButtonCallback(CostumeInfo info, UICostumeSlot slot)
    {
        userCostume.Equip(info.GetData().index);

        for (int i = 0; i < setableSlotList.Count; i++)
        {
            setableSlotList[i].UpdateSlot();
        }

        for (int i = 0; i < nonSetableSlotList.Count; i++)
        {
            nonSetableSlotList[i].UpdateSlot();
        }

        updateEquipSlots();
        updateSlots();

        slot.SetPreviewState(false);
    }

    private void clickUnequipButtonCallback(CostumeInfo info, UICostumeSlot slot)
    {
        userCostume.UnEquip(info.GetData().index);

        for (int i = 0; i < setableSlotList.Count; i++)
        {
            setableSlotList[i].UpdateSlot();
        }

        for (int i = 0; i < nonSetableSlotList.Count; i++)
        {
            nonSetableSlotList[i].UpdateSlot();
        }

        updateEquipSlots();
        updateSlots();

        slot.SetPreviewState(true);
    }

    private void updateEquipSlots()
    {
        for (int i = 0; i < equipSlots.Length; i++)
        {
            var equipSlot = equipSlots[i];
            equipSlot.Set(userCostume, selectItemType, getCurrentSelectQualityType(selectItemType));
        }
    }

    private void updateSlots()
    {
        for (int i = 0; i < setableSlotList.Count; i++)
        {
            setableSlotList[i].SetPreviewState(false);
        }

        for (int i = 0; i < nonSetableSlotList.Count; i++)
        {
            nonSetableSlotList[i].SetPreviewState(false);
        }

        var currentSelectQualityType = getCurrentSelectQualityType(selectItemType);

        setStatableSlotPool.FreeCollection(setableSlotList);
        nonSetStatableSlotPool.FreeCollection(nonSetableSlotList);

        var infos = userCostume.GetInfos(selectItemType);
        foreach (var info in infos)
        {
            if (info.GetData().qualityType != currentSelectQualityType)
            {
                continue;
            }

            UICostumeSlot slot = null;
            if (info.IsSetable())
            {
                slot = setStatableSlotPool.Alloc();
                setableSlotList.Add(slot);
            }
            else
            {
                slot = nonSetStatableSlotPool.Alloc();
                nonSetableSlotList.Add(slot);
            }

            slot.Set(userCostume, info, clickSlotCallback, clickEquipButtonCallback, clickUnequipButtonCallback);
            var qualityType = getCurrentSelectQualityType(info.GetData().equipType);
            var previewCostumeIndex = getPreviewIndex(info.GetData().equipType, info.GetData().qualityType);

            if (previewCostumeIndex != null
             && qualityType == info.GetData().qualityType
             && previewCostumeIndex.Value == info.GetIndex())
            {
                if (userCostume.IsEquip(qualityType, out int equipCostumeIndex) && equipCostumeIndex == info.GetIndex())
                {
                    slot.SetPreviewState(false);
                }
                else
                {
                    slot.SetPreviewState(true);
                }
            }
            else
            {
                slot.SetPreviewState(false);
            }
        }
    }

    public void OnClick_OpenStatPage()
    {
        statPage.OpenPage();
    }

    public void OnClick_CloseStatPage()
    {
        statPage.ClosePage();
    }

    public void OnClick_ResetPreview()
    {
        for (int i = 0; i < setableSlotList.Count; i++)
        {
            setableSlotList[i].SetPreviewState(false);
        }

        for (int i = 0; i < nonSetableSlotList.Count; i++)
        {
            nonSetableSlotList[i].SetPreviewState(false);
        }

        initPreviewDic();
        setActorDummy();
    }

    private void Release()
    {
        UserGameData.Get().UnsubscribeObserver(E_UserMessageGroup.Costume, this);

        setStatableSlotPool.FreeCollection(setableSlotList);
        nonSetStatableSlotPool.FreeCollection(nonSetableSlotList);

        setStatableSlotPool.Release();
        nonSetStatableSlotPool.Release();

        statPage.Release();
    }

    public TabPackage getPackage(ITEM_TYPE itemType)
    {
        foreach (var toggle in ItemTabs)
        {
            if (itemType == toggle.TabType)
            {
                return toggle;
            }
        }
        return null;
    }
    public TabPackage getToggle(Toggle tabToggle)
    {
        foreach (var toggle in ItemTabs)
        {
            if (tabToggle == toggle.Tab)
            {
                return toggle;
            }
        }
        return null;
    }
    public TabSubToggle getSubToggle(Toggle tabToggle)
    {
        foreach (var toggle in ItemSubTabs)
        {
            if (tabToggle == toggle.Tab)
            {
                return toggle;
            }
        }
        return null;
    }

    private void setPreviewDic(CostumeInfo info)
    {
        var data = info.GetData();
        previewDic[data.equipType][data.qualityType] = info.Get3DPrefabName();
    }

    private void setPreviewIndexDic(CostumeInfo info)
    {
        var data = info.GetData();
        previewIndexDic[data.equipType][data.qualityType] = info.GetIndex();
    }

    private string getPreviewPrefab(ITEM_TYPE type, E_CostumeItemQuality qualityType)
    {
        return previewDic[type][qualityType];
    }

    private int? getPreviewIndex(ITEM_TYPE type, E_CostumeItemQuality qualityType)
    {
        return previewIndexDic[type][qualityType];
    }

    private void createActorDummy()
    {
        var currentSelectWeaponQualityType = getCurrentSelectQualityType(ITEM_TYPE.WEAPON);
        var currentSelectShieldQualityType = getCurrentSelectQualityType(ITEM_TYPE.SHIELD);
        var currentSelectAvatarQualityType = getCurrentSelectQualityType(ITEM_TYPE.AVATAR);
        var currentSelectAspectQualityType = getCurrentSelectQualityType(ITEM_TYPE.ASPECT);

        string weaponCostumePrefab = getPreviewPrefab(ITEM_TYPE.WEAPON, currentSelectWeaponQualityType);
        string shieldCostumePrefab = getPreviewPrefab(ITEM_TYPE.SHIELD, currentSelectShieldQualityType);
        string avatarCostumePrefab = getPreviewPrefab(ITEM_TYPE.AVATAR, currentSelectAvatarQualityType);
        string aspectCostumePrefab = getPreviewPrefab(ITEM_TYPE.ASPECT, currentSelectAspectQualityType);

        int currentSelectWeaponQualityIndex = userCostume.GetQualityIndex(ITEM_TYPE.WEAPON, currentSelectWeaponQualityType);

        actorDummy = CharacterManager.Instance.CreateCostumeInfoDummy(avatarCostumePrefab, dummyAnimationNames, actorDummyPos, actorDummyRot);
        actorDummy.ChangeShape(weaponCostumePrefab, shieldCostumePrefab, currentSelectWeaponQualityType, currentSelectWeaponQualityIndex);

        actorDummy.ChangeAspectShape(aspectCostumePrefab);

        if (uiPreview != null)
        {
            return;
        }

        var previewWorld = UIManager.Instance.CreatePreviewWorld(actorDummy, actorDummyPos, actorDummyRot);

        uiPreview = UIManager.Instance.CreateUIItem<UIPreview>(E_UIChildType.Preview, previewPos);
        uiPreview.Initialze(actorDummy.transform, previewWorld.GetObjectParentTransform(), previewWorld.ShowRenderTexture(), null, null, null);
    }

    private void setActorDummy()
    {
        var currentSelectWeaponQualityType = getCurrentSelectQualityType(ITEM_TYPE.WEAPON);
        var currentSelectShieldQualityType = getCurrentSelectQualityType(ITEM_TYPE.SHIELD);
        string weaponPrefab = getPreviewPrefab(ITEM_TYPE.WEAPON, currentSelectWeaponQualityType);
        string shieldPrefab = getPreviewPrefab(ITEM_TYPE.SHIELD, currentSelectShieldQualityType);
        int weaponQualityIndex = userCostume.GetQualityIndex(ITEM_TYPE.WEAPON, currentSelectWeaponQualityType);

        var currentSelectAvatarQualityType = getCurrentSelectQualityType(ITEM_TYPE.AVATAR);
        string avatarPrefab = getPreviewPrefab(ITEM_TYPE.AVATAR, currentSelectAvatarQualityType);

        var currentSelectAspectQualityType = getCurrentSelectQualityType(ITEM_TYPE.ASPECT);
        string aspectPrefab = getPreviewPrefab(ITEM_TYPE.ASPECT, currentSelectAspectQualityType);

        CharacterManager.Instance.SetCostumeInfoDummy(avatarPrefab, actorDummyPos, actorDummyRot);
        actorDummy.ChangeShape(weaponPrefab, shieldPrefab, currentSelectWeaponQualityType, weaponQualityIndex);
        actorDummy.ChangeAspectShape(aspectPrefab);
    }

    private void initPreviewDic()
    {
        string getDefaultPrefabName(ITEM_TYPE type, E_CostumeItemQuality costumeQualityType, out bool isCostumePreview, out int previewCostumeIndex)
        {
            isCostumePreview = false;
            previewCostumeIndex = -1;

            if (type == ITEM_TYPE.WEAPON)
            {
                if (userCostume.IsEquip(costumeQualityType, out int equipCostumeWeaponIndex))
                {
                    isCostumePreview = true;
                    previewCostumeIndex = equipCostumeWeaponIndex;

                    return userCostume.GetGrowthInfo(equipCostumeWeaponIndex).Get3DPrefabName();
                }
                else
                {
                    int equipWeaponIndex = UserGameData.Get().EquipWeaponItemid;
                    var equipInfoData = TableManager.Instance.EquipInfoData.Find(equipWeaponIndex);
                    int equipQuality = equipInfoData?.qualityIndex ?? 1;
                    int costumeQualityIndex = userCostume.GetQualityIndex(type, costumeQualityType);

                    if (equipWeaponIndex < 0 && costumeQualityIndex == equipQuality)
                    {
                        return TableManager.Instance.DefaultStatData[0].Prefab[0];
                    }

                    int qualityIndex = userCostume.GetQualityIndex(type, costumeQualityType);
                    if (qualityIndex != equipQuality)
                    {
                        return TableManager.Instance.CommonData.Find("DefaultCostume").GetData(qualityIndex - 1).GetString();
                    }
                    else
                    {
                        return equipInfoData.PrefabPath;
                    }
                }
            }
            else if (type == ITEM_TYPE.SHIELD)
            {
                int equipShieldIndex = UserGameData.Get().EquipSubWeaponItemid;
                var equipInfoData = TableManager.Instance.EquipInfoData.Find(equipShieldIndex);
                int equipQuality = equipInfoData?.qualityIndex ?? 1;
                var qualityType = userCostume.GetQualityType(type, equipQuality);
                if (userCostume.IsEquip(qualityType, out int equipCostumeShieldIndex))
                {
                    isCostumePreview = true;
                    previewCostumeIndex = equipCostumeShieldIndex;

                    return userCostume.GetGrowthInfo(equipCostumeShieldIndex).Get3DPrefabName();
                }
                else
                {
                    if (equipShieldIndex < 0)
                    {
                        return TableManager.Instance.DefaultStatData[0].Prefab[1];
                    }

                    return equipInfoData.PrefabPath;
                }
            }
            else if (type == ITEM_TYPE.AVATAR)
            {
                var userAvatar = UserGameData.Get().GetUserData<UserAvatar>();
                int equipAvatarIndex = userAvatar.GetEquipAvatarIndex();
                var equipAvatarData = TableManager.Instance.AvatarInfoData.Find(equipAvatarIndex);
                int equipQuality = 1;

                var qualityType = userCostume.GetQualityType(type, equipQuality);
                if (userCostume.IsEquip(qualityType, out int equipCostumeAvatarIndex))
                {
                    isCostumePreview = true;
                    previewCostumeIndex = equipCostumeAvatarIndex;

                    return userCostume.GetGrowthInfo(equipCostumeAvatarIndex).Get3DPrefabName();
                }
                else
                {
                    if (equipAvatarIndex <= 0)
                    {
                        return TableManager.Instance.DefaultStatData[0].Prefab[2];
                    }

                    return equipAvatarData.PrefabName;
                }
            }
            else if (type == ITEM_TYPE.ASPECT)
            {
                int equipQuality = 1;
                var qualityType = userCostume.GetQualityType(type, equipQuality);
                if (userCostume.IsEquip(qualityType, out int equipCostumeAspectIndex))
                {
                    isCostumePreview = true;
                    previewCostumeIndex = equipCostumeAspectIndex;

                    return userCostume.GetGrowthInfo(equipCostumeAspectIndex).Get3DPrefabName();
                }
                else
                {
                    return string.Empty;
                }
            }

            return string.Empty;
        }

        for (int i = 0; i < UserCostume.CostumeEquipTypes.Length; i++)
        {
            var type = UserCostume.CostumeEquipTypes[i];
            if (previewDic.TryGetValue(type, out EquipPrefabNames prefabNames) == false)
            {
                prefabNames = new EquipPrefabNames();
                previewDic.Add(type, prefabNames);
            }

            if (previewIndexDic.TryGetValue(type, out EquipPreviewIndexer previewIndexer) == false)
            {
                previewIndexer = new EquipPreviewIndexer();
                previewIndexDic.Add(type, previewIndexer);
            }

            var qualityTypes = userCostume.GetQualityTypes(type);
            foreach (var qualityType in qualityTypes)
            {
                string prefab = getDefaultPrefabName(type, qualityType, out bool isCostumePreview, out int previewCostumeIndex);
                if (previewDic[type].ContainsKey(qualityType) == false)
                {
                    previewDic[type].Add(qualityType, prefab);
                }
                else
                {
                    previewDic[type][qualityType] = prefab;
                }

                int? costumeIndex = null;
                if (isCostumePreview)
                {
                    costumeIndex = previewCostumeIndex;
                }

                if (previewIndexDic[type].ContainsKey(qualityType) == false)
                {
                    previewIndexDic[type].Add(qualityType, costumeIndex);
                }
                else
                {
                    previewIndexDic[type][qualityType] = costumeIndex;
                }
            }
        }
    }

    private E_CostumeItemQuality getCurrentSelectQualityType(ITEM_TYPE equipType)
    {
        return currentSelectQualityTypes[equipType];
    }

    public void OnEvent(UserGameData owner, UserParameter param)
    {

    }
}
