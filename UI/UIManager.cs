using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// UI/, Popup/, ObserverMachine/ 세 폴더가 실제로는 전부 이 매니저 하나를 통해 연결된다.
// UILayer 인스턴스를 소유·배치하고(UI 폴더), 팝업 생성/풀링을 담당하며(Popup 폴더),
// UIObserver/UIRedDotActivator를 들고 있다가 정적 접근자로 노출한다(ObserverMachine 폴더).
// 다른 파일들이 UIManager.Instance.XXX()를 호출하는 지점들이 실제로 여기서 어떻게
// 구현돼 있는지 보여주기 위해, 전투 HUD/이펙트 등 이번 포트폴리오 주제와 무관한 필드·메서드는
// 빼고 그 연결 지점만 발췌했다. loadAtlas/removeLayer 같은 비공개 보조 메서드는 생략.
public class UIManager : GTGameManager.Manager<UIManager>
{
    private Dictionary<E_UILayers, UILayer> layers = new Dictionary<E_UILayers, UILayer>();

    private RectTransform rootCanvasTransform = null;
    private UISettingFile settingFile = null;

    private UIDimmedCreator dimmedCreator = new UIDimmedCreator();
    private UIPanelDirectingCreator directingCreator = new UIPanelDirectingCreator();

    private Dictionary<System.Type, ObjectPool<UIPanelBehaviourPooled>> popupPools
        = new Dictionary<System.Type, ObjectPool<UIPanelBehaviourPooled>>();

    private List<UIUserEventListener> userEventListeners = new List<UIUserEventListener>();

    // ObserverMachine 폴더의 UIObserver/UIRedDotActivator를 매니저가 직접 들고 있고,
    // 정적 메서드로만 외부에 노출한다 — 호출부는 UIManager.RedDot() 처럼 인스턴스를 몰라도 된다.
    private UIObserver uiObserver = new UIObserver();
    private UIRedDotActivator redDotActivator = null;

    public static UIRedDotActivator RedDot()
    {
        return Instance?.redDotActivator ?? null;
    }

    // Layer 등록: UILayer(UI 폴더 참고)를 E_UILayers 값마다 하나씩 생성해서 딕셔너리에 보관한다.
    public void RegistLayer(E_UILayers depth)
    {
        if (layers.ContainsKey(depth))
        {
            Debugger.Error($"CreateLayer Depth Error. {depth}");
        }
        else
        {
            UILayer Layer = new UILayer(rootCanvasTransform, depth);

            layers.Add(depth, Layer);
        }
    }

    // 패널이 자기 레이어(GetFinalLayer)를 알려주면, 그 레이어에 실제로 붙이는 건 UILayer.Add()에 위임한다.
    public void AddLayer(UIPanelBehaviour panelBehaviour)
    {
        var depthLayer = panelBehaviour.GetFinalLayer();

        UILayer layer;
        if (!layers.TryGetValue(depthLayer, out layer))
        {
            Debugger.Error($"UIManager Error. AddLayer Failed. depth: {depthLayer}");
            return;
        }

        layer.Add(panelBehaviour, settingFile, dimmedCreator, directingCreator);
    }

    private void removeLayer(UIPanelBehaviour panelBehaviour)
    {
        UILayer layer;
        if (!layers.TryGetValue(panelBehaviour.GetFinalLayer(), out layer))
        {
            return;
        }

        layer.Remove(panelBehaviour, dimmedCreator);
    }

    // 풀링 대상으로 등록해두면, 이후 CreateUIPanelByPath는 매번 새로 생성하지 않고
    // 이 풀에서 꺼내 쓴다 (UIPanelBehaviourPooled, UI/UIPanelBehaviour.cs 참고).
    public void RegistPooling<T>() where T : UIPanelBehaviourPooled
    {
        var path = ResourceManager.Instance.GetResourcePathFromName(UIPathBinder.GetUIPath<T>(), E_DataType.UIPOPUP);

        var panelAllocator = new UIPanelAllocator(path, rootCanvasTransform);

        popupPools.Add(typeof(T), new ObjectPool<UIPanelBehaviourPooled>(allocator: panelAllocator));
    }

    public T CreateUIPanel<T>(E_UILayers? customLayer = null) where T : UIPanelBehaviour
    {
        string name = UIPathBinder.GetUIPath<T>();
        if (string.IsNullOrEmpty(name))
        {
            Debugger.Error($"CreateUIPanel Error. Invalid child path. type: {name}");
            return null;
        }

        return CreateUIPanel<T>(name, customLayer);
    }

    public T CreateUIPanel<T>(string name, E_UILayers? customLayer = null) where T : UIPanelBehaviour
    {
        var path = ResourceManager.Instance.GetResourcePathFromName(name, E_DataType.UIPOPUP);

        return CreateUIPanelByPath<T>(path, customLayer);
    }

    public T CreateUIPanelByPath<T>(string path, E_UILayers? customLayer = null) where T : UIPanelBehaviour
    {
        UIPanelBehaviour panel = null;
        if (popupPools.TryGetValue(typeof(T), out ObjectPool<UIPanelBehaviourPooled> pool))
        {
            panel = pool.Alloc();
        }
        else
        {
            panel = ResourceManager.Instance.Instantiate<T>(path);

            var canvas = panel.GetComponent<Canvas>();
            if (canvas != null)
            {
                Debugger.Error($"Already canvas. {path}");
            }
            else
            {
                panel.gameObject.AddComponent<Canvas>();
            }

            panel.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (panel == null)
        {
            return null;
        }

        this.loadAtlas(panel, path);
        panel.SetLayer(customLayer);

        this.AddLayer(panel);

        return panel as T;
    }

    public void RemoveUIPanel(UIPanelBehaviour panelBehaviour)
    {
        this.removeLayer(panelBehaviour);

        if (popupPools.TryGetValue(panelBehaviour.GetType(), out ObjectPool<UIPanelBehaviourPooled> pool))
        {
            pool.Free(panelBehaviour as UIPanelBehaviourPooled);
        }
        else
        {
            panelBehaviour.DestroyGameObject();
        }
    }

    // ObserverMachine의 UIRedDotActivator/UIUserEventInitializer가 실제로 부르는 지점들.
    public void SubscribeObserver(UIObserver.IListener listener)
    {
        uiObserver.Subscribe(listener);
    }

    public void UnSubscribeObserver(UIObserver.IListener listener)
    {
        uiObserver.Unsubscribe(listener);
    }

    public void SendEvent(E_UIEvent eventType)
    {
        uiObserver.Send(new UIEventMessage
        {
            eventType = eventType
        });
    }

    // 그룹 단위로 UserObserver 리스너를 등록한다 (UserGameData_Core.SubscribeObserver로 위임).
    // ObserverMachine/UIUserEventInitializer.cs가 이 메서드의 실제 호출부다.
    public void AddUserEventListener<T>(params E_UserMessageGroup[] groups) where T : UIUserEventListener, UserObserver.IListener, new()
    {
        var listener = new T();
        listener.Initialize(groups);

        foreach (var group in groups)
        {
            UserGameData.Get().SubscribeObserver(group, listener);
        }

        userEventListeners.Add(listener);
    }
}
