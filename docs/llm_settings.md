### Profile
LLM Scene -> Canvas -> Information Panel -> Tabs -> 프로필 프리팹들 있음  
프로필 프리팹 클릭 시 보이는 Profile 스크립트에서 설정 가능한 목록:  
ProfileName - 프로필 이름, 이 이름과 나중에 설정될 ScriptableObject의 이름이 같아야 정보 검색 가능.  
InformationString - 표시될 정보 스트링. 프롬프트와는 상관 없음

### Prompt Settings
Scriptable Objects 폴더의 CharacterPrompts  
CharacterPrompts추가 가능  
현재 캐릭터 3개의 프롬프트 있음
**CharacterName** - Profile의 ProfileName과 동일해야 함.  
**GeneralPrompt** - 공통정보, 캐릭터의 자백 여부와 상관 없이 동일한 정보  
**BeforeConfessionPrompt** - 자백하기 전의 프롬프트  
**ConfessionPrompt** - 자백한 후의 프롬프트  
**TriggerPrompt** - 자백모드를 트리거할 정보들. ','로 분리되어야 함.  
**IsConfession** - 현재 자백모드인지에 대한 정보. 게임 시작 전 인스펙터에서 설정 가능 (게임 시작 이후에도 가능하긴 하지만 **토글에선 표시 안됨**. 반대로 **게임 시작 이후에 토글**에서 설정 시 인스펙터에는 반영 됨.)  

### STT 인풋
게임 시작 전, LLM씬의 STTSystem게임 오브젝트 내 STTSystem스크립트 부분을 보면 설정 가능한 부분:  
- SttType - 애저일지 위스퍼일지
- WhisperModel - 위스퍼 모델 사용 시 위스퍼 모델 크기
  반드시 **게임 시작 전 인스펙터**에서 설정해야 함.

마이크 버튼 누르고 위스퍼이면 다시 한번 눌러서 인풋 중지, 애저면 자동 탐지

인풋 필드에 보이는 STT변환 결과에 만족할 시 인풋 시스템에서 엔터 치면 Claude로 전송됨. 