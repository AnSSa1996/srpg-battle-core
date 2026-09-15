using SRPG.Battle.Core.Rng;
using Xunit;

namespace SRPG.Battle.Core.Tests
{
    /// <summary>
    /// 결정론 난수(TECH-200~205) 검증.
    /// 자동 전투가 리플레이 가능하려면 "같은 seed = 같은 수열"이 깨지지 않아야 한다.
    /// </summary>
    public class DetRngTests
    {
        private const int SampleCount = 1000;

        [Fact]
        public void SameSeed_ProducesIdenticalSequence()
        {
            var a = new DetRng(20260915L);
            var b = new DetRng(20260915L);

            for (int i = 0; i < SampleCount; i++)
            {
                Assert.Equal(a.Next(), b.Next());
            }
        }

        [Fact]
        public void DifferentSeed_ProducesDifferentSequence()
        {
            var a = new DetRng(1L);
            var b = new DetRng(2L);

            bool diverged = false;
            for (int i = 0; i < SampleCount && diverged == false; i++)
            {
                if (a.Next() != b.Next())
                {
                    diverged = true;
                }
            }

            Assert.True(diverged, "서로 다른 seed 인데 1000 회까지 동일한 수열이 나왔다.");
        }

        [Fact]
        public void Init_ResetsToSameSequence()
        {
            var rng = new DetRng(777L);
            ulong first = rng.Next();

            rng.Init(777L);

            Assert.Equal(first, rng.Next());
        }

        /// <summary>
        /// 1-ply 룩어헤드는 원본 수열을 소비하지 않고 미리 굴려봐야 한다.
        /// Clone 이 같은 지점에서 갈라지지 않으면 AI 평가가 실제 전투 결과를 바꿔버린다.
        /// </summary>
        [Fact]
        public void Clone_ContinuesTheSameSequenceIndependently()
        {
            var origin = new DetRng(4242L);
            origin.Next();
            origin.Next();

            DetRng clone = origin.Clone();

            for (int i = 0; i < 100; i++)
            {
                Assert.Equal(origin.Next(), clone.Next());
            }
        }

        [Fact]
        public void NextPerMille_StaysWithinZeroTo999()
        {
            var rng = new DetRng(-31337L);

            for (int i = 0; i < SampleCount; i++)
            {
                int value = rng.NextPerMille();
                Assert.InRange(value, 0, 999);
            }
        }

        [Fact]
        public void NextRange_StaysBelowExclusiveMax()
        {
            var rng = new DetRng(99L);

            for (int i = 0; i < SampleCount; i++)
            {
                int value = rng.NextRange(6);
                Assert.InRange(value, 0, 5);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void NextRange_NonPositiveMax_ReturnsZero(int exclusiveMax)
        {
            var rng = new DetRng(5L);

            Assert.Equal(0, rng.NextRange(exclusiveMax));
        }

        [Fact]
        public void Chance_ZeroPerMille_NeverSucceeds()
        {
            var rng = new DetRng(11L);

            for (int i = 0; i < SampleCount; i++)
            {
                Assert.False(rng.Chance(0));
            }
        }

        [Fact]
        public void Chance_FullPerMille_AlwaysSucceeds()
        {
            var rng = new DetRng(11L);

            for (int i = 0; i < SampleCount; i++)
            {
                Assert.True(rng.Chance(1000));
            }
        }
    }
}
