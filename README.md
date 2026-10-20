# RandomTeamDraw

서버 없이 실행되고 Windows에도 설치할 수 있는 정적 PWA로 전환 중인 랜덤 조 편성 앱입니다.
기존 C# / WPF 버전은 PWA 전환 완료 전까지 동작 비교용 레거시로 유지합니다.

## 웹 버전

`web/` 폴더는 HTML/CSS/JavaScript만 사용하는 정적 웹앱입니다. 참가자 명단, 마지막 배정,
조 이름과 조별 가중치를 현재 브라우저의 `localStorage`에 저장합니다. 서버나 데이터베이스는
사용하지 않으며, 다른 브라우저나 기기와 데이터가 자동으로 공유되지 않습니다.

로컬에서는 `web/index.html`을 직접 열어도 되고, 개발용 서버가 있다면 다음처럼 실행할 수 있습니다.

```powershell
python -m http.server 8080 --directory web
```

GitHub Pages 자동 배포 워크플로가 `.github/workflows/deploy-pages.yml`에 포함되어 있습니다.
GitHub 저장소의 **Settings → Pages → Build and deployment → Source**를 **GitHub Actions**로
한 번 지정한 뒤 `main` 브랜치에 push하면 웹 버전이 자동 배포됩니다.

## 주요 기능

- 참가자를 현재 인원이 가장 적은 조에 배정하고, 동률이면 암호학적 난수로 선택
- 참가자 이름 수정과 즉시 삭제(기존 참가자의 조는 유지)
- 마지막 배정 참가자를 조 현황 상단에 크게 표시하고 목록에서도 강조
- 실행 파일 옆 `config.json`에서 초기 1~4조 이름 지정
- `설정` 버튼 또는 `Ctrl+Shift+W`로 조 이름과 조별 배정 가중치 설정
- 모든 변경을 `%LOCALAPPDATA%\RandomTeamDraw\session.json`에 자동 저장
- 임시 파일 교체, 이전 정상 파일 백업, 손상 파일 보존 및 백업 복구
- 확인 후 전체 초기화

## 개발 및 실행

Windows에서 .NET 10 SDK가 필요합니다.

```powershell
dotnet build RandomTeamDraw.csproj
dotnet run --project RandomTeamDraw.csproj
```

Windows x64 self-contained 게시:

```powershell
dotnet publish RandomTeamDraw.csproj -c Release -r win-x64 --self-contained true
```

현재 생성된 배포본은 `dist/RandomTeamDraw-win-x64/`에 있으며, 전달용 압축 파일은
`dist/RandomTeamDraw-v1.1.0-win-x64.zip`입니다. 배포 산출물은 Git에 포함하지 않습니다.

## 조 이름 설정

앱을 종료한 뒤 실행 파일과 같은 폴더의 `config.json`을 편집하고 다시 실행합니다.
각 이름은 공백이 아닌 20자 이하 문자열이어야 하며, 잘못된 값은 기본 조 이름으로 대체됩니다.

```json
{
  "team1Name": "1조",
  "team2Name": "2조",
  "team3Name": "3조",
  "team4Name": "4조"
}
```

## 조별 배정 가중치

메인 창 하단의 `설정` 버튼을 누르거나 `Ctrl+Shift+W`를 누르면 운영자용 보조
설정창이 열립니다. 여기서 조 이름과 배정 가중치를 함께 변경할 수 있으며 세션에 자동
저장됩니다. 가중치 값 자체는 메인 화면에 표시되지 않습니다. 가중치는
`현재 인원 + 가중치`로 계산되므로 `-1`은 해당 조에 한 명을 더, `+1`은 한 명을 덜
배정하는 효과가 있습니다. 설정 범위는 -10~+10이며 세션 데이터와 함께 자동 저장됩니다.

## 구조

단일 WPF 프로젝트이며 `Models`, `Services`, `ViewModels`, `MainWindow`만 사용하는 작은 MVVM 구조입니다. 별도 Core/API/테스트 프로젝트는 없습니다.

개발 기준 문서는 `docs/` 폴더에 있습니다.

- [기획](docs/01_기획.md)
- [마일스톤](docs/02_마일스톤.md)
- [로드맵](docs/03_로드맵.md)
- [UI](docs/04_UI.md)

## 현재 검증

- .NET SDK 10.0.302에서 Debug/Release 빌드 성공(경고 0, 오류 0)
- Windows에서 앱 시작 및 메인 창 응답 확인
- 최소 인원 배정, 5명 이상 균형, 삭제 후 빈 조 우선 배정 확인
- 중복 방지, 이름 수정 시 조 유지, 최근 참가자 강조 상태 확인
- JSON 저장/복원, 손상된 주 파일의 백업 복구·원본 보존, 초기화 후 빈 백업 유지 확인
- 화면 자동화 연결 제한으로 실제 마우스 조작과 한글 IME 조합은 수동 확인 필요
