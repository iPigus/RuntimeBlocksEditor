using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System.Collections;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Main coordinator for the Gizmo system, delegating to specialized tool components.
    /// </summary>
    public class GizmoTools : MonoBehaviour
    {
        // Serialized fields
        [SerializeField] public LayerMask maskSel;
        [SerializeField] private float gridSnapb = 1f;

        // Public properties
        public GameObject selected;
        public GameObject[] controlObjects;
        private Vector3[] objectOffsets;
        
        // Components
        private GizmoController controller;
        private GizmoHistory history;
        private GizmoVisuals visuals;
        
        // Tool components
        private GizmoMoveTool moveTool;
        private GizmoRotateTool rotateTool;
        private GizmoScaleTool scaleTool;
        private GizmoSelectTool selectTool;
        private GizmoDeleteTool deleteTool;
        private GizmoGroupTool groupTool;
        private GizmoHoverManager hoverManager;
        private MultipleSelectionTool multipleSelectionTool;
        
        // Active tool tracking
        private int currentToolType = 0;
        private int previousToolType = 0;
        
        // Selection handling
        private float selectionCooldown = 0f;
        private GameObject lastSelectedObject = null;
        private bool wasMultiSelecting = false;
        
        // Static instance
        public static GizmoTools Singleton;
        
        private void Awake()
        {
            Singleton = this;
            
            // Get required components
            controller = GetComponent<GizmoController>();
            history = GetComponent<GizmoHistory>();
            visuals = GetComponent<GizmoVisuals>();
            
            // Get tool components
            moveTool = GetComponent<GizmoMoveTool>();
            rotateTool = GetComponent<GizmoRotateTool>();
            scaleTool = GetComponent<GizmoScaleTool>();
            selectTool = GetComponent<GizmoSelectTool>();
            deleteTool = GetComponent<GizmoDeleteTool>();
            groupTool = GetComponent<GizmoGroupTool>();
            hoverManager = GetComponent<GizmoHoverManager>();
            multipleSelectionTool = GetComponent<MultipleSelectionTool>();
            
            // Add any missing tool components
            if (moveTool == null) moveTool = gameObject.AddComponent<GizmoMoveTool>();
            if (rotateTool == null) rotateTool = gameObject.AddComponent<GizmoRotateTool>();
            if (scaleTool == null) scaleTool = gameObject.AddComponent<GizmoScaleTool>();
            if (selectTool == null) selectTool = gameObject.AddComponent<GizmoSelectTool>();
            if (deleteTool == null) deleteTool = gameObject.AddComponent<GizmoDeleteTool>();
            if (groupTool == null) groupTool = gameObject.AddComponent<GizmoGroupTool>();
            if (hoverManager == null) hoverManager = gameObject.AddComponent<GizmoHoverManager>();
            
        }
        
        private void Start()
        {
            // Initialize with the first tool
            UpdateActiveTool(0);
        }
        
        private void Update()
        {
            // Decrease selection cooldown if active
            if (selectionCooldown > 0)
            {
                selectionCooldown -= Time.deltaTime;
            }
            
            // Update the current active tool
            UpdateActiveTool(currentToolType);
        }
        
        /// <summary>
        /// Sets the active tool type
        /// </summary>
        public void SetActiveTool(int toolType)
        {
            previousToolType = currentToolType;
            currentToolType = toolType;
            
            // Trigger tool change event
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.TriggerToolChanged(previousToolType, currentToolType);
            }
        }
        
        /// <summary>
        /// Updates the active tool functionality
        /// </summary>
        public void UpdateActiveTool(int toolType)
        {
            // Do nothing if no camera is available
            if (!Camera.main) return;
            
            // Store the current tool type
            currentToolType = toolType;
            
            // Handle the active tool
            switch (toolType)
            {
                case 0:
                    moveTool.HandleMoveTool();
                    break;
                case 1:
                    rotateTool.HandleRotateTool();
                    break;
                case 2:
                    scaleTool.HandleScaleTool();
                    break;
                case 3:
                    selectTool.HandleSelectTool();
                    break;
                case 5:
                    deleteTool.HandleDeleteTool();
                    break;
            }
            
            // Handle hover highlighting for all tools except delete
            if (toolType != 5) // Don't show hover effects for delete tool
            {
                hoverManager.HandleHoverHighlighting();
            }
        }
        
        /// <summary>
        /// Updates the list of selected objects.
        /// </summary>
        public void UpdateSelectedObjects()
        {
            Vector3 newPos = Vector3.zero;
            GameObject[] tags = GameObject.FindGameObjectsWithTag(controller.tagToFind);
            
            int tagsCount = 0;
            GameObject[] tagsObj = new GameObject[tags.Length];
            
            foreach (GameObject theobj in tags)
            {
                BlockToEdit blockToEdit = theobj.GetComponent<BlockToEdit>();
                if (blockToEdit != null && blockToEdit.selected)
                {
                    tagsObj[tagsCount] = theobj;
                    tagsCount += 1;
                }
            }
            
            if (tagsCount != 0)
            {
                // Keep track of previously selected objects to prevent accidental deselection
                // during quick transform changes
                bool controlObjectsChanged = false;
                
                // Only update the control objects if they've changed
                if (controlObjects == null || controlObjects.Length != tagsCount)
                {
                    controlObjectsChanged = true;
                }
                else
                {
                    // Check if the objects are different
                    for (int i = 0; i < tagsCount; i++)
                    {
                        if (i >= controlObjects.Length || controlObjects[i] != tagsObj[i])
                        {
                            controlObjectsChanged = true;
                            break;
                        }
                    }
                }
                
                // If objects have changed, update the control objects array
                if (controlObjectsChanged)
                {
                    controlObjects = new GameObject[tagsCount];
                    objectOffsets = new Vector3[tagsCount];
                    
                    for (int i = 0; i < tagsCount; i++)
                    {
                        controlObjects[i] = tagsObj[i];
                    }
                }
                else if (objectOffsets == null || objectOffsets.Length != tagsCount)
                {
                    // Just update the offsets if they're missing
                    objectOffsets = new Vector3[tagsCount];
                }
                
                // Calculate the center position of all selected objects
                for (int i = 0; i < tagsCount; i++)
                {
                    if (i == 0)
                    {
                        newPos = controlObjects[i].transform.position;
                    }
                    else
                    {
                        newPos += controlObjects[i].transform.position;
                    }
                }
                
                // Calculate the center of selection
                newPos = newPos / tagsCount;
                
                // Set Gizmo position to the center of selection
                transform.position = newPos;
                
                // Update offsets
                for (int i = 0; i < controlObjects.Length; i++)
                {
                    if (controlObjects[i] != null)
                    {
                        objectOffsets[i] = transform.position - controlObjects[i].transform.position;
                    }
                }
            }
            else if (controlObjects != null && controlObjects.Length > 0)
            {
                // Check if any of the previously selected objects still exist
                // This prevents deselection during quick transform operations
                bool anyObjectsStillValid = false;
                foreach (GameObject obj in controlObjects)
                {
                    if (obj != null)
                    {
                        BlockToEdit blockToEdit = obj.GetComponent<BlockToEdit>();
                        if (blockToEdit != null)
                        {
                            // If the object exists but is not marked as selected, re-select it
                            if (!blockToEdit.selected)
                            {
                                blockToEdit.selected = true;
                                blockToEdit.EnableOutline();
                                anyObjectsStillValid = true;
                            }
                            else
                            {
                                anyObjectsStillValid = true;
                            }
                        }
                    }
                }
                
                // Only clear selection if all objects are truly gone
                if (!anyObjectsStillValid)
                {
                    controlObjects = null;
                    objectOffsets = null;
                }
                else
                {
                    // Re-run the update to refresh with the corrected selection
                    UpdateSelectedObjects();
                }
            }
            else
            {
                controlObjects = null;
                objectOffsets = null;
            }
        }
        
        /// <summary>
        /// Deselects all blocks in the scene and clears the control objects array.
        /// </summary>
        public void DeselectAllBlocks()
        {
            foreach (var blockToEdit in BlockToEdit.AllObjects)
            {
                if (blockToEdit != null)
                {
                    blockToEdit.selected = false;
                    blockToEdit.DisableOutline();
                    blockToEdit.DisableHoverOutline();
                }
            }
            
            controlObjects = new GameObject[0];
        }
        
        /// <summary>
        /// Unified block selection handler used by all tools
        /// </summary>
        /// <param name="toolName">The name of the tool calling this method, for logging</param>
        /// <returns>True if a block was hit, false otherwise</returns>
        public bool HandleBlockSelection(string toolName = "")
        {
            if (!Camera.main) return false;
            
            // Skip if over UI elements
            if (UnityEngine.EventSystems.EventSystem.current && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return false;
            }
            
            // Only process mouse clicks, not every frame
            bool mouseClicked = Input.GetMouseButtonDown(0);
            if (!mouseClicked) return false;
            
            // Try to select a block
            Ray rayS = Camera.main.ScreenPointToRay(Input.mousePosition);
            bool hitBlock = false;
            
            if (Physics.Raycast(rayS, out RaycastHit hitBlockInfo, Mathf.Infinity, controller.maskBlocks))
            {
                if (hitBlockInfo.transform.CompareTag(controller.tagToFind))
                {
                    hitBlock = true;
                    BlockToEdit blockToEdit = hitBlockInfo.transform.GetComponent<BlockToEdit>();
                    if (blockToEdit != null)
                    {
                        // Check if we're using RuntimeBlocksManager
                        bool usingBlocksManager = RuntimeBlocksManager.Instance != null;
                        
                        // Check for selection cooldown to prevent double-processing
                        if (selectionCooldown > 0 && blockToEdit.gameObject == lastSelectedObject)
                        {
                            return hitBlock;
                        }
                        
                        // Set cooldown to prevent this object from being processed again immediately
                        selectionCooldown = 0.5f; // Half-second cooldown
                        lastSelectedObject = blockToEdit.gameObject;
                        
                        if (!string.IsNullOrEmpty(toolName))
                            Debug.Log($"Selected block with {toolName} tool: {blockToEdit.gameObject.name}");
                        else
                            Debug.Log($"Selected block: {blockToEdit.gameObject.name}");
                        
                        // Store current selection state before modification to avoid toggling twice
                        bool wasSelected = blockToEdit.selected;
                        
                        // Check if multi-selection key is pressed
                        bool isMultiSelecting = Input.GetKey(controller.selectMultipleCode);
                        wasMultiSelecting = isMultiSelecting;
                        
                        if (!isMultiSelecting)
                        {
                            // If shift is not pressed, deselect all other objects first
                            
                            // When using RuntimeBlocksManager
                            if (usingBlocksManager)
                            {
                                // Clear selection and add the new selection without triggering events
                                RuntimeBlocksManager.Instance.ClearSelection();
                                blockToEdit.selected = true;
                                blockToEdit.EnableOutline();
                                RuntimeBlocksManager.Instance.AddSelectedObject(blockToEdit.gameObject, false);
                                
                                // Manually fire the event only once
                                if (GizmoEvents.Instance != null)
                                {
                                    // We won't trigger individual events since they'd cause double selection
                                    GizmoEvents.Instance.TriggerSelectionUpdated(new GameObject[] { blockToEdit.gameObject });
                                }
                            }
                            else
                            {
                                // Handle without RuntimeBlocksManager - deselect all other objects
                                foreach (var obj in BlockToEdit.AllObjects)
                                {
                                    if (obj != null && obj != blockToEdit)
                                    {
                                        bool objWasSelected = obj.selected;
                                        obj.selected = false;
                                        obj.DisableOutline();
                                        obj.DisableHoverOutline();
                                        
                                        // Trigger deselect event
                                        if (objWasSelected && GizmoEvents.Instance != null)
                                        {
                                            GizmoEvents.Instance.TriggerObjectDeselected(obj.gameObject);
                                        }
                                    }
                                }
                                
                                // Always select the clicked object
                                if (!blockToEdit.selected)
                                {
                                    blockToEdit.selected = true;
                                    blockToEdit.EnableOutline();
                                    
                                    // Trigger select event
                                    if (GizmoEvents.Instance != null)
                                    {
                                        GizmoEvents.Instance.TriggerObjectSelected(blockToEdit.gameObject);
                                    }
                                }
                            }
                        }
                        else
                        {
                            // If multi-selection key is pressed, toggle the selection of just this object
                            
                            // When using RuntimeBlocksManager
                            if (usingBlocksManager)
                            {
                                // Toggle selection without triggering extra events
                                if (wasSelected)
                                {
                                    // If already selected, remove it from selection
                                    blockToEdit.selected = false;
                                    blockToEdit.DisableOutline();
                                    blockToEdit.DisableHoverOutline();
                                    RuntimeBlocksManager.Instance.RemoveSelectedObject(blockToEdit.gameObject, true);
                                    
                                    // Trigger just one event with the current selection
                                    if (GizmoEvents.Instance != null)
                                    {
                                        // Update with the current selection state after removal
                                        GameObject[] currentSelection = RuntimeBlocksManager.Instance.GetSelectedObjects();
                                        GizmoEvents.Instance.TriggerSelectionUpdated(currentSelection);
                                    }
                                }
                                else
                                {
                                    // If not selected, add it to selection
                                    blockToEdit.selected = true;
                                    blockToEdit.EnableOutline();
                                    RuntimeBlocksManager.Instance.AddSelectedObject(blockToEdit.gameObject, true);
                                    
                                    // Trigger just one event with the current selection
                                    if (GizmoEvents.Instance != null)
                                    {
                                        // Update with the current selection state after addition
                                        GameObject[] currentSelection = RuntimeBlocksManager.Instance.GetSelectedObjects();
                                        GizmoEvents.Instance.TriggerSelectionUpdated(currentSelection);
                                    }
                                }
                            }
                            else
                            {
                                // Toggle selection without RuntimeBlocksManager
                                blockToEdit.selected = !blockToEdit.selected;
                                
                                if (blockToEdit.selected)
                                {
                                    blockToEdit.EnableOutline();
                                    
                                    // Trigger select event
                                    if (GizmoEvents.Instance != null)
                                    {
                                        GizmoEvents.Instance.TriggerObjectSelected(blockToEdit.gameObject);
                                    }
                                }
                                else
                                {
                                    blockToEdit.DisableOutline();
                                    blockToEdit.DisableHoverOutline();
                                    
                                    // Trigger deselect event
                                    if (GizmoEvents.Instance != null)
                                    {
                                        GizmoEvents.Instance.TriggerObjectDeselected(blockToEdit.gameObject);
                                    }
                                }
                            }
                        }
                        
                        // If we're not using RuntimeBlocksManager, update control objects ourselves
                        if (!usingBlocksManager)
                        {
                            // Update control objects and Gizmo position
                            UpdateSelectedObjects();
                        }
                        else
                        {
                            // Force sync with RuntimeBlocksManager's selection
                            controlObjects = RuntimeBlocksManager.Instance.GetSelectedObjects();
                        }
                        
                        // If this is the select tool, ensure gizmos remain hidden
                        if (toolName == "select")
                        {
                            visuals.SetGizmoVisibility(false, false, false);
                        }
                    }
                }
            }
            
            // If clicked in empty space and not holding shift, deselect all
            // Also check that we're not clicking on UI elements
            if (!hitBlock && !Input.GetKey(controller.selectMultipleCode) &&
                !(UnityEngine.EventSystems.EventSystem.current && 
                  UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()))
            {
                // Reset selection cooldown and last selected object
                selectionCooldown = 0f;
                lastSelectedObject = null;
                
                var previouslySelected = controlObjects != null ? controlObjects.ToArray() : null;
                
                // If we're using RuntimeBlocksManager, let it handle deselection
                if (RuntimeBlocksManager.Instance != null)
                {
                    RuntimeBlocksManager.Instance.ClearSelection();
                    
                    // Clear visual selection states on all objects for certainty
                    foreach (var blockToEdit in BlockToEdit.AllObjects)
                    {
                        if (blockToEdit != null)
                        {
                            blockToEdit.selected = false;
                            blockToEdit.DisableOutline();
                            blockToEdit.DisableHoverOutline();
                        }
                    }
                    
                    // Force reset control objects to empty array
                    controlObjects = new GameObject[0];
                    
                    // Trigger selection updated (to empty array)
                    if (GizmoEvents.Instance != null)
                    {
                        GizmoEvents.Instance.TriggerSelectionUpdated(new GameObject[0]);
                    }
                    
                    Debug.Log("Cleared selection by clicking in empty space");
                }
                else
                {
                    DeselectAllBlocks();
                    
                    // Trigger deselect events for all previously selected objects
                    if (GizmoEvents.Instance != null && previouslySelected != null)
                    {
                        foreach (var obj in previouslySelected)
                        {
                            if (obj != null)
                            {
                                GizmoEvents.Instance.TriggerObjectDeselected(obj);
                            }
                        }
                        
                        // Trigger selection updated (to empty array)
                        GizmoEvents.Instance.TriggerSelectionUpdated(new GameObject[0]);
                    }
                    
                    Debug.Log("Cleared selection by clicking in empty space");
                }
            }
            
            return hitBlock;
        }
        
        /// <summary>
        /// Gets the offset for a controlled object
        /// </summary>
        public Vector3 GetObjectOffset(int index)
        {
            if (objectOffsets != null && index >= 0 && index < objectOffsets.Length)
                return objectOffsets[index];
            
            return Vector3.zero;
        }
        
        /// <summary>
        /// Sets the list of control objects
        /// </summary>
        public void SetControlObjects(GameObject[] objects)
        {
            controlObjects = objects;
        }
        
        /// <summary>
        /// Returns the array of currently controlled objects.
        /// </summary>
        public GameObject[] GetSelectedObjects()
        {
            return controlObjects;
        }
        
        /// <summary>
        /// Duplicates the currently selected objects and selects the new copies.
        /// </summary>
        public void DuplicateSelectedObjects()
        {
            if (controlObjects == null || controlObjects.Length == 0)
                return;
                
            // Keep a reference to original objects
            GameObject[] originalObjects = controlObjects.ToArray();
            List<GameObject> duplicatedObjects = new List<GameObject>();
            
            // Save state for history before duplication
            if (history.historyEnabled)
                history.SaveState(controlObjects, GizmoHistory.HistoryActionType.Creation);
            
            // Deselect all current objects
            foreach (var obj in originalObjects)
                        {
                            if (obj != null)
                            {
                    BlockToEdit blockToEdit = obj.GetComponent<BlockToEdit>();
                    if (blockToEdit != null)
                    {
                        blockToEdit.selected = false;
                        blockToEdit.DisableOutline();
                    }
                }
            }
            
            // Create duplicates
            foreach (var obj in originalObjects)
                        {
                            if (obj != null)
                            {
                    // Create duplicate with offset
                    GameObject duplicate = Instantiate(obj, 
                        obj.transform.position + new Vector3(1f, 0f, 0f), 
                        obj.transform.rotation);
                        
                    duplicate.name = obj.name + " (Copy)";
                    
                    // Set the tag to match the original
                    duplicate.tag = controller.tagToFind;
                    
                    // Select the new object
                    BlockToEdit blockToEdit = duplicate.GetComponent<BlockToEdit>();
                            if (blockToEdit != null)
                            {
                        blockToEdit.selected = true;
                        blockToEdit.EnableOutline();
                    }
                    
                    duplicatedObjects.Add(duplicate);
                }
            }
            
            // Update control objects to select the new duplicates
                                UpdateSelectedObjects();
            
            // Trigger duplication event
            if (GizmoEvents.Instance != null && duplicatedObjects.Count > 0)
            {
                GizmoEvents.Instance.TriggerObjectsDuplicated(originalObjects, duplicatedObjects.ToArray());
            }
        }
        
        // Public methods to delegate to the appropriate tool
        public void QuickRotateObjects() => rotateTool.QuickRotateObjects();
        public void GroupSelectedObjects() => groupTool.GroupSelectedObjects();
        public void UngroupSelectedObjects() => groupTool.UngroupSelectedObjects();
        
        /// <summary>
        /// Checks if an object is a child of a specific group
        /// </summary>
        private bool IsChildOfGroup(GameObject obj, GameObject groupObj)
        {
            if (obj == null || groupObj == null)
                return false;
                
            Transform parent = obj.transform.parent;
            while (parent != null)
            {
                if (parent.gameObject == groupObj)
                    return true;
                    
                parent = parent.parent;
            }
            
            return false;
        }
    }
} 