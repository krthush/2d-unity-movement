
# 2d Mechanics Playground

Unity project focused on building a solid foundation for 2d movement - feel free to use it!
## Features:

 - [x] No rigidbodies / 2d physics (tight controls)
 - [x] Horizontal and vertical collisions
 - [x] Ascend and descend slopes (constant speed)
 - [x] Variable jump height
 - [x] Wall jumps
 - [x] Wall sliding
 - [x] Wall grab
 - [x] Slide down steep slopes
 - [x] Jump through platforms
 - [x] Moving platforms (moves passengers as well)

## Controls:
A/D: Left/Right Movement
W: Jump
S: Fast Fall

## Project Usage:
If you'd like to use the scripts/project, here's a quick run down of the important scripts/variables.

**Scripts:**
 - [PlayerInput](Assets/Scripts/PlayerInput.cs) script handles controls.
 - [PlayerVelocity](Assets/Scripts/PlayerVelocity.cs) script takes input from the [PlayerInput](Assets/Scripts/PlayerInput.cs) and calculates velocities then outputs displacements.
 - [Movement](Assets/Scripts/Movement.cs) takes input displacements (e.g. from [PlayerVelocity](Assets/Scripts/PlayerVelocity.cs)/[PassengerMover](Assets/Scripts/PassengerMover.cs)) then checks for collisions and moves the given object accordingly.
 -  [MovingPlatform](Assets/Scripts/MovingPlatform.cs) moves a platform, you can also assign the platform to be a [PassengerMover](Assets/Scripts/PassengerMover.cs) then it will move any other objects that have been assigned as a [Passenger](Assets/Scripts/Passenger.cs).
 - [BoxColliderCasts](Assets/Scripts/BoxColliderCasts.cs) is a base class to set up raycasts and boxcast origins for collision detection.

##
This was original made following a Sebastian Lague [video series](https://www.youtube.com/watch?v=MbWK8bCAU2w&list=PLFt_AvWsXl0f0hqURlhyIoAabKPgRsqjz&index=1), but has been heavily adapted to be tighter/smoother. If you'd like to start from scratch or don't understand components of the project I'd suggest using the videos as a guide!
