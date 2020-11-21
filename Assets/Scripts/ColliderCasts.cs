/*
 * Class to setup needed raycasts around a BoxCollider2D player for proper collision detection
 */

using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class ColliderCasts : MonoBehaviour
{

	public LayerMask collisionMask;

	public float skinWidth = .015f;
	public float dstBetweenRays = .25f;

	[HideInInspector] public BoxCollider2D boxCollider;

	[HideInInspector] public int horizontalRayCount;
	[HideInInspector] public int verticalRayCount;

	[HideInInspector] public float horizontalRaySpacing;
	[HideInInspector] public float verticalRaySpacing;

	public RaycastOrigins raycastOrigins;
	public BoxCastOrigins boxCastOrigins;

	public float boundsWidth;
	public float boundsHeight;

	public virtual void Awake()
	{
		boxCollider = GetComponent<BoxCollider2D>();
	}

	public virtual void Start()
	{
		CalculateRaySpacing();
	}

	public void CalculateRaySpacing()
	{
		Bounds bounds = boxCollider.bounds;
		// Skin width for ray detection even when boxCollider is flush against surfaces
		bounds.Expand(skinWidth * -2);

		boundsWidth = bounds.size.x;
		boundsHeight = bounds.size.y;

		horizontalRayCount = Mathf.RoundToInt(boundsHeight / dstBetweenRays);
		verticalRayCount = Mathf.RoundToInt(boundsWidth / dstBetweenRays);

		horizontalRaySpacing = bounds.size.y / (horizontalRayCount - 1);
		verticalRaySpacing = bounds.size.x / (verticalRayCount - 1);
	}

	public void UpdateRaycastOrigins()
	{
		Bounds bounds = boxCollider.bounds;
		// Skin width for ray detection even when boxCollider is flush against surfaces
		bounds.Expand(skinWidth * -2);

		// Match corners of box boxCollider
		raycastOrigins.bottomLeft = new Vector2(bounds.min.x, bounds.min.y);
		raycastOrigins.bottomRight = new Vector2(bounds.max.x, bounds.min.y);
		raycastOrigins.topLeft = new Vector2(bounds.min.x, bounds.max.y);
		raycastOrigins.topRight = new Vector2(bounds.max.x, bounds.max.y);
	}

	public void UpdateBoxcastOrigins()
	{
		Bounds bounds = boxCollider.bounds;
		// Skin width for ray detection even when boxCollider is flush against surfaces
		bounds.Expand(skinWidth * -2);

		boxCastOrigins.bottomCenter = new Vector2(bounds.center.x, bounds.min.y);
		boxCastOrigins.topCenter = new Vector2(bounds.center.x, bounds.max.y);
		boxCastOrigins.leftCenter = new Vector2(bounds.min.x, bounds.center.y);
		boxCastOrigins.rightCenter = new Vector2(bounds.max.x, bounds.center.y);
	}

	public struct RaycastOrigins
	{
		public Vector2 bottomLeft, bottomRight;
		public Vector2 topLeft, topRight;
	}

	public struct BoxCastOrigins
	{
		public Vector2 bottomCenter, topCenter, leftCenter, rightCenter;
	}
}
