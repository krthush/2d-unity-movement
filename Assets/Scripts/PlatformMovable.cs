using UnityEngine;

public class PlatformMovable : MonoBehaviour
{
	/// <summary>
	/// Moves object when on platform
	/// </summary>
	public void Move(Vector2 displacement, bool standingOnPlatform)
	{
		if (gameObject.tag == "Player")
        {
			PlayerMovement playerMovement = GetComponent<PlayerMovement>();
			playerMovement.Move(displacement, Vector2.zero);
		} else
		{
			transform.Translate(displacement);
		}
	}
}
