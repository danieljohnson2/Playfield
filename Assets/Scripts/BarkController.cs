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
			if (CheckShouldBark () && !isAnyCreatureBarkActive) {
				int barkIndex = Random.Range (0, barkPrefabs.Length);
				activeBark = Instantiate (barkPrefabs [barkIndex]);
				
				Vector3 localPos = activeBark.transform.localPosition;
				Vector3 barkOrientation = activeBark.transform.localScale;
				activeBark.transform.parent = transform;
				activeBark.transform.localPosition = localPos;
				if (barkOrientation.x < 0)
				{
					barkOrientation.x = -barkOrientation.x;
					activeBark.transform.localScale = barkOrientation;
				}
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
	/// If true, a speech balloon is being displued by this or any other controller.
	/// </summary>
	private bool isAnyCreatureBarkActive {
		get { return GetComponents<BarkController> ().Any (bc => bc.isBarking); }
	}

	/// <summary>
	/// This method checks to see if a bark can happen right now; it
	/// applies the 'barkChance' and can randomly return false because
	/// of that. Subclass can add additional conditions by overriding this
	/// method.
	/// </summary>
	protected virtual bool CheckShouldBark ()
	{
		return Random.value < barkChance;
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