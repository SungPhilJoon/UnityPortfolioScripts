// Strategy 패턴: 레드닷(알림 뱃지)의 활성화 조건을 판단 로직과 UI 배치 로직으로 분리.
// UIRedDotActivator 는 이 타입을 통해서만 활성화 여부를 묻고, 조건이 어떻게 계산되는지는 몰라도 된다.
public abstract class UIRedDotChecker
{
    // 공통 선행 조건(콘텐츠 오픈 여부)을 먼저 검사한 뒤, 세부 조건은 하위 클래스에 위임한다.
    public bool IsActivate(UserGameData data, Parameter InParameter)
    {
        var openType = GetContentsOpenType(data, InParameter);
        if (openType != null)
        {
            if (UserGameData.Get().GetContentsOpenValue(openType.Value) == false)
            {
                return false;
            }
        }

        return IsOnActivate(data, InParameter);
    }

    public abstract bool IsOnActivate(UserGameData data, Parameter parameter);

    // 콘텐츠 오픈 여부와 연동해야 하는 체커만 override
    public virtual eContentsOpenType? GetContentsOpenType(UserGameData data, Parameter InParameter)
    {
        return null;
    }
}
