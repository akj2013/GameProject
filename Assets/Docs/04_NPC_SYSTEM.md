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