using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PopupSeasonPass : BasePopup
{
    [SerializeField] private Toggle[] passPanelToggles = null;
    [SerializeField] private SeasonPassPanel Panel = null;

    private Animation ani = null;

    public override E_UILayers GetLayer()
    {
        return E_UILayers.FrontGame;
    }

    public override bool HasDimmed()
    {
        return true;
    }
    public override UIDimmed.E_Alpha GetDimmedAlpha()
    {
        return UIDimmed.E_Alpha.Full;
    }
    public override bool IsLowSoundVolume()
    {
        return true;
    }

    protected override void OnCreate()
    {
        base.OnCreate();
    }
    protected override void OnUIDestroy()
    {
        Panel.Release();

        UIManager.RedDot().UnInstallAllRedDots(E_RedDotType.SeasonPass_LV);
        UIManager.RedDot().UnInstallAllRedDots(E_RedDotType.SeasonPass_Stage);
        UIManager.RedDot().UnInstallAllRedDots(E_RedDotType.SeasonPass_Kill);

        base.OnUIDestroy();
    }
    public void Init()
    {
        var levelToggle = passPanelToggles[(int)SeasonPassGoalType.LVUP];
        var stageToggle = passPanelToggles[(int)SeasonPassGoalType.STAGECLEARID];
        var killToggle = passPanelToggles[(int)SeasonPassGoalType.KILLALL];

        levelToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
                SelectPassToggle(SeasonPassGoalType.LVUP);
        });

        stageToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
                SelectPassToggle(SeasonPassGoalType.STAGECLEARID);
        });

        killToggle.onValueChanged.AddListener(isOn =>
        {
            if (isOn)
                SelectPassToggle(SeasonPassGoalType.KILLALL);
        });

        UIManager.RedDot().InstallRedDot(E_RedDotType.SeasonPass_LV, levelToggle.gameObject, E_RedDotAnchor.RightTop);
        UIManager.RedDot().InstallRedDot(E_RedDotType.SeasonPass_Stage, stageToggle.gameObject, E_RedDotAnchor.RightTop);
        UIManager.RedDot().InstallRedDot(E_RedDotType.SeasonPass_Kill, killToggle.gameObject, E_RedDotAnchor.RightTop);

        for (int i = 0; i < passPanelToggles.Length; i++)
        {
            passPanelToggles[i].gameObject.SetActive(false);
        }

        for (int i = 0; i < TableManager.Instance.SeasonPass.Count; i++)
        {
            passPanelToggles[i].gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        passPanelToggles[(int)SeasonPassGoalType.STAGECLEARID].isOn = true;
    }


    private void SelectPassToggle(SeasonPassGoalType _goalType)
    {
        SeasonPassData _data = TableManager.Instance.SeasonPass.Find(x => x.GoalType == _goalType);
        if (_data == null) return;

        Panel.SetData(_data);
    }

    public override void OnClickOk()
    {
        OnClickClose();
    }

    public override void OnClose()
    {
        if (ani != null)
            ani.Play("BasePopupClose");
        else
            EndClose();
    }
}
