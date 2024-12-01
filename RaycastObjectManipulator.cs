using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// This class handles object manipulation using raycasts.
/// It allows for selecting, moving, rotating, copying, and deleting objects in the scene.
/// The manipulation is controlled using the left and right controllers.
/// </summary>
public class RaycastObjectManipulator : MonoBehaviour
{

    /// <summary>
    /// A bunch of flags for which move mode were in
    /// </summary>
public enum ManipulationMode
{   
    None,
    Move,
    Rotate,
    MoveX,
    MoveY,
    MoveZ,
    RotateX,
    RotateY,
    RotateZ,
    Delete,
    Copy
}

    /// <summary>
    /// Allows movement in x direction
    /// </summary>
    public ManipulationMode currentMode = ManipulationMode.MoveX;

    /// <summary>
    /// Flag to turn on move mode
    /// </summary>
    public bool isMoveMode = true;

    /// <summary>
    /// Flag to make sure only one object is moving
    /// </summary>
    public static bool IsObjectBeingManipulated = false;
    /// <summary>
    /// The source of the raycast used for object selection.
    /// This can be the camera or a controller, depending on your setup.
    /// </summary>
    public Transform raycastSource;
    
    /// <summary>
    /// The line renderer component used to visualize the raycast.
    /// This helps in debugging and understanding where the raycast is pointing.
    /// </summary>
    public LineRenderer lineRenderer;
    
    /// <summary>
    /// The maximum length of the raycast.
    /// Objects beyond this distance will not be selectable.
    /// </summary>
    public float rayLength = 100f;
    
    /// <summary>
    /// The layer mask used to filter interactable objects.
    /// Only objects on the specified layer(s) will be selectable.
    /// </summary>
    public LayerMask interactableLayer;
    
    /// <summary>
    /// The size of the grid unit for snapping object positions.
    /// Objects will be moved in increments of this value when snapping to the grid.
    /// </summary>
    public float gridUnit = .1f;
    
    /// <summary>
    /// The speed of rotation in degrees per second.
    /// This determines how fast the object rotates when using the rotation input.
    /// </summary>
    public float rotationSpeed = 10f;
    
    /// <summary>
    /// The angle increment for snapping rotations.
    /// Objects will be rotated in increments of this value when snapping to specific angles.
    /// </summary>
    public float snapAngle = 20f;

    /// <summary>
    /// The currently selected object.
    /// This is the object that will be manipulated (moved, rotated, copied, or deleted).
    /// </summary>
    private GameObject selectedObject = null;
    
    /// <summary>
    /// Flag indicating if an object is currently being manipulated.
    /// This is used to control the manipulation state and prevent unintended actions.
    /// </summary>
    private bool isManipulatingObject = false;
    
    /// <summary>
    /// Flag indicating if an object is currently being moved.
    /// This is used to control the movement state and prevent continuous movement.
    /// </summary>
    private bool isObjectBeingMoved = false;
    
    /// <summary>
    /// Flag indicating if an object is currently being copied.
    /// This is used to control the copying state and prevent multiple copies from being created simultaneously.
    /// </summary>
    private bool isCopying = false;

    /// <summary>
    /// Update is called once per frame.
    /// It handles the main logic of the script, including raycasting, object manipulation, and deletion.
    /// </summary>

    void Update()
    {
        HandleRaycast();

        HandleObjectManipulation();

        //HandleObjectDeletion();

    }

    /// <summary>
    /// Allows movement script to activate
    /// </summary>
    public bool IsMoveModeActive()
    {
        return currentMode == ManipulationMode.Move;
    }
    /// <summary>
    /// Allows rotate script to activate
    /// </summary>
    public bool IsRotateModeActive()
    {
        return currentMode == ManipulationMode.Rotate;
    }
    /// <summary>
    /// Allows Delete script to activate
    /// </summary>
    public bool IsDeleteModeActive()
    {
        return currentMode == ManipulationMode.Delete;
    }
    /// <summary>
    /// Allows Copy script to activate
    /// </summary>
    public bool IsCopyModeActive()
    {
        return currentMode == ManipulationMode.Copy;
    }
    /// <summary>
    /// Turns off all movement
    /// </summary>
public void SetNoMode()
{
    currentMode = ManipulationMode.None;
}
    /// <summary>
    /// Allows x y z movement
    /// </summary>
public void SetMoveMode()
{
    currentMode = ManipulationMode.Move;
}
    /// <summary>
    /// Allows rotation
    /// </summary>
public void SetRotateMode()
{
    currentMode = ManipulationMode.Rotate;
}
    /// <summary>
    /// Allows deletion
    /// </summary>
public void SetDeleteMode()
{
    currentMode = ManipulationMode.Delete;
}
    /// <summary>
    /// Allows copying
    /// </summary>
public void SetCopyMode()
{
    currentMode = ManipulationMode.Copy;
}

    /// <summary>
    /// Performs a raycast and selects the object hit by the raycast.
    /// If an object is hit, it is assigned to the selectedObject variable.
    /// The line renderer is updated to visualize the raycast.
    /// </summary>
    private void HandleRaycast()
    {
        // Check if the raycast source and line renderer are assigned
        if (raycastSource != null && lineRenderer != null)
        {
            // Get the start position and direction of the raycast
            Vector3 start = raycastSource.position;
            Vector3 direction = raycastSource.forward;
            Vector3 end = start + direction * rayLength;

            // Perform the raycast and check if it hits an interactable object
            if (Physics.Raycast(start, direction, out RaycastHit hit, rayLength, interactableLayer))
            {
                // Update the end position of the line renderer to the hit point
                end = hit.point;

                // Check if the hit object has a parent named "SegmentsParent"
                Transform parentObject = hit.collider.transform.parent;
                while (parentObject != null)
                {
                    if (parentObject.name == "SegmentsParent")
                    {
                        // Assign the parent as the selected object
                        selectedObject = parentObject.gameObject;
                        break;
                    }
                    parentObject = parentObject.parent;
                }

                // If the object is not part of "SegmentsParent", select the hit object directly
                if (selectedObject == null)
                {
                    selectedObject = hit.collider.gameObject;
                }

                // Check if the primary index trigger or primary hand trigger is pressed on the Right controller
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) || OVRInput.GetDown(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
                {
                    // Start manipulating the selected object
                    isManipulatingObject = true;
                    IsObjectBeingManipulated = true;
                    isObjectBeingMoved = false;
                }
            }

            // Update the positions of the line renderer
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
        }
    }

    /// <summary>
    /// Handles object manipulation, including movement, rotation, and copying.
    /// The manipulation is controlled using the left controller's input.
    /// </summary>
private void HandleObjectManipulation()
{
    if (selectedObject != null && isManipulatingObject)
    {

        Vector2 topTriggerInput = Vector2.zero;
        float bottomTriggerInput = 0f;
        if (OVRInput.Get(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
        {
            topTriggerInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch);
        }
        if (OVRInput.Get(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            bottomTriggerInput = OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick, OVRInput.Controller.RTouch).y;
        }

        switch (currentMode)
        {
            case ManipulationMode.Move:
                if (Mathf.Abs(topTriggerInput.x) > Mathf.Abs(topTriggerInput.y) && Mathf.Abs(topTriggerInput.x) > Mathf.Abs(bottomTriggerInput))
                {
                    HandleMovement(topTriggerInput.x, 0f, 0f);
                }
                else if (Mathf.Abs(topTriggerInput.y) > Mathf.Abs(bottomTriggerInput))
                {
                    HandleMovement(0f, topTriggerInput.y, 0f);
                }
                else
                {
                    HandleMovement(0f, 0f, bottomTriggerInput);
                }
                break;
            case ManipulationMode.Rotate:
                if (Mathf.Abs(topTriggerInput.x) > Mathf.Abs(topTriggerInput.y) && Mathf.Abs(topTriggerInput.x) > Mathf.Abs(bottomTriggerInput))
                {
                    HandleRotation(selectedObject, Vector3.up, topTriggerInput.x);
                }
                else if (Mathf.Abs(topTriggerInput.y) > Mathf.Abs(bottomTriggerInput))
                {
                    HandleRotation(selectedObject, Vector3.right, topTriggerInput.y);
                }
                else
                {
                    HandleRotation(selectedObject, Vector3.forward, bottomTriggerInput);
                }
                break;
        case ManipulationMode.Delete:
            if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
            {
                RaycastHit[] hits;
                hits = Physics.RaycastAll(raycastSource.position, raycastSource.forward, Mathf.Infinity, interactableLayer);

                foreach (RaycastHit hit in hits)
                {
                    if (hit.collider.gameObject.layer == LayerMask.NameToLayer("cube"))
                    {
                        GameObject objectToDelete = hit.collider.gameObject;
                        if (objectToDelete != null)
                        {
                            if (objectToDelete.transform.parent != null && objectToDelete.transform.parent.name == "SegmentsParent")
                            {
                                Destroy(objectToDelete.transform.parent.gameObject);
                            }
                            else
                            {
                                Destroy(objectToDelete);
                            }
                            selectedObject = null; // Clear the selection as the object is deleted
                            break; // Exit the loop after deleting the first valid object
                        }
                    }
                }
            }
                break;
            case ManipulationMode.Copy:
                if (OVRInput.GetDown(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch))
                {
                    CopySelectedObject();
                }
                break;
        }

        // Additional input checks for releasing objects
        if (OVRInput.GetUp(OVRInput.Button.PrimaryIndexTrigger, OVRInput.Controller.RTouch) || OVRInput.GetUp(OVRInput.Button.PrimaryHandTrigger, OVRInput.Controller.RTouch))
        {
            // Stop manipulating the object and deselect it
            isManipulatingObject = false;
            IsObjectBeingManipulated = false;
            selectedObject = null;
        }
    }
} 
    /// <summary>
    /// Handles horizontal movement of the selected object based on input.
    /// The object is moved in the XZ plane relative to the player's position.
    /// </summary>
private void HandleMovement(float moveX, float moveY, float moveZ)
{
    if (!isObjectBeingMoved)
    {
        isObjectBeingMoved = true;

        Vector3 userForward = raycastSource.transform.forward;
        Vector3 userRight = raycastSource.transform.right;
        Vector3 userUp = raycastSource.transform.up;

        Vector3 moveDirection = userRight * moveX + userUp * moveY + userForward * moveZ;
        moveDirection.Normalize();

        Vector3 newPosition = selectedObject.transform.position + moveDirection * gridUnit;
        newPosition = SnapPositionToGrid(newPosition);
        selectedObject.transform.position = newPosition;

        StartCoroutine(ResetMovementFlag());
    }
}

    /// <summary>
    /// Handles vertical movement and rotation of the selected object based on input.
    /// The object is moved vertically or rotated based on the stronger input.
    /// </summary>
    private void HandleVerticalMovement(float verticalMove)
    {
        if (Mathf.Abs(verticalMove) > 0.1f && !isObjectBeingMoved)
        {
            isObjectBeingMoved = true;

            Vector3 playerUp = raycastSource.transform.up;

            float verticalAdjustment = Mathf.Sign(verticalMove) * gridUnit;
            Vector3 newPosition = selectedObject.transform.position + playerUp * verticalAdjustment;
            newPosition.y = Mathf.Round(newPosition.y / gridUnit) * gridUnit;
            selectedObject.transform.position = newPosition;

            StartCoroutine(ResetMovementFlag());
        }
    }

    private void HandleRotation(GameObject obj, Vector3 rotationAxis, float rotationInput)
    {
        if (Mathf.Abs(rotationInput) > 0.1f && !isObjectBeingMoved)
        {
            isObjectBeingMoved = true;

            float snappedRotation = Mathf.Sign(rotationInput) * snapAngle;
            obj.transform.Rotate(rotationAxis, snappedRotation, Space.World);

            SnapRotation(obj, rotationAxis);

            StartCoroutine(ResetMovementFlag());
        }
    }

/// <summary>
/// Rotates the specified object by the snap angle in the given direction and snaps the rotation.
/// </summary>
private void RotateAndSnap(GameObject obj, float direction, Vector3 rotationAxis)
{
    // Calculate the snapped rotation based on the direction and snap angle
    float snappedRotation = direction * snapAngle;
    
    // Rotate the object around the specified axis by the snapped rotation
    obj.transform.Rotate(rotationAxis, snappedRotation, Space.World);

    // Snap the object's rotation to the nearest multiple of the snap angle
    SnapRotation(obj, rotationAxis);
}

    /// <summary>
    /// Snaps the rotation of the specified object to the nearest multiple of the snap angle.
    /// </summary>
    /// <param name="obj">The object to snap the rotation of.</param>
    private void SnapRotation(GameObject obj, Vector3 rotationAxis)
    {
        Vector3 eulerAngles = obj.transform.eulerAngles;

        if (rotationAxis == Vector3.right)
        {
            eulerAngles.x = RoundToNearest(eulerAngles.x, snapAngle);
        }
        else if (rotationAxis == Vector3.up)
        {
            eulerAngles.y = RoundToNearest(eulerAngles.y, snapAngle);
        }
        else if (rotationAxis == Vector3.forward)
        {
            eulerAngles.z = RoundToNearest(eulerAngles.z, snapAngle);
        }

        obj.transform.eulerAngles = eulerAngles;
    }

        private float RoundToNearest(float value, float increment)
    {
        return Mathf.Round(value / increment) * increment;
    }

    /// <summary>
    /// Snaps the specified position to the nearest grid point.
    /// </summary>
    /// <param name="position">The position to snap.</param>
    /// <returns>The snapped position.</returns>
    private Vector3 SnapPositionToGrid(Vector3 position)
    {
        // Round the X and Z components of the position to the nearest multiple of the grid unit
        position.x = Mathf.Round(position.x / gridUnit) * gridUnit;
        position.z = Mathf.Round(position.z / gridUnit) * gridUnit;
        
        // Return the snapped position
        return position;
    }

        public void SetManipulationMode(ManipulationMode mode)
    {
        currentMode = mode;
    }

    /// <summary>
    /// Coroutine to reset the movement flag after a short delay.
    /// This is used to prevent continuous movement when the input is held down.
    /// </summary>
    IEnumerator ResetMovementFlag()
    {
        // Wait for a short delay (adjust as needed)
        yield return new WaitForSeconds(0.2f);
        
        // Reset the movement flag
        isObjectBeingMoved = false;
    }

    /// <summary>
    /// Copies the selected object or its parent.
    /// If the selected object is a child of an object named "SegmentsParent", the parent object is copied instead.
    /// </summary>
private void CopySelectedObject()
{
    // Check if copying is not already in progress
    if (!isCopying)
    {
        // Set the flag to prevent multiple copies
        isCopying = true;

        // Perform a raycast to get all the colliders hit
        RaycastHit[] hits;
        hits = Physics.RaycastAll(raycastSource.position, raycastSource.forward, Mathf.Infinity, interactableLayer);

        // Iterate through the hits to find the first valid object on the "Cube" layer
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.layer == LayerMask.NameToLayer("cube"))
            {
                selectedObject = hit.collider.gameObject;

                // Determine the object to copy (selected object or its parent if it's a child of "SegmentsParent")
                GameObject objectToCopy = selectedObject;
                if (selectedObject.transform.parent != null && selectedObject.transform.parent.name.Equals("SegmentsParent"))
                {
                    objectToCopy = selectedObject.transform.parent.gameObject;
                }

                // Create a copy of the object at a slight offset from the original position
                GameObject copiedObject = Instantiate(objectToCopy, objectToCopy.transform.position + new Vector3(gridUnit, 0, 0), objectToCopy.transform.rotation);

                // Set the name of the copied object based on the original object's name
                if (objectToCopy.name.StartsWith("SegmentsParent"))
                {
                    copiedObject.name = "SegmentsParent";
                }
                else
                {
                    copiedObject.name = objectToCopy.name.Replace("(Clone)", "");
                }

                break; // Exit the loop after copying the first valid object
            }
        }

        // Start a coroutine to reset the copying flag after a short delay
        StartCoroutine(ResetCopyingFlag());
    }
}



    /// <summary>
    /// Coroutine to reset the copying flag after a short delay.
    /// This is used to prevent multiple copies from being created simultaneously.
    /// </summary>
    IEnumerator ResetCopyingFlag()
    {
        // Wait for a short delay (adjust as needed)
        yield return new WaitForSeconds(0.1f);
        
        // Reset the copying flag
        isCopying = false;
    }
    public void ToggleMode()
    {
        isMoveMode = !isMoveMode;
    }

}