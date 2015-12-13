using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// This is the controller for the player, which reads user
/// input and acts upon it. This will also move the main camera
/// to track the player.
/// </summary>
public class PlayerController : CreatureController
{
    public UnityEngine.UI.Text playerStatusText;
    public Skybox movingSkybox;

    private float step = 1;
    private int lastDeltaX = 1;
    private float spin = 0;

    public override IEnumerator DoTurnAsync()
    {
        UpdateStatusText();

        bool moveComplete = false;
        do
        {
            // we'll yield until the user enters a move.

            while (commandCommanded == Command.None)
                yield return null;

            // IF we are saving the game, we'll continue waiting
            // for a real move. For anything else, including 'Restore',
            // we must return to allow other creatures to move.

            if (commandCommanded != Command.Save)
                moveComplete = true;

            DoTurn();
            SyncCamera();
        } while (!moveComplete);
    }

    protected override void DoTurn()
    {
        PerformCommandedCommand();

        // The spinny backgrounds spins faster the more
        // you are damaged.

        spin = (102 - Mathf.Pow(hitPoints, 2));
        if (lastDeltaX == 1)
            spin = -spin;
    }

    protected override void Die()
    {
        base.Die();
        UpdateStatusText();

        mapController.GameOver();
    }

    void Start()
    {
        GameObject psTextObj = GameObject.Find("Player Status Text");

        if (psTextObj != null)
        {
            playerStatusText = psTextObj.GetComponent<UnityEngine.UI.Text>();
            UpdateStatusText();
        }
    }

    void FixedUpdate()
    {
        // As time goes on, we slow down the
        // spin.
        spin *= 0.98f;

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

    void Update()
    {
        UpdateCommandSelection();
        SyncCamera();
    }

    #region Commands

    private Command commandStarted;
    private Command commandCommanded;

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
            commandStarted = Command.None;
        }
        else if (cmd != Command.None)
        {
            commandStarted = cmd;
            commandCommanded = Command.None;
        }
    }

    /// <summary>
    /// PerformCommandedCommand() performs whatever command is indicated by
    /// 'commandCommanded', and it also clears the command fields so the
    /// command is not repeated.
    /// </summary>
    private void PerformCommandedCommand()
    {
        try
        {
            switch (commandCommanded)
            {
                case Command.Left: Move(-1, 0); lastDeltaX = -1; break;
                case Command.Right: Move(1, 0); lastDeltaX = 1; break;
                case Command.Up: Move(0, -1); break;
                case Command.Down: Move(0, 1); break;

                case Command.Save: Save(); break;
                case Command.Restore: Restore(); break;
            }
        }
        finally
        {
            commandCommanded = Command.None;
            commandStarted = Command.None;
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
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

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
        else if (Input.GetButton("Pass"))
            return Command.Pass;
        else
            return Command.None;
    }

    private enum Command
    {
        None,
        Pass,
        Up,
        Down,
        Left,
        Right,
        Save,
        Restore
    }

    #endregion

    #region User Interface

    /// <summary>
    /// SyncCamera() moves the camera to point right
    /// at the player.
    /// </summary>
    private void SyncCamera()
    {
        Vector3 playerPos = transform.position;
        playerPos.z = -10f;
        Camera.main.transform.position = playerPos;
    }

    /// <summary>
    /// UpdateStatusText() updates the status text that
    /// shows the user how many HP he has.
    /// </summary>
    private void UpdateStatusText()
    {
        if (playerStatusText != null)
        {
            playerStatusText.text = string.Format("HP: {0}", hitPoints);
        }
    }

    #endregion

    #region Saved Games

    /// <summary>
    /// Save() saves the game.
    /// </summary>
    private void Save()
    {
        string path = Path.Combine(Application.persistentDataPath, @"Save.dat");

        using (Stream stream = File.Open(path, FileMode.Create))
        using (var writer = new BinaryWriter(stream))
        {
            SavingController.Save(mapController.entities.Entities(), writer);
        }

        mapController.transcript.AddLine("Game saved.");
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
    private static string GetSaveGamePath()
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
            SavingController.Restore(mapController.entities.Entities(), reader);
        }

        mapController.transcript.AddLine("Game restored.");
    }

    #endregion
}