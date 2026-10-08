// 실제 게임 데이터를 조회해서 판단하는 override 예시.
public class UIRedDotSkillPromotionChecker : UIRedDotChecker
{
    public override bool IsOnActivate(UserGameData data, Parameter parameter)
    {
        int skillIndex = parameter.GetValue<int>();
        var skillClass = data.GetSkillClass(skillIndex);

        if (skillClass == null)
        {
            return false;
        }

        return data.CheckSkillPromotion(skillClass);
    }
}
