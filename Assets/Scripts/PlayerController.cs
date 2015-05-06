using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class PlayerController : CreatureController
{
	private int moveDeltaX, moveDeltaY;

	public override IEnumerator DoTurnAsync ()
	{
		while (moveDeltaX==0 && moveDeltaY==0) {
			yield return null;
		}

		DoTurn ();
		moveDeltaX = 0;
		moveDeltaY = 0;

		SyncCamera ();
	}

	public override void DoTurn ()
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
