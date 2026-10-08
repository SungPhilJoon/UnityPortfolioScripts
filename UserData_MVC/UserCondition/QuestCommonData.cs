// 하나의 데이터 클래스가 세 인터페이스(ITable, IConditionData, IQuestTable)를 동시에 구현하는
// 예시. 각 인터페이스는 서로 다른 소비자를 위한 것이다:
//   - ITable<int>        : 테이블 매니저가 인덱스로 조회할 때 쓰는 최소 계약
//   - IConditionData      : UserCondition 모듈이 진행도를 판정할 때 쓰는 계약 (UserCondition.cs 참고)
//   - IQuestTable         : 퀘스트 UI/진행 로직이 보상·이벤트 종류를 물을 때 쓰는 계약
// 명시적 인터페이스 구현을 쓰기 때문에 세 인터페이스의 메서드 이름이 겹쳐도 충돌하지 않고,
// 각 소비자는 자신이 아는 인터페이스로만 캐스팅해서 필요한 것만 볼 수 있다.
public interface IQuestTable
{
    int GetIndex();
    int GetGoalCount();
    GameAsset GetReward();
    eQuestCategoryType GetCategory();
    E_UserMessage? GetCountEvent();
    E_UserMessage? GetCompleteEvent();
    E_UserMessage? GetClearEvent();
    E_ConditionType? GetRewardCondition();
    eContentShortcutType GetShortcutType();
}

public class QuestCommonData : ITable<int>, IConditionData, IQuestTable
{
    public int index;
    public eQuestCategoryType questCategory;
    public E_ConditionType questType;
    public int questGoal;
    public GameAsset rewardAsset;
    public eContentShortcutType contentShortcuts;

    int IConditionData.GetConditionValue()
    {
        return -1;
    }

    int IConditionData.GetGoalCount()
    {
        return questGoal;
    }

    E_ConditionType IConditionData.GetKey()
    {
        return questType;
    }

    E_ConditionTableType IConditionData.GetTableType()
    {
        return E_ConditionTableType.Quest;
    }

    int IConditionData.GetTableKey()
    {
        return index;
    }

    int ITable<int>.getKey()
    {
        return index;
    }

    int IQuestTable.GetIndex()
    {
        return index;
    }

    int IQuestTable.GetGoalCount()
    {
        return questGoal;
    }

    GameAsset IQuestTable.GetReward()
    {
        return rewardAsset;
    }

    eQuestCategoryType IQuestTable.GetCategory()
    {
        return questCategory;
    }

    E_UserMessage? IQuestTable.GetCountEvent()
    {
        if (questCategory == eQuestCategoryType.DAILY)
        {
            return E_UserMessage.Quest_DailyCount;
        }
        else if (questCategory == eQuestCategoryType.WEEKLY)
        {
            return E_UserMessage.Quest_WeeklyCount;
        }
        else if (questCategory == eQuestCategoryType.REPEAT)
        {
            return E_UserMessage.Quest_RepeatCount;
        }

        return null;
    }

    E_UserMessage? IQuestTable.GetCompleteEvent()
    {
        if (questCategory == eQuestCategoryType.DAILY)
        {
            return E_UserMessage.Quest_DailyComplete;
        }
        else if (questCategory == eQuestCategoryType.WEEKLY)
        {
            return E_UserMessage.Quest_WeeklyComplete;
        }
        else if (questCategory == eQuestCategoryType.REPEAT)
        {
            return E_UserMessage.Quest_RepeatComplete;
        }

        return null;
    }

    E_UserMessage? IQuestTable.GetClearEvent()
    {
        if (questCategory == eQuestCategoryType.DAILY)
        {
            return E_UserMessage.Quest_DailyGetItem;
        }
        else if (questCategory == eQuestCategoryType.WEEKLY)
        {
            return E_UserMessage.Quest_WeeklyGetItem;
        }
        else if (questCategory == eQuestCategoryType.REPEAT)
        {
            return E_UserMessage.Quest_RepeatGetItem;
        }

        return null;
    }

    E_ConditionType? IQuestTable.GetRewardCondition()
    {
        if (questCategory == eQuestCategoryType.DAILY)
        {
            return E_ConditionType.DailyQuest_GetReward;
        }
        else if (questCategory == eQuestCategoryType.WEEKLY)
        {
            return E_ConditionType.WeeklyQuest_GetReward;
        }
        else if (questCategory == eQuestCategoryType.REPEAT)
        {
            return E_ConditionType.RepeatQuest_GetReward;
        }

        return null;
    }

    eContentShortcutType IQuestTable.GetShortcutType()
    {
        return contentShortcuts;
    }
}
