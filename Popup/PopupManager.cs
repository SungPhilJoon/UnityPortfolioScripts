using System;
using System.Collections.Generic;
using UnityEngine;

// 실제 PopupManager(싱글톤 GTGameManager.Manager<T>)는 500줄이 넘는 매니저 클래스라,
// 이 파일에서는 "팝업을 어떻게 캐싱하고, 어떤 순서로 화면에 붙이고, 어떻게 되돌려주는지"에
// 해당하는 CreatePopup / addPopup / getPopup 계열 메서드만 뽑아서 흐름을 보여준다.
// PopupCreator.cs가 부르는 PopupManager.Instance.CreatePopup<T>()가 바로 이 클래스다.
public class PopupManager : GTGameManager.Manager<PopupManager>
{
    private class PopupInfo
    {
        public Type type;
        public BasePopup popup;

        public PopupInfo(Type type, BasePopup popup)
        {
            this.type = type;
            this.popup = popup;
        }
    }

    private List<PopupInfo> popupList = new List<PopupInfo>();
    private PopupInfo currentPopupInfo = null;

    // 자주 열고 닫는 팝업(성장/장비/아바타 등)은 Destroy 하지 않고 재사용한다.
    // 최초 화면 진입 시 addCachePopup 으로 "캐시 대상"만 등록해두고,
    // 실제 생성은 처음 요청이 들어올 때 지연 생성(setCachePopup)한다.
    private Dictionary<Type, BasePopup> cachePopups = new Dictionary<Type, BasePopup>();

    // CreatePopup<T>() 하나의 진입점이 캐시 팝업 재사용 / 중복 오픈 방지 / 신규 생성을
    // 모두 처리하고, 마지막에 addPopup 으로 공통 후처리(스택 등록, 사운드, 콜백)를 태운다.
    public T CreatePopup<T>(Action<T> _action = null, Action close = null, E_UILayers? customLayer = null) where T : BasePopup
    {
        T popup = null;
        if (cachePopups.TryGetValue(typeof(T), out BasePopup cachePopup))
        {
            popup = cachePopup == null ? setCachePopup<T>() : cachePopup as T;
        }
        else
        {
            // 중복 오픈이 금지된 팝업(IsUnique)은 이미 떠 있으면 새로 만들지 않는다.
            var alreadyPopup = getPopup<T>();
            if (alreadyPopup != null && alreadyPopup.IsUnique())
            {
                return null;
            }

            popup = UIManager.Instance.CreateUIPanel<T>(customLayer);
            if (popup == null)
            {
                return null;
            }
        }

        popup.Show();
        popup.SetCloseCallback(close);

        return addPopup(popup, _action);
    }

    public void ClosePopup(BasePopup popup)
    {
        removePopup(popup);
    }

    private T addPopup<T>(T popup, Action<T> callback) where T : BasePopup
    {
        currentPopupInfo = new PopupInfo(typeof(T), popup);
        popupList.Add(currentPopupInfo);

        popup.Create();

        callback?.Invoke(popup);

        if (popup.IsLowSoundVolume())
        {
            SoundManager.Instance.PushVolumeStack(0f, E_SoundType.SFX, E_SoundType.BGM);
        }

        return popup;
    }

    private void removePopup(BasePopup popup)
    {
        if (currentPopupInfo != null && currentPopupInfo.popup == popup)
        {
            popupList.Remove(currentPopupInfo);
            findNextCurrentPopup();
            return;
        }

        for (int i = 0; i < popupList.Count; ++i)
        {
            if (popupList[i].popup != popup)
            {
                continue;
            }

            removePopup(popupList[i]);
            break;
        }
    }

    private void removePopup(PopupInfo info)
    {
        popupList.Remove(info);

        if (cachePopups.ContainsKey(info.type))
        {
            // 캐시 대상 팝업은 파괴하지 않고 숨기기만 한다.
            info.popup.Hide();
            info.popup.SetCloseCallback(null);
        }
        else
        {
            UIManager.Instance.RemoveUIPanel(info.popup);
        }
    }

    private void findNextCurrentPopup()
    {
        currentPopupInfo = popupList.Count > 0 ? popupList[popupList.Count - 1] : null;
    }

    private T setCachePopup<T>() where T : BasePopup
    {
        var cacheType = typeof(T);
        T popup = UIManager.Instance.CreateUIPanel<T>();
        popup.transform.localScale = Vector3.one;

        cachePopups[cacheType] = popup;

        return popup;
    }

    private BasePopup getPopup<T>() where T : BasePopup
    {
        var enumerator = popupList.GetEnumerator();
        while (enumerator.MoveNext())
        {
            if (enumerator.Current.type == typeof(T))
            {
                return enumerator.Current.popup;
            }
        }

        return null;
    }

    private void addCachePopup<T>() where T : BasePopup
    {
        if (cachePopups.TryGetValue(typeof(T), out _) == false)
        {
            cachePopups.Add(typeof(T), null);
        }
    }
}
