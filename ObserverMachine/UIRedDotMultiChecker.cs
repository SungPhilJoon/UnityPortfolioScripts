using System.Collections.Generic;

// Composite 패턴: 여러 개의 UIRedDotChecker 를 하나의 UIRedDotChecker 처럼 조합해서
// "하위 조건 중 하나라도 만족하면 활성화" 같은 규칙을 재귀적으로 구성할 수 있게 한다.
public class UIRedDotMultiChecker : UIRedDotChecker
{
    protected List<UIRedDotChecker> checkers = new List<UIRedDotChecker>();

    public UIRedDotMultiChecker(params UIRedDotChecker[] InCheckers)
    {
        checkers.AddRange(InCheckers);
    }

    public override bool IsOnActivate(UserGameData data, Parameter parameter)
    {
        foreach (var checker in checkers)
        {
            if (checker.IsActivate(data, parameter))
            {
                return true;
            }
        }

        return false;
    }
}
