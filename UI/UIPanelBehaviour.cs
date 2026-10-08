using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// 모든 UI 패널의 공통 기반 클래스. 레이어/딤드(배경 어둡게)/오픈 연출 여부를
// 가상 메서드로 정의해두고, 각 패널이 필요한 것만 override 하는 Template Method 구조.
// PopupManager/UILayer 는 이 추상 타입만 알면 되므로, 팝업 종류가 늘어나도
// 생성/배치/딤드 처리 로직은 그대로 재사용된다.
public abstract class UIPanelBehaviour : UIBehaviour
{
    private E_UILayers? customLayer = null;
    private UIDimmed dimmed = null;

    public E_UILayers GetFinalLayer()
    {
        return customLayer ?? GetLayer();
    }
    public void SetLayer(E_UILayers? customLayer)
    {
        this.customLayer = customLayer;
    }

    public virtual bool ApplySafeArea() { return true; }

    public void SetDimmedObject(UIDimmed value) => dimmed = value;
    public UIDimmed GetDimmedObject() => dimmed;

    public abstract void SetParent(Transform transform);
    public abstract E_UILayers GetLayer();

    public virtual bool HasDimmed() { return false; }
    public virtual bool HasOpenDirecting() { return false; }
    public virtual UIDimmed.E_Alpha GetDimmedAlpha() { return UIDimmed.E_Alpha.Normal; }
    public virtual System.Action GetDimmedCallback() { return null; }
    public virtual bool IsFullScreenSize() { return false; }
}

// 오브젝트 풀링 가능한 패널. ObjectPool<T> 가 요구하는 생명주기 훅(IPoolObject)을 구현해서
// 팝업을 닫을 때 Destroy 하지 않고 풀로 반환할 수 있게 한다.
public abstract class UIPanelBehaviourPooled : UIPanelBehaviour, IPoolObject
{
    public virtual void OnAlloc() { }
    public virtual void OnFirstCreate() { }
    public virtual void OnFree() { }
    public virtual void OnLastRelease() { }
}
