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
	private static readonly ReadOnlyCollection<GameObject> noObjects =
		new ReadOnlyCollection<GameObject> (new GameObject[0]);
	private readonly MapLegend mapLegend;
	private readonly string[] mapText;
	public readonly string name;
	public readonly int width;
	public readonly int height;

	private Map (string name, string[] mapText, MapLegend mapLegend)
	{
		this.name = name;
		this.mapText = mapText;
		this.mapLegend = mapLegend;
		this.width = mapText.Max (l => l.Length);
		this.height = mapText.Length;
	}

	/// <summary>
	/// This indexer retrives the prefabs for a given cell;
	/// if x or y is out of range, this returns an empty collection.
	/// </summary>
	public ReadOnlyCollection<GameObject> this [int x, int y] {
		get {
			if (x >= 0 && x < width && y >= 0 && y < height) {
				if (x < mapText [y].Length) {
					char ch = mapText [y] [x];

					ReadOnlyCollection<GameObject> prefabs;
					if (mapLegend.TryGetValue (ch, out prefabs)) {
						return prefabs;
					}
				}
			}

			return noObjects;
		}
	}

	/// <summary>
	/// Load() reads the map data from the reader given, and selects
	/// suitable prefabs by name from the set of prefabs offered (which
	/// must include all prefabs that could be used).
	/// 
	/// To ensure maps are not loaded twice, the MapController calls this;
	/// everyone else calls methods on the MapController.
	/// </summary>
	public static Map Load (string name, TextReader reader, IEnumerable<GameObject> prefabs)
	{
		var mapLegendByName = new Dictionary<char, string[]> ();
		var buffer = new List<string> ();
		
		string line;
		while ((line = reader.ReadLine()) != null) {
			if (line == "-")
				break;
			
			buffer.Add (line);
		}
		
		while ((line = reader.ReadLine()) != null) {
			if (line.Length > 0) {
				char ch = line [0];
				string[] names = line.Substring (1).
					Split (new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).
						Select (n => n.Trim ()).
						ToArray ();
				
				mapLegendByName.Add (ch, names);
			}
		}
		
		return new Map (name, buffer.ToArray (), new MapLegend (mapLegendByName, prefabs));
	}

	/// <summary>
	/// This class holds onto the list of prefabs for each map character;
	/// it mainly exists to save typing, by having a short name!
	/// </summary>
	private sealed class MapLegend : Dictionary<char, ReadOnlyCollection<GameObject>>
	{
		public MapLegend (Dictionary<char, string[]> byName, NamedObjectDictionary namedPrefabs)
		{
			foreach (var pair in byName) {
				Add (pair.Key, namedPrefabs.GetRange (pair.Value));
			}
		}

		public MapLegend (Dictionary<char, string[]> byName, IEnumerable<GameObject> prefabs) :
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
		public NamedObjectDictionary (IEnumerable<GameObject> objects)
		{
			foreach (GameObject obj in objects)
				Add (obj.name, obj);
		}
		
		public ReadOnlyCollection<GameObject> GetRange (string[] names)
		{
			IEnumerable<GameObject> resolved = names.Select (delegate(string name) {
				GameObject pf;
				if (TryGetValue (name, out pf))
					return pf;
				
				string msg = string.Format ("The prefab named '{0}' was not found.", name);
				throw new KeyNotFoundException (msg);
			});

			return new ReadOnlyCollection<GameObject> (resolved.ToArray ());
		}
	}
}