// UserGameData(Model)를 구성하는 하위 데이터 모듈(UserQuest, UserAvatar, UserGoodsBuy ...)이
// 공통으로 따라야 하는 생명주기 계약. Start/End는 항상 호출되고, OnResetData는
// 개발 빌드에서만 실행되도록 ResetData가 감싸고 있다 (Template Method).
public abstract class BaseUserData
{
    protected UserGameData ownerData;

    public void Start()
    {
        OnStart();
    }

    public void End()
    {
        OnEnd();
    }
    protected abstract void OnStart();
    protected abstract void OnEnd();
    protected abstract void OnResetData();

    public void SetOwnerData(UserGameData ownerData)
    {
        this.ownerData = ownerData;
    }

    // 옵저버를 직접 구독하지 않아도, UserGameData가 이벤트를 보낼 때마다
    // 등록된 모든 하위 데이터에 자동으로 전파해주는 훅 (필요한 모듈만 override)
    public virtual void OnEvent(UserGameData owner, UserParameter param)
    {
    }

    public void ResetData()
    {
#if DEV_BUILD
        OnResetData();
#endif
    }
}
