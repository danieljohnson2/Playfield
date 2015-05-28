using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// There are 15 sprites named with bitwise naming: in theory it should be possible to get the sprite with the
/// correct name by looking at adjacent spots on the map. Bits are true (1) if the location does NOT have more
/// wall. Form is CastleWall_0000 and the bits are Top,Bottom,Left,Right.
/// </summary>
public class CastleWallSpriteController : MonoBehaviour
{
	public Sprite hasWall;
	public Sprite noWall;
	private SpriteRenderer spriteRenderer;
	
	void Awake ()
	{
		this.spriteRenderer = GetComponent<SpriteRenderer> ();
	}
	
	void Update ()
	{
		Sprite newSprite = PickSprite ();
		
		if (newSprite != null)
			spriteRenderer.sprite = newSprite;
	}
	
	/// <summary>
	/// I would totally do this by grabbing the sprite by name but we're not doing anything like that,
	/// so everything about this is totally opaque and ungraspable to me including how the map works.
	/// </summary>
	private Sprite PickSprite ()
	{
		MapController mapController = MapController.instance;

		Location loc = Location.Of (gameObject);
		Location[] buffer = loc.Adjacent ().ToArray (); // order is n, s, e, w

		GameObject north = mapController.GetTerrain (buffer [0]);

		//	Location[] buffer = loc.GetAdjacentInto();   //no overload for GetAdjacentInto takes 0 arguments

		//	GameObject topWall = GameObject.Find ((loc.x) + "," + (loc.y + 1));       //is always null when loaded as a gameobject
		
		//	bool topWall = (loc.Adjacent (north).ToString == "Foo");     //useless because the adjacent method can't be used this way

		//	loc.GetAdjacentInto (buffer);
		//	bool topWall = (buffer[0].mapIndex == 0);   /// nope, not even in a do-nothing form

		bool useParapet = !IsWall (north);

		if (useParapet) {
			return hasWall;  //currently assigned to sprite CastleWall_1000, tile to north is NOT more wall
		} else {
			return noWall;  //currently assigned to sprite CastleWall_0000, tiles to NSEW are also more wall
		}
	}

	/// <summary>
	/// IsWall() decides if the terrain object given is a wall.
	/// </summary>
	private static bool IsWall (GameObject terrain)
	{
		// this is a lousy way to test for wall-ness, but I'm not sure
		// what the right way is! We can replace this later.
		return terrain != null && terrain.name == "Wall";
	}
}
