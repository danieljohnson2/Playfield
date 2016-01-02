using UnityEngine;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// This class holds the map data used to generate
/// game objects. This contains prefabs, not the live
/// game obejcts, and the same prefab can appear many times
/// in the map; don't try to alter these objects.
/// 
/// The map class does not use Location structures because
/// it can only deal with x,y co-ordiantes, but the Location
/// structure should specify which map you are on.
/// </summary>
public sealed class Map
{
    private readonly MapLegend mapLegend;
    private readonly string[] mapText;
    private ILookup<string, Location> lazyDoorLocations;
    public readonly int mapIndex;
    public readonly string name;
    public readonly int width;
    public readonly int height;
    public readonly float cameraSize;

    private Map(int mapIndex, string name, float cameraSize, string[] mapText, MapLegend mapLegend)
    {
        this.mapIndex = mapIndex;
        this.name = name;
        this.mapText = mapText;
        this.mapLegend = mapLegend;
        this.width = mapText.Max(l => l.Length);
        this.height = mapText.Length;
        this.cameraSize = cameraSize;
    }

    /// <summary>
    /// This indexer retrives the prefabs for a given cell;
    /// if x or y is out of range, this returns an empty collection.
    /// </summary>
    public Cell this[int x, int y]
    {
        get
        {
            if (x >= 0 && x < width && y >= 0 && y < height)
            {
                if (x < mapText[y].Length)
                {
                    char ch = mapText[y][x];

                    Cell cell;
                    if (mapLegend.TryGetValue(ch, out cell))
                    {
                        return cell;
                    }
                }
            }

            return Cell.empty;
        }
    }

    /// <summary>
    /// Yields each location that contains the door name indicated.
    /// </summary>
    public IEnumerable<Location> FindDoors(string doorName)
    {
        ILookup<string, Location> doorLocs = Lazy.Init(ref lazyDoorLocations, delegate
        {
            return (from y in Enumerable.Range(0, height)
                    from x in Enumerable.Range(0, width)
                    let name = this[x, y].doorName
                    where name != null
                    let loc = new Location(x, y, this)
                    select new { name, loc }).
                    ToLookup(pair => pair.name, pair => pair.loc);
        });

        return doorLocs[doorName];
    }

    /// <summary>
    /// Load() reads the map data from the reader given, and selects
    /// suitable prefabs by name from the set of prefabs offered (which
    /// must include all prefabs that could be used).
    /// 
    /// To ensure maps are not loaded twice, the MapController calls this;
    /// everyone else calls methods on the MapController.
    /// </summary>
    public static Map Load(int mapIndex, string name, TextReader reader, IEnumerable<GameObject> prefabs)
    {
        var mapLegendByName = new Dictionary<char, string>();
        var buffer = new List<string>();
        float cameraSize = 3f;

        string line;
        while ((line = reader.ReadLine()) != null)
        {
            if (line == "-")
                break;

            buffer.Add(line);
        }

        while ((line = reader.ReadLine()) != null)
        {
            line = line.Trim();

            if (line.Length > 0)
            {
                int firstSpace = line.IndexOf(' ');

                if (firstSpace == 1)
                    mapLegendByName.Add(line[0], line.Substring(1).Trim());
                else if (firstSpace < 0 && line.Length == 1)
                    mapLegendByName.Add(line[0], "");
                else if (firstSpace > 1)
                {
                    string optionName = line.Substring(0, firstSpace);
                    string optionValue = line.Substring(firstSpace + 1);

                    if (optionName.Equals("CameraSize", StringComparison.InvariantCultureIgnoreCase))
                        cameraSize = float.Parse(optionValue);
                    else
                        throw new FormatException(string.Format("'{0}' is not a valid map option.", optionName));
                }
                else
                    throw new FormatException(string.Format("Map legend line '{0}' is malformed", line));
            }
        }

        return new Map(mapIndex, name, cameraSize, buffer.ToArray(), new MapLegend(mapLegendByName, prefabs));
    }

    /// <summary>
    /// This class holds onto the list of prefabs for each map character;
    /// it mainly exists to save typing, by having a short name!
    /// </summary>
    private sealed class MapLegend : Dictionary<char, Cell>
    {
        public MapLegend(Dictionary<char, string> byName, NamedObjectDictionary namedPrefabs)
        {
            foreach (var pair in byName)
            {
                Add(pair.Key, namedPrefabs.GetCell(pair.Value));
            }
        }

        public MapLegend(Dictionary<char, string> byName, IEnumerable<GameObject> prefabs) :
            this(byName, new NamedObjectDictionary(prefabs))
        {
        }
    }

    /// <summary>
    /// This class is a dictionary that holds objects by name, and provides
    /// a convenient method to extract them in bunches.
    /// </summary>
    private sealed class NamedObjectDictionary : Dictionary<string, GameObject>
    {
        public NamedObjectDictionary(IEnumerable<GameObject> objects)
        {
            foreach (GameObject obj in objects)
                Add(obj.name, obj);
        }

        public Cell GetCell(string text)
        {
            GameObject[] prefabs = new GameObject[0];
            string doorName = null;

            string[] parts = text.Split(new[] { ":" }, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length > 0)
            {
                string[] objNames = parts[0].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                prefabs = objNames.Select(n => ResolvePrefab(n.Trim())).ToArray();

                if (parts.Length == 2)
                {
                    doorName = parts[1].Trim();
                }
                else if (parts.Length > 2)
                {
                    throw new FormatException("'{0}' is not valid because it contains too many ':' characters.");
                }
            }

            return new Cell(new ReadOnlyCollection<GameObject>(prefabs), doorName);
        }

        public GameObject ResolvePrefab(string name)
        {
            GameObject prefab;
            if (TryGetValue(name, out prefab))
            {
                return prefab;
            }

            string msg = string.Format("The prefab named '{0}' was not found.", name);
            throw new KeyNotFoundException(msg);
        }
    }

    /// <summary>
    /// This class holds the data for one cell in the map. The same cell object
    /// will be used for many places in the same map.
    /// </summary>
    public sealed class Cell
    {
        public static readonly Cell empty = new Cell(new ReadOnlyCollection<GameObject>(new GameObject[0]));
        public readonly ReadOnlyCollection<GameObject> prefabs;
        public readonly string doorName;

        public Cell(ReadOnlyCollection<GameObject> prefabs) : this(prefabs, null)
        {
        }

        public Cell(ReadOnlyCollection<GameObject> prefabs, string doorName)
        {
            this.prefabs = prefabs;
            this.doorName = doorName;
        }

        public override string ToString()
        {
            string text = string.Join(
                ",",
                (from pf in prefabs select pf.name).ToArray());

            if (doorName != null)
                text += string.Format(" [{0}]", doorName);

            return text;
        }

        /// <summary>
        /// This creates the terrain object for the cell, setting its
        /// parent and position as indicated. This method returns null only
        /// if this cell is empty.
        /// </summary>
        public GameObject InstantiateTerrain(Location location, GameObject container)
        {
            if (prefabs.Count == 0)
            {
                return null;
            }

            GameObject terrain = GameObject.Instantiate(prefabs[0]);
            terrain.name = prefabs[0].name;
            terrain.transform.parent = container.transform;
            terrain.transform.localPosition = location.ToPosition();
            return terrain;
        }

        /// <summary>
        /// Instantiates the non-terrain entities, if any. If this cell is empty
        /// or contains terrain only, this will return an empty array.
        /// </summary>
        public GameObject[] InstantiateEntities(Location location, GameObject container)
        {
            if (prefabs.Count < 2)
            {
                return new GameObject[0];
            }

            GameObject[] created = new GameObject[prefabs.Count - 1];

            for (int i = 1; i < prefabs.Count; ++i)
            {
                GameObject go = GameObject.Instantiate(prefabs[i]);
                go.name = prefabs[i].name;
                go.transform.parent = container.transform;
                go.transform.localPosition = location.ToPosition();
                created[i - 1] = go;
            }

            return created;
        }
    }
}