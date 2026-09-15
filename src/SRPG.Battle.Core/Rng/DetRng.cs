namespace SRPG.Battle.Core.Rng
{
    /// <summary>
    /// 전투 전용 결정론 난수 생성기 (xorshift128+). 같은 seed 는 항상 같은 64비트 수열을 만든다.
    /// 명중·치명·상태 적용 등 전투의 모든 확률 판정은 이 생성기만 사용한다.
    /// (참조: Docs/01_전투/24_PRNG_난수.md TECH-200~210)
    ///
    /// 값 복사가 일어나면 같은 수열을 두 번 소비하는 결정론 버그가 생기므로,
    /// 의도적으로 참조 타입(class)으로 둔다. (struct 금지)
    /// </summary>
    public sealed class DetRng
    {
        // SplitMix64 시드 확장 상수 (TECH-204)
        private const ulong SPLIT_MIX_GAMMA = 0x9E3779B97F4A7C15UL;
        private const ulong SPLIT_MIX_MIX_1 = 0xBF58476D1CE4E5B9UL;
        private const ulong SPLIT_MIX_MIX_2 = 0x94D049BB133111EBUL;

        // s0,s1 이 모두 0이 되는 것을 막기 위한 고정 대체값 (TECH-204)
        private const ulong NON_ZERO_FALLBACK = 0x9E3779B97F4A7C15UL;

        // 천분율(per-mille) 판정 분해능 (TECH-205)
        private const int PER_MILLE_DIVISOR = 1000;

        // xorshift128+ 비트 시프트 상수 (알고리즘 고정값, TECH-201)
        private const int XORSHIFT_SHIFT_A = 23;
        private const int XORSHIFT_SHIFT_B = 17;
        private const int XORSHIFT_SHIFT_C = 26;

        // SplitMix64 비트 시프트 상수 (알고리즘 고정값, TECH-204)
        private const int SPLIT_MIX_SHIFT_1 = 30;
        private const int SPLIT_MIX_SHIFT_2 = 27;
        private const int SPLIT_MIX_SHIFT_3 = 31;

        private ulong _state0 = 0UL;
        private ulong _state1 = 0UL;

        /// <summary>battleSeed 로 상태를 초기화한 생성기를 만든다. (TECH-203)</summary>
        public DetRng(long battleSeed)
        {
            Init(battleSeed);
        }

        /// <summary>battleSeed 를 SplitMix64 로 확장해 내부 상태(s0,s1)를 채운다. (TECH-204)</summary>
        public void Init(long battleSeed)
        {
            ulong seed = unchecked((ulong)battleSeed);
            _state0 = SplitMix64(ref seed);
            _state1 = SplitMix64(ref seed);

            if (_state0 == 0UL && _state1 == 0UL)
            {
                _state1 = NON_ZERO_FALLBACK;
            }
        }

        /// <summary>다음 64비트 난수를 반환한다. (xorshift128+, 64비트 wraparound, TECH-201)</summary>
        public ulong Next()
        {
            unchecked
            {
                ulong x = _state0;
                ulong y = _state1;
                _state0 = y;
                x ^= x << XORSHIFT_SHIFT_A;
                _state1 = x ^ y ^ (x >> XORSHIFT_SHIFT_B) ^ (y >> XORSHIFT_SHIFT_C);
                return _state1 + y;
            }
        }

        /// <summary>0~999 범위의 천분율 난수를 반환한다. 명중·치명·상태 적용 판정용. (TECH-205)</summary>
        public int NextPerMille()
        {
            return (int)(Next() % PER_MILLE_DIVISOR);
        }

        /// <summary>0~(exclusiveMax-1) 범위의 정수 난수를 반환한다. exclusiveMax 가 0 이하면 0을 반환한다. (TECH-205, G4)</summary>
        public int NextRange(int exclusiveMax)
        {
            if (exclusiveMax <= 0)
            {
                return 0;
            }

            return (int)(Next() % (ulong)exclusiveMax);
        }

        /// <summary>천분율 확률 perMille(0~1000)로 성공 여부를 판정한다. (TECH-205)</summary>
        public bool Chance(int perMille)
        {
            return NextPerMille() < perMille;
        }

        /// <summary>현재 내부 상태를 그대로 복제한다(1-ply 결정론 클론용). 같은 수열을 이어 만든다.</summary>
        public DetRng Clone()
        {
            DetRng copy = new DetRng(0L);
            copy._state0 = _state0;
            copy._state1 = _state1;
            return copy;
        }

        private static ulong SplitMix64(ref ulong seed)
        {
            unchecked
            {
                seed += SPLIT_MIX_GAMMA;
                ulong result = seed;
                result = (result ^ (result >> SPLIT_MIX_SHIFT_1)) * SPLIT_MIX_MIX_1;
                result = (result ^ (result >> SPLIT_MIX_SHIFT_2)) * SPLIT_MIX_MIX_2;
                return result ^ (result >> SPLIT_MIX_SHIFT_3);
            }
        }
    }
}
