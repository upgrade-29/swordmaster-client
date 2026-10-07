using System;

// 시드가 같으면 어느 플랫폼(Mono, IL2CPP, 서버 .NET)에서든 같은 순서의 값을 내는 난수 생성기
// System.Random은 런타임마다 구현이 같다는 보장이 없어서, 클라와 서버가 같은 전투를 계산해야 하는 곳에서는 이걸 쓴다
// 암호용이 아니다. 시드를 알면 다음 값을 알 수 있다
public class SplitMix64
{
    private const ulong Gamma = 0x9E3779B97F4A7C15UL;
    private const double DoubleUnit = 1.0 / (1UL << 53);

    private ulong state;

    public SplitMix64(ulong seed)
    {
        state = seed;
    }

    public ulong NextULong()
    {
        unchecked
        {
            state += Gamma;
            return Mix(state);
        }
    }

    // 비슷한 입력(1, 2, 3...)도 서로 연관 없어 보이는 값으로 섞는다. 상태가 없으므로 시드를 나눠 만들 때 쓴다
    public static ulong Mix(ulong z)
    {
        // 프로젝트 설정과 상관없이 오버플로는 넘쳐서 돌아가야 같은 값이 나온다
        unchecked
        {
            z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
            z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
            return z ^ (z >> 31);
        }
    }

    // [0, 1). 상위 53비트를 정수로 꺼낸 뒤 한 번만 곱하므로 플랫폼마다 값이 달라지지 않는다
    public double NextDouble()
    {
        return (NextULong() >> 11) * DoubleUnit;
    }

    // [0, maxExclusive). 나머지 연산으로 생기는 치우침을 없애려고 범위를 넘는 값은 버리고 다시 뽑는다
    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxExclusive), maxExclusive, "Must be positive");

        var range = (ulong)maxExclusive;
        ulong limit = ulong.MaxValue - ulong.MaxValue % range;
        ulong value;
        do
        {
            value = NextULong();
        } while (value >= limit);

        return (int)(value % range);
    }
}
