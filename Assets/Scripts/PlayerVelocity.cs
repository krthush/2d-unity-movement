/*
 * Script gets player's intended velocity & displacement (caused by enviroment variables + user input which is taken from PlayerInput)
 * See for equations/physics: https://en.wikipedia.org/wiki/Equations_of_motion
 * See: http://lolengine.net/blog/2011/12/14/understanding-motion-in-games for Verlet integration vs. Euler
 */

using UnityEngine;

[RequireComponent(typeof(ObjectDisplacement))]
public class PlayerVelocity : MonoBehaviour
{

	[SerializeField] private float maxJumpHeight = 4;
	[SerializeField] private float minJumpHeight = 1;
	[SerializeField] private float timeToJumpApex = .4f;
	[SerializeField] private float accelerationTimeAirborne = .2f;
	[SerializeField] private float accelerationTimeGrounded = .1f;
	[SerializeField] private float moveSpeed = 6;
	[SerializeField] private float forceFallSpeed = 20;

	[SerializeField] private Vector2 wallJump;
	[SerializeField] private Vector2 wallJumpClimb;
	[SerializeField] private Vector2 wallLeap;

	[SerializeField] private float wallSlideSpeedMax = 3;
	[SerializeField] private float wallStickTime = .25f;

	private float timeToWallUnstick;
	private float gravity;
	private float maxJumpVelocity;
	private float minJumpVelocity;
	private Vector3 velocity;
	private Vector3 oldVelocity;
	private float velocityXSmoothing;

	private ObjectDisplacement playerMovement;

	private Vector2 directionalInput;
	private bool wallContact;
	private int wallDirX;

	void Start()
	{
		playerMovement = GetComponent<ObjectDisplacement>();

		// see suvat calculations; s = ut + 1/2at^2, v^2 = u^2 + 2at, where u=0, scalar looking at only y dir
		gravity = -(2 * maxJumpHeight) / Mathf.Pow(timeToJumpApex, 2);
		maxJumpVelocity = Mathf.Abs(gravity) * timeToJumpApex;
		minJumpVelocity = Mathf.Sqrt(2 * Mathf.Abs(gravity) * minJumpHeight);
	}

	void Update()
	{
		CalculateVelocity();
		HandleWallSliding();

		// r = r0 + 1/2(v+v0)t, note Vector version used here
		// displacement = 1/2(v+v0)t since the playerMovementController uses Translate which moves from r0
		Vector3 displacement = (velocity + oldVelocity) * 0.5f * Time.deltaTime;
		// Move player using movement controller which checks for collisions then applies correct transform (displacement) translation
		playerMovement.Move(displacement, directionalInput);

		bool verticalCollision = playerMovement.collisionDirection.above || playerMovement.collisionDirection.below;

		if (verticalCollision)
		{
			if (playerMovement.slidingDownMaxSlope)
			{
				velocity.y += playerMovement.collisionAngle.slopeNormal.y * -gravity * Time.deltaTime;
			}
			else
			{
				velocity.y = 0;
			}
		}
	}

	void CalculateVelocity()
	{
		// suvat; s = ut, note a=0
		float targetVelocityX = directionalInput.x * moveSpeed;
		oldVelocity = velocity;
		// ms when player is on the ground faster vs. in air
		float smoothTime = (playerMovement.collisionDirection.below) ? accelerationTimeGrounded : accelerationTimeAirborne;
		velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, smoothTime);
        velocity.y += gravity * Time.deltaTime;
    }

	void HandleWallSliding()
	{
		wallDirX = (playerMovement.collisionDirection.left) ? -1 : 1;
		bool horizontalCollision = playerMovement.collisionDirection.left || playerMovement.collisionDirection.right;

		if (horizontalCollision && !playerMovement.collisionDirection.below && !playerMovement.forceFall && playerMovement.collisionAngle.onWall)
		{
			wallContact = true;

			// Check if falling down - only wall slide then
			if (velocity.y < 0)
			{
				// Grab wall if input facing wall
				if (directionalInput.x == wallDirX)
				{
					velocity.y = 0;
				}
				else
				{
					// Only slow down if falling faster than slide speed
					if (velocity.y < -wallSlideSpeedMax)
					{
						velocity.y = -wallSlideSpeedMax;
					}

					// Stick to wall until timeToWallUnstick has counted down to 0 from wallStickTime
					if (timeToWallUnstick > 0)
					{
						velocityXSmoothing = 0;
						velocity.x = 0;

						if (directionalInput.x != wallDirX && directionalInput.x != 0)
						{
							timeToWallUnstick -= Time.deltaTime;
						}
						else
						{
							timeToWallUnstick = wallStickTime;
						}
					}
					else
					{
						timeToWallUnstick = wallStickTime;
					}
				}
			}

		} else
		{
			wallContact = false;
		}

	}

	/* Public Functions used by PlayerInput script */

	/// <summary>
	/// Handle horizontal movement input
	/// </summary>
	public void SetDirectionalInput(Vector2 input)
	{
		directionalInput = input;
	}

	/// <summary>
	/// Handle jumps
	/// </summary>
	public void OnJumpInputDown()
	{
		if (wallContact)
		{
			// Standard wall jump
			if (directionalInput.x == 0)
			{
				velocity.x = -wallDirX * wallJump.x;
				velocity.y = wallJump.y;
			}
			// Climb up if input is facing wall
			else if (wallDirX == directionalInput.x)
			{
				velocity.x = -wallDirX * wallJumpClimb.x;
				velocity.y = wallJumpClimb.y;
			}
			// Leap wall if input facing away from wall
			else
			{
				velocity.x = -wallDirX * wallLeap.x;
				velocity.y = wallLeap.y;
			}
		}
		if (playerMovement.collisionDirection.below)
		{
			if (playerMovement.slidingDownMaxSlope)
			{
				// Jumping away from max slope dir
				if (directionalInput.x != -Mathf.Sign(playerMovement.collisionAngle.slopeNormal.x))
				{ 
					velocity.y = maxJumpVelocity * playerMovement.collisionAngle.slopeNormal.y;
					velocity.x = maxJumpVelocity * playerMovement.collisionAngle.slopeNormal.x;
				}
			}
			else
			{
				velocity.y = maxJumpVelocity;
			}
		}
	}

	/// <summary>
	/// Handle not fully commited jumps - allow for mini jumps
	/// </summary>
	public void OnJumpInputUp()
	{
		if (velocity.y > minJumpVelocity)
		{
			velocity.y = minJumpVelocity;
		}
	}

	/// <summary>
	/// Handle down direction - force fall
	/// </summary>
	public void OnFallInputDown()
    {
		if (!playerMovement.collisionDirection.below)
		{
			velocity.y = -forceFallSpeed;
			playerMovement.forceFall = true;
		}
	}
}
