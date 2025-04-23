using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Handles the Rotate tool functionality for the Gizmo system
    /// </summary>
    public class GizmoRotateTool : MonoBehaviour
    {
        private GizmoTools gizmoTools;
        private GizmoController controller;
        private GizmoHistory history;
        private GizmoVisuals visuals;
        
        private Vector2 mousePrevPos;
        private Vector3 prevGizmoPosition;
        
        private bool canRotate = false;
        private float selectionTime = 0f;
        private Vector3 lastGizmoPosition;
        
        private bool gizmoClickedAfterSelection = false;
        
        // For rotation snap
        private float accumulatedXRotation = 0f;
        private float accumulatedYRotation = 0f;
        private float accumulatedZRotation = 0f;
        private Quaternion[] initialRotations;
        
        private void Awake()
        {
            gizmoTools = GetComponent<GizmoTools>();
            controller = GetComponent<GizmoController>();
            history = GetComponent<GizmoHistory>();
            visuals = GetComponent<GizmoVisuals>();
        }
        
        /// <summary>
        /// Handles the Rotate tool functionality
        /// </summary>
        public void HandleRotateTool()
        {
            if (gizmoTools.controlObjects == null)
            {
                visuals.SetGizmoVisibility(false, false, false);
                // Even if no blocks are selected, we still allow them to be selected
            }
            else
            {
                visuals.SetGizmoVisibility(false, true, false);
                
                if (controller.localControl)
                    transform.rotation = gizmoTools.controlObjects[0].transform.rotation;
                else
                    transform.rotation = Quaternion.identity;
                    
                // Make looker face the camera
                if (visuals.looker != null)
                    visuals.looker.transform.LookAt(Camera.main.transform.position);
                
                if (Input.GetMouseButtonDown(0) && gizmoTools.selected != null)
                {
                    // Save initial state when starting rotation
                    // This ensures we can undo back to the original rotation
                    if (history.historyEnabled)
                        history.SaveState(gizmoTools.controlObjects, GizmoHistory.HistoryActionType.Transform);
                        
                    // Don't save state here, it will be saved on mouse button up
                    // This prevents duplicate history entries
                    mousePrevPos = Input.mousePosition;
                    prevGizmoPosition = transform.position;
                    
                    // Reset accumulated rotation values when starting a new rotation
                    accumulatedXRotation = 0f;
                    accumulatedYRotation = 0f;
                    accumulatedZRotation = 0f;
                    
                    // Store initial rotations of objects for snap rotation
                    if (controller.rotationSnap && gizmoTools.controlObjects != null)
                    {
                        initialRotations = new Quaternion[gizmoTools.controlObjects.Length];
                        for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                        {
                            if (gizmoTools.controlObjects[i] != null)
                            {
                                initialRotations[i] = gizmoTools.controlObjects[i].transform.rotation;
                            }
                        }
                    }
                    
                    // Trigger rotation start event
                    if (GizmoEvents.Instance != null)
                    {
                        Quaternion[] startRotations = new Quaternion[gizmoTools.controlObjects.Length];
                        for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                        {
                            if (gizmoTools.controlObjects[i] != null)
                            {
                                startRotations[i] = gizmoTools.controlObjects[i].transform.rotation;
                            }
                        }
                        
                        GizmoEvents.Instance.TriggerRotateStart(gizmoTools.controlObjects, startRotations);
                    }
                }
            }
            
            if (transform.position != lastGizmoPosition)
            {
                lastGizmoPosition = transform.position;
                canRotate = false;
                selectionTime = Time.time;
            }
            
            if (!canRotate && Time.time > selectionTime + 0.2f)
            {
                canRotate = true;
            }
            
            if (Input.GetMouseButton(0) && gizmoTools.selected != null && canRotate && gizmoClickedAfterSelection)
            {
                // Calculate difference in mouse position from last frame
                float mouseDeltaX = (Input.mousePosition.x - mousePrevPos.x) * 0.5f; // Reduce sensitivity
                float mouseDeltaY = (Input.mousePosition.y - mousePrevPos.y) * 0.5f;
                
                // Make sure we don't have too large movements
                mouseDeltaX = Mathf.Clamp(mouseDeltaX, -5f, 5f);
                mouseDeltaY = Mathf.Clamp(mouseDeltaY, -5f, 5f);
                
                // Store original rotations for event
                Quaternion[] startRotations = null;
                Quaternion[] currentRotations = null;
                
                if (GizmoEvents.Instance != null && gizmoTools.controlObjects != null)
                {
                    startRotations = new Quaternion[gizmoTools.controlObjects.Length];
                    currentRotations = new Quaternion[gizmoTools.controlObjects.Length];
                    
                    // Store the original rotations before applying the new rotation
                    for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                    {
                        if (gizmoTools.controlObjects[i] != null)
                        {
                            startRotations[i] = gizmoTools.controlObjects[i].transform.rotation;
                        }
                    }
                }
                
                foreach (GameObject obj in gizmoTools.controlObjects)
                {
                    if (obj == null) continue;
                    
                    // Select the appropriate rotation axis
                    Vector3 rotationAxis = Vector3.zero;
                    float rotationAmount = 0;
                    
                    if (gizmoTools.selected == visuals.xRot)
                    {
                        rotationAxis = controller.localControl ? transform.right : Vector3.right;
                        rotationAmount = mouseDeltaY;
                        
                        // Accumulate rotation for snapping
                        if (controller.rotationSnap)
                            accumulatedXRotation += rotationAmount;
                    }
                    else if (gizmoTools.selected == visuals.yRot)
                    {
                        rotationAxis = controller.localControl ? transform.up : Vector3.up;
                        rotationAmount = -mouseDeltaX;
                        
                        // Accumulate rotation for snapping
                        if (controller.rotationSnap)
                            accumulatedYRotation += rotationAmount;
                    }
                    else if (gizmoTools.selected == visuals.zRot)
                    {
                        rotationAxis = controller.localControl ? transform.forward : Vector3.forward;
                        rotationAmount = mouseDeltaY;
                        
                        // Accumulate rotation for snapping
                        if (controller.rotationSnap)
                            accumulatedZRotation += rotationAmount;
                    }
                    
                    // Perform rotation
                    if (rotationAxis != Vector3.zero)
                    {
                        // Apply rotation snap if enabled
                        if (controller.rotationSnap)
                        {
                            float snapAngle = controller.rotationSnapAngle;
                            float accumulatedRotation = 0f;
                            
                            // Determine which accumulated value to use based on axis
                            if (rotationAxis == (controller.localControl ? transform.right : Vector3.right))
                                accumulatedRotation = accumulatedXRotation;
                            else if (rotationAxis == (controller.localControl ? transform.up : Vector3.up))
                                accumulatedRotation = accumulatedYRotation;
                            else if (rotationAxis == (controller.localControl ? transform.forward : Vector3.forward))
                                accumulatedRotation = accumulatedZRotation;
                            
                            // Calculate the target rotation amount (snapped to increments)
                            float targetAngle = Mathf.Round(accumulatedRotation / snapAngle) * snapAngle;
                            
                            // Find the object's index to get its initial rotation
                            int objIndex = System.Array.IndexOf(gizmoTools.controlObjects, obj);
                            if (objIndex >= 0 && initialRotations != null && objIndex < initialRotations.Length && initialRotations[objIndex] != null)
                            {
                                // Reset to initial rotation
                                obj.transform.rotation = initialRotations[objIndex];
                                
                                // Apply the snapped rotation
                                obj.transform.RotateAround(transform.position, rotationAxis, targetAngle);
                            }
                            else
                            {
                                // Fallback if we can't find the initial rotation
                                obj.transform.RotateAround(transform.position, rotationAxis, rotationAmount);
                            }
                        }
                        else
                        {
                            // Apply normal rotation without snapping
                            obj.transform.RotateAround(transform.position, rotationAxis, rotationAmount);
                        }
                    }
                }
                
                // Capture current rotations after applying rotation and trigger event
                if (GizmoEvents.Instance != null && gizmoTools.controlObjects != null && startRotations != null)
                {
                    // Store the rotations after applying the new rotation
                    for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                    {
                        if (gizmoTools.controlObjects[i] != null)
                        {
                            currentRotations[i] = gizmoTools.controlObjects[i].transform.rotation;
                        }
                    }
                    
                    // Trigger the rotating event
                    GizmoEvents.Instance.TriggerRotating(gizmoTools.controlObjects, startRotations, currentRotations);
                }
                
                // Update mouse position
                mousePrevPos = Input.mousePosition;
            }
            else if (Input.GetMouseButtonUp(0) && gizmoTools.selected != null && gizmoTools.controlObjects != null)
            {
                // Save state before triggering end event
                if (history.historyEnabled)
                    history.SaveState(gizmoTools.controlObjects, GizmoHistory.HistoryActionType.Transform);
                
                // Trigger rotate end event
                if (GizmoEvents.Instance != null)
                {
                    Quaternion[] startRotations = new Quaternion[gizmoTools.controlObjects.Length];
                    Quaternion[] endRotations = new Quaternion[gizmoTools.controlObjects.Length];
                    
                    for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                    {
                        if (gizmoTools.controlObjects[i] != null)
                        {
                            // Get the original rotation from history
                            startRotations[i] = history.GetLastState(gizmoTools.controlObjects[i]).rotation;
                            // Get the current rotation
                            endRotations[i] = gizmoTools.controlObjects[i].transform.rotation;
                        }
                    }
                    
                    GizmoEvents.Instance.TriggerRotateEnd(gizmoTools.controlObjects, startRotations, endRotations);
                }
            }
            else
            {
                // Handle selection of rotation controls
                RaycastHit hitIs;
                bool hitGizmoElement = false;
                bool hitAnyObject = false;
                
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitIs, Mathf.Infinity, gizmoTools.maskSel))
                {
                    hitAnyObject = true;
                    GameObject hitObject = hitIs.transform.gameObject;
                    
                    if (visuals.IsRotateElement(hitObject))
                    {
                        // Hit a Gizmo element
                        hitGizmoElement = true;
                        
                        // Highlight the element
                        if (gizmoTools.selected == null)
                            visuals.HighlightGizmoElement(hitObject);
                        else if (hitObject != gizmoTools.selected)
                        {
                            visuals.RevertHighlight();
                            visuals.HighlightGizmoElement(hitObject);
                        }
                        
                        gizmoTools.selected = hitObject;
                        
                        // Only set this flag when the user explicitly clicks on a gizmo element
                        if (Input.GetMouseButtonDown(0))
                        {
                            gizmoClickedAfterSelection = true;
                        }
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
                
                // If gizmo element was not hit, try to select a block
                if (!hitGizmoElement && Input.GetMouseButtonDown(0))
                {
                    gizmoClickedAfterSelection = false;
                    gizmoTools.HandleBlockSelection("rotate");
                }
            }
            
            // Update after mouse button release
            if (Input.GetMouseButtonUp(0) && gizmoTools.controlObjects != null)
            {
                // No need to save state here, it's already saved in the specific block above
                // This prevents duplicate history entries
                
                // Force full position update
                if (gizmoTools.selected == null) {
                    gizmoTools.UpdateSelectedObjects();
                } else {
                    // Update was already done in the specific MouseButtonUp block
                }
            }
            
        }
        
        /// <summary>
        /// Rotates the selected objects 90 degrees around the Y axis.
        /// </summary>
        public void QuickRotateObjects()
        {
            if (gizmoTools.controlObjects == null || gizmoTools.controlObjects.Length == 0)
                return;
                
            // Save state for history before rotation
            if (history.historyEnabled)
                history.SaveState(gizmoTools.controlObjects, GizmoHistory.HistoryActionType.Transform);
                
            // Store original rotations for event
            Quaternion[] startRotations = new Quaternion[gizmoTools.controlObjects.Length];
            Quaternion[] endRotations = new Quaternion[gizmoTools.controlObjects.Length];
            
            // Store the original rotations
            for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
            {
                if (gizmoTools.controlObjects[i] != null)
                {
                    startRotations[i] = gizmoTools.controlObjects[i].transform.rotation;
                }
            }
            
            // Calculate the center point of all selected objects
            Vector3 centerPoint = Vector3.zero;
            int validObjectCount = 0;
            
            foreach (var obj in gizmoTools.controlObjects)
            {
                if (obj != null)
                {
                    centerPoint += obj.transform.position;
                    validObjectCount++;
                }
            }
            
            if (validObjectCount > 0)
            {
                centerPoint /= validObjectCount;
            }
            
            // Rotate each object 90 degrees around the center point
            foreach (var obj in gizmoTools.controlObjects)
            {
                if (obj != null)
                {
                    // Determine rotation axis and space
                    Vector3 rotationAxis = controller.localControl ? obj.transform.up : Vector3.up;
                    Space rotationSpace = controller.localControl ? Space.Self : Space.World;
                    
                    if (gizmoTools.controlObjects.Length > 1)
                    {
                        // For multiple objects, rotate around the center point
                        // This mimics how the rotation tool works in the manual rotation case
                        if (controller.localControl)
                        {
                            // In local mode, rotate the object around its own up axis,
                            // but maintain the position relative to center point
                            Vector3 directionFromCenter = obj.transform.position - centerPoint;
                            Quaternion rotation = Quaternion.AngleAxis(90f, Vector3.up);
                            Vector3 newPosition = centerPoint + rotation * directionFromCenter;
                            
                            // Rotate the object in local space
                            obj.transform.Rotate(Vector3.up, 90f, Space.Self);
                            
                            // Move to maintain the center point
                            obj.transform.position = newPosition;
                        }
                        else
                        {
                            // In global mode, simply rotate around the center point
                            obj.transform.RotateAround(centerPoint, Vector3.up, 90f);
                        }
                    }
                    else
                    {
                        // For a single object, just rotate it in place
                        obj.transform.Rotate(rotationAxis, 90f, rotationSpace);
                    }
                }
            }
            
            // Store end rotations
            for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
            {
                if (gizmoTools.controlObjects[i] != null)
                {
                    endRotations[i] = gizmoTools.controlObjects[i].transform.rotation;
                }
            }
            
            // Trigger quick rotate event
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.TriggerQuickRotated(gizmoTools.controlObjects, startRotations, endRotations);
            }
            
            // Update gizmo position after rotation
            gizmoTools.UpdateSelectedObjects();
        }

     
    }
} 