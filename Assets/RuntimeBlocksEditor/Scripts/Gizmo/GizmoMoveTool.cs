using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Handles the Move tool functionality for the Gizmo system
    /// </summary>
    public class GizmoMoveTool : MonoBehaviour
    {
        private GizmoTools gizmoTools;
        private GizmoController controller;
        private GizmoHistory history;
        private GizmoVisuals visuals;
        
        private Vector3 thisOffset = Vector3.zero;
        private Vector2 mousePrevPos;
        private float gridSnapb = 1f;
        
        private void Awake()
        {
            gizmoTools = GetComponent<GizmoTools>();
            controller = GetComponent<GizmoController>();
            history = GetComponent<GizmoHistory>();
            visuals = GetComponent<GizmoVisuals>();
        }
        
        /// <summary>
        /// Handles the Move tool functionality
        /// </summary>
        public void HandleMoveTool()
        {
            // Reset offset on new click
            if (Input.GetMouseButtonDown(0))
            {
                thisOffset = Vector3.zero;
                
                // Trigger move start event when starting to move objects
                if (gizmoTools.selected != null && gizmoTools.controlObjects != null && GizmoEvents.Instance != null)
                {
                    Vector3[] startPositions = new Vector3[gizmoTools.controlObjects.Length];
                    for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                    {
                        if (gizmoTools.controlObjects[i] != null)
                        {
                            startPositions[i] = gizmoTools.controlObjects[i].transform.position;
                        }
                    }
                    
                    GizmoEvents.Instance.TriggerMoveStart(gizmoTools.controlObjects, startPositions);
                }
            }

            if (gizmoTools.controlObjects == null)
            {
                visuals.SetGizmoVisibility(false, false, false);
                // Even if no blocks are selected, we still allow them to be selected
            }
            else
            {
                visuals.SetGizmoVisibility(true, false, false);
                
                if (controller.localControl)
                    transform.rotation = gizmoTools.controlObjects[0].transform.rotation;
                else
                    transform.rotation = Quaternion.identity;
                    
                if (Input.GetMouseButtonDown(0) && gizmoTools.selected != null)
                {
                    // Save initial state when starting movement
                    // This ensures we can undo back to the original position
                    if (history.historyEnabled)
                        history.SaveState(gizmoTools.controlObjects, GizmoHistory.HistoryActionType.Transform);
                    
                    // Only save state when we're actually going to move the object
                    // This prevents duplicate history entries
                    // The final state will be saved on mouse button up
                    Vector3[] startPositions = new Vector3[gizmoTools.controlObjects.Length];
                    
                    for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                    {
                        if (gizmoTools.controlObjects[i] != null)
                        {
                            startPositions[i] = gizmoTools.controlObjects[i].transform.position;
                        }
                    }
                    
                    GizmoEvents.Instance.TriggerMoveStart(gizmoTools.controlObjects, startPositions);
                }
            }
            
            if (Input.GetMouseButton(0) && gizmoTools.selected != null)
            {
                // Store initial positions if this is the first move
                if (gizmoTools.controlObjects != null && GizmoEvents.Instance != null)
                {
                    Vector3[] startPositions = new Vector3[gizmoTools.controlObjects.Length];
                    Vector3[] currentPositions = new Vector3[gizmoTools.controlObjects.Length];
                    
                    for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                    {
                        if (gizmoTools.controlObjects[i] != null)
                        {
                            // We use the saved position from history as the start position
                            // and the current position as the current position
                            startPositions[i] = transform.position - gizmoTools.GetObjectOffset(i);
                            currentPositions[i] = gizmoTools.controlObjects[i].transform.position;
                        }
                    }
                    
                    // Trigger the moving event
                    GizmoEvents.Instance.TriggerMoving(gizmoTools.controlObjects, startPositions, currentPositions);
                }
                
                HandleMoveControl();
            }
            else if (Input.GetMouseButtonUp(0) && gizmoTools.selected != null && gizmoTools.controlObjects != null)
            {
                // Save state before triggering end event
                if (history.historyEnabled)
                    history.SaveState(gizmoTools.controlObjects, GizmoHistory.HistoryActionType.Transform);
                
                // Trigger move end event
                if (GizmoEvents.Instance != null)
                {
                    Vector3[] startPositions = new Vector3[gizmoTools.controlObjects.Length];
                    Vector3[] endPositions = new Vector3[gizmoTools.controlObjects.Length];
                    
                    for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                    {
                        if (gizmoTools.controlObjects[i] != null)
                        {
                            // Use the saved positions from history and current positions
                            startPositions[i] = history.GetLastState(gizmoTools.controlObjects[i]).position;
                            endPositions[i] = gizmoTools.controlObjects[i].transform.position;
                        }
                    }
                    
                    GizmoEvents.Instance.TriggerMoveEnd(gizmoTools.controlObjects, startPositions, endPositions);
                }
            }
            else
            {
                // Handle selection of move controls
                RaycastHit hitIs;
                bool hitGizmoElement = false;
                
                if (Physics.Raycast(Camera.main.ScreenPointToRay(Input.mousePosition), out hitIs, Mathf.Infinity, gizmoTools.maskSel))
                {
                    GameObject hitObject = hitIs.transform.gameObject;
                    
                    if (visuals.IsMoveElement(hitObject))
                    {
                        // Hit a Gizmo element
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
                
                // If Gizmo element was not hit, try to select a block
                if (!hitGizmoElement && Input.GetMouseButtonDown(0))
                {
                    gizmoTools.HandleBlockSelection("move");
                }
            }
            
            // Update after mouse button release
            if (Input.GetMouseButtonUp(0) && gizmoTools.controlObjects != null)
            {
                // No need to save state here, it's already saved in the specific block above
                // This prevents duplicate history entries
                    
                // Apply snapping to positions
                ApplyPositionSnapping();
                
                // Update Gizmo position after snapping
                gizmoTools.UpdateSelectedObjects();
            }
        }
        
        /// <summary>
        /// Handles the movement control logic
        /// </summary>
        private void HandleMoveControl()
        {
            if (gizmoTools.controlObjects == null || gizmoTools.selected == null) 
                return;
            
            // Calculate the current cursor position in world space
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
            Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
            Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint);
            
            // Initialize offset only on first click
            if (thisOffset == Vector3.zero)
                thisOffset = transform.position - curPosition;
            
            // Get information about the selected element
            Vector3 elementAxis = visuals.GetElementAxis(gizmoTools.selected);
            bool isArrow = gizmoTools.selected == visuals.xArrow || gizmoTools.selected == visuals.yArrow || gizmoTools.selected == visuals.zArrow;
            bool isPlane = gizmoTools.selected == visuals.xPlane || gizmoTools.selected == visuals.yPlane || gizmoTools.selected == visuals.zPlane;
            
            // Calculate new position depending on element type
            Vector3 newPosition = transform.position;
            
            if (isArrow)
            {
                // Moving along an axis
                if (controller.localControl)
                {
                    // Local movement along axis
                    Vector3 localAxis = transform.TransformDirection(elementAxis);
                    Vector3 movement = Vector3.Project(curPosition + thisOffset - transform.position, localAxis);
                    newPosition += movement;
                }
                else
                {
                    // Global movement along axis
                    if (elementAxis == Vector3.right)
                        newPosition.x = curPosition.x + thisOffset.x;
                    else if (elementAxis == Vector3.up)
                        newPosition.y = curPosition.y + thisOffset.y;
                    else if (elementAxis == Vector3.forward)
                        newPosition.z = curPosition.z + thisOffset.z;
                }
            }
            else if (isPlane)
            {
                // Moving on a plane
                if (controller.localControl)
                {
                    // Local movement on plane
                    if (elementAxis == Vector3.right) // YZ plane
                    {
                        Vector3 localY = transform.TransformDirection(Vector3.up);
                        Vector3 localZ = transform.TransformDirection(Vector3.forward);
                        Vector3 movementY = Vector3.Project(curPosition + thisOffset - transform.position, localY);
                        Vector3 movementZ = Vector3.Project(curPosition + thisOffset - transform.position, localZ);
                        newPosition += movementY + movementZ;
                    }
                    else if (elementAxis == Vector3.up) // XZ plane
                    {
                        Vector3 localX = transform.TransformDirection(Vector3.right);
                        Vector3 localZ = transform.TransformDirection(Vector3.forward);
                        Vector3 movementX = Vector3.Project(curPosition + thisOffset - transform.position, localX);
                        Vector3 movementZ = Vector3.Project(curPosition + thisOffset - transform.position, localZ);
                        newPosition += movementX + movementZ;
                    }
                    else // XY plane
                    {
                        Vector3 localX = transform.TransformDirection(Vector3.right);
                        Vector3 localY = transform.TransformDirection(Vector3.up);
                        Vector3 movementX = Vector3.Project(curPosition + thisOffset - transform.position, localX);
                        Vector3 movementY = Vector3.Project(curPosition + thisOffset - transform.position, localY);
                        newPosition += movementX + movementY;
                    }
                }
                else
                {
                    // Global movement on plane
                    if (elementAxis == Vector3.right) // YZ plane
                    {
                        newPosition.y = curPosition.y + thisOffset.y;
                        newPosition.z = curPosition.z + thisOffset.z;
                    }
                    else if (elementAxis == Vector3.up) // XZ plane
                    {
                        newPosition.x = curPosition.x + thisOffset.x;
                        newPosition.z = curPosition.z + thisOffset.z;
                    }
                    else // XY plane
                    {
                        newPosition.x = curPosition.x + thisOffset.x;
                        newPosition.y = curPosition.y + thisOffset.y;
                    }
                }
            }
            
            // Remove snapping during dragging - it will only happen after movement ends
            
            // Set new Gizmo position
            transform.position = newPosition;
            
            // Update positions of all selected blocks
            if (gizmoTools.controlObjects != null)
            {
                for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
                {
                    if (gizmoTools.controlObjects[i] != null)
                        gizmoTools.controlObjects[i].transform.position = transform.position - gizmoTools.GetObjectOffset(i);
                }
            }
        }

        /// <summary>
        /// Applies position snapping to selected objects.
        /// </summary>
        private void ApplyPositionSnapping()
        {
            if (gizmoTools.controlObjects == null || gizmoTools.controlObjects.Length == 0 || !controller.snap)
                return;
                
            float snapValue = controller.gridSnap > 0 ? controller.gridSnap : gridSnapb;
            
            // Apply snapping to each object
            foreach (GameObject obj in gizmoTools.controlObjects)
            {
                if (obj == null) continue;
                
                // Store original position
                Vector3 originalPos = obj.transform.position;
                
                // Calculate fully snapped position
                Vector3 snappedPos = originalPos;
                snappedPos.x = Mathf.Round(snappedPos.x / snapValue) * snapValue;
                snappedPos.y = Mathf.Round(snappedPos.y / snapValue) * snapValue;
                snappedPos.z = Mathf.Round(snappedPos.z / snapValue) * snapValue;
                
                // Calculate difference between original and snapped
                Vector3 posDiff = snappedPos - originalPos;
                
                // Move object by half the snapping distance
                Vector3 halfSnappedPos = originalPos + (posDiff * 0.5f);
                
                // Apply the half-snapped position
                obj.transform.position = halfSnappedPos;
            }
            
            // Update gizmo position to reflect snapped positions
            gizmoTools.UpdateSelectedObjects();
        }
    }
}