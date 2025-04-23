using UnityEngine;
using System;
using System.Collections.Generic;
using UnityEngine.Events;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Events that can be subscribed to for Gizmo operations.
    /// </summary>
    public class GizmoEvents : MonoBehaviour
    {
        // Singleton instance
        public static GizmoEvents Instance { get; private set; }
        
        // Flag to track if RuntimeBlocksManager has been registered
        public bool IsRuntimeBlocksManagerRegistered { get; private set; } = false;

        #region UnityEvent Types
        
        // Toggle event types
        [Serializable] public class ToggleSnappingUnityEvent : UnityEvent<bool> { }
        [Serializable] public class ToggleLocalControlUnityEvent : UnityEvent<bool> { }
        [Serializable] public class ToggleRotationSnappingUnityEvent : UnityEvent<bool> { }
        
        // Selection event types
        [Serializable] public class SelectionUnityEvent : UnityEvent<GameObject[]> { }
        [Serializable] public class ObjectSelectionUnityEvent : UnityEvent<GameObject, bool> { }
        
        // Movement event types
        [Serializable] public class MoveStartUnityEvent : UnityEvent<GameObject[], Vector3[]> { }
        [Serializable] public class MovingUnityEvent : UnityEvent<GameObject[], Vector3[], Vector3[]> { }
        [Serializable] public class MoveEndUnityEvent : UnityEvent<GameObject[], Vector3[], Vector3[]> { }
        
        // Rotation event types
        [Serializable] public class RotateStartUnityEvent : UnityEvent<GameObject[], Quaternion[]> { }
        [Serializable] public class RotatingUnityEvent : UnityEvent<GameObject[], Quaternion[], Quaternion[]> { }
        [Serializable] public class RotateEndUnityEvent : UnityEvent<GameObject[], Quaternion[], Quaternion[]> { }
        [Serializable] public class RotationSnapValueChangeUnityEvent : UnityEvent<float> { }
        
        // Scale event types
        [Serializable] public class ScaleStartUnityEvent : UnityEvent<GameObject[], Vector3[]> { }
        [Serializable] public class ScalingUnityEvent : UnityEvent<GameObject[], Vector3[], Vector3[]> { }
        [Serializable] public class ScaleEndUnityEvent : UnityEvent<GameObject[], Vector3[], Vector3[]> { }
        
        // Delete event types
        [Serializable] public class DeleteUnityEvent : UnityEvent<GameObject> { }
        [Serializable] public class MultiDeleteUnityEvent : UnityEvent<GameObject[]> { }
        
        // Duplicate event types
        [Serializable] public class DuplicateUnityEvent : UnityEvent<GameObject[], GameObject[]> { }
        
        // Quick rotate event type
        [Serializable] public class QuickRotateUnityEvent : UnityEvent<GameObject[], Quaternion[], Quaternion[]> { }
        
        // Tool change event type
        [Serializable] public class ToolChangeUnityEvent : UnityEvent<int, int> { }

        // Values change event types
        [Serializable] public class GridSnapValueChangeUnityEvent : UnityEvent<float> { }
        
        // Group/Ungroup events
        [Serializable] public class GroupUnityEvent : UnityEvent<GameObject, GameObject[]> { }
        
        #endregion
        
        #region Serialized Events
        
        // Toggle events
        [Header("Toggle Events")]
        [Tooltip("Triggered when snapping is toggled")]
        [SerializeField] private ToggleSnappingUnityEvent onToggleSnapping = new ToggleSnappingUnityEvent();
        
        [Tooltip("Triggered when local control is toggled")]
        [SerializeField] private ToggleLocalControlUnityEvent onToggleLocalControl = new ToggleLocalControlUnityEvent();

        [Tooltip("Triggered when rotation snapping is toggled")]
        [SerializeField] private ToggleRotationSnappingUnityEvent onToggleRotationSnapping = new ToggleRotationSnappingUnityEvent();

        // Selection events
        [Header("Selection Events")]
        [Tooltip("Triggered when the selection of objects changes")]
        [SerializeField] private SelectionUnityEvent onSelectionUpdated = new SelectionUnityEvent();
        
        [Tooltip("Triggered when an object is selected")]
        [SerializeField] private ObjectSelectionUnityEvent onObjectSelected = new ObjectSelectionUnityEvent();
        
        [Tooltip("Triggered when an object is deselected")]
        [SerializeField] private ObjectSelectionUnityEvent onObjectDeselected = new ObjectSelectionUnityEvent();
        
        // Movement events
        [Header("Movement Events")]
        [Tooltip("Triggered when objects start moving")]
        [SerializeField] private MoveStartUnityEvent onMoveStart = new MoveStartUnityEvent();
        
        [Tooltip("Triggered continuously during movement")]
        [SerializeField] private MovingUnityEvent onMoving = new MovingUnityEvent();
        
        [Tooltip("Triggered when movement completes")]
        [SerializeField] private MoveEndUnityEvent onMoveEnd = new MoveEndUnityEvent();
        
        // Rotation events
        [Header("Rotation Events")]
        [Tooltip("Triggered when rotation begins")]
        [SerializeField] private RotateStartUnityEvent onRotateStart = new RotateStartUnityEvent();
        
        [Tooltip("Triggered continuously during rotation")]
        [SerializeField] private RotatingUnityEvent onRotating = new RotatingUnityEvent();
        
        [Tooltip("Triggered when rotation completes")]
        [SerializeField] private RotateEndUnityEvent onRotateEnd = new RotateEndUnityEvent();
        
        // Scale events
        [Header("Scale Events")]
        [Tooltip("Triggered when scaling begins")]
        [SerializeField] private ScaleStartUnityEvent onScaleStart = new ScaleStartUnityEvent();
        
        [Tooltip("Triggered continuously during scaling")]
        [SerializeField] private ScalingUnityEvent onScaling = new ScalingUnityEvent();
        
        [Tooltip("Triggered when scaling completes")]
        [SerializeField] private ScaleEndUnityEvent onScaleEnd = new ScaleEndUnityEvent();
        
        // Delete events
        [Header("Delete Events")]
        [Tooltip("Triggered when a single object is deleted")]
        [SerializeField] private DeleteUnityEvent onObjectDeleted = new DeleteUnityEvent();
        
        [Tooltip("Triggered when multiple objects are deleted at once")]
        [SerializeField] private MultiDeleteUnityEvent onMultipleObjectsDeleted = new MultiDeleteUnityEvent();
        
        // Duplicate events
        [Header("Duplicate Events")]
        [Tooltip("Triggered when objects are duplicated")]
        [SerializeField] private DuplicateUnityEvent onObjectsDuplicated = new DuplicateUnityEvent();
        
        // Quick rotate events
        [Header("Quick Rotate Events")]
        [Tooltip("Triggered when objects are quickly rotated")]
        [SerializeField] private QuickRotateUnityEvent onQuickRotated = new QuickRotateUnityEvent();
        
        // Tool change events
        [Header("Tool Change Events")]
        [Tooltip("Triggered when switching between tools (move, rotate, scale, etc.)")]
        [SerializeField] private ToolChangeUnityEvent onToolChanged = new ToolChangeUnityEvent();

        // Values change events
        [Header("Values Change Events")]
        [Tooltip("Triggered when the grid snap value changes")]
        [SerializeField] private GridSnapValueChangeUnityEvent onGridSnapValueChanged = new GridSnapValueChangeUnityEvent();
        
        [Tooltip("Triggered when the rotation snap value changes")]
        [SerializeField] private RotationSnapValueChangeUnityEvent onRotationSnapValueChanged = new RotationSnapValueChangeUnityEvent();
        
        // Group/Ungroup events
        [Header("Group/Ungroup Events")]
        [Tooltip("Triggered when objects are grouped together")]
        [SerializeField] private GroupUnityEvent onObjectsGrouped = new GroupUnityEvent();
        
        [Tooltip("Triggered when a group is ungrouped (broken apart)")]
        [SerializeField] private GroupUnityEvent onObjectsUngrouped = new GroupUnityEvent();
        
        #endregion
        
        #region Public Event Properties
        
        // Property accessors for the events so they can be subscribed to in code as well
        public ToggleSnappingUnityEvent OnToggleSnapping => onToggleSnapping;
        public ToggleLocalControlUnityEvent OnToggleLocalControl => onToggleLocalControl;
        public ToggleRotationSnappingUnityEvent OnToggleRotationSnapping => onToggleRotationSnapping;

        public SelectionUnityEvent OnSelectionUpdated => onSelectionUpdated;
        public ObjectSelectionUnityEvent OnObjectSelected => onObjectSelected;
        public ObjectSelectionUnityEvent OnObjectDeselected => onObjectDeselected;
        
        public MoveStartUnityEvent OnMoveStart => onMoveStart;
        public MovingUnityEvent OnMoving => onMoving;
        public MoveEndUnityEvent OnMoveEnd => onMoveEnd;
        
        public RotateStartUnityEvent OnRotateStart => onRotateStart;
        public RotatingUnityEvent OnRotating => onRotating;
        public RotateEndUnityEvent OnRotateEnd => onRotateEnd;
        
        public ScaleStartUnityEvent OnScaleStart => onScaleStart;
        public ScalingUnityEvent OnScaling => onScaling;
        public ScaleEndUnityEvent OnScaleEnd => onScaleEnd;
        
        public DeleteUnityEvent OnObjectDeleted => onObjectDeleted;
        public MultiDeleteUnityEvent OnMultipleObjectsDeleted => onMultipleObjectsDeleted;
        
        public DuplicateUnityEvent OnObjectsDuplicated => onObjectsDuplicated;
        
        public QuickRotateUnityEvent OnQuickRotated => onQuickRotated;
        
        public ToolChangeUnityEvent OnToolChanged => onToolChanged;
        
        public GridSnapValueChangeUnityEvent OnGridSnapValueChanged => onGridSnapValueChanged;
        public RotationSnapValueChangeUnityEvent OnRotationSnapValueChanged => onRotationSnapValueChanged;
        
        public GroupUnityEvent OnObjectsGrouped => onObjectsGrouped;
        public GroupUnityEvent OnObjectsUngrouped => onObjectsUngrouped;
        
        #endregion
        
        #region Event Triggers
        
        // Use these methods to trigger events from your Gizmo system
        
        // Toggle event triggers
        public void TriggerToggleSnapping(bool snappingEnabled)
        {
            onToggleSnapping.Invoke(snappingEnabled);
        }
        
        public void TriggerToggleLocalControl(bool localControlEnabled)
        {
            onToggleLocalControl.Invoke(localControlEnabled);
        }
        
        public void TriggerToggleRotationSnapping(bool rotationSnappingEnabled)
        {
            onToggleRotationSnapping.Invoke(rotationSnappingEnabled);
        }
        
        // Selection event triggers
        public void TriggerSelectionUpdated(GameObject[] selectedObjects)
        {
            onSelectionUpdated.Invoke(selectedObjects);
        }
        
        public void TriggerObjectSelected(GameObject obj)
        {
            onObjectSelected.Invoke(obj, true);
        }
        
        public void TriggerObjectDeselected(GameObject obj)
        {
            onObjectDeselected.Invoke(obj, false);
        }
        
        // Movement event triggers
        public void TriggerMoveStart(GameObject[] objects, Vector3[] startPositions)
        {
            onMoveStart.Invoke(objects, startPositions);
        }
        
        public void TriggerMoving(GameObject[] objects, Vector3[] startPositions, Vector3[] currentPositions)
        {
            onMoving.Invoke(objects, startPositions, currentPositions);
        }
        
        public void TriggerMoveEnd(GameObject[] objects, Vector3[] startPositions, Vector3[] endPositions)
        {
            onMoveEnd.Invoke(objects, startPositions, endPositions);
        }
        
        // Rotation event triggers
        public void TriggerRotateStart(GameObject[] objects, Quaternion[] startRotations)
        {
            onRotateStart.Invoke(objects, startRotations);
        }
        
        public void TriggerRotating(GameObject[] objects, Quaternion[] startRotations, Quaternion[] currentRotations)
        {
            onRotating.Invoke(objects, startRotations, currentRotations);
        }
        
        public void TriggerRotateEnd(GameObject[] objects, Quaternion[] startRotations, Quaternion[] endRotations)
        {
            onRotateEnd.Invoke(objects, startRotations, endRotations);
        }
        
        // Scale event triggers
        public void TriggerScaleStart(GameObject[] objects, Vector3[] startScales)
        {
            onScaleStart.Invoke(objects, startScales);
        }
        
        public void TriggerScaling(GameObject[] objects, Vector3[] startScales, Vector3[] currentScales)
        {
            onScaling.Invoke(objects, startScales, currentScales);
        }
        
        public void TriggerScaleEnd(GameObject[] objects, Vector3[] startScales, Vector3[] endScales)
        {
            onScaleEnd.Invoke(objects, startScales, endScales);
        }
        
        // Delete event triggers
        public void TriggerObjectDeleted(GameObject obj)
        {
            onObjectDeleted.Invoke(obj);
        }
        
        public void TriggerMultipleObjectsDeleted(GameObject[] objects)
        {
            onMultipleObjectsDeleted.Invoke(objects);
        }
        
        // Duplicate event trigger
        public void TriggerObjectsDuplicated(GameObject[] sourceObjects, GameObject[] duplicatedObjects)
        {
            onObjectsDuplicated.Invoke(sourceObjects, duplicatedObjects);
        }
        
        // Quick rotate event trigger
        public void TriggerQuickRotated(GameObject[] objects, Quaternion[] startRotations, Quaternion[] endRotations)
        {
            onQuickRotated.Invoke(objects, startRotations, endRotations);
        }
        
        // Tool change event trigger
        public void TriggerToolChanged(int previousTool, int currentTool)
        {
            onToolChanged.Invoke(previousTool, currentTool);
        }

        public void TriggerGridSnapValueChanged(float newValue)
        {
            onGridSnapValueChanged.Invoke(newValue);
        }
        
        public void TriggerRotationSnapValueChanged(float newValue)
        {
            onRotationSnapValueChanged.Invoke(newValue);
        }
        
        // Group/Ungroup event triggers
        public void TriggerObjectsGrouped(GameObject groupParent, GameObject[] groupedObjects)
        {
            if (onObjectsGrouped != null)
                onObjectsGrouped.Invoke(groupParent, groupedObjects);
        }
        
        public void TriggerObjectsUngrouped(GameObject formerGroup, GameObject[] ungroupedObjects)
        {
            if (onObjectsUngrouped != null)
                onObjectsUngrouped.Invoke(formerGroup, ungroupedObjects);
        }
        
        #endregion
        
        private void Awake()
        {
            // Singleton pattern
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        
        #region Usage Examples
        
        // Example of how to subscribe to events in code:
        /*
        private void OnEnable()
        {
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.OnObjectDeleted.AddListener(HandleObjectDeleted);
                GizmoEvents.Instance.OnMoveEnd.AddListener(HandleObjectMoved);
            }
        }
        
        private void OnDisable()
        {
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.OnObjectDeleted.RemoveListener(HandleObjectDeleted);
                GizmoEvents.Instance.OnMoveEnd.RemoveListener(HandleObjectMoved);
            }
        }
        
        private void HandleObjectDeleted(GameObject obj)
        {
            Debug.Log($"Object {obj.name} was deleted");
        }
        
        private void HandleObjectMoved(GameObject[] objects, Vector3[] startPositions, Vector3[] endPositions)
        {
            Debug.Log($"{objects.Length} objects were moved");
        }
        */
        
        #endregion

        /// <summary>
        /// Registers the RuntimeBlocksManager with the events system
        /// </summary>
        public void RegisterRuntimeBlocksManager()
        {
            if (RuntimeBlocksManager.Instance != null)
            {
                // If already registered, don't register again
                if (IsRuntimeBlocksManagerRegistered)
                {
                    Debug.Log("GizmoEvents: RuntimeBlocksManager already registered");
                    return;
                }
                
                Debug.Log("GizmoEvents: Registering RuntimeBlocksManager");
                
                // Register selection update event - only use this event instead of individual select/deselect
                onSelectionUpdated.AddListener((objectsArray) => {
                    // This will be handled by GizmoTools directly
                    // We're not registering for individual selection events anymore
                });
                
                // Register for object deletion and other events
                onObjectDeleted.AddListener(RuntimeBlocksManager.Instance.RemoveBlock);
                
                // Register for grouping events
                onObjectsGrouped.AddListener((group, children) => {
                    RuntimeBlocksManager.Instance.RefreshAllBlocks();
                });
                
                onObjectsUngrouped.AddListener((group, children) => {
                    RuntimeBlocksManager.Instance.RefreshAllBlocks();
                });
                
                // Register for duplicates
                onObjectsDuplicated.AddListener((sourceObjs, duplicateObjs) => {
                    if (RuntimeBlocksManager.Instance != null) {
                        RuntimeBlocksManager.Instance.RefreshAllBlocks();
                    }
                });
                
                // Set flag to indicate registration is complete
                IsRuntimeBlocksManagerRegistered = true;
            }
        }
    }
}
