# Remake Party

A 4-player board-and-minigame party game made in Unity. The words used throughout the project (Match, Round, Turn, Safe Die, Crown Space…) are defined in [CONTEXT.md](CONTEXT.md).

## Current state: grey-box Board

The core rules are built and tested. You can watch 4 CPU Players play a full 10-Round Match on the Tropical Board, drawn with simple shapes. Minigames are placeholders for now: the results are random, but Players with a higher Difficulty win more often.

## Open it in Unity

1. **Install Git LFS** (Git's add-on for large art and audio files). Run this once on your PC:
   ```bash
   git lfs install
   ```
2. **Get the repo** onto your PC, for example with GitHub Desktop, or with:
   ```bash
   git clone https://github.com/jorsky/Remake_party.git
   ```
3. **Add it to Unity Hub:** click **Add → Add project from disk** and select the `Remake_party` folder.
   - If Hub says the editor version is missing, pick the Unity 6 version you have installed and click **Change version**.
4. **Open it.** The first time, Unity downloads its packages and runs a setup script. That takes a few minutes and does two things:
   - sets up the Universal Render Pipeline,
   - creates and opens `Assets/Scenes/Main.unity`, with the Match already in it.
5. Press **Play**.

If the Main scene doesn't open by itself, choose **Remake Party → Open Main Scene** from the top menu.

**Settings:** click the `Match` object in the Hierarchy.
- Set each CPU Player's Difficulty: Easy, Normal or Hard.
- Set a **Seed** (any number except 0) to replay the exact same Match. Leave it at 0 to get a different Match each time.
- Use the **Speed** slider in the top left, during play, to fast-forward.

## Run the tests

1. Choose **Window → General → Test Runner → EditMode**.
2. Click **Run All**. Every test should go green.

## After the first open

Unity creates a few new files:
- `Assets/Scenes/Main.unity`,
- the `Assets/Settings` folder,
- `Packages/packages-lock.json`,
- extra files in `ProjectSettings`.

Commit them all so the project opens the same way on any computer.

## Where things live

| Folder | What's in it |
|---|---|
| `Assets/Scripts/Core` | The game rules: Board, dice, Match flow, CPU Players. No Unity code, so it can be tested on its own. |
| `Assets/Scripts/Core/Rules.cs` | Every tunable number: Coins, Crown cost, payouts and Round count. |
| `Assets/Scripts/Core/Boards/TropicalBoard.cs` | The first Board's layout. |
| `Assets/Scripts/Presentation` | The Unity side: draws the Board and animates the Match. |
| `Assets/Tests/EditMode` | Automated tests for the rules. |
