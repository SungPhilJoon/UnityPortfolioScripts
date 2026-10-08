using UnityEngine.UI;
using TMPro;

// 재화/아이템 데이터를 아이콘+수량 UI 두 요소에 채워주는 정적 바인더.
// 재화(펄/골드)는 UserGameData에서 직접 읽고, 그 외 아이템은 인벤토리(GetBagItem)에서 읽는다는
// 조회 방식 차이를 이 안에서 흡수해서, 호출하는 UI 쪽은 아이템 타입만 넘기면 된다.
//
// 실제 파일에는 이 외에도 등급별 색상/로컬라이즈 텍스트를 반환하는 메서드들이 섞여 있는데
// (바인더의 책임보다는 아이템 표시 정책에 가까워서) 포트폴리오에서는 바인딩 메서드만 남겼다.
public class UIItemBinder
{
    public static void BindUIFromUserData(ITEM_TYPE itemType, Image img, TMP_Text count)
    {
        var itemInfo = StaticItemInfo.Get(itemType);
        if (itemInfo == null)
        {
            Debugger.Error($"UIItemBinder error. invalid item type. {itemType}");

            return;
        }

        img.sprite = UIIconLoader.LoadItemSprite(itemType);

        if (itemType == ITEM_TYPE.PEARL)
        {
            count.SetUnitText(UserGameData.Get().GetPearl(), UnitTextType.Resource);
        }
        else if (itemType == ITEM_TYPE.GOLD)
        {
            count.SetUnitText(UserGameData.Get().GetGold(), UnitTextType.Resource);
        }
        else
        {
            long itemCount = UserGameData.Get().GetBagItem(itemInfo.Index)?.Count ?? 0;
            count.SetUnitText(itemCount, UnitTextType.Resource);
        }
    }

    public static void BindUIItem(GameAsset gameAsset, Image img, TMP_Text text)
    {
        img.sprite = UIIconLoader.LoadItemSprite(gameAsset.index);
        text.text = gameAsset.count.ToNumber();
    }
}
