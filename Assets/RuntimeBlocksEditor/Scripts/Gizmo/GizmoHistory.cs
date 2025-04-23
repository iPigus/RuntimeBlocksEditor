using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.UI;
namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Manages operation history (undo/redo) for Gizmo tools.
    /// </summary>
    public class GizmoHistory : MonoBehaviour
    {
        public bool historyEnabled = true;
        public int historySize = 100;
        
        private Stack<HistoryState> undoStack = new Stack<HistoryState>();
        private Stack<HistoryState> redoStack = new Stack<HistoryState>();

        [Header("UI References")]
        public Button undoButton;
        public Button redoButton;
        
        // Track if the last action was a selection to avoid saving selection-only actions
        private bool lastActionWasSelection = false;

        // Add a recursion counter at the class level
        private int operationDepth = 0;
        private const int MAX_RECURSION_DEPTH = 5;

        // Add a flag to prevent recursive operations
        private bool isPerformingOperation = false;

        private void Start()
        {
            if (undoButton != null) undoButton.onClick.AddListener(Undo);
            if (redoButton != null) redoButton.onClick.AddListener(Redo);
            
            // Subscribe to relevant events
            if (GizmoEvents.Instance != null)
            {
                // Track tool changes to ignore them for history
                GizmoEvents.Instance.OnToolChanged.AddListener((prevTool, newTool) => { 
                    lastActionWasSelection = true; 
                });
                
                // Track selection to ignore pure selection actions
                GizmoEvents.Instance.OnObjectSelected.AddListener((obj, selected) => { 
                    lastActionWasSelection = true; 
                });
                GizmoEvents.Instance.OnObjectDeselected.AddListener((obj, selected) => { 
                    lastActionWasSelection = true; 
                });
                GizmoEvents.Instance.OnSelectionUpdated.AddListener((objects) => { 
                    lastActionWasSelection = true; 
                });
            }
        }

        /// <summary>
        /// Determines if an action should be saved to history based on the action type.
        /// </summary>
        /// <param name="actionType">Type of action being performed</param>
        /// <returns>True if the action should be saved to history</returns>
        public bool ShouldSaveAction(HistoryActionType actionType)
        {
            // Only save meaningful actions (not selections or tool changes)
            return actionType != HistoryActionType.Selection && 
                   actionType != HistoryActionType.ToolChange;
        }

        /// <summary>
        /// Saves the current state of objects to the operation history for a specific action type.
        /// </summary>
        /// <param name="controlObjects">Objects whose state should be saved</param>
        /// <param name="actionType">Type of action being performed</param>
        public void SaveState(GameObject[] controlObjects, HistoryActionType actionType = HistoryActionType.Transform)
        {
            // Skip if history is disabled, no objects provided, or it's not a tracked action type
            if (!historyEnabled || controlObjects == null || controlObjects.Length == 0 || !ShouldSaveAction(actionType))
                return;

            // Reset selection tracking for transformative actions
            if (actionType != HistoryActionType.Selection)
            {
                lastActionWasSelection = false;
            }
            
            // Simple duplicate check - only check if the previous operation was the same type
            // on the same objects and there were no actual changes to the transforms
            if (undoStack.Count > 0 && actionType == HistoryActionType.Transform)
            {
                HistoryState lastState = undoStack.Peek();
                
                // Only check for duplicates in transform operations
                if (lastState.ActionType == HistoryActionType.Transform && lastState.Objects.Length == controlObjects.Length)
                {
                    bool sameObjects = true;
                    for (int i = 0; i < controlObjects.Length; i++)
                    {
                        if (i >= lastState.Objects.Length || lastState.Objects[i] != controlObjects[i])
                        {
                            sameObjects = false;
                            break;
                        }
                    }
                    
                    // Only skip if it's the exact same objects with the exact same transforms
                    if (sameObjects)
                    {
                        bool noChanges = true;
                        for (int i = 0; i < controlObjects.Length; i++)
                        {
                            if (controlObjects[i] != null && i < lastState.Positions.Length)
                            {
                                // Check for any change in position, rotation, or scale
                                // Using Approximately to avoid floating point precision issues
                                if (!ApproximatelyEqual(controlObjects[i].transform.position, lastState.Positions[i]) ||
                                    !ApproximatelyEqual(controlObjects[i].transform.rotation, lastState.Rotations[i]) ||
                                    !ApproximatelyEqual(controlObjects[i].transform.localScale, lastState.Scales[i]))
                                {
                                    noChanges = false;
                                    break;
                                }
                            }
                        }
                        
                        // If absolutely nothing changed, skip saving this state
                        if (noChanges)
                        {
                            Debug.Log("Skipping duplicate history entry - no transform changes detected");
                            return;
                        }
                    }
                }
            }

            HistoryState state = new HistoryState
            {
                Objects = controlObjects.ToArray(), // Create a copy to avoid reference issues
                Positions = controlObjects.Select(obj => obj != null ? obj.transform.position : Vector3.zero).ToArray(),
                Scales = controlObjects.Select(obj => obj != null ? obj.transform.localScale : Vector3.one).ToArray(),
                Rotations = controlObjects.Select(obj => obj != null ? obj.transform.rotation : Quaternion.identity).ToArray(),
                ActionType = actionType,
                IsBlockPlacement = actionType == HistoryActionType.Creation,
                IsDeletion = actionType == HistoryActionType.Deletion
            };
            
            // For Group operations, store group-specific data
            if (actionType == HistoryActionType.Group)
            {
                state.GroupMemberIDs = controlObjects.Select(obj => obj != null ? obj.GetInstanceID() : -1).ToArray();
                
                // Get group data from RuntimeBlocksManager
                RuntimeBlocksManager manager = RuntimeBlocksManager.Instance;
                if (manager != null)
                {
                    // Find the group containing these objects
                    foreach (var group in manager.GetAllGroups())
                    {
                        if (group.Value.members.SequenceEqual(controlObjects))
                        {
                            state.GroupID = group.Key;
                            state.GroupPivot = group.Value.pivot;
                            state.GroupName = group.Value.name;
                            break;
                        }
                    }
                }
            }
            // For Ungroup operations, store group data before ungrouping
            else if (actionType == HistoryActionType.Ungroup)
            {
                state.GroupMemberIDs = controlObjects.Select(obj => obj != null ? obj.GetInstanceID() : -1).ToArray();
                
                // Get group data from RuntimeBlocksManager before ungrouping
                RuntimeBlocksManager manager = RuntimeBlocksManager.Instance;
                if (manager != null)
                {
                    // Find the group containing these objects
                    foreach (var group in manager.GetAllGroups())
                    {
                        if (group.Value.members.SequenceEqual(controlObjects))
                        {
                            state.GroupID = group.Key;
                            state.GroupPivot = group.Value.pivot;
                            state.GroupName = group.Value.name;
                            break;
                        }
                    }
                }
            }

            undoStack.Push(state);
            redoStack.Clear();

            if (undoStack.Count > historySize)
            {
                undoStack = new Stack<HistoryState>(undoStack.Take(historySize));
            }
            
            // Update UI button states
            UpdateButtonStates();
            
            // Debug info
            Debug.Log($"Saved state: {actionType} - Objects: {controlObjects.Length} - Stack size: {undoStack.Count}");
        }
        
        /// <summary>
        /// Helper method to check if two Vector3s are approximately equal
        /// </summary>
        private bool ApproximatelyEqual(Vector3 a, Vector3 b, float epsilon = 0.001f)
        {
            return Mathf.Abs(a.x - b.x) < epsilon &&
                   Mathf.Abs(a.y - b.y) < epsilon &&
                   Mathf.Abs(a.z - b.z) < epsilon;
        }
        
        /// <summary>
        /// Helper method to check if two Quaternions are approximately equal
        /// </summary>
        private bool ApproximatelyEqual(Quaternion a, Quaternion b, float epsilon = 0.01f)
        {
            return Quaternion.Angle(a, b) < epsilon;
        }
        
        /// <summary>
        /// Overload of SaveState that uses the default Transform action type
        /// </summary>
        public void SaveState(GameObject[] controlObjects)
        {
            SaveState(controlObjects, HistoryActionType.Transform);
        }
        
        /// <summary>
        /// Saves the state after deleting an object.
        /// </summary>
        /// <param name="deletedObject">Deleted object</param>
        public void SaveDeletionState(GameObject deletedObject)
        {
            if (!historyEnabled || deletedObject == null) return;
            
            // Always track deletions
            lastActionWasSelection = false;
            
            HistoryState deleteState = new HistoryState
            {
                Objects = new GameObject[] { deletedObject },
                Positions = new Vector3[] { deletedObject.transform.position },
                Scales = new Vector3[] { deletedObject.transform.localScale },
                Rotations = new Quaternion[] { deletedObject.transform.rotation },
                ActionType = HistoryActionType.Deletion,
                IsBlockPlacement = true,
                IsDeletion = true
            };
            
            undoStack.Push(deleteState);
            redoStack.Clear();
            
            // Update UI button states
            UpdateButtonStates();
        }
        
        /// <summary>
        /// Updates the enabled state of the UI buttons based on stack contents
        /// </summary>
        private void UpdateButtonStates()
        {
            if (undoButton != null)
                undoButton.interactable = undoStack.Count > 0;
                
            if (redoButton != null)
                redoButton.interactable = redoStack.Count > 0;
        }
        
        /// <summary>
        /// Undoes the last operation.
        /// </summary>
        public void Undo()
        {
            // Check for potential issues before proceeding
            SafeguardAgainstStackOverflow();
            
            // Prevent recursive undo operations
            if (isPerformingOperation)
            {
                Debug.LogWarning("Skipping nested Undo operation to prevent recursion");
                return;
            }

            if (undoStack.Count == 0) return;

            // Track recursion
            operationDepth++;
            if (operationDepth > MAX_RECURSION_DEPTH)
            {
                Debug.LogError($"Maximum recursion depth exceeded ({MAX_RECURSION_DEPTH}). Breaking execution to prevent stack overflow.");
                operationDepth = 0;
                ClearHistory(); // Clear history as a safety measure
                return;
            }

            isPerformingOperation = true;
            
            try
            {
                // Get the latest state from the stack
                HistoryState state = undoStack.Pop();
                
                // Skip invalid objects
                if (state.Objects == null || state.Objects.Length == 0)
                {
                    // Clear this invalid state and try the next one if available
                    Debug.LogWarning("Skipping invalid history state with no objects");
                    isPerformingOperation = false;
                    if (undoStack.Count > 0)
                        Undo();
                    return;
                }
                
                // Create a redo state based on the current state
                HistoryState redoState = CreateRedoState(state);
                
                // Handle special cases for Group/Ungroup operations
                if (state.ActionType == HistoryActionType.Group)
                {
                    // Undo a group operation = ungroup
                    UndoGroupOperation(state, redoState);
                }
                else if (state.ActionType == HistoryActionType.Ungroup)
                {
                    // Undo an ungroup operation = recreate the group
                    UndoUngroupOperation(state, redoState);
                }
                else if (state.IsBlockPlacement)
                {
                    // Handle placement/deletion operations as before
                    if (!state.IsDeletion)
                    {
                        // Undo placement - delete the block
                        if (state.Objects[0] != null)
                        {
                            Destroy(state.Objects[0]);
                        }
                    }
                    else
                    {
                        // Undo deletion - recreate the block
                        Debug.LogWarning("Undo deletion not fully implemented - can't recreate objects");
                    }
                    
                    // Push to redo stack
                    redoStack.Push(redoState);
                }
                else
                {
                    // Handle paired transform states for normal transforms
                    if (state.ActionType == HistoryActionType.Transform && undoStack.Count > 0)
                    {
                        HistoryState nextState = undoStack.Peek();
                        if (nextState.ActionType == HistoryActionType.Transform && 
                            nextState.Objects.Length == state.Objects.Length)
                        {
                            // Check if both states reference the same objects
                            bool sameObjects = true;
                            for (int i = 0; i < state.Objects.Length; i++)
                            {
                                if (i >= nextState.Objects.Length || nextState.Objects[i] != state.Objects[i])
                                {
                                    sameObjects = false;
                                    break;
                                }
                            }
                            
                            if (sameObjects)
                            {
                                // This means we have a "end state" followed by a "start state"
                                // Pop the start state as well and use it for the undo operation
                                undoStack.Pop(); // Remove the start state
                                
                                // Use the "start state" for the undo operation
                                state = nextState;
                                
                                // Recreate redo state with the new state
                                redoState = CreateRedoState(state);
                                
                                Debug.Log("Found paired transform states, undoing to initial state directly");
                            }
                        }
                    }
                    
                    // Undo move/rotate/scale - standard transform undo
                    for (int i = 0; i < state.Objects.Length; i++)
                    {
                        if (state.Objects[i] != null)
                        {
                            state.Objects[i].transform.position = state.Positions[i];
                            state.Objects[i].transform.localScale = state.Scales[i];
                            state.Objects[i].transform.rotation = state.Rotations[i];
                        }
                    }
                    
                    // Push to redo stack
                    redoStack.Push(redoState);
                }
                
                // Update UI button states
                UpdateButtonStates();
                
                // Debug info
                Debug.Log($"Undo operation: {state.ActionType} - Objects: {state.Objects.Length} - Undo stack: {undoStack.Count} - Redo stack: {redoStack.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in Undo operation: {e.Message}\n{e.StackTrace}");
                ClearHistory(); // Clear history on exception to prevent further issues
            }
            finally
            {
                // Always reset the operation flag and decrement depth
                isPerformingOperation = false;
                operationDepth--;
            }
        }

        // Helper method to create a redo state from the current transforms
        private HistoryState CreateRedoState(HistoryState undoState)
        {
            HistoryState redoState = new HistoryState
            {
                Objects = undoState.Objects,
                Positions = undoState.Objects.Select(obj => obj != null ? obj.transform.position : Vector3.zero).ToArray(),
                Scales = undoState.Objects.Select(obj => obj != null ? obj.transform.localScale : Vector3.one).ToArray(),
                Rotations = undoState.Objects.Select(obj => obj != null ? obj.transform.rotation : Quaternion.identity).ToArray(),
                ActionType = undoState.ActionType,
                IsBlockPlacement = undoState.IsBlockPlacement,
                IsDeletion = undoState.IsDeletion,
                // Don't copy direct references to child objects
                GroupMemberIDs = undoState.GroupMemberIDs,
                GroupID = undoState.GroupID,
                GroupPivot = undoState.GroupPivot,
                GroupName = undoState.GroupName
            };
            
            return redoState;
        }

        // Helper method to undo a group operation
        private void UndoGroupOperation(HistoryState state, HistoryState redoState)
        {
            Debug.Log("Starting UndoGroupOperation using reference-based grouping");
            
            // Get the objects that were grouped
            List<GameObject> validObjects = new List<GameObject>();
            
            // Try to find objects using instance IDs
            if (state.GroupMemberIDs != null)
            {
                foreach (int instanceID in state.GroupMemberIDs)
                {
                    if (instanceID != -1)
                    {
                        GameObject obj = FindObjectByInstanceID(instanceID);
                        if (obj != null)
                            validObjects.Add(obj);
                    }
                }
            }
            
            if (validObjects.Count == 0)
            {
                Debug.LogWarning("Cannot undo group - no valid objects found");
                return;
            }
            
            // Use RuntimeBlocksManager to ungroup
            RuntimeBlocksManager manager = RuntimeBlocksManager.Instance;
            if (manager != null)
            {
                // First select all objects to be ungrouped
                manager.SetSelectedObjects(validObjects);
                
                // Then ungroup them
                manager.UngroupSelectedObjects();
                
                Debug.Log($"UndoGroupOperation: Ungrouped {validObjects.Count} objects using RuntimeBlocksManager");
                
                // Create redo state
                redoState.Objects = validObjects.ToArray();
                redoState.ActionType = HistoryActionType.Group;
                redoState.GroupMemberIDs = validObjects.Select(obj => obj.GetInstanceID()).ToArray();
                redoState.GroupID = state.GroupID;
                redoState.GroupPivot = state.GroupPivot;
                redoState.GroupName = state.GroupName;
                
                // Push to redo stack
                redoStack.Push(redoState);
            }
            else
            {
                Debug.LogError("Cannot undo group - RuntimeBlocksManager not found");
            }
        }

        // Helper method to undo an ungroup operation
        private void UndoUngroupOperation(HistoryState state, HistoryState redoState)
        {
            Debug.Log("Starting UndoUngroupOperation using reference-based grouping");
            
            // Get the objects that were in the group
            List<GameObject> validObjects = new List<GameObject>();
            
            // Try to find objects using instance IDs
            if (state.GroupMemberIDs != null)
            {
                foreach (int instanceID in state.GroupMemberIDs)
                {
                    if (instanceID != -1)
                    {
                        GameObject obj = FindObjectByInstanceID(instanceID);
                        if (obj != null)
                            validObjects.Add(obj);
                    }
                }
            }
            
            if (validObjects.Count < 2)
            {
                Debug.LogWarning("Cannot undo ungroup - need at least 2 valid objects to regroup");
                return;
            }
            
            // Use RuntimeBlocksManager to create the group
            RuntimeBlocksManager manager = RuntimeBlocksManager.Instance;
            if (manager != null)
            {
                // First select all objects to be grouped
                manager.SetSelectedObjects(validObjects);
                
                // Then create the group
                manager.GroupSelectedObjects();
                
                Debug.Log($"UndoUngroupOperation: Created group with {validObjects.Count} objects using RuntimeBlocksManager");
                
                // Create redo state
                redoState.Objects = validObjects.ToArray();
                redoState.ActionType = HistoryActionType.Ungroup;
                redoState.GroupMemberIDs = validObjects.Select(obj => obj.GetInstanceID()).ToArray();
                redoState.GroupID = state.GroupID;
                redoState.GroupPivot = state.GroupPivot;
                redoState.GroupName = state.GroupName;
                
                // Push to redo stack
                redoStack.Push(redoState);
            }
            else
            {
                Debug.LogError("Cannot undo ungroup - RuntimeBlocksManager not found");
            }
        }
        
        /// <summary>
        /// Redoes the last undone operation.
        /// </summary>
        public void Redo()
        {
            // Check for potential issues before proceeding
            SafeguardAgainstStackOverflow();
            
            // Prevent recursive redo operations
            if (isPerformingOperation)
            {
                Debug.LogWarning("Skipping nested Redo operation to prevent recursion");
                return;
            }

            if (redoStack.Count == 0) return;

            // Track recursion
            operationDepth++;
            if (operationDepth > MAX_RECURSION_DEPTH)
            {
                Debug.LogError($"Maximum recursion depth exceeded ({MAX_RECURSION_DEPTH}). Breaking execution to prevent stack overflow.");
                operationDepth = 0;
                ClearHistory(); // Clear history as a safety measure
                return;
            }

            isPerformingOperation = true;
            
            try
            {
                HistoryState state = redoStack.Pop();
                
                // Skip invalid objects
                if (state.Objects == null || state.Objects.Length == 0)
                {
                    // Clear this invalid state and try the next one if available
                    Debug.LogWarning("Skipping invalid history state with no objects");
                    isPerformingOperation = false;
                    if (redoStack.Count > 0)
                        Redo();
                    return;
                }
                
                // Check if all objects have been destroyed
                bool allObjectsDestroyed = true;
                foreach (var obj in state.Objects)
                {
                    if (obj != null)
                    {
                        allObjectsDestroyed = false;
                        break;
                    }
                }
                
                // If all objects were destroyed, skip this redo
                if (allObjectsDestroyed)
                {
                    Debug.LogWarning("Skipping history state with all destroyed objects");
                    isPerformingOperation = false;
                    if (redoStack.Count > 0)
                        Redo(); // Try the next one
                    return;
                }
                
                // Create undo state with current transforms - to allow undoing this redo
                HistoryState undoState = CreateRedoState(state); // Reuse the helper method
                
                // Handle special cases for Group/Ungroup operations
                if (state.ActionType == HistoryActionType.Group)
                {
                    // Redo a group operation = recreate the group
                    RedoGroupOperation(state, undoState);
                }
                else if (state.ActionType == HistoryActionType.Ungroup)
                {
                    // Redo an ungroup operation = ungroup again
                    RedoUngroupOperation(state, undoState);
                }
                else if (state.IsBlockPlacement)
                {
                    if (!state.IsDeletion)
                    {
                        // Redo placement - this would require recreating the block
                    }
                    else
                    {
                        // Redo deletion
                        if (state.Objects[0] != null)
                        {
                            Destroy(state.Objects[0]);
                        }
                    }
                    
                    // Push to undo stack
                    undoStack.Push(undoState);
                }
                else
                {
                    // Redo move/rotate/scale
                    for (int i = 0; i < state.Objects.Length; i++)
                    {
                        if (state.Objects[i] != null)
                        {
                            state.Objects[i].transform.position = state.Positions[i];
                            state.Objects[i].transform.localScale = state.Scales[i];
                            state.Objects[i].transform.rotation = state.Rotations[i];
                        }
                    }
                    
                    // Push to undo stack
                    undoStack.Push(undoState);
                }

                // Update UI button states
                UpdateButtonStates();
                
                // Debug info
                Debug.Log($"Redo operation: {state.ActionType} - Objects: {state.Objects.Length} - Undo stack: {undoStack.Count} - Redo stack: {redoStack.Count}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error in Redo operation: {e.Message}\n{e.StackTrace}");
                ClearHistory(); // Clear history on exception to prevent further issues
            }
            finally
            {
                // Always reset the operation flag and decrement depth
                isPerformingOperation = false;
                operationDepth--;
            }
        }

        // Helper method to redo a group operation
        private void RedoGroupOperation(HistoryState state, HistoryState undoState)
        {
            Debug.Log("Starting RedoGroupOperation using reference-based grouping");
            
            // Get the objects to group
            List<GameObject> validObjects = new List<GameObject>();
            
            // Try to find objects using instance IDs
            if (state.GroupMemberIDs != null)
            {
                foreach (int instanceID in state.GroupMemberIDs)
                {
                    if (instanceID != -1)
                    {
                        GameObject obj = FindObjectByInstanceID(instanceID);
                        if (obj != null)
                            validObjects.Add(obj);
                    }
                }
            }
            
            if (validObjects.Count < 2)
            {
                Debug.LogWarning("Cannot redo group operation - need at least 2 valid objects");
                return;
            }
            
            // Use RuntimeBlocksManager to create the group
            RuntimeBlocksManager manager = RuntimeBlocksManager.Instance;
            if (manager != null)
            {
                // First select all objects to be grouped
                manager.SetSelectedObjects(validObjects);
                
                // Then create the group
                manager.GroupSelectedObjects();
                
                Debug.Log($"RedoGroupOperation: Created group with {validObjects.Count} objects using RuntimeBlocksManager");
                
                // Create undo state
                undoState.Objects = validObjects.ToArray();
                undoState.ActionType = HistoryActionType.Group;
                undoState.GroupMemberIDs = validObjects.Select(obj => obj.GetInstanceID()).ToArray();
                undoState.GroupID = state.GroupID;
                undoState.GroupPivot = state.GroupPivot;
                undoState.GroupName = state.GroupName;
                
                // Push to undo stack
                undoStack.Push(undoState);
            }
            else
            {
                Debug.LogError("Cannot redo group - RuntimeBlocksManager not found");
            }
        }

        // Helper method to redo an ungroup operation
        private void RedoUngroupOperation(HistoryState state, HistoryState undoState)
        {
            Debug.Log("Starting RedoUngroupOperation using reference-based grouping");
            
            // Get the objects to ungroup
            List<GameObject> validObjects = new List<GameObject>();
            
            // Try to find objects using instance IDs
            if (state.GroupMemberIDs != null)
            {
                foreach (int instanceID in state.GroupMemberIDs)
                {
                    if (instanceID != -1)
                    {
                        GameObject obj = FindObjectByInstanceID(instanceID);
                        if (obj != null)
                            validObjects.Add(obj);
                    }
                }
            }
            
            if (validObjects.Count == 0)
            {
                Debug.LogWarning("Cannot redo ungroup - no valid objects found");
                return;
            }
            
            // Use RuntimeBlocksManager to ungroup
            RuntimeBlocksManager manager = RuntimeBlocksManager.Instance;
            if (manager != null)
            {
                // First select all objects to be ungrouped
                manager.SetSelectedObjects(validObjects);
                
                // Then ungroup them
                manager.UngroupSelectedObjects();
                
                Debug.Log($"RedoUngroupOperation: Ungrouped {validObjects.Count} objects using RuntimeBlocksManager");
                
                // Create undo state
                undoState.Objects = validObjects.ToArray();
                undoState.ActionType = HistoryActionType.Ungroup;
                undoState.GroupMemberIDs = validObjects.Select(obj => obj.GetInstanceID()).ToArray();
                undoState.GroupID = state.GroupID;
                undoState.GroupPivot = state.GroupPivot;
                undoState.GroupName = state.GroupName;
                
                // Push to undo stack
                undoStack.Push(undoState);
            }
            else
            {
                Debug.LogError("Cannot redo ungroup - RuntimeBlocksManager not found");
            }
        }
        
        /// <summary>
        /// Gets the last saved state for a specific GameObject from the undo stack.
        /// </summary>
        /// <param name="obj">The GameObject to get state for</param>
        /// <returns>A TransformState containing position, rotation, and scale</returns>
        public TransformState GetLastState(GameObject obj)
        {
            if (!historyEnabled || undoStack.Count == 0 || obj == null) 
            {
                // Return current transform if no history available
                return new TransformState
                {
                    position = obj.transform.position,
                    rotation = obj.transform.rotation,
                    localScale = obj.transform.localScale
                };
            }
            
            // Try to find the object in the last history state
            HistoryState lastState = undoStack.Peek();
            for (int i = 0; i < lastState.Objects.Length; i++)
            {
                if (lastState.Objects[i] == obj)
                {
                    return new TransformState
                    {
                        position = lastState.Positions[i],
                        rotation = lastState.Rotations[i],
                        localScale = lastState.Scales[i]
                    };
                }
            }
            
            // If not found in the last state, return current transform
            return new TransformState
            {
                position = obj.transform.position,
                rotation = obj.transform.rotation,
                localScale = obj.transform.localScale
            };
        }
        
        /// <summary>
        /// Defines types of actions that can be saved in history
        /// </summary>
        public enum HistoryActionType
        {
            Transform,   // Movement, rotation, or scale change
            Creation,    // Creation of new objects
            Deletion,    // Deletion of objects
            Group,       // Grouping objects
            Ungroup,     // Ungrouping objects
            Selection,   // Selection changes (not tracked)
            ToolChange   // Tool changes (not tracked)
        }
        
        /// <summary>
        /// Represents the transform state of an object.
        /// </summary>
        [System.Serializable]
        public struct TransformState
        {
            public Vector3 position;
            public Quaternion rotation;
            public Vector3 localScale;
        }
        
        /// <summary>
        /// Represents the state of objects in the operation history.
        /// </summary>
        [System.Serializable]
        private class HistoryState
        {
            public Vector3[] Positions;
            public Vector3[] Scales;
            public Quaternion[] Rotations;
            public GameObject[] Objects;
            public HistoryActionType ActionType;
            public bool IsBlockPlacement;
            public bool IsDeletion;
            
            // For group/ungroup operations
            public int[] GroupMemberIDs; // Store instance IDs of group members
            public int GroupID; // Store the group ID for group operations
            public Vector3 GroupPivot; // Store the group's pivot point
            public string GroupName; // Store the group's name
        }

        // Helper method to find an object by instance ID
        private GameObject FindObjectByInstanceID(int instanceID)
        {
            if (instanceID == -1) return null;
            
            // First check all existing control objects, most likely to find matches here
            if (GizmoTools.Singleton != null && GizmoTools.Singleton.controlObjects != null)
            {
                foreach (var obj in GizmoTools.Singleton.controlObjects)
                {
                    if (obj != null && obj.GetInstanceID() == instanceID)
                        return obj;
                }
            }
            
            // Try to get all objects with the Block tag
            try
            {
                // Try "Block" tag
                GameObject[] blockObjects = GameObject.FindGameObjectsWithTag("Block");
                foreach (var obj in blockObjects)
                {
                    if (obj != null && obj.GetInstanceID() == instanceID)
                        return obj;
                }
            }
            catch (UnityException)
            {
                // If "Block" tag doesn't exist, try "BlockTag" instead
                try
                {
                    GameObject[] blockTagObjects = GameObject.FindGameObjectsWithTag("BlockTag");
                    foreach (var obj in blockTagObjects)
                    {
                        if (obj != null && obj.GetInstanceID() == instanceID)
                            return obj;
                    }
                }
                catch (UnityException)
                {
                    Debug.LogError("Neither 'Block' nor 'BlockTag' tags are defined in tags manager!");
                }
            }
            
            // As a last resort, try to find the object by ID among all GameObjects
            GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
            foreach (var obj in allObjects)
            {
                if (obj != null && obj.GetInstanceID() == instanceID)
                    return obj;
            }
            
            return null;
        }

        /// <summary>
        /// Cleans up history stacks to prevent memory leaks and circular references.
        /// Call this when scenes change or when memory issues might occur.
        /// </summary>
        public void ClearHistory()
        {
            // Explicitly remove all references to Unity objects
            if (undoStack != null)
            {
                foreach (var state in undoStack)
                {
                    if (state != null)
                    {
                        // Clear object references
                        state.Objects = null;
                        state.GroupMemberIDs = null;
                    }
                }
                undoStack.Clear();
            }
            
            if (redoStack != null)
            {
                foreach (var state in redoStack)
                {
                    if (state != null)
                    {
                        // Clear object references
                        state.Objects = null;
                        state.GroupMemberIDs = null;
                    }
                }
                redoStack.Clear();
            }
            
            // Force a garbage collection after clearing references
            System.GC.Collect();
            
            // Update UI button states
            UpdateButtonStates();
            
            Debug.Log("History cleared to prevent memory issues");
        }

        private void OnDisable()
        {
            // Clear history when the component is disabled to prevent leaks
            ClearHistory();
        }

        // Call at start of potentially problematic operations
        private void SafeguardAgainstStackOverflow()
        {
            // If we detect too many history entries or deep recursion,
            // clear history to prevent potential issues
            if (undoStack.Count > historySize * 0.9f || operationDepth > MAX_RECURSION_DEPTH - 2)
            {
                Debug.LogWarning("Potential stack overflow detected - clearing history as a precaution");
                ClearHistory();
            }
        }
    }
} 