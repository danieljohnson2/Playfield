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
    private int moveDeltaX, moveDeltaY;
    private bool moveMade;

    public override IEnumerator DoTurnAsync()
    {
        UpdateStatusText();

        // we'll yield until the user enters a move...

        while (!moveMade)
        {
            yield return null;
        }

        // and only then execute it.
        try
        {
            DoTurn();
            SyncCamera();
        }
        finally
        {
            moveDeltaX = 0;
            moveDeltaY = 0;
            moveMade = false;
        }
    }

    protected override void DoTurn()
    {
        if (moveDeltaX != 0 || moveDeltaY != 0)
            Move(moveDeltaX, moveDeltaY);
    }

    protected override void Die()
    {
        base.Die();
        UpdateStatusText();
    }

    private void SyncCamera()
    {
        Vector3 playerPos = transform.position;
        playerPos.z = -10f;
        Camera.main.transform.position = playerPos;
    }

    private void UpdateStatusText()
    {
        if (playerStatusText != null)
        {
            playerStatusText.text = string.Format("HP: {0}", hitPoints);
        }
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

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            moveDeltaX = -1;
            moveDeltaY = 0;
            moveMade = true;
        }
        else if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            moveDeltaX = 1;
            moveDeltaY = 0;
            moveMade = true;
        }
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            moveDeltaX = 0;
            moveDeltaY = -1;
            moveMade = true;
        }
        else if (Input.GetKeyDown(KeyCode.DownArrow))
        {
            moveDeltaX = 0;
            moveDeltaY = 1;
            moveMade = true;
        }
        else if (Input.GetKeyDown(KeyCode.F5))
        {
            Save();
        }
        else if (Input.GetKeyDown(KeyCode.F9))
        {
            Restore();
            moveMade = true;
        }

        SyncCamera();
    }

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

    private void Restore()
    {
        string path = Path.Combine(Application.persistentDataPath, @"Save.dat");

        MapController.ReloadWithInitialization(mc => RestoreAfterLoad(mapController, path));
    }

    private static void RestoreAfterLoad(MapController mapController, string path)
    {
        using (Stream stream = File.OpenRead(path))
        using (var reader = new BinaryReader(stream))
        {
            SavingController.Restore(mapController.entities.Entities(), reader);
        }

        mapController.transcript.AddLine("Game restored.");
    }
}