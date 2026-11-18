# 📜 프로젝트 대화 및 작업 히스토리 (Chat & Dev History)

이 문서는 Antigravity IDE 프로그램이 재시작되거나 새로운 대화 세션이 열리더라도, 이전 대화 맥락과 작업 내역을 영구적으로 보존하고 기억하기 위해 자동으로 유지관리되는 기록 문서입니다.

---

## 📌 프로젝트 정보: 무선 네트워크 선택
- **위치**: c:\Users\SH-3546\Desktop\오정환\Antigravity\무선 네트워크 선택
- **주요 파일**: AGENTS.md, app.ico, AutoNet.exe, AutoNet_original.exe.bak, build.bat, GEMINI.md, Pseudocode_시큐어코딩_가이드.md, tray_running.ico

---

## 📅 세션 기록

### [초기화] 히스토리 영구 보존 체계 적용
- **일시**: 2026-10-15
- **내용**: Antigravity 전역 및 프로젝트별 대화/작업 히스토리 영구 보존 시스템 활성화 완료.
- **다음 작업**: 사용자와의 대화 및 개발 요청 사항이 발생하면 여기에 상세 작업 내용과 맥락이 자동으로 지속 기록됩니다.


### [UI 단순화 및 저빈도 기능 정리]
- **일시**: 2026-11-03
- **요청**: 사용량이 적은 기능을 제거하고 UI를 단순하게 수정
- **주요 변경 내역**:
  - **저빈도 기능 제거**: 핑(Ping) 테스트 카드, 정밀 진단 팝업(`ShowDiagnosticsDialog`), 초소형 모드(`SetMiniMode`), AP 이름 수동 변경(`ShowRenameProfileDialog`), 종료 확인 팝업(`PromptExitOrMinimize`) 제거
  - **UI 통합 및 컴팩트화**: 창 높이를 `425px` → `316px`로 축소, 상단 우측 최소화(`—`)/종료(`✕`) 버튼 적용, `[고정 IP 설정]` · `[DHCP 자동]` 병렬 배치 및 `[저장된 AP 프로필 관리 (N개)]` 단일 버튼으로 통합
  - **빌드 검증**: `AutoNet.exe` 정상 빌드 완료.

### [저장된 AP 프로필 이름 변경 기능 복원]
- **일시**: 2026-11-03
- **요청**: 저장된 AP 네트워크 프로필 메뉴에서 리스트의 이름을 변경 가능하게 수정
- **주요 변경 내역**:
  - `src/MainForm.Dialogs.cs`의 `ShowSavedProfilesDialog` 내 프로필 카드에 연필(`pencil`) 수정 버튼 및 이름 더블클릭 이벤트를 복원하고, `ShowRenameProfileDialog`를 통해 실시간 유효성 검증 후 이름을 변경·저장하도록 적용
  - `AutoNet.exe` 재빌드 완료.

### [저장된 AP 자동 연결 및 IP 자동 할당 로직 강화]
- **일시**: 2026-11-03
- **요청**: 저장된 AP가 있을 시 자동으로 연결 및 IP 할당되게 수정
- **주요 변경 내역**:
  - **주변 저장 AP 자동 Wi-Fi 연결**: Wi-Fi 미연결 상태에서 주변 Wi-Fi 스캔 시 저장된 AP 프로필(원본 SSID 및 변경된 별칭 모두 매칭)이 감지되면 자동으로 `netsh wlan connect` 연결 수행
  - **IP 자동 할당 누락 버그 수정**: 기존에 Wi-Fi 직후 IP가 `0.0.0.0`일 때 고정 IP 할당이 건너뛰어지던 조건을 수정하여, 연결된 AP의 현재 IP 또는 게이트웨이가 저장된 프로필과 다르면 즉시 고정 IP/서브넷/게이트웨이/DNS가 할당되도록 개선 (`validate=no` 옵션으로 DNS 설정 지연 방지)
  - **별칭 변경 시 원본 SSID 보존**: `ApProfile`에 `OriginalSsid` 필드를 추가하여 리스트에서 이름을 변경해도 실제 공유기 SSID 자동 연결 및 IP 할당이 정상 작동하도록 개선

### [닫기(종료) 버튼 클릭 시 트레이 최소화 동작 적용]
- **일시**: 2026-11-03
- **요청**: 프로그램 실행 시 종료 버튼 클릭해도 종료되지 않고 최소화되어 실행되게 수정
- **주요 변경 내역**:
  - 상단 우측 `✕` 버튼(`btnHeaderClose`), `Alt+F4`, 작업표시줄 닫기(`CloseReason.UserClosing`) 시 프로그램이 종료되지 않고 작업표시줄에서 숨겨지며 시스템 트레이로 최소화되어 백그라운드 실행을 유지하도록 수정 (`MainForm.cs`, `MainForm.Designer.cs`)
  - 트레이 아이콘 우클릭 메뉴의 `종료`를 선택할 때만 완전 종료되도록 `allowExit` 플래그 적용
  - `AutoNet.exe` 재빌드 완료.

### [윈도우 시작 시 자동 실행 오류 수정 및 즉시 등록]
- **일시**: 2026-11-03
- **요청**: 윈도우 시작 시 자동으로 실행되도록 수정
- **주요 변경 내역**:
  - **원인 해결**: 기존 레지스트리(`HKCU\...\Run`) 및 작업 스케줄러(`AutoNet`)에 `AutoNet.exe`가 아닌 `powershell.exe --autostart`가 잘못 등록되어 있었고, 노트북 배터리 모드 실행 제한(`DisallowStartIfOnBatteries=true`)이 걸려 있던 문제 해결
  - **`StartupHelper.cs` 개선**:
    - 실행 경로가 현재 `AutoNet.exe`와 정확히 일치하는지 검증하여 항상 올바른 경로로 갱신
    - 작업 스케줄러 XML 등록 시 배터리 모드에서도 자동 시작 허용(`DisallowStartIfOnBatteries=false`, `StopIfGoingOnBatteries=false`), 실행 시간 제한 해제(`PT0S`) 적용
  - **`Program.cs` 개선**: UAC 권한 승격 시 `--autostart` 인자 유지 및 부팅 시 이중 실행 알림 팝업 방지
  - 현재 PC의 레지스트리 시작프로그램 항목도 즉시 `AutoNet.exe --autostart`로 등록 완료.

### [타 PC 최초 실행 시 윈도우 시작프로그램 자동 등록 호환성 강화]
- **일시**: 2026-11-03
- **요청**: 다른 PC에서 사용할 때 처음 실행하면 윈도우 시작 시 자동으로 실행되게 수정
- **주요 변경 내역**:
  - **작업 스케줄러 XML 표준 스키마(XSD) 준수 및 범용 SID 적용**: `<Triggers>` → `<Principals>` 순서 준수 및 모든 윈도우 언어/계정에 공통 동작하는 `Users` 그룹 SID(`S-1-5-32-545`)를 적용해 다른 PC에서도 오류 없이 즉시 작업 스케줄러 등록 보장
  - **3중 자동 시작 등록 (`StartupHelper.cs`, `Program.cs`)**:
    1. UAC 권한 승격 전 현재 로그인 계정의 `HKCU\...\Run` 선제 등록 (일반 계정과 관리자 계정이 분리된 기업용 PC 대응)
    2. 관리자 권한 진입 후 작업 스케줄러(`HighestAvailable`, 배터리 제한 해제) 등록
    3. `HKLM\...\Run` 및 `HKCU\...\Run` 동시 등록 및 실행 파일 경로 이동 시 자동 갱신
  - `AutoNet.exe` 재빌드 완료.

### [프로그램 사용 매뉴얼 작성]
- **일시**: 2026-11-03
- **요청**: 프로그램 사용 매뉴얼 제작
- **주요 변경 내역**:
  - 프로젝트 루트에 `사용_매뉴얼.md` 생성 (첫 실행 및 자동 시작, 화면 구성, 고정 IP/DHCP 등록, 자동 연결·할당 원리, 프로필 이름 변경/삭제, 트레이 완전 종료 방법 정리)

### [프로그램 매뉴얼 시각 이미지 생성 및 첨부]
- **일시**: 2026-11-03
- **요청**: 프로그램 매뉴얼 내용에 따라 이미지 첨부
- **주요 변경 내역**:
  - `manual_images/` 폴더에 매뉴얼 각 섹션별 번호 안내 콜아웃이 포함된 고해상도 UI 이미지 4종(`01_main_window.png`, `02_set_ip_dialog.png`, `03_saved_profiles.png`, `04_tray_and_ap_menu.png`) 생성 및 `사용_매뉴얼.md`에 삽입 완료.

### [단일 HTML 파일 매뉴얼 생성]
- **일시**: 2026-11-03
- **요청**: 매뉴얼을 HTML 파일로 제작
- **주요 변경 내역**:
  - 4종의 매뉴얼 이미지를 Base64로 내장한 단독 실행형 웹 매뉴얼 `사용_매뉴얼.html` 생성 완료 (별도 이미지 폴더 없이 HTML 파일 하나만 다른 PC로 전달해도 모든 이미지가 정상 표시됨)

### [GitHub 원격 저장소 생성 및 프로젝트 업로드]
- **일시**: 2026-11-03
- **요청**: GitHub에 프로젝트 업로드
- **주요 변경 내역**:
  - `.gitignore` 및 `README.md` 추가
  - Git 저장소 초기화(`main` 브랜치) 및 GitHub 원격 저장소(`https://github.com/quajimoto/AutoNet`) 생성·푸시 완료

### [전체 UI 디자인 리뉴얼]
- **일시**: 2026-11-09
- **요청**: 폴더 내용을 분석해서 보다 더 나은 디자인으로 변경
- **주요 변경 내역**:
  - **공통 디자인 시스템 도입**: `src/Theme.cs`(색상·글꼴·둥근 모서리 헬퍼, Windows 11 DWM 둥근 창 적용) 및 `src/ThemedControls.cs`(`RoundedPanel`, `ModernButton`, `StatusDot`, `PillLabel`, `ThemedInput`, `ThemedDialog`) 추가. 하드코딩 색상을 토큰으로 통일하고 한글 가독성을 위해 UI 글꼴을 `Malgun Gothic`으로 변경
  - **메인 창 재구성** (`MainForm.Designer.cs`, 340×366): 그라데이션 로고 헤더 + AUTO/MANUAL 알약형 토글, SSID·IP·GW·DNS를 하나의 둥근 연결 카드로 통합, SSID 아래 연결 상태 점/문구(연결됨·고정 IP 프로필 / 미등록 AP / 연결 안 됨) 신설(`UpdateConnectionStatus`), 게이트웨이·DNS 2열 타일, 그라데이션 기본 버튼, 프로필 관리 행에 `›` 표시
  - **다이얼로그 통일** (`MainForm.Dialogs.cs`): 고정 IP 설정·저장된 프로필·이름 수정·알림 창을 `ThemedDialog`로 재작성(둥근 창, 그림자, 아이콘 제목 바, ESC 닫기). 입력칸은 포커스/오류 시 테두리 색 변화, 프로필 카드는 둥근 카드 + 모드 배지 + 휴지통 아이콘. Wi-Fi 선택 메뉴에 아이콘(현재 연결 ✓, 저장된 AP 강조) 추가
  - **트레이/팝업 메뉴**: `DarkMenuRenderer`를 테마 색상 및 둥근 선택 하이라이트로 갱신
  - 기능 로직(자동 연결, IP 할당, 프로필 저장 등)은 변경 없음. 아이콘 `chevron-right`, `check` 추가
  - `AutoNet.exe` 재빌드 완료 (미리보기 하네스로 메인 창·다이얼로그 렌더링 확인, 실제 관리자 권한 실행은 미검증)

### [사용 매뉴얼 v2.0 — 새 디자인 반영]
- **일시**: 2026-11-09
- **요청**: 사용 매뉴얼의 화면 이미지(manual_images/)와 매뉴얼 내용을 디자인 개선에 맞게 버전업
- **주요 변경 내역**:
  - **화면 이미지 4종 교체** (`manual_images/01~04`): 실제 새 UI를 샘플 데이터로 렌더링한 스크린샷에 번호 콜아웃·점선 안내를 합성(2배 해상도). 메인 화면은 연결 상태 표시가 추가되어 콜아웃 ①~⑦로 변경
  - **`사용_매뉴얼.md` v2.0**: 연결 상태 표시(초록/노랑/회색) 표 신설, 고정 IP 창의 대상 AP 표시·빨간/노란 테두리 검증·Esc 취소, 프로필 모드 배지·휴지통 삭제·Enter 저장, Wi-Fi/트레이 메뉴 아이콘·체크 표시 안내 추가, `✕`/`▾` 표기를 `×`/`▼`로 정리, 매뉴얼 변경 이력 섹션 추가
  - **`사용_매뉴얼.html` 재생성**: 기존 스타일 유지, v2.0 본문과 새 이미지 4종 Base64 내장
  - **`README.md`**: 화면 구성·사용 방법 섹션을 매뉴얼 v2.0과 동기화하고 전체 매뉴얼 링크 추가
  - **버그 수정**: 트레이 메뉴가 이미지 여백을 숨겨 `[윈도우 시작 시 자동 실행]` 체크 표시가 보이지 않던 문제 수정(여백 표시 + 테마 체크 아이콘 렌더링), 트레이 메뉴 항목 아이콘 추가 (`MainForm.Designer.cs`, `DarkMenuRenderer.cs`). `AutoNet.exe` 재빌드

### [Superpowers 플러그인 설치]
- **일시**: 2026-11-11
- **요청**: `/plugin install superpowers@claude-plugins-official` 로 Superpowers 설치
- **주요 변경 내역**:
  - 이 PC에는 `claude-plugins-official` 마켓플레이스가 등록되어 있지 않아, 등록된 `anthropic-plugin-directory` 마켓플레이스에서 `superpowers` 플러그인을 사용자 범위(user scope)로 설치 완료. 프로젝트 코드 변경 없음

### [claude-plugins-official 마켓플레이스 등록]
- **일시**: 2026-11-11
- **요청**: 이 PC에 `claude-plugins-official` 마켓플레이스 등록
- **주요 변경 내역**:
  - `claude plugin marketplace add anthropics/claude-plugins-official`로 GitHub 공식 마켓플레이스를 사용자 설정에 등록 완료. 이제 `anthropic-plugin-directory`(내장)와 `claude-plugins-official` 두 마켓플레이스 사용 가능. 프로젝트 코드 변경 없음

### [Superpowers 설치 재실행 (claude-plugins-official)]
- **일시**: 2026-11-11
- **요청**: 이전에 실패한 `superpowers@claude-plugins-official` 설치 재실행
- **주요 변경 내역**:
  - 마켓플레이스 등록 후 재설치 실행. 동일 플러그인(obra/superpowers)이 `anthropic-plugin-directory`에서 이미 설치되어 있어 `--replace`로 설치 출처를 `claude-plugins-official`로 전환 (버전 6.4.2 → 7.0.0, 플러그인 설정 유지, user scope, 활성화 상태). 프로젝트 코드 변경 없음

### [Karpathy Skill 설치]
- **일시**: 2026-11-11
- **요청**: Karpathy Skill 설치
- **주요 변경 내역**:
  - 사용자 선택에 따라 GitHub 커뮤니티 저장소 `forrestchang/andrej-karpathy-skills`를 마켓플레이스(`karpathy-skills`)로 등록하고 `andrej-karpathy-skills@karpathy-skills` v1.0.0을 user scope로 설치 (스킬 `karpathy-guidelines`: Think Before Coding, Simplicity First, Surgical Changes, Goal-Driven Execution). 설치 전 확인 결과 Markdown 지침 파일만 있고 훅·스크립트 없음. 프로젝트 코드 변경 없음

### [Ponytail 설치 시도]
- **일시**: 2026-11-11
- **요청**: Ponytail 설치
- **주요 변경 내역**:
  - 원본 저장소 `DietrichGebert/ponytail`을 마켓플레이스(`ponytail`)로 등록 완료
  - 훅 검토: SessionStart/SubagentStart/UserPromptSubmit에서 Node 스크립트 실행(로컬 `git ls-files` 등), 작업 기록을 `~/.config/ponytail/band/`에 CSV로 저장, 웹훅은 URL 설정 시에만 전송(기본 비어 있음)
  - `claude plugin install ponytail@ponytail` 실행은 자동 모드 권한 분류기에 의해 차단됨(외부 코드 통합) → 사용자가 직접 설치 명령을 실행해야 함. 플러그인 미설치 상태. 프로젝트 코드 변경 없음

### [Ponytail 설치 완료 확인]
- **일시**: 2026-11-11
- **주요 변경 내역**:
  - 사용자가 직접 `claude plugin install ponytail@ponytail` 실행 → `ponytail@ponytail` v5.3.3 user scope 설치·활성화 확인. 현재 설치 플러그인: andrej-karpathy-skills 1.0.0, ponytail 5.3.3, superpowers 7.0.0

### [Unslop UI 설치 시도]
- **일시**: 2026-11-11
- **요청**: Unslop AI 설치 → 사용자 선택: 디자인 퀄리티 개선용
- **주요 변경 내역**:
  - 디자인용 Unslop은 `yuwen-lu/unslop-ui`(MIT, v1.0.0)로 확인. 저장소 검토 결과 SKILL.md(UI 안티패턴 규칙: 동일 색조 남용, 둥근 사각형 아이콘, 이모지 남용, 글래스모피즘, 과한 그라데이션·그림자 등)와 예시 이미지뿐, 훅·스크립트 없음
  - 마켓플레이스 등록·설치 명령은 자동 모드 권한 분류기에 의해 차단됨 → 사용자가 직접 실행 필요. 미설치 상태. 프로젝트 코드 변경 없음

### [Unslop UI 설치 확인 — 미설치]
- **일시**: 2026-11-11
- **주요 변경 내역**:
  - 확인 결과 `unslop-ui` 마켓플레이스 미등록, 플러그인 미설치 상태. 사용자에게 두 명령(`marketplace add yuwen-lu/unslop-ui`, `install unslop-ui@unslop-ui`) 재실행 안내

### [Unslop UI 설치 완료 확인]
- **일시**: 2026-11-11
- **주요 변경 내역**:
  - 사용자가 직접 설치 명령 실행 → `unslop-ui@unslop-ui` v1.0.0 user scope 설치·활성화 확인. 현재 설치 플러그인: andrej-karpathy-skills 1.0.0, ponytail 5.3.3, superpowers 7.0.0, unslop-ui 1.0.0

### [gstack 설치 검토]
- **일시**: 2026-11-11
- **요청**: gstack 설치
- **주요 변경 내역**:
  - 원본 `garrytan/gstack`을 임시 폴더에 받아 검토. 마켓플레이스 플러그인이 아니라 `~/.claude/skills/gstack`에 clone 후 `setup.ps1` 실행 방식
  - setup이 하는 일: Bun 필요(이 PC 미설치 → `winget install --id Oven-sh.Bun -e`), Playwright Chromium 다운로드, 로컬 바이너리 빌드, 전역 `~/.claude/settings.json`에 훅 등록(백업 생성), 업데이트 확인 기본 켜짐, 텔레메트리는 기본 꺼짐(최초 1회 질문). Windows는 "최소 지원" 등급, Smart App Control 켜져 있으면 일부 바이너리 차단
  - 시스템 패키지 설치·전역 훅 등록이 포함되어 직접 실행하지 않고 사용자에게 설치 명령 안내(`--prefix` 권장: 기존 Superpowers 등과 명령 이름 충돌 방지). 미설치 상태. 프로젝트 코드 변경 없음

### [gstack 설치 확인 — 미설치]
- **일시**: 2026-11-11
- **주요 변경 내역**:
  - 확인 결과 Bun 미설치, `~/.claude/skills/gstack` 폴더 없음, settings.json에 gstack 훅 없음 → 1단계(Bun 설치)부터 진행되지 않은 상태. 사용자에게 재안내

### [gstack 설치 완료 확인]
- **일시**: 2026-11-11
- **주요 변경 내역**:
  - Bun 1.4.4는 winget으로 설치되어 있었으나 기존 PowerShell 창에 PATH가 반영되지 않아 `bun` 인식 실패 → 새 창/PATH 갱신 안내
  - 사용자가 설치 완료 → `~/.claude/skills/gstack` v1.99.0.0, `gstack-` 접두사 스킬 62개 + `_gstack-command` 설치, browse 바이너리(`browse.exe`, `find-browse.exe`) 빌드 확인. 전역 settings.json에는 gstack 훅 항목 없음. `setup --status`는 자동 모드에서 실행 차단되어 미확인

### [gstack 상태 점검 결과]
- **일시**: 2026-11-11
- **주요 변경 내역**:
  - 사용자 터미널의 `setup.ps1 --status` 결과 확인: claude/global/current 1.99.0.0 (스킬 `gstack-` 접두사 61개 — 이전 기록의 62개는 오기)
  - 훅: Stop 훅(`gstack-timeline-stop`)만 전역 settings.json에 등록됨. plan-tune 훅 2개와 pre-push 자격증명 가드는 응답 없음으로 건너뜀
  - 문제 1건: 섹션 링크 114개 중 1개 깨짐(`gstack-review/SKILL.md` → `../qa/sections/exploratory.md`, 접두사 설치로 경로가 `gstack-qa`가 된 탓으로 추정). Codex 미설치로 외부 리뷰(codex_reviews=enabled)는 사용 불가

### [저장된 AP 프로필 더블클릭 시 설정 즉시 적용]
- **일시**: 2026-11-18
- **요청**: 저장된 AP 프로필 관리 메뉴에서 리스트 중 하나를 선택 후 더블클릭하면 해당 설정으로 변경
- **주요 변경 내역**:
  - `MainForm.Dialogs.cs`: 프로필 카드 클릭 시 선택 강조(파란 테두리), 카드(이름·배지·정보 포함) 더블클릭 시 해당 프로필 적용 후 창 닫기. 기존 "이름 더블클릭 = 이름 수정"은 충돌하므로 제거하고 이름 수정은 연필 아이콘으로만 가능
  - `MainForm.Network.cs`: `ApplySavedProfile` 추가 — 현재 연결된 AP의 프로필이면 고정 IP/DHCP 즉시 적용, 다른 AP면 먼저 연결 후 실제 연결 SSID를 재확인하고 적용(연결 실패 시 다른 네트워크에 잘못된 IP가 들어가지 않도록 취소). MANUAL 모드에서도 동작. `ConnectToSsid`에 연결 후 콜백 인자(선택) 추가
  - 매뉴얼 v2.1: `사용_매뉴얼.md`/`.html`, `README.md` 설명 갱신, `manual_images/03_saved_profiles.png` 콜아웃 문구 수정
  - `AutoNet.exe` 재빌드 (선택 강조는 미리보기로 확인, 실제 IP 적용은 관리자 권한 실행으로 미검증)

### [프로필 더블클릭 적용이 안 되는 문제 분석 및 수정]
- **일시**: 2026-11-18
- **요청**: 저장된 AP 프로필을 선택 후 더블클릭해도 변경되지 않음 — 분석 및 해결
- **원인 (autonet.log / autonet_profiles.txt 분석)**:
  1. 프로필 `회사`의 실제 SSID 필드가 `회사`로 저장되어 있었음(`회사|static|10.101.3.191|...|회사`). 실제 Wi-Fi 이름이 없어 연결 실패 → 적용 취소(`프로필 적용 취소: '회사' 연결 실패`). 실제 SSID 필드가 생기기 전 구버전에서 이름을 바꾼 데이터로 추정
  2. 성공/실패가 로그에만 남아 화면상 아무 반응이 없어 보였음 (`SHC_Guest`(DHCP) 더블클릭은 실제로 적용됐으나 이미 DHCP라 변화 없음)
  3. 수동 연결 직후 잠깐 끊긴 사이에 주기적 자동 연결이 첫 번째 저장 AP(`SHC_Guest`)를 잡아 수동 적용이 취소될 수 있는 경쟁 상태
- **조치**:
  - 사용자 확인에 따라 `%LocalAppData%\AutoNet\autonet_profiles.txt`의 `회사` 프로필 실제 SSID를 `SHC`로 수정 (백업: `autonet_profiles.txt.bak`)
  - `ApplySavedProfile`: 적용 성공/연결 실패를 트레이 알림으로 표시
  - `ConnectToSsid`: 수동 연결 시 자동 연결을 약 18초 유예
  - `AutoNet.exe` 재빌드 (실제 실행 검증은 사용자 확인 필요)
