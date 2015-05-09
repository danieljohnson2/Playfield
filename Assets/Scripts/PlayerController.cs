using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// This is the controller for the player, which reads user
/// input and acts upon it. This will also move the main camera
/// to track the player.
/// </summary>
public class PlayerController : CreatureController
{
	private int moveDeltaX, moveDeltaY;

	public override IEnumerator DoTurnAsync ()
	{
		// we'll yield until the user enters a move...

		while (moveDeltaX == 0 && moveDeltaY == 0) {
			yield return null;
		}

		// and only the execute it.
		try {
			DoTurn ();
			SyncCamera ();
		} finally {
			moveDeltaX = 0;
			moveDeltaY = 0;
		}
	}

	protected override void DoTurn ()
	{
		Move (moveDeltaX, moveDeltaY);
	}

	private void SyncCamera ()
	{
		Vector3 playerPos = transform.position;
		playerPos.z = -10f;
		Camera.main.transform.position = playerPos;
	}

	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.LeftArrow)) {
			moveDeltaX = -1;
			moveDeltaY = 0;
		} else if (Input.GetKeyDown (KeyCode.RightArrow)) {
			moveDeltaX = 1;
			moveDeltaY = 0;
		} else if (Input.GetKeyDown (KeyCode.UpArrow)) {
			moveDeltaX = 0;
			moveDeltaY = -1;
		} else if (Input.GetKeyDown (KeyCode.DownArrow)) {
			moveDeltaX = 0;
			moveDeltaY = 1;
		}

		SyncCamera ();
	}
}
