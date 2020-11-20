/*
 * Script adjusts player's intended movement i.e. displacement to correct displacement based on collision detection from raycasts
 * Collision happens with objects with the relevant layermask
 */

using UnityEngine;
using System.Collections;

public class PlayerMovement : BoxRaycasts
{

	[HideInInspector] public CollisionDirection collisionDirection;
	[HideInInspector] public CollisionAngle collisionAngle;
	[HideInInspector] public Vector2 playerInput;
	[HideInInspector] public bool slidingDownMaxSlope = false;
	[HideInInspector] public bool forceFall = false;

	private const float wallAngle = 90;
	private const float wallTolerence = 1;
	[SerializeField] [Range(0f, wallAngle - wallTolerence)] private float maxSlopeAngle = 80;

	private int faceDirection = 0;
	private bool fallThroughPlatform = false;
	private bool ascendSlope = false;
	private bool descendSlope = false;
	private int attemptingMaxSlopeEdgeClimb = 0;

	public override void Start()
	{
		base.Start();
	}

	/// <summary>
	/// Checks for collisions then applies correct transform translation to move player
	/// </summary>
	public void Move(Vector2 displacement, Vector2 input)
	{
		ResetDetection();

		playerInput = input;

		// Clamp movement if trying to move into max slope edge, reset otherwise
		if (attemptingMaxSlopeEdgeClimb != 0 && attemptingMaxSlopeEdgeClimb == Mathf.Sign(displacement.x) && displacement.y <= 0)
		{
			displacement.x = 0;
		} else
		{
			attemptingMaxSlopeEdgeClimb = 0;
		}

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
			// Also check change in slope and adjust displacement to prevent staggered movement between angle change
			if (ascendSlope)
			{
                CheckChangeInSlope(ref displacement);
            }
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
		collisionDirection.Reset();
		collisionAngle.Reset();
		ascendSlope = false;
		descendSlope = false;
		slidingDownMaxSlope = false;
	}

	void CheckHorizontalCollisions(ref Vector2 displacement)
	{
		float directionX = faceDirection;
		float rayLength = Mathf.Abs(displacement.x) + skinWidth;

		if (Mathf.Abs(displacement.x) < skinWidth)
		{
			rayLength = 2 * skinWidth;
		}

		for (int i = 0; i < horizontalRayCount; i++)
		{
			// Send out rays to check for collisions for given layer in y dir, starting based on whether travelling up/down
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
			rayOrigin += Vector2.up * (horizontalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

			if (hit)
			{
				//Debug.Break();

				// Shows green ray if hit detected
				Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.green);

				if (hit.distance == 0)
				{
					continue;
				}

				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				collisionAngle.setSlopeAngle(slopeAngle, hit.normal);

				// Calc slope movement logic when first ray hit is an allowed angled
				if (i == 0 && slopeAngle <= maxSlopeAngle)
				{
					if (descendSlope)
					{
						descendSlope = false;
                    }
					CheckSlopeAscent(ref displacement, slopeAngle);
				}

				if (!ascendSlope || slopeAngle > maxSlopeAngle)
				{
					// Move player to just before the hit ray
					bool wallHit = (wallAngle - wallTolerence < slopeAngle) && (slopeAngle < wallAngle + wallTolerence);
					if (wallHit)
					{
						displacement.x = (hit.distance - skinWidth) * directionX;
					} else
					{
						// double skin with to prevent overshooting/incorrect movement when hit a slope that is above player
						displacement.x = (hit.distance - skinWidth * 2) * directionX;
					}
					// Adjust ray length to make sure future rays don't lead to further movement past current hit
					rayLength = hit.distance;

					// Apparent problem arises if slow down during slope rise - check if different speeds used in future
					//displacement.x = Mathf.Min(Mathf.Abs(displacement.x), (hit.distance - skinWidth)) * directionX;
					//rayLength = Mathf.Min(Mathf.Abs(displacement.x) + skinWidth, hit.distance);

					// Adjust y accordingly using tan(angle) = O/A, to sit correctly on slope when wall hit
					if (ascendSlope)
					{
						displacement.y = Mathf.Tan(collisionAngle.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(displacement.x);
					}

					collisionDirection.left = directionX == -1;
					collisionDirection.right = directionX == 1;
				}
			} else
			{
				// Draw remaining rays being checked
				Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);
			}
		}
	}

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

				if (hit.collider.tag == "Through")
				{
					if (directionY == 1 || hit.distance == 0)
					{
						continue;
					}
					if (fallThroughPlatform)
					{
						continue;
					}
					if (playerInput.y == -1)
					{
						fallThroughPlatform = true;
						Invoke("ResetFallingThroughPlatform", .5f);
						continue;
					}
				}
				
				// Move player to just before the hit ray
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

	/// <summary>
	/// Use of trig to work out X/Y components of displacement up slope
	/// </summary>
	void CheckSlopeAscent(ref Vector2 displacement, float slopeAngle)
	{
		/// Use intended x dir speed for moveDistance (H) up slope 
		float moveDistance = Mathf.Abs(displacement.x);
		/// Work out ascendDisplacementY (O) with Sin(angle)=O/H
		float ascendDisplacementY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

		// Check if player is jumping already before ascend
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
	/// Fire additional ray to check if there is about to be change in slope angle in the next frame
	/// Adjust displacement.x in advance so that there is a smooth transition
	/// </summary>
	void CheckChangeInSlope(ref Vector2 displacement)
	{
		float directionX = Mathf.Sign(displacement.x);
		float rayLength = Mathf.Abs(displacement.x) + skinWidth;
		Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * displacement.y;
		RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

		if (hit)
		{
			// Check angle against previous frame, if not matching move towards hit
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			if (slopeAngle != collisionAngle.slopeAngle)
			{
				displacement.x = (hit.distance - skinWidth) * directionX;
			}
			collisionAngle.setSlopeAngle(slopeAngle, hit.normal);
			// Check if a max slope edge is hit to clamp movement - prevent judder
			if (slopeAngle > maxSlopeAngle)
			{
				attemptingMaxSlopeEdgeClimb = (int) directionX;
			}
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
