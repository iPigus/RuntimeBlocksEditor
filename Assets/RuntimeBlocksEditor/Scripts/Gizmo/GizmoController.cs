using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.InputSystem;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Main controller for Gizmo tools, managing the runtime block editing interface.
    /// </summary>
    public class GizmoController : MonoBehaviour
    {
        public bool localControl = false;
        public float scaleS = 5;
        public float gridSnap = 1f;
        public bool snap = false;
        
        // New rotation snap properties
        public float rotationSnapAngle = 15f; // Default to 15 degrees
        public bool rotationSnap = false;
        
        [HideInInspector]public string tagToFind = "BlockTag";
        [Tooltip("Layer mask for selecting blocks")]
        public LayerMask maskBlocks = ~0; // Default to everything
        public KeyCode selectCode = KeyCode.Alpha1;
        public KeyCode moveCode = KeyCode.Alpha2;
        public KeyCode rotateCode = KeyCode.Alpha3;
        public KeyCode scaleCode = KeyCode.Alpha4;
        public KeyCode localControlCode = KeyCode.L;
        public KeyCode duplicateCode = KeyCode.D; //control + d 
        public KeyCode quickRotateCode = KeyCode.R; // r, will rotate the object 90 degrees on the y axis
        public KeyCode selectMultipleCode = KeyCode.LeftShift;
        
        public Color UnselectedToolColor;
        public Color SelectedToolColor;
        
        private GizmoHistory _history;
        private GizmoTools _tools;
        private GizmoVisuals _visuals;
        private GizmoUI _ui;
        
        private int _activeToolType = 3; // Select tool by default
        private int _previousToolType = 3; // Track previous tool for events
        
        public static GizmoController Singleton { get; private set; }
        
        private void Awake()
        {
            Singleton = this;
            
            // Initialize components
            _history = GetComponent<GizmoHistory>();
            _tools = GetComponent<GizmoTools>();
            _visuals = GetComponent<GizmoVisuals>();
            _ui = GetComponent<GizmoUI>();
            
            // Initialize RuntimeBlocksManager if it doesn't exist
            if (RuntimeBlocksManager.Instance == null)
            {
                GameObject managerGo = new GameObject("RuntimeBlocksManager");
                managerGo.AddComponent<RuntimeBlocksManager>();
                Debug.Log("Created RuntimeBlocksManager");
            }
            
            // Register RuntimeBlocksManager with GizmoEvents
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.RegisterRuntimeBlocksManager();
            }
        }

        private void Start()
        {
            if (SceneManager.GetActiveScene().buildIndex == 2) return;
            ChangeToSelectGizmo();
        }
        
        private void Update()
        {
            if (!Camera.main) return;
            
            // Handle controls for tool switching
            if (Input.GetKeyDown(selectCode)) ChangeToSelectGizmo();
            if (Input.GetKeyDown(moveCode)) ChangeToMoveGizmo();
            if (Input.GetKeyDown(rotateCode)) ChangeToRotateGizmo();
            if (Input.GetKeyDown(scaleCode)) ChangeToScaleGizmo();
            if (Input.GetKeyDown(localControlCode)) ToggleLocalControl(!localControl);
            
            // Handle duplication with Ctrl+D
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(duplicateCode))
            {
                _tools.DuplicateSelectedObjects();
            }
            
            // Handle quick rotate with R
            if (Input.GetKeyDown(quickRotateCode))
            {
                _tools.QuickRotateObjects();
            }
            
            // Handle Undo/Redo
            if (_history.historyEnabled && Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.Z)) _history.Undo();
                if (Input.GetKeyDown(KeyCode.Y)) _history.Redo();
            }
            
            // Update active tool
            if (!UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                _tools.UpdateSelectedObjects();
            }
            
            // Adjust gizmo scale based on camera distance
            if (_activeToolType != 2) // Don't adjust scale when using Scale tool
            {
                float scale = Vector3.Distance(Camera.main.transform.position, transform.position) / scaleS;
                transform.localScale = Vector3.one * scale;
            }
            else
            {
                // Fixed scale for scale tool
                transform.localScale = Vector3.one;
            }
            
            // Handle the active tool
            _tools.UpdateActiveTool(_activeToolType);
        }
        
        private void UpdateTool(int newToolType)
        {
            if (_activeToolType != newToolType)
            {
                _previousToolType = _activeToolType;
                _activeToolType = newToolType;
                
                // Trigger tool change event
                if (GizmoEvents.Instance != null)
                {
                    GizmoEvents.Instance.TriggerToolChanged(_previousToolType, _activeToolType);
                }
            }
        }
        
        public void ToggleLocalControl(bool value)
        {
            localControl = value;
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.TriggerToggleLocalControl(localControl);
            }
            GizmoEvents.Instance.TriggerToggleLocalControl(localControl);
        }

        public void ToggleSnap(bool value)
        {
            snap = value;
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.TriggerToggleSnapping(snap);
            }
            GizmoEvents.Instance.TriggerToggleSnapping(snap);
        }
        
        /// <summary>
        /// Toggles rotation snap on or off
        /// </summary>
        public void ToggleRotationSnap(bool value)
        {
            rotationSnap = value;
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.TriggerToggleRotationSnapping(rotationSnap);
            }
        }
        
        #region Tool Mode Methods
        
        public void ChangeToSelectGizmo()
        {
            UpdateTool(3);
        }
        
        public void ChangeToMoveGizmo()
        {
            UpdateTool(0);
        }
        
        public void ChangeToRotateGizmo()
        {
            UpdateTool(1);
        }
        
        public void ChangeToScaleGizmo()
        {
            UpdateTool(2);
        }
        
        public void ChangeToTrashGizmo()
        {
            UpdateTool(5);
        }
        
        #endregion
    }
} 