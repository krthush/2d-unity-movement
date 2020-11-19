/*
 * Script adjusts player's intended movement i.e. displacement to correct displacement based on collision detection
 * Collision happens with objects with the relevant layermask
 */

using UnityEngine;
using System.Collections;

public class PlayerMovement : PlayerCollision
{

	public float maxSlopeAngle = 80;

	public CollisionInfo collisions;
	[HideInInspector] public Vector2 playerInput;

	private int faceDir = 1;
	private bool fallingThroughPlatform = false;

	public override void Start()
	{
		base.Start();
	}

	public void Move(Vector2 displacement, bool standingOnPlatform)
	{
		Move(displacement, Vector2.zero, standingOnPlatform);
	}

	public void Move(Vector2 displacement, Vector2 input, bool standingOnPlatform = false)
	{
		UpdateRaycastOrigins();

		collisions.Reset();
		playerInput = input;

		if (displacement.y < 0)
		{
			DescendSlope(ref displacement);
		}

		if (displacement.x != 0)
		{
			faceDir = (int)Mathf.Sign(displacement.x);
		}

        HorizontalCollisions(ref displacement);
        if (displacement.y != 0)
		{
            VerticalCollisions(ref displacement);
			// Also check change in slope and adjust displacement to prevent staggered movement between angle change
			if (collisions.climbingSlope)
			{
				CheckChangeInSlope(ref displacement);
			}
        }

		transform.Translate(displacement);

		if (standingOnPlatform)
		{
			collisions.below = true;
		}
	}

	void HorizontalCollisions(ref Vector2 displacement)
	{
		float directionX = faceDir;
		float rayLength = Mathf.Abs(displacement.x) + skinWidth;

		if (Mathf.Abs(displacement.x) < skinWidth)
		{
			rayLength = 2 * skinWidth;
		}

		for (int i = 0; i < horizontalRayCount; i++)
		{
			// Send out rays to check for collisions for given layer in y dir, starting based on whether travelling up/down
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
			// TODO: adjustment of rayOrigin by movement in y dir should be done, but since displacement.y is calculated after displacement.x its not possible
			// This creates slign miss alignment on hoz ray casts, but doesn't noticably seem to affect movement
			rayOrigin += Vector2.up * (horizontalRaySpacing * i);
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

			if (hit)
			{
				// Shows green ray if hit detected
				Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.green);

				if (hit.distance == 0)
				{
					continue;
				}

				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				collisions.setSlopeAngle(slopeAngle, hit.normal);

				// Calc slope movement logic when first ray hit is an allowed angled
				if (i == 0 && slopeAngle <= maxSlopeAngle)
				{
					if (collisions.descendingSlope)
					{
						collisions.descendingSlope = false;
                    }
					ClimbSlope(ref displacement, slopeAngle, hit.normal);
				}

				if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
				{
					// Move player to just before the hit ray
					displacement.x = (hit.distance - skinWidth) * directionX;
					// Adjust ray length to make sure future rays don't lead to further movement past current hit
					rayLength = hit.distance;

					// Apparent problem arises if slow down during slope rise - check if different speeds used in future
					//displacement.x = Mathf.Min(Mathf.Abs(displacement.x), (hit.distance - skinWidth)) * directionX;
					//rayLength = Mathf.Min(Mathf.Abs(displacement.x) + skinWidth, hit.distance);

					// Adjust y accordingly using tan(angle) = O/A, to sit correctly on slope when wall hit
					if (collisions.climbingSlope)
					{
						displacement.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(displacement.x);
					}

					collisions.left = directionX == -1;
					collisions.right = directionX == 1;
				}
			} else
			{
				// Draw remaining rays being checked
				Debug.DrawRay(rayOrigin, Vector2.right * directionX, Color.red);
			}
		}
	}

	void VerticalCollisions(ref Vector2 displacement)
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
					if (fallingThroughPlatform)
					{
						continue;
					}
					if (playerInput.y == -1)
					{
						fallingThroughPlatform = true;
						Invoke("ResetFallingThroughPlatform", .5f);
						continue;
					}
				}
				
				// Move player to just before the hit ray
				displacement.y = (hit.distance - skinWidth) * directionY;
				// Adjust ray length to make sure future rays don't lead to further movement past current hit
				rayLength = hit.distance;

				// Adjust x accordingly using tan(angle) = O/A, to prevent further climbing when ceiling hit
				if (collisions.climbingSlope)
				{
					displacement.x = displacement.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(displacement.x);
				}

				collisions.below = directionY == -1;
				collisions.above = directionY == 1;
			}
			else
			{
				// Draw remaining rays being checked
				Debug.DrawRay(rayOrigin, Vector2.up * directionY, Color.red);
			}
		}
	}

	/// <summary>
	/// Use of trig and use intended x dir speed, for moveDistance up slope (H)
	/// Then work out climbdisplacementY (O) with Sin(angle)=O/H
	/// And work out climbdisplacementX (A) with Cos(angle)=A/H
	/// </summary>
	void ClimbSlope(ref Vector2 displacement, float slopeAngle, Vector2 slopeNormal)
	{
		float moveDistance = Mathf.Abs(displacement.x);
		float climbdisplacementY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

		// Check if player is jumping already before climbing
		if (displacement.y <= climbdisplacementY)
		{
			displacement.y = climbdisplacementY;
			displacement.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(displacement.x);
			collisions.below = true;
			collisions.climbingSlope = true;
			collisions.setSlopeAngle(slopeAngle, slopeNormal);
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
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			collisions.setSlopeAngle(slopeAngle, hit.normal);
			if (slopeAngle != collisions.slopeAngle)
			{
				displacement.x = (hit.distance - skinWidth) * directionX;
			}
		}
	}

	void DescendSlope(ref Vector2 displacement)
	{

		RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(displacement.y) + skinWidth, collisionMask);
		RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(displacement.y) + skinWidth, collisionMask);
		if (maxSlopeHitLeft ^ maxSlopeHitRight)
		{
			SlideDownMaxSlope(maxSlopeHitLeft, ref displacement);
			SlideDownMaxSlope(maxSlopeHitRight, ref displacement);
		}

		if (!collisions.slidingDownMaxSlope)
		{
			float directionX = Mathf.Sign(displacement.x);
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

			if (hit)
			{
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				collisions.setSlopeAngle(slopeAngle, hit.normal);
				if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
				{
					if (Mathf.Sign(hit.normal.x) == directionX)
					{
						if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(displacement.x))
						{
							float moveDistance = Mathf.Abs(displacement.x);
							float descenddisplacementY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
							displacement.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(displacement.x);
							displacement.y -= descenddisplacementY;

							collisions.descendingSlope = true;
							collisions.below = true;
						}
					}
				}
			}
		}
	}

	void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 displacement)
	{
		if (hit)
		{
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			collisions.setSlopeAngle(slopeAngle, hit.normal);
			if (slopeAngle > maxSlopeAngle)
			{
				displacement.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(displacement.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

				collisions.slidingDownMaxSlope = true;
			}
		}
	}

	/// <summary>
	/// Contains information about location of collisions and slope/platform falling
	/// </summary>
	public struct CollisionInfo
	{
		public bool above, below;
		public bool left, right;

		public bool climbingSlope;
		public bool descendingSlope;
		public bool slidingDownMaxSlope;

		public float slopeAngle;
		public Vector2 slopeNormal;
		public bool wallHit;

		const float wallAngle = 90;

		public void Reset()
		{
			above = below = false;
			left = right = false;

			climbingSlope = false;
			descendingSlope = false;
			slidingDownMaxSlope = false;

			slopeAngle = 0;
			slopeNormal = Vector2.zero;
			wallHit = false;
		}

		public void setSlopeAngle(float angle, Vector2 normal)
		{
			slopeAngle = angle;
			slopeNormal = normal;
			if (slopeAngle == wallAngle)
            {
				wallHit = true;
			}
		}
	}

}
