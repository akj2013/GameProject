# NPC System

NPC는 두 종류로 나뉜다.

## Worker NPC

자동으로 일을 수행하는 NPC

예:

- 농부
- 광부
- 목수
- 어부

행동 루프:

Idle
→ MoveToResource
→ Work
→ CarryResource
→ Deliver
→ Repeat

## Social NPC

플레이어와 상호작용하는 NPC

예:

- 상인
- 관리
- 왕
- 승려

기능:

- 대화
- 상점
- 퀘스트 제공

## NPC 구조

NPC
 ├ WorkerNPC
 │   ├ Farmer
 │   ├ Miner
 │   └ Lumberjack
 │
 └ SocialNPC
     ├ Merchant
     ├ Official
     └ King


## NPC System Status

NPCs are not implemented yet.

Planned NPC roles:

Worker NPC
- Lumberjack
- Miner
- Farmer

Social NPC
- Merchant
- Village chief
- King

Future NPC behaviors:

- Autonomous resource harvesting
- Resource transportation
- Trading interaction
- Dialogue and quests

## 추가 아이디어 메모

- Worker NPC의 행동 우선순위를 \"플레이어가 보기 재밌는 것\" 기준으로 조정해, 멀리서 바라만 봐도 살아있는 마을 느낌을 준다.
- Social NPC는 단순 상점/대화뿐 아니라, 타일 확장 목표를 자연스럽게 안내하는 가이드 역할을 맡길 수 있다.
- 초기 버전에서는 역할이 겹치는 NPC 수를 줄이고, 소수의 NPC에 개성을 집중시키는 편이 작업량 대비 효과가 크다.