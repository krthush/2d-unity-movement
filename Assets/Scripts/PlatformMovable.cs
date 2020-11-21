using UnityEngine;

public class PlatformMovable : MonoBehaviour
{
	/// <summary>
	/// Moves object when on platform
	/// </summary>
	public void Move(Vector2 displacement, bool standingOnPlatform)
	{
		if (gameObject.tag == "Player" || gameObject.tag == "Enemy")
        {
			ObjectDisplacement objectMovement = GetComponent<ObjectDisplacement>();
			objectMovement.Move(displacement, Vector2.zero);
		} else
		{
			transform.Translate(displacement);
		}
	}
}
