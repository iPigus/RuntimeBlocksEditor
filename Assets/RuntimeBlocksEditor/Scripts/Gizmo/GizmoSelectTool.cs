using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Handles the Select tool functionality for the Gizmo system
    /// </summary>
    public class GizmoSelectTool : MonoBehaviour
    {
        private GizmoTools gizmoTools;
        private GizmoController controller;
        private GizmoVisuals visuals;
        
        private void Awake()
        {
            gizmoTools = GetComponent<GizmoTools>();
            controller = GetComponent<GizmoController>();
            visuals = GetComponent<GizmoVisuals>();
        }
        
        /// <summary>
        /// Handles the Select Tool functionality
        /// </summary>
        public void HandleSelectTool()
        {
            if (!Camera.main) return;
            
            // Always hide all gizmos in select tool
            visuals.SetGizmoVisibility(false, false, false);
            
            // Handle left click selection
            if (Input.GetMouseButtonDown(0))
            {
                gizmoTools.HandleBlockSelection("select");
            }
        }
    }
} 