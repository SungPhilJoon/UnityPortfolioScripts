using System;
using System.Collections.Generic;
using UnityEngine;

// 주의: 값 순서를 바꾸면 기존에 저장된 인덱스 기반 참조가 깨진다.
[Flags]
public enum E_RedDotType
{
    None = 0,

    Main_Menu = 100,
    Main_Dungeon,
    Main_HeroGrowth,
    Main_Skill,
    Main_ItemGrowth,
    Main_Avatar,
    Main_Gacha,
    Main_SeasonPass,
    Main_Campaign,
    Main_Buff,
    Main_Pet,
    Main_ChallengeAdventure,

    HeroGrowth_SpecificTab = 500,
    HeroGrowth_RelicTab,
    HeroGrowth_BreakAbilityTab,
    HeroGrowth_AbilityTab,
    HeroGrowth_Promotion,
    HeroGrowth_RelicUpgrade,

    SeasonPass_LV = 900,
    SeasonPass_Stage,
    SeasonPass_Kill,
    SeasonPass_Passes,

    Gacha_EquipTab = 1400,
    Gacha_SkillTab,
    Gacha_AvatarTab,
    Gacha_SummonBtns,
    Gacha_AdsBtn,
    Gacha_Bonus,
    Gacha_Tab,

    Equip_Weapon = 1600,
    Equip_Shield,
    Equip_Accessary,
    Equip_New,

    Skill_New = 1200,
    Skill_Promotion,

    Adventure_Dungeons = 400,
    Campaign_BonusReward = 2100,

    // ... 실제로는 메뉴/메일박스/퀘스트/장비/펫 등을 포함해 100여 개 항목이 더 있다
}

public enum E_RedDotAnchor
{
    LeftTop,
    RightTop
}

public class UIRedDotActivator
{
    public static readonly int MAX_RedDot = EnumUtils.GetMax<E_RedDotType>();

    private Dictionary<E_RedDotType, UIRedDotObject> objects
        = new Dictionary<E_RedDotType, UIRedDotObject>();

    // UIObserver / UserObserver 를 동시에 구독하는 단일 리스너 (UIRedDotUserEvent 참고)
    private UIRedDotUserEvent redDotUserEvent;
    public UIRedDotActivator()
    {
        redDotUserEvent = new UIRedDotUserEvent(this);

        UserGameData.Get().SubscribeAllObservers(redDotUserEvent);
        UIManager.Instance.SubscribeObserver(redDotUserEvent);
    }

    public void Release()
    {
        UserGameData.Get().UnsubscribeAllObservers(redDotUserEvent);
        UIManager.Instance.UnSubscribeObserver(redDotUserEvent);

        foreach(var obj in objects)
        {
            obj.Value.ClearAllTrackObjects();
        }

        objects.Clear();
    }

    // Strategy 패턴: 타입별로 UIRedDotChecker 구현체를 등록해두면,
    // 이후 모든 활성화 판단은 이 체커 인스턴스에 위임된다 (UIRedDotChecker 참고).
    public void Setup(E_RedDotType InType, UIRedDotChecker InChecker)
    {
        objects.Add(InType, new UIRedDotObject(InChecker));
    }
    public void Setup<T>(E_RedDotType InType) where T : UIRedDotChecker, new()
    {
        Setup(InType, new T());
    }

    public void InstallRedDot(E_RedDotType InType, GameObject InTarget, E_RedDotAnchor InAnchor)
    {
        InstallRedDot(InType, InTarget, InAnchor, 0);
    }

    public void InstallRedDot<T>(E_RedDotType InType, GameObject InTarget, E_RedDotAnchor InAnchor, T InParameter, bool isNewObject = false) where T : struct
    {
        UIRedDotObject redDotObj;
        if (!objects.TryGetValue(InType, out redDotObj))
        {
            Debugger.Error($"UIRedDotActivator.InstallRedDot Error. not installed type. {InType}");
            return;
        }

        redDotObj.TrackObject(InTarget, InParameter, InAnchor, isNewObject);

        if (GTGameManager.IsEnteredGame())
        {
            redDotObj.Check(UserGameData.Get(), InParameter);
        }
    }

    public bool IsInstalledRedDot<T>(E_RedDotType InType, T InParameter) where T : struct
    {
        UIRedDotObject redDotObject;
        if (objects.TryGetValue(InType, out redDotObject))
        {
            return redDotObject.HasRedDot(InParameter);
        }

        return false;
    }
    public void UnInstallAllRedDots(E_RedDotType InType)
    {
        UIRedDotObject redDotObject;
        if (objects.TryGetValue(InType, out redDotObject))
        {
            redDotObject.ClearAllTrackObjects();
        }
        else
        {
            Debugger.Error($"UnInstallAllRedDots Error. Invalid type. {InType}");
        }

    }
    public void UnInstallRedDot(E_RedDotType InType)
    {
        UIRedDotObject redDotObject;
        if (objects.TryGetValue(InType, out redDotObject))
        {
            redDotObject.ClearTrackObject(0);
        }
        else
        {
            Debugger.Error($"UnInstallRedDot Error. Invalid type. {InType}");
        }
    }
    public void UnInstallRedDot<T>(E_RedDotType InType, T InParameter) where T : struct
    {
        UIRedDotObject redDotObject;
        if (objects.TryGetValue(InType, out redDotObject))
        {
            redDotObject.ClearTrackObject<T>(InParameter);
        }
        else
        {
            Debugger.Error($"UnInstallRedDot<T> Error. Invalid type. {InType}");
        }
    }

    public void UpdateAll()
    {
        foreach (var obj in objects.Values)
        {
            obj.CheckAll(UserGameData.Get());
        }
    }
    public void Update(E_RedDotType InType)
    {
        UIRedDotObject redDotObj;

        if (!objects.TryGetValue(InType, out redDotObj))
        {
            return;
        }

        redDotObj.CheckAll(UserGameData.Get());
    }

    public void Update<T>(E_RedDotType InType, T InParameter) where T : struct
    {
        UIRedDotObject redDotObj;

        if (!objects.TryGetValue(InType, out redDotObj))
        {
            return;
        }

        redDotObj.Check(UserGameData.Get(), InParameter);
    }
}
