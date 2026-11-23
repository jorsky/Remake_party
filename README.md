# Remake Party

A 4-player board-and-minigame party game made in Unity. The words used throughout the project (Match, Round, Turn, Safe Die, Crown Space…) are defined in [CONTEXT.md](CONTEXT.md).

## Current state: grey-box Board

The core rules are built and tested. You can watch 4 CPU Players play a full 10-Round Match on the Tropical Board, drawn with simple shapes. Minigames are placeholders for now: the results are random, but Players with a higher Difficulty win more often.

## One-time setup

1. **Install Git LFS** (Git's add-on for large art and audio files). Run this once on your PC:
   ```bash
   git lfs install
   ```
2. **Get the repo** onto your PC, for example with GitHub Desktop, or with:
   ```bash
   git clone https://github.com/jorsky/Remake_party.git
   ```
3. **Create the Unity project files:**
   1. In Unity Hub, click **New project**, pick **Universal 3D**, and name it `RemakeParty`. Save it anywhere except inside the repo folder.
   2. Wait for Unity to finish opening the project, then close Unity.
   3. Copy the `Assets`, `Packages` and `ProjectSettings` folders from that new project into the repo folder. If asked, merge the folders; don't replace them.
   4. In Unity Hub, click **Add → Add project from disk**, select the repo folder, and open it.

## See it play

1. Open `Assets/Scenes/SampleScene`.
2. Add the Match to the scene:
   1. Choose **GameObject → Create Empty** and rename the new object `Match`.
   2. In the Inspector, click **Add Component** and choose **Match Director**.
3. Press **Play**.

**Settings on the Match Director:**
- Set each CPU Player's Difficulty: Easy, Normal or Hard.
- Set a **Seed** (any number except 0) to replay the exact same Match. Leave it at 0 to get a different Match each time.
- Use the **Speed** slider in the top left, during play, to fast-forward.

After step 2, save the scene with **Ctrl+S**.

## Run the tests

1. Choose **Window → General → Test Runner → EditMode**.
2. Click **Run All**. Every test should go green.

## Your first commit from Unity

Once the project opens without errors, commit:
- the new `.meta` files Unity created,
- the `Packages` folder,
- the `ProjectSettings` folder.

The `.gitignore` already leaves out Unity's large generated folders, such as `Library`.

## Where things live

| Folder | What's in it |
|---|---|
| `Assets/Scripts/Core` | The game rules: Board, dice, Match flow, CPU Players. No Unity code, so it can be tested on its own. |
| `Assets/Scripts/Core/Rules.cs` | Every tunable number: Coins, Crown cost, payouts and Round count. |
| `Assets/Scripts/Core/Boards/TropicalBoard.cs` | The first Board's layout. |
| `Assets/Scripts/Presentation` | The Unity side: draws the Board and animates the Match. |
| `Assets/Tests/EditMode` | Automated tests for the rules. |
