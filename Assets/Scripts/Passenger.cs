using UnityEngine;

/// <summary>
/// Object can be moved by passenger movers
/// </summary>
public class Passenger : MonoBehaviour
{
	/// <summary>
	/// Move passenger by given displacement
	/// </summary>
	public void Move(Vector2 displacement, bool standingOnPlatform)
	{
		if (gameObject.tag == "Player" || gameObject.tag == "Enemy")
        {
			Movement movement = GetComponent<Movement>();
			if (movement)
			{
				movement.Move(displacement, Vector2.zero);
			} else
            {
				Debug.LogError("gameObject requires movement script if passenger");
            }
		} else
		{
			transform.Translate(displacement);
		}
	}
}
