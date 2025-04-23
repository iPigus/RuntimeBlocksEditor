using UnityEngine;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Handles hover highlighting for objects in the Gizmo system
    /// </summary>
    public class GizmoHoverManager : MonoBehaviour
    {
        private GizmoTools gizmoTools;
        private GizmoController controller;
        private MultipleSelectionTool selectionTool;
        
        private void Awake()
        {
            gizmoTools = GetComponent<GizmoTools>();
            controller = GetComponent<GizmoController>();
            selectionTool = GetComponent<MultipleSelectionTool>();
        }
        
        /// <summary>
        /// Checks if highlighting should be skipped (when selection box is active)
        /// </summary>
        public bool ShouldSkipHighlighting()
        {
            // Skip highlighting when selection box is active - this takes priority
            if (selectionTool != null && selectionTool.IsSelectionBoxActive)
                return true;
            
            // Skip highlighting when dragging objects
            if (Input.GetMouseButton(0))
                return true;
                
            // Skip highlighting when the cursor is over UI elements
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                return true;
                
            return false;
        }
        
        // Method to determine objects that should be highlighted during selection box preview
        public bool CanHighlightDuringSelection(BlockToEdit obj)
        {
            // If we're not in selection box mode or obj is already selected, no need for special handling
            if (selectionTool == null || !selectionTool.IsPreviewingSelection() || obj == null || obj.selected)
                return false;
            
            // If we're in selection preview mode, the MultipleSelectionTool will handle highlighting
            // through the PreviewSelectionHighlighting method
            return true;
        }
        
        /// <summary>
        /// Handles hover highlighting for objects under the cursor
        /// </summary>
        public void HandleHoverHighlighting()
        {
            // Skip in certain conditions
            if (ShouldSkipHighlighting())
                return;
                
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            BlockToEdit hitBlockToEdit = null;
            
            // Check if mouse is over a block
            if (Physics.Raycast(ray, out RaycastHit hitInfo))
            {
                if (hitInfo.transform.CompareTag(controller.tagToFind))
                {
                    BlockToEdit blockToEdit = hitInfo.transform.GetComponentInParent<BlockToEdit>();
                    if (blockToEdit != null)
                    {
                        hitBlockToEdit = blockToEdit;
                        
                        // Show hover outline if not already selected and not selecting with mouse button
                        if (!blockToEdit.selected && !Input.GetMouseButton(0))
                        {
                            // Check if it's part of a group and apply hover outline to all objects in the group
                            bool isGroup = blockToEdit.ApplyHoverOutlineToGroup();
                            
                            // If not part of a group, apply hover outline to just this object
                            if (!isGroup)
                            {
                                blockToEdit.EnableHoverOutline();
                            }
                        }
                    }
                }
            }
            
            // Remove hover highlighting from blocks that aren't under the cursor
            foreach (var obj in BlockToEdit.AllObjects)
            {
                if (obj != null && !obj.selected && obj.hovered && obj != hitBlockToEdit)
                {
                    // Don't remove hover if mouse button is down (might be in the middle of selection)
                    if (Input.GetMouseButton(0))
                        continue;
                        
                    // If it's part of a group, remove hover from all objects in the group
                    bool isGroup = obj.RemoveHoverOutlineFromGroup();
                    
                    // If not part of a group, just remove hover from this object
                    if (!isGroup)
                    {
                        obj.DisableHoverOutline();
                    }
                }
            }
        }
    }
} 