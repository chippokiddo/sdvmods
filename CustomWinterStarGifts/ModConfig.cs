using System.Collections.Generic;
using StardewModdingAPI.Utilities;
using CustomWinterStarGifts.Utilities;

namespace CustomWinterStarGifts
{
    public class ModConfig
    {
        public string LikedGiftIds { get; set; } = "(O)18,(O)346,(O)348";
        public string LovedGiftIds { get; set; } = "(O)60,(O)72,(O)424";
        public KeybindList OpenVisualMenuKey { get; set; } = KeybindList.Parse("F9");

        public List<string> GetLikedGiftIds()
        {
            return ItemIdHelper.ParseItemIds(LikedGiftIds);
        }

        public List<string> GetLovedGiftIds()
        {
            return ItemIdHelper.ParseItemIds(LovedGiftIds);
        }

        public List<(string id, int quantity)> GetLikedGiftItems()
        {
            return ItemIdHelper.ParseItemIdsWithQuantity(LikedGiftIds);
        }

        public List<(string id, int quantity)> GetLovedGiftItems()
        {
            return ItemIdHelper.ParseItemIdsWithQuantity(LovedGiftIds);
        }

        /// <summary>
        /// Convert legacy numeric item IDs to new string format
        /// This helps with backwards compatibility for existing config files
        /// </summary>
        public void MigrateLegacyIds()
        {
            LikedGiftIds = ItemIdHelper.ConvertLegacyIdsToStringFormat(LikedGiftIds);
            LovedGiftIds = ItemIdHelper.ConvertLegacyIdsToStringFormat(LovedGiftIds);
        }

        /// <summary>
        /// Helper method to get numeric ID from string format for backwards compatibility
        /// Delegate to ItemIdHelper
        /// </summary>
        public static int ExtractNumericId(string stringId)
        {
            return ItemIdHelper.ExtractNumericId(stringId);
        }
    }
}