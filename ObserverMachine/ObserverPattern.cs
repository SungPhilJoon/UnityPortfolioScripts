// delegate/Action 등록 방식 대신 인터페이스 기반으로 설계한 이유:
// 함수 등록 방식은 확장은 쉽지만, 어떤 메서드가 옵저버 콜백으로 쓰이는지 시그니처만으로는
// 강제되지 않아 코드를 훑어봐도 알아보기 어렵다. IListener 인터페이스로 강제하면
// "이 클래스는 이 이벤트를 구독한다"는 사실이 클래스 선언부에서 한눈에 드러난다.

using System.Collections.Generic;

public class ObserverPattern<TSender, TParam> where TSender : class
{
    public interface IListener
    {
        void OnEvent(TSender owner, TParam param);
    }

    private List<IListener> subscribes = new List<IListener>();
    private TSender sender = null;

    public ObserverPattern()
    {

    }

    ~ObserverPattern()
    {
        sender = null;
        subscribes.Clear();
    }

    public void SetSender(TSender sender)
    {
        if (this.sender != null)
        {
            Debugger.Error($"null != sender");
        }

        this.sender = sender;
    }

    public void Subscribe(IListener listener)
    {
        subscribes.Add(listener);
    }

    public void Unsubscribe(IListener listener)
    {
        subscribes.Remove(listener);
    }

    public void Send(TParam param)
    {
        for(int i = 0; i < subscribes.Count; ++i)
        {
            subscribes[i].OnEvent(sender, param);
        }
    }

    public void Clear()
    {
        subscribes.Clear();
    }
}
