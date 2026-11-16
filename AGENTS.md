# RandomTeamDraw 작업 지침

<!-- ai-development-standard:start -->
## 적용 표준

- [버전 잠금](.development-standard/standard-lock.json)에 고정된 로컬 사본을 사용한다.
- [정책 색인](.development-standard/standards/INDEX.md)에서 작업에 필요한 정책만 읽는다.
- 작업 전 [ARCH](.development-standard/standards/ARCH.md)로 영향을 분류한다.
  L0/L1은 요청 범위에서 진행하고, L2/L3는 구체적인 변경 범위를 제시한 뒤 사전 승인을 받는다.
- 표준 사본을 직접 수정하지 않는다. 갱신·검증·복구는
  [개발 표준 운영](docs/08_개발표준_운영.md)을 따른다.

## 프로젝트 경계와 문서 소유권

- `web/`의 HTML/CSS/JavaScript PWA가 유일한 제품이다. 루트 WPF와
  `Models/`, `Services/`, `ViewModels/`는 비교용 레거시다.
- 서버·계정·DB 없이 브라우저 `localStorage`를 사용한다. 저장 형식, Service Worker
  캐시·업데이트 정책, 배포 경계를 바꾸려면 L2 영향을 검토한다.
- [README](README.md)는 실행법과 제품 구조,
  [기획](docs/01_기획.md)은 제품 의도,
  [마일스톤](docs/02_마일스톤.md)은 완료 조건,
  [로드맵](docs/03_로드맵.md)은 구현 순서를 소유한다.
- UI 작업은 [UI](docs/04_UI.md), 후속 기획은 [향후 기획](docs/05_향후기획.md),
  배정 작업은 [일괄 배정 기획](docs/07_일괄배정_기획.md)을 추가로 읽는다.
- 검증 기준은 [PWA 품질 검증 계획](docs/06_PWA_검증계획.md)이다.
  기존 문서를 유지하며 같은 사실을 담은 빈 project/task/roadmap JSON을 만들지 않는다.

## 작업과 검증

- 요청 범위와 영향 수준을 정하고 필요한 코드·문서만 수정한다.
- JavaScript 수정 시 `node --check web/app.js`와
  `node --check web/service-worker.js`로 문법을 확인한다. Node.js는 개발 검증용이며
  제품 실행 의존성이나 설치 승인을 뜻하지 않는다.
- `python -m http.server 8080 --directory web`은 로컬 실행 명령이며 품질 판정이 아니다.
- Pages 워크플로는 `web/` 배포만 수행한다. CI 성공을 기능 테스트 통과로 보고하지 않는다.
- 설치·오프라인·업데이트·모바일·한글 IME 등 필요한 수동 항목을 검증 계획에 따라 확인한다.
  환경이 없으면 `not_run`과 이유를 기록하고 P3/P4를 완료로 바꾸지 않는다.
- 결과에는 변경 내용, 영향 수준, 실제 검증 명령·판정·남은 제약을 기록한다.
<!-- ai-development-standard:end -->
