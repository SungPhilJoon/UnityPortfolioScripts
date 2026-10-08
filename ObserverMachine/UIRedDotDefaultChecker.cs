// 가장 단순한 override 예시: 항상 비활성. 신규 타입 추가 시 우선 기본값으로 등록해두는 용도.
public class UIRedDotDefaultChecker : UIRedDotChecker
{
    public override bool IsOnActivate(UserGameData data, Parameter parameter)
    {
        return false;
    }
}
