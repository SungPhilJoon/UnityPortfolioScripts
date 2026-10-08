// UI 상태 변화(팝업 오픈/클로즈, 탭 전환 등)를 브로드캐스트하는 옵저버 채널.
// ObserverPattern<TSender,TParam>을 UIManager 전용으로 특수화한 것뿐이라 본문은 비어있다.
public enum E_UIEvent
{
    None = 0,

    OpenEquip,
    OpenSkill,
    OpenAvatar,
    OpenHeroGrowth,
    OpenGacha,
    OpenShop,
    OpenPet,

    CloseEquip,
    CloseSkill,
    CloseAvatar,
    CloseHeroGrowth,
    CloseGacha,
    CloseShop,
    ClosePet,

    OnWeaponTab,
    OnShieldTab,
    OnAccessaryTab,

    ConfirmEquipSlot,
    ConfirmSkillSlot,
    ConfirmAvatarSlot,
    ConfirmPetSlot,

    // ... 나머지 탭/슬롯 이벤트 다수 생략
}

public struct UIEventMessage
{
    public E_UIEvent eventType;
}

public class UIObserver : ObserverPattern<UIManager, UIEventMessage>
{
}
