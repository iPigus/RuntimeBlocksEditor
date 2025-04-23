using UnityEngine;
using System.Collections.Generic;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Implements rectangular box selection for selecting multiple objects at once.
    /// Works independently of the active tool.
    /// </summary>
    public class MultipleSelectionTool : MonoBehaviour
    {
        //references   
        private GizmoController controller => GizmoController.Singleton;
        private GizmoTools tools => GizmoTools.Singleton;
        [Header("Settings")]
        [SerializeField] private Color selectionBoxColor = new Color(0.5f, 0.5f, 1f, 0.3f);
        [SerializeField] private Color selectionBoxBorderColor = new Color(0.5f, 0.5f, 1f, 0.8f);
        [SerializeField] private float borderThickness = 1f;
        [SerializeField] private KeyCode cancelSelectionKey = KeyCode.Escape;
        [SerializeField] private float selectionThreshold = 0.5f; // Percentage of points that must be inside the box
        
        // Selection box state
        private bool isDrawingSelectionBox = false;
        private Vector2 selectionBoxStart;
        private Vector2 selectionBoxEnd;
        
        // Public property to check if selection box is active
        public bool IsSelectionBoxActive => isDrawingSelectionBox;
        
        // Visual elements
        private Texture2D selectionBoxTexture;
        private GUIStyle selectionBoxStyle;
        
        // Public method to toggle selection preview mode
        public void SetPreviewHighlightingEnabled(bool enabled)
        {
            // For this project, we always want the preview highlighting disabled
            // Override any attempts to enable it
            previewSelectionEnabled = false;
            
            // Clear all highlights from non-selected objects when disabling
            if (isDrawingSelectionBox)
            {
                DisableHighlightingOnAllObjects();
            }
        }
        
        // Flag to control preview highlighting behavior
        private bool previewSelectionEnabled = true;
        
        private void Awake()
        {
            // Initialize selection box visual elements
            InitializeVisuals();
        }
        
        private void Start()
        {
            // Ensure we have all required references
            if (controller == null || tools == null)
            {
                Debug.LogError("MultipleSelectionTool: Missing controller or tools reference");
            }
        }
        
        private void InitializeVisuals()
        {
            // Create a white texture that we'll tint with GUI.color
            selectionBoxTexture = new Texture2D(1, 1);
            selectionBoxTexture.SetPixel(0, 0, Color.white);
            selectionBoxTexture.Apply();
            
            selectionBoxStyle = new GUIStyle();
            selectionBoxStyle.normal.background = selectionBoxTexture;
        }
        
        private void Update()
        {
            // Don't process when over UI elements
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return;
                
            // Cancel selection with Escape key
            if (Input.GetKeyDown(cancelSelectionKey) && isDrawingSelectionBox)
            {
                isDrawingSelectionBox = false;
                return;
            }
            
            // Get current mouse position
            Vector2 currentMousePos = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
            
            // Begin selection box on left mouse button down in empty space
            if (Input.GetMouseButtonDown(0) && !isDrawingSelectionBox)
            {
                // Check if we clicked on a specific object first
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                bool hitObject = Physics.Raycast(ray, out RaycastHit hitInfo) && 
                                hitInfo.transform.CompareTag(controller.tagToFind);
                
                // Also check if we hit a gizmo element
                bool hitGizmo = tools.selected != null;
                
                // If we didn't hit an object or gizmo, start selection box
                if (!hitObject && !hitGizmo)
                {
                    // Start selection box
                    isDrawingSelectionBox = true;
                    selectionBoxStart = currentMousePos;
                    selectionBoxEnd = selectionBoxStart;
                    
                    // Initialize highlighting of objects that would be selected
                    PreviewSelectionHighlighting();
                }
            }
            
            // Update selection box while dragging
            if (isDrawingSelectionBox)
            {
                // Continuously update the end position to follow the mouse
                selectionBoxEnd = currentMousePos;
                
                // Preview which objects would be selected
                PreviewSelectionHighlighting();
                
                // Complete selection box on mouse up
                if (Input.GetMouseButtonUp(0))
                {
                    // Only process the selection if the box has a reasonable size
                    float minSize = 5f; // Minimum size in pixels to consider the box valid
                    if (Vector2.Distance(selectionBoxStart, selectionBoxEnd) > minSize)
                    {
                        SelectObjectsInSelectionBox();
                    }
                    
                    // Reset selection box
                    isDrawingSelectionBox = false;
                }
            }
        }
        
        private void OnGUI()
        {
            if (isDrawingSelectionBox)
            {
                // Preview which objects would be selected
                PreviewSelectionHighlighting();
                
                // Calculate the rectangle in GUI space
                // GUI coordinates have (0,0) at top-left, and Input.mousePosition has (0,0) at bottom-left
                float left = Mathf.Min(selectionBoxStart.x, selectionBoxEnd.x);
                float right = Mathf.Max(selectionBoxStart.x, selectionBoxEnd.x);
                
                // Invert the Y coordinates for GUI space
                float bottom = Mathf.Min(Screen.height - selectionBoxStart.y, Screen.height - selectionBoxEnd.y);
                float top = Mathf.Max(Screen.height - selectionBoxStart.y, Screen.height - selectionBoxEnd.y);
                
                // Create the correct GUI rect
                Rect selectionRect = new Rect(left, bottom, right - left, top - bottom);
                
                // Draw the box with fill color
                GUI.color = selectionBoxColor;
                GUI.Box(selectionRect, "", selectionBoxStyle);
                
                // Draw a border around the box
                DrawScreenRectBorder(selectionRect, borderThickness, selectionBoxBorderColor);
                
                // Debug display
                if (Debug.isDebugBuild)
                {
                    GUI.color = Color.white;
                    string debugText = string.Format(
                        "Start: ({0:F0}, {1:F0}), End: ({2:F0}, {3:F0}), Rect: ({4:F0}, {5:F0}, {6:F0}, {7:F0})", 
                        selectionBoxStart.x, selectionBoxStart.y, 
                        selectionBoxEnd.x, selectionBoxEnd.y,
                        selectionRect.x, selectionRect.y, selectionRect.width, selectionRect.height
                    );
                    GUI.Label(new Rect(10, 10, 500, 20), debugText);
                }
            }
        }
        
        private Rect GetScreenRect(Vector2 start, Vector2 end)
        {
            // Ensure start and end are correctly ordered
            float left = Mathf.Min(start.x, end.x);
            float right = Mathf.Max(start.x, end.x);
            float top = Mathf.Min(start.y, end.y);
            float bottom = Mathf.Max(start.y, end.y);
            
            // Create the rectangle with the correct dimensions
            return new Rect(left, top, right - left, bottom - top);
        }
        
        private void DrawScreenRectBorder(Rect rect, float thickness, Color color)
        {
            // Draw borders as four lines
            GUI.color = color;
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, rect.width, thickness), selectionBoxTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMin, thickness, rect.height), selectionBoxTexture);
            GUI.DrawTexture(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), selectionBoxTexture);
            GUI.DrawTexture(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), selectionBoxTexture);
        }
        
        // Check if this object is in a group, and if so, select the group parent instead
        private bool SelectGroupParentIfNeeded(BlockToEdit blockToEdit)
        {
            if (blockToEdit == null) return false;
            
            // Always try to select the group parent if this is part of a group
            return blockToEdit.SelectGroupParentIfPartOfGroup();
        }
        
        private void SelectObjectsInSelectionBox()
        {
            // Deselect all if not holding the multi-select key (Shift)
            if (!Input.GetKey(controller.selectMultipleCode))
            {
                // Deselect all currently selected objects
                foreach (var obj in BlockToEdit.AllObjects)
                {
                    if (obj != null && obj.selected)
                    {
                        obj.selected = false;
                        obj.DisableOutline();
                        
                        // Trigger deselect event
                        if (GizmoEvents.Instance != null)
                        {
                            GizmoEvents.Instance.TriggerObjectDeselected(obj.gameObject);
                        }
                    }
                }
            }
            
            // Calculate selection area in screen space
            float minX = Mathf.Min(selectionBoxStart.x, selectionBoxEnd.x);
            float maxX = Mathf.Max(selectionBoxStart.x, selectionBoxEnd.x);
            float minY = Mathf.Min(selectionBoxStart.y, selectionBoxEnd.y);
            float maxY = Mathf.Max(selectionBoxStart.y, selectionBoxEnd.y);
            
            // Track which objects are newly selected
            List<GameObject> newlySelectedObjects = new List<GameObject>();
            
            // Track which objects we've already handled (to avoid selecting a child and its group)
            HashSet<BlockToEdit> processedObjects = new HashSet<BlockToEdit>();
            
            // Check all objects with the target tag
            foreach (var obj in BlockToEdit.AllObjects)
            {
                if (obj != null && !processedObjects.Contains(obj))
                {
                    // Skip if this is a child object in a group (we'll select the group instead)
                    if (SelectGroupParentIfNeeded(obj))
                    {
                        // The parent was selected, mark this object as processed
                        processedObjects.Add(obj);
                        continue;
                    }
                    
                    // Get the renderer component to check bounds
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer == null) 
                    {
                        // If no renderer, fall back to checking center point
                        Vector3 screenPos = Camera.main.WorldToScreenPoint(obj.transform.position);
                        bool isInSelectionBox = 
                            screenPos.x >= minX && screenPos.x <= maxX &&
                            screenPos.y >= minY && screenPos.y <= maxY;
                            
                        if (isInSelectionBox && !obj.selected)
                        {
                            obj.selected = true;
                            obj.EnableOutline();
                            newlySelectedObjects.Add(obj.gameObject);
                            processedObjects.Add(obj);
                            
                            if (GizmoEvents.Instance != null)
                            {
                                GizmoEvents.Instance.TriggerObjectSelected(obj.gameObject);
                            }
                        }
                        continue;
                    }
                    
                    // For objects with renderers, check how much of the object is inside the selection box
                    Bounds bounds = renderer.bounds;
                    
                    // Sample points around the object's bounds (8 corners plus center)
                    Vector3[] testPoints = new Vector3[]
                    {
                        bounds.min,                                              // Bottom-left-back
                        new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),   // Bottom-left-front
                        new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),   // Top-left-back
                        new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),   // Top-left-front
                        new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),   // Bottom-right-back
                        new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),   // Bottom-right-front
                        new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),   // Top-right-back
                        bounds.max,                                              // Top-right-front
                        bounds.center                                            // Center
                    };
                    
                    // Count how many points are inside the selection box
                    int pointsInside = 0;
                    foreach (Vector3 point in testPoints)
                    {
                        Vector3 screenPoint = Camera.main.WorldToScreenPoint(point);
                        if (screenPoint.x >= minX && screenPoint.x <= maxX &&
                            screenPoint.y >= minY && screenPoint.y <= maxY)
                        {
                            pointsInside++;
                        }
                    }
                    
                    // Calculate percentage of points inside
                    float percentInside = (float)pointsInside / testPoints.Length;
                    
                    // Select the object if enough of it is inside the selection box
                    if (percentInside >= selectionThreshold && !obj.selected)
                    {
                        obj.selected = true;
                        obj.EnableOutline();
                        newlySelectedObjects.Add(obj.gameObject);
                        processedObjects.Add(obj);
                        
                        // Trigger select event
                        if (GizmoEvents.Instance != null)
                        {
                            GizmoEvents.Instance.TriggerObjectSelected(obj.gameObject);
                        }
                    }
                }
            }
            
            // Update the gizmo's selected objects
            tools.UpdateSelectedObjects();
            
            // Trigger selection updated event
            if (GizmoEvents.Instance != null && tools.GetSelectedObjects() != null)
            {
                GizmoEvents.Instance.TriggerSelectionUpdated(tools.GetSelectedObjects());
            }
        }
        
        // Disable highlighting on all non-selected objects to prevent distraction during selection
        private void DisableHighlightingOnAllObjects()
        {
            if (BlockToEdit.AllObjects == null) return;
            
            foreach (var obj in BlockToEdit.AllObjects)
            {
                if (obj != null && !obj.selected)
                {
                    obj.DisableOutline();
                }
            }
        }
        
        /// <summary>
        /// Previews which objects would be selected in the selection box
        /// </summary>
        private void PreviewSelectionHighlighting()
        {
            if (!isDrawingSelectionBox || ShouldSkipPreview())
                return;
                
            // Calculate selection area in screen space
            float minX = Mathf.Min(selectionBoxStart.x, selectionBoxEnd.x);
            float maxX = Mathf.Max(selectionBoxStart.x, selectionBoxEnd.x);
            float minY = Mathf.Min(selectionBoxStart.y, selectionBoxEnd.y);
            float maxY = Mathf.Max(selectionBoxStart.y, selectionBoxEnd.y);
            
            // Preview selection for objects
            foreach (var blockToEdit in BlockToEdit.AllObjects)
            {
                if (blockToEdit == null || blockToEdit.selected) continue;
                
                // Check if this object would be selected
                bool wouldBeSelected = IsObjectInSelectionBox(blockToEdit.gameObject, minX, maxX, minY, maxY);
                
                if (wouldBeSelected)
                {
                    // Show hover outline for objects that would be selected
                    if (!blockToEdit.hovered)
                    {
                        blockToEdit.EnableHoverOutline();
                    }
                }
                else
                {
                    // Hide hover outline for objects that wouldn't be selected
                    if (blockToEdit.hovered)
                    {
                        blockToEdit.DisableHoverOutline();
                    }
                }
            }
        }
        
        // This method is called by GizmoTools to determine if it should check for objects under cursor
        public bool ShouldBlockRaycasting()
        {
            // Block all raycast-based highlighting during selection box drawing
            return isDrawingSelectionBox;
        }
        
        // This method is called by GizmoTools to explicitly disable pointer highlights during selection
        public bool IsPreviewingSelection()
        {
            // Return true if we're showing a selection preview (objects that would be selected)
            return isDrawingSelectionBox && previewSelectionEnabled;
        }
        
        /// <summary>
        /// Determines if preview highlighting should be skipped
        /// </summary>
        private bool ShouldSkipPreview()
        {
            // Skip preview if selection box is too small
            bool tooSmall = Vector2.Distance(selectionBoxStart, selectionBoxEnd) < 5f;
            
            // Skip preview if preview selection is disabled
            return tooSmall || !previewSelectionEnabled;
        }
        
        /// <summary>
        /// Checks if an object would be included in the current selection box
        /// </summary>
        private bool IsObjectInSelectionBox(GameObject obj, float minX, float maxX, float minY, float maxY)
        {
            if (obj == null) return false;
            
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer == null)
            {
                // If no renderer, check center point
                Vector3 screenPos = Camera.main.WorldToScreenPoint(obj.transform.position);
                return screenPos.x >= minX && screenPos.x <= maxX &&
                       screenPos.y >= minY && screenPos.y <= maxY;
            }
            
            // For objects with renderers, check how much of the object is inside the selection box
            Bounds bounds = renderer.bounds;
            
            // Sample points around the object's bounds (8 corners plus center)
            Vector3[] testPoints = new Vector3[]
            {
                bounds.min,                                              // Bottom-left-back
                new Vector3(bounds.min.x, bounds.min.y, bounds.max.z),   // Bottom-left-front
                new Vector3(bounds.min.x, bounds.max.y, bounds.min.z),   // Top-left-back
                new Vector3(bounds.min.x, bounds.max.y, bounds.max.z),   // Top-left-front
                new Vector3(bounds.max.x, bounds.min.y, bounds.min.z),   // Bottom-right-back
                new Vector3(bounds.max.x, bounds.min.y, bounds.max.z),   // Bottom-right-front
                new Vector3(bounds.max.x, bounds.max.y, bounds.min.z),   // Top-right-back
                bounds.max,                                              // Top-right-front
                bounds.center                                            // Center
            };
            
            // Count how many points are inside the selection box
            int pointsInside = 0;
            foreach (Vector3 point in testPoints)
            {
                Vector3 screenPoint = Camera.main.WorldToScreenPoint(point);
                if (screenPoint.x >= minX && screenPoint.x <= maxX &&
                    screenPoint.y >= minY && screenPoint.y <= maxY)
                {
                    pointsInside++;
                }
            }
            
            // Calculate percentage of points inside
            float percentInside = (float)pointsInside / testPoints.Length;
            
            // Return true if enough of the object is inside the selection box
            return percentInside >= selectionThreshold;
        }
    }
} 