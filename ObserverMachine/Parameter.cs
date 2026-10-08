using System;
using System.Runtime.InteropServices;

// 이벤트 시스템(ObserverMachine 폴더의 UserParameter 등)에서 "값이 뭐가 올지 모르는" 페이로드를
// 박싱 없이 넘기기 위한 타입입니다. object로 박싱하는 대신 고정 크기 byte[] 버퍼에 구조체를
// 그대로 메모리 복사(MemoryMarshal)해서 넣고 꺼냅니다 — 매 프레임 수십~수백 번 발생할 수 있는
// 이벤트 전파 경로에서 GC 압박을 줄이기 위한 선택입니다.
//
// UNITY_EDITOR 빌드에서만 SetValue<T>로 넣은 타입을 기억해뒀다가 GetValue<T>에서 검증합니다.
// 릴리즈 빌드에서는 이 안전장치가 빠지므로, 타입이 어긋나면 조용히 잘못된 값을 읽게 됩니다 —
// 성능과 안전성을 맞바꾼 지점이라 주석 없이 보면 놓치기 쉬운 설계 결정입니다.
public class Parameter
{
#if UNITY_EDITOR
    private Type paramType;
#endif
    public byte[] param = null;

    public Parameter()
        : this(8) // default 8 bytes
    {
    }
    public Parameter(int size)
    {
        param = new byte[size];
    }

    public void SetValue<T>(T val) where T : struct
    {
        MemoryMarshal.Write(param, ref val);
#if UNITY_EDITOR
        paramType = typeof(T);
#endif
    }

    public T GetValue<T>() where T : struct
    {
#if UNITY_EDITOR
        if (typeof(T) != paramType)
        {
            Debugger.Error($"Parameter GetValue Error. {typeof(T).Name} -> {paramType.Name}");
        }
#endif
        ReadOnlySpan<byte> span = param;

        return MemoryMarshal.Read<T>(span);
    }

    public static Parameter Create<T>(T value, int size = 8) where T : struct
    {
        Parameter param = new Parameter(size);
        param.SetValue(value);
        return param;
    }
}
