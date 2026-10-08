using System;
using System.Collections.Generic;

// Factory + Strategy 조합: "무엇을(PopupCreator<T>) 언제(IPopupCreateCondition) 만들지"를 분리.
// PopupReserve 는 생성 시점을 모르는 채로 예약만 걸어두고, 매 프레임 조건을 재평가해서
// 모든 조건이 충족되는 순간 실제 팝업을 생성한다. (예: 로딩 종료 + 튜토리얼 종료 후에만 띄우는 팝업)
public interface IPopupCreator
{
    BasePopup Create();
}

public class PopupCreator<T> : IPopupCreator where T : BasePopup, IReservePopup
{
    private Action close = null;
    private E_UILayers? customLayer = null;
    private Parameter? parameter = null;
    private IUILogic logic = null;

    public PopupCreator(Action close = null, Parameter? parameter = null, E_UILayers? customLayer = null, IUILogic logic = null)
    {
        this.close = close;
        this.customLayer = customLayer;
        this.parameter = parameter;
        this.logic = logic;
    }

    public BasePopup Create()
    {
        var popup = PopupManager.Instance.CreatePopup<T>(null, close, customLayer);
        if (logic != null)
        {
            popup.BindingLogic(logic);
        }

        popup.InitializeReserve(parameter);

        return popup;
    }
}

public class PopupReserve
{
    private IPopupCreator creator = null;
    private List<E_PopupCreateCondition> conditions = new List<E_PopupCreateCondition>();

    private static Dictionary<E_PopupCreateCondition, IPopupCreateCondition> statics = createStaticConditions();
    private static E_PopupCreateCondition[] conditionList = EnumUtils.GetValues<E_PopupCreateCondition>();

    // 등록된 조건이 전부 만족될 때만 실제 생성을 트리거한다.
    public bool Execute()
    {
        for (int i = 0; i < conditions.Count; ++i)
        {
            var condition = conditions[i];
            if (!statics[condition].Check())
            {
                return false;
            }
        }

        creator.Create();
        conditions.Clear();

        return true;
    }

    public static PopupReserve Create<T>(E_PopupCreateCondition conditions, Parameter? parameter = null, Action close = null, E_UILayers? customLayer = null, IUILogic logic = null) where T : BasePopup, IReservePopup
    {
        PopupReserve reserve = new PopupReserve();
        reserve.creator = new PopupCreator<T>(close, parameter, customLayer, logic);

        for (int i = 0; i < conditionList.Length; ++i)
        {
            if ((conditionList[i] & conditions) > 0)
            {
                reserve.conditions.Add(conditionList[i]);
            }
        }

        return reserve;
    }

    private static Dictionary<E_PopupCreateCondition, IPopupCreateCondition> createStaticConditions()
    {
        var dictionary = new Dictionary<E_PopupCreateCondition, IPopupCreateCondition>();

        dictionary.Add(E_PopupCreateCondition.Campaign, new PopupConditionCampaign());
        dictionary.Add(E_PopupCreateCondition.CleanPopup, new PopupConditionCleanPopup());

        return dictionary;
    }
}
