using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.IO;

// TODO: comment
public class SavingController : MonoBehaviour
{
    public virtual string saveName
    {
        get { return name; }
    }

    /// <summary>
    /// If true, this blocker is not saved or restored,
    /// and should be static.
    /// </summary>
    public bool excludeFromSave;

    public virtual void SaveTo(BinaryWriter writer)
    {
        if (saveName == name)
        {
            Location location = Location.Of(gameObject);
            location.WriteTo(writer);

            writer.Write(gameObject.activeInHierarchy);

            string parentName = transform.parent != null ? transform.parent.name : "";
            writer.Write(parentName);
        }
    }

    public virtual void RestoreFrom(BinaryReader reader)
    {
        if (saveName == name)
        {
            Location location = Location.ReadFrom(reader);
            gameObject.SetActive(reader.ReadBoolean());

            string parentName = reader.ReadString();

            if (parentName != "")
            {
                GameObject parent = FindGameObjectsByName(parentName).FirstOrDefault();

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

    private static IEnumerable<GameObject> FindGameObjectsByName(string name)
    {
        ILookup<string, GameObject> lookup = Lazy.Init(ref lazyGameObjectsByName, delegate
        {
            return (from t in Resources.FindObjectsOfTypeAll<Transform>()
                    select t.gameObject).ToLookup(go => go.name);
        });

        return lookup[name];
    }

    private static ILookup<string, GameObject> lazyGameObjectsByName;

    public static void Save(IEnumerable<GameObject> objects, BinaryWriter writer)
    {
        foreach (GameObject go in objects)
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

    public static void Restore(IEnumerable<GameObject> objects, BinaryReader reader)
    {
        lazyGameObjectsByName = null;

        Dictionary<string, Queue<byte[]>> byNames =
            (from pair in ReadSections(reader)
             group pair.Value by pair.Key).
             ToDictionary(g => g.Key, g => new Queue<byte[]>(g));

        try
        {
            PlayableEntityController player = null;
            var toDestroy = new List<GameObject>();

            foreach (GameObject go in objects)
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
                                sc.RestoreFrom(new BinaryReader(new MemoryStream(arrays.Dequeue())));
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
                
                if (shouldDestroy)
                    toDestroy.Add(go);
            }

            MapController mapController = MapController.instance;

            mapController.activeMap = mapController.maps[Location.Of(player.gameObject).mapIndex];

            mapController.entities.ActivateMapContainers();
            mapController.entities.ActivateEntities();

            foreach (GameObject go in toDestroy)
                mapController.entities.RemoveEntity(go);
        }
        finally
        {
            lazyGameObjectsByName = null;
        }
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
