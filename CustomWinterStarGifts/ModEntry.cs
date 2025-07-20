using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using CustomWinterStarGifts.Data;
using CustomWinterStarGifts.Interface;
using CustomWinterStarGifts.Utilities;
using Object = StardewValley.Object;

namespace CustomWinterStarGifts
{
	public class ModEntry : Mod
	{
		private ModConfig Config;
		private new IModHelper Helper;
		private IGenericModConfigMenuApi ConfigMenu;

		// Track inventory before Winter Star to detect new gifts
		private System.Collections.Generic.Dictionary<string, int> InventoryBeforeGift;
		private bool IsWinterStarActive = false;

		public override void Entry(IModHelper helper)
		{
			this.Helper = helper;
			this.Config = helper.ReadConfig<ModConfig>();

			// Migrate legacy IDs if needed
			Config.MigrateLegacyIds();
			helper.WriteConfig(Config);

			helper.Events.GameLoop.GameLaunched += OnGameLaunched;
			helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
			helper.Events.Input.ButtonsChanged += OnButtonsChanged;

			// Monitor for Winter Star festival
			helper.Events.Player.InventoryChanged += OnInventoryChanged;
			helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
		}

		private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
		{
			// Register console commands
			Helper.ConsoleCommands.Add("cwsg", "Manage Custom Winter Star Gifts. Usage: cwsg <menu|reset|test|keybind>", HandleCwsgCommand);

			// Setup Generic Mod Config Menu integration
			SetupConfigMenu();
		}

		private void HandleCwsgCommand(string command, string[] args)
		{
			if (args.Length == 0)
			{
				Monitor.Log("Usage: cwsg <menu|reset|test|keybind>", LogLevel.Info);
				Monitor.Log("	menu - Open visual gift configuration menu", LogLevel.Info);
				Monitor.Log("	reset - Reset gift configuration to defaults", LogLevel.Info);
				Monitor.Log("	test - Test Winter Star gift distribution", LogLevel.Info);
				Monitor.Log("	keybind - Configure keybind for opening menu", LogLevel.Info);
				return;
			}

			switch (args[0].ToLower())
			{
				case "menu":
					ConfigureGifts(command, args);
					break;

				case "reset":
					SetDefaultWinterGifts(command, args);
					break;

				case "test":
					TestWinterStarGifts(command, args);
					break;

				case "keybind":
					ConfigureKeybind(command, args.Skip(1).ToArray()); // Pass remaining args for keybind value
					break;

				default:
					Monitor.Log($"Unknown subcommand: {args[0]}", LogLevel.Error);
					Monitor.Log("Usage: cwsg <menu|reset|test|keybind>", LogLevel.Info);
					break;
			}
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
			if (!IsWinterStarActive)
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
			return ItemIdHelper.WasInPreviousInventory(newItem, InventoryBeforeGift);
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
					   obj.Category == Object.flowersCategory ||
					   obj.Category == Object.junkCategory
				   )) ||
				   item.Name.Contains("Bar") ||
				   item.Name.Contains("Geode") ||
				   item.Name.Contains("Seed")
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
				var lowFriendshipGifts = CategoryData.LowFriendshipGifts.ToList();

				// Select gift pool based on friendship level
				List<(string id, int quantity)> giftPool;

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
					// 0-3 hearts: low friendship gifts
					giftPool = CategoryData.LowFriendshipGifts.ToList();
					Monitor.Log($"Using low friendship gifts for {friendshipLevel} heart friendship", LogLevel.Debug);
				}
			}
			catch (Exception ex)
			{
				Monitor.Log($"Error creating custom gift: {ex.Message}", LogLevel.Error);
			}

			return null;
		}

		/// <summary>Create an Object from a string ID</summary>
		private Object CreateObjectFromStringId(string itemId, int quantity)
		{
			return ItemIdHelper.CreateObjectFromStringId(itemId, quantity, Monitor);
		}

		private string GetWinterStarGiftGiver()
		{
			try
			{
				if (Game1.CurrentEvent != null)
				{
					var playerTile = Game1.player.TilePoint.ToVector2();

					// Method 1: During the gift-receiving cutscene, look for the NPC approaching the tree
					if (Game1.currentLocation?.Name == "Town")
					{
						var nearbyValidNPCs = Game1.currentLocation.characters
							.Where(npc => npc != null &&
										 !string.IsNullOrEmpty(npc.Name) &&
										 IsValidWinterStarGiftGiver(npc.Name))
							.Select(npc => new
							{
								NPC = npc,
								Distance = Vector2.Distance(playerTile, npc.TilePoint.ToVector2())
							})
							.Where(x => x.Distance <= 5f) // NPCs near the Christmas tree area
							.OrderBy(x => x.Distance)
							.ToList();

						// there should ideally be only one relevant NPC
						if (nearbyValidNPCs.Count == 1)
						{
							Monitor.Log($"Gift giver identified at Christmas tree: {nearbyValidNPCs[0].NPC.Name} (distance: {nearbyValidNPCs[0].Distance:F1})", LogLevel.Debug);
							return nearbyValidNPCs[0].NPC.Name;
						}

						// if multiple NPCs near tree,  prioritize the closest one
						if (nearbyValidNPCs.Any())
						{
							var closest = nearbyValidNPCs.First();
							if (closest.Distance <= 3f)
							{
								Monitor.Log($"Gift giver identified - closest to Christmas tree: {closest.NPC.Name} (distance: {closest.Distance:F1})", LogLevel.Debug);
								return closest.NPC.Name;
							}
							else
							{
								Monitor.Log($"Multiple NPCs near tree, closest ({closest.NPC.Name}) not close enough (distance: {closest.Distance:F1})", LogLevel.Debug);
							}
						}
					}

					// Method 2: Check event actors for gift givers (fallback)
					if (Game1.CurrentEvent.actors != null && Game1.CurrentEvent.actors.Count > 0)
					{
						var validEventActors = Game1.CurrentEvent.actors
							.Where(actor => actor != null &&
										   !string.IsNullOrEmpty(actor.Name) &&
										   IsValidWinterStarGiftGiver(actor.Name))
							.ToList();

						if (validEventActors.Count == 1)
						{
							Monitor.Log($"Gift giver identified - single valid event actor: {validEventActors[0].Name}", LogLevel.Debug);
							return validEventActors[0].Name;
						}

						if (validEventActors.Count > 1)
						{
							var closestEventActor = validEventActors
								.OrderBy(actor => Vector2.Distance(playerTile, actor.TilePoint.ToVector2()))
								.First();

							float distance = Vector2.Distance(playerTile, closestEventActor.TilePoint.ToVector2());
							if (distance <= 4f)
							{
								Monitor.Log($"Gift giver identified - closest valid event actor: {closestEventActor.Name} (distance: {distance:F1})", LogLevel.Debug);
								return closestEventActor.Name;
							}
						}
					}

					// Method 3: Current speaker check - only if very close and during event
					if (Game1.currentSpeaker != null &&
						!string.IsNullOrEmpty(Game1.currentSpeaker.Name) &&
						IsValidWinterStarGiftGiver(Game1.currentSpeaker.Name))
					{
						float speakerDistance = Vector2.Distance(playerTile, Game1.currentSpeaker.TilePoint.ToVector2());
						if (speakerDistance <= 2f)
						{
							Monitor.Log($"Gift giver identified - current speaker giving gift: {Game1.currentSpeaker.Name} (distance: {speakerDistance:F1})", LogLevel.Debug);
							return Game1.currentSpeaker.Name;
						}
					}
				}

				// No event or unable to determine - mod will use random selection
				Monitor.Log("Could not determine Winter Star gift giver - using random gift selection", LogLevel.Debug);
				return null;
			}
			catch (Exception ex)
			{
				Monitor.Log($"Error determining gift giver: {ex.Message}", LogLevel.Warn);
				return null;
			}
		}

		private bool IsValidWinterStarGiftGiver(string npcName)
		{
			if (string.IsNullOrEmpty(npcName)) return false;

			// NPCs that are explicitly NEVER Winter Star gift givers according to wiki
			var excludedNPCs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"Dwarf", "Krobus", "Marlon", "Sandy", "Wizard"
			};

			// NPCs that give specific gifts - these ARE participants
			var specificGiftGivers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"Clint", "Marnie", "Robin", "Willy", "Evelyn"
			};

			// Children who can be gift givers
			var childGiftGivers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				"Jas", "Vincent", "Leo"
			};

			// If explicitly excluded, they can't be gift givers
			if (excludedNPCs.Contains(npcName))
			{
				return false;
			}

			// If they're a known specific gift giver or child gift giver, they definitely can be
			if (specificGiftGivers.Contains(npcName) || childGiftGivers.Contains(npcName))
			{
				return true;
			}

			// For any adult except Clint, Evelyn, Marnie, Robin, or Willy gifts,
			// need to check if this is a marriageable NPC or other adult villager
			var adultVillagers = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				// bachelor and bachelorettes
				"Alex", "Elliott", "Harvey", "Sam", "Sebastian", "Shane",
				"Abigail", "Emily", "Haley", "Leah", "Maru", "Penny",
				
				// Other adult NPCs who can give gifts
				"Caroline", "Demetrius", "George", "Gus", "Jodi", "Kent",
				"Lewis", "Linus", "Pam", "Pierre"
			};

			return adultVillagers.Contains(npcName);
		}

		private System.Collections.Generic.Dictionary<string, int> GetCurrentInventory()
		{
			return ItemIdHelper.GetInventoryAsStringIds(Game1.player);
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
					Config.MigrateLegacyIds(); // Check IDs are migrated when saving
					Helper.WriteConfig(Config);
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
				tooltip: () => "Comma-separated list of item IDs for liked gifts (4-7 hearts). Consider using the visual menu instead for easier selection. Format: (O)ID or (O)ID:quantity (e.g., (O)472:10 for 10 parsnip seeds).",
				getValue: () => Config.LikedGiftIds,
				setValue: value => Config.LikedGiftIds = value
			);

			ConfigMenu.AddTextOption(
				mod: ModManifest,
				name: () => "Loved Gift IDs",
				tooltip: () => "Comma-separated list of item IDs for loved gifts (8+ hearts). Consider using the visual menu instead for easier selection. Format: (O)ID or (O)ID:quantity (e.g., (O)472:10 for 10 parsnip seeds).",
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
				text: () => "Example item IDs: Prismatic Shard (O)74, Diamond (O)72, Cheese (O)424, Truffle Oil (O)432, Ancient Fruit (O)454, Starfruit (O)268, Parsnip Seeds (O)472:10 for 10 seeds"
			);
		}

		private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
		{
			// Migrate legacy IDs on save load
			Config.MigrateLegacyIds();
			Helper.WriteConfig(Config);
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
			Config.MigrateLegacyIds(); // consistency
			Helper.WriteConfig(Config);
			Game1.addHUDMessage(new HUDMessage("Gift preferences saved!", 2));
		}

		private void ConfigureGifts(string command, string[] args)
		{
			OpenVisualGiftConfigurationMenu();
		}

		private void SetDefaultWinterGifts(string command, string[] args)
		{
			Config = new ModConfig();
			Config.MigrateLegacyIds(); // Check new config uses string format
			Helper.WriteConfig(Config);

			Monitor.Log("Gift configuration reset to default Winter Star gifts:", LogLevel.Info);
			Monitor.Log($"Liked gifts: {Config.LikedGiftIds}", LogLevel.Info);
			Monitor.Log($"Loved gifts: {Config.LovedGiftIds}", LogLevel.Info);
		}

		private void TestWinterStarGifts(string command, string[] args)
		{
			var testGift = GetCustomGift();
			if (testGift != null)
			{
				Game1.player.addItemToInventoryBool(testGift);
				Monitor.Log($"Test gift given: {testGift.Name} x{testGift.Stack}", LogLevel.Info);
				Game1.addHUDMessage(new HUDMessage($"Test: {testGift.Name} x{testGift.Stack}", 2));
			}
			else
			{
				// This shouldn't happen since there are default gifts but... just in case
				Monitor.Log("Error: Could not generate test gift - unexpected error", LogLevel.Error);
			}
		}

		private void ConfigureKeybind(string command, string[] args)
		{
			if (args.Length == 0)
			{
				Monitor.Log($"Current keybind: {Config.OpenVisualMenuKey}", LogLevel.Info);
				Monitor.Log("Usage: cwsg keybind <key>", LogLevel.Info);
				Monitor.Log("Examples:", LogLevel.Info);
				Monitor.Log("	cwsg keybind F9", LogLevel.Info);
				Monitor.Log("	cwsg keybind LeftControl + G", LogLevel.Info);
				Monitor.Log("	cwsg keybind LeftShift + F10", LogLevel.Info);
				Monitor.Log("	Available keys: F1-F12, A-Z, 0-9, LeftControl, RightControl, LeftShift, RightShift, LeftAlt, RightAlt", LogLevel.Info);
				return;
			}

			string keybindString = string.Join(" ", args);

			try
			{
				var newKeybind = KeybindList.Parse(keybindString);
				Config.OpenVisualMenuKey = newKeybind;
				Helper.WriteConfig(Config);

				Monitor.Log($"Keybind updated to: {Config.OpenVisualMenuKey}", LogLevel.Info);
				Game1.addHUDMessage(new HUDMessage($"Gift menu keybind set to: {Config.OpenVisualMenuKey}", 2));
			}
			catch (Exception ex)
			{
				Monitor.Log($"Invalid keybind format: {keybindString}", LogLevel.Error);
				Monitor.Log($"Error: {ex.Message}", LogLevel.Error);
				Monitor.Log("Usage: cwsg keybind <key>", LogLevel.Info);
				Monitor.Log("Examples:", LogLevel.Info);
				Monitor.Log("	cwsg keybind F9", LogLevel.Info);
				Monitor.Log("	cwsg keybind LeftControl + G", LogLevel.Info);
				Monitor.Log("	cwsg keybind LeftShift + F10", LogLevel.Info);
			}
		}
	}
}