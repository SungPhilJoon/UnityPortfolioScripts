// 퀘스트/시스템오픈/상품/핸드북/이벤트 등, 서로 완전히 다른 테이블 데이터가
// "조건 판정 대상"이라는 공통점 하나만으로 같은 인터페이스를 구현한다.
// 인터페이스가 5개 메서드로 작고 명확해서, 구현하는 쪽에 불필요한 부담을 주지 않는다.
public interface IConditionData
{
    E_ConditionType GetKey();
    int GetConditionValue();
    int GetGoalCount();
    E_ConditionTableType GetTableType();
    int GetTableKey();
}
