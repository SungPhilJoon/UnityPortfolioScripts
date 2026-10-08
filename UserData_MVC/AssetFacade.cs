using System.Collections.Generic;

// UserFacade.Asset의 실제 구현체. "재화/장비/스킬/아바타/펫 등 아이템 종류에 따라
// UserGameData의 어느 메서드를 불러야 하는지"를 이 클래스 하나가 알고 있고, 호출하는
// View 쪽(UI, UILogicBoxOpenCounter 등)은 ITEM_TYPE만 넘기면 된다 — Model의 세부
// 저장 구조(재화는 UserGameData 직접 필드, 장비는 ItemSystem, 스킬은 SkillSystem ...)를
// 몰라도 되게 만드는 Facade.
public class AssetFacade
{
    public void AddItem(GameAsset asset)
    {
        Increase(asset.type, asset.index, asset.count);
    }

    public void AddItem(IEnumerable<GameAsset> assets)
    {
        foreach (var asset in assets)
        {
            AddItem(asset);
        }
    }

    public bool CheckCount(ITEM_TYPE itemType, int itemIndex, long count, out long currentCount)
    {
        UserGameData data = UserGameData.Get();

        switch (itemType)
        {
            case ITEM_TYPE.GOLD:
                {
                    currentCount = data.GetGold();
                }
                break;
            case ITEM_TYPE.PEARL:
                {
                    currentCount = data.GetPearl();
                }
                break;
            case ITEM_TYPE.WEAPON:
            case ITEM_TYPE.SHIELD:
            case ITEM_TYPE.ACCESSARY:
                {
                    EnableItem useritem = UserGameData.Get().GetEquipItem(itemIndex);
                    currentCount = useritem?.num ?? 0;
                }
                break;
            default:
                {
                    var item = data.GetBagItem(itemIndex);

                    currentCount = item?.Count ?? 0;
                }
                break;
        }

        return currentCount >= count;
    }

    public void Increase(ITEM_TYPE itemType, int index, long count)
    {
        switch (itemType)
        {
            case ITEM_TYPE.WEAPON:
            case ITEM_TYPE.SHIELD:
            case ITEM_TYPE.ACCESSARY:
                {
                    ItemSystem.AddEquipItem(index, count);
                }
                break;
            case ITEM_TYPE.SKILL:
                {
                    SkillInfoData _skillData = SkillManager.Instance.GetSkillData(index);
                    SkillSystem.AddSkillCard(_skillData.Index, count);
                }
                break;
            case ITEM_TYPE.GOLD:
                {
                    UserGameData.Get().IncreaseGold(count);
                }
                break;
            case ITEM_TYPE.PEARL:
                {
                    UserGameData.Get().AddPearl(count);
                }
                break;
            default:
                {
                    bool hasItem = UserGameData.Get().IncreaseBagItem(index, (int)count);

                    if (!hasItem)
                    {
                        UserGameData.Get().AddBag(index, (int)count);
                    }
                }
                break;
        }
    }

    public bool Decrease(ITEM_TYPE itemType, int index, long count)
    {
        if (!CheckCount(itemType, index, count, out _))
        {
            return false;
        }

        switch (itemType)
        {
            case ITEM_TYPE.WEAPON:
            case ITEM_TYPE.SHIELD:
            case ITEM_TYPE.ACCESSARY:
                {
                    ItemSystem.AddEquipItem(index, -count);
                }
                break;
            case ITEM_TYPE.GOLD:
                {
                    UserGameData.Get().DecreaseGold(count);
                }
                break;
            case ITEM_TYPE.PEARL:
                {
                    UserGameData.Get().DecreasePearl(count);
                }
                break;
            default:
                {
                    UserGameData.Get().DecreaseBagItem(index, count);
                }
                break;
        }
        return true;
    }

    // ... 실제로는 AVATAR/PET/Exp/COSTUME 등 ITEM_TYPE별 케이스가 더 있다 (패턴은 동일)
}
