using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI;
using StardewValley;
using Object = StardewValley.Object;

namespace CustomWinterStarGifts.Utilities
{
    /// <summary>Centralized utility class for handling item ID operations in Stardew Valley 1.6</summary>
    public static class ItemIdHelper
    {
        /// <summary>
        /// Create Object from a string ID
        /// Handle both new format and legacy numeric IDs
        /// </summary>
        /// <param name="itemId">The item ID (e.g., "(O)72" or "72")</param>
        /// <param name="quantity">The quantity to create</param>
        /// <param name="monitor">Optional monitor for logging (can be null)</param>
        /// <returns>Created Object or null if creation failed</returns>
        public static Object CreateObjectFromStringId(string itemId, int quantity = 1, IMonitor monitor = null)
        {
            try
            {
                // Try using ItemRegistry.Create()
                if (!string.IsNullOrEmpty(itemId))
                {
                    var item = ItemRegistry.Create(itemId, quantity);
                    if (item is Object obj)
                    {
                        return obj;
                    }
                }

                monitor?.Log($"ItemRegistry.Create() failed for ID: {itemId}, trying fallback methods", LogLevel.Debug);

                // Fallback: if it's in legacy format "(O)123", extract the numeric ID
                int numericId = ExtractNumericId(itemId);
                if (numericId > 0)
                {
                    // Try creating with the numeric ID as string - legacy Object constructor
                    return new Object(numericId.ToString(), quantity);
                }

                monitor?.Log($"Could not create object from ID: {itemId}", LogLevel.Warn);
                return null;
            }
            catch (Exception ex)
            {
                monitor?.Log($"Error creating object from ID {itemId}: {ex.Message}", LogLevel.Error);

                // Second fallback: try extracting numeric ID and creating with that
                int numericId = ExtractNumericId(itemId);
                if (numericId > 0)
                {
                    try
                    {
                        return new Object(numericId.ToString(), quantity);
                    }
                    catch (Exception ex2)
                    {
                        monitor?.Log($"Failed to create object even with numeric ID {numericId}: {ex2.Message}", LogLevel.Error);
                    }
                }

                return null;
            }
        }

        /// <summary>Get the string ID for an item, preferring QualifiedItemId</summary>
        /// <param name="item">The item to get ID for</param>
        /// <returns>String ID for the item</returns>
        public static string GetItemStringId(Item item)
        {
            if (item == null) return string.Empty;

            // Use QualifiedItemId if available - globally unique in 1.6
            if (!string.IsNullOrEmpty(item.QualifiedItemId))
                return item.QualifiedItemId;

            // Fallback to ItemId
            if (!string.IsNullOrEmpty(item.ItemId))
                return item.ItemId;

            // Second fallback: construct from ParentSheetIndex for Objects
            if (item is Object obj)
                return $"(O){obj.ParentSheetIndex}";

            // For non-Object items, try to construct based on type
            return ConstructQualifiedId(item);
        }

        /// <summary>
        /// Constructs a qualified ID based on item type (fallback for items without proper IDs)
        /// </summary>
        /// <param name="item">The item to construct ID for</param>
        /// <returns>Constructed qualified ID</returns>
        private static string ConstructQualifiedId(Item item)
        {
            switch (item)
            {
                case StardewValley.Object _:
                    return $"(O){item.ParentSheetIndex}";
                case StardewValley.Tool _:
                    return $"(T){item.ParentSheetIndex}";
                case StardewValley.Objects.Hat _:
                    return $"(H){item.ParentSheetIndex}";
                case StardewValley.Objects.Boots _:
                    return $"(B){item.ParentSheetIndex}";
                case StardewValley.Objects.Ring _:
                    return $"(O){item.ParentSheetIndex}"; // Rings are objects
                default:
                    // Default to Object type for unknown items
                    return $"(O){item.ParentSheetIndex}";
            }
        }

        /// <summary>Helper method to extract numeric ID from string format for backwards compatibility</summary>
        /// <param name="stringId">String ID in format "(O)123" or "123"</param>
        /// <returns>Numeric ID, or -1 if parsing fails</returns>
        public static int ExtractNumericId(string stringId)
        {
            if (string.IsNullOrEmpty(stringId)) return -1;

            // Handle new string format "(O)123"
            if (stringId.StartsWith("(") && stringId.Contains(")"))
            {
                var closingParen = stringId.IndexOf(')');
                if (closingParen > 1 && closingParen < stringId.Length - 1)
                {
                    var numericPart = stringId.Substring(closingParen + 1);
                    if (int.TryParse(numericPart, out int id))
                        return id;
                }
            }

            // Handle legacy numeric format
            if (int.TryParse(stringId, out int legacyId))
                return legacyId;

            return -1;
        }

        /// <summary>Convert legacy numeric item IDs to new string format</summary>
        /// <param name="numericId">The old numeric item ID</param>
        /// <returns>String ID in the format "(O)123"</returns>
        public static string ConvertToStringId(int numericId)
        {
            return $"(O){numericId}";
        }

        /// <summary>
        /// Validate if a string is a valid item ID format
        /// Supports both new string format "(O)123" and legacy numeric format "123"
        /// </summary>
        /// <param name="itemId">The item ID to validate</param>
        /// <returns>True if the ID format is valid</returns>
        public static bool IsValidItemId(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return false;

            // Check for new string format like "(O)123"
            if (itemId.StartsWith("(") && itemId.Contains(")"))
            {
                var closingParen = itemId.IndexOf(')');
                if (closingParen > 1 && closingParen < itemId.Length - 1)
                {
                    var numericPart = itemId.Substring(closingParen + 1);
                    return int.TryParse(numericPart, out _);
                }
            }

            // Check for legacy numeric format "123" (for backwards compatibility)
            return int.TryParse(itemId, out _);
        }

        /// <summary>Create a dictionary of current inventory using string IDs</summary>
        /// <param name="player">The player whose inventory to read</param>
        /// <returns>Dictionary mapping item IDs to quantities</returns>
        public static Dictionary<string, int> GetInventoryAsStringIds(Farmer player)
        {
            var inventory = new Dictionary<string, int>();

            foreach (var item in player.Items)
            {
                if (item != null)
                {
                    string itemId = GetItemStringId(item);

                    // Skip items without valid IDs
                    if (string.IsNullOrEmpty(itemId)) continue;

                    inventory[itemId] = inventory.ContainsKey(itemId) ? inventory[itemId] + item.Stack : item.Stack;
                }
            }

            return inventory;
        }

        /// <summary>Check if an item was in a previous inventory snapshot</summary>
        /// <param name="newItem">The new item to check</param>
        /// <param name="previousInventory">Previous inventory snapshot</param>
        /// <returns>True if the item was already in the inventory</returns>
        public static bool WasInPreviousInventory(Item newItem, Dictionary<string, int> previousInventory)
        {
            if (previousInventory == null || newItem == null) return false;

            string itemId = GetItemStringId(newItem);

            // Skip items without valid IDs
            if (string.IsNullOrEmpty(itemId)) return false;

            int currentQuantity = previousInventory.ContainsKey(itemId) ? previousInventory[itemId] : 0;

            // Check if already had this item in the expected quantity
            return currentQuantity >= newItem.Stack;
        }

        /// <summary>Parse comma-separated string of item IDs into a list</summary>
        /// <param name="itemIdsString">Comma-separated item IDs string</param>
        /// <returns>List of valid item IDs</returns>
        public static List<string> ParseItemIds(string itemIdsString)
        {
            if (string.IsNullOrEmpty(itemIdsString)) return new List<string>();

            return itemIdsString.Split(',')
                .Select(id => id.Trim())
                .Where(id => IsValidItemId(id))
                .ToList();
        }

        /// <summary>Parse comma-separated string of item IDs with optional quantities</summary>
        /// <param name="itemIdsString">Comma-separated item IDs string with optional quantities (e.g., "(O)72:5,(O)74")</param>
        /// <returns>List of tuples with item ID and quantity</returns>
        public static List<(string id, int quantity)> ParseItemIdsWithQuantity(string itemIdsString)
        {
            if (string.IsNullOrEmpty(itemIdsString)) return new List<(string id, int quantity)>();

            return itemIdsString.Split(',')
                .Select(itemString => itemString.Trim())
                .Select(itemString =>
                {
                    if (itemString.Contains(':'))
                    {
                        var parts = itemString.Split(':');
                        if (parts.Length == 2 &&
                            IsValidItemId(parts[0].Trim()) &&
                            int.TryParse(parts[1].Trim(), out int quantity))
                        {
                            return (id: parts[0].Trim(), quantity: Math.Max(1, quantity));
                        }
                    }
                    else if (IsValidItemId(itemString))
                    {
                        return (id: itemString, quantity: 1);
                    }

                    return (id: string.Empty, quantity: 0);
                })
                .Where(item => !string.IsNullOrEmpty(item.id))
                .ToList();
        }

        /// <summary>Convert legacy numeric IDs in a comma-separated string to new string format</summary>
        /// <param name="itemIdsString">String containing item IDs that may include legacy numeric IDs</param>
        /// <returns>String with all IDs converted to new format</returns>
        public static string ConvertLegacyIdsToStringFormat(string itemIdsString)
        {
            if (string.IsNullOrEmpty(itemIdsString)) return itemIdsString;

            var convertedIds = itemIdsString.Split(',')
                .Select(id => id.Trim())
                .Select(id =>
                {
                    // Handle quantity format "123:5" or "(O)123:5"
                    if (id.Contains(':'))
                    {
                        var parts = id.Split(':');
                        if (parts.Length == 2)
                        {
                            string itemPart = parts[0].Trim();
                            string quantityPart = parts[1].Trim();

                            // Convert the item part if it's numeric
                            if (int.TryParse(itemPart, out _) && !itemPart.StartsWith("("))
                            {
                                return $"(O){itemPart}:{quantityPart}";
                            }
                            // If already in correct format, keep as is
                            return id;
                        }
                    }

                    // If already in string format, keep it as is
                    if (id.StartsWith("(") && id.Contains(")"))
                        return id;

                    // If numeric ID, convert to string format
                    if (int.TryParse(id, out _))
                        return $"(O){id}";

                    // If invalid, keep as is - will be filtered out later
                    return id;
                })
                .Where(id => IsValidItemId(id.Split(':')[0])); // Validate the item ID part only

            return string.Join(",", convertedIds);
        }

        /// <summary>Format a list of items with quantities back to a comma-separated string</summary>
        /// <param name="items">List of items with quantities</param>
        /// <returns>Formatted string for storage</returns>
        public static string FormatItemsWithQuantities(List<(string id, int quantity)> items)
        {
            return string.Join(",", items.Select(item =>
                item.quantity == 1 ? item.id : $"{item.id}:{item.quantity}"));
        }
    }
}