﻿using UnityEngine;
using System.Collections;

/// <summary>
/// This is a component that blocks other object's movement,
/// so they either can't move into the same square as this one,
/// or something happens if they do.
/// </summary>
public class MovementBlocker : MonoBehaviour
{
	protected MapController mapController { get; private set; }

	/// <summary>
	/// If true, creatures can try to move into this square
	/// (though Block() might still reject them).
	/// </summary>
	public bool passable { get; set; }

	/// <summary>
	/// If true, the AI pathfinding paths through this
	/// square; if false the square blocks the AI's awareness
	/// of the square, though they can still move randonly into
	/// it anyway.
	/// </summary>
	public bool pathable { get; set; }

	void Start ()
	{
		mapController = GameObject.FindGameObjectWithTag ("GameController").GetComponent<MapController> ();
	}

	/// <summary>
	/// This method is called when some object tries to
	/// move into this square. It can return true to allow
	/// the move or false to fail it, and can take other
	/// actions triggered by the movement as well.
	/// </summary>
	public virtual bool Block (GameObject mover)
	{
		return passable;
	}
}
