# SRPG.Battle.Core

방치형 SRPG의 **전투 코어 라이브러리**다. Unity 프로젝트에서 전투 로직만 떼어내 **엔진 의존 없이** 빌드·테스트할 수 있게 분리했다. (Unity 2022.3 LTS · HD-2D 도트 · 타일 그리드 전투)

> ⚠️ **이 저장소는 게임 본체가 아니다.** 게임 클라이언트는 유료 에셋을 포함하므로 공개하지 않는다. 여기 있는 코드는 **전부 직접 작성한 전투 로직**이며, 기획서의 **REQ-ID**로 원본 명세와 연결된다.

## 한 줄 정의
> 타일·이동력·사거리·방향(측/후면)·필드효과를 정수 연산으로 계산하고, 결과를 **BattleEvent 로만 외부에 알리는** 순수 C# 전투 시뮬레이터.

## 핵심 정체성 (가장 중요)
> **"전투는 엔진 없이 돌아가고, 같은 seed 면 같은 전투가 나온다."**
> 전투 규칙(모델)과 연출(뷰)을 **완전히 분리**했다. 코어는 결과만 계산하고 무슨 일이 일어났는지는 이벤트로 흘려보낸다. 덕분에 **콘솔에서 전투를 돌릴 수 있고**, 수천 판을 자동으로 굴려 밸런스를 볼 수 있으며, 같은 판을 언제든 재현할 수 있다.

## 왜 떼어냈나

전투를 Unity 안에 두면 다음 세 가지가 **불가능하다.**

| # | 문제 |
|---|------|
| 1 | **테스트 불가** — 재생 버튼을 눌러야만 검증되는 로직은 회귀를 잡지 못한다. |
| 2 | **밸런스 검증 불가** — 배치만 바꿔 수천 판을 돌려야 수치가 잡히는데, 에디터에선 못 돌린다. |
| 3 | **재현 불가** — 결과가 float·프레임 타이밍에 의존하면 같은 판을 두 번 만들 수 없다. |

→ 그래서 **모델–뷰 분리**를 먼저 하고, 코어를 `netstandard2.1` 라이브러리로 독립시켰다.

## 설계 규칙 (Core Rules)

| # | 규칙 |
|---|------|
| C1 | **엔진 타입 금지** — `UnityEngine` 을 참조하지 않는다. 외부 의존은 `System` · `System.Collections.Generic` · `System.Text` 뿐이다. |
| C2 | **부동소수점 금지** — 모든 배율은 천분율 정수(`Permille`, 1000 = 100%)로 계산한다. (→ `Constants/Permille.cs`) |
| C3 | **난수는 단일 창구** — 확률 판정은 전부 `DetRng` 하나만 쓴다. 다른 난수원 사용 금지. (TECH-200~205) |
| C4 | **뷰는 이벤트로만** — 코어는 렌더링을 모른다. 발생 사실만 `IEventSink` 로 내보낸다. (VIEW-001~) |
| C5 | **REQ-ID 인용** — 모든 규칙성 코드 주석에 기획서 ID를 단다. 수식이 왜 그런지 문서로 바로 넘어갈 수 있어야 한다. |
| C6 | **결정론을 깨는 코드 금지** — 순서·시간·해시 순회에 의존하지 않는다. 같은 입력이면 항상 같은 출력이다. |

> C3 실제 사례: 룩어헤드가 원본 수열을 소비하면 AI 평가가 **실제 전투 결과를 바꿔버린다.** 그래서 `DetRng` 는 의도적으로 참조 타입(class)이고, 평가용은 `Clone()` 으로 갈라 쓴다. **(struct 금지)**

## 모듈 목록

> 규모: **74 파일 · 5,460 라인**. 괄호 안은 원본 기획서의 REQ-ID 대역이다.

### 📁 `Rng/` — 결정론 난수 ★핵심★
| 파일 | 내용 |
|------|------|
| **DetRng.cs** | **xorshift128+ · SplitMix64 시드 확장 · 천분율 판정 · Clone (TECH-200~205)** |

### 📁 `Grid/` — 거리·이동·방향 ★핵심★
| 파일 | 내용 |
|------|------|
| **GridDistance.cs** | **맨해튼 거리 · 직교 인접 · 범위 판정 (CMB-400/401)** |
| **ReachCalculator.cs** | **균일 코스트 BFS 도달 집합 (CMB-410~413)** |
| FacingMath.cs | 방향 회전 · 피격면(정면/측면/후면) 분류 (CMB-430~433) |
| AoeShape.cs | 스킬 적중 범위 패턴 (CMB-440) |
| Coord.cs · CoordSort.cs · GridEnums.cs · IReachQuery.cs | 좌표 타입 · 정렬 · 열거형 · 맵 조회 인터페이스 |

### 📁 `Formula/` — 전투 수식
| 파일 | 내용 |
|------|------|
| **DamageCalculator.cs** | **명중/피해/경감/모디파이어 처리 순서 (CMB-100~105)** |
| FieldEffectResolver.cs | 층별 필드효과 적용 (CMB-520~522) |
| DamageRequest.cs · DamageResult.cs | 입출력 DTO |

### 📁 `Ai/` — 유틸리티 AI
| 파일 | 내용 |
|------|------|
| **EvalBoard.cs · UtilityScorer.cs** | **정규화·곡선·가중치 스코어링 (AI-100~171)** |
| CandidateGenerator.cs · Candidate.cs · ScoredCandidate.cs | 행동 후보 생성·평가 결과 |
| LookaheadEvaluator.cs | 1-ply 룩어헤드 (AI-032/140) |
| SupportPlanner.cs · SupportPlan.cs | 지원 스킬 판단 |
| Consideration.cs · Blackboard.cs · AiWeights.cs | 판단 요소 · 공유 상태 · 가중치 |

### 📁 `Loop/` — 턴 진행
| 파일 | 내용 |
|------|------|
| **CtbScheduler.cs** | **CT 게이지 기반 행동 순서 (CMB-010/014)** |
| BattleSimulator.cs · TurnResolution.cs | 전투 루프 · 턴 해석 |
| **HeadlessRunner.cs · HeadlessResult.cs** | **엔진 없이 전투 실행 — 밸런스 시뮬용** |
| StatusTurnProcessor.cs | 상태이상 턴 처리 (CMB-202/208) |
| ObjectiveResolver.cs | 승패 목표 판정 (CMB-053) |
| IActionDecider.cs · DummyDecider.cs · BattleResult.cs | 행동 결정 인터페이스 · 더미 · 결과 |

### 📁 `Events/` — 모델-뷰 분리 ★핵심★
| 파일 | 내용 |
|------|------|
| **BattleEvent.cs · IEventSink.cs** | **코어가 외부에 알리는 유일한 통로 (VIEW-001~010)** |
| NullEventSink.cs | 아무것도 그리지 않음 — 밸런스 시뮬용 |
| ListEventSink.cs | 발생 이벤트 수집 — 테스트 검증용 |
| AsciiRenderer.cs | 콘솔 전투판 출력 — 눈으로 확인용 (VIEW-003) |

### 📁 `Model/` — 전투 상태
| 파일 | 내용 |
|------|------|
| **BattleState.cs** | **전투 상태의 단일 진실 공급원 (CMB-040/014)** |
| Unit.cs · StatusEffect.cs · StatusSystem.cs | 유닛 · 상태이상 (CMB-200~208) |
| MoveResolver.cs · BattleReachQuery.cs | 이동 확정 · 도달 조회 구현 (CMB-412) |
| ControlStatus.cs · ModelEnums.cs | 행동 제약 · 열거형 |

### 📁 `Data/` · `Stats/` · `Commands/` · `Constants/`
| 폴더 | 내용 |
|------|------|
| `Data/` | 맵·스킬·상태이상·필드효과 정의 (CMB-200/300/500~521) |
| `Stats/` | 스탯 · 모디파이어 · CP 산출 (GROW-100~132) |
| `Commands/` | 이동/공격/스킬/대기 — 행동 단위 (CMB-300~308) |
| `Constants/` | `IntMath` · `Permille` · 전투/AI 상수 |

## 검증

```bash
dotnet build src/SRPG.Battle.Core/SRPG.Battle.Core.csproj
dotnet test  tests/SRPG.Battle.Core.Tests/SRPG.Battle.Core.Tests.csproj
```

```
빌드: 경고 0개, 오류 0개
테스트: 실패 0, 통과 39, 전체 39
```

테스트는 "돌아간다"가 아니라 **깨지면 게임이 망가지는 것**만 건다.

| 대상 | 거는 것 |
|------|--------|
| `DetRng` | 같은 seed 수열 일치 · `Clone()` 독립성 · 천분율 경계(0‰ / 1000‰) · `NextRange(0)` 예외 |
| `GridDistance` | **대각선이 인접으로 새지 않는지** — 여기가 깨지면 사거리 전체가 틀어진다 |
| `FacingMath` | 델타 동률 시 좌우 우선 · 정면/측면/**후면(백어택)** 분류 |
| `ReachCalculator` | 벽·적 통과 불가 · **아군은 통과 가능·정지 불가** · 도달 집합 (y,x) 정렬 |

## 전투 규칙 요약

- 플레이어는 **배치만** 하고, 전투는 턴제 그리드 위에서 **자동 진행**된다.
- 거리는 전부 **맨해튼**이다. **대각은 인접이 아니다.** (CMB-401)
- 이동은 **균일 코스트 BFS**다. 벽·적은 통과 불가, **아군은 통과는 되나 정지 불가**, 제자리는 항상 후보다. (CMB-410~413)
- 피격면을 **정면 / 측면 / 후면**으로 나누고, 후면 피격에 보정이 붙는다. (CMB-433)

## 진행 상태
- [x] 코어 분리 (엔진 의존 0)
- [x] 결정론 난수 · 그리드 · 이동 BFS 테스트
- [ ] 피해 수식 · 상태이상 테스트
- [ ] AI 스코어링 회귀 테스트
- [ ] 밸런스 시뮬 러너 공개

## 라이선스
MIT
