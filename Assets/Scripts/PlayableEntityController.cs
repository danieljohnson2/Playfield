﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System;

/// <summary>
/// PlayableEntityController is the controller for creatures
/// controlled by the player. Only one creature should have its
/// 'isPlayerControlled' flag set; any that does will ignore
/// its normal AI and juet follow player instructions.
/// </summary>
public class PlayableEntityController : MovementBlocker
{
    public float speed = 1;

    private float maxSpeed = 20.0f;
    private float turnCounter = 0;

    public PlayableEntityController()
    {
        this.passable = true;
    }

    /// <summary>
    /// isPlayerControlled may be set to make this creature the
    /// player; if set the player will control its behavior, and
    /// its AI will be ignored.
    /// </summary>
    public bool isPlayerControlled;

    /// <summary>
    /// isPlayerCandidate should be a temporary thing; this lets us know
    /// which creatures can be the player.
    /// </summary>
    public bool isPlayerCandidate;

    #region Turns

    /// <summary>
    /// This method decides if the creatures turn has
    /// arrived; we reduce the turnCounter until it goes 0
    /// or negative, and then its this creature's turn. By
    /// having a larger or smaller speed, turns will come up
    /// more or less often.
    /// </summary>
    public virtual bool CheckTurn()
    {
        if (turnCounter <= float.Epsilon)
        {
            turnCounter += maxSpeed;
            return true;
        }
        else
        {
            turnCounter -= speed;
            return false;
        }
    }

    /// <summary>
    /// This is the entry point used to start the
    /// creatures turn. The next creature's turn
    /// begins only when this one ends, but in most
    /// cases aren't really much of a co-routine; this
    /// method calls DoTurn(), then ends the turn
    /// synchronously.
    /// </summary>
    public IEnumerator DoTurnAsync()
    {
        if (isPlayerControlled)
        {
            Location startingLocation = Location.Of(gameObject);

            UpdateStatusText();

            MoveEffect effect;
            do
            {
                // we'll yield until the user enters a move.

                while (commandCommanded == Command.None)
                    yield return null;

                // we'll continue processing until we get a move that has;
                // an effect; saving or moving into walls does not count.

                effect = PerformCommandedCommand();

                // The spinny backgrounds spins faster the more
                // you are damaged.

                spin = GetSpinnyness();
                if (lastDeltaX == 1)
                    spin = -spin;
            } while (effect == MoveEffect.None);

            SyncCamera(smooth: startingLocation.mapIndex == Location.Of(gameObject).mapIndex);
        }
        else
        {
            DoTurn();
        }
    }

    /// <summary>
    /// This is a sychrnonous entry point; you override this
    /// and do whatever the creature should do during its turn.
    /// </summary>
    protected virtual void DoTurn()
    {
    }

    #endregion

    #region Movements

    /// <summary>
    /// This method moves the creature by the delta indicated
    /// within the current map. This executes Block() methods
    /// and may fail; if the creature could not move this method
    /// returns false. If it did move it returns true.
    /// </summary>
    protected MoveEffect Move(int dx, int dy)
    {
        Location loc = Location.Of(gameObject).WithOffset(dx, dy);
        return MoveTo(loc);
    }

    /// <summary>
    /// This method moves the creature to a specific location,
    /// which could be on a different map. Like Move(), this runs
    /// Block() methods and returns false if the movement is blocked,
    /// true if it succeeds. 
    /// </summary>
    protected MoveEffect MoveTo(Location destination)
    {
        if (mapController.terrain.GetTerrain(destination) == null)
        {
            return MoveEffect.None;
        }

        foreach (var blocker in mapController.ComponentsInCell<MovementBlocker>(destination).Reverse())
        {
            MoveEffect effect = blocker.Block(gameObject, destination);
            if (effect != MoveEffect.Moved)
                return effect;
        }

        Vector3 destPos = destination.ToLocalPosition();
        FlipToFace(destPos);
        transform.localPosition = destPos;
        return MoveEffect.Moved;
    }

    /// <summary>
    /// FlipToFace() updates the scale of this creature so
    /// that he will face towards the position 'faceTo'
    /// in a horizontal sense (only the x positions are
    /// considered, not y or z). We do this just before
    /// moving so that the creature faces the directory he
    /// moves.
    /// </summary>
    private void FlipToFace(Vector3 faceTo)
    {
        Vector3 charFlip = transform.localScale;

        int currentFacing = Math.Sign(charFlip.x);
        int desiredFacing;

        if (transform.localPosition.x < faceTo.x)
            desiredFacing = 1;
        else if (transform.localPosition.x > faceTo.x)
            desiredFacing = -1;
        else
            desiredFacing = currentFacing;

        if (desiredFacing != currentFacing)
        {
            charFlip.x = -charFlip.x;
            transform.localScale = charFlip;

            // Bark 'bubbles' must be counterflipped so that they don't
            // appear reversed.

            foreach (var bc in GetComponents<BarkController>())
                bc.NormalizeBarkFlip();
        }
    }

    #endregion

    #region Saved Games

    public override void SaveTo(BinaryWriter writer)
    {
        base.SaveTo(writer);

        writer.Write(isPlayerControlled);
        writer.Write(turnCounter);
    }

    public override void RestoreFrom(BinaryReader reader, Restoration restoration)
    {
        base.RestoreFrom(reader, restoration);

        isPlayerControlled = reader.ReadBoolean();
        turnCounter = reader.ReadSingle();
    }

    #endregion

    #region Player Commands

    private Command commandStarted;
    private Command commandCommanded;
	private Command lastCommand;

    public static bool isCommandPending;

    /// <summary>
    /// UpdateCommandSelection() examines the buttons pressed
    /// and updates 'commandStarted' and 'commandCommanded',
    /// and when the later is set DoTurn() will be able to
    /// actually continue, and PerformCommandedCommand() wil
    /// execute this command.
    /// </summary>
    private void UpdateCommandSelection()
    {
        Command cmd = GetCommandOfKeyPressed();

        if (commandStarted != Command.None && cmd == Command.None)
        {
            commandCommanded = commandStarted;
			lastCommand = commandStarted;
            commandStarted = Command.None;
            isCommandPending = true;
        }
        else if (cmd != Command.None)
        {
            commandStarted = cmd;
            commandCommanded = Command.None;
            isCommandPending = true;
        }
    }

    /// <summary>
    /// PerformCommandedCommand() performs whatever command is indicated by
    /// 'commandCommanded', and it also clears the command fields so the
    /// command is not repeated.
    /// </summary>
    private MoveEffect PerformCommandedCommand()
    {
        try
        {
            switch (commandCommanded)
            {
                case Command.Left: lastDeltaX = -1; return Move(-1, 0);
                case Command.Right: lastDeltaX = 1; return Move(1, 0);
                case Command.Up: return Move(0, -1);
                case Command.Down: return Move(0, 1);

                case Command.Save:
                    Save();
                    mapController.transcript.AddLine("Game saved.");
                    return MoveEffect.None;

                case Command.Restore:
                    if (File.Exists(GetSaveGamePath()))
                    {
                        mapController.transcript.AddLine("Restoring game...");
                        Restore();
                        // we need to end the turn to allow the game to reload the level!
                        return MoveEffect.Action;
                    }
                    else
                    {
                        mapController.transcript.AddLine("No saved game to restore!");
                        return MoveEffect.None;
                    }

                default:
                    return MoveEffect.None;
            }
        }
        finally
        {
            commandCommanded = Command.None;
            commandStarted = Command.None;
            isCommandPending = false;
        }
    }

    /// <summary>
    /// GetCommandOfKeyPressed() returns the command being
    /// indicated right now; this tests what buttons are
    /// pressed. We don't actually execute a command until
    /// it is both pressed, and then later released.
    /// </summary>
    private Command GetCommandOfKeyPressed()
    {
        // 'Raw' means unfiltered, unsmoothed, so we will
        // react immediately to the axis.

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (horizontal < 0.0f)
            return Command.Left;
        else if (horizontal > 0.0f)
            return Command.Right;
        else if (vertical < 0.0f)
            return Command.Down;
        else if (vertical > 0.0f)
            return Command.Up;
        else if (Input.GetButton("Save"))
            return Command.Save;
        else if (Input.GetButton("Restore"))
            return Command.Restore;
        else
            return Command.None;
    }

    private enum Command
    {
        None,
        Up,
        Down,
        Left,
        Right,
        Save,
        Restore
    }

    #endregion

    #region User Interface

    private float step = 1;
    private int lastDeltaX = 1;
    private float spin = 0;

    void FixedUpdate()
    {
        if (isPlayerControlled)
        {
            // As time goes on, we slow down the
            // spin.
            spin *= 0.985f;

            // Clamp the spin to be no less than 1 (or -1 if negative).

            if ((spin < 1) && (spin > -1))
                spin = -lastDeltaX;

            // step is the degree of rotation of the skybox,
            // where the spin is the speed at which this changes.

            step += spin;
            step %= 36000;
            Material skybox = RenderSettings.skybox;
            skybox.SetFloat("_Rotation", step / 100);
        }
    }

    void Update()
    {
        if (isPlayerControlled)
        {
            UpdateCommandSelection();
            SyncCamera();

            if (transcript != null)
            {
                transcript.isInstructionTextVisible =
                    commandStarted == Command.None &&
                    commandCommanded == Command.None &&
                    Input.anyKey;
            }
        }
    }

    /// <summary>
    /// SyncCamera() moves the camera to point right
    /// at the player.
    /// </summary>
    private void SyncCamera(bool smooth = true)
    {
        Vector3 newCameraPos = transform.position;
        newCameraPos.z = -10f;
		if (lastCommand == Command.Up) newCameraPos.y += 1.25f;
		if (lastCommand == Command.Down) newCameraPos.y -= 1.25f;
		if (lastCommand == Command.Right) newCameraPos.x += 1.25f;
		if (lastCommand == Command.Left) newCameraPos.x -= 1.25f;
		//offset the target camera location so that our visibility doesn't severely lag behind character position:
		//if we're moving in a direction and slowed by lerp, our attention is on what we're moving towards

		Vector3 cameraPos = Camera.main.transform.position;

        if (smooth)
            newCameraPos = Vector3.Lerp(cameraPos, newCameraPos, 2.3f * Time.deltaTime);

        Camera.main.transform.position = newCameraPos;
    }

    /// <summary>
    /// UpdateStatusText() updates the status text that
    /// shows the user how many HP he has.
    /// </summary>
    protected virtual void UpdateStatusText()
    {
    }

    /// <summary>
    /// GetSpinnyness() indicates how fast the background haze effect
    /// should spin; we override this to return a spinnyness that
    /// increases as the player is hurt.
    /// </summary>
    protected virtual float GetSpinnyness()
    {
        return 0;
    }

    #endregion

    #region Saved Games

    /// <summary>
    /// Save() saves the game.
    /// </summary>
    private void Save()
    {
        string path = GetSaveGamePath();

        using (Stream stream = File.Open(path, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            SavingController.Save(mapController, writer);
        }
    }

    /// <summary>
    /// Restore() restores the saved game. This does not happen instantly;
    /// the map first reloads, and then we restore the saved game. All that
    /// happens after this method returns.
    /// </summary>
    public static void Restore()
    {
        string path = GetSaveGamePath();

        MapController.ReloadWithInitialization(mc => RestoreAfterLoad(mapController, path));
    }

    /// <summary>
    /// CanRestore tests to see if the saved game file is present.
    /// </summary>
    public static bool CanRestore
    {
        get { return File.Exists(GetSaveGamePath()); }
    }

    /// <summary>
    /// GetSaveGamePath() reutrns the path to the saved game file.
    /// </summary>
    public static string GetSaveGamePath()
    {
        return Path.Combine(Application.persistentDataPath, @"Save.dat");
    }

    /// <summary>
    /// RestoreAfterLoad() executes after the game reloads to its initial
    /// state; this method then restores the saved game.
    /// </summary>
    private static void RestoreAfterLoad(MapController mapController, string path)
    {
        using (Stream stream = File.OpenRead(path))
        using (var reader = new BinaryReader(stream))
        {
            Restoration.Read(reader).Restore(mapController);
        }

        mapController.transcript.AddLine("Game restored.");
    }

    #endregion
}
