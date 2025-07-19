using System;
using System.Collections.Generic;
using System.Linq;
using StardewModdingAPI.Utilities;

namespace CustomWinterStarGifts
{
    public class ModConfig
    {
        public string LikedGiftIds { get; set; } = "18,348,346";
        public string LovedGiftIds { get; set; } = "72,74,424";
        public KeybindList OpenVisualMenuKey { get; set; } = KeybindList.Parse("F9");

        public List<int> GetLikedGiftIds()
        {
            return ParseItemIds(LikedGiftIds);
        }

        public List<int> GetLovedGiftIds()
        {
            return ParseItemIds(LovedGiftIds);
        }

        public List<(int id, int quantity)> GetLikedGiftItems()
        {
            return ParseItemIdsWithQuantity(LikedGiftIds);
        }

        public List<(int id, int quantity)> GetLovedGiftItems()
        {
            return ParseItemIdsWithQuantity(LovedGiftIds);
        }

        private List<int> ParseItemIds(string itemIdsString)
        {
            if (string.IsNullOrEmpty(itemIdsString)) return new List<int>();

            return itemIdsString.Split(',')
                .Select(id => id.Trim())
                .Where(id => int.TryParse(id, out _))
                .Select(int.Parse)
                .ToList();
        }

        private List<(int id, int quantity)> ParseItemIdsWithQuantity(string itemIdsString)
        {
            if (string.IsNullOrEmpty(itemIdsString)) return new List<(int id, int quantity)>();

            return itemIdsString.Split(',')
                .Select(itemString => itemString.Trim())
                .Select(itemString =>
                {
                    if (itemString.Contains(':'))
                    {
                        var parts = itemString.Split(':');
                        if (parts.Length == 2 &&
                            int.TryParse(parts[0].Trim(), out int id) &&
                            int.TryParse(parts[1].Trim(), out int quantity))
                        {
                            return (id: id, quantity: Math.Max(1, quantity));
                        }
                    }
                    else if (int.TryParse(itemString, out int id))
                    {
                        return (id: id, quantity: 1);
                    }

                    return (id: 0, quantity: 0);
                })
                .Where(item => item.id > 0)
                .ToList();
        }
    }
}