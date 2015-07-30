using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This controller displays a sprite (a speach balloon) over the
/// creature when it notices that the current heatmap has changed.
/// </summary>
public class HeatmapSwitchBarkController : BarkController
{
	public string newHeatmapName;
	public string oldHeatmapName;
	private string lastKnownHeatmapName;

	/// <summary>
	/// This method checks to see if a bark can happen right now; it
	/// applies the 'barkChance' and can randomly return false because
	/// of that. Otherwise, it checks that the correct heatmap is active and
	/// there is not any bark playing now (even from another controller).
	/// </summary>
	protected override bool CheckShouldBark ()
	{
		if (!base.CheckShouldBark ()) {
			return false;
		}

		var ai = GetComponent<HeatmapAIController> ();

		if (ai != null) {
			Heatmap heatmap = ai.activeHeatmap;

			string activeName = heatmap != null ? ai.activeHeatmap.name : "";

			if ((lastKnownHeatmapName ?? "") != activeName) {
				bool canBark = true;

				if (!string.IsNullOrEmpty (oldHeatmapName) && oldHeatmapName != lastKnownHeatmapName)
					canBark = false;

				if (!string.IsNullOrEmpty (newHeatmapName) && newHeatmapName != activeName)
					canBark = false;

				lastKnownHeatmapName = activeName;
				return canBark;
			}
		}

		return false;
	}
}