using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// class to control the UI
/// </summary>
/// <remarks>
/// Pressing start will toggle the tool bar to switch modes.
/// </remarks>
public class ModeController : MonoBehaviour
{

    public enum Mode
    {
        /// <summary>Mode for building objects.</summary>
        MODE_BUILD = 0,
        /// <summary>Mode for adjusting lights.</summary>
        MODE_LIGHT = 1,
        /// <summary>Mode for viewing scenes.</summary>
        MODE_VIEW = 2,
        /// <summary>Default mode with minimal interaction enabled.</summary>
        MODE_DEFAULT = 3,
        /// <summary>Mode specifically for constructing walls.</summary>
        MODE_WALL = 4,
        /// <summary>Mode for creating windows within walls.</summary>
        MODE_WINDOW = 5,
        /// <summary>Mode for copying existing objects (not implemented).</summary>
        MODE_COPY = 6,
        /// <summary>Mode for deleting objects.</summary>
        MODE_DELETE = 7,
        /// <summary>Mode for changing textures.</summary>
        MODE_TEXTURE = 8
    }


    /// <summary>Reference to the player's head transform for UI alignment.</summary>
    public Transform head;
    /// <summary>UI element that represents the full menu.</summary>
    public GameObject menuWhole;
    /// <summary>UI element that represents a partial menu, shown during specific modes.</summary>
    public GameObject menuPartial;
    /// <summary>Distance from the head transform at which spawned UI elements appear.</summary>
    public float SpawnDistance = 2;
    /// <summary>Objects associated with building mode.</summary>
    public GameObject[] buildingModeObjects;
    /// <summary>Objects associated with wall mode.</summary>
    public GameObject[] wallModeObjects;
    /// <summary>Objects associated with window mode.</summary>
    public GameObject[] windowModeObjects;
    /// <summary>Objects associated with lighting mode.</summary>
    public GameObject[] lightingModeObjects;
    /// <summary>Objects associated with viewing mode.</summary>
    public GameObject[] viewModeObjects;
    /// <summary>Objects associated with the default mode.</summary>
    public GameObject[] defaultModeObjects;
    /// <summary>Objects associated with the texture mode.</summary>
    public GameObject[] textureModeObjects;
    /// <summary>Manages object manipulation via raycasting.</summary>
    public RaycastObjectManipulator objectManipulator;

    /// <summary>
    /// Reference to a text object that will be changed to display building tool instructions
    /// </summary>
    public TMP_Text text;

    private Mode currentMode = Mode.MODE_DEFAULT; // 0 for building, 1 for lighting, 2 for view, 3 for default
    /// <summary>
    /// Initialize the UI by setting the current mode.
    /// </summary>
    void Start()
    {
        // Initialize to the building mode
        SetMode(currentMode);
        // SetNoMode();
    }
    /// <summary>
    /// Monitor user input to toggle the toolbar and switch modes.
    /// </summary>
    void Update()
    {
        // Check for the Menu button press on the left controller
        if (OVRInput.GetDown(OVRInput.Button.Start))
        {
            // Toggle the tool bar
            ToggleModes();
        }
        // menuWhole.transform.LookAt(new Vector3(head.position.x, menuWhole.transform.position.y, head.position.z));
    }
    /// <summary>
    /// Toggle the visibility of the partial menu and cycle through available modes.
    /// </summary>
    void ToggleModes()
    {
        // Toggle the tool bar
        menuPartial.SetActive(!menuPartial.activeSelf);
        // print if the menu is active, for debugging purposes
        Debug.Log(!menuPartial.activeSelf);
        // menuWhole.transform.position = head.position + new Vector3(head.forward.x, 0, head.forward.z).normalized * SpawnDistance;
        // Cycle through the modes
        // currentMode = (currentMode + 1) % 3;
        // SetMode(currentMode);
        // if the mode is set to build mode, pressing start again with switch to light mode
        // if (currentMode == 0)
        // {
        //     // change to default mode
        //     setDefault();
        // }
    }

    /// <summary>
    /// Returns the current mode
    /// </summary>
    /// <returns></returns>
    public Mode getMode()
    {
        return currentMode;
    }

    /// <summary>
    /// Change the mode to build mode
    /// </summary>
    public void setBuild()
    {
        Debug.Log("Build mode button clicked");
        if (currentMode != Mode.MODE_BUILD && currentMode != Mode.MODE_WALL && currentMode != Mode.MODE_WINDOW)

        {
            currentMode = Mode.MODE_BUILD;
            SetMode(Mode.MODE_BUILD);
        }
        else
        {
            setDefault();
        }
    }

    /// <summary>
    /// change the mode to view mode
    /// </summary>
    public void setViews()
    {
        if (currentMode != Mode.MODE_VIEW)
        {
            currentMode = Mode.MODE_VIEW;
            SetMode(Mode.MODE_VIEW);
        }
        else
        {
            setDefault();
        }
    }

    /// <summary>
    /// Change the mode to light mode
    /// </summary>
    public void setLights()
    {
        if (currentMode != Mode.MODE_LIGHT)
        {
            currentMode = Mode.MODE_LIGHT;
            SetMode(Mode.MODE_LIGHT);
        }
        else
        {
            setDefault();
        }
    }

    /// <summary>
    /// Change the mode to texture mode
    /// </summary>
    public void setTexture()
    {
        if (currentMode != Mode.MODE_TEXTURE)
        {
            currentMode = Mode.MODE_TEXTURE;
            SetMode(Mode.MODE_TEXTURE);
        }
        else
        {
            setDefault();
        }
    }

    /// <summary>
    /// Remove all UI elements
    /// </summary>
    public void setDefault()
    {
        currentMode = Mode.MODE_DEFAULT;
        SetMode(Mode.MODE_DEFAULT);
    }
    /// <summary>
    /// Makes it so you can only create walls within build mode
    /// </summary>
    public void setWallMode()
    {
        if (currentMode != Mode.MODE_WALL)
        {
            currentMode = Mode.MODE_WALL;
            SetMode(Mode.MODE_WALL);
            SetNoMode();
            text.text = "Trigger press to make a wall \n Move the controller to shape \n Trigger press to finish";
        }
    }
    /// <summary>
    /// Makes it so you can only create windows within build mode
    /// </summary>
    public void setWindowMode()
    {
        if (currentMode != Mode.MODE_WINDOW)
        {
            currentMode = Mode.MODE_WINDOW;
            SetMode(Mode.MODE_WINDOW);
            SetNoMode();
            text.text = "Trigger press to draw windows";
        }
    }
    /// <summary>
    /// Testing for only deleting
    /// </summary>
    public void SetDeleteMode()
    {
        if (currentMode != Mode.MODE_DELETE)
        {
            currentMode = Mode.MODE_DELETE;
            Debug.Log("Delete mode activated");
            DisableWallAndWindowObjects();
            objectManipulator.SetDeleteMode();
            text.text = "Trigger press to delete";
        }
    }

    /// <summary>
    /// Allows Copying in build mode
    /// </summary>
    public void SetCopyMode()
    {
        if (currentMode != Mode.MODE_COPY)
        {
            currentMode = Mode.MODE_COPY;
            Debug.Log("Copy mode activated");
            DisableWallAndWindowObjects();
            objectManipulator.SetCopyMode();
            text.text = "Trigger press to copy";
        }
    }

    /// <summary>
    /// Deactivates everything in build mode and calls a flag to allow movement
    /// </summary>
    public void SetMoveMode()
    {
        Debug.Log("Move mode activated");
        DisableWallAndWindowObjects();
        objectManipulator.SetMoveMode();
        text.text = "Trigger press and joystick: move up/down, left/right \n Grip press and joystick: move forward/back";
    }
    /// <summary>
    /// Deactivates everything in build mode and calls a flag to allow rotation
    /// </summary>
    public void SetRotateMode()
    {
        Debug.Log("Rotate mode activated");
        DisableWallAndWindowObjects();
        objectManipulator.SetRotateMode();
        text.text = "Trigger press and joystick: rotate up/down, left/right \n Grip press and joystick: rotate sideways";
    }
    /// <summary>
    /// Deactivates move mode
    /// </summary>
    public void SetNoMode()
    {
        Debug.Log("No mode selected");
        objectManipulator.SetNoMode();
    }
    /// <summary>
    /// Called during move mode to disable the ability to create walls or windows
    /// </summary>
    private void DisableWallAndWindowObjects()
    {
        foreach (var obj in wallModeObjects)
        {
            obj.SetActive(false);
        }

        foreach (var obj in windowModeObjects)
        {
            obj.SetActive(false);
        }
    }

    /// <summary>
    /// Changes the active objects in the hierarchy based on what mode is selected
    /// </summary>
    /// <param name="mode"></param>
    private void SetMode(Mode mode)
    {
        // Handle existing modes
        foreach (var obj in buildingModeObjects)
        {
            // Building mode objects remain active for Wall and Window modes
            obj.SetActive(mode == Mode.MODE_BUILD || mode == Mode.MODE_WALL || mode == Mode.MODE_WINDOW);
        }
        foreach (var obj in lightingModeObjects)
        {
            obj.SetActive(mode == Mode.MODE_LIGHT);
        }
        foreach (var obj in viewModeObjects)
        {
            obj.SetActive(mode == Mode.MODE_VIEW);
        }
        foreach (var obj in defaultModeObjects)
        {
            obj.SetActive(mode == Mode.MODE_DEFAULT);
        }

        foreach (var obj in textureModeObjects)
        {
            obj.SetActive(mode == Mode.MODE_TEXTURE);
        }

        // Handle new Wall mode objects
        foreach (var obj in wallModeObjects)
        {
            obj.SetActive(mode == Mode.MODE_WALL);
        }

        // Handle new Window mode objects
        foreach (var obj in windowModeObjects)
        {
            obj.SetActive(mode == Mode.MODE_WINDOW);
        }

        // Deselect both move and rotate modes if not in Build mode
        if (mode != Mode.MODE_BUILD)
        {
            SetNoMode();
        }
    }


}

