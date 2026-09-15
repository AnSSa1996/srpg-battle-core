using SRPG.Battle.Core.Grid;
using SRPG.Battle.Core.Model;

namespace SRPG.Battle.Core.Ai
{
    /// <summary>
    /// AI 행동 후보. 이동 목적지 + (선택)공격 대상 + 생성 순서 인덱스(동점 타이브레이크용).
    /// 대상이 null 이면 이동/대기만. (참조: AI-020/031)
    /// </summary>
    public sealed class Candidate
    {
        /// <summary>이동 목적지 타일.</summary>
        public Coord Destination { get; }

        /// <summary>공격 대상(없으면 null).</summary>
        public Unit Target { get; }

        /// <summary>생성 순서 인덱스. 점수 동점 시 작은 값을 선택(무작위 금지). (AI-031)</summary>
        public int GenerationIndex { get; }

        /// <summary>후보를 만든다.</summary>
        public Candidate(Coord destination, Unit target, int generationIndex)
        {
            Destination = destination;
            Target = target;
            GenerationIndex = generationIndex;
        }
    }
}
