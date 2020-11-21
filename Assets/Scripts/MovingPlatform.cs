using UnityEngine;
using System.Collections.Generic;

public class MovingPlatform : ColliderCasts
{

	public LayerMask passengerMask;

	public Vector3[] localWaypoints;
	Vector3[] globalWaypoints;

	public float speed;
	public bool cyclic;
	public float waitTime;
	[Range(0, 2)]
	public float easeAmount;

	int fromWaypointIndex;
	float percentBetweenWaypoints;
	float nextMoveTime;

	List<PassengerMovement> passengerMovement;
	Dictionary<Transform, Passenger> passengerDictionary = new Dictionary<Transform, Passenger>();

	public override void Start()
	{
		base.Start();

		globalWaypoints = new Vector3[localWaypoints.Length];
		for (int i = 0; i < localWaypoints.Length; i++)
		{
			globalWaypoints[i] = localWaypoints[i] + transform.position;
		}
	}

	void Update()
	{
		UpdateBoxCastOrigins();

		if (globalWaypoints.Length > 0)
		{
			Vector3 displacement = CalculatePlatformMovement();

			CalculatePassengerMovement(displacement);

			MovePassengers(true);
			transform.Translate(displacement);
			MovePassengers(false);
		}
	}

	float Ease(float x)
	{
		float a = easeAmount + 1;
		return Mathf.Pow(x, a) / (Mathf.Pow(x, a) + Mathf.Pow(1 - x, a));
	}

	Vector3 CalculatePlatformMovement()
	{

		if (Time.time < nextMoveTime)
		{
			return Vector3.zero;
		}

		fromWaypointIndex %= globalWaypoints.Length;
		int toWaypointIndex = (fromWaypointIndex + 1) % globalWaypoints.Length;
		float distanceBetweenWaypoints = Vector3.Distance(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex]);
		percentBetweenWaypoints += Time.deltaTime * speed / distanceBetweenWaypoints;
		percentBetweenWaypoints = Mathf.Clamp01(percentBetweenWaypoints);
		float easedPercentBetweenWaypoints = Ease(percentBetweenWaypoints);

		Vector3 newPos = Vector3.Lerp(globalWaypoints[fromWaypointIndex], globalWaypoints[toWaypointIndex], easedPercentBetweenWaypoints);

		if (percentBetweenWaypoints >= 1)
		{
			percentBetweenWaypoints = 0;
			fromWaypointIndex++;

			if (!cyclic)
			{
				if (fromWaypointIndex >= globalWaypoints.Length - 1)
				{
					fromWaypointIndex = 0;
					System.Array.Reverse(globalWaypoints);
				}
			}
			nextMoveTime = Time.time + waitTime;
		}

		return newPos - transform.position;
	}

	void MovePassengers(bool beforeMovePlatform)
	{
		foreach (PassengerMovement passenger in passengerMovement)
		{
			if (!passengerDictionary.ContainsKey(passenger.transform))
			{
				passengerDictionary.Add(passenger.transform, passenger.transform.GetComponent<Passenger>());
			}

			if (passenger.moveBeforePlatform == beforeMovePlatform)
			{
				passengerDictionary[passenger.transform].Move(passenger.displacement, passenger.standingOnPlatform);
			}
		}
	}

	void CalculatePassengerMovement(Vector3 displacement)
	{
		HashSet<Transform> movedPassengers = new HashSet<Transform>();
		passengerMovement = new List<PassengerMovement>();

		float directionX = Mathf.Sign(displacement.x);
		float directionY = Mathf.Sign(displacement.y);

        // Vertically moving platform
        if (displacement.y != 0)
        {
            float rayLength = Mathf.Abs(displacement.y);

            Vector2 boxRayOrigin = (directionY == -1) ? boxCastOrigins.bottomCenter : boxCastOrigins.topCenter;
            Vector2 boxCastSize = new Vector2(boundsWidth, skinWidth);
            ContactFilter2D contactFilter2D = new ContactFilter2D();
            contactFilter2D.SetLayerMask(passengerMask);
            List<RaycastHit2D> results = new List<RaycastHit2D>();

            Physics2D.BoxCast(boxRayOrigin, boxCastSize, 0, Vector2.up * directionY, contactFilter2D, results, rayLength);

            results.ForEach(delegate (RaycastHit2D hit)
            {
                if (hit)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);

                        float pushX = (directionY == 1) ? displacement.x : 0;
                        float pushY = displacement.y - (hit.distance) * directionY;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), directionY == 1, true));
                    }
                }
            });
        }

        // Horizontally moving platform
        if (displacement.x != 0)
        {
            float rayLength = Mathf.Abs(displacement.x);

            Vector2 boxRayOrigin = (directionX == -1) ? boxCastOrigins.leftCenter : boxCastOrigins.rightCenter;
            Vector2 boxCastSize = new Vector2(skinWidth, boundsHeight);
            ContactFilter2D contactFilter2D = new ContactFilter2D();
            contactFilter2D.SetLayerMask(passengerMask);
            List<RaycastHit2D> results = new List<RaycastHit2D>();

            Physics2D.BoxCast(boxRayOrigin, boxCastSize, 0, Vector2.right * directionX, contactFilter2D, results, rayLength);

            results.ForEach(delegate (RaycastHit2D hit)
            {
                if (hit)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);

                        float pushX = displacement.x - (hit.distance - skinWidth) * directionX;
                        float pushY = -skinWidth;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), false, true));
                    }
                }
            });
        }

        // Passenger on top of a horizontally or downward moving platform
        if (directionY == -1 || displacement.y == 0 && displacement.x != 0)
        {
            Vector2 boxCastSize = new Vector2(boundsWidth, skinWidth);
            ContactFilter2D contactFilter2D = new ContactFilter2D();
            contactFilter2D.SetLayerMask(passengerMask);
            List<RaycastHit2D> results = new List<RaycastHit2D>();

            Physics2D.BoxCast(boxCastOrigins.topCenter, boxCastSize, 0, Vector2.up, contactFilter2D, results, skinWidth);

            results.ForEach(delegate (RaycastHit2D hit)
            {
                if (hit)
                {
                    if (!movedPassengers.Contains(hit.transform))
                    {
                        movedPassengers.Add(hit.transform);
                        float pushX = displacement.x;
                        float pushY = displacement.y;

                        passengerMovement.Add(new PassengerMovement(hit.transform, new Vector3(pushX, pushY), true, false));
                    }
                }
            });
        }
    }

	struct PassengerMovement
	{
		public Transform transform;
		public Vector3 displacement;
		public bool standingOnPlatform;
		public bool moveBeforePlatform;

		public PassengerMovement(Transform _transform, Vector3 _displacement, bool _standingOnPlatform, bool _moveBeforePlatform)
		{
			transform = _transform;
			displacement = _displacement;
			standingOnPlatform = _standingOnPlatform;
			moveBeforePlatform = _moveBeforePlatform;
		}
	}

	void OnDrawGizmos()
	{
		if (localWaypoints != null)
		{
			Gizmos.color = Color.red;
			float size = .3f;

			for (int i = 0; i < localWaypoints.Length; i++)
			{
				Vector3 globalWaypointPos = (Application.isPlaying) ? globalWaypoints[i] : localWaypoints[i] + transform.position;
				Gizmos.DrawLine(globalWaypointPos - Vector3.up * size, globalWaypointPos + Vector3.up * size);
				Gizmos.DrawLine(globalWaypointPos - Vector3.left * size, globalWaypointPos + Vector3.left * size);
			}
		}
	}

}
