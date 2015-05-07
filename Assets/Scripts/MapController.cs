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
	public TextAsset mapData;
	public GameObject[] prefabs;
	private Dictionary<string, MapPopulation> mapPopulations = new Dictionary<string, MapPopulation> ();
	private Dictionary<GameObject, MapPopulation> entityPlacements = new Dictionary<GameObject, MapPopulation> ();

	private class MapPopulation
	{
		public Map map;
		public GameObject[,] terrainObjects;
		public readonly List<GameObject> entities = new List<GameObject> ();

		public MapPopulation (Map map, MapController mapController)
		{
			this.map = map;
			var pf = new PlayfieldGenerator (map.width, map.height);
			entities.Clear ();
			
			for (int y = 0; y < map.height; ++y) {
				for (int x = 0; x < map.width; ++x) {
					ReadOnlyCollection<GameObject> templates = map [x, y];
					
					if (templates.Count > 0) {
						pf [x, y] = templates [0];
						
						foreach (GameObject t in templates.Skip (1)) {
							GameObject go = Instantiate (t);
							go.transform.parent = mapController.transform;
							go.transform.position = new Location (x, y, map).ToPosition ();
							entities.Add (go);
							mapController.entityPlacements.Add (go, this);
						}
					}
				}
			}
			
			terrainObjects = pf.Generate (mapController.gameObject);

			foreach (GameObject go in terrainObjects) {
				if (go != null) {
					mapController.entityPlacements.Add (go, this);
				}
			}
		}
	}

	void Awake ()
	{
		mapPopulations.Add (mapData.name, new MapPopulation (LoadMap (), this));
	}

	void Start ()
	{
		StartCoroutine (ExecuteTurns ());
	}

	public Map LoadMap ()
	{
		using (var reader = new StringReader(mapData.text)) {
			return Map.Load (mapData.name, reader, prefabs);
		}
	}

	private MapPopulation GetPopulation (Map map)
	{
		return mapPopulations [map.name];
	}

	private MapPopulation GetPopulation (GameObject gameObject)
	{
		MapPopulation pop;
		if (!entityPlacements.TryGetValue (gameObject, out pop)) {
			pop = mapPopulations.Values.FirstOrDefault (
				p => p.entities.Contains (gameObject));

			if (pop == null) {
				pop = mapPopulations.Values.FirstOrDefault (
					p => p.terrainObjects.Cast<GameObject> ().Contains (gameObject));
			}

			entityPlacements.Add (gameObject, pop);
		}

		return pop;
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
				GetPopulation (toRemove).entities.Remove (toRemove);
				entityPlacements.Remove (toRemove);
				Destroy (toRemove);
			}
		} while(playerFound);
	}

	public GameObject GetTerrain (Location location)
	{
		MapPopulation pop = GetPopulation (location.map);
		if (location.x >= 0 && location.x < pop.terrainObjects.GetLength (0) &&
			location.y >= 0 && location.y < pop.terrainObjects.GetLength (1)) {
			return pop.terrainObjects [location.x, location.y];
		} else {
			return null;
		}
	}

	public Location GetLocation (GameObject gameObject)
	{
		MapPopulation pop = GetPopulation (gameObject);
		return Location.FromPosition (gameObject.transform.position, pop.map);
	}

	public IEnumerable<GameObject> EntityObjects ()
	{
		return
			from pop in mapPopulations.Values
			from e in pop.entities
			select e;
	}
	
	public IEnumerable<GameObject> EntityObjectsAt (Location location)
	{
		return 
			from go in GetPopulation (location.map).entities
			where location == GetLocation (go)
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