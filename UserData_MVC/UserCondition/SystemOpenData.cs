// 가장 단순한 구현 예시: 테이블 데이터 클래스 하나가 두 인터페이스(ITable, IConditionData)를
// 명시적 인터페이스 구현으로 각각 만족시킨다. 필드는 그대로 두고, 인터페이스로 노출되는
// 이름/의미만 변환해주는 얇은 어댑터 역할.
public class SystemOpenData : ITable<eContentsOpenType>, IConditionData
{
    public int index;
    public eContentsOpenType contentsType;
    public E_ConditionType contentOpenCon;
    public int contentOpenValue;
    public int contentOpenCount;

    int IConditionData.GetConditionValue()
    {
        return contentOpenValue;
    }

    int IConditionData.GetGoalCount()
    {
        return contentOpenCount;
    }

    E_ConditionType IConditionData.GetKey()
    {
        return contentOpenCon;
    }

    int IConditionData.GetTableKey()
    {
        return (int)contentsType;
    }

    E_ConditionTableType IConditionData.GetTableType()
    {
        return E_ConditionTableType.SystemOpen;
    }

    eContentsOpenType ITable<eContentsOpenType>.getKey()
    {
        return contentsType;
    }
}
