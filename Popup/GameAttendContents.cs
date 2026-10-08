// PopupCreator/PopupReserve 흐름의 실제 호출부. 게임 진입 시 "출석 이벤트를 아직 안 봤고,
// 출석 콘텐츠가 열려 있으면" 조건으로 PopupAttend를 예약한다. 실제 생성 시점은
// PopupReserve.Execute()가 매 프레임 조건을 재평가해서 결정하므로, 여기서는
// "무엇을, 어떤 조건으로" 예약할지만 선언하면 된다 (PopupCreator.cs 참고).
public class GameAttendContents : IGameContents, ITimeNotifier
{
    private bool isEnterGame = false;

    private UserAttend userAttend = null;

    public void ApplicationFocus(bool focus)
    {
        if (!focus && isEnterGame)
        {
            userAttend.CheckAttendTime();
        }
    }

    public void ApplicationPause(bool pause)
    {
        if (pause && isEnterGame)
        {
            userAttend.CheckAttendTime();
        }
    }

    public void ApplicationQuit()
    {
        if (isEnterGame)
        {
            userAttend.CheckAttendTime();
        }
    }

    public void EnterGame()
    {
        isEnterGame = true;

        userAttend = UserGameData.Get().GetUserData<UserAttend>();

        bool isAttend = userAttend.IsAttend();

        if (!isAttend && UserGameData.Get().GetContentsOpenValue(eContentsOpenType.EVENTATT))
        {
            PopupManager.Instance.ReservePopup<PopupAttend>(E_PopupCreateCondition.Campaign | E_PopupCreateCondition.CleanPopup);
        }
    }

    public void Initialize()
    {
    }

    public void OnTime(UUID uid, Parameter parameter, ITimeManager timeManager)
    {
        if (uid == TimeStaticID.DailyReset)
        {
            userAttend.CheckAttendTime();
        }
    }

    public void UnInitialize()
    {
    }

    void IGameContents.Update(float deltaTime)
    {
    }
}
