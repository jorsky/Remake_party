# Party Board Game — Context

A 4-player board-and-minigame party game. Names here are placeholders until a theme is chosen.

## Language

**Match**:
One full play session, from the first Turn to the declared winner.
_Avoid_: Game (ambiguous with the product), session

**Board**:
The map of connected Spaces that Players move around during a Match. Each Board has its own theme; the first is tropical.

**Space**:
One position on the Board that a Player can stop on or pass over. The core types are Gain Space, Lose Space, Event Space and Crown Space.

**Gain Space**:
A green Space that gives the Player 3 Coins when they land on it.
_Avoid_: Blue space

**Lose Space**:
A red Space that takes 5 Coins from the Player when they land on it. A Player's Coins never drop below zero.

**Event Space**:
A Space that triggers an Event when a Player lands on it.

**Event**:
An effect triggered by an Event Space, such as Tide Shift.

**Tide Shift**:
The Event that moves the Crown Space to a new spot on the Board.

**Coconut Drop**:
The Event that takes 5 Coins from every Player.

**Swap Current**:
The Event that swaps the triggering Player's position with a random other Player.

**Turn-Order Roll**:
The Safe Die roll every Player makes at the start of a Match; the highest Roll takes the first Turn.

**Crown Space**:
The one Space where a Player can buy a Crown. It moves to a new spot on the Board after each purchase.

**Player**:
One of the 4 seats in a Match, controlled by either a person or the computer.
_Avoid_: Character (a Character is who the Player chose to play as)

**CPU Player**:
A Player controlled by the computer, used to fill any seat without a person. Each one has a Difficulty of Easy, Normal or Hard.
_Avoid_: Bot, AI

**Character**:
One of the original cast members a Player picks to represent them.

**Turn**:
One Player's go: roll, move, then resolve the Space they stop on.

**Round**:
Every Player takes one Turn, then all Players play one Minigame.

**Roll**:
The number a Player gets at the start of their Turn by throwing either the Safe Die or the Risky Die.

**Safe Die**:
The die with only positive faces, which always moves the Player forward.

**Risky Die**:
The die with bigger high faces and some negative faces, which can move the Player backward.

**Negative Roll**:
A Roll below zero, which moves the Player backward along the Board.
_Avoid_: Backroll, minus roll

**Coins**:
The currency Players earn and lose on the Board and in Minigames, spent to buy Crowns.

**Crown**:
The item a Player buys for 20 Coins when they pass or stop on the Crown Space, moving forward or backward. The Player with the most Crowns at the end of the Match wins.
_Avoid_: Star

**Minigame**:
A short action game played between Rounds whose result feeds back into the Board.

## Relationships

- A **Match** has exactly 4 **Players** and one **Board**
- A **Match** is made of a fixed number of **Rounds** (10 in the core version)
- Landing on a **Space** after a **Negative Roll** triggers that Space, just like moving forward
- A **Round** contains one **Turn** per **Player** followed by one **Minigame**
- On each **Turn**, the **Player** chooses either the **Safe Die** or the **Risky Die**
- A **Match** is won by the most **Crowns**; ties go to the most **Coins**, then the win is shared
- Each **Player** plays as exactly one **Character**; the core version has 4 Characters
- Every **Player** starts a **Match** with 10 **Coins**
- **Minigame** payout by placing: 15 / 8 / 4 / 0 **Coins**
- The core version has 10 **Minigames**: the first 3 are polished before the other 7 are added

## Flagged ambiguities

- How a **Minigame** is chosen and introduced is not yet confirmed.
