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

    public void Update()
    {
        float time = Time.time;

        if (barkTime == 0.0f)
        {
            barkTime = PickNextTime();
        }
        else if (activeBark != null)
        {
            if (time >= barkTime + barkDuration)
            {
                Destroy(activeBark);
                activeBark = null;
                barkTime = PickNextTime();
            }
        }
        else if (!isBarking && time >= barkTime && barkPrefabs != null && barkPrefabs.Length > 0)
        {
            if (CheckShouldBark() && !isAnyCreatureBarkActive)
            {
                int barkIndex = Random.Range(0, barkPrefabs.Length);
                activeBark = Instantiate(barkPrefabs[barkIndex]);

                Vector3 localPos = activeBark.transform.localPosition;
                activeBark.transform.parent = transform;
                activeBark.transform.localPosition = localPos;
                NormalizeBarkFlip();
            }
            else
            {
                barkTime = PickNextTime();
            }
        }
    }

    /// <summary>
    /// NormalizeBarkFlip() update sthe active bark (if any) so that it is
    /// flipped horizontally if this game object is; this has the effect
    /// of 'undoing' the flip, so that the bark bubble is still legible.
    /// </summary>
    public void NormalizeBarkFlip()
    {
        if (activeBark != null)
        {
            Vector3 barkOrientation = activeBark.transform.localScale;

            bool creatureflipped = transform.localScale.x < 0.0f;
            bool barkFlipped = barkOrientation.x < 0.0f;

            if (creatureflipped != barkFlipped)
            {
                barkOrientation.x = -barkOrientation.x;
                activeBark.transform.localScale = barkOrientation;
            }
        }
    }

    /// <summary>
    /// If true, a speech balloon is being displayed because of this controller.
    /// </summary>
    public bool isBarking
    {
        get { return activeBark != null; }
    }

    /// <summary>
    /// If true, a speech balloon is being displued by this or any other controller.
    /// </summary>
    private bool isAnyCreatureBarkActive
    {
        get { return GetComponents<BarkController>().Any(bc => bc.isBarking); }
    }

    /// <summary>
    /// This method checks to see if a bark can happen right now; it
    /// applies the 'barkChance' and can randomly return false because
    /// of that. Subclass can add additional conditions by overriding this
    /// method.
    /// </summary>
    protected virtual bool CheckShouldBark()
    {
        return Random.value < barkChance;
    }

    /// <summary>
    /// This method selects a random time when the next
    /// bark can happen.
    /// </summary>
    private float PickNextTime()
    {
        float diff = maximumBarkInterval - minimumBarkInterval;
        float deltaTime = Random.value * diff + minimumBarkInterval;
        return Time.time + deltaTime;
    }
}