using UnityEngine;
using System;
using System.Linq;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// This class loads the map and places all game-objects; it keeps track of
/// what it creates so we can find them again more efficiently, without
/// depending upon tags.
/// </summary>
public class MapController : MonoBehaviour
{	
	public TextAsset[] mapTexts;
	public GameObject[] prefabs;
	private Map[] lazyMaps;
	private GameObject[,] activeTerrainObjects;
	private readonly List<GameObject> entities = new List<GameObject> ();

	void Awake ()
	{
		entities.Clear ();
		lazyMaps = new Map[mapTexts.Length];
		activeMapIndex = 0;
	}

	private int storedActiveMapIndex = -1;

	public int activeMapIndex {
		get { return storedActiveMapIndex; }

		set {
			if (storedActiveMapIndex != value) {
				Map map = GetMap (value);
				storedActiveMapIndex = value;
				PopulateMapObjects (map, value);
			}
		}
	}

	private void PopulateMapObjects (Map map, int mapIndex)
	{
		var pf = new PlayfieldGenerator (map.width, map.height);
		entities.Clear ();

		for (int y = 0; y < map.height; ++y) {
			for (int x = 0; x < map.width; ++x) {
				ReadOnlyCollection<GameObject> templates = map [x, y];
				
				if (templates.Count > 0) {
					pf [x, y] = templates [0];
					
					foreach (GameObject t in templates.Skip (1)) {
						GameObject go = Instantiate (t);
						go.transform.parent = transform;
						go.transform.position = new Location (x, y, mapIndex).ToPosition ();
						entities.Add (go);
					}
				}
			}
		}
		
		activeTerrainObjects = pf.Generate (gameObject);
	}

	public Map GetMap (int mapIndex)
	{
		if (lazyMaps [mapIndex] == null) {
			TextAsset textAsset = mapTexts [mapIndex];
			using (var reader = new StringReader(textAsset.text)) {
				lazyMaps [mapIndex] = Map.Load (textAsset.name, reader, prefabs);
			}
		}

		return lazyMaps [mapIndex];
	}

	void Start ()
	{
		StartCoroutine (ExecuteTurns ());
	}

	private readonly Queue<GameObject> pendingRemoval = new Queue<GameObject> ();

	private IEnumerator ExecuteTurns ()
	{
		bool playerFound;
		do {
			playerFound = false;

			foreach (GameObject e in EntityObjects()) {
				var cc = e.GetComponent<CreatureController> ();

				if (!playerFound && cc is PlayerController)
					playerFound = true;

				if (cc != null) {
					yield return StartCoroutine (cc.DoTurnAsync ());
				}
			}

			while (pendingRemoval.Count>0) {
				GameObject toRemove = pendingRemoval.Dequeue ();
				entities.Remove (toRemove);
				Destroy (toRemove);
			}
		} while(playerFound);
	}

	public GameObject GetTerrain (Location location)
	{
		if (activeMapIndex == location.mapIndex) {
			if (location.x >= 0 && location.x < activeTerrainObjects.GetLength (0) &&
				location.y >= 0 && location.y < activeTerrainObjects.GetLength (1)) {
				return activeTerrainObjects [location.x, location.y];
			} else {
				return null;
			}
		} else {
			Map map = GetMap (location.mapIndex);
			ReadOnlyCollection<GameObject> prefabs = map [location.x, location.y];
			if (prefabs.Count > 0) {
				return prefabs [0];
			} else {
				return null;
			}
		}
	}

	public IEnumerable<GameObject> EntityObjects ()
	{
		return entities;
	}
	
	public IEnumerable<GameObject> EntityObjectsAt (Location location)
	{
		return 
			from go in entities
			where location == Location.Of (go)
			select go;
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

	public IEnumerable<GameObject> GameObjectsInCell (Location location)
	{
		GameObject terrain = GetTerrain (location);

		if (terrain != null) {
			yield return terrain;
		}

		foreach (GameObject go in EntityObjectsAt(location)) {
			yield return go;
		}
	}

	public IEnumerable<T> ComponentsInCell<T> (Location location)
		where T : Component
	{
		return
			from go in GameObjectsInCell (location)
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

	public GameObject InstantiateByName (string name, Location location)
	{
		GameObject template = prefabs.First (go => go.name == name);
		GameObject obj = Instantiate (template);
		obj.transform.parent = this.transform;
		obj.transform.position = location.ToPosition ();
		return obj;
	}
}