using UnityEngine;
using System.Collections;

/// <summary>
/// This controller sets the sprite for the object to
/// one of four values depending on its location; the idea
/// is to have an effect like double-sized tiles.
/// </summary>
public class FourWaySpriteController : MonoBehaviour
{
	public Sprite topLeft;
	public Sprite topRight;
	public Sprite bottomLeft;
	public Sprite bottomRight;
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
	/// This method returns the sprite to use based on
	/// the current location; the 'top left' sprite is
	/// used for even numbered x and y positions, and
	/// we go to 'bottom' and 'right' for odd numbered
	/// ones. Simple!
	/// </summary>
	private Sprite PickSprite ()
	{
		Location loc = Location.Of (gameObject);
		
		bool hEven = (loc.x % 2) == 0;
		bool vEven = (loc.y % 2) == 0;

		if (hEven && vEven) {
			return topLeft;
		} else if (hEven && !vEven) {
			return bottomLeft;
		} else if (!hEven && vEven) {
			return topRight;
		} else if (!hEven && !vEven) {
			return bottomRight;
		} else {
			return null;
		}
	}
}
