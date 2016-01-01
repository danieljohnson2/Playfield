using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System;

/// <summary>
/// SavingController is a controller base class for practically
/// everything; this is used for objects that can be saved. We
/// save in a very primitive way, by writing a 'save name' and
/// some binary data to a writer. At restore time we match up
/// the saved-object records with saved objects by name, and
/// restore the binary data other than the name.
/// 
/// Terrain is never saved, and objects that have the excludeFromSave
/// flag set are also skipped.
/// </summary>
public class SavingController : MonoBehaviour
{
    /// <summary>
    /// saveName is the name used to identify this controller
    /// in a save file. If you want to have two controllers
    /// in the same object be saved, you'll need to override
    /// this to give them distinct names.
    /// </summary>
    public virtual string saveName
    {
        get { return name; }
    }

    /// <summary>
    /// If true, this blocker is not saved or restored,
    /// and should be static.
    /// </summary>
    public bool excludeFromSave;

    /// <summary>
    /// SaveTo() writes this controllers state to the
    /// writer.
    /// </summary>
    public virtual void SaveTo(BinaryWriter writer)
    {
        // We save and restore the location and parent data only
        // if this controller has the default name, to avoid doing
        // it more than once per GameObject.

        if (saveName == name)
        {
            Location location = Location.Of(gameObject);
            location.WriteTo(writer);

            writer.Write(gameObject.activeInHierarchy);

            string parentName = transform.parent != null ? transform.parent.name : "";
            writer.Write(parentName);
        }
    }

    /// <summary>
    /// RestoreFrom() restores the state of this controller by
    /// reading the data SaveTo() writes.
    /// </summary>
    public virtual void RestoreFrom(BinaryReader reader, Restoration restoration)
    {
        if (saveName == name)
        {
            Location location = Location.ReadFrom(reader);
            gameObject.SetActive(reader.ReadBoolean());

            string parentName = reader.ReadString();

            if (parentName != "")
            {
                GameObject parent = restoration.gameObjectsByName[parentName].FirstOrDefault();

                if (parent != null)
                    transform.parent = parent.transform;
                else
                {
                    Debug.LogError(string.Format("Parent {0} not found for {1}.", parentName, name));
                }
            }

            gameObject.transform.localPosition = location.ToPosition();
        }
    }

    #region Saving and Restoring

    /// <summary>
    /// saveFileSignature contains the the bytes we put at the start of the
    /// save file. This makes it possible to identify our save game format,
    /// which we use to validate that the file is a real save file.
    /// </summary>
    private static readonly byte[] saveFileSignature = Encoding.ASCII.GetBytes("Playfield");

    /// <summary>
    /// This method saves a saved game; this reads the entities from the
    /// map controller and writes each saveable component of each one out
    /// to the write.
    /// </summary>
    public static void Save(MapController mapController, BinaryWriter writer)
    {
        // write a signature so we can recognize the data.
        writer.Write(saveFileSignature);
        // write a version number so we can do compatibility hackery if we need to.
        writer.Write((byte)1);

        foreach (GameObject go in mapController.entities.Entities())
        {
            foreach (var sc in go.GetComponents<SavingController>())
            {
                if (!sc.excludeFromSave)
                {
                    writer.Write(sc.saveName);

                    using (var ms = new MemoryStream())
                    {
                        using (var w = new BinaryWriter(ms))
                        {
                            sc.SaveTo(w);
                        }

                        byte[] array = ms.ToArray();
                        writer.Write(array.Length);
                        writer.Write(array);
                    }
                }
            }
        }

        writer.Write("");
    }

    /// <summary>
    /// Restoration is an object used to restore games; it holds
    /// a copy of the saved game data, and alookup to find game
    /// objects by name. This is passed to the RestoreFrom() methods
    /// to allow them to find their parents.
    /// </summary>
    public sealed class Restoration
    {
        private readonly Dictionary<string, Queue<byte[]>> byNames;
        private bool restored;

        private Restoration(Dictionary<string, Queue<byte[]>> byNames)
        {
            this.byNames = byNames;

            this.gameObjectsByName =
                (from t in Resources.FindObjectsOfTypeAll<Transform>()
                 select t.gameObject).ToLookup(go => go.name);
        }

        /// <summary>
        /// gameObjectsByName is a lookup of all the game objects, keyed by
        /// their names.
        /// </summary>
        public ILookup<string, GameObject> gameObjectsByName { get; private set; }

        /// <summary>
        /// Read() reads a saved came from a reader, and generates
        /// a Restoration object containing an in-memory copy of
        /// that data.
        /// </summary>
        public static Restoration Read(BinaryReader reader)
        {
            byte[] signature = reader.ReadBytes(saveFileSignature.Length);

            if (!signature.SequenceEqual(saveFileSignature))
                throw new FormatException("The file does not contain a Playfield save.");

            byte version = reader.ReadByte();

            if (version != 1)
                throw new FormatException("The save file is of an unknown version.");

            Dictionary<string, Queue<byte[]>> byNames =
                (from pair in ReadSections(reader)
                 group pair.Value by pair.Key).
                 ToDictionary(g => g.Key, g => new Queue<byte[]>(g));

            return new Restoration(byNames);
        }

        /// <summary>
        /// Restore() restores a saved game, updating the entities and other state
        /// of the map controller given. A Restoration() can be restored
        /// only once; after that it is useless.
        /// </summary>
        public void Restore(MapController mapController)
        {
            if (restored)
                throw new System.InvalidOperationException("A Restoration can be restored only once.");

            restored = true;

            PlayableEntityController player = null;
            var toDestroy = new List<GameObject>();

            // First we update all entities; we keep track of entities
            // that should not be present, and the player. Nothing can
            // be destroyed yet, lest a RestoreFrom() method fail because
            // it wants an object we've destroyed.

            foreach (GameObject go in mapController.entities.Entities())
            {
                bool shouldDestroy = true;

                foreach (var sc in go.GetComponents<SavingController>())
                {
                    if (!sc.excludeFromSave)
                    {
                        Queue<byte[]> arrays;
                        if (byNames.TryGetValue(sc.saveName, out arrays))
                        {
                            if (arrays.Count > 0)
                            {
                                shouldDestroy = false;
                                sc.RestoreFrom(
                                    new BinaryReader(new MemoryStream(arrays.Dequeue())),
                                    this);
                            }
                        }
                    }
                    else
                        shouldDestroy = false;

                    // must check this after RestoreFrom()!
                    var pec = sc as PlayableEntityController;
                    if (pec != null && pec.isPlayerControlled)
                        player = pec;
                }

                // We will destroy each object that is 
                if (shouldDestroy)
                    toDestroy.Add(go);
            }

            mapController.activeMap = mapController.maps[Location.Of(player.gameObject).mapIndex];

            mapController.entities.ActivateMapContainers();
            mapController.entities.ActivateEntities();

            foreach (GameObject go in toDestroy)
                mapController.entities.RemoveEntity(go);

            mapController.entities.ProcessRemovals();
        }

        private static IEnumerable<KeyValuePair<string, byte[]>> ReadSections(BinaryReader reader)
        {
            for (;;)
            {
                string saveName = reader.ReadString();

                if (saveName == "")
                    break;

                int len = reader.ReadInt32();
                byte[] array = reader.ReadBytes(len);

                yield return new KeyValuePair<string, byte[]>(saveName, array);
            }
        }
    }

    #endregion
}
