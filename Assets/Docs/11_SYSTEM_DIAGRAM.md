# System Architecture Diagram

This document describes the core architecture of the current gameplay prototype.

The game is built around a tile-based world system.

---

# High Level Architecture

Player
↓
Tile Interaction
↓
Tile Unlock System
↓
Resource Spawn
↓
Harvest System
↓
Respawn System

---

# Core Systems

## World System

SquareGridManager  
Responsible for generating the tile grid.

Functions:
- Create 10x10 grid
- Manage tile positions
- Provide tile lookup
- Provide neighbor queries

TileController  
Controls individual tile behavior.

Responsibilities:
- Locked / Unlocked state
- Cloud visual
- Player trigger detection
- Resource spawner activation

TileUnlockSystem  
Handles unlocking rules.

Responsibilities:
- Check adjacency rules
- Calculate unlock cost
- Show unlock UI
- Save unlocked tiles

---

# Resource System

TileResourceSpawner  
Spawns resources on unlocked tiles.

Responsibilities:
- Spawn initial resources
- Track spawned resources
- Handle respawn after depletion

ResourceNode  
Attached to each resource object.

Responsibilities:
- Health system
- Depletion logic
- Respawn trigger

---

# Player System

PlayerInventory  
Stores player currency.

Responsibilities:
- Gold tracking
- Spend gold
- Add gold

PlayerHarvest  
Handles player resource interaction.

Responsibilities:
- Detect resource
- Apply damage
- Trigger resource depletion

---

# UI System

UnlockPanelUI

Displays unlock interface.

Features:
- Tile position display
- Unlock cost
- Unlock button
- Close button

---

# Save System

Uses Unity PlayerPrefs.

Saved data:

Unlocked tile coordinates.

Example:

{
  "tiles": [
    { "x":5, "y":5 },
    { "x":5, "y":6 }
  ]
}

Save location (Windows):

HKEY_CURRENT_USER\Software\<CompanyName>\<ProductName>

---

# Current Gameplay Loop

Player explores map
↓
Player approaches locked tile
↓
Unlock UI appears
↓
Player pays gold
↓
Cloud disappears
↓
Resources spawn
↓
Player harvests resources
↓
Resources respawn
↓
Player unlocks new tiles

This loop forms the core progression of the game.

---

# Current Prototype Status

The following systems are already implemented and functional:

- Tile grid generation
- Tile unlock system
- Resource spawning
- Harvest interaction
- Resource respawn
- Save/load system
- Unlock UI

This version represents the **Core Gameplay Prototype**.