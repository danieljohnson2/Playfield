using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This controller displays a sprite (a speach balloon) over the
/// creature at intervals. The intervals are randomized, and the bark is
/// played only if a designated heatmap is active.
/// </summary>
public class HeatmapBarkController : BarkController
{
	public string heatmapName;
	public string heatSource;
	public int minimumHeatmapStrength;

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

		return 
			ai != null && 
			ai.CheckActiveHeatmap (heatmapName, minimumHeatmapStrength, HeatSourceIdentifier.Parse (heatSource ?? ""));
	}
}