using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Reflection;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Manages user interface for Gizmo tools.
    /// </summary>
    public class GizmoUI : MonoBehaviour
    {
        [Header("UI References")]
        public Button selectButton;
        public Button moveButton;
        public Button rotateButton;
        public Button deleteButton;
        public Button duplicateButton;
        public Button quickRotateButton;
        public Button groupButton;
        public Button ungroupButton;
        public Toggle localGlobalToggleButton;

        public Toggle snapToggleButton;
        public Toggle rotationSnapToggleButton;

        public TMP_InputField gridSnapInputField;
        public TMP_InputField rotationSnapInputField;
        
        [Header("UI Indicators")]
        public Text currentToolText;
        public Text localGlobalText;
        public Image toolModePanel;
        
        [Header("Tool Colors")]
        public Color selectColor = new Color(0.2f, 0.6f, 1.0f);
        public Color moveColor = new Color(1.0f, 0.6f, 0.2f);
        public Color rotateColor = new Color(0.2f, 1.0f, 0.6f);
        public Color deleteColor = new Color(1.0f, 0.2f, 0.2f);
        
        private GizmoController controller;
        
        private void Awake()
        {
            controller = GetComponentInParent<GizmoController>();
        }
        
        private void Start()
        {
            if (controller == null)
            {
                Debug.LogError("GizmoUI: GizmoController not found!");
                return;
            }
            
            // Button assignments
            if (selectButton != null)
                selectButton.onClick.AddListener(OnSelectButtonClicked);
            
            if (moveButton != null)
                moveButton.onClick.AddListener(OnMoveButtonClicked);
            
            if (rotateButton != null)
                rotateButton.onClick.AddListener(OnRotateButtonClicked);
            
            if (deleteButton != null)
                deleteButton.onClick.AddListener(OnDeleteButtonClicked);
            
            if (duplicateButton != null)
                duplicateButton.onClick.AddListener(OnDuplicateButtonClicked);
            
            if (quickRotateButton != null)
                quickRotateButton.onClick.AddListener(OnQuickRotateButtonClicked);
            
            if (groupButton != null)
                groupButton.onClick.AddListener(OnGroupButtonClicked);
            
            if (ungroupButton != null)
                ungroupButton.onClick.AddListener(OnUngroupButtonClicked);
            
            if (localGlobalToggleButton != null)
                localGlobalToggleButton.onValueChanged.AddListener(OnLocalGlobalToggleClicked);
            
            if (gridSnapInputField != null)
                gridSnapInputField.onValueChanged.AddListener(OnGridSnapInputFieldChanged);
            
            if (snapToggleButton != null)
                snapToggleButton.onValueChanged.AddListener(OnSnapToggleClicked);
            
            if (rotationSnapToggleButton != null)
                rotationSnapToggleButton.onValueChanged.AddListener(OnRotationSnapToggleClicked);
                
            if (rotationSnapInputField != null)
                rotationSnapInputField.onValueChanged.AddListener(OnRotationSnapInputFieldChanged);
            
            // Default setting
            UpdateUI(3); // Select tool
            UpdateSnapToggleVisual(controller.snap);
            UpdateLocalGlobalToggleVisual(controller.localControl);
            UpdateGridSnapValueVisual(controller.gridSnap);
            
            UpdateRotationSnapToggleVisual(controller.rotationSnap);
            UpdateRotationSnapValueVisual(controller.rotationSnapAngle);
            
            // Add event listeners
            GizmoEvents.Instance.OnToggleSnapping.AddListener(UpdateSnapToggleVisual);
            GizmoEvents.Instance.OnToggleLocalControl.AddListener(UpdateLocalGlobalToggleVisual);
            GizmoEvents.Instance.OnGridSnapValueChanged.AddListener(UpdateGridSnapValueVisual);
            
            if (GizmoEvents.Instance != null)
            {
                if (GizmoEvents.Instance.OnToggleRotationSnapping != null)
                    GizmoEvents.Instance.OnToggleRotationSnapping.AddListener(UpdateRotationSnapToggleVisual);
                
                if (GizmoEvents.Instance.OnRotationSnapValueChanged != null)
                    GizmoEvents.Instance.OnRotationSnapValueChanged.AddListener(UpdateRotationSnapValueVisual);
            }
        }
        
        private void Update()
        {
            // Update local/global mode indicator
            if (localGlobalText != null)
            {
                localGlobalText.text = controller.localControl ? "Local" : "Global";
            }
        }
        
        private void OnSelectButtonClicked()
        {
            controller.ChangeToSelectGizmo();
            UpdateUI(3);
        }
        
        private void OnMoveButtonClicked()
        {
            controller.ChangeToMoveGizmo();
            UpdateUI(0);
        }
        
        private void OnRotateButtonClicked()
        {
            controller.ChangeToRotateGizmo();
            UpdateUI(1);
        }
        
        private void OnDeleteButtonClicked()
        {
            controller.ChangeToTrashGizmo();
            UpdateUI(5);
        }
        
        private void OnDuplicateButtonClicked()
        {
            GizmoTools tools = controller.GetComponent<GizmoTools>();
            if (tools != null)
            {
                tools.DuplicateSelectedObjects();
            }
        }
        
        private void OnQuickRotateButtonClicked()
        {
            GizmoTools tools = controller.GetComponent<GizmoTools>();
            if (tools != null)
            {
                tools.QuickRotateObjects();
            }
        }
        
        /// <summary>
        /// Handles the Group button click
        /// </summary>
        public void OnGroupButtonClicked()
        {
            Debug.Log("Group button clicked");
            
            // Force selection update first - this makes sure the internal selection state is consistent
            GizmoTools tools = controller.GetComponent<GizmoTools>();
            if (tools != null)
            {
                tools.UpdateSelectedObjects();
            }
            
            // Use the RuntimeBlocksManager for consistent selection state
            if (RuntimeBlocksManager.Instance != null)
            {
                GameObject[] selectedObjects = RuntimeBlocksManager.Instance.GetSelectedObjects();
                Debug.Log($"RuntimeBlocksManager reports {(selectedObjects != null ? selectedObjects.Length : 0)} selected objects");
                
                // If we have at least 2 objects selected, proceed directly
                if (selectedObjects != null && selectedObjects.Length >= 2)
                {
                    Debug.Log($"Grouping {selectedObjects.Length} objects");
                    RuntimeBlocksManager.Instance.GroupSelectedObjects();
                    return;
                }
                
                // If no objects are selected in the RuntimeBlocksManager, check GizmoTools directly
                if (selectedObjects == null || selectedObjects.Length < 2)
                {
                    Debug.Log("Not enough objects in RuntimeBlocksManager selection, checking GizmoTools...");
                    
                    // Get objects directly from GizmoTools
                    GizmoTools innerTools = controller.GetComponent<GizmoTools>();
                    if (innerTools != null)
                    {
                        GameObject[] toolsSelectedObjects = innerTools.GetSelectedObjects();
                        Debug.Log($"GizmoTools has {(toolsSelectedObjects != null ? toolsSelectedObjects.Length : 0)} selected objects");
                        
                        if (toolsSelectedObjects != null && toolsSelectedObjects.Length >= 2)
                        {
                            Debug.Log($"Found {toolsSelectedObjects.Length} objects selected in GizmoTools, syncing to RuntimeBlocksManager");
                            
                            // Convert to list for the method
                            List<GameObject> objectsList = new List<GameObject>(toolsSelectedObjects);
                            RuntimeBlocksManager.Instance.SetSelectedObjects(objectsList);
                            
                            // Now group the objects after syncing
                            RuntimeBlocksManager.Instance.GroupSelectedObjects();
                            return;
                        }
                    }
                    
                    // Fallback to checking visual selection (outlines)
                    Debug.Log("Checking for objects with outlines enabled...");
                    GameObject[] innerObjectsWithTag = GameObject.FindGameObjectsWithTag(controller.tagToFind);
                    Debug.Log($"Found {innerObjectsWithTag.Length} tagged objects in scene");
                    
                    List<GameObject> objectsWithOutlines = new List<GameObject>();
                    
                    foreach (GameObject obj in innerObjectsWithTag)
                    {
                        if (obj != null)
                        {
                            BlockToEdit blockToEdit = obj.GetComponent<BlockToEdit>();
                            if (blockToEdit != null)
                            {
                                Outline outline = obj.GetComponent<Outline>();
                                if (outline != null && outline.enabled)
                                {
                                    objectsWithOutlines.Add(obj);
                                    Debug.Log($"Found object with outline: {obj.name}");
                                }
                                else if (blockToEdit.selected)
                                {
                                    // The object is selected but the outline might not be visible
                                    // This happens sometimes due to internal state inconsistencies
                                    objectsWithOutlines.Add(obj);
                                    Debug.Log($"Found selected object (no visible outline): {obj.name}");
                                }
                            }
                        }
                    }
                    
                    if (objectsWithOutlines.Count >= 2)
                    {
                        Debug.Log($"Found {objectsWithOutlines.Count} objects with outlines or selected state, syncing to RuntimeBlocksManager");
                        RuntimeBlocksManager.Instance.SetSelectedObjects(objectsWithOutlines);
                        RuntimeBlocksManager.Instance.GroupSelectedObjects();
                        return;
                    }
                    
                    Debug.LogWarning("Could not find enough selected objects to group after all attempts.");
                    return;
                }
                
                // Group the objects
                RuntimeBlocksManager.Instance.GroupSelectedObjects();
                return;
            }
            
            // Original implementation as fallback
            // Fallback to the old direct GizmoTools way if RuntimeBlocksManager is not available
            if (tools == null)
            {
                Debug.LogError("GizmoTools not found!");
                return;
            }

            // Get the objects directly from GizmoTools
            GameObject[] directSelectedObjects = tools.GetSelectedObjects();
            Debug.Log($"Direct access of controlObjects: {(directSelectedObjects != null ? directSelectedObjects.Length : 0)} objects");
            
            if (directSelectedObjects != null && directSelectedObjects.Length >= 2)
            {
                // Explicitly mark all these objects as selected (fix selection state)
                foreach (var obj in directSelectedObjects)
                {
                    if (obj != null)
                    {
                        BlockToEdit blockToEdit = obj.GetComponent<BlockToEdit>();
                        if (blockToEdit != null)
                        {
                            blockToEdit.selected = true;
                            blockToEdit.EnableOutline();
                        }
                    }
                }
                
                // Group the selected objects directly
                tools.GroupSelectedObjects();
                return;
            }
            
            // Fallback to the existing outline-based detection
            Debug.Log("Fallback: searching for objects with enabled outlines");
            GameObject[] objectsWithTag = GameObject.FindGameObjectsWithTag(controller.tagToFind);
            List<GameObject> objectsToGroup = new List<GameObject>();
            
            // Select all objects that are visually highlighted with outline
            foreach (GameObject obj in objectsWithTag)
            {
                if (obj != null)
                {
                    BlockToEdit blockToEdit = obj.GetComponent<BlockToEdit>();
                    if (blockToEdit != null)
                    {
                        Outline outline = obj.GetComponent<Outline>();
                        if (outline != null && outline.enabled)
                        {
                            Debug.Log($"Found selected object with outline: {obj.name}");
                            objectsToGroup.Add(obj);
                            blockToEdit.selected = true;
                        }
                        else if (blockToEdit.selected)
                        {
                            Debug.Log($"Found selected object (no visible outline): {obj.name}");
                            objectsToGroup.Add(obj);
                        }
                    }
                }
            }
            
            Debug.Log($"Found {objectsToGroup.Count} objects to group");
            
            if (objectsToGroup.Count >= 2)
            {
                tools.SetControlObjects(objectsToGroup.ToArray());
                tools.GroupSelectedObjects();
            }
            else
            {
                Debug.LogWarning("Not enough objects selected to group (need at least 2)");
            }
        }
        
        private void OnUngroupButtonClicked()
        {
            Debug.Log("Ungroup button clicked");
            
            // Use the RuntimeBlocksManager for consistent selection state
            if (RuntimeBlocksManager.Instance != null)
            {
                RuntimeBlocksManager.Instance.UngroupSelectedObjects();
                return;
            }
            
            // Fallback to direct GizmoTools way
            GizmoTools tools = controller.GetComponent<GizmoTools>();
            if (tools != null)
            {
                tools.UngroupSelectedObjects();
            }
        }
        
        private void OnLocalGlobalToggleClicked(bool value)
        {
            controller.ToggleLocalControl(value);
        }

        private void OnSnapToggleClicked(bool value)
        {
            controller.ToggleSnap(value);
        }

        private void OnGridSnapInputFieldChanged(string value)
        {
            if (gridSnapInputField != null)
            {
                controller.gridSnap = float.Parse(value);
            }
        }

        private void OnRotationSnapToggleClicked(bool value)
        {
            controller.ToggleRotationSnap(value);
        }

        private void OnRotationSnapInputFieldChanged(string value)
        {
            if (rotationSnapInputField != null && float.TryParse(value, out float snapValue))
            {
                controller.rotationSnapAngle = snapValue;
                
                if (GizmoEvents.Instance != null && GizmoEvents.Instance.OnRotationSnapValueChanged != null)
                {
                    GizmoEvents.Instance.TriggerRotationSnapValueChanged(snapValue);
                }
            }
        }

        #region UI Update Visuals
        private void UpdateUI(int toolType)
        {
            if (currentToolText != null)
            {
                switch (toolType)
                {
                    case 0:
                        currentToolText.text = "Move";
                        if (toolModePanel != null) toolModePanel.color = moveColor;
                        break;
                    case 1:
                        currentToolText.text = "Rotate";
                        if (toolModePanel != null) toolModePanel.color = rotateColor;
                        break;
                    case 3:
                        currentToolText.text = "Select";
                        if (toolModePanel != null) toolModePanel.color = selectColor;
                        break;
                    case 5:
                        currentToolText.text = "Delete";
                        if (toolModePanel != null) toolModePanel.color = deleteColor;
                        break;
                }
            }
        }

        private void UpdateSnapToggleVisual(bool value)
        {
            if (snapToggleButton != null)
            {
                snapToggleButton.isOn = value;
            }
        }

        private void UpdateLocalGlobalToggleVisual(bool value)
        {
            if (localGlobalToggleButton != null)
            {
                localGlobalToggleButton.isOn = value;
            }
        }

        private void UpdateGridSnapValueVisual(float value)
        {
            if (gridSnapInputField != null)
            {
                gridSnapInputField.text = value.ToString();
            }
        }
        
        private void UpdateRotationSnapToggleVisual(bool value)
        {
            if (rotationSnapToggleButton != null)
            {
                rotationSnapToggleButton.isOn = value;
            }
        }
        
        private void UpdateRotationSnapValueVisual(float value)
        {
            if (rotationSnapInputField != null)
            {
                rotationSnapInputField.text = value.ToString();
            }
        }
        #endregion
    }
} 