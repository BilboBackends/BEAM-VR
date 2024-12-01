using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// This class handles the creation of windows on walls by drawing a rectangle and instantiating wall segments.
/// </summary>
public class CreateWindows : MonoBehaviour
{
    /// <summary>
    /// The transform of the right-hand anchor, used as the starting point for raycasting.
    /// </summary>
    public Transform rightHandAnchor;

    /// <summary>
    /// The line renderer component used to visualize the drawn rectangle.
    /// </summary>
    public LineRenderer lineRenderer;

    /// <summary>
    /// Stores the face your drawing on
    /// </summary>
    public string selectedFace;

    /// <summary>
    /// The layer mask used to filter wall objects for raycasting.
    /// </summary>
    public LayerMask wallLayerMask;

    /// <summary>
    /// The prefab for the wall segments that will be instantiated to create the window.
    /// </summary>
    public GameObject wallSegmentPrefab;

    /// <summary>
    /// The currently selected wall object.
    /// </summary>
    private GameObject selectedWall;

    /// <summary>
    /// The starting and ending points of the drawn rectangle.
    /// </summary>
    private Vector3 startPoint, endPoint;

    /// <summary>
    /// Flag indicating if the user is currently selecting a rectangle.
    /// </summary>
    private bool isSelecting = false;

    /// <summary>
    /// The normal vector of the hit point on the wall.
    /// </summary>
    private Vector3 hitNormal;

    /// <summary>
    /// The local right direction of the wall plane.
    /// </summary>
    private Vector3 localRight;

    /// <summary>
    /// The local up direction of the wall plane.
    /// </summary>
    private Vector3 localUp;

    /// <summary>
    /// The list of instantiated wall segments.
    /// </summary>
    private List<GameObject> instantiatedSegments = new List<GameObject>();

    /// <summary>
    /// Everything happens within one frame
    /// </summary>
    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// Handles the input for drawing a rectangle on a wall.
    /// This method is called every frame to check for user input.
    /// It uses raycasting to detect when the user is pointing at a wall and handles the following actions:
    /// - When the primary index trigger is pressed, it starts drawing a rectangle on the wall.
    /// - While the primary index trigger is held, it updates the end point of the rectangle based on the controller's position.
    /// - When the primary index trigger is released, it finalizes the rectangle and creates a window by calling the CreateWindow method.
    /// </summary>
void HandleInput()
{
    if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
    {
        // Perform the raycast
        if (Physics.Raycast(rightHandAnchor.position, rightHandAnchor.forward, out RaycastHit hit, Mathf.Infinity, wallLayerMask))
        {
            // Check if the raycast hit a plane used as a reference
            if (hit.collider != null && (hit.collider.CompareTag("XY") || hit.collider.CompareTag("XZ") || hit.collider.CompareTag("YZ")))
            {
                selectedFace = hit.collider.tag;
                Debug.Log("Selected Face: " + selectedFace);

                // Set the selectedWall to the rectangular prism instead of the plane
                // Assuming the parent of the plane is the prism itself
                selectedWall = hit.collider.transform.parent.gameObject; 

                // Use the hit point on the plane to start drawing on the prism
                // Adjust startPoint calculation if necessary
                startPoint = hit.point; 
                isSelecting = true;
                lineRenderer.positionCount = 5;
                UpdateLineRendererWithRectangle(startPoint, startPoint, hit.normal, selectedWall.transform);
                lineRenderer.enabled = true;
            }
        }
    }
    else if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) && isSelecting)
    {
        // Continue drawing on the rectangular prism based on the initial selection
        if (Physics.Raycast(rightHandAnchor.position, rightHandAnchor.forward, out RaycastHit hit, Mathf.Infinity, wallLayerMask))
        {
            endPoint = hit.point;
            UpdateLineRendererWithRectangle(startPoint, endPoint, hit.normal, selectedWall.transform);
        }
    }
    else if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) && isSelecting)
    {
        isSelecting = false;
        CreateWindow(); // Finalize the drawing on the prism based on the initially selected face
        lineRenderer.enabled = false;
    }
}


    /// <summary>
    /// Updates the LineRenderer to display a rectangle on the wall based on the start and end points.
    /// This method calculates the corners of the rectangle in the wall's local coordinate system and transforms them back to world space.
    /// It adjusts the orientation of the rectangle to match the wall's rotation and ensures proper alignment.
    /// The LineRenderer is then updated with the calculated corners to visualize the rectangle on the wall.
    /// </summary>
    /// <param name="start">The starting point of the rectangle.</param>
    /// <param name="end">The ending point of the rectangle.</param>
    /// <param name="hitNormal">The normal vector of the hit point on the wall.</param>
    /// <param name="hitTransform">The transform of the hit object.</param>
void UpdateLineRendererWithRectangle(Vector3 start, Vector3 end, Vector3 hitNormal, Transform hitTransform)
{
    // Get the right and up directions of the hit object's transform
    Vector3 referenceRight = hitTransform.right;
    Vector3 referenceUp = hitTransform.up;

    // Calculate vectors that represent the plane's local coordinate system, adjusted for hit object's rotation
    // localRight is perpendicular to the hitNormal and referenceUp
    Vector3 localRight = Vector3.Cross(hitNormal, referenceUp).normalized;

    // Ensure localRight aligns with the hit object's right direction
    // If the dot product is negative, it means localRight points in the opposite direction of referenceRight
    // In that case, we flip localRight to make it align with referenceRight
    if (Vector3.Dot(localRight, referenceRight) < 0)
    {
        localRight = -localRight;
    }

    // localUp is perpendicular to localRight and hitNormal
    Vector3 localUp = Vector3.Cross(localRight, hitNormal).normalized;

    // Ensure localUp aligns with the hit object's up direction
    // If the dot product is negative, it means localUp points in the opposite direction of referenceUp
    // In that case, we flip localUp to make it align with referenceUp
    if (Vector3.Dot(localUp, referenceUp) < 0)
    {
        localUp = -localUp;
    }

    // Store the hitNormal, localRight, and localUp for later use
    this.hitNormal = hitNormal;
    this.localRight = localRight;
    this.localUp = localUp;

    // Project the start and end points onto the plane defined by the hitNormal
    // This ensures that the end point is on the same plane as the start point
    Plane plane = new Plane(hitNormal, start);
    plane.Raycast(new Ray(end, -hitNormal), out float enter);
    end = end - (-hitNormal) * enter;

    // Convert start and end points to the plane's local coordinate system
    // The start point becomes the origin (0, 0, 0)
    Vector3 rectStart = new Vector3(0, 0, 0);
    // The end point is converted to the local coordinate system using the dot product
    // This gives us the relative position of the end point in terms of localRight and localUp
    Vector3 rectEnd = new Vector3(Vector3.Dot((end - start), localRight), Vector3.Dot((end - start), localUp), 0);

    // Calculate the rectangle's corners in this local space
    Vector3[] corners = new Vector3[5];
    corners[0] = rectStart; 
    corners[1] = new Vector3(rectEnd.x, rectStart.y, 0); 
    corners[2] = rectEnd; 
    corners[3] = new Vector3(rectStart.x, rectEnd.y, 0); 
    corners[4] = corners[0]; // Close the loop by connecting back to the first corner

    // Transform the corners back to world space
    for (int i = 0; i < corners.Length; i++)
    {
        // Each corner is transformed by adding the start point and multiplying by localRight and localUp
        // This converts the local coordinates of each corner to world space coordinates
        corners[i] = start + localRight * corners[i].x + localUp * corners[i].y;
    }

    // Update the LineRenderer with the world-space corners
    // This sets the positions of the LineRenderer to match the calculated corners of the rectangle
    lineRenderer.SetPositions(corners);
}

    /// <summary>
    /// Creates a window by instantiating wall segments around the selected rectangle.
    /// This method is called when the user finalizes the rectangle drawing.
    /// It performs the following steps:
    /// 1. Saves the original position and rotation of the selected wall.
    /// 2. Moves the selected wall to the origin and aligns it with the world axes.
    /// 3. Calculates the window center and size based on the start and end points of the rectangle.
    /// 4. Disables the selected wall to create the appearance of a window hole.
    /// 5. Instantiates wall segments around the window using the InstantiateWallSegments method.
    /// 6. Groups the instantiated segments under a parent object using the GroupSegmentsUnderParent method.
    /// </summary>
void CreateWindow()
{
    if (!selectedWall) return;

    Debug.Log("Selected Face: " + selectedFace);

    // Save the original position and rotation of the selected wall
    Vector3 originalPosition = selectedWall.transform.position;
    Quaternion originalRotation = selectedWall.transform.rotation;

    // Transform the start and end points to the selected wall's local space
    Vector3 localStartPoint = selectedWall.transform.InverseTransformPoint(startPoint);
    Vector3 localEndPoint = selectedWall.transform.InverseTransformPoint(endPoint);

    // Move the selected wall to the origin and align it with the x and y axis
    selectedWall.transform.position = Vector3.zero;
    selectedWall.transform.rotation = Quaternion.identity;

    // Transform the local start and end points to the new object's location
    Vector3 newStartPoint = selectedWall.transform.TransformPoint(localStartPoint);
    Vector3 newEndPoint = selectedWall.transform.TransformPoint(localEndPoint);

    selectedWall.SetActive(false); // Disable the selected wall to simulate the window hole

    Bounds bounds = selectedWall.GetComponent<MeshRenderer>().bounds;

    Vector3 windowCenter;
    Vector3 windowSize;

    if (selectedFace == "XZ")
    {
        // Calculate the window center and size for the XZ face
        windowCenter = (newStartPoint + newEndPoint) / 2;
        windowCenter.y = bounds.center.y;
        windowSize = new Vector3(Mathf.Abs(newEndPoint.x - newStartPoint.x), bounds.size.y, Mathf.Abs(newEndPoint.z - newStartPoint.z));
    }
    else if (selectedFace == "XY")
    {
        // Calculate the window center and size for the XY face
        windowCenter = (newStartPoint + newEndPoint) / 2;
        windowCenter.z = bounds.center.z;
        windowSize = new Vector3(Mathf.Abs(newEndPoint.x - newStartPoint.x), Mathf.Abs(newEndPoint.y - newStartPoint.y), bounds.size.z);
    }
    else // Default to YZ face
    {
        // Calculate the window center and size for the YZ face
        windowCenter = (newStartPoint + newEndPoint) / 2;
        windowCenter.x = bounds.center.x;
        windowSize = new Vector3(bounds.size.x, Mathf.Abs(newEndPoint.y - newStartPoint.y), Mathf.Abs(newEndPoint.z - newStartPoint.z));
    }

    InstantiateWallSegments(windowCenter, windowSize, bounds, originalPosition, originalRotation, selectedFace);
    GroupSegmentsUnderParent();
}

    /// <summary>
    /// Instantiates the wall segments around the window based on the calculated window center and size.
    /// This method calculates the positions and sizes of the eight wall segments (top, bottom, left, right, and four corners) that form the window frame.
    /// It then instantiates each segment using the InstantiateWallSegment method, passing the calculated position, size, and the original wall's position and rotation.
    /// The instantiated segments are placed around the window to create the appearance of a window frame.
    /// </summary>
    /// <param name="windowCenter">The center of the window.</param>
    /// <param name="windowSize">The size of the window.</param>
    /// <param name="bounds">The bounds of the original wall object.</param>
    /// <param name="originalPosition">The original position of the wall object.</param>
    /// <param name="originalRotation">The original rotation of the wall object.</param>
void InstantiateWallSegments(Vector3 windowCenter, Vector3 windowSize, Bounds bounds, Vector3 originalPosition, Quaternion originalRotation, string selectedFace)
{
    float wallDepth;
    Vector3 topSegmentCenter, bottomSegmentCenter, leftSegmentCenter, rightSegmentCenter;
    Vector3 topSegmentSize, bottomSegmentSize, leftSegmentSize, rightSegmentSize;
    Vector3 topLeftSegmentCenter, topRightSegmentCenter, bottomLeftSegmentCenter, bottomRightSegmentCenter;
    Vector3 topLeftSegmentSize, topRightSegmentSize, bottomLeftSegmentSize, bottomRightSegmentSize;

    if (selectedFace == "XZ")
    {
        wallDepth = bounds.size.y;

        // Top segment
        float topDepth = Mathf.Abs(bounds.max.z - windowCenter.z - windowSize.z / 2);
        topSegmentCenter = new Vector3(windowCenter.x, windowCenter.y, bounds.max.z - topDepth / 2);
        topSegmentSize = new Vector3(windowSize.x, wallDepth, topDepth);

        // Bottom segment
        float bottomDepth = Mathf.Abs(windowCenter.z - bounds.min.z - windowSize.z / 2);
        bottomSegmentCenter = new Vector3(windowCenter.x, windowCenter.y, bounds.min.z + bottomDepth / 2);
        bottomSegmentSize = new Vector3(windowSize.x, wallDepth, bottomDepth);

        // Left segment
        float leftWidth = Mathf.Abs(windowCenter.x - bounds.min.x - windowSize.x / 2);
        leftSegmentCenter = new Vector3(bounds.min.x + leftWidth / 2, windowCenter.y, windowCenter.z);
        leftSegmentSize = new Vector3(leftWidth, wallDepth, windowSize.z);

        // Right segment
        float rightWidth = Mathf.Abs(bounds.max.x - windowCenter.x - windowSize.x / 2);
        rightSegmentCenter = new Vector3(bounds.max.x - rightWidth / 2, windowCenter.y, windowCenter.z);
        rightSegmentSize = new Vector3(rightWidth, wallDepth, windowSize.z);

        // Top-left corner
        float topLeftWidth = Mathf.Abs(windowCenter.x - bounds.min.x - windowSize.x / 2);
        float topLeftDepth = Mathf.Abs(bounds.max.z - windowCenter.z - windowSize.z / 2);
        topLeftSegmentCenter = new Vector3(bounds.min.x + topLeftWidth / 2, windowCenter.y, bounds.max.z - topLeftDepth / 2);
        topLeftSegmentSize = new Vector3(topLeftWidth, wallDepth, topLeftDepth);

        // Top-right corner
        float topRightWidth = Mathf.Abs(bounds.max.x - windowCenter.x - windowSize.x / 2);
        float topRightDepth = Mathf.Abs(bounds.max.z - windowCenter.z - windowSize.z / 2);
        topRightSegmentCenter = new Vector3(bounds.max.x - topRightWidth / 2, windowCenter.y, bounds.max.z - topRightDepth / 2);
        topRightSegmentSize = new Vector3(topRightWidth, wallDepth, topRightDepth);

        // Bottom-left corner
        float bottomLeftWidth = Mathf.Abs(windowCenter.x - bounds.min.x - windowSize.x / 2);
        float bottomLeftDepth = Mathf.Abs(windowCenter.z - bounds.min.z - windowSize.z / 2);
        bottomLeftSegmentCenter = new Vector3(bounds.min.x + bottomLeftWidth / 2, windowCenter.y, bounds.min.z + bottomLeftDepth / 2);
        bottomLeftSegmentSize = new Vector3(bottomLeftWidth, wallDepth, bottomLeftDepth);

        // Bottom-right corner
        float bottomRightWidth = Mathf.Abs(bounds.max.x - windowCenter.x - windowSize.x / 2);
        float bottomRightDepth = Mathf.Abs(windowCenter.z - bounds.min.z - windowSize.z / 2);
        bottomRightSegmentCenter = new Vector3(bounds.max.x - bottomRightWidth / 2, windowCenter.y, bounds.min.z + bottomRightDepth / 2);
        bottomRightSegmentSize = new Vector3(bottomRightWidth, wallDepth, bottomRightDepth);
    }
    else if (selectedFace == "XY")
    {
        wallDepth = bounds.size.z;

        // Top segment
        float topHeight = Mathf.Abs(bounds.max.y - windowCenter.y - windowSize.y / 2);
        topSegmentCenter = new Vector3(windowCenter.x, bounds.max.y - topHeight / 2, windowCenter.z);
        topSegmentSize = new Vector3(windowSize.x, topHeight, wallDepth);

        // Bottom segment
        float bottomHeight = Mathf.Abs(windowCenter.y - bounds.min.y - windowSize.y / 2);
        bottomSegmentCenter = new Vector3(windowCenter.x, bounds.min.y + bottomHeight / 2, windowCenter.z);
        bottomSegmentSize = new Vector3(windowSize.x, bottomHeight, wallDepth);

        // Left segment
        float leftWidth = Mathf.Abs(windowCenter.x - bounds.min.x - windowSize.x / 2);
        leftSegmentCenter = new Vector3(bounds.min.x + leftWidth / 2, windowCenter.y, windowCenter.z);
        leftSegmentSize = new Vector3(leftWidth, windowSize.y, wallDepth);

        // Right segment
        float rightWidth = Mathf.Abs(bounds.max.x - windowCenter.x - windowSize.x / 2);
        rightSegmentCenter = new Vector3(bounds.max.x - rightWidth / 2, windowCenter.y, windowCenter.z);
        rightSegmentSize = new Vector3(rightWidth, windowSize.y, wallDepth);

        // Top-left corner
        float topLeftWidth = Mathf.Abs(windowCenter.x - bounds.min.x - windowSize.x / 2);
        float topLeftHeight = Mathf.Abs(bounds.max.y - windowCenter.y - windowSize.y / 2);
        topLeftSegmentCenter = new Vector3(bounds.min.x + topLeftWidth / 2, bounds.max.y - topLeftHeight / 2, windowCenter.z);
        topLeftSegmentSize = new Vector3(topLeftWidth, topLeftHeight, wallDepth);

        // Top-right corner
        float topRightWidth = Mathf.Abs(bounds.max.x - windowCenter.x - windowSize.x / 2);
        float topRightHeight = Mathf.Abs(bounds.max.y - windowCenter.y - windowSize.y / 2);
        topRightSegmentCenter = new Vector3(bounds.max.x - topRightWidth / 2, bounds.max.y - topRightHeight / 2, windowCenter.z);
        topRightSegmentSize = new Vector3(topRightWidth, topRightHeight, wallDepth);

        // Bottom-left corner
        float bottomLeftWidth = Mathf.Abs(windowCenter.x - bounds.min.x - windowSize.x / 2);
        float bottomLeftHeight = Mathf.Abs(windowCenter.y - bounds.min.y - windowSize.y / 2);
        bottomLeftSegmentCenter = new Vector3(bounds.min.x + bottomLeftWidth / 2, bounds.min.y + bottomLeftHeight / 2, windowCenter.z);
        bottomLeftSegmentSize = new Vector3(bottomLeftWidth, bottomLeftHeight, wallDepth);

        // Bottom-right corner
        float bottomRightWidth = Mathf.Abs(bounds.max.x - windowCenter.x - windowSize.x / 2);
        float bottomRightHeight = Mathf.Abs(windowCenter.y - bounds.min.y - windowSize.y / 2);
        bottomRightSegmentCenter = new Vector3(bounds.max.x - bottomRightWidth / 2, bounds.min.y + bottomRightHeight / 2, windowCenter.z);
        bottomRightSegmentSize = new Vector3(bottomRightWidth, bottomRightHeight, wallDepth);
    }
    else // Default to YZ face
{
    wallDepth = bounds.size.x;

    // Top segment
    float topDepth = Mathf.Abs(bounds.max.z - windowCenter.z - windowSize.z / 2);
    topSegmentCenter = new Vector3(windowCenter.x, windowCenter.y, bounds.max.z - topDepth / 2);
    topSegmentSize = new Vector3(wallDepth, windowSize.y, topDepth);

    // Bottom segment
    float bottomDepth = Mathf.Abs(windowCenter.z - bounds.min.z - windowSize.z / 2);
    bottomSegmentCenter = new Vector3(windowCenter.x, windowCenter.y, bounds.min.z + bottomDepth / 2);
    bottomSegmentSize = new Vector3(wallDepth, windowSize.y, bottomDepth);

    // Left segment
    float leftHeight = Mathf.Abs(windowCenter.y - bounds.min.y - windowSize.y / 2);
    leftSegmentCenter = new Vector3(windowCenter.x, bounds.min.y + leftHeight / 2, windowCenter.z);
    leftSegmentSize = new Vector3(wallDepth, leftHeight, windowSize.z);

    // Right segment
    float rightHeight = Mathf.Abs(bounds.max.y - windowCenter.y - windowSize.y / 2);
    rightSegmentCenter = new Vector3(windowCenter.x, bounds.max.y - rightHeight / 2, windowCenter.z);
    rightSegmentSize = new Vector3(wallDepth, rightHeight, windowSize.z);

    // Top-left corner
    float topLeftHeight = Mathf.Abs(bounds.max.y - windowCenter.y - windowSize.y / 2);
    float topLeftDepth = Mathf.Abs(bounds.max.z - windowCenter.z - windowSize.z / 2);
    topLeftSegmentCenter = new Vector3(windowCenter.x, bounds.max.y - topLeftHeight / 2, bounds.max.z - topLeftDepth / 2);
    topLeftSegmentSize = new Vector3(wallDepth, topLeftHeight, topLeftDepth);

    // Top-right corner
    float topRightHeight = Mathf.Abs(bounds.max.y - windowCenter.y - windowSize.y / 2);
    float topRightDepth = Mathf.Abs(windowCenter.z - bounds.min.z - windowSize.z / 2);
    topRightSegmentCenter = new Vector3(windowCenter.x, bounds.max.y - topRightHeight / 2, bounds.min.z + topRightDepth / 2);
    topRightSegmentSize = new Vector3(wallDepth, topRightHeight, topRightDepth);

    // Bottom-left corner
    float bottomLeftHeight = Mathf.Abs(windowCenter.y - bounds.min.y - windowSize.y / 2);
    float bottomLeftDepth = Mathf.Abs(bounds.max.z - windowCenter.z - windowSize.z / 2);
    bottomLeftSegmentCenter = new Vector3(windowCenter.x, bounds.min.y + bottomLeftHeight / 2, bounds.max.z - bottomLeftDepth / 2);
    bottomLeftSegmentSize = new Vector3(wallDepth, bottomLeftHeight, bottomLeftDepth);

    // Bottom-right corner
    float bottomRightHeight = Mathf.Abs(windowCenter.y - bounds.min.y - windowSize.y / 2);
    float bottomRightDepth = Mathf.Abs(windowCenter.z - bounds.min.z - windowSize.z / 2);
    bottomRightSegmentCenter = new Vector3(windowCenter.x, bounds.min.y + bottomRightHeight / 2, bounds.min.z + bottomRightDepth / 2);
    bottomRightSegmentSize = new Vector3(wallDepth, bottomRightHeight, bottomRightDepth);
}

    // Instantiate the wall segments
    InstantiateWallSegment(topSegmentCenter, topSegmentSize, originalPosition, originalRotation);
    InstantiateWallSegment(bottomSegmentCenter, bottomSegmentSize, originalPosition, originalRotation);
    InstantiateWallSegment(leftSegmentCenter, leftSegmentSize, originalPosition, originalRotation);
    InstantiateWallSegment(rightSegmentCenter, rightSegmentSize, originalPosition, originalRotation);
    InstantiateWallSegment(topLeftSegmentCenter, topLeftSegmentSize, originalPosition, originalRotation);
    InstantiateWallSegment(topRightSegmentCenter, topRightSegmentSize, originalPosition, originalRotation);
    InstantiateWallSegment(bottomLeftSegmentCenter, bottomLeftSegmentSize, originalPosition, originalRotation);
    InstantiateWallSegment(bottomRightSegmentCenter, bottomRightSegmentSize, originalPosition, originalRotation);
}

    /// <summary>
    /// Instantiates a single wall segment at the specified position and size.
    /// This method is called by the InstantiateWallSegments method to create each individual wall segment.
    /// It instantiates the wall segment prefab at the given position and rotation, sets its scale to match the desired size, and parents it to the selected wall's parent.
    /// The instantiated segment is then added to the list of instantiated segments for further processing.
    /// </summary>
    /// <param name="position">The position of the wall segment.</param>
    /// <param name="size">The size of the wall segment.</param>
    /// <param name="originalPosition">The original position of the wall object.</param>
    /// <param name="originalRotation">The original rotation of the wall object.</param>
void InstantiateWallSegment(Vector3 position, Vector3 size, Vector3 originalPosition, Quaternion originalRotation)
{
    // Instantiate the wall segment at the origin with no rotation
    GameObject wallSegment = Instantiate(wallSegmentPrefab, Vector3.zero, Quaternion.identity);
    wallSegment.transform.localScale = size;
    wallSegment.transform.SetParent(selectedWall.transform.parent, false);

    // Get the material from the selected wall
    Material wallMaterial = selectedWall.GetComponent<Renderer>().sharedMaterial;

    // Assign the material to the instantiated segment
    wallSegment.GetComponent<Renderer>().sharedMaterial = wallMaterial;

    // Perform plane creation for each block while the segment is at the origin
    CreateAllPlanes(wallSegment);

    // Move the wall segment to the desired position and rotation
    wallSegment.transform.position = originalPosition + originalRotation * position;
    wallSegment.transform.rotation = originalRotation;

    instantiatedSegments.Add(wallSegment); // Add the new segment to the list
}

    /// <summary>
    /// A copy paste from VRDrawing with the same functionality.
    /// A cube is assigned 6 planes -- one per face.
    /// </summary>
    /// <param name="currentCube">Cube to assign planes onto</param>
    private void CreateAllPlanes(GameObject currentCube)
    {
        // Calculate the offset distance for the planes
        float offsetDistance = 0.0001f;
        // Get the renderer component of the current cube
        Renderer cubeRenderer = currentCube.GetComponent<Renderer>();
        // Get the bounds of the cube
        Bounds cubeBounds = cubeRenderer.bounds;

        // Dimensions for planes on each set of faces
        Vector2 XYPlaneSize = new Vector2(cubeBounds.size.x, cubeBounds.size.y);
        Vector2 XZPlaneSize = new Vector2(cubeBounds.size.x, cubeBounds.size.z);
        Vector2 YZPlaneSize = new Vector2(cubeBounds.size.y, cubeBounds.size.z);

        // Create planes for each cube face
        // Front and Back (XY)
        CreatePlane("XY", Vector3.forward, cubeBounds.center + Vector3.forward * (cubeBounds.extents.z + offsetDistance), currentCube.transform, XYPlaneSize);
        CreatePlane("XY", Vector3.back, cubeBounds.center - Vector3.forward * (cubeBounds.extents.z + offsetDistance), currentCube.transform, XYPlaneSize);
        // Top and Bottom (XZ)
        CreatePlane("XZ", Vector3.up, cubeBounds.center + Vector3.up * (cubeBounds.extents.y + offsetDistance), currentCube.transform, XZPlaneSize);
        CreatePlane("XZ", Vector3.down, cubeBounds.center - Vector3.up * (cubeBounds.extents.y + offsetDistance), currentCube.transform, XZPlaneSize);
        // Right and Left (YZ)
        CreatePlane("YZ", Vector3.right, cubeBounds.center + Vector3.right * (cubeBounds.extents.x + offsetDistance), currentCube.transform, YZPlaneSize);
        CreatePlane("YZ", Vector3.left, cubeBounds.center - Vector3.right * (cubeBounds.extents.x + offsetDistance), currentCube.transform, YZPlaneSize);
    }

    /// <summary>
    /// Create a plan given a load of parameters
    /// </summary>
    /// <param name="faceName">Cardinal plane name</param>
    /// <param name="normal">Direction of plane</param>
    /// <param name="position">Global position</param>
    /// <param name="parent">Cube position to assign plane onto</param>
    /// <param name="size">Size of plane</param>
    private void CreatePlane(string faceName, Vector3 normal, Vector3 position, Transform parent, Vector2 size)
    {
        // Create a new plane GameObject
        GameObject plane = new GameObject($"Plane_{faceName}");
        plane.transform.SetParent(parent, false); // Set parent without worldPositionStays

        // Add a mesh filter and mesh renderer component to the plane
        MeshFilter meshFilter = plane.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = plane.AddComponent<MeshRenderer>();

        // Create a new quad mesh for the plane
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { new Vector3(-0.5f, -0.5f, 0), new Vector3(0.5f, -0.5f, 0), new Vector3(0.5f, 0.5f, 0), new Vector3(-0.5f, 0.5f, 0) };
        mesh.uv = new Vector2[] { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;

        // Determine the scaling factor based on the cube's scale and the plane's orientation
        Vector3 scalingFactor = Vector3.one;
        if (normal == Vector3.forward || normal == Vector3.back) // XY plane
        {
            scalingFactor = new Vector3(size.x / parent.localScale.x, size.y / parent.localScale.y, 1f);
        }
        else if (normal == Vector3.up || normal == Vector3.down) // XZ plane
        {
            scalingFactor = new Vector3(size.x / parent.localScale.x, size.y / parent.localScale.z, 1f);
        }
        else if (normal == Vector3.right || normal == Vector3.left) // YZ plane
        {
            scalingFactor = new Vector3(size.x / parent.localScale.y, size.y / parent.localScale.z, 1f);
        }

        // Set the local scale based on calculated scaling factor
        plane.transform.localScale = scalingFactor;

        // Position the plane
        plane.transform.position = position;
        plane.transform.rotation = Quaternion.LookRotation(normal);

        // Add a box collider to the plane
        BoxCollider boxCollider = plane.AddComponent<BoxCollider>();
        boxCollider.size = new Vector3(1f, 1f, 0.01f); // Adjust as necessary

        // Set the layer and tag as before
        plane.layer = LayerMask.NameToLayer("ReferencePlane");
        plane.tag = faceName;


        // find the dimensions of the face of the original wall
        // dimensions for xz will be used for xz, xy for xy, etc
        foreach (Transform child in selectedWall.transform)
        {
            string[] s = child.name.Split("_");
            string face = s[1];

            // when the right face is found
            if (face == faceName) 
            {
                // get the texture scale
                Vector2 bigPlaneSize = child.GetComponent<Renderer>().sharedMaterial.mainTextureScale;

                // get the scale of the wall segment's smaller plane
                Vector2 smallPlaneSize = size;

                Vector3 scaledSize = smallPlaneSize;

                meshRenderer.material = ScaleMaterialToPlaneScale(selectedWall.GetComponent<Renderer>().sharedMaterial, scaledSize, faceName);
                break;
            }

        }
        
    }

    /// <summary>
    /// Creates a new textured material for the renderer component of a plane
    /// and scales the texture.
    /// </summary>
    /// <param name="unscaledMaterial">The material the object wants to look like</param>
    /// <param name="planeSize">The size of the face of the plane</param>
    /// <param name="planeDimensions">The cardinal plane</param>
    /// <returns>Returns a scaled material</returns>
    private Material ScaleMaterialToPlaneScale(Material unscaledMaterial, Vector2 planeSize, string planeDimensions)
    {
        Material scaledMaterial = new Material(Shader.Find("Standard"));


        float prefabScale = 0.005f;
        float brickSizeScale = 0.01f;

        scaledMaterial.CopyMatchingPropertiesFromMaterial(unscaledMaterial);
        // scaledMaterial.mainTextureScale = new Vector2(1 / planeSize.x, 1 / planeSize.y);
        scaledMaterial.mainTextureScale = new Vector2(planeSize.x, planeSize.y) / 2 / prefabScale * brickSizeScale;

        // yz dimension swaps xy positions
        if (planeDimensions == "YZ")
            scaledMaterial.mainTextureScale = new Vector2(planeSize.y, planeSize.x) / 2 / prefabScale * brickSizeScale;

        return scaledMaterial;

    }


    /// <summary>
    /// Groups the instantiated wall segments under a parent object and configures the parent object's collider and OVRGrabbable component.
    /// This method is called after all the wall segments have been instantiated.
    /// It creates a new parent object named "SegmentsParent" and performs the following steps:
    /// 1. Adds a Rigidbody component to the parent object and sets it to kinematic.
    /// 2. Adds a BoxCollider component to the parent object and adjusts its size and center to encapsulate all the child segments.
    /// 3. Adds an OVRGrabbable component to the parent object and enables it.
    /// 4. Adds the parent object's collider as a grab point for the OVRGrabbable component.
    /// 5. Parents all the instantiated segments to the "SegmentsParent" object.
    /// 6. Removes the OVRGrabbable component from the child segments.
    /// </summary>
void GroupSegmentsUnderParent()
{
    GameObject segmentsParent = new GameObject("SegmentsParent");
    segmentsParent.tag = "wallParent";

    // Add Rigidbody to make it interactable in the physics system
    Rigidbody rb = segmentsParent.AddComponent<Rigidbody>();
    rb.isKinematic = true; // Set to true to prevent physics from moving the object

    // First, add and configure the BoxCollider
    BoxCollider collider = segmentsParent.AddComponent<BoxCollider>();
    SetColliderToEncapsulateChildren(segmentsParent); // Adjust the size and center

    // Now, add the OVRGrabbable script, after collider is configured
    OVRGrabbable grabbableComponent = segmentsParent.AddComponent<OVRGrabbable>();
    grabbableComponent.enabled = true;

    // Use the new method to add the parent's collider as a grab point
    grabbableComponent.AddGrabPoint(collider);

    // Calculate the bounds of the instantiated segments
    Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
    bool hasBounds = false;

    foreach (GameObject segment in instantiatedSegments)
    {
        segment.transform.SetParent(segmentsParent.transform, false);

        // Disable or remove OVRGrabbable from children
        OVRGrabbable grabbable = segment.GetComponent<OVRGrabbable>();
        if (grabbable != null)
        {
            Destroy(grabbable);
        }

        // Update the bounds with each segment's bounds
        Renderer segmentRenderer = segment.GetComponent<Renderer>();
        if (segmentRenderer != null)
        {
            if (hasBounds)
            {
                bounds.Encapsulate(segmentRenderer.bounds);
            }
            else
            {
                bounds = segmentRenderer.bounds;
                hasBounds = true;
            }
        }
    }

    // Set the pivot point of the "SegmentsParent" object to the center of the bounds
    segmentsParent.transform.position = bounds.center;

    // Reset the local positions of the child segments relative to the new pivot point
    foreach (GameObject segment in instantiatedSegments)
    {
        segment.transform.localPosition -= segmentsParent.transform.localPosition;
    }

    instantiatedSegments.Clear();
}

    /// <summary>
    /// Sets the collider of the parent object to encapsulate all its children.
    /// This method is called by the GroupSegmentsUnderParent method to adjust the size and center of the parent object's BoxCollider.
    /// </summary>
    /// <param name="parentObject">The parent object to set the collider for.</param>
void SetColliderToEncapsulateChildren(GameObject parentObject)
{
    Bounds bounds = new Bounds(Vector3.zero, Vector3.zero);
    bool hasBounds = false;
    Renderer[] renderers = parentObject.GetComponentsInChildren<Renderer>();

    // Calculate the bounds relative to the parent's pivot point
    foreach (Renderer renderer in renderers)
    {
        if (hasBounds)
        {
            bounds.Encapsulate(renderer.bounds);
        }
        else
        {
            bounds = renderer.bounds;
            hasBounds = true;
        }
    }

    // If the parent object has a renderer, we should exclude its bounds
    Renderer parentRenderer = parentObject.GetComponent<Renderer>();
    if (parentRenderer != null)
    {
        bounds.Encapsulate(parentRenderer.bounds);
    }

    // Calculate the size and center relative to the parent's pivot point
    BoxCollider collider = parentObject.GetComponent<BoxCollider>();
    if (collider == null)
    {
        collider = parentObject.AddComponent<BoxCollider>();
    }

    collider.center = bounds.center - parentObject.transform.position;
    collider.size = bounds.size;
}





}

