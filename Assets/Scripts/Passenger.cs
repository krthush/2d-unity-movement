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
		PassengerMover passengerMover = gameObject.GetComponent<PassengerMover>();

		if (passengerMover)
        {
			passengerMover.CalculatePassengerMovement(displacement);

			passengerMover.MovePassengers(true);
			MoveTarget(displacement);
			passengerMover.MovePassengers(false);
		} else
        {
			MoveTarget(displacement);
		}
	}

	void MoveTarget(Vector2 displacement)
    {
		if (gameObject.tag == "Player" || gameObject.tag == "Enemy" || gameObject.tag == "Ally")
		{
			Movement movement = GetComponent<Movement>();
			if (movement)
			{
				movement.Move(displacement, Vector2.zero);
			}
			else
			{
				Debug.LogError("gameObject requires movement script if passenger");
			}
		}
		else
		{
			transform.Translate(displacement);
		}
	}
}
