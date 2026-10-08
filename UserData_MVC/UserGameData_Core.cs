using System;
using System.Collections.Generic;

// UserGameData는 유저의 전체 진행 상태(Model)를 대표하는 싱글톤이지만, 필드를 전부
// 한 클래스에 두지 않고 도메인별 하위 데이터(BaseUserData 구현체)로 쪼갠 뒤 타입으로
// 등록/조회하는 레지스트리 역할만 한다. 실제 파일은 수백 줄짜리 partial 클래스라
// 여기서는 그 등록/조회/이벤트 브로드캐스트 핵심만 발췌했다.
public partial class UserGameData
{
    private static UserGameData THIS = null;
    public UserGameData()
    {
        THIS = this;
    }
    public static UserGameData Get()
    {
        return THIS;
    }

    // 타입을 키로 하위 데이터 모듈을 보관 — 도메인 모듈이 늘어나도 이 딕셔너리 하나로 충분하다.
    private Dictionary<Type, BaseUserData> userDatas = new Dictionary<Type, BaseUserData>();
    private List<ISynchronizeUserData> syncUserDatas = new List<ISynchronizeUserData>();
    private List<IUserStatCollector> userStats = new List<IUserStatCollector>();

    private UserObservers Observer = new UserObservers();
    private ObjectPool<UserParameter> parameterPool = new ObjectPool<UserParameter>();

    // 등록 시점에 리플렉션으로 "이 모듈이 동기화 대상인지 / 스탯 집계 대상인지"를 자동 판별해서
    // 별도 리스트에도 함께 담아둔다. 호출부에서 매번 캐스팅해서 분류할 필요가 없다.
    public void AddUserData<T>(DateTime currentTime) where T : BaseUserData, new()
    {
        var baseUserData = new T();
        baseUserData.SetOwnerData(this);

        userDatas.Add(typeof(T), baseUserData);

        if (typeof(IUserStatCollector).IsAssignableFrom(typeof(T)))
        {
            userStats.Add(baseUserData as IUserStatCollector);
        }

        if (typeof(ISynchronizeUserData).IsAssignableFrom(typeof(T)))
        {
            syncUserDatas.Add(baseUserData as ISynchronizeUserData);
        }
    }

    public T GetUserData<T>() where T : BaseUserData
    {
        BaseUserData result;

        userDatas.TryGetValue(typeof(T), out result);

        return (T)result;
    }

    // 등록이 전부 끝난 뒤(UserManager_Lifecycle.cs의 AddUserData<T> 호출들이 끝난 뒤) 한 번만 호출된다.
    // 등록 순서와 시작 순서를 분리해서, 모듈 A의 Start()가 아직 등록 안 된 모듈 B를 참조하는
    // 실수를 구조적으로 막는다 — 이 시점에는 모든 모듈이 이미 딕셔너리에 들어있는 게 보장된다.
    public void Initialize(DateTime currentTime)
    {
        Observer.SetSender(this);

        foreach (var userData in userDatas.Values)
        {
            userData.Start();

            ISynchronizeUserData syncData = userData as ISynchronizeUserData;
            if (syncData == null)
            {
                continue;
            }

            syncData.OnExtract(SyncData, currentTime);
        }
    }

    // 등록된 순서와 무관하게 모든 모듈의 End()를 호출하고 레지스트리를 비운다.
    // 개별 모듈이 스스로를 해제하는 게 아니라, 레지스트리를 쥐고 있는 쪽이 일괄 종료를 책임진다.
    public void UnInitialize()
    {
        foreach (var userData in userDatas)
        {
            userData.Value.End();
        }

        userDatas.Clear();
    }

    public void SubscribeObserver(E_UserMessageGroup group, UserObserver.IListener listener)
    {
        Observer.Subscribe(group, listener);
    }
    public void SubscribeAllObservers(UserObserver.IListener listener)
    {
        Observer.SubscribeAll(listener);
    }
    public void UnsubscribeAllObservers(UserObserver.IListener listener)
    {
        Observer.UnsubscribeAll(listener);
    }

    // 이벤트 하나로 두 종류의 구독자에게 동시에 전파한다: 내가 직접 등록한 하위 데이터 모듈들
    // (OnEvent를 override한 것들, 옵저버 구독 없이 자동으로 받음)과, 외부에서 Observer로
    // 명시적으로 구독한 리스너들(UI 쪽 UIRedDotUserEvent 등, ObserverMachine 폴더 참고).
    public void SendEvent<T>(E_UserMessage message, T param) where T : struct
    {
        var parameter = parameterPool.Alloc();
        parameter.SetValue(message, param);

        foreach (var userData in userDatas)
        {
            userData.Value.OnEvent(this, parameter);
        }

        Observer.Send(parameter);

        parameterPool.Free(parameter);
    }
}
