/*
 * Script gets player's intended velocity + displacement after factoring adjustable enviroment modifiers, user input is taken from PlayerInput
 * See for equations/physics: https://en.wikipedia.org/wiki/Equations_of_motion
 * See: http://lolengine.net/blog/2011/12/14/understanding-motion-in-games for Verlet integration vs. Euler
 */

using UnityEngine;

[RequireComponent(typeof(PlayerMovementController))]
public class PlayerVelocity : MonoBehaviour
{

	public float maxJumpHeight = 4;
	public float minJumpHeight = 1;
	public float timeToJumpApex = .4f;
	float accelerationTimeAirborne = .2f;
	float accelerationTimeGrounded = .1f;
	float moveSpeed = 6;

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

	PlayerMovementController playerMovementController;

	Vector2 directionalInput;
	bool wallSliding;
	int wallDirX;

	void Start()
	{
		playerMovementController = GetComponent<PlayerMovementController>();

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
		// moveAmount = 1/2(v+v0)t since the playerMovementController uses Translate which moves from r0
		Vector3 moveAmount = (velocity + oldVelocity) * 0.5f * Time.deltaTime;
		// Move player using movement controller which checks for collisions then applies correct transform (displacement)
		playerMovementController.Move(moveAmount, directionalInput);

		if (playerMovementController.collisions.above || playerMovementController.collisions.below)
		{
			if (playerMovementController.collisions.slidingDownMaxSlope)
			{
				velocity.y += playerMovementController.collisions.slopeNormal.y * -gravity * Time.deltaTime;
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
		float smoothTime = (playerMovementController.collisions.below) ? accelerationTimeGrounded : accelerationTimeAirborne;
		velocity.x = Mathf.SmoothDamp(velocity.x, targetVelocityX, ref velocityXSmoothing, smoothTime);
		velocity.y += gravity * Time.deltaTime;
	}

	void HandleWallSliding()
	{
		wallDirX = (playerMovementController.collisions.left) ? -1 : 1;
		wallSliding = false;
		if ((playerMovementController.collisions.left || playerMovementController.collisions.right) && !playerMovementController.collisions.below && velocity.y < 0)
		{
			wallSliding = true;

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
		if (playerMovementController.collisions.below)
		{
			if (playerMovementController.collisions.slidingDownMaxSlope)
			{
				if (directionalInput.x != -Mathf.Sign(playerMovementController.collisions.slopeNormal.x))
				{ // not jumping against max slope
					velocity.y = maxJumpVelocity * playerMovementController.collisions.slopeNormal.y;
					velocity.x = maxJumpVelocity * playerMovementController.collisions.slopeNormal.x;
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
