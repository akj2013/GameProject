# Economy & Trade System

This document defines the economic system used in the game.

Unlike most mobile games, this project does **not use a gold-based economy**.  
Instead, the game uses a **resource-based barter system** where players exchange materials directly with NPC traders.

The main goal of this system is:

- Prevent early-game resources from becoming useless
- Encourage players to revisit earlier regions
- Enable simple but interesting trade mechanics
- Support world expansion with regional specialization


---

# Core Philosophy

Most resource collection games follow this pattern:

Resources → Gold → Purchases

This project uses a different structure:

Resources → Trade → Other Resources

Example:

Wood → Stone → Iron → Tools


---

# Resource-Based Economy

All economic activity revolves around **resource exchange**.

Example exchange values:

| Resource | Exchange |
|--------|--------|
Wood | 2 Stone
Stone | 1 Iron
Rice | 10 Wood
Wheat | 5 Rice

This creates a **resource chain progression** instead of a single currency.


---

# Resource Tiers

Resources are grouped by tier.

Tier 1 resources are common and appear in early maps.  
Higher tiers appear in later regions.

Tier 1
- Wood
- Stone
- Rice

Tier 2
- Iron
- Wheat
- Vegetables

Tier 3
- Steel
- Tea
- Silk

Tier 4
- Rare cultural resources
- Regional special items


---

# Regional Resources

Each region introduces **unique local resources**.

Example:

## Korea

- Rice
- Ginseng
- Garlic
- Pepper

## Japan

- Tea
- Wasabi

## China

- Silk
- Soybean

## USA

- Corn
- Pumpkin

These resources create **regional identity and trade opportunities**.


---

# Regional Trade Differences

The same item can have different exchange costs depending on the region.

Example:

Ginseng exchange value

Korea  
1 Ginseng = 10 Wood

China  
1 Ginseng = 20 Wood

This creates a **simple trading incentive** between regions.


---

# Trader NPCs

Trading is handled through NPC characters.

Example NPC types:

Carpenter  
Stone Mason  
Farmer  
Merchant

Each NPC offers a limited set of trade recipes.


Example:

Carpenter

10 Wood → 2 Stone

Farmer

10 Rice → 3 Vegetables


---

# Fixed Exchange Rates

The game uses **fixed trade ratios**.

Dynamic market systems are intentionally avoided because:

- They add unnecessary complexity
- They can destabilize the game economy
- They make balancing much harder

Fixed exchange ratios ensure:

- Predictable gameplay
- Stable progression
- Easier balancing


---

# Preventing Resource Obsolescence

A key design goal is ensuring that **early-game resources remain valuable**.

This is achieved by:

1. Using early resources in higher-tier crafting
2. Requiring base resources for upgrades
3. Maintaining trade value across regions


Example:

Steel Tool crafting might require:

Iron + Wood + Rice


This ensures that players still return to earlier maps.


---

# World Trade Loop

The economic gameplay loop looks like this:

Collect resources  
↓  
Trade with NPCs  
↓  
Obtain new materials  
↓  
Unlock new regions  
↓  
Access new trade routes  
↓  
Repeat


---

# Example Trade Route

Korea

Produce Ginseng

↓

Travel to Japan

Trade for Tea

↓

Travel to China

Trade Tea for Silk


This creates a **world travel trading loop**.


---

# Data-Driven Trade System

Trades should be defined using data rather than hard-coded values.

Example JSON structure:

{
  "trades":[
    {
      "give":"Wood",
      "amount":10,
      "get":"Stone",
      "amount2":2
    },
    {
      "give":"Rice",
      "amount":5,
      "get":"Wheat",
      "amount2":1
    }
  ]
}

This allows the system to be easily expanded without modifying game logic.


---

# Future Expansion

Possible future features include:

- Multi-resource trades
- Regional trade quests
- Special merchant caravans
- Crafting chains
- Trade-based achievements


---

# Summary

This economy system focuses on:

- Resource-driven gameplay
- Fixed barter exchange
- Regional specialization
- Persistent value for early resources
- Exploration through trade

The system is designed to remain simple while supporting deep progression across multiple world regions.