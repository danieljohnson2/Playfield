using UnityEngine;
using System.Collections;

public class SlashController : MonoBehaviour
{
	void Start ()
	{
		var anim = GetComponent<Animator> ();
		float len = anim.GetCurrentAnimatorClipInfo(0)[0].clip.length;
		Invoke ("Unslash", len);
	}

	public void Unslash ()
	{
		Destroy (gameObject);
	}
}
