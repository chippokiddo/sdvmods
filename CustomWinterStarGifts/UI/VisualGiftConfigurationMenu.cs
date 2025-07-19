using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Menus;
using CustomWinterStarGifts.Data;
using Object = StardewValley.Object;

namespace CustomWinterStarGifts.UI
{
	/// <summary> Visual gift configuration menu </summary>
	public class VisualGiftConfigurationMenu : IClickableMenu
	{
		private ModConfig Config;
		private IModHelper Helper;
		private Action<ModConfig> OnComplete;

		private List<ClickableComponent> ItemSlots = new List<ClickableComponent>();
		private List<ClickableComponent> CategoryTabs = new List<ClickableComponent>();
		private List<Object> AllItems = new List<Object>();
		private List<Object> FilteredItems = new List<Object>();
		private List<(int id, int quantity)> LikedGifts = new List<(int, int)>();
		private List<(int id, int quantity)> LovedGifts = new List<(int, int)>();

		private bool ShowingLikedGifts = true;
		private int CurrentCategory = 0;
		private int QuantityMenuItemId = -1;
		private int ScrollPosition = 0;

		// Visual constants
		private const int SLOT_SIZE = 64;
		private const int SLOT_MARGIN = 12;
		private const int SLOTS_PER_ROW = 10;
		private const int VISIBLE_ROWS = 6;
		private const int TAB_HEIGHT = 64;
		private const int MENU_PADDING = 32;
		private const int COUNTER_HEIGHT = 50;

		// Menu regions
		private Rectangle ItemGrid;
		private Rectangle CounterArea;
		private int MaxScroll = 0;

		// UI Components
		private ClickableComponent OkButton;
		private ClickableComponent CancelButton;
		private ClickableComponent ClearButton;
		private ClickableComponent ToggleModeButton;
		private ClickableComponent UpArrow;
		private ClickableComponent DownArrow;

		// Visual feedback
		private Object HoveredItem = null;
		private Rectangle HoveredSlot;
		private float AnimationTimer = 0f;

		public VisualGiftConfigurationMenu(ModConfig config, IModHelper helper, Action<ModConfig> onComplete)
		{
			Config = config;
			Helper = helper;
			OnComplete = onComplete;

			LikedGifts = Config.GetLikedGiftItems();
			LovedGifts = Config.GetLovedGiftItems();

			// Menu sizing
			width = (SLOTS_PER_ROW * (SLOT_SIZE + SLOT_MARGIN)) + (MENU_PADDING * 2);
			height = TAB_HEIGHT + COUNTER_HEIGHT + (VISIBLE_ROWS * (SLOT_SIZE + SLOT_MARGIN)) + 120; // Space for buttons

			xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
			yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

			// Define regions
			CounterArea = new Rectangle(xPositionOnScreen, yPositionOnScreen + TAB_HEIGHT + 8, width, COUNTER_HEIGHT);
			ItemGrid = new Rectangle(
				xPositionOnScreen + MENU_PADDING,
				yPositionOnScreen + TAB_HEIGHT + COUNTER_HEIGHT + 50,
				width - (MENU_PADDING * 2),
				VISIBLE_ROWS * (SLOT_SIZE + SLOT_MARGIN)
			);

			InitializeItems();
			SetupInterface();
		}

		private void InitializeItems()
		{
			var allItemIds = CategoryData.GetAllItemIds();
			AllItems = allItemIds
				.Select(id => new StardewValley.Object(id.ToString(), 1))
				.Where(item => item.Name != "Error Item" && !string.IsNullOrEmpty(item.Name))
				.OrderBy(item => item.getCategoryName())
				.ThenBy(item => item.salePrice())
				.ThenBy(item => item.Name)
				.ToList();

			FilterItemsForCategory();
		}

		private void FilterItemsForCategory()
		{
			if (CurrentCategory == 0) // "All Items"
			{
				FilteredItems = AllItems.ToList();
			}
			else
			{
				var categoryName = CategoryData.CategoryNames[CurrentCategory];
				if (CategoryData.CategoryItems.ContainsKey(categoryName))
				{
					var categoryItemIds = CategoryData.CategoryItems[categoryName].ToHashSet();
					FilteredItems = AllItems.Where(item => categoryItemIds.Contains(item.ParentSheetIndex)).ToList();
				}
			}

			ScrollPosition = 0;
			SetupItemSlots();
		}

		private void SetupInterface()
		{
			// Category tabs
			CategoryTabs.Clear();
			int tabWidth = width / CategoryData.CategoryNames.Length;
			for (int i = 0; i < CategoryData.CategoryNames.Length; i++)
			{
				int tabX = xPositionOnScreen + (i * tabWidth);
				CategoryTabs.Add(new ClickableComponent(
					new Rectangle(tabX, yPositionOnScreen, tabWidth, TAB_HEIGHT),
					i.ToString(),
					CategoryData.CategoryNames[i]
				));
			}

			// Action buttons
			int buttonY = yPositionOnScreen + height - 64;
			int buttonSpacing = 16;
			int buttonWidth = 104;
			int totalButtonWidth = (buttonWidth * 4) + (buttonSpacing * 3);
			int buttonStartX = xPositionOnScreen + (width - totalButtonWidth) / 2;

			OkButton = new ClickableComponent(
				new Rectangle(buttonStartX, buttonY, buttonWidth, 44),
				"OK", "Save and close"
			);

			CancelButton = new ClickableComponent(
				new Rectangle(buttonStartX + buttonWidth + buttonSpacing, buttonY, buttonWidth, 44),
				"Cancel", "Close without saving"
			);

			ClearButton = new ClickableComponent(
				new Rectangle(buttonStartX + (buttonWidth + buttonSpacing) * 2, buttonY, buttonWidth, 44),
				"Default", "Reset to default gifts"
			);

			ToggleModeButton = new ClickableComponent(
				new Rectangle(buttonStartX + (buttonWidth + buttonSpacing) * 3, buttonY, buttonWidth, 44),
				ShowingLikedGifts ? "Liked" : "Loved", "Toggle between Liked and Loved gifts"
			);

			// Scroll arrows
			UpArrow = new ClickableComponent(
				new Rectangle(xPositionOnScreen + width + 24, ItemGrid.Y + 16 - 52, 44, 44),
				"up", "Scroll up"
			);

			DownArrow = new ClickableComponent(
				new Rectangle(xPositionOnScreen + width + 24, ItemGrid.Bottom - 36, 44, 44),
				"down", "Scroll down"
			);

			SetupItemSlots();
		}

		private void SetupItemSlots()
		{
			ItemSlots.Clear();

			int totalRows = (int)Math.Ceiling((double)FilteredItems.Count / SLOTS_PER_ROW);
			MaxScroll = Math.Max(0, totalRows - VISIBLE_ROWS);
			ScrollPosition = Math.Max(0, Math.Min(ScrollPosition, MaxScroll));

			for (int row = 0; row < VISIBLE_ROWS; row++)
			{
				int actualRow = row + ScrollPosition;
				if (actualRow >= totalRows) break;

				for (int col = 0; col < SLOTS_PER_ROW; col++)
				{
					int itemIndex = actualRow * SLOTS_PER_ROW + col;
					if (itemIndex >= FilteredItems.Count) break;

					int slotX = ItemGrid.X + col * (SLOT_SIZE + SLOT_MARGIN);
					int slotY = ItemGrid.Y + row * (SLOT_SIZE + SLOT_MARGIN);

					ItemSlots.Add(new ClickableComponent(
						new Rectangle(slotX, slotY, SLOT_SIZE, SLOT_SIZE),
						itemIndex.ToString()
					));
				}
			}
		}

		public override void receiveScrollWheelAction(int direction)
		{
			if (QuantityMenuItemId >= 0) return;

			int scrollDelta = direction > 0 ? -1 : 1;
			ScrollPosition = Math.Max(0, Math.Min(ScrollPosition + scrollDelta, MaxScroll));
			SetupItemSlots();
		}

		public override void update(GameTime time)
		{
			base.update(time);
			AnimationTimer += (float)time.ElapsedGameTime.TotalMilliseconds;
		}

		public override void draw(SpriteBatch b)
		{
			// Background overlay
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);

			// Main menu background
			Game1.drawDialogueBox(
				xPositionOnScreen - 16, yPositionOnScreen - 16,
				width + 32, height + 32,
				false, true, null, false, true
			);

			// Draw category tabs
			DrawCategoryTabs(b);

			// Draw selection counter area
			DrawSelectionCounter(b);

			// Draw item grid
			DrawItemGrid(b);

			// Draw scroll arrows
			DrawScrollArrows(b);

			// Draw action buttons
			DrawActionButtons(b);

			// Draw quantity selector if active
			if (QuantityMenuItemId >= 0)
			{
				DrawQuantitySelector(b);
			}

			// Draw tooltips last
			DrawTooltips(b);

			// Draw mouse cursor
			this.drawMouse(b);
		}

		// Draw the selection counter
		private void DrawSelectionCounter(SpriteBatch b)
		{
			// Calculate custom dimensions for the counter background
			int counterWidth = width - 200;
			int counterHeight = COUNTER_HEIGHT + 10; 
			int counterX = CounterArea.X + (CounterArea.Width - counterWidth) / 2; // center horizontally
			int counterY = CounterArea.Y + 10;

			// Background for counter area with custom dimensions
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors,
				new Rectangle(384, 396, 15, 15),
				counterX, counterY, counterWidth, counterHeight,
				Color.White, Game1.pixelZoom, false
			);

			// Get current selection counts
			var currentList = ShowingLikedGifts ? LikedGifts : LovedGifts;
			var otherList = ShowingLikedGifts ? LovedGifts : LikedGifts;

			int currentCount = currentList.Count;
			int otherCount = otherList.Count;
			int totalQuantity = currentList.Sum(item => item.quantity);

			// Main counter text for current mode
			string currentModeText = ShowingLikedGifts ? "Liked" : "Loved";
			string countText = $"{currentModeText}: {currentCount} items";
			if (totalQuantity != currentCount)
			{
				countText += $" ({totalQuantity} total)";
			}

			// Secondary counter for other mode
			string otherModeText = ShowingLikedGifts ? "Loved" : "Liked";
			string otherCountText = $"{otherModeText}: {otherCount} items";

			// Calculate text positions - use the custom counter rectangle
			Vector2 currentTextSize = Game1.smallFont.MeasureString(countText);
			Vector2 otherTextSize = Game1.smallFont.MeasureString(otherCountText);

			// Position texts side by side - center in the custom counter area
			float totalTextWidth = currentTextSize.X + otherTextSize.X + 40; // 40px spacing between texts
			float startX = counterX + (counterWidth - totalTextWidth) / 2;
			float textY = counterY + (counterHeight - currentTextSize.Y) / 2; // Center in the taller rectangle

			Vector2 currentTextPos = new Vector2(startX, textY);
			Vector2 otherTextPos = new Vector2(startX + currentTextSize.X + 40, textY);

			// Draw current mode text - highlighted
			Color currentTextColor = ShowingLikedGifts ? Color.Orange : Color.Red;
			Utility.drawTextWithShadow(b, countText, Game1.smallFont, currentTextPos, currentTextColor);

			// Draw other mode text - dimmed
			Color otherTextColor = Color.Gray;
			Utility.drawTextWithShadow(b, otherCountText, Game1.smallFont, otherTextPos, otherTextColor);

			// Draw a small separator line between the texts
			Rectangle separatorRect = new Rectangle(
				(int)(currentTextPos.X + currentTextSize.X + 16),
				(int)(textY + 4),
				8,
				(int)(currentTextSize.Y - 8)
			);
			b.Draw(Game1.staminaRect, separatorRect, Color.Gray * 0.5f);
		}

		private void DrawCategoryTabs(SpriteBatch b)
		{
			for (int i = 0; i < CategoryTabs.Count; i++)
			{
				var tab = CategoryTabs[i];
				bool isSelected = i == CurrentCategory;
				bool isHovered = tab.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());

				// Tab background
				Rectangle sourceRect = new Rectangle(16, 368, 16, 16);

				Color tabColor = Color.White;
				if (isSelected)
				{
					tabColor = Color.White; // Selected stays bright
				}
				else if (isHovered)
				{
					tabColor = Color.Wheat; // Hover effect
				}
				else
				{
					tabColor = Color.Gray; // Unselected is dimmed
				}

				// Draw the tab background - selected tabs draw slightly forward
				int tabY = isSelected ? tab.bounds.Y - 4 : tab.bounds.Y;
				IClickableMenu.drawTextureBox(b, Game1.mouseCursors, sourceRect,
					tab.bounds.X, tabY, tab.bounds.Width, tab.bounds.Height + (isSelected ? 4 : 0),
					tabColor, Game1.pixelZoom, false
				);

				// Tab icon
				var iconItem = new Object(CategoryData.CategoryIcons[i].ToString(), 1);
				if (iconItem.Name != "Error Item")
				{
					float iconScale = 0.75f;
					Vector2 iconPos = new Vector2(
						tab.bounds.X + (tab.bounds.Width - 64 * iconScale) / 2 - 4,
						tabY + (tab.bounds.Height - 64 * iconScale) / 2
					);

					iconItem.drawInMenu(b, iconPos, iconScale, 1f, 0f,
						StackDrawType.Hide, Color.White, true);
				}
			}
		}

		private void DrawItemGrid(SpriteBatch b)
		{
			HoveredItem = null;

			// Draw item slots
			foreach (var slot in ItemSlots)
			{
				int itemIndex = int.Parse(slot.name);
				if (itemIndex >= FilteredItems.Count) continue;

				var item = FilteredItems[itemIndex];
				var currentList = ShowingLikedGifts ? LikedGifts : LovedGifts;
				var selectedItem = currentList.FirstOrDefault(x => x.id == item.ParentSheetIndex);
				bool isSelected = selectedItem.id != 0;
				bool isHovered = slot.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());

				// Slot background
				Rectangle slotSource = isSelected ?
					new Rectangle(128, 128, 64, 64) : // Selected slot
					new Rectangle(64, 128, 64, 64);   // Empty slot

				Color slotColor = Color.White;
				if (isHovered) slotColor = Color.LightBlue;
				if (isSelected) slotColor = ShowingLikedGifts ? Color.Orange : Color.Red;

				b.Draw(Game1.menuTexture, slot.bounds, slotSource, slotColor);

				// Item sprite
				item.drawInMenu(b, new Vector2(slot.bounds.X, slot.bounds.Y), 1f, 1f, 0f,
					StackDrawType.Hide, Color.White, true);

				// Quantity indicator
				if (isSelected && selectedItem.quantity > 1)
				{
					// Position in bottom-right corner
					Vector2 quantityPos = new Vector2(
						slot.bounds.Right - 20,
						slot.bounds.Bottom - 16
					);

					// Draw the quantity text
					Utility.drawTinyDigits(selectedItem.quantity, b, quantityPos, 3f, 1f, Color.White);
				}

				// Store hovered item for tooltip - DON'T draw tooltip here!
				if (isHovered)
				{
					HoveredItem = item;
					HoveredSlot = slot.bounds;
				}
			}
		}

		private void DrawScrollArrows(SpriteBatch b)
		{
			if (MaxScroll <= 0) return;

			// Position scroll bar further to the right
			Rectangle scrollTrack = new Rectangle(
				xPositionOnScreen + width + 24,
				ItemGrid.Y + 16,
				32,
				ItemGrid.Height - 32
			);

			// Draw scroll bar background
			Rectangle trackSource = new Rectangle(403, 383, 6, 6);
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, trackSource,
				scrollTrack.X, scrollTrack.Y, scrollTrack.Width, scrollTrack.Height,
				Color.White, Game1.pixelZoom, false);

			// Calculate scroll thumb position and size
			float scrollPercentage = MaxScroll > 0 ? (float)ScrollPosition / MaxScroll : 0f;
			int thumbHeight = Math.Max(24, scrollTrack.Height / Math.Max(1, (int)Math.Ceiling((double)FilteredItems.Count / SLOTS_PER_ROW) - VISIBLE_ROWS + 1));
			int thumbY = scrollTrack.Y + (int)((scrollTrack.Height - thumbHeight) * scrollPercentage);

			// Draw scroll thumb 
			Rectangle thumbSource = new Rectangle(435, 463, 6, 10);
			IClickableMenu.drawTextureBox(b, Game1.mouseCursors, thumbSource,
				scrollTrack.X + 2, thumbY, scrollTrack.Width - 4, thumbHeight,
				Color.White, Game1.pixelZoom, false);

			// Up arrow - positioned above the scroll track
			bool canScrollUp = ScrollPosition > 0;
			bool upHovered = UpArrow.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());

			Rectangle upArrowSource = new Rectangle(421, 459, 11, 12);
			Color upColor = canScrollUp ? (upHovered ? Color.White : Color.LightGray) : Color.Gray * 0.5f;

			b.Draw(Game1.mouseCursors,
				new Vector2(scrollTrack.X + (scrollTrack.Width - 44) / 2, scrollTrack.Y - 52),
				upArrowSource,
				upColor, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0f
			);

			// Down arrow - positioned below the scroll track
			bool canScrollDown = ScrollPosition < MaxScroll;
			bool downHovered = DownArrow.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());

			Rectangle downArrowSource = new Rectangle(421, 472, 11, 12);
			Color downColor = canScrollDown ? (downHovered ? Color.White : Color.LightGray) : Color.Gray * 0.5f;

			b.Draw(Game1.mouseCursors,
				new Vector2(scrollTrack.X + (scrollTrack.Width - 44) / 2, scrollTrack.Bottom + 8),
				downArrowSource,
				downColor, 0f, Vector2.Zero, Game1.pixelZoom, SpriteEffects.None, 0f
			);
		}

		private void DrawActionButtons(SpriteBatch b)
		{
			DrawButton(b, OkButton, "OK", Color.White);
			DrawButton(b, CancelButton, "Cancel", Color.White);
			DrawButton(b, ClearButton, "Default", Color.White);
			DrawButton(b, ToggleModeButton, ShowingLikedGifts ? "Liked" : "Loved", Color.White);
		}

		private void DrawButton(SpriteBatch b, ClickableComponent button, string text, Color color)
		{
			bool isHovered = button.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());
			Color buttonColor = isHovered ? Color.LightGray : color;

			IClickableMenu.drawTextureBox(b, Game1.mouseCursors,
				new Rectangle(432, 439, 9, 9),
				button.bounds.X, button.bounds.Y, button.bounds.Width, button.bounds.Height,
				buttonColor, Game1.pixelZoom, false
			);

			Vector2 textSize = Game1.smallFont.MeasureString(text);
			Vector2 textPos = new Vector2(
				button.bounds.X + (button.bounds.Width - textSize.X) / 2,
				button.bounds.Y + (button.bounds.Height - textSize.Y) / 2
			);

			Utility.drawTextWithShadow(b, text, Game1.smallFont, textPos, Game1.textColor);
		}

		private void DrawQuantitySelector(SpriteBatch b)
		{
			// Dim behind popup
			b.Draw(Game1.fadeToBlackRect, Game1.graphics.GraphicsDevice.Viewport.Bounds, Color.Black * 0.75f);

			// Select quantity popup
			Rectangle popup = new Rectangle(
				xPositionOnScreen + width / 2 - 200,
				yPositionOnScreen + height / 2 - 140,
				400, 280
			);

			Game1.drawDialogueBox(popup.X, popup.Y, popup.Width, popup.Height, false, true);

			// Select quantity title
			string title = "Select Quantity";
			Vector2 titleSize = Game1.dialogueFont.MeasureString(title);
			Vector2 titlePos = new Vector2(
				popup.X + (popup.Width - titleSize.X) / 2,
				popup.Y + 100
			);
			Utility.drawTextWithShadow(b, title, Game1.dialogueFont, titlePos, Game1.textColor);

			// Quantity buttons 
			int[] quantities = { 1, 5, 10, 25, 50, 99 };
			int buttonsPerRow = 3;
			int buttonWidth = 60;
			int buttonHeight = 32;
			int buttonSpacing = 16;

			// Calculate starting position to center the button grid
			int totalButtonWidth = (buttonWidth * buttonsPerRow) + (buttonSpacing * (buttonsPerRow - 1));
			int startX = popup.X + (popup.Width - totalButtonWidth) / 2;
			int startY = popup.Y + 150;

			for (int i = 0; i < quantities.Length; i++)
			{
				int col = i % buttonsPerRow;
				int row = i / buttonsPerRow;
				int buttonX = startX + col * (buttonWidth + buttonSpacing);
				int buttonY = startY + row * (buttonHeight + buttonSpacing);

				Rectangle buttonRect = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

				bool isHovered = buttonRect.Contains(Game1.getOldMouseX(), Game1.getOldMouseY());
				Color buttonColor = isHovered ? Color.White : Color.LightGray;

				IClickableMenu.drawTextureBox(b, Game1.mouseCursors,
					new Rectangle(432, 439, 9, 9),
					buttonRect.X, buttonRect.Y, buttonRect.Width, buttonRect.Height,
					buttonColor, Game1.pixelZoom, false
				);

				string qtyText = quantities[i].ToString();
				Vector2 qtySize = Game1.smallFont.MeasureString(qtyText);
				Vector2 qtyPos = new Vector2(
					buttonRect.X + (buttonRect.Width - qtySize.X) / 2,
					buttonRect.Y + (buttonRect.Height - qtySize.Y) / 2
				);

				Utility.drawTextWithShadow(b, qtyText, Game1.smallFont, qtyPos, Game1.textColor);
			}
		}

		private void DrawTooltips(SpriteBatch b)
		{
			// Don't show tooltips when quantity selector is active
			if (QuantityMenuItemId >= 0) return;

			// Draw item tooltip if hovering over an item
			if (HoveredItem != null)
			{
				var currentList = ShowingLikedGifts ? LikedGifts : LovedGifts;
				var selectedItem = currentList.FirstOrDefault(x => x.id == HoveredItem.ParentSheetIndex);

				string tooltipText = HoveredItem.getDescription();

				// Add quantity if selected and more than 1
				if (selectedItem.id != 0 && selectedItem.quantity > 1)
				{
					tooltipText = $"(x{selectedItem.quantity})\n" + tooltipText; // Prepend quantity
				}

				// Add interaction hints
				tooltipText += "\n\nLeft-click to select/deselect";
				tooltipText += "\nRight-click to set quantity";

				IClickableMenu.drawHoverText(b, tooltipText, Game1.smallFont, 0, 0, -1,
					HoveredItem.DisplayName);
			}

			// Draw tab tooltip if hovering over a tab
			foreach (var tab in CategoryTabs)
			{
				if (tab.bounds.Contains(Game1.getOldMouseX(), Game1.getOldMouseY()))
				{
					string tabName = CategoryData.CategoryNames[int.Parse(tab.name)];
					IClickableMenu.drawHoverText(b, tabName, Game1.smallFont);
					break; // Only show one tooltip at a time
				}
			}
		}

		public override void receiveLeftClick(int x, int y, bool playSound = true)
		{
			// Handle quantity selector
			if (QuantityMenuItemId >= 0)
			{
				Rectangle popup = new Rectangle(
					xPositionOnScreen + width / 2 - 200,
					yPositionOnScreen + height / 2 - 140,
					400, 280
				);

				if (popup.Contains(x, y))
				{
					int[] quantities = { 1, 5, 10, 25, 50, 99 };
					int buttonsPerRow = 3;
					int buttonWidth = 60;
					int buttonHeight = 32;
					int buttonSpacing = 16;

					int totalButtonWidth = (buttonWidth * buttonsPerRow) + (buttonSpacing * (buttonsPerRow - 1));
					int startX = popup.X + (popup.Width - totalButtonWidth) / 2;
					int startY = popup.Y + 150;

					for (int i = 0; i < quantities.Length; i++)
					{
						int col = i % buttonsPerRow;
						int row = i / buttonsPerRow;
						int buttonX = startX + col * (buttonWidth + buttonSpacing);
						int buttonY = startY + row * (buttonHeight + buttonSpacing);
						Rectangle buttonRect = new Rectangle(buttonX, buttonY, buttonWidth, buttonHeight);

						if (buttonRect.Contains(x, y))
						{
							SetItemQuantity(QuantityMenuItemId, quantities[i]);
							QuantityMenuItemId = -1;
							Game1.playSound("smallSelect");
							return;
						}
					}
				}

				QuantityMenuItemId = -1;
				return;
			}

			// Category tabs
			foreach (var tab in CategoryTabs)
			{
				if (tab.bounds.Contains(x, y))
				{
					CurrentCategory = int.Parse(tab.name);
					FilterItemsForCategory();
					Game1.playSound("smallSelect");
					return;
				}
			}

			// Item slots
			foreach (var slot in ItemSlots)
			{
				if (slot.bounds.Contains(x, y))
				{
					int itemIndex = int.Parse(slot.name);
					if (itemIndex < FilteredItems.Count)
					{
						var item = FilteredItems[itemIndex];
						ToggleItemSelection(item.ParentSheetIndex);
						Game1.playSound("smallSelect");
					}
					return;
				}
			}

			// Scroll arrows
			if (UpArrow.bounds.Contains(x, y) && ScrollPosition > 0)
			{
				ScrollPosition--;
				SetupItemSlots();
				Game1.playSound("shiny4");
			}
			else if (DownArrow.bounds.Contains(x, y) && ScrollPosition < MaxScroll)
			{
				ScrollPosition++;
				SetupItemSlots();
				Game1.playSound("shiny4");
			}

			// Action buttons
			if (OkButton.bounds.Contains(x, y))
			{
				SaveAndExit();
				Game1.playSound("money");
			}
			else if (CancelButton.bounds.Contains(x, y))
			{
				Game1.exitActiveMenu();
				Game1.playSound("cancel");
			}
			else if (ClearButton.bounds.Contains(x, y))
			{
				ClearCurrentSelection();
				Game1.playSound("coin");
			}
			else if (ToggleModeButton.bounds.Contains(x, y))
			{
				ShowingLikedGifts = !ShowingLikedGifts;
				ToggleModeButton.label = ShowingLikedGifts ? "Liked" : "Loved";
				Game1.playSound("drumkit6");
			}
		}

		public override void receiveRightClick(int x, int y, bool playSound = true)
		{
			if (QuantityMenuItemId >= 0) return;

			// Right-click on item to set quantity
			foreach (var slot in ItemSlots)
			{
				if (slot.bounds.Contains(x, y))
				{
					int itemIndex = int.Parse(slot.name);
					if (itemIndex < FilteredItems.Count)
					{
						var item = FilteredItems[itemIndex];
						QuantityMenuItemId = item.ParentSheetIndex;
						Game1.playSound("smallSelect");
					}
					return;
				}
			}
		}

		public override void receiveKeyPress(Keys key)
		{
			if (QuantityMenuItemId >= 0)
			{
				if (key == Keys.Escape)
				{
					QuantityMenuItemId = -1;
				}
				return;
			}

			switch (key)
			{
				case Keys.Tab:
					ShowingLikedGifts = !ShowingLikedGifts;
					ToggleModeButton.label = ShowingLikedGifts ? "Liked" : "Loved";
					Game1.playSound("drumkit6");
					break;

				case Keys.Left:
				case Keys.A:
					if (CurrentCategory > 0)
					{
						CurrentCategory--;
						FilterItemsForCategory();
						Game1.playSound("shwip");
					}
					break;

				case Keys.Right:
				case Keys.D:
					if (CurrentCategory < CategoryData.CategoryNames.Length - 1)
					{
						CurrentCategory++;
						FilterItemsForCategory();
						Game1.playSound("shwip");
					}
					break;

				case Keys.Up:
				case Keys.W:
					if (ScrollPosition > 0)
					{
						ScrollPosition--;
						SetupItemSlots();
						Game1.playSound("shiny4");
					}
					break;

				case Keys.Down:
				case Keys.S:
					if (ScrollPosition < MaxScroll)
					{
						ScrollPosition++;
						SetupItemSlots();
						Game1.playSound("shiny4");
					}
					break;

				case Keys.Enter:
				case Keys.Space:
					SaveAndExit();
					break;

				case Keys.Escape:
					Game1.exitActiveMenu();
					break;

				case Keys.Delete:
				case Keys.Back:
					ClearCurrentSelection();
					Game1.playSound("coin");
					break;
			}
		}

		private void ToggleItemSelection(int itemId)
		{
			var currentList = ShowingLikedGifts ? LikedGifts : LovedGifts;
			var existingItem = currentList.FirstOrDefault(x => x.id == itemId);

			if (existingItem.id != 0)
			{
				currentList.Remove(existingItem);
			}
			else
			{
				currentList.Add((id: itemId, quantity: 1));
			}
		}

		private void SetItemQuantity(int itemId, int quantity)
		{
			var currentList = ShowingLikedGifts ? LikedGifts : LovedGifts;
			var existingItem = currentList.FirstOrDefault(x => x.id == itemId);

			if (existingItem.id != 0)
			{
				currentList.Remove(existingItem);
			}

			if (quantity > 0)
			{
				currentList.Add((id: itemId, quantity: quantity));
			}
		}

		private void ClearCurrentSelection()
		{
			// Reset to default configuration
			var defaultConfig = new ModConfig(); 

			// Reset both lists to defaults
			LikedGifts = defaultConfig.GetLikedGiftItems();
			LovedGifts = defaultConfig.GetLovedGiftItems();

			Game1.addHUDMessage(new HUDMessage("Reset to default gifts!", 2));
		}

		private void SaveAndExit()
		{
			// Format the gift strings with quantities
			Config.LikedGiftIds = string.Join(",", LikedGifts.Select(item =>
				item.quantity == 1 ? item.id.ToString() : $"{item.id}:{item.quantity}"));

			Config.LovedGiftIds = string.Join(",", LovedGifts.Select(item =>
				item.quantity == 1 ? item.id.ToString() : $"{item.id}:{item.quantity}"));

			OnComplete?.Invoke(Config);
			Game1.exitActiveMenu();
		}

		public override bool readyToClose()
		{
			return QuantityMenuItemId < 0;
		}

		public override void gameWindowSizeChanged(Rectangle oldBounds, Rectangle newBounds)
		{
			// Recenter the menu when window size changes
			xPositionOnScreen = (Game1.uiViewport.Width - width) / 2;
			yPositionOnScreen = (Game1.uiViewport.Height - height) / 2;

			// Rebuild the interface with new positions
			SetupInterface();
		}

		protected override void cleanupBeforeExit()
		{
			// Any cleanup needed before the menu closes
			base.cleanupBeforeExit();
		}
	}
}