﻿using System;
using System.Linq;
using System.Collections.Generic;
using ColossalFramework.UI;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Abstract class for building and network BOB tree/prop replacement panels.
	/// </summary>
	internal abstract class BOBInfoPanel : BOBInfoPanelBase
	{
		/// <summary>
		/// Replacement modes.
		/// </summary>
		protected enum ReplacementModes : int
		{
			Individual = 0,
			Grouped,
			All,
			NumModes
		}


		// Layout constants - mode buttons.
		private const float ModeX = Margin + (ToggleSize * 4f);
		protected const float ModeY = ToggleY;

		// Layout constants - detail control sizes.
		protected const float SliderHeight = 38f;
		private const float FieldOffset = SliderHeight + Margin;
		private const float OffsetLabelY = Margin;
		private const float XOffsetY = OffsetLabelY + 20f;
		private const float ZOffsetY = XOffsetY + SliderHeight;
		private const float OffsetPanelHeight = ZOffsetY + SliderHeight;
		protected const float HeightPanelFullHeight = YOffsetY + SliderHeight;
		private const float HeightPanelShortHeight = FixedHeightY + SliderHeight;

		// Layout constants - detail control locations - align bottom with bottom of lists, and work up.
		protected const float RepeatSliderY = ListY + ListHeight - FieldOffset;
		private const float HeightPanelY = RepeatSliderY - HeightPanelShortHeight;
		private const float OffsetPanelY = HeightPanelY - OffsetPanelHeight - Margin;
		private const float YOffsetY = FixedHeightY + 20f;
		protected const float FixedHeightY = OffsetLabelY + 20f;
		private const float AngleY = OffsetPanelY - FieldOffset;
		private const float ProbabilityY = AngleY - FieldOffset;

		// Layout constants - other controls.
		protected const float RandomButtonX = MiddleX + ToggleSize;


		// Panel components.
		protected BOBSlider probabilitySlider, angleSlider, xSlider, ySlider, zSlider;
		protected UIButton hideButton;
		private readonly UICheckBox randomCheck;
		private readonly UICheckBox[] modeChecks;
		protected UIPanel heightPanel;
		private UIPanel anglePanel;

		// Status flags.
		private bool ignoreModeCheckChanged = false;
		protected bool ignoreSliderValueChange = true;
		protected bool ignoreSelectedPrefabChange = true;


		/// <summary>
		/// Mode icon atlas names for prop modes.
		/// </summary>
		protected abstract string[] PropModeAtlas { get; }


		/// <summary>
		/// Mode icon atlas names for tree modes.
		/// </summary>
		protected abstract string[] TreeModeAtlas { get; }


		/// <summary>
		/// Mode icon tootlip keys for prop modes.
		/// </summary>
		protected abstract string[] PropModeTipKeys { get; }


		/// <summary>
		/// Mode icon tootlip keys for tree modes.
		/// </summary>
		protected abstract string[] TreeModeTipKeys { get; }


		/// <summary>
		// Initial tree/prop checked state.
		/// </summary>
		protected override PropTreeModes InitialPropTreeMode => ModSettings.lastPropTreeMode;


		/// <summary>
		/// Currently selected building.
		/// </summary>
		protected virtual BuildingInfo SelectedBuilding => null;


		/// <summary>
		/// Currently selected network.
		/// </summary>
		protected NetInfo SelectedNet => selectedPrefab as NetInfo;


		/// <summary>
		/// Current status of unapplied changes.
		/// </summary>
		protected bool UnappliedChanges
        {
			get => _unappliedChanges;

			set
			{
				// Don't do anything if no changes.
				if (_unappliedChanges != value)
				{
					_unappliedChanges = value;

					// Update apply button atlas to show/hide alert mark as appropriate.
					applyButton.atlas = TextureUtils.LoadSpriteAtlas(value ? "BOB-OkSmallWarn" : "BOB-OkSmall");

					// Set button states for new state.
					UpdateButtonStates();
				}
			}
		}
		private bool _unappliedChanges = false;


		/// <summary>
		/// Sets the current replacement prefab and updates button states accordingly.
		/// </summary>
		internal override PrefabInfo ReplacementPrefab
		{
			set
			{
				base.ReplacementPrefab = value;

				// If not ignoring events and value isn't null, apply live changes.
				if (!ignoreSelectedPrefabChange && value != null)
				{
					PreviewChange();
				}
			}
		}
		
		
		/// <summary>
		/// Sets the current target item and updates button states accordingly.
		/// </summary>
		internal override TargetListItem CurrentTargetItem
		{
			set
			{
				base.CurrentTargetItem = value;

				// Don't show angle slider for trees.
				anglePanel.isVisible = !(value?.CurrentPrefab is TreeInfo);
			}
		}


		/// <summary>
		/// Current replacement mode.
		/// </summary>
		protected virtual ReplacementModes CurrentMode
		{
			get => _currentMode;

			set
			{
				if (_currentMode != value)
				{
					_currentMode = value;

					// Update render overlays.
					if (_currentMode == ReplacementModes.Individual || _currentMode == ReplacementModes.Grouped)
					{
						// Update render target to specific building/net.
						RenderOverlays.CurrentBuilding = SelectedBuilding;
						RenderOverlays.CurrentNet = SelectedNet;
					}
					else
					{
						// Clear render prefab references to render overlays for all prefabs.
						RenderOverlays.CurrentBuilding = null;
						RenderOverlays.CurrentNet = null;
					}
				}
			}
		}
		// Current replacement mode.
		private ReplacementModes _currentMode = ReplacementModes.Grouped;


		/// <summary>
		/// Constructor.
		/// </summary>
		internal BOBInfoPanel()
		{
			try
			{
				// Replacement mode buttons.
				modeChecks = new UICheckBox[(int)ReplacementModes.NumModes];
				for (int i = 0; i < (int)ReplacementModes.NumModes; ++i)
				{
					bool useTreeLabels = PropTreeMode == PropTreeModes.Tree;
					modeChecks[i] = IconToggleCheck(this, ModeX + (i * ToggleSize), ModeY, useTreeLabels ? TreeModeAtlas[i] : PropModeAtlas[i], useTreeLabels ? TreeModeTipKeys[i] : PropModeTipKeys[i]);
					modeChecks[i].objectUserData = i;
					modeChecks[i].eventCheckChanged += ModeCheckChanged;
				}
				// Set initial mode state.
				modeChecks[(int)CurrentMode].isChecked = true;

				// Adjust mode label position to be centred over all mode toggles.
				float modeRight = ModeX + ((float)ReplacementModes.NumModes * ToggleSize);
				float modeOffset = (modeRight - Margin - modeLabel.width) / 2f;
				modeLabel.relativePosition += new Vector3(modeOffset, 0f);

				// Hide button.
				hideButton = AddIconButton(this, MidControlX + ActionSize + ActionSize, ActionsY, ActionSize, "BOB_PNL_HID", TextureUtils.LoadSpriteAtlas("BOB-InvisibleProp"));
				hideButton.eventClicked += HideProp;

				// Probability.
				UIPanel probabilityPanel = Sliderpanel(this, MidControlX, ProbabilityY, SliderHeight);
				probabilitySlider = AddBOBSlider(probabilityPanel, Margin, 0f, MidControlWidth - (Margin * 2f), "BOB_PNL_PRB", 0, 100, 1, "Probability");
				probabilitySlider.TrueValue = 100f;
				probabilitySlider.LimitToVisible = true;

				// Angle.
				anglePanel = Sliderpanel(this, MidControlX, AngleY, SliderHeight);
				angleSlider = AddBOBSlider(anglePanel, Margin, 0f, MidControlWidth - (Margin * 2f), "BOB_PNL_ANG", -180, 180, 1, "Angle");

				// Offset panel.
				UIPanel offsetPanel = Sliderpanel(this, MidControlX, OffsetPanelY, OffsetPanelHeight);
				UILabel offsetLabel = UIControls.AddLabel(offsetPanel, 0f, OffsetLabelY, Translations.Translate("BOB_PNL_OFF"));
				offsetLabel.textAlignment = UIHorizontalAlignment.Center;
				while (offsetLabel.width > MidControlWidth)
				{
					offsetLabel.textScale -= 0.05f;
					offsetLabel.PerformLayout();
				}
				offsetLabel.relativePosition = new Vector2((offsetPanel.width - offsetLabel.width) / 2f, OffsetLabelY);

				// Offset sliders.
				xSlider = AddBOBSlider(offsetPanel, Margin, XOffsetY, MidControlWidth - (Margin * 2f), "BOB_PNL_XOF", -16f, 16f, 0.01f, "X offset");
				zSlider = AddBOBSlider(offsetPanel, Margin, ZOffsetY, MidControlWidth - (Margin * 2f), "BOB_PNL_ZOF", -16f, 16f, 0.01f, "Z offset");

				// Height panel.
				heightPanel = Sliderpanel(this, MidControlX, HeightPanelY, HeightPanelShortHeight);
				UILabel heightLabel = UIControls.AddLabel(heightPanel, 0f, OffsetLabelY, Translations.Translate("BOB_PNL_HEI"));
				ySlider = AddBOBSlider(heightPanel, Margin, YOffsetY - 20f, MidControlWidth - (Margin * 2f), "BOB_PNL_YOF", -16f, 16f, 0.01f, "Y offset");
				while (heightLabel.width > MidControlWidth)
				{
					heightLabel.textScale -= 0.05f;
					heightLabel.PerformLayout();
				}
				heightLabel.relativePosition = new Vector2((heightPanel.width - heightLabel.width) / 2f, OffsetLabelY);

				// Live application of position changes.
				xSlider.eventValueChanged += SliderChange;
				ySlider.eventValueChanged += SliderChange;
				zSlider.eventValueChanged += SliderChange;
				angleSlider.eventValueChanged += SliderChange;
				probabilitySlider.eventValueChanged += SliderChange;

				// Normal/random toggle.
				randomCheck = UIControls.LabelledCheckBox((UIComponent)(object)this, hideVanilla.relativePosition.x, hideVanilla.relativePosition.y + hideVanilla.height + (Margin / 2f), Translations.Translate("BOB_PNL_RSW"), 12f, 0.7f);
				randomCheck.eventCheckChanged += RandomCheckChanged;

				// Random settings button.
				UIButton randomButton = AddIconButton(this, RandomButtonX, ToggleY, ToggleSize, "BOB_PNL_RST", TextureUtils.LoadSpriteAtlas("BOB-Random"));
				randomButton.eventClicked += (control, clickEvent) => BOBRandomPanel.Create();

				// Set initial button states.
				UpdateButtonStates();
				UpdateModeIcons();
			}
			catch (Exception e)
			{
				// Log and report any exception.
				Logging.LogException(e, "exception creating info panel");
			}
		}


		/// <summary>
		/// Performs any actions-on-close for the panel.
		/// </summary>
		internal override void Close()
		{
			// Perform post-update tasks, such as saving the config file and refreshing renders.
			FinishUpdate();
		}


		/// <summary>
		/// Sets the target prefab.
		/// </summary>
		/// <param name="targetPrefabInfo">Target prefab to set</param>
		internal override void SetTarget(PrefabInfo targetPrefabInfo)
		{
			// First, undo any preview.
			RevertPreview();

			base.SetTarget(targetPrefabInfo);

			// Title label.
			SetTitle(Translations.Translate("BOB_NAM") + ": " + GetDisplayName(targetPrefabInfo.name));
		}


		/// <summary>
		/// Refreshes the random prop/tree list.
		/// </summary>
		internal void RefreshRandom()
        {
			if (randomCheck.isChecked)
            {
				LoadedList();
            }
        }


		/// <summary>
		/// Reverts any previewed changes back to original prop/tree state.
		/// </summary>
		protected abstract void RevertPreview();


		/// <summary>
		/// Previews the current change.
		/// </summary>
		protected abstract void PreviewChange();


		/// <summary>
		/// Event handler for ptop/tree checkbox changes.
		/// </summary>
		/// <param name="control">Calling component</param>
		/// <param name="isChecked">New checked state</param>
		protected override void PropTreeCheckChanged(UIComponent control, bool isChecked)
        {
			base.PropTreeCheckChanged(control, isChecked);


			// Save last used mode.
			ModSettings.lastPropTreeMode = PropTreeMode;

			// Update mode icons.
			UpdateModeIcons();
        }


		/// <summary>
		/// Updates button states (enabled/disabled) according to current control states.
		/// </summary>
		protected override void UpdateButtonStates()
		{
			// Disable by default (selectively (re)-enable if eligible).
			applyButton.Disable();
			revertButton.Disable();
			hideButton.Disable();

			// Buttons are only enabled if a current target item is selected.
			if (CurrentTargetItem != null)
			{
				// Replacement requires a valid replacement selection.
				if (ReplacementPrefab != null)
				{
					applyButton.Enable();
				}

				// Reversion requires a currently active replacement.
				if (CurrentTargetItem.ActiveReplacement)
				{
					revertButton.Enable();
					revertButton.tooltip = Translations.Translate("BOB_PNL_REV_UND");
				}
				else
				{
					revertButton.tooltip = Translations.Translate("BOB_PNL_REV_TIP");
				}

				// Hide button is enabled whenever there's a valid target item.
				hideButton.Enable();
			}

			// Show revert button if unapplied changes.
			if (UnappliedChanges)
            {
				revertButton.Enable();
				revertButton.tooltip = Translations.Translate("BOB_PNL_REV_UND");
			}
		}


		/// <summary>
		/// Populates a fastlist with a filtered list of loaded trees or props.
		/// </summary>
		protected override void LoadedList()
		{
			// Are we using random props?
			if (randomCheck.isChecked)
			{
				// Yes - show only random trees/props.
				if (PropTreeMode == PropTreeModes.Tree)
				{
					// Trees.
					loadedList.rowsData = new FastList<object>
					{
						m_buffer = PrefabLists.RandomTrees.OrderBy(x => x.name.ToLower()).ToArray(),
						m_size = PrefabLists.RandomTrees.Count
					};
				}
				else if (PropTreeMode == PropTreeModes.Prop)
				{
					// Props.
					loadedList.rowsData = new FastList<object>
					{
						m_buffer = PrefabLists.RandomProps.OrderBy(x => x.name.ToLower()).ToArray(),
						m_size = PrefabLists.RandomProps.Count
					};
				}
				else if (PropTreeMode == PropTreeModes.Both)
				{
					// Trees and props - combine both.
					List<BOBRandomPrefab> randomList = new List<BOBRandomPrefab>(PrefabLists.RandomProps.Count + PrefabLists.RandomTrees.Count);
					randomList.AddRange(PrefabLists.RandomProps);
					randomList.AddRange(PrefabLists.RandomTrees);

					loadedList.rowsData = new FastList<object>
					{
						m_buffer = randomList.OrderBy(x => x.name.ToLower()).ToArray(),
						m_size = randomList.Count
					};
				}
				// Reverse order of filtered list if we're searching name descending.
				if (loadedSearchStatus == (int)OrderBy.NameDescending)
				{
					Array.Reverse(loadedList.rowsData.m_buffer);
					loadedList.Refresh();
				}

				// Clear selection.
				loadedList.selectedIndex = -1;
			}
			else
			{
				// No - show normal loaded prefab list.
				base.LoadedList();
			}
		}


		/// <summary>
		/// Sets the sliders to the values specified in the given replacement record.
		/// </summary>
		/// <param name="replacement">Replacement record to use</param>
		protected virtual void SetSliders(BOBReplacementBase replacement)
		{
			// Disable events.
			ignoreSliderValueChange = true;

			// Null check first.
			if (replacement == null)
			{
				// In the absense of valid data, set all offset fields to defaults.
				angleSlider.TrueValue = 0f;
				xSlider.TrueValue = 0;
				ySlider.TrueValue = 0;
				zSlider.TrueValue = 0;
				probabilitySlider.TrueValue = CurrentTargetItem != null ? CurrentTargetItem.originalProb : 100;
			}
			else
			{
				// Valid replacement - set slider values.
				angleSlider.TrueValue = replacement.angle;
				xSlider.TrueValue = replacement.offsetX;
				ySlider.TrueValue = replacement.offsetY;
				zSlider.TrueValue = replacement.offsetZ;
				probabilitySlider.TrueValue = replacement.probability;
			}

			// Re-enable events.
			ignoreSliderValueChange = false;
		}


		/// <summary>
		/// Adds a slider panel to the specified component.
		/// </summary>
		/// <param name="parent">Parent component</param>
		/// <param name="parent">Parent component</param>
		/// <param name="xPos">Relative X position</param>
		/// <param name="yPos">Relative Y position</param>
		/// <param name="height">Panel height</param>
		/// <returns>New UIPanel</returns>
		protected UIPanel Sliderpanel(UIComponent parent, float xPos, float yPos, float height)
		{
			// Slider panel.
			UIPanel sliderPanel = parent.AddUIComponent<UIPanel>();
			sliderPanel.atlas = TextureUtils.InGameAtlas;
			sliderPanel.backgroundSprite = "GenericPanel";
			sliderPanel.color = new Color32(206, 206, 206, 255);
			sliderPanel.size = new Vector2(MidControlWidth, height);
			sliderPanel.relativePosition = new Vector2(xPos, yPos);

			return sliderPanel;
		}


		/// <summary>
		/// Event handler for applying live changes on slider value change.
		/// </summary>
		/// <param name="control">Calling component</param>
		/// <param name="isChecked">New checked state</param>
		protected void SliderChange(UIComponent control, float value)
		{
			// Don't do anything if already ignoring events.
			if (!ignoreSliderValueChange)
			{
				// Disable events while applying changes.
				ignoreSliderValueChange = true;
				PreviewChange();
				ignoreSliderValueChange = false;
			}
		}


		/// <summary>
		/// Event handler for mode checkbox changes.
		/// </summary>
		/// <param name="control">Calling component</param>
		/// <param name="isChecked">New checked state</param>
		private void ModeCheckChanged(UIComponent control, bool isChecked)
		{
			// Don't do anything if we're ignoring events.
			if (ignoreModeCheckChanged)
			{
				return;
			}

			// Suspend event handling while processing.
			ignoreModeCheckChanged = true;

			if (control is UICheckBox thisCheck)
			{
				// If this checkbox is being enabled, uncheck all others:
				if (isChecked)
				{
					// Don't do anything if the selected mode index isn't different to the current mode.
					if (thisCheck.objectUserData is int index && index != (int)CurrentMode)
					{
						// Iterate through all checkboxes, unchecking all those that aren't this one (checkbox index stored in objectUserData).
						for (int i = 0; i < (int)ReplacementModes.NumModes; ++i)
						{
							if (i != index)
							{
								modeChecks[i].isChecked = false;
							}
						}

						// Set current replacement mode, while saving old value.
						ReplacementModes oldMode = CurrentMode;
						CurrentMode = (ReplacementModes)index;

						// Update target list if we've changed between individual and grouped modes (we've already filtered out non-changes, so checking for any individual mode will do).
						if (oldMode == ReplacementModes.Individual || CurrentMode == ReplacementModes.Individual)
						{
							// Rebuild target list.
							TargetList();

							// Clear selection.
							targetList.selectedIndex = -1;
							CurrentTargetItem = null;
						}
					}
				}
				else
				{
					// If no other check is checked, force this one to still be checked.
					thisCheck.isChecked = true;
				}
			}

			// Resume event handling.
			ignoreModeCheckChanged = false;
		}


		/// <summary>
		/// Updates mode icons and tooltips (when switching between trees and props).
		/// </summary>
		private void UpdateModeIcons()
		{
			string[] atlasNames, tipKeys;

			// Null check to avoid race condition with base constructor.
			if (modeChecks == null)
            {
				return;
            }				

			// Get releveant atlases and tooltips for current mode.
			if (PropTreeMode == PropTreeModes.Tree)
            {
				atlasNames = TreeModeAtlas;
				tipKeys = TreeModeTipKeys;
            }
			else
            {
				atlasNames = PropModeAtlas;
				tipKeys = PropModeTipKeys;
            }

			// Iterate through all mode checks.
			for (int i = 0; i < (int)ReplacementModes.NumModes; ++i)
			{
				// Load atlas.
				UITextureAtlas checkAtlas = TextureUtils.LoadSpriteAtlas(atlasNames[i]);

				// Update unchecked sprite. 
				UISprite uncheckedSprite = modeChecks[i].Find<UISprite>("UncheckedSprite");
				uncheckedSprite.atlas = checkAtlas;

				// Update checked sprite.
				((UISprite)modeChecks[i].checkedBoxObject).atlas = checkAtlas;

				// Update tooltip.
				modeChecks[i].tooltip = Translations.Translate(tipKeys[i]);
			}
		}


		/// <summary>
		/// Random check event handler.
		/// </summary>
		/// <param name="control">Calling component (unused)</param>
		/// <param name="isChecked">New checked state</param>
		private void RandomCheckChanged(UIComponent control, bool isChecked)
		{
			// Regenerate loaded list.
			LoadedList();
		}


		/// <summary>
		/// Hides the selected prop/tree.
		/// <param name="control">Calling component (unused)</param>
		/// <param name="clickEvent">Mouse click event (unused)</param>
		/// </summary>
		private void HideProp(UIComponent control, UIMouseEventParameter clickevent) => probabilitySlider.TrueValue = 0f;


		/// <summary>
		/// Returns a cleaned-up display name for the given prefab.
		/// </summary>
		/// <param name="prefabName">Raw prefab name</param>
		/// <returns>Cleaned display name</returns>
		private string GetDisplayName(string prefabName) => prefabName.Substring(prefabName.IndexOf('.') + 1).Replace("_Data", "");
	}
}
