/*
 * Script gets player's intended velocity + displacement caused by enviroment variables + user input which is taken from PlayerInput
 * See for equations/physics: https://en.wikipedia.org/wiki/Equations_of_motion
 * See: http://lolengine.net/blog/2011/12/14/understanding-motion-in-games for Verlet integration vs. Euler
 */

using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerVelocity : MonoBehaviour
{

	public float maxJumpHeight = 4;
	public float minJumpHeight = 1;
	public float timeToJumpApex = .4f;
	public float accelerationTimeAirborne = .2f;
	public float accelerationTimeGrounded = .1f;
	public float moveSpeed = 6;

	public Vector2 wallJumpClimb;
	public Vector2 wallJumpOff;
	public Vector2 wallLeap;

	public float wallSlideSpeedMax = 3;
	public float wallStickTime = .25f;
	float timeToWallUnstick;

	float gravity;
	float maxJumpVelocity;
	float minJumpVelocity;
	Vector3 velocity;
	Vector3 oldVelocity;
	float velocityXSmoothing;

	PlayerMovement playerMovement;

	Vector2 directionalInput;
	bool wallSliding;
	int wallDirX;

	void Start()
	{
		playerMovement = GetComponent<PlayerMovement>();

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
		// Move player using movement controller which checks for collisions then applies correct transform (displacement)
		playerMovement.Move(displacement, directionalInput);

		bool verticalCollision = playerMovement.collisions.above || playerMovement.collisions.below;

		if (verticalCollision)
		{
			if (playerMovement.collisions.slidingDownMaxSlope)
			{
				velocity.y += playerMovement.collisions.slopeNormal.y * -gravity * Time.deltaTime;
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
		float smoothTime = (playerMovement.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne;
		velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, smoothTime);
		velocity.y += gravity * Time.deltaTime;
	}

	void HandleWallSliding()
	{
		wallDirX = (playerMovement.collisions.left) ? -1 : 1;
		bool horizontalCollision = playerMovement.collisions.left || playerMovement.collisions.right;
		bool falling = !playerMovement.collisions.below && velocity.y < 0;

		if (horizontalCollision && falling)
		{
			wallSliding = true;

			if (directionalInput.x == wallDirX && playerMovement.collisions.wallHit)
            {
				velocity.y = 0;
			} 
			else
            {
				if (velocity.y < -wallSlideSpeedMax)
				{
					velocity.y = -wallSlideSpeedMax;
				}

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

		} else
		{
			wallSliding = false;
		}

	}

	/* Public Functions used by PlayerInput script */

	public void SetDirectionalInput(Vector2 input)
	{
		directionalInput = input;
	}

	public void OnJumpInputDown()
	{
		if (wallSliding)
		{
			if (wallDirX == directionalInput.x)
			{
				velocity.x = -wallDirX * wallJumpClimb.x;
				velocity.y = wallJumpClimb.y;
			}
			else if (directionalInput.x == 0)
			{
				velocity.x = -wallDirX * wallJumpOff.x;
				velocity.y = wallJumpOff.y;
			}
			else
			{
				velocity.x = -wallDirX * wallLeap.x;
				velocity.y = wallLeap.y;
			}
		}
		if (playerMovement.collisions.below)
		{
			if (playerMovement.collisions.slidingDownMaxSlope)
			{
				if (directionalInput.x != -Mathf.Sign(playerMovement.collisions.slopeNormal.x))
				{ 
					// not jumping against max slope
					velocity.y = maxJumpVelocity * playerMovement.collisions.slopeNormal.y;
					velocity.x = maxJumpVelocity * playerMovement.collisions.slopeNormal.x;
				}
			}
			else
			{
				velocity.y = maxJumpVelocity;
			}
		}
	}

	public void OnJumpInputUp()
	{
		if (velocity.y > minJumpVelocity)
		{
			velocity.y = minJumpVelocity;
		}
	}
}
