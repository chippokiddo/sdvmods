# Stardew Valley Mods

## Contents:

[Custom Winter Start Gifts](#custom-winter-star-gifts)

## Custom Winter Star Gifts

Allows players to customize the gifts they receive at the Feast of the Winter Star, based on their friendship level with the gift-giving NPC.

### Features

- Configure specific items (and/or quantities) that you'd like to receive as gifts from NPCs
- Different gift pools for "Liked" (4-7 hearts) and "Loved" (8+ hearts) friendship levels. NPCs with less than 4 hearts will receive a set of basic gifts.
- Visual configuration menu (`F9`; configurable) to make it easy to pick your preferred gifts without needing to know item IDs
-  All mod settings, including the keybind and advanced text fields, are accessible through Generic Mod Config Menu (GMCM)
- Developer console commands for opening the visual menu, resetting settings, and testing gift distribution

### Installation

1. Install [SMAPI](https://smapi.io)
2. Install [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (optional, but recommended)
3. Download this mod
4. Unzip the mod folder into your `Stardew Valley/Mods` folder
5. Run the game using SMAPI

### How to Use

There are three ways to access the mod's configuration:

1. Through Generic Mod Config Menu (GMCM):

   From the Stardew Valley title screen, click the small cog icon in the bottom-left corner In-game, press `Esc` or `E` to open the game menu, then click the Mod Options button at the bottom. Find "Custom Winter Star Gifts" in the list.

2. Visual Configuration Menu:

   By default, press `F9` (while in-game) to open the visual configuration menu directly. You can change this keybind in the GMCM settings.

3. Via SMAPI Console Commands:

   Open the SMAPI console (usually by pressing the `~` or `F1` key). Type `configure_gifts` and press `Enter` to open the visual configuration menu.

### Visual Gift Configuration Menu

- `Left-click` on an item sprite to add it to or remove it from your selected gift list.
- `Right-click` on an item sprite to open a small popup where you can choose a specific quantity (e.g., 1, 5, 10, 99).
- Press the `Tab` key to toggle between configuring "Liked Gifts" (for 4-7 heart NPCs) and "Loved Gifts" (for 8+ heart NPCs).
- You can use the `Up` ⬆️ and `Down` ⬇️ arrow keys to switch between item categories (e.g., Gems, Crops, Fish)
- You can use the `Left` ⬅️ and `Right` ➡️ arrow keys to move through pages of items within the current category.
- You can press `Enter` to save your configuration and close the menu.
- You can press `Esc` to close the menu without saving changes.

### Generic Mod Config Menu

For users who prefer direct input or already know item IDs, you can manually enter comma-separated item IDs in the GMCM.

Format: ID (for quantity 1) or ID:quantity (e.g., `472:10` for 10 Parsnip Seeds).

Example Items: Prismatic Shard (74), Diamond (72), Cheese (424), Truffle Oil (432), Ancient Fruit (454), Starfruit (268).

### Console Commands

| Commands           | Description                                                  |
| ------------------ | ------------------------------------------------------------ |
| `configure_gifts`  | Opens the visual gift configuration menu.                    |
| `reset_gifts`      | Resets all liked and loved gift configurations to their default values. |
| `test_winter_star` | Immediately distributes Winter Star gifts based on your current configuration (for testing purposes). |

## Attributions
- Pathoschild for SMAPI
- spacechase0 for Generic Mod Config Menu (GMCM)
