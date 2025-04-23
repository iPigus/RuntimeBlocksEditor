using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Handles the Scale tool functionality for the Gizmo system
    /// </summary>
    public class GizmoScaleTool : MonoBehaviour
    {
        private GizmoTools gizmoTools;
        private GizmoController controller;
        private GizmoHistory history;
        private GizmoVisuals visuals;
        
        private Vector2 mousePrevPos;
        private Vector3 prevGizmoPosition;
        private Vector3[] originalScales; // Store original scales when starting a scale operation
        private Vector3 initialHitPoint; // Store initial hit point for scaling
        private bool scaleInitialized = false; // Flag to track if scaling has been initialized
        private float gridSnapb = 1f;
        
        // Add accumulated movement for consistent grid stepping
        private float accumulatedScaleMovement = 0f;
        private Vector3 lastAppliedScale = Vector3.zero;
        
        private void Awake()
        {
            gizmoTools = GetComponent<GizmoTools>();
            controller = GetComponent<GizmoController>();
            history = GetComponent<GizmoHistory>();
            visuals = GetComponent<GizmoVisuals>();
        }
        
        /// <summary>
        /// Updates the positions of scale handles based on object size.
        /// </summary>
        public void UpdateScaleHandlePositions(GameObject obj)
        {
            if (obj == null || visuals == null ||
                visuals.xScalePos == null || visuals.xScaleNeg == null ||
                visuals.yScalePos == null || visuals.yScaleNeg == null ||
                visuals.zScalePos == null || visuals.zScaleNeg == null)
                return;
                
            // Get object size
            Vector3 size = Vector3.one;
            Renderer renderer = obj.GetComponent<Renderer>();
            if (renderer != null && renderer.bounds.size != Vector3.zero)
            {
                size = renderer.bounds.size;
            }
            else
            {
                size = Vector3.Scale(obj.transform.localScale, Vector3.one);
            }
            
            // Calculate offset for each handle - exactly 0.5 units from object boundaries
            float offsetX = size.x / 2.0f + 0.5f;
            float offsetY = size.y / 2.0f + 0.5f;
            float offsetZ = size.z / 2.0f + 0.5f;
            
            // Update positions for each handle in local space
            visuals.xScalePos.transform.localPosition = new Vector3(offsetX, 0, 0);
            visuals.xScaleNeg.transform.localPosition = new Vector3(-offsetX, 0, 0);
            visuals.yScalePos.transform.localPosition = new Vector3(0, offsetY, 0);
            visuals.yScaleNeg.transform.localPosition = new Vector3(0, -offsetY, 0);
            visuals.zScalePos.transform.localPosition = new Vector3(0, 0, offsetZ);
            visuals.zScaleNeg.transform.localPosition = new Vector3(0, 0, -offsetZ);
        }
        
        /// <summary>
        /// Handles the Scale tool functionality
        /// </summary>
        public void HandleScaleTool()
        {
            if (gizmoTools.controlObjects == null)
            {
                visuals.SetGizmoVisibility(false, false, false);
            }
            else
            {
                visuals.SetGizmoVisibility(false, false, true);
                
                // Always apply rotation from the object when only one object is selected
                if (gizmoTools.controlObjects.Length == 1)
                {
                    transform.rotation = gizmoTools.controlObjects[0].transform.rotation;
                    
                    // Position scaling handles outside the object boundaries
                    if (visuals.xScalePos != null && visuals.xScaleNeg != null && 
                        visuals.yScalePos != null && visuals.yScaleNeg != null && 
                        visuals.zScalePos != null && visuals.zScaleNeg != null)
                    {
                        // Get the object size and place handles at appropriate distance
                        GameObject obj = gizmoTools.controlObjects[0];
                        
                        // Update the handle positions by calling our dedicated method
                        UpdateScaleHandlePositions(obj);
                    }
                }
                // For multiple objects, always use global orientation for visual consistency
                else
                {
                    transform.rotation = Quaternion.identity;
                }
                
                if (Input.GetMouseButtonDown(0) && gizmoTools.selected != null)
                {
                    // Save initial state when starting scaling
                    // This ensures we can undo back to the original scale
                    if (history.historyEnabled)
                        history.SaveState(gizmoTools.controlObjects, GizmoHistory.HistoryActionType.Transform);
                    
                    // Don't save state here, it will be saved on mouse button up
                    // This prevents duplicate history entries
                    mousePrevPos = Input.mousePosition;
                    prevGizmoPosition = transform.position;
                    
                    // Reset scaling initialization flag on new click
                    scaleInitialized = false;
                    
                    // Reset accumulated movement when starting scale operation
                    accumulatedScaleMovement = 0f;
                    
                    // Store original scales
                    if (gizmoTools.controlObjects != null && gizmoTools.controlObjects.Length > 0)
                    {
                        originalScales = new Vector3[gizmoTools.controlObjects.Length];
                        for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                        {
                            if (gizmoTools.controlObjects[i] != null)
                                originalScales[i] = gizmoTools.controlObjects[i].transform.localScale;
                        }
                    }
                    
                    // Trigger scale start event
                    if (GizmoEvents.Instance != null && gizmoTools.controlObjects != null)
                    {
                        Vector3[] startScales = new Vector3[gizmoTools.controlObjects.Length];
                        for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                        {
                            if (gizmoTools.controlObjects[i] != null)
                            {
                                startScales[i] = gizmoTools.controlObjects[i].transform.localScale;
                            }
                        }
                        
                        GizmoEvents.Instance.TriggerScaleStart(gizmoTools.controlObjects, startScales);
                    }
                }
            }
            
            if (Input.GetMouseButton(0) && gizmoTools.selected != null)
            {
                // Calculate mouse movement
                float mouseDeltaX = Input.mousePosition.x - mousePrevPos.x;
                float mouseDeltaY = Input.mousePosition.y - mousePrevPos.y;
            
                // Camera-based scaling sensitivity - further away = more sensitive
                float cameraDistance = Vector3.Distance(Camera.main.transform.position, transform.position);
                float baseSensitivity = 0.01f; // Base sensitivity value
                float distanceModifier = cameraDistance * 0.1f; // Adjust scale based on distance
                float sensitivity = baseSensitivity * Mathf.Max(distanceModifier, 1f);
                
                // Store original scales for event
                Vector3[] startScales = null;
                Vector3[] currentScales = null;
                
                if (GizmoEvents.Instance != null && gizmoTools.controlObjects != null)
                {
                    startScales = new Vector3[gizmoTools.controlObjects.Length];
                    currentScales = new Vector3[gizmoTools.controlObjects.Length];
                    
                    // Store the original scales before applying the new scale
                    for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                    {
                        if (gizmoTools.controlObjects[i] != null)
                        {
                            startScales[i] = gizmoTools.controlObjects[i].transform.localScale;
                        }
                    }
                }
                
                // Scaling objects
                foreach (GameObject obj in gizmoTools.controlObjects)
                {
                    if (obj == null) continue;
                    
                    Vector3 originalScale = obj.transform.localScale;
                    Vector3 newScale = originalScale;
                    Vector3 newPosition = obj.transform.position;
                    
                    // Handle uniform/multi-object scaling
                    if (gizmoTools.controlObjects.Length > 1 || gizmoTools.selected == visuals.allScale)
                    {
                        // Initialize scaling on first movement
                        if (!scaleInitialized)
                        {
                            scaleInitialized = true;
                            lastAppliedScale = originalScale;
                            continue; // Skip scaling on the first frame to avoid jumps
                        }
                        
                        // Use vertical mouse movement to determine scaling direction
                        // Positive mouseDeltaY (moving up) = increase scale
                        // Negative mouseDeltaY (moving down) = decrease scale
                        
                        // Only apply grid snapping if we're scaling a single object and snap is enabled
                        if (controller.snap && gizmoTools.controlObjects.Length == 1)
                        {
                            float snapValue = controller.gridSnap > 0 ? controller.gridSnap : gridSnapb;
                            
                            // Fixed step threshold - how much mouse movement needed for one grid step
                            float stepThreshold = 10f; // 10 pixels of movement = 1 grid step
                            
                            // Normalize mouse movement to make steps more consistent
                            // Sign preserves direction, but magnitude is normalized 
                            float normalizedMovement = (mouseDeltaY != 0) ? 
                                                      Mathf.Sign(mouseDeltaY) * Mathf.Min(Mathf.Abs(mouseDeltaY), stepThreshold) : 
                                                      0f;
                            
                            // Accumulate normalized mouse movement
                            accumulatedScaleMovement += normalizedMovement;
                            
                            // Check if we've accumulated enough movement for a grid step
                            if (Mathf.Abs(accumulatedScaleMovement) >= stepThreshold)
                            {
                                // Calculate how many steps to take
                                int steps = Mathf.FloorToInt(Mathf.Abs(accumulatedScaleMovement) / stepThreshold);
                                
                                // Apply steps in correct direction
                                float stepAmount = Mathf.Sign(accumulatedScaleMovement) * snapValue * steps;
                                
                                // Apply the change to each axis
                                newScale.x = lastAppliedScale.x + stepAmount;
                                newScale.y = lastAppliedScale.y + stepAmount;
                                newScale.z = lastAppliedScale.z + stepAmount;
                                
                                // Update the last applied scale
                                lastAppliedScale = newScale;
                                
                                // Reduce accumulated movement by what we've used
                                accumulatedScaleMovement -= Mathf.Sign(accumulatedScaleMovement) * steps * stepThreshold;
                            }
                            else
                            {
                                // Not enough movement for a step, use last applied scale
                                newScale = lastAppliedScale;
                            }
                        }
                        else
                        {
                            // Non-snapped scaling with standard sensitivity
                            float scaleFactor = mouseDeltaY * sensitivity * 0.1f;
                            newScale = originalScale * (1.0f + scaleFactor);
                        }
                        
                        // Limit minimum scale
                        newScale.x = Mathf.Max(newScale.x, 0.05f);
                        newScale.y = Mathf.Max(newScale.y, 0.05f);
                        newScale.z = Mathf.Max(newScale.z, 0.05f);
                        
                        // For multi-object scaling, also scale the distance between objects
                        if (gizmoTools.controlObjects.Length > 1)
                        {
                            // Calculate vector from gizmo center to object
                            Vector3 directionFromCenter = obj.transform.position - transform.position;
                            
                            // Calculate scaling ratio (new scale divided by original scale)
                            // Using average of all axes to maintain proportions
                            float scaleRatio = (newScale.x / originalScale.x + 
                                                newScale.y / originalScale.y + 
                                                newScale.z / originalScale.z) / 3f;
                            
                            // Limit minimum distance scaling to prevent objects collapsing
                            scaleRatio = Mathf.Max(scaleRatio, 0.05f);
                            
                            // Calculate new position based on scaled distance from center
                            newPosition = transform.position + (directionFromCenter * scaleRatio);
                        }
                        
                        // Apply new scale and position
                        obj.transform.localScale = newScale;
                        
                        // For multiple objects, update position to scale distances between objects
                        if (gizmoTools.controlObjects.Length > 1)
                        {
                            obj.transform.position = newPosition;
                        }
                        continue;
                    }
                    
                    // Single-axis scaling (X, Y, or Z handle)
                    int axisIndex = 0; // 0=X, 1=Y, 2=Z
                    bool isPositiveHandle = false;
                    Vector3 worldAxisDir = Vector3.zero;
                    Vector3 oppositeAnchorPoint = Vector3.zero;
                    
                    // Determine which handle is selected, axis direction, and opposite anchor point
                    if (gizmoTools.selected == visuals.xScalePos)
                    {
                        axisIndex = 0;
                        isPositiveHandle = true;
                        worldAxisDir = obj.transform.right;
                    }
                    else if (gizmoTools.selected == visuals.xScaleNeg)
                    {
                        axisIndex = 0;
                        isPositiveHandle = false;
                        worldAxisDir = -obj.transform.right;
                    }
                    else if (gizmoTools.selected == visuals.yScalePos)
                    {
                        axisIndex = 1;
                        isPositiveHandle = true;
                        worldAxisDir = obj.transform.up;
                    }
                    else if (gizmoTools.selected == visuals.yScaleNeg)
                    {
                        axisIndex = 1;
                        isPositiveHandle = false;
                        worldAxisDir = -obj.transform.up;
                    }
                    else if (gizmoTools.selected == visuals.zScalePos)
                    {
                        axisIndex = 2;
                        isPositiveHandle = true;
                        worldAxisDir = obj.transform.forward;
                    }
                    else if (gizmoTools.selected == visuals.zScaleNeg)
                    {
                        axisIndex = 2;
                        isPositiveHandle = false;
                        worldAxisDir = -obj.transform.forward;
                    }
                    
                    // Determine the anchor point in the local space of the object
                    Vector3 localAnchorPoint = Vector3.zero;
                    if (axisIndex == 0) // X axis
                        localAnchorPoint = new Vector3(isPositiveHandle ? -0.5f : 0.5f, 0, 0);
                    else if (axisIndex == 1) // Y axis
                        localAnchorPoint = new Vector3(0, isPositiveHandle ? -0.5f : 0.5f, 0);
                    else // Z axis
                        localAnchorPoint = new Vector3(0, 0, isPositiveHandle ? -0.5f : 0.5f);
                    
                    // Transform the local anchor point to the global space
                    oppositeAnchorPoint = obj.transform.TransformPoint(localAnchorPoint);
                    
                    // Create a plane perpendicular to the camera view but aligned with the scaling axis
                    Vector3 cameraForward = Camera.main.transform.forward;
                    Vector3 planeNormal = Vector3.Cross(Vector3.Cross(worldAxisDir, cameraForward).normalized, worldAxisDir).normalized;
                    
                    // If the plane normal is too close to zero (camera aligned with axis), use the camera right vector
                    if (planeNormal.magnitude < 0.1f)
                    {
                        planeNormal = Vector3.Cross(Camera.main.transform.right, worldAxisDir).normalized;
                    }
                    
                    // Create a dragging plane passing through the handle (not the anchor point)
                    // We need to get the current handle position based on the current scale
                    Vector3 handlePosition = Vector3.zero;
                    
                    // Determine handle position in local space and transform to world space
                    Vector3 localHandlePoint = Vector3.zero;
                    if (axisIndex == 0) // X axis
                        localHandlePoint = new Vector3(isPositiveHandle ? 0.5f : -0.5f, 0, 0);
                    else if (axisIndex == 1) // Y axis
                        localHandlePoint = new Vector3(0, isPositiveHandle ? 0.5f : -0.5f, 0);
                    else // Z axis
                        localHandlePoint = new Vector3(0, 0, isPositiveHandle ? 0.5f : -0.5f);
                    
                    // Transform handle position to world space
                    handlePosition = obj.transform.TransformPoint(localHandlePoint);
                    
                    // Now create a plane for mouse interaction that's perpendicular to the camera view direction
                    // This makes it easier to hit with the mouse ray regardless of viewing angle
                    Plane draggingPlane = new Plane(planeNormal, handlePosition);
                    
                    // teleport mouse cursor to the center of the handle VERY IMPORTANT
                    if (Input.GetMouseButtonDown(0) && !scaleInitialized)
                    {
                        // Convert handle position from world space to screen position
                        Vector2 screenHandlePos = Camera.main.WorldToScreenPoint(handlePosition);
                        
                        // Teleport mouse cursor to the center of the handle
                        Mouse.current.WarpCursorPosition(screenHandlePos);
                        
                        // Update previous mouse position for later delta movement calculations
                        mousePrevPos = screenHandlePos;

                        break; //break there is needed, without break there will be visual bug
                    }
                    
                    // Cast a ray from the camera through the mouse position
                    Ray mouseRay = Camera.main.ScreenPointToRay(Input.mousePosition);
                    float distance;
                    
                    // If the ray intersects the dragging plane
                    if (draggingPlane.Raycast(mouseRay, out distance))
                    {
                        // Get the world position where the mouse ray intersects the dragging plane
                        Vector3 hitPoint = mouseRay.origin + mouseRay.direction * distance;
                        
                        // Project the mouse hit point directly onto the axis for direct manipulation
                        Vector3 axisHitPoint = oppositeAnchorPoint + Vector3.Project(hitPoint - oppositeAnchorPoint, worldAxisDir);
                        
                        // Calculate the handle distance from the anchor point based on direct mouse position
                        float handleDistance = Vector3.Distance(oppositeAnchorPoint, axisHitPoint);
                        
                        // Check if the handle is on the correct side of the anchor point
                        Vector3 directionToHandle = (axisHitPoint - oppositeAnchorPoint).normalized;
                        bool correctDirection = Vector3.Dot(directionToHandle, worldAxisDir) > 0;
                        
                        if (!correctDirection)
                        {
                            // Handle is on wrong side of anchor, set to minimum scale
                            handleDistance = 0.05f;
                            axisHitPoint = oppositeAnchorPoint + worldAxisDir * handleDistance;
                        }
                        
                        // The scale is 1x 
                        float newAxisScale = handleDistance * 1f;
                        
                        // Apply grid snapping if enabled
                        if (controller.snap)
                        {
                            float snapValue = controller.gridSnap > 0 ? controller.gridSnap : gridSnapb;
                            newAxisScale = Mathf.Round(newAxisScale / snapValue) * snapValue;
                            
                            // Recalculate handle distance and position based on snapped scale
                            handleDistance = newAxisScale * 0.5f;
                            axisHitPoint = oppositeAnchorPoint + worldAxisDir * handleDistance;
                        }
                        
                        // Ensure minimum scale constraint
                        newAxisScale = Mathf.Max(newAxisScale, 0.05f);
                        handleDistance = newAxisScale * 0.5f;
                        
                        // Update the appropriate scale component
                        if (axisIndex == 0) // X axis
                        {
                            newScale.x = newAxisScale;
                        }
                        else if (axisIndex == 1) // Y axis
                        {
                            newScale.y = newAxisScale;
                        }
                        else // Z axis
                        {
                            newScale.z = newAxisScale;
                        }
                        
                        // Position the object so the anchor point stays fixed
                        // This is the key to making scaling happen from the handle, not the center
                        newPosition = oppositeAnchorPoint + (worldAxisDir * handleDistance);
                    }
                    
                    // Apply the new scale and position
                    obj.transform.localScale = newScale;
                    obj.transform.position = newPosition;
                }
                    
                // Update scale handle positions
                if (gizmoTools.controlObjects.Length == 1)
                {
                    UpdateScaleHandlePositions(gizmoTools.controlObjects[0]);
                }
                
                // Update gizmo position
                gizmoTools.UpdateSelectedObjects();
                
                // Update mouse position
                mousePrevPos = Input.mousePosition;
                
                // Capture current scales after applying scaling and trigger event
                if (GizmoEvents.Instance != null && gizmoTools.controlObjects != null && startScales != null)
                {
                    // Store the scales after applying the new scale
                    for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                    {
                        if (gizmoTools.controlObjects[i] != null)
                        {
                            currentScales[i] = gizmoTools.controlObjects[i].transform.localScale;
                        }
                    }
                    
                    // Trigger the scaling event
                    GizmoEvents.Instance.TriggerScaling(gizmoTools.controlObjects, startScales, currentScales);
                }
            }
            else if (Input.GetMouseButtonUp(0) && gizmoTools.selected != null && gizmoTools.controlObjects != null)
            {
                // Save state before snapping
                if (history.historyEnabled)
                    history.SaveState(gizmoTools.controlObjects, GizmoHistory.HistoryActionType.Transform);
                
                // Trigger scale end event
                if (GizmoEvents.Instance != null)
                {
                    Vector3[] startScales = new Vector3[gizmoTools.controlObjects.Length];
                    Vector3[] endScales = new Vector3[gizmoTools.controlObjects.Length];
                    
                    for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                    {
                        if (gizmoTools.controlObjects[i] != null)
                        {
                            // Get the original scale from history
                            startScales[i] = history.GetLastState(gizmoTools.controlObjects[i]).localScale;
                            // Get the current scale
                            endScales[i] = gizmoTools.controlObjects[i].transform.localScale;
                        }
                    }
                    
                    GizmoEvents.Instance.TriggerScaleEnd(gizmoTools.controlObjects, startScales, endScales);
                }
                
                // Force full position update
                gizmoTools.UpdateSelectedObjects();
            }
            else
            {
                // Handle selection of scaling handles
                RaycastHit hitIs;
                bool hitGizmoElement = false;
                
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitIs, Mathf.Infinity, gizmoTools.maskSel))
                {
                    GameObject hitObject = hitIs.transform.gameObject;
                    
                    if (visuals.IsScaleElement(hitObject))
                    {
                        hitGizmoElement = true;
                        
                        if (gizmoTools.selected == null)
                            visuals.HighlightGizmoElement(hitObject);
                        else if (hitObject != gizmoTools.selected)
                        {
                            visuals.RevertHighlight();
                            visuals.HighlightGizmoElement(hitObject);
                        }
                        
                        gizmoTools.selected = hitObject;
                    }
                    else if (gizmoTools.selected != null)
                    {
                        visuals.RevertHighlight();
                        gizmoTools.selected = null;
                    }
                }
                else if (gizmoTools.selected != null)
                {
                    visuals.RevertHighlight();
                    gizmoTools.selected = null;
                }
                
                // If gizmo handle was not hit, try to select a block
                if (!hitGizmoElement && Input.GetMouseButtonDown(0))
                {
                    gizmoTools.HandleBlockSelection("scale");
                }
                
                // Reset mouse position
                mousePrevPos = Input.mousePosition;
            }
            
            // Update after mouse button release
            if (Input.GetMouseButtonUp(0) && gizmoTools.controlObjects != null)
            {
                // No need to save state here, it's already saved in the block above
                // This prevents duplicate history entries
                
                // Force full position update (if not done above)
                if (gizmoTools.selected == null) {
                    gizmoTools.UpdateSelectedObjects();
                }
            }
        }
    }
    
} 