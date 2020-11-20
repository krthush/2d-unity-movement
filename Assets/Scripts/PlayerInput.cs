using UnityEngine;

[RequireComponent(typeof(PlayerVelocity))]
public class PlayerInput : MonoBehaviour
{

	private PlayerVelocity playerVelocity;

	void Start()
	{
		playerVelocity = GetComponent<PlayerVelocity>();
	}

	void Update()
	{
		Vector2 directionalInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
		playerVelocity.SetDirectionalInput(directionalInput);

		if (Input.GetKeyDown(KeyCode.W))
		{
			playerVelocity.OnJumpInputDown();
		}
		if (Input.GetKeyUp(KeyCode.W))
		{
			playerVelocity.OnJumpInputUp();
		}
		if (Input.GetKeyDown(KeyCode.S))
		{
			playerVelocity.OnFallInputDown();
		}
	}
}
