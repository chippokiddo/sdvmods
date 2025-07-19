using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using CustomWinterStarGifts.UI;
using Object = StardewValley.Object;

namespace CustomWinterStarGifts
{
	public class ModEntry : Mod
	{
		private ModConfig Config;
		private new IModHelper Helper;
		private bool HasConfiguredGifts = false;
		private IGenericModConfigMenuApi ConfigMenu;

		// Track inventory before Winter Star to detect new gifts
		private System.Collections.Generic.Dictionary<int, int> InventoryBeforeGift;
		private bool IsWinterStarActive = false;

		public override void Entry(IModHelper helper)
		{
			this.Helper = helper;
			this.Config = helper.ReadConfig<ModConfig>();

			// Check if player has configured their gifts
			HasConfiguredGifts = !string.IsNullOrEmpty(Config.LikedGiftIds) || !string.IsNullOrEmpty(Config.LovedGiftIds);

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.Input.ButtonsChanged += OnButtonsChanged;

			// Monitor for Winter Star festival
			helper.Events.Player.InventoryChanged += OnInventoryChanged;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Register console commands for testing
			Helper.ConsoleCommands.Add("configure_gifts", "Open visual gift configuration menu", ConfigureGifts);
			Helper.ConsoleCommands.Add("reset_gifts", "Reset gift configuration", ResetGifts);
			Helper.ConsoleCommands.Add("test_winter_star", "Test Winter Star gift distribution", TestWinterStarGifts);

			// Setup Generic Mod Config Menu integration
			SetupConfigMenu();
		}

		private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
		{
			// Check if Winter Star festival is active
			bool wasActive = IsWinterStarActive;

			// Check if today is winter 25 and there's festival data loaded
			bool isFestivalDay = Game1.currentSeason == "winter" && Game1.dayOfMonth == 25;
			bool isFestivalActive = isFestivalDay && Game1.isFestival();

			// Check festival location during festival hours
			bool inFestivalTimeAndPlace = Game1.timeOfDay >= 900 &&
										 Game1.timeOfDay <= 1400 &&
										 Game1.currentLocation?.Name == "Town";

			IsWinterStarActive = isFestivalActive && inFestivalTimeAndPlace;

			// Clear inventory snapshot when festival ends
			if (!IsWinterStarActive && wasActive)
			{
				InventoryBeforeGift = null;
				Monitor.Log("Winter Star festival ended", LogLevel.Info);
			}
		}

		private void OnInventoryChanged(object sender, InventoryChangedEventArgs e)
		{
			// Only act during Winter Star festival
			if (!IsWinterStarActive || !HasConfiguredGifts)
				return;

			// Check for items being removed (player giving their gift)
			var removedItems = e.Removed.Where(item => item != null).ToList();
			if (removedItems.Any() && InventoryBeforeGift == null)
			{
				InventoryBeforeGift = GetCurrentInventory();
				Monitor.Log("Player gave their Winter Star gift - monitoring for incoming gift", LogLevel.Info);
				return;
			}

			// Check for new items (potential Winter Star gifts received from NPCs)
			var newItems = e.Added.Where(item => item != null).ToList();
			if (InventoryBeforeGift == null || !newItems.Any())
				return;

			foreach (var newItem in newItems)
			{
				// Check if this item wasn't in previous inventory
				if (IsPotentialWinterStarGift(newItem) && !WasInPreviousInventory(newItem))
				{
					// Remove the original gift
					Game1.player.removeItemFromInventory(newItem);

					// Give custom gift instead
					var customGift = GetCustomGift();
					if (customGift != null)
					{
						Game1.player.addItemToInventoryBool(customGift);
						string quantityText = customGift.Stack > 1 ? $" x{customGift.Stack}" : "";
						Game1.addHUDMessage(new HUDMessage($"Custom Winter Star gift: {customGift.Name}{quantityText}", 2));
						Monitor.Log($"Replaced Winter Star gift with custom gift: {customGift.Name} x{customGift.Stack}", LogLevel.Info);
					}
					else
					{
						// If no custom gift available, give back the original
						Game1.player.addItemToInventoryBool(newItem);
					}

					// Reset the snapshot after processing the gift
					InventoryBeforeGift = null;
				}
			}
		}

		private bool WasInPreviousInventory(Item newItem)
		{
			if (InventoryBeforeGift == null) return false;

			int itemId = newItem.ParentSheetIndex;
			int currentQuantity = InventoryBeforeGift.ContainsKey(itemId) ? InventoryBeforeGift[itemId] : 0;

			// Check if already had this item in the expected quantity
			return currentQuantity >= newItem.Stack;
		}

		private bool IsPotentialWinterStarGift(Item item)
		{
			// Need to be specific to avoid replacing items the player legitimately obtained
			// Check if in a cutscene/event since Winter Star gift giving is a cutscene
			if (Game1.CurrentEvent != null)
				return true;

			// Fallback heuristics for Winter Star gifts
			return item.Stack == 1 && (
				   (item is Object obj && (
					   obj.Category == Object.GemCategory ||
					   obj.Category == Object.artisanGoodsCategory ||
					   obj.Category == Object.CookingCategory ||
					   obj.Category == Object.FishCategory ||
					   obj.Category == Object.VegetableCategory ||
					   obj.Category == Object.SeedsCategory ||
					   obj.Category == Object.flowersCategory
				   )) ||
				   item.Name.Contains("Bar") ||
				   item.Name.Contains("Geode") ||
				   item.Name.Contains("Seed") ||
				   item.Name.Contains("Trash") ||
				   item.Name.Contains("Broken Glasses") ||
				   item.Name.Contains("Broken CD") ||
				   item.Name.Contains("Driftwood") ||
				   item.Name.Contains("Soggy Newspaper") ||
				   item.Name.Contains("Rotten Plant") ||
				   item.Name.Contains("Joja Cola")
			);
		}

		private Object GetCustomGift()
		{
			try
			{
				// Try to determine which NPC is giving the gift
				string giftGiverName = GetWinterStarGiftGiver();
				int friendshipLevel = 0;

				if (!string.IsNullOrEmpty(giftGiverName))
				{
					friendshipLevel = Game1.player.getFriendshipHeartLevelForNPC(giftGiverName);
					Monitor.Log($"Gift from {giftGiverName} (friendship level: {friendshipLevel} hearts)", LogLevel.Debug);
				}
				else
				{
					Monitor.Log("Could not determine gift giver, using random selection", LogLevel.Debug);
				}

				var likedGiftItems = Config.GetLikedGiftItems();
				var lovedGiftItems = Config.GetLovedGiftItems();

				// Select gift pool based on friendship level
				List<(int id, int quantity)> giftPool;

				if (friendshipLevel >= 8 && lovedGiftItems.Count > 0)
				{
					// 8+ hearts: loved gifts
					giftPool = lovedGiftItems;
					Monitor.Log($"Using loved gifts for {friendshipLevel} heart friendship", LogLevel.Debug);
				}
				else if (friendshipLevel >= 4 && likedGiftItems.Count > 0)
				{
					// 4-7 hearts: liked gifts
					giftPool = likedGiftItems;
					Monitor.Log($"Using liked gifts for {friendshipLevel} heart friendship", LogLevel.Debug);
				}
				else
				{
					// Fallback: use loved gifts if available, otherwise liked gifts
					giftPool = lovedGiftItems.Count > 0 ? lovedGiftItems : likedGiftItems;
					Monitor.Log($"Using fallback gift selection (friendship: {friendshipLevel} hearts)", LogLevel.Debug);
				}

				if (giftPool.Count > 0)
				{
					var random = new Random();
					var selectedGift = giftPool[random.Next(giftPool.Count)];
					return new Object(selectedGift.id.ToString(), selectedGift.quantity);
				}
			}
			catch (Exception ex)
			{
				Monitor.Log($"Error creating custom gift: {ex.Message}", LogLevel.Error);
			}

			return null;
		}

		private string GetWinterStarGiftGiver()
		{
			try
			{
				// Method 1: Check if in an event first
				if (Game1.CurrentEvent != null)
				{
					// During the gift-receiving cutscene, check for current speaker
					if (Game1.currentSpeaker != null)
					{
						return Game1.currentSpeaker.Name;
					}

					// If no current speaker, look for NPCs in the current location during the event
					foreach (var npc in Game1.currentLocation.characters)
					{
						if (npc != null && !string.IsNullOrEmpty(npc.Name))
						{
							var playerTile = Game1.player.TilePoint.ToVector2();
							var npcTile = npc.TilePoint.ToVector2();
							if (Vector2.Distance(playerTile, npcTile) <= 3) // Close proximity during cutscene
							{
								return npc.Name;
							}
						}
					}
				}

				// Method 2: If not in event, find the closest NPC
				if (Game1.currentLocation != null)
				{
					var playerTile = Game1.player.TilePoint.ToVector2();
					var closestNPC = Game1.currentLocation.characters
						.Where(npc => npc != null && !string.IsNullOrEmpty(npc.Name))
						.OrderBy(npc => Vector2.Distance(playerTile, npc.TilePoint.ToVector2()))
						.FirstOrDefault();

					if (closestNPC != null && Vector2.Distance(playerTile, closestNPC.TilePoint.ToVector2()) <= 2)
					{
						return closestNPC.Name;
					}
				}
			}
			catch (Exception ex)
			{
				Monitor.Log($"Error determining gift giver: {ex.Message}", LogLevel.Warn);
			}

			return null; // Could not determine gift giver
		}

		private System.Collections.Generic.Dictionary<int, int> GetCurrentInventory()
		{
			var inventory = new System.Collections.Generic.Dictionary<int, int>();

			foreach (var item in Game1.player.Items)
			{
				if (item != null)
				{
					int id = item.ParentSheetIndex;
					inventory[id] = inventory.ContainsKey(id) ? inventory[id] + item.Stack : item.Stack;
				}
			}

			return inventory;
		}

		private void SetupConfigMenu()
		{
			// Get the API
			ConfigMenu = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
			if (ConfigMenu is null)
				return;

			// Register mod
			ConfigMenu.Register(
				mod: ModManifest,
				reset: () => Config = new ModConfig(),
				save: () =>
				{
					Helper.WriteConfig(Config);
					HasConfiguredGifts = !string.IsNullOrEmpty(Config.LikedGiftIds) || !string.IsNullOrEmpty(Config.LovedGiftIds);
				}
			);

			// Add keybind for visual menu
			ConfigMenu.AddKeybindList(
				mod: ModManifest,
				name: () => "Open Visual Gift Menu",
				tooltip: () => "Press this key to open the visual menu for configuring liked and loved gifts.",
				getValue: () => Config.OpenVisualMenuKey,
				setValue: value => Config.OpenVisualMenuKey = value
			);

			// Add text options for gift IDs
			ConfigMenu.AddTextOption(
				mod: ModManifest,
				name: () => "Liked Gift IDs",
				tooltip: () => "Comma-separated list of item IDs for liked gifts (4-7 hearts). Consider using the visual menu instead for easier selection. Format: ID or ID:quantity (e.g., 472:10 for 10 parsnip seeds).",
				getValue: () => Config.LikedGiftIds,
				setValue: value => Config.LikedGiftIds = value
			);

			ConfigMenu.AddTextOption(
				mod: ModManifest,
				name: () => "Loved Gift IDs",
				tooltip: () => "Comma-separated list of item IDs for loved gifts (8+ hearts). Consider using the visual menu instead for easier selection. Format: ID or ID:quantity (e.g., 472:10 for 10 parsnip seeds).",
				getValue: () => Config.LovedGiftIds,
				setValue: value => Config.LovedGiftIds = value
			);

			// Add helpful information
			ConfigMenu.AddParagraph(
				mod: ModManifest,
				text: () => "Currently selected gifts will be shown here after configuration."
			);

			ConfigMenu.AddParagraph(
				mod: ModManifest,
				text: () => "Example item IDs: Prismatic Shard (74), Diamond (72), Cheese (424), Truffle Oil (432), Ancient Fruit (454), Starfruit (268), Parsnip Seeds (472:10 for 10 seeds)"
			);
		}

		private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			// Update configuration status
			HasConfiguredGifts = !string.IsNullOrEmpty(Config.LikedGiftIds) || !string.IsNullOrEmpty(Config.LovedGiftIds);
		}

		private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
		{
			// Open the visual gift configuration menu using the configured keybind
			if (Config.OpenVisualMenuKey.JustPressed() && Context.IsWorldReady)
			{
				OpenVisualGiftConfigurationMenu();
			}
		}

		private void OpenVisualGiftConfigurationMenu()
		{
			var menu = new VisualGiftConfigurationMenu(Config, Helper, OnConfigurationComplete);
			Game1.activeClickableMenu = menu;
		}

		private void OnConfigurationComplete(ModConfig newConfig)
		{
			Config = newConfig;
			HasConfiguredGifts = !string.IsNullOrEmpty(Config.LikedGiftIds) || !string.IsNullOrEmpty(Config.LovedGiftIds);
			Helper.WriteConfig(Config);
			Game1.addHUDMessage(new HUDMessage("Gift preferences saved!", 2));
		}

		private void ConfigureGifts(string command, string[] args)
		{
			OpenVisualGiftConfigurationMenu();
		}

		private void ResetGifts(string command, string[] args)
		{
			Config = new ModConfig();
			HasConfiguredGifts = false;
			Helper.WriteConfig(Config);
			Game1.addHUDMessage(new HUDMessage("Gift preferences reset!", 2));
		}

		private void TestWinterStarGifts(string command, string[] args)
		{
			if (HasConfiguredGifts)
			{
				var testGift = GetCustomGift();
				if (testGift != null)
				{
					Game1.player.addItemToInventoryBool(testGift);
					Game1.addHUDMessage(new HUDMessage($"Test gift: {testGift.Name} x{testGift.Stack}", 2));
				}
			}
			else
			{
				Game1.addHUDMessage(new HUDMessage("No gifts configured! Use configure_gifts command first.", 2));
			}
		}
	}
}