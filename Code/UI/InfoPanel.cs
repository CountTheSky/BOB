﻿using System;
using System.Collections.Generic;
using System.Linq;
using ColossalFramework;
using ColossalFramework.UI;
using UnityEngine;


namespace BOB
{
	public abstract class BOBInfoPanel : UIPanel
	{
		// Layout constants.
		protected const float LeftWidth = 500f;
		protected const float MiddleWidth = 200f;
		protected const float RightWidth = 400f;
		protected const float PanelHeight = 490f;
		protected const float TitleHeight = 45f;
		protected const float ToolbarHeight = 50f;
		protected const float Margin = 5f;

		// Component locations.
		protected const float ProbabilityY = 95f;
		protected const float ReplaceLabelY = 185f;
		protected const float ReplaceY = ReplaceLabelY + 35f;
		protected const float ReplaceAllY = ReplaceY + 35f;
		protected const float RevertY = ReplaceAllY + 50f;

		// Current selections.
		protected PrefabInfo selectedPrefab;
		protected PropListItem currentTargetItem;
		protected PrefabInfo replacementPrefab;
		protected int probability;

		// Panel components.
		protected UIFastList targetList;
		protected UIFastList loadedList;
		protected UIButton replaceButton;
		protected UIButton replaceAllButton;
		protected UIButton revertButton;
		private UICheckBox hideVanilla;
		private UITextField nameFilter;
		protected UILabel noPropsLabel;

		// Button labels.
		protected abstract string ReplaceLabel { get; }
		protected abstract string ReplaceAllLabel { get; }


		// Trees or props?
		protected abstract bool IsTree { get; }


		/// <summary>
		/// Sets the current target item and updates button states accordingly.
		/// </summary>
		internal virtual PropListItem CurrentTargetItem
		{
			set
			{
				currentTargetItem = value;
				UpdateButtonStates();
			}
		}


		/// <summary>
		/// Sets the current replacement prefab and updates buttons states accordingly.
		/// </summary>
		internal PrefabInfo ReplacementPrefab
		{
			set
			{
				replacementPrefab = value;
				UpdateButtonStates();
			}
		}


		/// <summary>
		/// Performs initial setup 
		/// </summary>
		/// <param name="parentTransform">Parent transform</param>
		/// <param name="targetPrefabInfo">Currently selected target prefab</param>
		internal virtual void Setup(Transform parentTransform, PrefabInfo targetPrefabInfo)
		{
			try
			{
				// Set target reference.
				selectedPrefab = targetPrefabInfo;

				// Basic behaviour.
				transform.parent = parentTransform;
				autoLayout = false;
				canFocus = true;
				isInteractive = true;

				// Appearance.
				backgroundSprite = "MenuPanel2";
				opacity = 0.8f;

				// Size and position.
				size = new Vector2(LeftWidth + MiddleWidth + RightWidth + (Margin * 4), PanelHeight + TitleHeight + ToolbarHeight + (Margin * 3));
				relativePosition = new Vector2(Mathf.Floor((GetUIView().fixedWidth - width) / 2), Mathf.Floor((GetUIView().fixedHeight - height) / 2));

				// Drag bar.
				UIDragHandle dragHandle = AddUIComponent<UIDragHandle>();
				dragHandle.width = this.width - 50f;
				dragHandle.height = this.height;
				dragHandle.relativePosition = Vector3.zero;
				dragHandle.target = this;

				// Title label.
				UILabel titleLabel = AddUIComponent<UILabel>();
				titleLabel.relativePosition  = new Vector2(50f, 13f);
				titleLabel.text = Translations.Translate("BOB_NAM");

				// Close button.
				UIButton closeButton = AddUIComponent<UIButton>();
				closeButton.relativePosition = new Vector3(width - 35, 2);
				closeButton.normalBgSprite = "buttonclose";
				closeButton.hoveredBgSprite = "buttonclosehover";
				closeButton.pressedBgSprite = "buttonclosepressed";
				closeButton.eventClick += (component, clickEvent) =>
				{
					InfoPanelManager.Close();
				};

				// Target prop list.
				UIPanel leftPanel = AddUIComponent<UIPanel>();
				leftPanel.width = LeftWidth;
				leftPanel.height = PanelHeight;
				leftPanel.relativePosition = new Vector2(Margin, TitleHeight + ToolbarHeight);
				targetList = UIFastList.Create<UIPrefabPropRow>(leftPanel);
				ListSetup(targetList);

				// Loaded prop list.
				UIPanel rightPanel = AddUIComponent<UIPanel>();
				rightPanel.width = RightWidth;
				rightPanel.height = PanelHeight;
				rightPanel.relativePosition = new Vector2(LeftWidth + MiddleWidth + (Margin * 3), TitleHeight + ToolbarHeight);
				loadedList = UIFastList.Create<UILoadedPropRow>(rightPanel);
				ListSetup(loadedList);

				// 'No props' label (starts hidden).
				noPropsLabel = leftPanel.AddUIComponent<UILabel>();
				noPropsLabel.relativePosition = new Vector2(Margin, Margin);
				noPropsLabel.Hide();

				// Name filter.
				nameFilter = UIUtils.LabelledTextField(this, Translations.Translate("BOB_FIL_NAME"));
				nameFilter.relativePosition = new Vector2(width - nameFilter.width - Margin, 40f);

				// Vanilla filter.
				hideVanilla = UIUtils.AddCheckBox((UIComponent)(object)this, Translations.Translate("BOB_PNL_HDV"), nameFilter.relativePosition.x, 75f);
				hideVanilla.eventCheckChanged += (control, isChecked) =>
				{
					// Filter list.
					loadedList.rowsData = LoadedList(IsTree);

					// Store state.
					ModSettings.hideVanilla = isChecked;
				};

				// Replace text label.
				UILabel replaceLabel = AddUIComponent<UILabel>();
				replaceLabel.text = Translations.Translate("BOB_PNL_REP");
				replaceLabel.relativePosition = new Vector2(LeftWidth + (Margin * 2), ReplaceLabelY);

				// Replace button.
				replaceButton = UIUtils.CreateButton(this, ReplaceLabel, 190f, LeftWidth + (Margin * 2), ReplaceY);

				// Replace all button.
				replaceAllButton = UIUtils.CreateButton(this, ReplaceAllLabel, 190f, LeftWidth + (Margin * 2), ReplaceAllY);

				// Revert button.
				revertButton = UIUtils.CreateButton(this, Translations.Translate("BOB_PNL_REV"), 190f, LeftWidth + (Margin * 2), RevertY);

				// Event handlers for name filter textbox.
				nameFilter.eventTextChanged += (control, text) =>
				{
					loadedList.rowsData = LoadedList(IsTree);
				};
				nameFilter.eventTextSubmitted += (control, text) =>
				{
					loadedList.rowsData = LoadedList(IsTree);
				};

				// Set initial button and checkbox states.
				hideVanilla.isChecked = ModSettings.hideVanilla;
				UpdateButtonStates();
			}
			catch (Exception exception)
			{
				Debugging.LogException(exception);
			}
		}


		/// <summary>
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		protected void UpdateButtonStates()
		{
			// Disable by default (selectively (re)-enable if eligible).
			replaceButton.Disable();
			replaceAllButton.Disable();
			revertButton.Disable();

			// Buttons are only enabled if a current target item is selected.
			if (currentTargetItem != null)
			{
				// Reversion requires a currently active replacement (for the relevant target/all-building setting).
				if (currentTargetItem.currentPrefab != null || currentTargetItem.allPrefab != null)
				{
					revertButton.Enable();
				}

				// Replacement requires a valid replacement selection.
				if (replacementPrefab != null)
				{
					replaceButton.Enable();
					replaceAllButton.Enable();
				}
			}
		}


		/// <summary>
		/// Performs initial fastlist setup.
		/// </summary>
		/// <param name="fastList">Fastlist to set up</param>
		private void ListSetup(UIFastList fastList)
		{
			// Apperance, size and position.
			fastList.backgroundSprite = "UnlockingPanel";
			fastList.width = fastList.parent.width;
			fastList.height = fastList.parent.height;
			fastList.relativePosition = Vector2.zero;
			fastList.rowHeight = 30f;

			// Behaviour.
			fastList.canSelect = true;
			fastList.autoHideScrollbar = true;

			// Data.
			fastList.rowsData = new FastList<object>();
			fastList.selectedIndex = -1;
		}


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		protected FastList<object> LoadedList(bool isTree)
		{
			// List of prefabs that have passed filtering.
			List<PrefabInfo> list = new List<PrefabInfo>();

			// Clear current selection.
			loadedList.selectedIndex = -1;

			// Tree or prop?
			if (isTree)
			{
				// Tree - iterate through each tree in our list of loaded prefabs.
				foreach (TreeInfo loadedTree in PrefabLists.loadedTrees)
				{
					// Set display name.
					string displayName = UIUtils.GetDisplayName(loadedTree.name);

					// Apply vanilla filtering if selected.
					if (!hideVanilla.isChecked || !displayName.StartsWith("[v]"))
					{
						// Apply name filter.
						if (StringExtensions.IsNullOrWhiteSpace(nameFilter.text.Trim()) || displayName.ToLower().Contains(nameFilter.text.Trim().ToLower()))
						{
							// Filtering passed - add this prefab to our list.
							list.Add(loadedTree);
						}
					}
				}
			}
			else
			{
				// Prop - iterate through each prop in our list of loaded prefabs.
				foreach (PropInfo loadedProp in PrefabLists.loadedProps)
				{
					// Set display name.
					string displayName = UIUtils.GetDisplayName(loadedProp.name);

					// Apply vanilla filtering if selected.
					if (!hideVanilla.isChecked || !displayName.StartsWith("[v]"))
					{
						// Apply name filter.
						if (StringExtensions.IsNullOrWhiteSpace(nameFilter.text.Trim()) || displayName.ToLower().Contains(nameFilter.text.Trim().ToLower()))
						{
							// Filtering passed - add this prefab to our list.
							list.Add(loadedProp);
						}
					}
				}
			}

			// Create return fastlist from our filtered list, ordering by name.
			FastList<object> fastList = new FastList<object>();
			object[] array = fastList.m_buffer = list.OrderBy(item => UIUtils.GetDisplayName(item.name)).ToArray();
			fastList.m_size = list.Count;
			return fastList;
		}


		/// <summary>
		/// Populates a fastlist with a list of target-specific trees or props.
		/// </summary>
		/// <param name="isTree">True for a list of trees, false for props</param>
		/// <returns>Populated fastlist of loaded prefabs</returns>
		protected abstract FastList<object> TargetList(bool isTree);
	}
}
