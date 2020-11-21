using System.Collections.Generic;
using UnityEngine;

public class PassengerMover : BoxColliderCasts
{
    public LayerMask passengerMask;

    List<PassengerMovement> passengerMovement;
    Dictionary<Transform, Passenger> passengerDictionary = new Dictionary<Transform, Passenger>();

    public override void Start()
    {
        base.Start();
    }

    void Update()
    {
        UpdateBoxCastOrigins();
    }

    public void CalculatePassengerMovement(Vector3 displacement)
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

    public void MovePassengers(bool beforeMovePlatform)
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
}
