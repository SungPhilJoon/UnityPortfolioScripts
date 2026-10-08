using System;

// UserManager(GTGameManager.Manager<T> 싱글톤)가 게임 진입 시 UserGameData(Model)를
// 어떻게 조립하고 시작/종료시키는지 보여주는 발췌. 실제 OnGameReady()에는 이 외에도
// 랭킹/우편함 서버 통신, 테이블 파싱 등 UserData 초기화와 무관한 코드가 섞여 있어서,
// 여기서는 "도메인 모듈을 등록하고 Initialize/UnInitialize로 생명주기를 여는/닫는" 부분만 남겼다.
public partial class UserManager
{
    private UserGameData userData = new UserGameData();
    private EventLogUserEvent eventLogUserEvent = new EventLogUserEvent();

    public override void OnGameReady()
    {
        var currentTime = TimeManager.Instance.GetDateTime();

        // 도메인 모듈을 하나씩 등록한다. 순서는 서로 의존하지 않으므로 자유롭게 늘어나도 된다 —
        // 실제 시작(Start)은 전부 등록이 끝난 뒤 Initialize()에서 일괄적으로 이뤄진다 (UserGameData_Core.cs 참고).
        userData.AddUserData<UserSystemOpen>(currentTime);
        userData.AddUserData<UserGrowStat>(currentTime);
        userData.AddUserData<UserQuest>(currentTime);
        userData.AddUserData<UserBag>(currentTime);
        userData.AddUserData<UserMail>(currentTime);
        userData.AddUserData<UserAvatar>(currentTime);
        userData.AddUserData<UserPet>(currentTime);
        userData.AddUserData<UserGacha>(currentTime);
        userData.AddUserData<UserDungeon>(currentTime);
        userData.AddUserData<UserGoodsBuy>(currentTime);
        userData.AddUserData<UserEquip>(currentTime);
        userData.AddUserData<UserSkill>(currentTime);
        userData.AddUserData<UserSeasonPass>(currentTime);
        userData.AddUserData<UserGameEvent>(currentTime);
        userData.AddUserData<UserHandsBook>(currentTime);
        userData.AddUserData<UserCondition>(currentTime);
        userData.AddUserData<UserAttend>(currentTime);
        // ... 실제로는 24개 도메인 모듈을 등록한다 (성장/강화/각성/의상 등 생략)

        userData.Initialize(currentTime);

        // 초기화가 끝난 뒤에야 조건 누적치를 한 번 훑고(UserCondition.CheckAllAccumulateCount),
        // 입장 조건을 체크한다 — 모듈이 전부 Start된 상태여야 서로를 안전하게 참조할 수 있다.
        userData.CheckAllAccumulateCount();
        userData.CheckCondition(E_ConditionType.Enter_Game);

        userData.SubscribeAllObservers(this.eventLogUserEvent);
    }

    public override void OnUnInitialize()
    {
        userData.UnsubscribeAllObservers(eventLogUserEvent);

        userData.UnInitialize();

        base.OnUnInitialize();
    }
}
