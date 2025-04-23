using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Centralized manager for runtime blocks that maintains consistent selection state
    /// and handles operations like grouping, selection, and deletion.
    /// </summary>
    public class RuntimeBlocksManager : MonoBehaviour
    {
        private static RuntimeBlocksManager _instance;
        public static RuntimeBlocksManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<RuntimeBlocksManager>();
                    if (_instance == null)
                    {
                        GameObject manager = new GameObject("RuntimeBlocksManager");
                        _instance = manager.AddComponent<RuntimeBlocksManager>();
                    }
                }
                return _instance;
            }
        }

        // List of all objects with BlockToEdit components
        private List<GameObject> allBlocks = new List<GameObject>();
        
        // List of currently selected objects
        private List<GameObject> selectedBlocks = new List<GameObject>();

        // NEW: Dictionary to track group membership - maps group ID to list of member objects
        [SerializeField]
        private SerializableGroups serializedGroups = new SerializableGroups();
        private Dictionary<int, Group> groups = new Dictionary<int, Group>();
        private int nextGroupID = 1; // Used to generate unique group IDs

        // NEW: Serializable wrapper for groups dictionary
        [System.Serializable]
        private class SerializableGroups
        {
            public List<SerializableGroup> groups = new List<SerializableGroup>();

            public void FromDictionary(Dictionary<int, Group> dict)
            {
                groups.Clear();
                foreach (var kvp in dict)
                {
                    groups.Add(new SerializableGroup
                    {
                        id = kvp.Key,
                        group = kvp.Value
                    });
                }
            }

            public Dictionary<int, Group> ToDictionary()
            {
                Dictionary<int, Group> dict = new Dictionary<int, Group>();
                foreach (var serializedGroup in groups)
                {
                    dict[serializedGroup.id] = serializedGroup.group;
                }
                return dict;
            }
        }

        [System.Serializable]
        private class SerializableGroup
        {
            public int id;
            public Group group;
        }

        // NEW: Group class to track group data
        [System.Serializable]
        public class Group
        {
            public int id;
            public string name;
            public Vector3 pivot; // Center of the group
            public List<GameObject> members = new List<GameObject>();
            public bool isVisible = true;

            // Group bounds
            public Bounds CalculateBounds()
            {
                if (members.Count == 0) return new Bounds(pivot, Vector3.zero);
                
                Bounds bounds = new Bounds(members[0].transform.position, Vector3.zero);
                foreach (var member in members)
                {
                    if (member != null)
                    {
                        Renderer renderer = member.GetComponent<Renderer>();
                        if (renderer != null)
                        {
                            bounds.Encapsulate(renderer.bounds);
                        }
                        else
                        {
                            bounds.Encapsulate(member.transform.position);
                        }
                    }
                }
                return bounds;
            }
        }

        private GizmoController controller;
        private GizmoTools tools;
        private GizmoHistory history;

        private void Awake()
        {
            _instance = this;
            controller = FindObjectOfType<GizmoController>();
            if (controller != null)
            {
                tools = controller.GetComponent<GizmoTools>();
                history = controller.GetComponent<GizmoHistory>();
            }

            // Initialize groups from serialized data
            groups = serializedGroups.ToDictionary();
        }

        private void OnValidate()
        {
            // Update serialized data when groups change
            serializedGroups.FromDictionary(groups);
        }

        private void Start()
        {
            // Only register for events if they haven't been registered through GizmoEvents.RegisterRuntimeBlocksManager
            if (GizmoEvents.Instance != null && !GizmoEvents.Instance.IsRuntimeBlocksManagerRegistered)
            {
                GizmoEvents.Instance.RegisterRuntimeBlocksManager();
            }

            // Register for the object duplication event from GizmoTools
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.OnObjectsDuplicated.AddListener(HandleObjectsDuplicated);
            }

            // Initial scan for blocks
            RefreshAllBlocks();
        }

        private void OnDestroy()
        {
            // Unregister from events to prevent memory leaks
            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.OnObjectsDuplicated.RemoveListener(HandleObjectsDuplicated);
            }
        }

        /// <summary>
        /// Handles objects that have been duplicated by GizmoTools
        /// </summary>
        private void HandleObjectsDuplicated(GameObject[] originalObjects, GameObject[] duplicatedObjects)
        {
            if (duplicatedObjects == null || duplicatedObjects.Length == 0) return;
            
            Debug.Log($"RuntimeBlocksManager: Handling {duplicatedObjects.Length} duplicated objects from GizmoTools");
            
            // Map original objects to their groups
            Dictionary<int, List<GameObject>> groupMap = new Dictionary<int, List<GameObject>>();
            foreach (var obj in originalObjects)
            {
                if (obj == null) continue;
                
                int groupID = GetGroupIDForObject(obj);
                if (groupID > 0)
                {
                    if (!groupMap.ContainsKey(groupID))
                    {
                        groupMap[groupID] = new List<GameObject>();
                    }
                    groupMap[groupID].Add(obj);
                }
            }
            
            // If any of the original objects were in groups, create a new group with all duplicates
            if (groupMap.Count > 0)
            {
                // Create a new group with all duplicated objects
                int newGroupID = nextGroupID++;
                Group newGroup = new Group
                {
                    id = newGroupID,
                    name = "Group_" + newGroupID,
                    members = new List<GameObject>(duplicatedObjects.Where(obj => obj != null))
                };
                
                // Calculate group pivot (center)
                if (newGroup.members.Count > 0)
                {
                    Vector3 totalPos = Vector3.zero;
                    foreach (var obj in newGroup.members)
                    {
                        totalPos += obj.transform.position;
                    }
                    newGroup.pivot = totalPos / newGroup.members.Count;
                }
                
                // Add to groups dictionary
                groups[newGroupID] = newGroup;
                
                // Update serialized data
                serializedGroups.FromDictionary(groups);
                
                Debug.Log($"RuntimeBlocksManager: Created new group {newGroupID} with {newGroup.members.Count} duplicated objects");
            }
            
            // Refresh the block list to include new objects
            RefreshAllBlocks();
        }

        /// <summary>
        /// Scans the scene for all objects with the correct tag and BlockToEdit component
        /// </summary>
        public void RefreshAllBlocks()
        {
            if (controller == null) return;

            allBlocks.Clear();
            GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag(controller.tagToFind);
            
            foreach (GameObject obj in taggedObjects)
            {
                if (obj.GetComponent<BlockToEdit>() != null)
                {
                    allBlocks.Add(obj);
                }
            }
            
            Debug.Log($"RuntimeBlocksManager: Found {allBlocks.Count} blocks in scene");
        }

        /// <summary>
        /// Adds a block to the selection
        /// </summary>
        /// <param name="obj">The object to add to selection</param>
        /// <param name="forceMultiSelect">Force using multi-select mode (used when shift-clicking)</param>
        public void AddSelectedObject(GameObject obj, bool forceMultiSelect = false)
        {
            if (obj == null) return;
            
            if (!selectedBlocks.Contains(obj))
            {
                selectedBlocks.Add(obj);
                Debug.Log($"RuntimeBlocksManager: Added {obj.name} to selection. Total: {selectedBlocks.Count}");
                
                // Update the BlockToEdit component's selected state
                BlockToEdit blockToEdit = obj.GetComponent<BlockToEdit>();
                if (blockToEdit != null)
                {
                    blockToEdit.selected = true;
                    blockToEdit.EnableOutline();
                }

                // Check if this object is part of a group, and select the entire group
                // Only do this if we're not in multi-selection mode
                int groupID = GetGroupIDForObject(obj);
                if (groupID > 0 && !forceMultiSelect && !IsMultiSelectActive())
                {
                    SelectEntireGroup(groupID, forceMultiSelect);
                }
            }
            
            // Sync with GizmoTools
            SyncSelectionToGizmoTools();
        }

        /// <summary>
        /// Checks if multi-selection mode is active (shift key held)
        /// </summary>
        private bool IsMultiSelectActive()
        {
            if (controller != null)
            {
                return Input.GetKey(controller.selectMultipleCode);
            }
            // Fallback to default shift keys
            return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        }

        /// <summary>
        /// Removes a block from the selection
        /// </summary>
        /// <param name="obj">The object to remove from selection</param>
        /// <param name="forceMultiSelect">Force using multi-select mode (used when shift-clicking)</param>
        public void RemoveSelectedObject(GameObject obj, bool forceMultiSelect = false)
        {
            if (obj == null) return;
            
            if (selectedBlocks.Contains(obj))
            {
                selectedBlocks.Remove(obj);
                Debug.Log($"RuntimeBlocksManager: Removed {obj.name} from selection. Total: {selectedBlocks.Count}");
                
                // Update the BlockToEdit component's selected state
                BlockToEdit blockToEdit = obj.GetComponent<BlockToEdit>();
                if (blockToEdit != null)
                {
                    blockToEdit.selected = false;
                    blockToEdit.DisableOutline();
                }

                // Only deselect the entire group if we're not in multi-select mode
                if (!forceMultiSelect && !IsMultiSelectActive())
                {
                    // Check if this object is part of a group, and deselect the entire group
                    int groupID = GetGroupIDForObject(obj);
                    if (groupID > 0)
                    {
                        DeselectEntireGroup(groupID, forceMultiSelect);
                    }
                }
            }
            
            // Sync with GizmoTools
            SyncSelectionToGizmoTools();
        }

        /// <summary>
        /// Deselects all objects in a group
        /// </summary>
        /// <param name="groupID">The ID of the group to deselect</param>
        /// <param name="forceMultiSelect">If true, respects multi-select mode</param>
        private void DeselectEntireGroup(int groupID, bool forceMultiSelect = false)
        {
            if (!groups.ContainsKey(groupID)) return;
            
            Group group = groups[groupID];
            bool needsSync = false;
            
            // Use the passed forceMultiSelect parameter or check current key state
            bool isMultiSelecting = forceMultiSelect || IsMultiSelectActive();
            
            foreach (var member in group.members)
            {
                // Skip deselection of objects in multi-select mode
                if (member != null && selectedBlocks.Contains(member) && !isMultiSelecting)
                {
                    selectedBlocks.Remove(member);
                    
                    // Update the BlockToEdit component's selected state
                    BlockToEdit blockToEdit = member.GetComponent<BlockToEdit>();
                    if (blockToEdit != null)
                    {
                        blockToEdit.selected = false;
                        blockToEdit.DisableOutline();
                    }
                    
                    needsSync = true;
                }
            }
            
            if (needsSync)
            {
                Debug.Log($"RuntimeBlocksManager: Deselected entire group {groupID} with {group.members.Count} members");
                SyncSelectionToGizmoTools();
            }
        }

        /// <summary>
        /// Gets the group ID for an object, or 0 if it's not in a group
        /// </summary>
        public int GetGroupIDForObject(GameObject obj)
        {
            if (obj == null) return 0;
            
            foreach (var group in groups)
            {
                if (group.Value.members.Contains(obj))
                {
                    return group.Key;
                }
            }
            
            return 0; // Not in a group
        }

        /// <summary>
        /// Selects all objects in a group
        /// </summary>
        /// <param name="groupID">The ID of the group to select</param>
        /// <param name="forceMultiSelect">If true, won't auto-select other objects in the group</param>
        private void SelectEntireGroup(int groupID, bool forceMultiSelect = false)
        {
            if (!groups.ContainsKey(groupID)) return;
            
            Group group = groups[groupID];
            bool needsSync = false;
            
            // Use the passed forceMultiSelect parameter or check current key state
            bool isMultiSelecting = forceMultiSelect || IsMultiSelectActive();
            
            foreach (var member in group.members)
            {
                // Only add members if they're not already selected and we're not shift-selecting
                if (member != null && !selectedBlocks.Contains(member) && !isMultiSelecting)
                {
                    selectedBlocks.Add(member);
                    
                    // Update the BlockToEdit component's selected state
                    BlockToEdit blockToEdit = member.GetComponent<BlockToEdit>();
                    if (blockToEdit != null)
                    {
                        blockToEdit.selected = true;
                        blockToEdit.EnableOutline();
                    }
                    
                    needsSync = true;
                }
            }
            
            if (needsSync)
            {
                Debug.Log($"RuntimeBlocksManager: Selected entire group {groupID} with {group.members.Count} members");
                SyncSelectionToGizmoTools();
            }
        }

        /// <summary>
        /// Clears selection without updating BlockToEdit components (useful for mass operations)
        /// </summary>
        public void ClearSelection()
        {
            foreach (GameObject obj in selectedBlocks.ToArray())
            {
                BlockToEdit blockToEdit = obj.GetComponent<BlockToEdit>();
                if (blockToEdit != null)
                {
                    blockToEdit.selected = false;
                    blockToEdit.DisableOutline();
                }
            }
            
            selectedBlocks.Clear();
            Debug.Log("RuntimeBlocksManager: Cleared selection");
            
            // Sync with GizmoTools
            SyncSelectionToGizmoTools();
        }

        /// <summary>
        /// Sets the selected objects directly
        /// </summary>
        public void SetSelectedObjects(List<GameObject> objects)
        {
            if (objects == null) return;
            
            // Clear current selection first
            ClearSelection();
            
            // Add new objects
            foreach (GameObject obj in objects)
            {
                if (obj != null)
                {
                    selectedBlocks.Add(obj);
                    
                    // Update the BlockToEdit component's selected state
                    BlockToEdit blockToEdit = obj.GetComponent<BlockToEdit>();
                    if (blockToEdit != null)
                    {
                        blockToEdit.selected = true;
                        blockToEdit.EnableOutline();
                    }
                    
                    // Only auto-select the entire group if we're not in multi-select mode
                    if (!IsMultiSelectActive())
                    {
                        // NEW: Check if this object is part of a group, and select the entire group
                        int groupID = GetGroupIDForObject(obj);
                        if (groupID > 0)
                        {
                            SelectEntireGroup(groupID);
                        }
                    }
                }
            }
            
            Debug.Log($"RuntimeBlocksManager: Set selection to {selectedBlocks.Count} objects");
            
            // Sync with GizmoTools
            SyncSelectionToGizmoTools();
        }

        /// <summary>
        /// Completely removes a block from tracking
        /// </summary>
        public void RemoveBlock(GameObject obj)
        {
            if (obj == null) return;
            
            if (selectedBlocks.Contains(obj))
            {
                selectedBlocks.Remove(obj);
            }
            
            if (allBlocks.Contains(obj))
            {
                allBlocks.Remove(obj);
            }
            
            // NEW: Remove from any groups
            foreach (var group in groups.Values.ToList())
            {
                if (group.members.Contains(obj))
                {
                    group.members.Remove(obj);
                    
                    // If group is now empty, remove it
                    if (group.members.Count == 0)
                    {
                        groups.Remove(group.id);
                    }
                }
            }
            
            // Update serialized data
            serializedGroups.FromDictionary(groups);
            
            // Sync with GizmoTools
            SyncSelectionToGizmoTools();
        }

        /// <summary>
        /// Gets all currently selected objects
        /// </summary>
        public GameObject[] GetSelectedObjects()
        {
            return selectedBlocks.ToArray();
        }

        /// <summary>
        /// Gets all blocks in the scene
        /// </summary>
        public GameObject[] GetAllBlocks()
        {
            return allBlocks.ToArray();
        }

        /// <summary>
        /// Synchronizes the selection state with GizmoTools
        /// </summary>
        private void SyncSelectionToGizmoTools()
        {
            if (tools != null)
            {
                // Access controlObjects field using reflection
                System.Reflection.FieldInfo controlObjectsField = typeof(GizmoTools).GetField(
                    "controlObjects", 
                    System.Reflection.BindingFlags.Instance | 
                    System.Reflection.BindingFlags.NonPublic | 
                    System.Reflection.BindingFlags.Public);
                
                if (controlObjectsField != null)
                {
                    // Set controlObjects directly
                    controlObjectsField.SetValue(tools, selectedBlocks.ToArray());
                    Debug.Log($"RuntimeBlocksManager: Synced {selectedBlocks.Count} objects to GizmoTools");
                }
            }
        }

        /// <summary>
        /// Groups the currently selected objects
        /// </summary>
        public void GroupSelectedObjects()
        {
            if (selectedBlocks.Count < 2)
            {
                Debug.LogWarning("Cannot group: need at least 2 selected objects");
                return;
            }
            
            Debug.Log($"RuntimeBlocksManager: Grouping {selectedBlocks.Count} objects");
            
            // Save state for history
            if (history != null && history.historyEnabled)
            {
                history.SaveState(selectedBlocks.ToArray(), GizmoHistory.HistoryActionType.Group);
            }

            // Find all groups that have any of the selected objects
            HashSet<int> existingGroups = new HashSet<int>();
            List<GameObject> ungroupedObjects = new List<GameObject>();
            
            foreach (var obj in selectedBlocks)
            {
                int groupID = GetGroupIDForObject(obj);
                if (groupID > 0)
                {
                    existingGroups.Add(groupID);
                }
                else
                {
                    ungroupedObjects.Add(obj);
                }
            }

            // If we have existing groups, merge them and add ungrouped objects
            if (existingGroups.Count > 0)
            {
                // Get all objects from existing groups
                List<GameObject> allGroupMembers = new List<GameObject>();
                foreach (int groupID in existingGroups)
                {
                    if (groups.ContainsKey(groupID))
                    {
                        allGroupMembers.AddRange(groups[groupID].members);
                    }
                }

                // Add ungrouped objects
                allGroupMembers.AddRange(ungroupedObjects);

                // Remove old groups
                foreach (int groupID in existingGroups)
                {
                    if (groups.ContainsKey(groupID))
                    {
                        groups.Remove(groupID);
                    }
                }

                // Create a new group with all objects
                int newGroupID = nextGroupID++;
                Group newGroup = new Group
                {
                    id = newGroupID,
                    name = "Group_" + newGroupID,
                    members = allGroupMembers
                };

                // Calculate group pivot (center)
                if (allGroupMembers.Count > 0)
                {
                    Vector3 totalPos = Vector3.zero;
                    foreach (var obj in allGroupMembers)
                    {
                        totalPos += obj.transform.position;
                    }
                    newGroup.pivot = totalPos / allGroupMembers.Count;
                }

                // Add to groups dictionary
                groups[newGroupID] = newGroup;
                
                Debug.Log($"RuntimeBlocksManager: Merged {existingGroups.Count} groups and added {ungroupedObjects.Count} objects. New group {newGroupID} has {newGroup.members.Count} members");
            }
            else
            {
                // Create a new group with just the selected objects
                int newGroupID = nextGroupID++;
                Group newGroup = new Group
                {
                    id = newGroupID,
                    name = "Group_" + newGroupID,
                    members = new List<GameObject>(selectedBlocks)
                };

                // Calculate group pivot (center)
                if (selectedBlocks.Count > 0)
                {
                    Vector3 totalPos = Vector3.zero;
                    foreach (var obj in selectedBlocks)
                    {
                        totalPos += obj.transform.position;
                    }
                    newGroup.pivot = totalPos / selectedBlocks.Count;
                }

                // Add to groups dictionary
                groups[newGroupID] = newGroup;
                
                Debug.Log($"RuntimeBlocksManager: Created new group {newGroupID} with {newGroup.members.Count} members");
            }

            // Update serialized data
            serializedGroups.FromDictionary(groups);
        }

        /// <summary>
        /// Ungroups the selected objects
        /// </summary>
        public void UngroupSelectedObjects()
        {
            // Check if selectedBlocks is empty
            if (selectedBlocks.Count == 0)
            {
                Debug.LogWarning("Cannot ungroup: no objects selected");
                return;
            }
            
            Debug.Log($"RuntimeBlocksManager: Ungrouping {selectedBlocks.Count} objects");
            
            // Find all groups that have any of the selected objects
            HashSet<int> groupsToUngroup = new HashSet<int>();
            foreach (var obj in selectedBlocks)
            {
                int groupID = GetGroupIDForObject(obj);
                if (groupID > 0)
                {
                    groupsToUngroup.Add(groupID);
                }
            }
            
            if (groupsToUngroup.Count == 0)
            {
                Debug.LogWarning("No groups found among selected objects");
                return;
            }
            
            // Save state for history before ungrouping
            if (history != null && history.historyEnabled)
            {
                // For each group, save its state
                foreach (int groupID in groupsToUngroup)
                {
                    if (groups.ContainsKey(groupID))
                    {
                        history.SaveState(groups[groupID].members.ToArray(), GizmoHistory.HistoryActionType.Ungroup);
                    }
                }
            }
            
            // Remove the groups
            foreach (int groupID in groupsToUngroup)
            {
                if (groups.ContainsKey(groupID))
                {
                    Group group = groups[groupID];
                    Debug.Log($"RuntimeBlocksManager: Removing group {groupID} with {group.members.Count} members");
                    
                    // Remove the group
                    groups.Remove(groupID);
                }
            }
            
            // Update serialized data
            serializedGroups.FromDictionary(groups);
            
            // Re-sync selection to GizmoTools
            SyncSelectionToGizmoTools();
        }
        
        /// <summary>
        /// Gets all members of a group
        /// </summary>
        public GameObject[] GetGroupMembers(int groupID)
        {
            if (groups.ContainsKey(groupID))
            {
                return groups[groupID].members.ToArray();
            }
            
            return new GameObject[0];
        }
        
        /// <summary>
        /// Gets group information
        /// </summary>
        public Group GetGroup(int groupID)
        {
            if (groups.ContainsKey(groupID))
            {
                return groups[groupID];
            }
            
            return null;
        }
        
        /// <summary>
        /// Gets all groups
        /// </summary>
        public Dictionary<int, Group> GetAllGroups()
        {
            return groups;
        }

        /// <summary>
        /// Creates a new group from history data (for undo/redo)
        /// </summary>
        public int CreateGroupFromHistory(GameObject[] members)
        {
            if (members == null || members.Length < 2)
            {
                Debug.LogWarning("Cannot create group from history: need at least 2 valid objects");
                return 0;
            }
            
            // Create a new group
            int newGroupID = nextGroupID++;
            Group newGroup = new Group
            {
                id = newGroupID,
                name = "Group_" + newGroupID,
                members = new List<GameObject>(members)
            };
            
            // Calculate group pivot (center)
            if (members.Length > 0)
            {
                Vector3 totalPos = Vector3.zero;
                foreach (var obj in members)
                {
                    if (obj != null)
                    {
                        totalPos += obj.transform.position;
                    }
                }
                newGroup.pivot = totalPos / members.Length;
            }
            
            // Add to groups dictionary
            groups[newGroupID] = newGroup;
            
            // Update serialized data
            serializedGroups.FromDictionary(groups);
            
            Debug.Log($"RuntimeBlocksManager: Created group {newGroupID} from history with {newGroup.members.Count} members");
            
            return newGroupID;
        }
    }
} 
