using System.Collections.Generic;

// 유저 데이터 변화(재화, 레벨, 장비, 퀘스트 등)를 브로드캐스트하는 옵저버 채널.
// E_UserMessageGroup으로 먼저 그룹을 나눠서, 리스너가 관심 없는 그룹의 이벤트까지
// 전부 받아서 스위치문으로 걸러내지 않고 필요한 그룹만 구독할 수 있게 했다.
public static class UserMessageMask
{
    public const int Value = 8;
}

public enum E_UserMessageGroup : long
{
    User = 1 << 1,
    Character = 1 << 2,
    Bag = 1 << 3,
    Avatar = 1 << 4,
    Equip = 1 << 5,
    Skill = 1 << 6,
    Quest = 1 << 8,
    Stage = 1 << 9,
    Asset = 1 << 11,
    GoodsBuy = 1 << 12,
    Mail = 1 << 13,
    Gacha = 1 << 17,
    Pet = 1 << 22,

    // ... 나머지 그룹(Attend/Event/SeasonPass/Buff/Rank/Costume/Offline/Handbook 등) 생략
}

public enum E_UserMessage : long
{
    None = 0,

    Character_LevelChange = E_UserMessageGroup.Character << UserMessageMask.Value,
    Character_NormalMobKill,

    Bag_AddItem = E_UserMessageGroup.Bag << UserMessageMask.Value,
    Bag_UseItem,

    Asset_ChangeGold = E_UserMessageGroup.Asset << UserMessageMask.Value,
    Asset_ChangePearl,

    Gacha_Play = E_UserMessageGroup.Gacha << UserMessageMask.Value,
    Gacha_GetBonus,

    Stage_Clear = E_UserMessageGroup.Stage << UserMessageMask.Value,

    // ... 실제로는 그룹당 여러 개씩, 총 100여 개의 세부 이벤트가 있다
}

// UserParameter는 MemoryMarshalingParameter 폴더의 Parameter를 감싸서
// "이번 이벤트가 무엇이었는지(type)"와 "값(SetValue/GetValue)"을 함께 들고 다니는 이벤트 페이로드.
public class UserParameter : IPoolObject
{
    public E_UserMessage type = E_UserMessage.None;
    private Parameter parameter = new Parameter(128);

    public static E_UserMessageGroup ToGroup(E_UserMessage message)
    {
        return (E_UserMessageGroup)((long)message >> UserMessageMask.Value);
    }

    public void SetValue<T>(E_UserMessage message, T val) where T : struct
    {
        this.type = message;

        parameter.SetValue(val);
    }
    public E_UserMessageGroup GetGroup()
    {
        return ToGroup(type);
    }
    public T GetValue<T>() where T : struct
    {
        return parameter.GetValue<T>();
    }

    void IPoolObject.OnFirstCreate() { }
    void IPoolObject.OnLastRelease() { }
    void IPoolObject.OnAlloc() { }
    void IPoolObject.OnFree() { }
}

public class UserObserver : ObserverPattern<UserGameData, UserParameter> { }

// 그룹별로 ObserverPattern 인스턴스를 하나씩 두는 라우팅 테이블.
// Send()가 들어오면 메시지 타입에서 그룹을 역산해서 해당 그룹의 구독자에게만 전파한다.
public class UserObservers
{
    private Dictionary<E_UserMessageGroup, UserObserver> observers
        = new Dictionary<E_UserMessageGroup, UserObserver>();

    public UserObservers()
    {
        observers.FillEnumKey();
    }

    public void SetSender(UserGameData sender)
    {
        foreach(var observer in observers.Values)
        {
            observer.SetSender(sender);
        }
    }
    public void Subscribe(E_UserMessageGroup group, UserObserver.IListener listener)
    {
        observers[group].Subscribe(listener);
    }
    public void SubscribeAll(UserObserver.IListener listener)
    {
        foreach (var observer in observers)
        {
            observer.Value.Subscribe(listener);
        }
    }

    public void Unsubscribe(E_UserMessageGroup group, UserObserver.IListener listener)
    {
        observers[group].Unsubscribe(listener);
    }
    public void UnsubscribeAll(UserObserver.IListener listener)
    {
        foreach (var observer in observers)
        {
            observer.Value.Unsubscribe(listener);
        }
    }

    public void Send(UserParameter param)
    {
        var group = UserParameter.ToGroup(param.type);

        observers[group].Send(param);
    }
}
