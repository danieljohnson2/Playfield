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
	private int lastDeltaX;
	private float spin = 0;
    private int moveDeltaX, moveDeltaY;
    private bool moveMade, moveReady;

    public override IEnumerator DoTurnAsync()
    {
        UpdateStatusText();

        // we'll yield until the user enters a move...

        while (!moveMade || !moveReady)
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
            moveReady = false;
        }
    }

    protected override void DoTurn()
    {
        if (moveDeltaX != 0 || moveDeltaY != 0)
            Move(moveDeltaX, moveDeltaY);
		spin = (102 - Mathf.Pow (hitPoints, 2));
		if (lastDeltaX == 1)
			spin = -spin;
	}

    protected override void Die()
    {
        base.Die();
        UpdateStatusText();

        mapController.GameOver();
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

	void FixedUpdate()
	{
		spin *= 0.98f;
		if ((spin < 1)&&(spin > -1))
		spin = -lastDeltaX;
		step += spin;
		step %= 36000;
		Material skybox = RenderSettings.skybox;
		skybox.SetFloat ("_Rotation", step / 100);
	}


    void Update()
    {
        if (Input.GetKey(KeyCode.LeftArrow))
        {
            moveDeltaX = -1;
			lastDeltaX = -1;
            moveDeltaY = 0;
            moveMade = true;
        }
        else if (Input.GetKey(KeyCode.RightArrow))
        {
            moveDeltaX = 1;
			lastDeltaX = 1;
            moveDeltaY = 0;
            moveMade = true;
        }
        else if (Input.GetKey(KeyCode.UpArrow))
        {
            moveDeltaX = 0;
            moveDeltaY = -1;
            moveMade = true;
        }
        else if (Input.GetKey(KeyCode.DownArrow))
        {
            moveDeltaX = 0;
            moveDeltaY = 1;
            moveMade = true;
        }
        else if (Input.GetKey(KeyCode.F5))
        {
            Save();
        }
        else if (Input.GetKey(KeyCode.F9))
        {
            Restore();
            moveMade = true;
        }
        else if (moveMade)
        {
            moveReady = true;
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

    public static bool CanRestore
    {
        get { return File.Exists(GetSaveGamePath()); }
    }

    public static void Restore()
    {
        string path = GetSaveGamePath();

        MapController.ReloadWithInitialization(mc => RestoreAfterLoad(mapController, path));
    }

    private static string GetSaveGamePath()
    {
        return Path.Combine(Application.persistentDataPath, @"Save.dat");
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