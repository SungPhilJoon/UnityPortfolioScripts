using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// E_UILayers 값 하나당 독립된 Canvas를 만들어서 sortingOrder로 렌더링 순서를 제어한다.
// (UILayer_Base < UILayer_Popup < UILayer_ToastMessage 순으로 값이 커짐)
public class UILayer
{
    private List<UIPanelBehaviour> panels = new List<UIPanelBehaviour>();

    private GameObject layerObject = null;

    private RectTransform layerTransform = null;

    private Canvas canvas = null;

    private CanvasGroup group = null;

    private GraphicRaycaster raycaster = null;

    private E_UILayers depth;

    private RectTransform rootTransform = null;

    public bool ApplySafeArea { private set; get; } = true;


    public UILayer(RectTransform rootTransform, E_UILayers InDepth)
    {
        depth = InDepth;
        this.rootTransform = rootTransform;

        layerObject = new GameObject($"UILayer_{InDepth}");
        layerObject.transform.SetParent(rootTransform);
        layerObject.transform.localPosition = Vector3.zero;
        layerObject.transform.localRotation = Quaternion.identity;
        layerObject.transform.localScale = Vector3.one;

        layerObject.layer = LayerMask.NameToLayer("UI");

        canvas = layerObject.AddComponent<Canvas>();
        canvas.sortingLayerName = $"SortingLayer_{InDepth}";
        canvas.overrideSorting = true;
        canvas.pixelPerfect = false;
        canvas.planeDistance = 1.0f;
        canvas.sortingOrder = (int)InDepth;
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        layerTransform = layerObject.GetComponent<RectTransform>();

        UIUtil.Stretch(layerTransform);

        group = layerObject.AddComponent<CanvasGroup>();
        raycaster = layerObject.AddComponent<GraphicRaycaster>();

        ApplySafeArea = true;
    }

    public void Add(UIPanelBehaviour InController,
        UISettingFile setting,
        UIDimmedCreator dimmedCreator,
        UIPanelDirectingCreator directingCreator)
    {
        panels.Add(InController);

        InController.transform.SetParent(layerTransform);
        var rt = InController.rectTransform;

        if (InController.HasDimmed())
        {
            var dimmed = dimmedCreator.Create(InController.rectTransform);
            dimmed.SetClickEvent(InController.GetDimmedCallback());
            dimmed.SetAlpha(setting.GetAlphaValueFromType(InController.GetDimmedAlpha()));

            InController.SetDimmedObject(dimmed);
        }

        if(InController.IsFullScreenSize())
        {
            rt.sizeDelta = rootTransform.sizeDelta;
        }
        else
        {
            UIUtil.Stretch(rt);
        }

        if(InController.HasOpenDirecting())
        {
            directingCreator.Play(InController);
        }

        rt.anchoredPosition = Vector2.zero;
        rt.localScale = Vector3.one;

        ApplySafeArea = InController.ApplySafeArea();
    }

    public void Remove(UIPanelBehaviour InController, UIDimmedCreator dimmedCreator)
    {
        var dimmedObject = InController.GetDimmedObject();
        if (dimmedObject != null)
        {
            dimmedCreator.Free(dimmedObject);

            InController.SetDimmedObject(null);
        }

        panels.Remove(InController);

        ApplySafeArea = panels.Count > 0 ? panels.Last().ApplySafeArea() : true;
    }

    public List<UIPanelBehaviour> GetPanels()
    {
        return panels;
    }

    public void ClearPanels()
    {
        panels.Clear();
    }

    public int GetVisibleCount()
    {
        int count = 0;
        foreach(var panel in panels)
        {
            if(panel.IsActive())
            {
                ++count;
            }
        }
        return count;
    }
    public GameObject GetLayerObject()
    {
        return layerObject;
    }

    public void UpdateSafeArea(Rect safeArea)
    {
        Vector2 anchorMin = safeArea.position;
        Vector2 anchorMax = safeArea.position + safeArea.size;

        anchorMin.x /= Screen.width;
        anchorMin.y /= Screen.height;
        anchorMax.x /= Screen.width;
        anchorMax.y /= Screen.height;

        if (anchorMin.x >= 0 && anchorMin.y >= 0 && anchorMax.x >= 0 && anchorMax.y >= 0)
        {
            layerTransform.anchorMin = anchorMin;
            layerTransform.anchorMax = anchorMax;
        }
    }
}
