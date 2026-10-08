using System.Collections;
using UnityEngine;
#if UNITY_ANDROID
using Google.Play.Review;
#endif

#if UNITY_IOS
using UnityEngine.iOS;
#endif

// PopupCreator/PopupReserve가 조건 충족 시 생성하는 대상 타입의 실제 구현 예시.
// BasePopup에 IReservePopup을 함께 구현하기만 하면, PopupManager.ReservePopup<T>()로
// 조건부 예약 팝업 목록에 등록될 수 있다 (실제 등록 지점은 GameAttendContents.cs 참고,
// 거기서는 PopupAttend를 예약하지만 등록 방식은 동일하다).
public class PopupInAppReview : BasePopup, IReservePopup
{
#if UNITY_ANDROID
    private ReviewManager reviewManager = new ReviewManager();
    private PlayReviewInfo playReviewInfo = null;
#endif

    public override UIDimmed.E_Alpha GetDimmedAlpha()
    {
        return UIDimmed.E_Alpha.Normal;
    }

    public override bool HasDimmed()
    {
        return true;
    }

    protected override void OnCreate()
    {
    }

    public override E_UILayers GetLayer()
    {
        return E_UILayers.Message;
    }

    public override void OnClickOk()
    {
#if UNITY_ANDROID && !ONESTORE
        StartCoroutine(launchReviewFlowEnumerator());
#else
        var serverSetting = BackendManager.Instance.GetServerSetting();
        string storeURL = string.Empty;

#if UNITY_IOS
        storeURL = serverSetting.iosAppURL;
#elif ONESTORE
        storeURL = serverSetting.oneAppURL;
#endif
        launchReviewFlow(storeURL);
#endif
    }

    public override void OnClickClose()
    {
        EndClose();
    }

    // IReservePopup 계약의 유일한 멤버. 예약 조건이 충족돼 실제로 생성된 직후 호출된다.
    public void InitializeReserve(Parameter parameter)
    {
    }

#if UNITY_ANDROID
    private IEnumerator launchReviewFlowEnumerator()
    {
        var popupLoad = PopupManager.Instance.CreatePopup<PopupDataLoad>();

        var requestFlowOperation = reviewManager.RequestReviewFlow();
        yield return new WaitUntil(() => requestFlowOperation.IsDone);

        if (requestFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debugger.Error($"Request Flow Operation Error : {requestFlowOperation.Error}");

            yield break;
        }

        playReviewInfo = requestFlowOperation.GetResult();

        var launchFlowOperation = reviewManager.LaunchReviewFlow(playReviewInfo);
        yield return new WaitUntil(() => launchFlowOperation.IsDone);
        playReviewInfo = null;

        if (launchFlowOperation.Error != ReviewErrorCode.NoError)
        {
            Debugger.Error($"Launch Flow Operation Error : {launchFlowOperation.Error}");
        }

        popupLoad.DestroyPanel();
        popupLoad = null;

        EndClose();
    }
#endif

    private void launchReviewFlow(string storeURL)
    {
        var serverSetting = BackendManager.Instance.GetServerSetting();
        Application.OpenURL(storeURL);

        EndClose();
    }
}
