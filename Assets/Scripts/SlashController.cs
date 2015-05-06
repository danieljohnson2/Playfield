using UnityEngine;
using System.Collections;

public class SlashController : MonoBehaviour
{
	void Start ()
	{
		Invoke ("Unslash", 0.25f);
	}

	public void Unslash ()
	{
		Destroy (gameObject);
	}
}
