// View(UI)가 Model(UserGameData)이나 그 하위 도메인 모듈을 직접 참조하지 않도록
// 감싸는 정적 진입점. 지금은 자산 관련 파사드 하나만 노출하고 있지만,
// 새 도메인 파사드가 늘어나도 View 쪽 호출부(UserFacade.Xxx.Method())는 형태가 동일하다.
public static class UserFacade
{
    public static AssetFacade Asset { get; private set; } = new AssetFacade();
}
