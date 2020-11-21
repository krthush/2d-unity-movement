# 2d Unity Movement

Unity project focused on building a solid foundation for 2d movement - feel free to use it!

## Features:

✅ No rigidbodies / No 2d physics (allows for tight controls) <br>✅ Horizontal and vertical collisions <br>✅ Ascend and descend slopes (constant speed) <br>✅ Variable jump height <br>✅ Wall jumps <br>✅ Wall sliding <br>✅ Wall grab <br>✅ Slide down steep slopes <br>✅ Jump through platforms <br>✅ Moving platforms (moves passengers as well)

## Controls:

**Movement:**<br>
A/D: Left/Right movement <br>W: Jump

**In air:**<br>
S: Fast Fall

**On wall (facing left):**<br>
W: Wall jump <br>A+W: Wall jump climb<br>D+W: Wall leap off <br> *Opposite controls for facing right*

## Project Usage:

If you'd like to use the scripts/project, here's a quick run down of the important scripts/variables.

**Scripts:**

 - [PlayerInput](Assets/Scripts/PlayerInput.cs) script handles controls.
 - [PlayerVelocity](Assets/Scripts/PlayerVelocity.cs) script takes input from the [PlayerInput](Assets/Scripts/PlayerInput.cs) and calculates velocities then outputs displacements.
 - [Movement](Assets/Scripts/Movement.cs) takes input displacements (e.g. from [PlayerVelocity](Assets/Scripts/PlayerVelocity.cs)/[PassengerMover](Assets/Scripts/PassengerMover.cs)) then checks for collisions and moves the given object accordingly.
 -  [MovingPlatform](Assets/Scripts/MovingPlatform.cs) moves a platform, you can also assign the platform to be a [PassengerMover](Assets/Scripts/PassengerMover.cs) then it will move any other objects that have been assigned as a [Passenger](Assets/Scripts/Passenger.cs).
 - [BoxColliderCasts](Assets/Scripts/BoxColliderCasts.cs) is a base class to set up raycasts and boxcast origins for collision detection.

**Adjustable Variables:**

- [PlayerVelocity](Assets/Scripts/PlayerVelocity.cs) allows you to adjust:
	- Move speed (Left/Right movement)
	- Jump heights and time to reach jump height (calculates jump speed / gravity)
	- Acceleration time on ground and in air (calculates drag on ground and in air)
	- Fast fall speed
	- Wall jump, jump climb and leap off velocities
	- Wall slide speed
	- Wall stick time
- [Movement](Assets/Scripts/Movement.cs) allows you to adjust:
	- Collision mask - what layers the object should collide with
	- Skin width - small tolerance given to collision detection
	- Distance between rays - smaller givens better detection but more computation
- [MovingPlatform](Assets/Scripts/MovingPlatform.cs) allows you to adjust:
	- Waypoints to travel through
	- Platform's travel speed
	- Cycle through waypoints or not
	- Wait time at waypoints
	- Easing
- [PassengerMover](Assets/Scripts/PassengerMover.cs) allows you to adjust:
	- Passenger mask - what layer objects should be moved
	- Collision mask, Skin width and Distance between rays

**Layers/Tags:**

- Through tag should be applied to platforms that can be jumped through
- Player/Enemies/Allies layers can be used to allow collision or not as you wish
- If Player/Enemy/Ally tag used, then object requires [Movement](Assets/Scripts/Movement.cs) to be able to detect collision and move correctly.

##

This was original made following a Sebastian Lague [video series](https://www.youtube.com/watch?v=MbWK8bCAU2w&list=PLFt_AvWsXl0f0hqURlhyIoAabKPgRsqjz&index=1), but has been heavily adapted to be tighter/smoother. If you'd like to start from scratch or don't understand components of the project I'd suggest using the videos as a guide! <br>
**Please feel free to post any issues or submit any pull requests and I'll get on em asap :)**
