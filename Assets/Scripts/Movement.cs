/*
 * Script adjusts object's intended displacement (i.e. movement) based on collision detection from raycasts
 * Collision happens with objects with the relevant layermask
 */

using UnityEngine;
using System.Collections.Generic;

public class Movement : BoxColliderCasts
{

	[HideInInspector] public CollisionDirection collisionDirection;
	[HideInInspector] public CollisionAngle collisionAngle;
	[HideInInspector] public Vector2 objectInput;
	[HideInInspector] public bool slidingDownMaxSlope = false;
	[HideInInspector] public bool forceFall = false;

	private const float wallAngle = 90;
	private const float wallTolerence = 1;
	[SerializeField] [Range(0f, wallAngle - wallTolerence)] private float maxSlopeAngle = 80;

	private int faceDirection = 0;
	private bool passThroughPlatform = false;
	private bool ascendSlope = false;
	private bool descendSlope = false;

	public override void Start()
	{
		base.Start();
	}

	/// <summary>
	/// Checks for collisions then applies correct transform translation to move object
	/// </summary>
	public void Move(Vector2 displacement, Vector2 input)
	{
		ResetDetection();

		objectInput = input;

		if (displacement.y < 0)
		{
			CheckSlopeDescent(ref displacement);
		}

		// Check face direction - done after slope descent in case of sliding down max slope
		if (displacement.x != 0)
		{
			faceDirection = (int) Mathf.Sign(displacement.x);
		}

        CheckHorizontalCollisions(ref displacement);

        if (displacement.y != 0)
		{
            CheckVerticalCollisions(ref displacement);
        }

		transform.Translate(displacement);

		// Reset grounded variables
		if (collisionDirection.below == true)
		{
			forceFall = false;
		}
	}

	void ResetDetection()
	{
		UpdateRaycastOrigins();
		UpdateBoxCastOrigins();
		collisionDirection.Reset();
		collisionAngle.Reset();
		ascendSlope = false;
		descendSlope = false;
		slidingDownMaxSlope = false;
	}

	/// <summary>
	/// Check horizontal collisions using box cast (more smooth than ray cast), if angle hit found check for ascent
	/// </summary>
	void CheckHorizontalCollisions(ref Vector2 displacement)
	{
		float directionX = faceDirection;

		// Use 2x skin due box cast origin being brought in 
		float rayLength = Mathf.Abs(displacement.x) + skinWidth * 2;

        Vector2 boxRayOrigin = (directionX == -1) ? boxCastOrigins.leftCenter : boxCastOrigins.rightCenter;
		boxRayOrigin -= Vector2.right * directionX * skinWidth;

		Vector2 boxCastSize = new Vector2(skinWidth, boundsHeight - skinWidth);

        ContactFilter2D contactFilter2D = new ContactFilter2D();
        contactFilter2D.SetLayerMask(collisionMask);

        List<RaycastHit2D> results = new List<RaycastHit2D>();

        Physics2D.BoxCast(boxRayOrigin, boxCastSize, 0, Vector2.right * directionX, contactFilter2D, results, rayLength);

        for (int i = 0; i < results.Count; i++)
        {
            RaycastHit2D hit = results[i];
            if (hit)
            {
                if (hit.collider.tag == "Through")
                {
                    continue;
                }

                float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
                collisionAngle.setSlopeAngle(slopeAngle, hit.normal);

                // Calc slope movement logic when first ray hit is an allowed angled
                if (slopeAngle <= maxSlopeAngle)
                {
                    if (descendSlope)
                    {
                        descendSlope = false;
                    }
                    CheckSlopeAscent(ref displacement, slopeAngle);
                }

                if (!ascendSlope || slopeAngle > maxSlopeAngle)
                {
					// Set displacement be at hit
                    displacement.x = hit.distance * directionX;

                    // Adjust y accordingly using tan(angle) = O/A, to prevent further ascend when wall hit
                    if (ascendSlope)
                    {
                        displacement.y = Mathf.Tan(collisionAngle.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(displacement.x);
                    }

                    collisionDirection.left = directionX == -1;
                    collisionDirection.right = directionX == 1;
                }
            }
        }
    }

	/// <summary>
	/// Check vertical collisions using ray cast - not using box cast here as it starts to interfere with horizontal box cast / slopes
	/// </summary>
	void CheckVerticalCollisions(ref Vector2 displacement)
	{
		float directionY = Mathf.Sign(displacement.y);
		float rayLength = Mathf.Abs(displacement.y) + skinWidth;

        for (int i = 0; i < verticalRayCount; i++)
        {
            // Send out rays to check for collisions for given layer in y dir, starting based on whether travelling up/down
            Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
            // Note additional distance from movement in x dir needed to adjust rayOrigin correctly
            rayOrigin += Vector2.right * (verticalRaySpacing * i + displacement.x);
            RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.up * directionY, rayLength, collisionMask);

            if (hit)
            {
                // Shows green ray if hit detected
                Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.green);

				// Allow drop/jump through "Through" platforms
                if (hit.collider.tag == "Through")
                {
                    if (directionY == 1 || hit.distance == 0)
                    {
                        continue;
                    }
                    if (passThroughPlatform)
                    {
                        continue;
                    }
                    if (objectInput.y == -1)
                    {
                        passThroughPlatform = true;
                        Invoke("ResetPassThroughPlatform", .5f);
                        continue;
                    }
                }

                // Move object to just before the hit ray
                displacement.y = (hit.distance - skinWidth) * directionY;
                // Adjust ray length to make sure future rays don't lead to further movement past current hit
                rayLength = hit.distance;

                // Adjust x accordingly using tan(angle) = O/A, to prevent further ascend when ceiling hit
                if (ascendSlope)
                {
                    displacement.x = displacement.y / Mathf.Tan(collisionAngle.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(displacement.x);
                }

                collisionDirection.below = directionY == -1;
                collisionDirection.above = directionY == 1;
            }
            else
            {
                // Draw remaining rays being checked
                Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);
            }
        }

    }

	void ResetPassThroughPlatform()
	{
		passThroughPlatform = false;
	}

	/// <summary>
	/// Use of trig to work out X/Y components of displacement up slope
	/// </summary>
	void CheckSlopeAscent(ref Vector2 displacement, float slopeAngle)
	{
		/// Use intended x dir speed for moveDistance (H) up slope 
		float moveDistance = Mathf.Abs(displacement.x);
		/// Work out ascendDisplacementY (O) with Sin(angle)=O/H
		float ascendDisplacementY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

		// Check if object is jumping already before ascend
		if (displacement.y <= ascendDisplacementY)
		{
			displacement.y = ascendDisplacementY;
			/// Work out ascendDisplacementX (A) with Cos(angle)=A/H
			displacement.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(displacement.x);
			collisionDirection.below = true;
			ascendSlope = true;
		}
	}

	/// <summary>
	/// Use of trig to work out X/Y components of displacement down slope
	/// Additional checks done for max slope descent
	/// </summary>
	void CheckSlopeDescent(ref Vector2 displacement)
	{
		// Check for max slope angle hits, XOR ensures only on side checked at a time
		RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(displacement.y) + skinWidth, collisionMask);
		RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(displacement.y) + skinWidth, collisionMask);
		if (maxSlopeHitLeft ^ maxSlopeHitRight)
		{
			if (maxSlopeHitLeft)
			{
				SlideDownMaxSlope(maxSlopeHitLeft, ref displacement);
			} else
            {
				SlideDownMaxSlope(maxSlopeHitRight, ref displacement);
			}
		}

		if (!slidingDownMaxSlope)
		{
			float directionX = Mathf.Sign(displacement.x);
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
			// Cast ray downwards infinitly to check for slope
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

			if (hit)
			{
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				collisionAngle.setSlopeAngle(slopeAngle, hit.normal);

				bool descendableSlope = slopeAngle != 0 && slopeAngle <= maxSlopeAngle;
				bool moveInSlopeDirection = Mathf.Sign(hit.normal.x) == directionX;
				// Calculate accordingly using tan(angle) = O/A, to prevent further falling when slope hit
				bool fallingToSlope = hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(displacement.x);

				if (descendableSlope && moveInSlopeDirection && fallingToSlope)
				{
					/// Use intended x dir speed for moveDistance (H) down slope 
					float moveDistance = Mathf.Abs(displacement.x);
					/// Work out descendDisplacementY (O) with Sin(angle)=O/H
					float descendDisplacementY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
					/// Work out descendDisplacementX (A) with Cos(angle)=A/H
					displacement.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(displacement.x);
					displacement.y -= descendDisplacementY;

					descendSlope = true;
					collisionDirection.below = true;
				}
			}
		}
	}

	/// <summary>
	/// Slides down a non-climbable i.e. max slope based on gravity component affecting y
	/// </summary>
	void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 displacement)
	{
		float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
		collisionAngle.setSlopeAngle(slopeAngle, hit.normal);
		if (slopeAngle > maxSlopeAngle && slopeAngle < wallAngle - wallTolerence)
		{
			// Calculate accordingly using tan(angle) = O / A, to slide on slope, where x (A), where y (O)
			displacement.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(displacement.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);
			slidingDownMaxSlope = true;
		}
	}

	/// <summary>
	/// Contains information about the most recent collision's directions
	/// </summary>
	public struct CollisionDirection
	{
		public bool above, below;
		public bool left, right;

		public void Reset()
		{
			above = below = false;
			left = right = false;
		}
	}

	/// <summary>
	/// Contains information about the most recent collision's slope
	/// </summary>
	public struct CollisionAngle
	{
		public float slopeAngle;
		public Vector2 slopeNormal;
		public bool onWall;

		public void Reset()
		{
			slopeAngle = 0;
			slopeNormal = Vector2.zero;
			onWall = false;
		}

		public void setSlopeAngle(float angle, Vector2 normal)
		{
			slopeAngle = angle;
			slopeNormal = normal;

			bool wallHit = (wallAngle - wallTolerence < slopeAngle) && (slopeAngle < wallAngle + wallTolerence);
			if (wallHit)
			{
				onWall = true;
			}
		}
	}

}
