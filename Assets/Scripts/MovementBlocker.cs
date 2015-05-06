using UnityEngine;
using System.Collections;

public class MovementBlocker : MonoBehaviour
{
	protected MapController mapController { get; private set; }

	public bool passable { get; set; }

	public bool pathable { get; set; }

	void Start ()
	{
		mapController = GameObject.FindGameObjectWithTag ("GameController").GetComponent<MapController> ();
	}

	public virtual bool Block (GameObject mover)
	{
		return passable;
	}
}
