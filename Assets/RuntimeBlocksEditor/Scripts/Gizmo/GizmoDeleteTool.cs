using UnityEngine;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Handles the Delete tool functionality for the Gizmo system
    /// </summary>
    public class GizmoDeleteTool : MonoBehaviour
    {
        private GizmoTools gizmoTools;
        private GizmoController controller;
        private GizmoHistory history;
        private GizmoVisuals visuals;
        
        private void Awake()
        {
            gizmoTools = GetComponent<GizmoTools>();
            controller = GetComponent<GizmoController>();
            history = GetComponent<GizmoHistory>();
            visuals = GetComponent<GizmoVisuals>();
        }
        
        /// <summary>
        /// Handles the Delete tool functionality
        /// </summary>
        public void HandleDeleteTool()
        {
            visuals.SetGizmoVisibility(false, false, false);
            
            if (!Camera.main) return;
            
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(ray, out RaycastHit hitInfo) && 
                hitInfo.transform.CompareTag(controller.tagToFind) &&
                !UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                BlockToEdit editorObj = hitInfo.transform.GetComponent<BlockToEdit>();
                if (editorObj != null)
                {
                    editorObj.ChangeMaterialToRed();
                    
                    // Handle block deletion
                    if (Input.GetMouseButtonDown(0))
                    {
                        HandleBlockDeletion();
                    }
                }
            }
        }
        
        /// <summary>
        /// Handles selecting and immediately deleting a block
        /// </summary>
        private void HandleBlockDeletion()
        {
            if (!Camera.main) return;
            
            // Try to select and delete a block
            Ray rayS = Camera.main.ScreenPointToRay(Input.mousePosition);
            
            if (Physics.Raycast(rayS, out RaycastHit hitBlockInfo, Mathf.Infinity, controller.maskBlocks))
            {
                if (hitBlockInfo.transform.CompareTag(controller.tagToFind))
                {
                    BlockToEdit blockToEdit = hitBlockInfo.transform.GetComponent<BlockToEdit>();
                    if (blockToEdit != null)
                    {
                        // Check if this is part of a group
                        Transform parentTrans = blockToEdit.transform.parent;
                        if (parentTrans != null)
                        {
                            BlockToEdit parentBlock = parentTrans.GetComponent<BlockToEdit>();
                            if (parentBlock != null && parentTrans.gameObject != blockToEdit.gameObject)
                            {
                                // Delete the parent group instead
                                DeleteGameObject(parentTrans.gameObject);
                                return;
                            }
                        }
                        
                        // If not part of a group, proceed with normal deletion
                        DeleteGameObject(blockToEdit.gameObject);
                    }
                }
            }
        }
        
        /// <summary>
        /// Deletes a GameObject with proper history and event handling
        /// </summary>
        private void DeleteGameObject(GameObject objectToDelete)
        {
            if (objectToDelete == null) return;
            
            // Save state for history before deletion if enabled
            if (history.historyEnabled)
            {
                history.SaveDeletionState(objectToDelete);
            }
            
            // Trigger delete event
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.TriggerObjectDeleted(objectToDelete);
            }
            
            // If deleting a block that's currently selected, deselect it first
            BlockToEdit blockToEdit = objectToDelete.GetComponent<BlockToEdit>();
            if (blockToEdit != null && blockToEdit.selected)
            {
                blockToEdit.selected = false;
                blockToEdit.DisableOutline();
                blockToEdit.DisableHoverOutline();
                
                // Update selected objects
                gizmoTools.UpdateSelectedObjects();
            }
            
            // Delete the object
            Destroy(objectToDelete);
        }
    }
} 