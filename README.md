# SRPG.Battle.Core

개인 개발 중인 방치형 SRPG(Unity 2022.3)의 전투 로직을 엔진 의존 없이 떼어낸 라이브러리다.
74파일 5,460라인, `UnityEngine` 참조 0개, 외부 의존은 `System` 계열뿐이다.

## 왜 뗐나

전투가 Unity 안에 있으면 재생 버튼을 눌러야만 검증된다. 밸런스 잡겠다고 배치만 바꿔 수천 판을 돌릴 수도 없고,
결과가 float과 프레임 타이밍에 걸려 있으면 같은 판을 두 번 만들지도 못한다.

그래서 규칙과 연출을 갈랐다. 코어는 결과만 계산하고 무슨 일이 있었는지는 `BattleEvent`로 내보낸다.
`IEventSink` 구현만 바꿔 끼우면 같은 전투가 콘솔 ASCII로도, 테스트용 이벤트 목록으로도, 아무것도 안 그리는 시뮬로도 돌아간다.

## 빌드 · 테스트

```bash
dotnet build src/SRPG.Battle.Core/SRPG.Battle.Core.csproj
dotnet test  tests/SRPG.Battle.Core.Tests/SRPG.Battle.Core.Tests.csproj
```

`netstandard2.1` 타깃이라 Unity 프로젝트에서 그대로 참조한다. 빌드 경고 0 · 오류 0, 테스트 39/39 통과.

## 구성

| 폴더 | 내용 | REQ-ID |
|------|------|--------|
| `Rng/` | 결정론 난수 (xorshift128+) | TECH-200~205 |
| `Grid/` | 맨해튼 거리 · 이동 BFS · 방향/피격면 · AoE | CMB-400~450 |
| `Formula/` | 피해 계산 · 필드효과 | CMB-100~105, 520~522 |
| `Ai/` | 후보 생성 · 유틸리티 스코어링 · 1-ply 룩어헤드 | AI-020~171 |
| `Loop/` | CT 스케줄러 · 턴 진행 · 목표 판정 · 헤드리스 러너 | CMB-010~054 |
| `Events/` | `BattleEvent` · 싱크 3종(Null/List/Ascii) | VIEW-001~010 |
| `Model/` | `BattleState` · 유닛 · 상태이상 · 이동 확정 | CMB-040, 200~208 |
| `Data/` | 맵 · 스킬 · 상태이상 · 필드효과 정의 | CMB-200/300/500~ |
| `Stats/` | 스탯 · 모디파이어 · CP 산출 | GROW-100~132 |
| `Commands/` | 이동/공격/스킬/대기 | CMB-300~308 |
| `Constants/` | `IntMath` · `Permille` · 전투/AI 상수 | — |

## 정해둔 것

- 확률 판정은 `DetRng` 하나만 쓴다. xorshift128+에 SplitMix64 시드 확장. 같은 seed면 항상 같은 수열이다.
- 배율은 전부 천분율 정수다(`Permille`, 1000 = 100%). float을 쓰지 않는다.
- `DetRng`는 struct가 아니라 class다. 값 복사가 일어나면 같은 수열을 두 번 소비하는 결정론 버그가 난다. 룩어헤드는 `Clone()`으로 갈라 쓴다.
- 거리는 맨해튼이고 대각은 인접이 아니다. 이동 BFS에서 벽과 적은 통과 불가, 아군은 통과는 되지만 정지는 못 한다.

테스트는 위 네 가지가 깨지는지를 건다. 대각이 인접으로 새면 사거리 전체가 틀어지고, `Clone()`이 독립적이지 않으면 AI 평가가 실제 전투 결과를 바꿔버린다.

## 없는 것

게임 본체(Unity 프로젝트)는 유료 에셋이 들어 있어 공개하지 않는다. 여기 있는 건 직접 쓴 전투 로직뿐이다.
코드 주석의 `CMB-401` 같은 번호는 기획서 요구사항 ID다.

MIT
