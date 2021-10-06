﻿using System.Collections.Generic;
using UnityEngine;


namespace BOB
{
	/// <summary>
	/// Class to manage network prop and tree replacements.
	/// </summary>
	internal class NetworkReplacement
	{
		// Instance reference.
		internal static NetworkReplacement instance;

		// Master dictionary of replaced prop references.
		internal Dictionary<NetInfo, Dictionary<PrefabInfo, BOBNetReplacement>> replacements;


		/// <summary>
		/// Constructor - initializes instance reference and calls initial setup.
		/// </summary>
		internal NetworkReplacement()
		{
			instance = this;
			Setup();
		}


		/// <summary>
		/// Performs setup and initialises the master dictionary.  Must be called prior to use.
		/// </summary>
		internal void Setup()
		{
			replacements = new Dictionary<NetInfo, Dictionary<PrefabInfo, BOBNetReplacement>>();
		}


		/// <summary>
		/// Reverts all active network replacements and re-initialises the master dictionary.
		/// </summary>
		internal void RevertAll()
		{
			foreach (NetInfo network in replacements.Keys)
			{
				// Iterate through each entry in the master prop dictionary.
				foreach (PrefabInfo prop in replacements[network].Keys)
				{
					// Revert this replacement (but don't remove the entry, as the dictionary is currently immutable while we're iterating through it).
					Revert(network, prop, removeEntries: false);
				}
			}

			// Re-initialise the dictionaries.
			Setup();
		}


		/// <summary>
		/// Reverts a network replacement.
		/// </summary>
		/// <param name="network">Targeted network</param>
		/// <param name="target">Targeted (original) tree/prop prefab</param>
		/// <param name="removeEntries">True (default) to remove the reverted entries from the master dictionary, false to leave the dictionary unchanged</param>
		/// <returns>True if the entire network record was removed from the dictionary (due to no remaining replacements for that prefab), false if the prefab remains in the dictionary (has other active replacements)</returns>
		internal void Revert(NetInfo network, PrefabInfo target, bool removeEntries = true)
		{
			// Safety check.
			if (network == null || !replacements.ContainsKey(network))
            {
				return;
            }

			// Iterate through each entry in our dictionary.
			foreach (NetPropReference propReference in replacements[network][target].references)
			{
				// Revert entry.
				if (target is PropInfo propTarget)
				{
					propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalProp = propTarget;
				}
				else
                {
					propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalTree = (TreeInfo)target;
				}
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_angle = propReference.angle;
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_position = propReference.position;
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_probability = propReference.probability;

				// Restore any all-network replacement.
				if (!AllNetworkReplacement.instance.Restore(network, target, propReference.laneIndex, propReference.propIndex))
				{
					// No all-network restoration occured - restore any pack replacement.
					PackReplacement.instance.Restore(network, target, propReference.laneIndex, propReference.propIndex);
				}

				// Add network to dirty list.
				NetData.DirtyList.Add(propReference.network);
			}

			// Remove entry from dictionary, if we're doing so.
			if (removeEntries)
			{
				replacements[network].Remove(target);

				// Delete entire network entry if nothing left after removing this one.
				if (replacements[network].Count == 0)
                {
					replacements.Remove(network);
                }
			}
		}


		/// <summary>
		/// Applies a new (or updated) network replacement.
		/// </summary>
		/// <param name="network">Targeted network</param>
		/// <param name="target">Targeted (original) prop prefab</param>
		/// <param name="replacement">Replacment prop prefab</param>
		/// <param name="angle">Replacment prop angle adjustment</param>
		/// <param name="offsetX">Replacment X position offset</param>
		/// <param name="offsetY">Replacment Y position offset</param>
		/// <param name="offsetZ">Replacment Z position offset</param>
		/// <param name="probability">Replacement probability</param>
		internal void Apply(NetInfo network, PrefabInfo target, PrefabInfo replacement, float angle, float offsetX, float offsetY, float offsetZ, int probability)
		{
			// Safety check.
			if (network?.m_lanes == null)
			{
				return;
			}

			// Make sure that target and replacement are the same type before doing anything.
			if (target == null || replacement == null || (target is TreeInfo && !(replacement is TreeInfo)) || (target is PropInfo) && !(replacement is PropInfo))
			{
				return;
			}

			// Check to see if we already have a replacement entry for this prop - if so, revert the replacement first.
			if (replacements.ContainsKey(network) && replacements[network].ContainsKey(target))
			{
				Revert(network, target, true);
			}

			// Create new dictionary entry for network if none already exists.
			if (!replacements.ContainsKey(network))
			{
				replacements.Add(network, new Dictionary<PrefabInfo, BOBNetReplacement>());
			}

			// Create new dictionary entry for prop if none already exists.
			if (!replacements[network].ContainsKey(target))
			{
				replacements[network].Add(target, new BOBNetReplacement());
			}

			// Add/replace dictionary replacement data.
			replacements[network][target].references = new List<NetPropReference>();
			replacements[network][target].tree = target is TreeInfo;
			replacements[network][target].targetInfo = target;
			replacements[network][target].target = target.name;
			replacements[network][target].angle = angle;
			replacements[network][target].offsetX = offsetX;
			replacements[network][target].offsetY = offsetY;
			replacements[network][target].offsetZ = offsetZ;
			replacements[network][target].probability = probability;

			// Record replacement prop.
			replacements[network][target].replacementInfo = replacement;
			replacements[network][target].Replacement = replacement.name;

			// Iterate through each lane.
			for (int laneIndex = 0; laneIndex < network.m_lanes.Length; ++laneIndex)
			{
				// If no props in this lane, skip it and go to the next one.
				if (network.m_lanes[laneIndex].m_laneProps?.m_props == null)
				{
					continue;
				}

				// Iterate through each prop in lane.
				for (int propIndex = 0; propIndex < network.m_lanes[laneIndex].m_laneProps.m_props.Length; ++propIndex)
				{
					// Check for any existing pack or all-network replacement.
					PrefabInfo thisProp = PackReplacement.instance.GetOriginal(network, laneIndex, propIndex) ?? AllNetworkReplacement.instance.GetOriginal(network, laneIndex, propIndex);
					if (thisProp == null)
                    {
						// No active replacement; use current PropInfo.
						if (target is PropInfo)
						{
							thisProp = network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalProp;
						}
						else
						{
							thisProp = network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_finalTree;
						}
					}

					// See if this prop matches our replacement.
					if (thisProp != null && thisProp == target)
					{
						// Match!  Add reference data to the list.
						replacements[network][target].references.Add(new NetPropReference
						{
							network = network,
							laneIndex = laneIndex,
							propIndex = propIndex,
							angle = network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_angle,
							position = network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_position,
							probability = network.m_lanes[laneIndex].m_laneProps.m_props[propIndex].m_probability
						});
					}
				}
			}

			// Now, iterate through each entry found.
			foreach (NetPropReference propReference in replacements[network][target].references)
			{
				// Reset any pack or all-network replacements first.
				PackReplacement.instance.RemoveEntry(network, target, propReference.laneIndex, propReference.propIndex);
				AllNetworkReplacement.instance.RemoveEntry(network, target, propReference.laneIndex, propReference.propIndex);

				// Apply the replacement.
				ReplaceProp(replacements[network][target], propReference);
			}
		}


		/// <summary>
		/// Checks if there's a currently active network replacement applied to the given network prop index, and if so, returns the *original* prefab.
		/// </summary>
		/// <param name="netPrefab">Network prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Original prefab if a network replacement is currently applied, null if no network replacement is currently applied</returns>
		internal PrefabInfo GetOriginal(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Safety check.
			if (netPrefab != null && replacements.ContainsKey(netPrefab))
			{
				// Iterate through each entry in master dictionary.
				foreach (PrefabInfo target in replacements[netPrefab].Keys)
				{
					BOBNetReplacement reference = replacements[netPrefab][target];
					// Iterate through each network in this entry.
					foreach (NetPropReference propRef in reference.references)
					{
						// Check for a network, lane, and prop index match.
						if (propRef.network == netPrefab && propRef.laneIndex == laneIndex && propRef.propIndex == propIndex)
						{
							// Match!  Return the original prefab.
							return target;
						}
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Checks if there's a currently active network replacement applied to the given network prop index, and if so, returns the replacement record.
		/// </summary>
		/// <param name="netPrefab">Network prefab to check</param>
		/// <param name="laneIndex">Lane index to check</param>
		/// <param name="propIndex">Prop index to check</param>
		/// <returns>Replacement record if a network replacement is currently applied, null if no network replacement is currently applied</returns>
		internal BOBNetReplacement ActiveReplacement(NetInfo netPrefab, int laneIndex, int propIndex)
		{
			// Safety check.
			if (netPrefab != null && replacements.ContainsKey(netPrefab))
			{
				// Iterate through each entry in master dictionary.
				foreach (PrefabInfo target in replacements[netPrefab].Keys)
				{
					BOBNetReplacement reference = replacements[netPrefab][target];
					// Iterate through each network in this entry.
					foreach (NetPropReference propRef in reference.references)
					{
						// Check for a network, lane, and prop index match.
						if (propRef.network == netPrefab && propRef.laneIndex == laneIndex && propRef.propIndex == propIndex)
						{
							// Match!  Return the original prefab.
							return replacements[netPrefab][target];
						}
					}
				}
			}

			// If we got here, no entry was found - return null to indicate no active replacement.
			return null;
		}


		/// <summary>
		/// Replaces a prop using a network replacement.
		/// </summary>
		/// <param name="netElement">Network replacement element to apply</param>
		/// <param name="propReference">Individual prop reference to apply to</param>
		internal void ReplaceProp(BOBNetReplacement netElement, NetPropReference propReference)
		{
			// Convert offset to Vector3.
			Vector3 offset = new Vector3
			{
				x = netElement.offsetX,
				y = netElement.offsetY,
				z = netElement.offsetZ
			};

			// Apply replacement.
			if (netElement.replacementInfo is PropInfo propInfo)
			{
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalProp = propInfo;
			}
			else
			{
				propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_finalTree = (TreeInfo)netElement.replacementInfo;
			}

			// Invert x offset if original prop x position is negative.
			if (propReference.position.x < 0)
            {
				offset.x = 0 - offset.x;
            }

			// Angle and offset.
			propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_angle = propReference.angle + netElement.angle;
			propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_position = propReference.position + offset;

			// Probability.
			propReference.network.m_lanes[propReference.laneIndex].m_laneProps.m_props[propReference.propIndex].m_probability = netElement.probability;

			// Add network to dirty list.
			NetData.DirtyList.Add(propReference.network);
		}
	}
}
