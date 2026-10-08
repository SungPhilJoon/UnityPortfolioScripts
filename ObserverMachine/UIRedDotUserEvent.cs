// 하나의 리스너가 서로 다른 두 Observer(UIObserver, UserObserver)를 동시에 구독하는 예시.
// C#의 명시적 인터페이스 구현(Explicit Interface Implementation)을 이용해
// "이 OnEvent가 어느 이벤트 소스에서 왔는지"를 타입 레벨에서 명확히 구분한다.
//
// 실제 파일은 게임 내 이벤트 종류만큼(약 40여 개) case가 이어지는 매핑 테이블이라
// 포트폴리오용으로 구조를 보여줄 수 있는 대표 case만 남기고 생략했다.
public class UIRedDotUserEvent : UserObserver.IListener, UIObserver.IListener
{
    private UIRedDotActivator activator = null;

    public UIRedDotUserEvent(UIRedDotActivator InActivator)
    {
        activator = InActivator;
    }

    // UI 상태 변화(팝업 오픈/클로즈, 탭 전환)에 반응
    void UIObserver.IListener.OnEvent(UIManager owner, UIEventMessage param)
    {
        switch (param.eventType)
        {
            case E_UIEvent.ConfirmEquipSlot:
            case E_UIEvent.CloseEquip:
                {
                    activator.Update(E_RedDotType.Main_ItemGrowth);
                    activator.Update(E_RedDotType.Equip_New);
                    activator.Update(E_RedDotType.Equip_Weapon);
                }
                break;

            case E_UIEvent.ConfirmSkillSlot:
            case E_UIEvent.CloseSkill:
                {
                    activator.Update(E_RedDotType.Main_Skill);
                    activator.Update(E_RedDotType.Skill_New);
                }
                break;

            // ... 나머지 탭/슬롯 이벤트 케이스 생략
        }
    }

    // 유저 데이터 변화(재화, 레벨, 장비 등)에 반응
    void ObserverPattern<UserGameData, UserParameter>.IListener.OnEvent(UserGameData owner, UserParameter param)
    {
        switch (param.type)
        {
            // 단일 레드닷만 갱신하는 가장 단순한 케이스
            case E_UserMessage.Asset_ChangeGold:
                {
                    activator.Update(E_RedDotType.Main_HeroGrowth);
                    activator.Update(E_RedDotType.HeroGrowth_AbilityTab);
                }
                break;

            // 하나의 이벤트가 여러 화면(시즌패스/성장/도전)의 레드닷을 함께 갱신하는 케이스
            case E_UserMessage.Stage_Clear:
                {
                    activator.Update(E_RedDotType.Main_SeasonPass);
                    activator.Update(E_RedDotType.SeasonPass_Stage);
                    activator.Update(E_RedDotType.SeasonPass_Passes);

                    activator.Update(E_RedDotType.Main_HeroGrowth);
                    activator.Update(E_RedDotType.HeroGrowth_Promotion);
                    activator.Update(E_RedDotType.HeroGrowth_RelicUpgrade);

                    activator.Update(E_RedDotType.Main_Campaign);
                    activator.Update(E_RedDotType.Campaign_BonusReward);
                }
                break;

            // 파라미터를 그대로 넘겨 특정 슬롯(가챠 타입 등)만 갱신하는 케이스
            case E_UserMessage.Gacha_GetBonus:
                {
                    activator.Update(E_RedDotType.Main_Gacha);
                    activator.Update(E_RedDotType.Gacha_Tab, param.GetValue<E_GachaType>());
                    activator.Update(E_RedDotType.Gacha_Bonus, param.GetValue<E_GachaType>());
                }
                break;

            // 아이템 사용처럼 파라미터 안의 세부 타입을 한 번 더 분기해야 하는 케이스는
            // 별도 private 메서드로 위임
            case E_UserMessage.Bag_AddItem:
            case E_UserMessage.Bag_RemoveItem:
            case E_UserMessage.Bag_ChangeCount:
            case E_UserMessage.Bag_UseItem:
                {
                    processBag(param.GetValue<int>());
                }
                break;

            // ... 나머지 유저 데이터 이벤트(약 40여 종) 케이스 생략
        }
    }

    private void processBag(int itemIndex)
    {
        var itemInfoData = TableManager.Instance.ItemInfoData.Find(itemIndex);
        if (itemInfoData == null)
        {
            return;
        }

        switch (itemInfoData.itemType)
        {
            case ITEM_TYPE.REFINING_STONE:
                {
                    activator.Update(E_RedDotType.Main_ItemGrowth);
                    activator.Update(E_RedDotType.Equip_Weapon);
                }
                break;

            case ITEM_TYPE.KEY_RAID:
                {
                    activator.Update(E_RedDotType.Main_ChallengeAdventure);
                    activator.Update(E_RedDotType.Adventure_Dungeons);
                }
                break;

            // ... 나머지 아이템 타입 케이스 생략
        }
    }
}
