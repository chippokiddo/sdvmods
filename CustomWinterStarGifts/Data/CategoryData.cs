using System.Collections.Generic;

namespace CustomWinterStarGifts.Data
{
	/// <summary> Category definition for visual gift configuration menu </summary>
	public static class CategoryData
	{
		/// <summary> Names of all available categories </summary>
		public static readonly string[] CategoryNames = { "All", "Gems", "Artisan", "Crops", "Foraged", "Cooked", "Flowers", "Fish" };

		/// <summary> Item IDs used as icons for each category. Indices correspond to CategoryNames. </summary>
		public static readonly int[] CategoryIcons = {
			434,	// All (Stardrop)
			72,		// Gems (Diamond)
			424,  	// Artisan (Cheese)
			24,   	// Crops (Parsnip)
			18,   	// Foraged (Daffodil)
			194,  	// Cooked (Fried Egg)
			376,  	// Flowers (Poppy)
			128   	// Fish (Pufferfish)
		};

		/// <summary> Categorized item IDs </summary>
		public static readonly Dictionary<string, int[]> CategoryItems = new Dictionary<string, int[]>
		{
			["Gems"] = new int[] {
				60, 62, 64, 66, 68, 70, 72
			},
			["Artisan"] = new int[] {
				303, 340, 346, 348, 424, 426, 428, 459
			},
			["Crops"] = new int[] {
				24, 188, 190, 192, 248, 250, 252, 254,
				256, 258, 260, 264, 266, 268, 272, 274,
				276, 278, 280, 282, 284, 304, 398, 400
			},
			["Foraged"] = new int[] {
				16, 18, 20, 22, 78, 152, 257, 281,
				283, 296, 296, 372, 392, 393, 394, 397,
				398, 399, 402, 404, 406, 408, 410, 412,
				414, 416, 418, 420, 422, 718, 719, 723
			},
			["Cooked"] = new int[] {
				194, 196, 198, 200, 202, 204, 206, 208,
				210, 212, 214, 216, 218, 220, 222, 224,
				226, 228, 232, 233, 234, 236, 238, 240,
				242, 244, 253
			},
			["Flowers"] = new int[] {
				376, 421, 591, 593, 595, 597
			},
			["Fish"] = new int[] {
				128, 130, 132, 136, 138, 140, 142, 144,
				146, 147, 150, 154, 164, 395, 614, 698,
				699, 701, 702, 704, 708, 715, 716, 717,
				720, 721, 722, 723
			}
		};

		/// <summary> Gets all available item IDs from all categories combined </summary>
		/// <returns> HashSet of all unique item IDs across all categories </returns>
		public static HashSet<int> GetAllItemIds()
		{
			var allItemIds = new HashSet<int>();

			// Add all items from every category
			foreach (var category in CategoryItems.Values)
			{
				foreach (var id in category)
				{
					allItemIds.Add(id);
				}
			}

			return allItemIds;
		}
	}
}