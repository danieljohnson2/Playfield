using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.IO;

public class MapController : MonoBehaviour
{	
	public TextAsset mapData;
	public GameObject[] prefabs;
	private GameObject[,] terrainObjects;
	private readonly List<GameObject> entities = new List<GameObject> ();

	void Start ()
	{
		string[] mapText;
		Dictionary<char, GameObject[]> mapLegend;
		ReadLinesOf (mapData, out mapText, out mapLegend);

		int width = mapText.Max (l => l.Length);
		int height = mapText.Length;

		var pf = new PlayfieldGenerator (width, height);
		entities.Clear ();

		for (int y = 0; y < mapText.Length; ++y) {
			string l = mapText [y];

			for (int x = 0; x < l.Length; ++x) {
				GameObject[] templates;

				if (mapLegend.TryGetValue (l [x], out templates) &&
					templates.Length > 0) {
					pf [x, y] = templates [0];

					foreach (GameObject t in templates.Skip (1)) {
						GameObject go = Instantiate (t);
						go.transform.parent = transform;
						go.transform.position = new Location (x, y).ToPosition ();
						entities.Add (go);
					}
				}
			}
		}

		terrainObjects = pf.Generate (gameObject);

		StartCoroutine (ExecuteTurns ());
	}

	private readonly Queue<GameObject> pendingRemoval = new Queue<GameObject> ();

	private IEnumerator ExecuteTurns ()
	{
		bool playerFound;
		do {
			playerFound = false;

			foreach (GameObject e in entities) {
				var cc = e.GetComponent<CreatureController> ();

				if (!playerFound && cc is PlayerController)
					playerFound = true;

				if (cc != null) {
					yield return StartCoroutine (cc.DoTurnAsync ());
				}
			}

			while (pendingRemoval.Count>0) {
				GameObject toRemove = pendingRemoval.Dequeue ();

				this.entities.Remove (toRemove);
				Destroy (toRemove);
			}
		} while(playerFound);
	}

	public GameObject GetTerrain (Location location)
	{
		if (location.x >= 0 && location.x < terrainObjects.GetLength (0) &&
			location.y >= 0 && location.y < terrainObjects.GetLength (1)) {
			return terrainObjects [location.x, location.y];
		} else {
			return null;
		}
	}

	public IEnumerable<GameObject> EntityObjects ()
	{
		return entities ?? Enumerable.Empty<GameObject> ();
	}

	public IEnumerable<T> EntityComponents<T> ()
		where T : Component
	{
		return
			from go in EntityObjects ()
			select go.GetComponent<T> () into c
			where c != null
			select c;
	}

	public void RemoveEntity (GameObject toRemove)
	{
		pendingRemoval.Enqueue (toRemove);
	}

	public List<GameObject> GetGameObjectsInCell (Location location)
	{
		var buffer = new List<GameObject> ();

		GameObject terrain = GetTerrain (location);

		if (terrain != null) {
			buffer.Add (terrain);
		}

		foreach (GameObject go in entities) {
			if (location == Location.Of (go)) {
				buffer.Add (go);
			}
		}

		return buffer;
	}

	public IEnumerable<T> ComponentsInCell<T> (Location location)
		where T : Component
	{
		return
			from go in GetGameObjectsInCell (location)
			select go.GetComponent<T> () into c
			where c != null
			select c;
	}
	
	public bool IsPassable (Location loc)
	{
		return ComponentsInCell<MovementBlocker> (loc).All (mb => mb.passable);
	}

	public bool IsPathable (Location loc)
	{
		return ComponentsInCell<MovementBlocker> (loc).All (mb => mb.pathable);
	}

	private void ReadLinesOf (TextAsset text, out string[] mapText, out Dictionary<char, GameObject[]> mapLegend)
	{
		var mapLegendByName = new Dictionary<char, string[]> ();
		var buffer = new List<string> ();
		
		using (var reader = new StringReader(text.text)) {
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
		}
		
		mapText = buffer.ToArray ();
		mapLegend = CreateMapLegend (mapLegendByName);
	}

	public GameObject InstantiateByName (string name, Location location)
	{
		GameObject template = prefabs.First (go => go.name == name);
		GameObject obj = Instantiate (template);
		obj.transform.parent = this.transform;
		obj.transform.position = location.ToPosition ();
		return obj;
	}

	private Dictionary<char, GameObject[]> CreateMapLegend (Dictionary<char, string[]> legendByName)
	{
		var byName = new NamedObjectDictionary (prefabs);

		return legendByName.ToDictionary (
			pair => pair.Key,
			pair => byName.GetArray (pair.Value));
	}

	private sealed class NamedObjectDictionary : Dictionary<string, GameObject>
	{
		public NamedObjectDictionary (IEnumerable<GameObject> objects)
		{
			foreach (GameObject obj in objects)
				Add (obj.name, obj);
		}

		public GameObject[] GetArray (string[] names)
		{
			return names.Select (delegate(string name) {
				GameObject pf;
				if (TryGetValue (name, out pf))
					return pf;
				
				string msg = string.Format ("The prefab named '{0}' was not found.", name);
				throw new KeyNotFoundException (msg);
			}).ToArray ();
		}
	}
}