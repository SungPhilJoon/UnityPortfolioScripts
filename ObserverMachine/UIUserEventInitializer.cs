// 그룹 단위로 리스너를 등록하는 진입점. E_UserMessageGroup 하나당 관심있는 리스너 타입을
// 여기 한 줄씩 추가하면 되므로, 새 그룹의 UI 반응 로직을 추가할 때 이 파일 하나만 보면 된다.
public static class UIUserEventInitializer
{
    public static void Initialize()
    {
        UIManager.Instance.AddUserEventListener<UIUserEventGoodsBuyCondition>(E_UserMessageGroup.GoodsBuy);
        UIManager.Instance.AddUserEventListener<UIUserEventOpenHandbook>(E_UserMessageGroup.Handbook);
    }
}
