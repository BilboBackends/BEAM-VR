using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// The VRDrawing class handles drawing and extruding cubes in VR using the right-hand controller.
/// </summary>
public class VRDrawing : MonoBehaviour
{
    

    public Material defaultMaterial;

    /// <summary>
    /// The cube prefab to instantiate for drawing.
    /// </summary>
    public GameObject cubePrefab;

    /// <summary>
    /// Administrative variable size of the prefab to
    /// scale texture along with brickSizeScale
    /// </summary>
    [SerializeField]
    private float prefabScale = 0.005f;

    /// <summary>
    /// Scale factor for controlling size of brick
    /// </summary>
    private float brickSizeScale = 0.01f;

    /// <summary>
    /// The controller to use for drawing (default is right-hand controller).
    /// </summary>
    public OVRInput.Controller controller = OVRInput.Controller.RTouch;

    /// <summary>
    /// The transform of the right-hand anchor.
    /// </summary>
    public Transform rightHandAnchor;

    /// <summary>
    /// The current cube being drawn.
    /// </summary>
    private GameObject currentCube = null;


    /// <summary>
    /// Flag indicating if drawing is currently active.
    /// </summary>
    private bool isDrawing = false;

    /// <summary>
    /// Flag indicating if the extrusion preview mode is active.
    /// </summary>
    private bool isInPreviewMode = false;

    /// <summary>
    /// The initial position of the controller when starting the extrusion preview.
    /// </summary>
    private Vector3 initialControllerPosition;

    /// <summary>
    /// The initial rotation of the controller when starting the extrusion preview.
    /// </summary>
    private Quaternion initialControllerRotation;

    /// <summary>
    /// The initial scale of the cube when starting the drawing.
    /// </summary>
    private Vector3 initialScale;

    /// <summary>
    /// It calls the HandleInput method to process controller inputs.
    /// </summary>
    void Update()
    {
        // Call HandleInput to process controller inputs.
        HandleInput();
        ScalePlaneMaterialEveryUpdate(currentCube);
    }

    /// <summary>
    /// Handles the input from the controller to start drawing, preview extrusion, and finalize drawing.
    /// </summary>
private void HandleInput()
{
    // Get the current position of the right-hand controller.
    Vector3 controllerPosition = rightHandAnchor.position;

    // Perform the raycast to check if hovering over a UI element
    RaycastHit hit;
    if (Physics.Raycast(controllerPosition, rightHandAnchor.forward, out hit, Mathf.Infinity, LayerMask.GetMask("UI")))
    {
        // If the raycast hits a UI element, return without executing anything
        return;
    }

    // Check if the primary hand trigger button is pressed down.
    if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, controller))
    {
        // If drawing is not active, start a new drawing.
        if (!isDrawing)
        {
            StartDrawing(controllerPosition);
        }
        // If drawing is active but preview mode is not, start the extrusion preview.
        else if (!isInPreviewMode)
        {
            StartPreview(controllerPosition);
        }
        // If both drawing and preview mode are active, finalize the drawing.
        else
        {
            FinalizeDrawing();
        }
    }

    // If drawing is active and preview mode is active, update the extrusion preview.
    if (isDrawing && isInPreviewMode)
    {
        UpdateExtrusionPreview(controllerPosition);
    }
}

    /// <summary>
    /// Starts the extrusion preview mode.
    /// It sets the isInPreviewMode flag to true and stores the initial controller position.
    /// </summary>
    /// <param name="controllerPosition">The current position of the controller.</param>
    private void StartPreview(Vector3 controllerPosition)
    {
        // Set the preview mode flag to true.
        isInPreviewMode = true;

        // Store the initial controller position for extrusion calculations.
        initialControllerPosition = controllerPosition;
    }

    /// <summary>
    /// Updates the extrusion preview based on the controller position.
    /// It calculates the scale change based on the controller movement and updates the cube's scale and position accordingly.
    /// </summary>
    /// <param name="controllerPosition">The current position of the controller.</param>
    private void UpdateExtrusionPreview(Vector3 controllerPosition)
    {
        // If there is no current cube, return early.
        if (currentCube == null) return;

        // Calculate the direction from the initial controller position to the current position.
        Vector3 direction = controllerPosition - initialControllerPosition;

        // Convert the direction to the cube's local space.
        Vector3 localDirection = currentCube.transform.InverseTransformDirection(direction);

        // Initialize the scale change vector.
        Vector3 scaleChange = Vector3.zero;

        // Determine the axis with the largest absolute value in the local direction.
        if (Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.y) && Mathf.Abs(localDirection.x) > Mathf.Abs(localDirection.z))
        {
            // If the X-axis has the largest absolute value, set the scale change to the X component.
            scaleChange.x = localDirection.x;
        }
        else if (Mathf.Abs(localDirection.y) > Mathf.Abs(localDirection.x) && Mathf.Abs(localDirection.y) > Mathf.Abs(localDirection.z))
        {
            // If the Y-axis has the largest absolute value, set the scale change to the Y component.
            scaleChange.y = localDirection.y;
        }
        else if (Mathf.Abs(localDirection.z) > Mathf.Abs(localDirection.x) && Mathf.Abs(localDirection.z) > Mathf.Abs(localDirection.y))
        {
            // If the Z-axis has the largest absolute value, set the scale change to the Z component.
            scaleChange.z = localDirection.z;
        }

        // Apply an extrusion speed multiplier to control the speed of extrusion.
        float extrusionSpeedMultiplier = 0.1f;
        scaleChange *= extrusionSpeedMultiplier;

        // Calculate the new scale by adding the scale change to the current scale.
        Vector3 newScale = currentCube.transform.localScale + scaleChange;

        // Adjust the scale to ensure it doesn't go below the initial scale.
        Vector3 adjustedScale = new Vector3(
            Mathf.Abs(newScale.x) < initialScale.x ? initialScale.x : newScale.x,
            Mathf.Abs(newScale.y) < initialScale.y ? initialScale.y : newScale.y,
            Mathf.Abs(newScale.z) < initialScale.z ? initialScale.z : newScale.z
        );

        // Check if the cube is being unextruded (scale reduced below initial scale).
        bool isUnextruding = newScale.x < initialScale.x || newScale.y < initialScale.y || newScale.z < initialScale.z;

        // Calculate the position adjustment based on the scale change.
        Vector3 newPositionAdjustment = (adjustedScale - currentCube.transform.localScale) * 0.5f;
        Vector3 newPosition = currentCube.transform.position + newPositionAdjustment;

        // If unextruding, adjust the initial controller position to allow growth in the opposite direction.
        if (isUnextruding)
        {
            initialControllerPosition += newPositionAdjustment * 2;
        }

        // Apply the new scale and position to the cube.
        currentCube.transform.localScale = adjustedScale;
        currentCube.transform.position = newPosition;
    }

    /// <summary>
    /// Starts the drawing process by instantiating a new cube at the controller position.
    /// It sets the initial scale, drawing flag, preview mode flag, and initial controller position.
    /// </summary>
    /// <param name="controllerPosition">The current position of the controller.</param>
    private string selectedFace = "";

    private void StartDrawing(Vector3 controllerPosition)
    {
        // Instantiate the cube prefab at the controller position with no rotation.
        currentCube = Instantiate(cubePrefab, controllerPosition, Quaternion.identity);
        Renderer renderer = currentCube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.material = defaultMaterial;  // Ensure new objects use the updated material
        }
        CubeData cubeData = currentCube.AddComponent<CubeData>();
        // Store the initial scale of the cube.
        initialScale = currentCube.transform.localScale;
        
        // Set the drawing flag to true.
        isDrawing = true;

        // Set the preview mode flag to true.
        isInPreviewMode = true;

        // Store the initial controller position for extrusion calculations.
        initialControllerPosition = controllerPosition;

        // Get the controller's forward direction relative to the cube.
        Vector3 controllerForward = rightHandAnchor.forward;
        Vector3 controllerLocalForward = currentCube.transform.InverseTransformDirection(controllerForward);

        // Determine the face based on the controller's local forward direction.
        if (Mathf.Abs(controllerLocalForward.y) > Mathf.Abs(controllerLocalForward.x) && Mathf.Abs(controllerLocalForward.y) > Mathf.Abs(controllerLocalForward.z))
        {
            cubeData.selectedFace = "XZ";
        }
        else if (Mathf.Abs(controllerLocalForward.z) > Mathf.Abs(controllerLocalForward.x) && Mathf.Abs(controllerLocalForward.z) > Mathf.Abs(controllerLocalForward.y))
        {
            cubeData.selectedFace = "XY";
        }
        else
        {
            cubeData.selectedFace = "YZ";
        }
        CreateAllPlanes();
        Debug.Log("Selected Face: " + cubeData.selectedFace);
    }

        /// <summary>
        /// Finalizes the drawing process and solidifies the extrusion changes.
        /// </summary>
    private void FinalizeDrawing()
    {
        // Set the drawing flag to false.
        isDrawing = false;
        // Set the preview mode flag to false.
        isInPreviewMode = false;
        // Create the XY planes on the front and back sides of the rectangle
        //CreateAllPlanes();
    }
        /// <summary>
        /// Calclates the size of the planes to then call each individually
        /// </summary>
    private void CreateAllPlanes()
    {
            // Calculate the offset distance for the planes
            float offsetDistance = 0.000008f;
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
            CreatePlane("XY", Vector3.forward, cubeBounds.center - Vector3.forward * (cubeBounds.extents.z + offsetDistance), currentCube.transform, XYPlaneSize);
            CreatePlane("XY", Vector3.back, cubeBounds.center + Vector3.forward * (cubeBounds.extents.z + offsetDistance), currentCube.transform, XYPlaneSize);

            CreatePlane("XY", Vector3.forward, cubeBounds.center + Vector3.forward * (cubeBounds.extents.z + offsetDistance), currentCube.transform, XYPlaneSize);
            CreatePlane("XY", Vector3.back, cubeBounds.center - Vector3.forward * (cubeBounds.extents.z + offsetDistance), currentCube.transform, XYPlaneSize);

        // Top and Bottom (XZ)
            CreatePlane("XZ", Vector3.up, cubeBounds.center - Vector3.up * (cubeBounds.extents.y + offsetDistance), currentCube.transform, XZPlaneSize);
            CreatePlane("XZ", Vector3.down, cubeBounds.center + Vector3.up * (cubeBounds.extents.y + offsetDistance), currentCube.transform, XZPlaneSize);

            CreatePlane("XZ", Vector3.up, cubeBounds.center + Vector3.up * (cubeBounds.extents.y + offsetDistance), currentCube.transform, XZPlaneSize);
            CreatePlane("XZ", Vector3.down, cubeBounds.center - Vector3.up * (cubeBounds.extents.y + offsetDistance), currentCube.transform, XZPlaneSize);

        // Right and Left (YZ)
            CreatePlane("YZ", Vector3.right, cubeBounds.center - Vector3.right * (cubeBounds.extents.x + offsetDistance), currentCube.transform, YZPlaneSize);
            CreatePlane("YZ", Vector3.left, cubeBounds.center + Vector3.right * (cubeBounds.extents.x + offsetDistance), currentCube.transform, YZPlaneSize);

            CreatePlane("YZ", Vector3.right, cubeBounds.center + Vector3.right * (cubeBounds.extents.x + offsetDistance), currentCube.transform, YZPlaneSize);
            CreatePlane("YZ", Vector3.left, cubeBounds.center - Vector3.right * (cubeBounds.extents.x + offsetDistance), currentCube.transform, YZPlaneSize);
    }

        /// <summary>
        /// The function calculating the meshes for each plane
        /// </summary>
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


        print("Plane: " + plane.name);
        print("Scale based on the face of the cube" + scalingFactor);
        Debug.Log("Size of plane: " + size);
        
        meshRenderer.material = ScaleMaterialToPlaneScale(defaultMaterial, size, faceName);

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


        // scaledMaterial.CopyMatchingPropertiesFromMaterial(unscaledMaterial);

        scaledMaterial.mainTexture = unscaledMaterial.mainTexture;
        // scaledMaterial.mainTextureScale = new Vector2(1 / planeSize.x, 1 / planeSize.y);

        scaledMaterial.CopyMatchingPropertiesFromMaterial(unscaledMaterial);

        scaledMaterial.mainTextureScale = new Vector2(planeSize.x, planeSize.y) / 2 / prefabScale;

        // yz dimension swaps xy positions
        if (planeDimensions == "YZ")
            scaledMaterial.mainTextureScale = new Vector2(planeSize.y, planeSize.x) / 2 / prefabScale;

        return scaledMaterial;

        // offset is 1 - plane scale factor
    }


    /// <summary>
    /// Assists in scaling the texture as the cube grows.
    /// </summary>
    /// <param name="cube">The object whose texture planes are to scale.</param>
    private void ScalePlaneMaterialEveryUpdate(GameObject cube)
    {
        Renderer cubeRenderer = cube.GetComponent<Renderer>();
        // Get the bounds of the cube
        Bounds cubeBounds = cubeRenderer.bounds;

        Vector2 XYPlaneSize = new Vector2(cubeBounds.size.x, cubeBounds.size.y);
        Vector2 XZPlaneSize = new Vector2(cubeBounds.size.x, cubeBounds.size.z);

        // swapped y and z
        Vector2 YZPlaneSize = new Vector2(cubeBounds.size.z, cubeBounds.size.y);

        // for each child plane of the cube, get the name of the plane for dimensions
        foreach (Transform plane in cube.transform)
        {
            string[] s = plane.name.Split('_');

            Renderer planeRenderer = plane.GetComponent<Renderer>();

            // scale the material for the plane according to the name of the plane
            switch(s[1])
            {
                case "XY":
                    planeRenderer.material.mainTextureScale = XYPlaneSize / 2 / prefabScale * brickSizeScale;
                    break;
                case "XZ":
                    planeRenderer.material.mainTextureScale = XZPlaneSize / 2 / prefabScale * brickSizeScale;
                    break;
                case "YZ":
                    planeRenderer.material.mainTextureScale = YZPlaneSize / 2 / prefabScale * brickSizeScale;
                    break;
            }


            
        }

        
    }

    /// <summary>
    /// Public function to expose the default material variable to change the material of newly created walls
    /// </summary>
    /// <param name="newMaterial"></param>
    public void setDefaultMaterial(Material newMaterial, GameObject m_cube) {
        defaultMaterial = newMaterial;
        Debug.Log("Material changed to " + newMaterial.name);

        ScalePlaneMaterialEveryUpdate(m_cube);
        // Update the material on the current cube if it exists
    if (currentCube != null)
    {
        Renderer cubeRenderer = currentCube.GetComponent<Renderer>();
        if (cubeRenderer != null)
        {
            cubeRenderer.material = defaultMaterial;
        }
    }
    }
} 