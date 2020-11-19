/*
 * Script gets player's intended velocity + displacement caused by enviroment variables + user input which is taken from PlayerInput
 * See for equations/physics: https://en.wikipedia.org/wiki/Equations_of_motion
 * See: http://lolengine.net/blog/2011/12/14/understanding-motion-in-games for Verlet integration vs. Euler
 */

using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerVelocity : MonoBehaviour
{

	[SerializeField] private float maxJumpHeight = 4;
	[SerializeField] private float minJumpHeight = 1;
	[SerializeField] private float timeToJumpApex = .4f;
	[SerializeField] private float accelerationTimeAirborne = .2f;
	[SerializeField] private float accelerationTimeGrounded = .1f;
	[SerializeField] private float moveSpeed = 6;
	[SerializeField] private float forceFallSpeed = 20;

	[SerializeField] private Vector2 wallJumpClimb;
	[SerializeField] private Vector2 wallJumpOff;
	[SerializeField] private Vector2 wallLeap;

	[SerializeField] private float wallSlideSpeedMax = 3;
	[SerializeField] private float wallStickTime = .25f;

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
		bool falling = !playerMovement.collisionDirection.below && velocity.y < 0;

		if (horizontalCollision && falling && !playerMovement.forceFall && playerMovement.collisionAngle.onWall)
		{
			wallSliding = true;

			if (directionalInput.x == wallDirX)
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
		if (playerMovement.collisionDirection.below)
		{
			if (playerMovement.slidingDownMaxSlope)
			{
				if (directionalInput.x != -Mathf.Sign(playerMovement.collisionAngle.slopeNormal.x))
				{ 
					// not jumping against max slope
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

	public void OnJumpInputUp()
	{
		if (velocity.y > minJumpVelocity)
		{
			velocity.y = minJumpVelocity;
		}
	}

	public void OnFallInputDown()
    {
		velocity.y = -forceFallSpeed;
		playerMovement.forceFall = true;
	}
}
