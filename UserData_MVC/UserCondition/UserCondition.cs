using System;

// UserGameData(Model)의 하위 데이터 모듈 중 하나. "유저 진행 조건이 지금 만족되는지"를
// 판정하는 책임을 가진다. BaseUserData를 상속하기 때문에 다른 모듈들과 동일한 방식으로
// UserManager에서 등록되고(UserManager_Lifecycle.cs 참고), UserGameData.GetUserData<UserCondition>()로
// 조회된다.
//
// IConditionData 구현체들을 테이블 타입(E_ConditionTableType)별로 분기해서
// 실제 유저 데이터(UserQuest, UserGameEvent, UserGoodsBuy...)에 반영하는 디스패처.
// IConditionData 쪽에 로직을 두지 않고 여기서 타입별로 라우팅하는 이유는,
// "조건을 만족했을 때 어떤 유저 데이터를 어떻게 갱신할지"가 테이블 데이터의 책임이 아니라
// 유저 진행 상태를 관리하는 이 모듈의 책임이기 때문이다.
//
// 실제 파일은 GetAccumulateCount() 안에 게임 콘텐츠 종류만큼(약 80여 개) case가 있는
// 대형 스위치인데, 포트폴리오용으로 구조가 드러나는 대표 케이스만 남기고 생략했다.
public class UserCondition : BaseUserData
{
    protected override void OnStart()
    {
    }

    protected override void OnEnd()
    {
    }

    protected override void OnResetData()
    {
    }

    // 게임 진입 시 모든 조건 타입을 한 번씩 훑어서, 이미 누적된 값이 있으면 보상/카운트를 맞춰준다.
    public void CheckAllAccumulateCount()
    {
        var values = Enum.GetValues(typeof(E_ConditionType)) as E_ConditionType[];
        for (int i = 0; i < values.Length; i++)
        {
            E_ConditionType conditionType = values[i];
            var conditionData = TableManager.Instance.ConditionDatas.Find(conditionType);

            for (int j = 0; j < conditionData.conditionDataList.Count; j++)
            {
                var tableData = conditionData.conditionDataList[j];
                CheckAccumulateCount(tableData);
            }
        }
    }

    // 조건 데이터 하나에 대해 "지금 카운트가 얼마나 쌓였는지"를 구해서 콜백으로 흘려보낸다.
    public void CheckAccumulateCount(IConditionData conditionData)
    {
        E_ConditionType conditionType = conditionData.GetKey();
        int conditionValue = conditionData.GetConditionValue();
        int count = GetAccumulateCount(conditionType, conditionValue);
        if (count <= 0)
        {
            return;
        }

        invokeAccumulateCallback(conditionData, count);
    }

    // 특정 조건 타입에 이벤트가 발생했을 때(아이템 사용, 장비 강화 등) 카운트를 올린다.
    public void CheckCondition(E_ConditionType type, int checkValue = -1, int count = 1)
    {
        var conditionData = TableManager.Instance.ConditionDatas.Find(type);
        var conditionList = conditionData.conditionDataList;

        for (int i = 0; i < conditionList.Count; i++)
        {
            if (!canCount(conditionData, checkValue, conditionList[i].GetConditionValue()))
            {
                continue;
            }

            this.addCountCallback(conditionList[i], checkValue, count);
        }
    }

    public void ResetCondition(E_ConditionType type)
    {
        var conditionData = TableManager.Instance.ConditionDatas.Find(type);
        var conditionList = conditionData.conditionDataList;

        for (int i = 0; i < conditionList.Count; i++)
        {
            invokeResetCallback(conditionList[i], conditionList[i].GetTableKey());
        }
    }

    // GetTableType() 하나로 서로 다른 유저 데이터 저장소(UserQuest/UserGameEvent/...)에 라우팅.
    // IConditionData 쪽은 자신이 어느 저장소로 가는지 전혀 몰라도 된다.
    private void addCountCallback(IConditionData conditionData, int checkValue, int count)
    {
        int tableKey = conditionData.GetTableKey();
        switch (conditionData.GetTableType())
        {
            case E_ConditionTableType.Guide:
                {
                    var userQuest = ownerData.GetUserData<UserQuest>();
                    var questData = conditionData as QuestGuideData;
                    var guideInfo = userQuest.GetGuideInfo();
                    if (guideInfo == null || guideInfo.GetData().GetIndex() != questData.index)
                    {
                        return;
                    }

                    userQuest.AddCount(questData, count);
                }
                break;

            case E_ConditionTableType.Quest:
                {
                    ownerData.GetUserData<UserQuest>().AddCount((conditionData as QuestCommonData), count);
                }
                break;

            case E_ConditionTableType.GameEvent:
                {
                    ownerData.GetUserData<UserGameEvent>().AddGameEventCount(tableKey, checkValue, count);
                }
                break;

            case E_ConditionTableType.SystemOpen:
                {
                    ownerData.AddOpenSystemConditionCount(tableKey, checkValue, count);
                }
                break;

            // ... Goods / HandBook 케이스 생략 (패턴은 동일)
        }
    }

    private void invokeAccumulateCallback(IConditionData conditionData, int count)
    {
        switch (conditionData.GetTableType())
        {
            case E_ConditionTableType.Guide:
                {
                    ownerData.GetUserData<UserQuest>().SetQuestAccumulateCount((conditionData as QuestGuideData), count);
                }
                break;

            case E_ConditionTableType.GameEvent:
                {
                    ownerData.GetUserData<UserGameEvent>().SetAccumulate(conditionData.GetTableKey(), conditionData.GetConditionValue(), count);
                }
                break;

            // ... SystemOpen / Goods / HandBook 케이스 생략 (패턴은 동일)
        }
    }

    private void invokeResetCallback(IConditionData conditionData, int index)
    {
        switch (conditionData.GetTableType())
        {
            case E_ConditionTableType.Guide:
                {
                    ownerData.GetUserData<UserQuest>().ResetGuide(index);
                }
                break;

            case E_ConditionTableType.GameEvent:
                {
                    ownerData.GetUserData<UserGameEvent>().ResetGameEventCount(index);
                }
                break;
        }
    }

    private bool canCount(ConditionData conditionData, int checkValue, int conditionValue)
    {
        if (conditionData.operatorType == E_ConditionOperator.Over)
        {
            return checkValue >= conditionValue;
        }
        else if (conditionData.operatorType == E_ConditionOperator.Equal)
        {
            return checkValue == conditionValue;
        }

        return true;
    }

    // 조건 타입마다 "현재 값이 얼마인지"를 구하는 방법이 완전히 다르기 때문에(재화 보유량,
    // 던전 클리어 층수, 장비 등급 카운트...) 타입별 조회 로직을 한 곳에 모아둔 조회 테이블.
    // 아래는 성격이 다른 세 가지 유형을 대표로 남겨뒀다: 단순 bool 판정 / 컬렉션 순회 집계 / 위임 조회.
    public int GetAccumulateCount(E_ConditionType type, int checkValue)
    {
        int count = 0;

        switch (type)
        {
            case E_ConditionType.None:
                {
                    return 1;
                }

            // 위임 조회: 다른 서브시스템(UserGacha)에 그대로 물어보는 가장 단순한 형태
            case E_ConditionType.Gacha_Weapon:
                {
                    return ownerData.GetUserData<UserGacha>().GetGachaSet(E_GachaType.Weapon).GetGachaCount();
                }

            // 단순 bool 판정: 임계값 이상인지만 확인해서 1/0으로 변환
            case E_ConditionType.Stage_Clear:
                {
                    return ownerData.SyncData.ClearStage.Value >= checkValue ? 1 : 0;
                }

            // 컬렉션 순회 집계: 보유 장비 중 조건을 만족하는 개수를 센다
            case E_ConditionType.Weapon_Acquire_Grade:
                {
                    var equipItems = ownerData.GetEquipItems();
                    foreach (var equipItem in equipItems)
                    {
                        var equipInfo = equipItem.GetInfoData();
                        if (equipInfo.itemType == ITEM_TYPE.WEAPON && (int)equipInfo.Grade >= checkValue)
                        {
                            count++;
                        }
                    }

                    return count;
                }

            // ... 나머지 콘텐츠별 조회 케이스 생략, 패턴은 위 세 가지의 조합

            default:
                {
                    return 0;
                }
        }
    }
}
