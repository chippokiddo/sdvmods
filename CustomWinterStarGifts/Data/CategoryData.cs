using System.Collections.Generic;

namespace CustomWinterStarGifts.Data
{
	/// <summary> Category definition for visual gift configuration menu </summary>
	public static class CategoryData
	{
		/// <summary> Names of all available categories </summary>
		public static readonly string[] CategoryNames = {
			"All", "Gems", "Artisan", "Crops", "Foraged",
			"Cooked", "Flowers", "Fish", "Animal Products"
		};

		/// <summary>
		/// Item IDs used as icons for each category
		/// Indices correspond to CategoryNames
		/// </summary>
		public static readonly string[] CategoryIcons = {
			"(O)434",	// All (Stardrop)
			"(O)72",	// Gems (Diamond)
			"(O)424",  	// Artisan (Cheese)
			"(O)24",   	// Crops (Parsnip)
			"(O)18",   	// Foraged (Daffodil)
			"(O)194",  	// Cooked (Fried Egg)
			"(O)376",  	// Flowers (Poppy)
			"(O)128",   // Fish (Pufferfish)
			"(O)176"	// Animal Products (Egg)
		};

		/// <summary>
		/// Low friendship gifts (0-3 hearts) - basic items to make liked/loved gifts more rewarding
		/// Not user-configurable by design
		/// </summary>
		public static readonly (string id, int quantity)[] LowFriendshipGifts = new (string, int)[]
		{
			("(O)388", 1),	// Wood
			("(O)390", 1),	// Stone  
			("(O)709", 1),	// Hardwood
			("(O)92", 1),	// Sap
			("(O)16", 1),	// Wild Horseradish
			("(O)18", 1),	// Daffodil
			("(O)20", 1),	// Leek
			("(O)22", 1),	// Dandelion
			("(O)397", 1),	// Sea Foam
			("(O)404", 1)	// Common Mushroom
		};

		/// <summary> Categorized item IDs </summary>
		public static readonly Dictionary<string, string[]> CategoryItems = new Dictionary<string, string[]>
		{
			["Gems"] = new string[] {
				"(O)60", "(O)62", "(O)64", "(O)66", "(O)68", "(O)70", "(O)72"
			},
			["Artisan"] = new string[] {
				"(O)303", "(O)340", "(O)346", "(O)348", "(O)424", "(O)426", "(O)428", "(O)459"
			},
			["Crops"] = new string[] {
				"(O)24", "(O)188", "(O)190", "(O)192", "(O)248", "(O)250", "(O)252", "(O)254",
				"(O)256", "(O)258", "(O)260", "(O)264", "(O)266", "(O)268", "(O)272", "(O)274",
				"(O)276", "(O)278", "(O)280", "(O)282", "(O)284", "(O)304", "(O)398", "(O)400"
			},
			["Foraged"] = new string[] {
				"(O)16", "(O)18", "(O)20", "(O)22", "(O)78", "(O)152", "(O)257", "(O)281",
				"(O)283", "(O)296", "(O)372", "(O)392", "(O)393", "(O)394", "(O)397",
				"(O)398", "(O)399", "(O)402", "(O)404", "(O)406", "(O)408", "(O)410", "(O)412",
				"(O)414", "(O)416", "(O)418", "(O)420", "(O)422", "(O)718", "(O)719", "(O)723"
			},
			["Cooked"] = new string[] {
				"(O)194", "(O)196", "(O)198", "(O)200", "(O)202", "(O)204", "(O)206", "(O)208",
				"(O)210", "(O)212", "(O)214", "(O)216", "(O)218", "(O)220", "(O)222", "(O)224",
				"(O)226", "(O)228", "(O)232", "(O)233", "(O)234", "(O)236", "(O)238", "(O)240",
				"(O)242", "(O)244", "(O)253"
			},
			["Flowers"] = new string[] {
				"(O)376", "(O)421", "(O)591", "(O)593", "(O)595", "(O)597"
			},
			["Fish"] = new string[] {
				"(O)128", "(O)130", "(O)132", "(O)136", "(O)138", "(O)140", "(O)142", "(O)144",
				"(O)146", "(O)147", "(O)150", "(O)154", "(O)164", "(O)395", "(O)614", "(O)698",
				"(O)699", "(O)701", "(O)702", "(O)704", "(O)708", "(O)715", "(O)716", "(O)717",
				"(O)720", "(O)721", "(O)722", "(O)723"
			},
			["Animal Products"] = new string[] {
				"(O)174", "(O)176", "(O)180", "(O)182", "(O)184", "(O)186", "(O)430", "(O)436",
				"(O)438", "(O)440", "(O)442", "(O)444", "(O)446", "(O)812"
			}
		};

		/// <summary> Gets all available item IDs from all categories combined </summary>
		/// <returns> HashSet of all unique item IDs across all categories </returns>
		public static HashSet<string> GetAllItemIds()
		{
			var allItemIds = new HashSet<string>();

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