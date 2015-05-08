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

	void Awake ()
	{
		lazyMaps = new Map[mapTexts.Length];
		lazyMapContainers = new GameObject[mapTexts.Length];
	}

	void Start ()
	{
		activeMapIndex = 0;
		StartCoroutine (ExecuteTurns ());
	}

	private IEnumerator ExecuteTurns ()
	{
		bool playerFound;
		do {
			playerFound = false;

			foreach (GameObject e in EntityObjects().ToArray ()) {
				var cc = e.GetComponent<CreatureController> ();

				if (!playerFound && cc is PlayerController) {
					playerFound = true;
					activeMapIndex = Location.Of (e).mapIndex;
				}

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

	#region Active Map
	
	private int storedActiveMapIndex = -1;
	private GameObject[] lazyMapContainers;

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

	private readonly HashSet<Location> loadedEntityLocations = new HashSet<Location> ();

	private void PopulateMapObjects (Map map, int mapIndex)
	{
		if (activeTerrainObjects != null) {
			foreach (GameObject go in activeTerrainObjects) {
				Destroy (go);
			}
			activeTerrainObjects = null;
		}

		activeTerrainObjects = new GameObject[map.width, map.height];

		GameObject mapContainer = lazyMapContainers [mapIndex];

		if (mapContainer == null) {
			mapContainer = new GameObject (map.name);
			mapContainer.transform.parent = transform;
			lazyMapContainers [mapIndex] = mapContainer;
		}

		for (int y = 0; y < map.height; ++y) {
			for (int x = 0; x < map.width; ++x) {
				Map.Cell cell = map [x, y];
				
				if (cell.prefabs.Count > 0) {
					Location location = new Location (x, y, mapIndex);

					GameObject terrain = Instantiate (cell.prefabs [0]);
					activeTerrainObjects [x, y] = terrain;
					terrain.transform.parent = mapContainer.transform;
					terrain.transform.position = location.ToPosition ();

					if (loadedEntityLocations.Add (location)) {
						foreach (GameObject prefab in cell.prefabs.Skip (1)) {
							GameObject go = Instantiate (prefab);
							go.transform.parent = mapContainer.transform;
							go.transform.position = location.ToPosition ();
							entities.Add (go);
						}
					}
				}
			}
		}

		foreach (GameObject go in entities) {
			go.SetActive (Location.Of (go).mapIndex == mapIndex);
		}
	}

	#endregion

	#region Maps

	private Map[] lazyMaps;

	public int FindMapIndex (string name)
	{
		for (int i = 0; i < mapTexts.Length; ++i) {
			if (mapTexts [i].name == name) {
				return i;
			}
		}

		throw new KeyNotFoundException (string.Format (
			"The map '{0}' could not be found.",
			name));
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

	#endregion

	#region Terrain and Movement

	private GameObject[,] activeTerrainObjects;
	
	public GameObject GetTerrain (Location location)
	{
		if (activeMapIndex == location.mapIndex && activeTerrainObjects != null) {
			if (location.x >= 0 && location.x < activeTerrainObjects.GetLength (0) &&
				location.y >= 0 && location.y < activeTerrainObjects.GetLength (1)) {
				return activeTerrainObjects [location.x, location.y];
			} else {
				return null;
			}
		} else {
			Map map = GetMap (location.mapIndex);
			Map.Cell cell = map [location.x, location.y];
			return cell.prefabs.FirstOrDefault ();
		}
	}

	public bool IsPassable (Location loc)
	{
		if (GetTerrain (loc) == null) {
			return false;
		}

		return ComponentsInCell<MovementBlocker> (loc).All (mb => mb.passable);
	}

	public bool IsPathable (Location loc)
	{
		if (GetTerrain (loc) == null) {
			return false;
		}

		return ComponentsInCell<MovementBlocker> (loc).All (mb => mb.pathable);
	}

	#endregion
	
	#region Entity Objects

	private readonly List<GameObject> entities = new List<GameObject> ();
	private readonly Queue<GameObject> pendingRemoval = new Queue<GameObject> ();

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
	
	#endregion
}