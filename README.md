# UnityPortfolioScripts

Unity 클라이언트 개발 포트폴리오에 기재된 내용을 뒷받침하는 스크립트 모음입니다.
실제 프로젝트 코드에서 구조가 드러나는 부분만 발췌·요약한 것이므로 **내용 확인 용도로만** 봐주세요.
(단독으로 빌드·실행되지 않으며, 의존하는 매니저/테이블 클래스 등은 포함되어 있지 않습니다.)

## 폴더 구성

| 폴더 | 설명 |
| --- | --- |
| `AssetDependencyTool` | 빌드 전 에디터 스크립트. 모든 UI 프리팹을 순회하며 각 프리팹이 사용하는 스프라이트가 어느 SpriteAtlas에 속하는지 조회해 "UI 프리팹 → 의존 아틀라스 목록"을 JSON으로 저장하고, 런타임에서 같은 파일을 읽어 재사용합니다. |
| `BuildAutomation` | Jenkins가 커맨드라인 인자로 호출하는 Editor 빌드 진입점. Android APK, iOS 빌드와 PBXProject 후처리, 에셋번들 빌드/업로드의 대표 흐름을 담았습니다. |
| `ObserverMachine` | 인터페이스 기반 옵저버 패턴(`ObserverPattern`)과 이를 이용한 유저 데이터 이벤트(`UserObserver`), UI 이벤트(`UIObserver`) 채널, 그리고 이를 구독하는 레드닷(알림 뱃지) 시스템. |
| `UserData_MVC` | 유저 진행 상태(Model)를 도메인별 모듈로 나눠 등록·조회하는 `UserGameData`, View가 Model을 직접 참조하지 않게 하는 Facade, 테이블 데이터를 "조건"으로 통일해 판정하는 `UserCondition`. |
| `UI` | UI 매니저와 레이어(Canvas) 구조, 패널 기반 클래스, 재화 표시/아이템 바인더, 개수 선택 팝업용 로직(`IUILogicOpenCounter`). |
| `Popup` | 팝업 생성·캐싱을 담당하는 `PopupManager`, 조건부 예약 생성(`PopupCreator`/`PopupReserve`), 그리고 실제 팝업 구현 예시(각성, 의상, 펫, 이벤트, 인벤토리, 시즌패스 등). |

## 주요 설계 포인트

- **옵저버 기반 이벤트 전파** — 이벤트를 `E_UserMessageGroup` 그룹 단위로 나눠 필요한 그룹만 구독할 수 있게 했고, 페이로드(`Parameter`)는 박싱 대신 고정 크기 버퍼로 전달해 GC 부담을 줄였습니다.
- **레드닷 시스템** — 활성화 조건을 `UIRedDotChecker`(Strategy)로 분리하고, `UIRedDotMultiChecker`(Composite)로 여러 조건을 조합합니다.
- **유저 데이터 모듈화** — `BaseUserData` 하위 모듈을 타입으로 등록·조회하고, 모든 등록이 끝난 뒤 일괄 `Initialize` 하여 모듈 간 초기화 순서 문제를 막았습니다.
- **조건 판정 통일** — 퀘스트·시스템 오픈 등 서로 다른 테이블이 `IConditionData`를 구현하고, `UserCondition`이 테이블 타입별로 라우팅합니다.
- **팝업 관리** — 캐시 대상 팝업 재사용, 중복 오픈 방지, 조건이 충족될 때 생성되는 예약 팝업 구조.
- **빌드 파이프라인** — Jenkins 인자 파싱부터 플랫폼별 빌드, 에셋번들 빌드·업로드까지 `IBuildWorker` 패턴으로 구성.
