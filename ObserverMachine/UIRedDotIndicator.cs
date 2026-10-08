using UnityEngine;

// 오브젝트의 활성/비활성 생명주기에 맞춰 레드닷 등록/해제를 자동화하는 컴포넌트.
// 씬에 붙여두기만 하면 UIRedDotActivator 에 스스로 등록되므로, 개별 UI 코드가
// 레드닷 설치/해제 타이밍을 직접 관리할 필요가 없다.
public class UIRedDotIndicator : Behaviour
{
    [SerializeField] private E_RedDotType redDotType = E_RedDotType.None;
    [SerializeField] private E_RedDotAnchor anchor = E_RedDotAnchor.LeftTop;

    private void OnEnable()
    {
        UIManager.RedDot()?.InstallRedDot(redDotType, gameObject, anchor);
    }

    private void OnDisable()
    {
        // 씬 언로드 시 GC보다 OnDisable 호출이 먼저 오는 경우가 있어 null 체크가 필요하다.
        var redDotActivator = UIManager.RedDot();

        if (redDotActivator != null)
        {
            redDotActivator.UnInstallRedDot(redDotType);
        }
    }
}
