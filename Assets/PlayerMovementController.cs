/*
 * Script adjusts player's intended displacement(moveAmount) to correct displacement based on collision detection
 * Collision happen with objects with the relevant layermask
 */

using UnityEngine;
using System.Collections;

public class PlayerMovementController : RaycastController
{

	public float maxSlopeAngle = 80;

	public CollisionInfo collisions;
	[HideInInspector] public Vector2 playerInput;

	public override void Start()
	{
		base.Start();
		collisions.faceDir = 1;
	}

	public void Move(Vector2 moveAmount, bool standingOnPlatform)
	{
		Move(moveAmount, Vector2.zero, standingOnPlatform);
	}

	public void Move(Vector2 moveAmount, Vector2 input, bool standingOnPlatform = false)
	{
		UpdateRaycastOrigins();

		collisions.Reset();
		collisions.moveAmountOld = moveAmount;
		playerInput = input;

		if (moveAmount.y < 0)
		{
			DescendSlope(ref moveAmount);
		}

		if (moveAmount.x != 0)
		{
			collisions.faceDir = (int)Mathf.Sign(moveAmount.x);
		}

        HorizontalCollisions(ref moveAmount);
        if (moveAmount.y != 0)
		{
            VerticalCollisions(ref moveAmount);
        }

		transform.Translate(moveAmount);

		if (standingOnPlatform)
		{
			collisions.below = true;
		}
	}

	void HorizontalCollisions(ref Vector2 moveAmount)
	{
		float directionX = collisions.faceDir;
		float rayLength = Mathf.Abs(moveAmount.x) + skinWidth;

		if (Mathf.Abs(moveAmount.x) < skinWidth)
		{
			rayLength = 2 * skinWidth;
		}

		for (int i = 0; i < horizontalRayCount; i++)
		{
			// Send out rays to check for collisions for given layer in y dir, starting based on whether travelling up/down
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight;
			// TODO: adjustment of rayOrigin by movement in y dir should be done, but since moveAmount.y is calculated after moveAmount.x its not possible
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

				// Calc slope movement logic when first ray hit is an allowed angled
				if (i == 0 && slopeAngle <= maxSlopeAngle)
				{
					if (collisions.descendingSlope)
					{
						collisions.descendingSlope = false;
						//moveAmount = collisions.moveAmountOld;
					}
					ClimbSlope(ref moveAmount, slopeAngle, hit.normal);
				}

				if (!collisions.climbingSlope || slopeAngle > maxSlopeAngle)
				{
					// Move player to just before the hit ray
					moveAmount.x = (hit.distance - skinWidth) * directionX;
					// Adjust ray length to make sure future rays don't lead to further movement past current hit
					rayLength = hit.distance;

					// Apparent problem arises if slow down during slope rise - check if different speeds used in future
					//moveAmount.x = Mathf.Min(Mathf.Abs(moveAmount.x), (hit.distance - skinWidth)) * directionX;
					//rayLength = Mathf.Min(Mathf.Abs(moveAmount.x) + skinWidth, hit.distance);

					// Adjust y accordingly using tan(angle) = O/A, to sit correctly on slope when wall hit
					if (collisions.climbingSlope)
					{
						moveAmount.y = Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x);
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

	void VerticalCollisions(ref Vector2 moveAmount)
	{
		float directionY = Mathf.Sign(moveAmount.y);
		float rayLength = Mathf.Abs(moveAmount.y) + skinWidth;

		for (int i = 0; i < verticalRayCount; i++)
		{
			// Send out rays to check for collisions for given layer in y dir, starting based on whether travelling up/down
			Vector2 rayOrigin = (directionY == -1) ? raycastOrigins.bottomLeft : raycastOrigins.topLeft;
			// Note additional distance from movement in x dir needed to adjust rayOrigin correctly
			rayOrigin += Vector2.right * (verticalRaySpacing * i + moveAmount.x);
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
					if (collisions.fallingThroughPlatform)
					{
						continue;
					}
					if (playerInput.y == -1)
					{
						collisions.fallingThroughPlatform = true;
						Invoke("ResetFallingThroughPlatform", .5f);
						continue;
					}
				}
				
				// Move player to just before the hit ray
				moveAmount.y = (hit.distance - skinWidth) * directionY;
				// Adjust ray length to make sure future rays don't lead to further movement past current hit
				rayLength = hit.distance;

				// Adjust x accordingly using tan(angle) = O/A, to prevent further climbing when ceiling hit
				if (collisions.climbingSlope)
				{
					moveAmount.x = moveAmount.y / Mathf.Tan(collisions.slopeAngle * Mathf.Deg2Rad) * Mathf.Sign(moveAmount.x);
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

		if (collisions.climbingSlope)
		{
			float directionX = Mathf.Sign(moveAmount.x);
			rayLength = Mathf.Abs(moveAmount.x) + skinWidth;
			Vector2 rayOrigin = ((directionX == -1) ? raycastOrigins.bottomLeft : raycastOrigins.bottomRight) + Vector2.up * moveAmount.y;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, Vector2.right * directionX, rayLength, collisionMask);

			if (hit)
			{
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				if (slopeAngle != collisions.slopeAngle)
				{
					moveAmount.x = (hit.distance - skinWidth) * directionX;
					collisions.slopeAngle = slopeAngle;
					collisions.slopeNormal = hit.normal;
				}
			}
		}
	}

	/// <summary>
	/// Use of trig and use intended x dir speed, for moveDistance up slope (H)
	/// Then work out climbmoveAmountY (O) with Sin(angle)=O/H
	/// And work out climbmoveAmountX (A) with Cos(angle)=A/H
	/// </summary>
	void ClimbSlope(ref Vector2 moveAmount, float slopeAngle, Vector2 slopeNormal)
	{
		float moveDistance = Mathf.Abs(moveAmount.x);
		float climbmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;

		// Check if player is jumping already before climbing
		if (moveAmount.y <= climbmoveAmountY)
		{
			moveAmount.y = climbmoveAmountY;
			moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
			collisions.below = true;
			collisions.climbingSlope = true;
			collisions.slopeAngle = slopeAngle;
			collisions.slopeNormal = slopeNormal;
		}
	}

	void DescendSlope(ref Vector2 moveAmount)
	{

		RaycastHit2D maxSlopeHitLeft = Physics2D.Raycast(raycastOrigins.bottomLeft, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
		RaycastHit2D maxSlopeHitRight = Physics2D.Raycast(raycastOrigins.bottomRight, Vector2.down, Mathf.Abs(moveAmount.y) + skinWidth, collisionMask);
		if (maxSlopeHitLeft ^ maxSlopeHitRight)
		{
			SlideDownMaxSlope(maxSlopeHitLeft, ref moveAmount);
			SlideDownMaxSlope(maxSlopeHitRight, ref moveAmount);
		}

		if (!collisions.slidingDownMaxSlope)
		{
			float directionX = Mathf.Sign(moveAmount.x);
			Vector2 rayOrigin = (directionX == -1) ? raycastOrigins.bottomRight : raycastOrigins.bottomLeft;
			RaycastHit2D hit = Physics2D.Raycast(rayOrigin, -Vector2.up, Mathf.Infinity, collisionMask);

			if (hit)
			{
				float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
				if (slopeAngle != 0 && slopeAngle <= maxSlopeAngle)
				{
					if (Mathf.Sign(hit.normal.x) == directionX)
					{
						if (hit.distance - skinWidth <= Mathf.Tan(slopeAngle * Mathf.Deg2Rad) * Mathf.Abs(moveAmount.x))
						{
							float moveDistance = Mathf.Abs(moveAmount.x);
							float descendmoveAmountY = Mathf.Sin(slopeAngle * Mathf.Deg2Rad) * moveDistance;
							moveAmount.x = Mathf.Cos(slopeAngle * Mathf.Deg2Rad) * moveDistance * Mathf.Sign(moveAmount.x);
							moveAmount.y -= descendmoveAmountY;

							collisions.slopeAngle = slopeAngle;
							collisions.descendingSlope = true;
							collisions.below = true;
							collisions.slopeNormal = hit.normal;
						}
					}
				}
			}
		}
	}

	void SlideDownMaxSlope(RaycastHit2D hit, ref Vector2 moveAmount)
	{

		if (hit)
		{
			float slopeAngle = Vector2.Angle(hit.normal, Vector2.up);
			if (slopeAngle > maxSlopeAngle)
			{
				moveAmount.x = Mathf.Sign(hit.normal.x) * (Mathf.Abs(moveAmount.y) - hit.distance) / Mathf.Tan(slopeAngle * Mathf.Deg2Rad);

				collisions.slopeAngle = slopeAngle;
				collisions.slidingDownMaxSlope = true;
				collisions.slopeNormal = hit.normal;
			}
		}

	}

	void ResetFallingThroughPlatform()
	{
		collisions.fallingThroughPlatform = false;
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

		public float slopeAngle, slopeAngleOld;
		public Vector2 slopeNormal;
		public Vector2 moveAmountOld;
		public int faceDir;
		public bool fallingThroughPlatform;

		public void Reset()
		{
			above = below = false;
			left = right = false;
			climbingSlope = false;
			descendingSlope = false;
			slidingDownMaxSlope = false;
			slopeNormal = Vector2.zero;

			slopeAngleOld = slopeAngle;
			slopeAngle = 0;
		}
	}

}
