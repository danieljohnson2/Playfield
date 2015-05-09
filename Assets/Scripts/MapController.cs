using UnityEngine;
using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.IO;

/// <summary>
/// This class loads the map and places all game-objects; it keeps track of
/// what it creates so we can find them again more efficiently, without
/// depending upon tags.
/// 
/// There can be only one of these for the entire game; it loads and
/// tracks the maps and active entities.
/// </summary>
public class MapController : MonoBehaviour
{
	void Start ()
	{
		activeMap = maps [0];
		StartCoroutine (ExecuteTurns ());
	}

	private IEnumerator ExecuteTurns ()
	{
		bool playerFound;
		do {
			entities.ActivateEntities (activeMap);

			playerFound = false;

			foreach (var cc in entities.Components<CreatureController>().ToArray ()) {
				if (!playerFound && cc is PlayerController) {
					playerFound = true;
					activeMap = maps [Location.Of (cc.gameObject).mapIndex];
				}
				yield return StartCoroutine (cc.DoTurnAsync ());
			}

			entities.ProcessRemovals ();
		} while(playerFound);
	}

	#region Active Map
	
	private Map storedActiveMap;
	private readonly LazyList<GameObject> lazyMapContainers = new LazyList<GameObject> ();
	private readonly HashSet<Location> loadedEntityLocations = new HashSet<Location> ();

	public Map activeMap {
		get { return storedActiveMap; }
		
		set {
			if (storedActiveMap != value) {
				storedActiveMap = value;
				PopulateMapObjects (value);
				entities.ActivateEntities (value);
			}
		}
	}

	private void PopulateMapObjects (Map map)
	{
		if (activeTerrainObjects != null) {
			foreach (GameObject go in activeTerrainObjects) {
				Destroy (go);
			}
			activeTerrainObjects = null;
		}

		activeTerrainObjects = new GameObject[map.width, map.height];
		int mapIndex = map.mapIndex;

		GameObject mapContainer = lazyMapContainers.GetOrCreate (mapIndex, delegate {
			var c = new GameObject (map.name);
			c.transform.parent = transform;
			return c;
		});

		for (int y = 0; y < map.height; ++y) {
			for (int x = 0; x < map.width; ++x) {
				Map.Cell cell = map [x, y];
				
				if (cell.prefabs.Count > 0) {
					Location location = new Location (x, y, mapIndex);

					activeTerrainObjects [x, y] = cell.InstantiateTerrain (location, mapContainer);

					if (loadedEntityLocations.Add (location)) {
						entities.RegisterEntities (cell.InstantiateEntities (location, mapContainer));
					}
				}
			}
		}
	}

	#endregion

	#region Terrain and Movement

	private GameObject[,] activeTerrainObjects;

	/// <summary>
	/// Returns the terrain object for a cell, or null if
	/// the location is outside the map. Warning: this will
	/// instantiate the object if it is not active, but will
	/// return the prefab itself for terrain not on the
	/// active map.
	/// </summary>
	public GameObject GetTerrain (Location location)
	{
		if (activeMap != null && 
			activeMap.mapIndex == location.mapIndex && 
			activeTerrainObjects != null) {
			if (location.x >= 0 && location.x < activeTerrainObjects.GetLength (0) &&
				location.y >= 0 && location.y < activeTerrainObjects.GetLength (1)) {
				return activeTerrainObjects [location.x, location.y];
			} else {
				return null;
			}
		} else {
			return maps [location].prefabs.FirstOrDefault ();
		}
	}

	/// <summary>
	/// ComponentsInCell() returns the components fo the type
	/// indicated of all the objects in the location, including
	/// terrain. Warning: the terrain object may be a prefab; we
	/// do not activate a map just to access the real object
	/// for the terrain.
	/// </summary>
	public IEnumerable<T> ComponentsInCell<T> (Location location)
		where T : Component
	{
		GameObject terrain = GetTerrain (location);
		T terrainComponent = terrain != null ? terrain.GetComponent<T> () : null;

		if (terrainComponent != null) {
			return new [] { terrainComponent }.Concat (entities.ComponentsAt<T> (location));
		} else {
			return entities.ComponentsAt<T> (location);
		}
	}

	/// <summary>
	/// IsPassable is true if the cell indicated by 'where' may be
	/// walked through; it is false if lcoations outside hte map,
	/// but true for cells that contain no movement-blocker objects.
	/// </summary>
	public bool IsPassable (Location where)
	{
		if (GetTerrain (where) == null) {
			return false;
		}

		return ComponentsInCell<MovementBlocker> (where).All (mb => mb.passable);
	}

	/// <summary>
	/// IsPathable indicates whether heatmaps can make a path through
	/// this cell; it is false outside the map, and may be false for cells
	/// inside depending on what movement blockers are found there.
	/// </summary>
	/// <returns><c>true</c> if this instance is pathable the specified where; otherwise, <c>false</c>.</returns>
	/// <param name="where">Where.</param>
	public bool IsPathable (Location where)
	{
		if (GetTerrain (where) == null) {
			return false;
		}

		return ComponentsInCell<MovementBlocker> (where).All (mb => mb.pathable);
	}

	#endregion
	
	#region Entity Objects

	public readonly EntityTracker entities = new EntityTracker ();

	/// <summary>
	/// This object tracks the active entities, which are created as
	/// we enter rooms and destroyed by components. The player is
	/// one of these.
	/// </summary>
	public sealed class EntityTracker
	{
		private readonly List<GameObject> entities = new List<GameObject> ();
		private readonly Queue<GameObject> pendingRemoval = new Queue<GameObject> ();

		/// <summary>
		/// RegisterEntities() places new entities into the tracker;
		/// we use this when we load rooms.
		/// </summary>
		public void RegisterEntities (IEnumerable<GameObject> toAdd)
		{
			entities.AddRange (toAdd);
		}

		/// <summary>
		/// RemoveEntity() causes the entity given to be removed
		/// from the game at end of turn; it is destroyed and
		/// also unregistered at tha time.
		/// </summary>
		public void RemoveEntity (GameObject toRemove)
		{
			if (toRemove != null) {
				pendingRemoval.Enqueue (toRemove);
			}
		}

		/// <summary>
		/// Actually performs the removals RemoveEntity() schedules;
		/// this is called at end of turn.
		/// </summary>
		public void ProcessRemovals ()
		{		
			while (pendingRemoval.Count> 0) {
				GameObject toRemove = pendingRemoval.Dequeue ();
				entities.Remove (toRemove);
				Destroy (toRemove);
			}
		}
		
		/// <summary>
		/// This method yields the component of type 'T'
		/// every active entity.
		/// </summary>
		public IEnumerable<T> Components<T> ()
			where T : Component
		{
			return
				from go in entities
				select go.GetComponent<T> () into c
				where c != null
				select c;
		}

		/// <summary>
		/// This yields the components of the type 'T' of the
		/// entties, but only entities in the locaiton given
		/// are considered; terrain is also not considered.
		/// </summary>
		public IEnumerable<T> ComponentsAt <T> (Location location)
			where T : Component
		{
			return 
				from go in entities
				where location == Location.Of (go)
				select go.GetComponent<T> () into c
				where c != null
				select c;
		}
				
		/// <summary>
		/// ActivateEntities() sets the active flag on each entity;
		/// entities not on the active map are deactivated, and
		/// those on the map are activated. If 'activeMap' is null,
		/// all are deactivated.
		/// 
		/// This means that wile entities on other rooms can move, you
		/// can't see them.
		/// 
		/// This is called at end of turn, but also when changing
		/// maps.
		/// </summary>
		public void ActivateEntities (Map activeMap)
		{
			int mapIndex = activeMap != null ? activeMap.mapIndex : -1;
			
			foreach (GameObject go in entities) {
				go.SetActive (Location.Of (go).mapIndex == mapIndex);
			}
		}
	}

	#endregion

	#region Map Tracking

	public MapTracker maps;

	/// <summary>
	/// This class holds onto map data; it loads maps as needed
	/// but must be configured in the Unity editor to have the text
	/// of each map as all the prefabs.
	/// </summary>
	[Serializable]
	public class MapTracker
	{
		public TextAsset[] mapTexts;
		public GameObject[] prefabs;
		private readonly LazyList<Map> lazyMaps = new LazyList<Map> ();

		/// <summary>
		/// This indexer retreives a map by name, or throws
		/// KeyNotFoundException if the map name is not valid.
		/// </summary>
		public Map this [string mapName] {
			get {
				for (int i = 0; i < mapTexts.Length; ++i) {
					if (mapTexts [i].name == mapName) {
						return this [i];
					}
				}
			
				throw new KeyNotFoundException (string.Format (
					"The map '{0}' could not be found.",
					mapName));
			}
		}
		
		/// <summary>
		/// This indexer retreives a map by map index, or throws
		/// KeyNotFoundException if the map index is not valid.
		/// </summary>
		public Map this [int mapIndex] {
			get {
				if (mapTexts != null &&
					mapIndex >= 0 && mapIndex < mapTexts.Length) {
					return lazyMaps.GetOrCreate (mapIndex, ReadMap);
				}
			
				throw new KeyNotFoundException (string.Format (
					"Map index '{0}' could not be found.",
					mapIndex));
			}
		}

		/// <summary>
		/// This indexer returns a cell given its location in the
		/// world; if the location is not part of any valid map this
		/// returns an empty cell rather than throwing an exception.
		/// </summary>
		public Map.Cell this [Location location] {
			get {
				if (location.mapIndex >= 0 && location.mapIndex < mapTexts.Length) {
					return this [location.mapIndex] [location.x, location.y];
				} else {
					return Map.Cell.empty;
				}
			}
		}

		/// <summary>
		/// Reads the text of a map into a Map object; used
		/// to implement one of the indexers, which caches
		/// the map objects.
		/// </summary>
		private Map ReadMap (int mapIndex)
		{
			TextAsset textAsset = mapTexts [mapIndex];
			using (var reader = new StringReader(textAsset.text)) {
				return Map.Load (mapIndex, textAsset.name, reader, prefabs);
			}
		}
	}

	#endregion
}