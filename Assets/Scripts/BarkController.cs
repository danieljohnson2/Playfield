using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This controller displays a sprite (a speach balloon) over the
/// creature at intervals. The intervals are randomized, and the bark is
/// played only if a designated heatmap is active.
/// </summary>
public class BarkController : MonoBehaviour
{
	public GameObject[] barkPrefabs = new GameObject[0];
	public string heatmapName;
	public float barkDuration = 3.0f;
	public float barkChance = 0.5f;
	public float minimumBarkInterval = 5.0f;
	public float maximumBarkInterval = 10.0f;
	private float barkTime;
	private GameObject activeBark;

	public void Update ()
	{
		float time = Time.time;

		if (barkTime == 0.0f) {
			barkTime = PickNextTime ();
		} else if (activeBark != null) {
			if (time >= barkTime + barkDuration) {
				Destroy (activeBark);
				activeBark = null;
				barkTime = PickNextTime ();
			}
		} else if (!isBarking && time >= barkTime && barkPrefabs != null && barkPrefabs.Length > 0) {
			if (CheckShouldBark ()) {
				int barkIndex = Random.Range (0, barkPrefabs.Length);
				activeBark = Instantiate (barkPrefabs [barkIndex]);

				Vector3 localPos = activeBark.transform.localPosition;
				activeBark.transform.parent = transform;
				activeBark.transform.localPosition = localPos;
			} else {
				barkTime = PickNextTime ();
			}
		}
	}

	/// <summary>
	/// If true, a speech balloon is being displayed because of this controller.
	/// </summary>
	public bool isBarking {
		get { return activeBark != null; }
	}

	/// <summary>
	/// This method checks to see if a bark can happen right now; it
	/// applies the 'barkChance' and can randomly return false because
	/// of that. Otherwise, it checks that the correct heatmap is active and
	/// there is not any bark playing now (even from another controller).
	/// </summary>
	private bool CheckShouldBark ()
	{
		if (Random.value >= barkChance) {
			return false;
		}

		var ai = GetComponent<HeapmapAIController> ();

		if (ai != null) {
			string activeName = ai.activeHeatmap != null ? ai.activeHeatmap.name : "";

			if ((heatmapName ?? "") != activeName) {
				return false;
			}
		}

		return !GetComponents<BarkController> ().Any (bc => bc.isBarking);
	}

	/// <summary>
	/// This method selects a random time when the next
	/// bark can happen.
	/// </summary>
	private float PickNextTime ()
	{
		float diff = maximumBarkInterval - minimumBarkInterval;
		float deltaTime = Random.value * diff + minimumBarkInterval;
		return Time.time + deltaTime;
	}
}
