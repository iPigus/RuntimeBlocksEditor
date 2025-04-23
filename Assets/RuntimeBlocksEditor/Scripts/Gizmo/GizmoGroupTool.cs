using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Handles the Group/Ungroup functionality for the Gizmo system
    /// </summary>
    public class GizmoGroupTool : MonoBehaviour
    {
        private GizmoTools gizmoTools;
        private GizmoController controller;
        private GizmoHistory history;

        private void Awake()
        {
            gizmoTools = GetComponent<GizmoTools>();
            controller = GetComponent<GizmoController>();
            history = GetComponent<GizmoHistory>();
        }

        /// <summary>
        /// Groups selected objects together under a single parent.
        /// </summary>
        public void GroupSelectedObjects()
        {
            if (gizmoTools.controlObjects == null)
            {
                Debug.LogError("Cannot group: controlObjects is null");
                return;
            }

            if (gizmoTools.controlObjects.Length < 2)
            {
                Debug.LogError("Cannot group: need at least 2 objects, but only have " + gizmoTools.controlObjects.Length);
                return;
            }

            for (int i = 0; i < gizmoTools.controlObjects.Length; i++)
            {
                if (gizmoTools.controlObjects[i] != null)
                {
                    if (gizmoTools.controlObjects[i].TryGetComponent(out BlockToEdit blockToEdit) && !blockToEdit.selected)
                    {
                        blockToEdit.selected = true;
                    }
                }
                else
                {
                    Debug.LogError("Object " + i + " is null!");
                }
            }

            if (history.historyEnabled) history.SaveState(gizmoTools.controlObjects, GizmoHistory.HistoryActionType.Group);

            GameObject groupParent = new("Group")
            {
                tag = controller.tagToFind
            };

            groupParent.AddComponent<BlockGroup>();

            Vector3 centerPos = Vector3.zero;
            foreach (var obj in gizmoTools.controlObjects)
            {
                if (obj) centerPos += obj.transform.position;
            }

            centerPos /= gizmoTools.controlObjects.Length;

            groupParent.transform.position = centerPos;

            BlockToEdit groupBlockToEdit = groupParent.AddComponent<BlockToEdit>();

            GameObject[] originalObjects = gizmoTools.controlObjects.ToArray();

            foreach (var obj in gizmoTools.controlObjects)
            {
                if (obj != null)
                {
                    if (obj.TryGetComponent(out BlockToEdit blockToEdit))
                    {
                        blockToEdit.selected = false;
                        blockToEdit.DisableOutline();
                    }

                    obj.transform.SetParent(groupParent.transform, true);
                }
            }

            groupBlockToEdit.selected = true;
            groupBlockToEdit.EnableOutline();

            GameObject[] newSelection = new GameObject[] { groupParent };
            gizmoTools.SetControlObjects(newSelection);

            if (GizmoEvents.Instance)
            {
                GizmoEvents.Instance.TriggerObjectsGrouped(groupParent, originalObjects);
            }

            gizmoTools.UpdateSelectedObjects();
        }

        /// <summary>
        /// Ungroups the selected group, breaking it apart into individual objects.
        /// </summary>
        public void UngroupSelectedObjects()
        {
            if (gizmoTools.controlObjects == null || gizmoTools.controlObjects.Length == 0) return;

            HashSet<GameObject> processedGroups = new();
            List<GameObject> allUngroupedObjects = new();

            foreach (var obj in gizmoTools.controlObjects)
            {
                if (obj == null) continue;

                GameObject groupObject = null;

                if (obj.GetComponent<BlockGroup>())
                {
                    groupObject = obj;
                }
                else
                {
                    Transform current = obj.transform.parent;
                    while (current)
                    {
                        if (current.GetComponent<BlockGroup>())
                        {
                            groupObject = current.gameObject;
                            break;
                        }
                        current = current.parent;
                    }
                }

                if (groupObject && !processedGroups.Contains(groupObject))
                {
                    processedGroups.Add(groupObject);

                    if (history.historyEnabled)
                        history.SaveState(new GameObject[] { groupObject }, GizmoHistory.HistoryActionType.Ungroup);

                    List<GameObject> childObjects = new();
                    foreach (Transform child in groupObject.transform)
                    {
                        if (child.TryGetComponent(out BlockToEdit childBlock))
                        {
                            childObjects.Add(child.gameObject);
                        }
                    }

                    if (childObjects.Count == 0)
                    {
                        continue;
                    }

                    foreach (GameObject child in childObjects)
                    {
                        Transform childTransform = child.transform;
                        childTransform.GetPositionAndRotation(out Vector3 worldPos, out Quaternion worldRot);
                        Vector3 worldScale = childTransform.lossyScale;

                        childTransform.SetParent(null);

                        childTransform.SetPositionAndRotation(worldPos, worldRot);
                        childTransform.localScale = worldScale;

                        if (child.TryGetComponent(out BlockToEdit blockToEdit))
                        {
                            blockToEdit.selected = false;
                            blockToEdit.DisableOutline();
                        }

                        allUngroupedObjects.Add(child);
                    }

                    if (GizmoEvents.Instance != null)
                    {
                        GizmoEvents.Instance.TriggerObjectsUngrouped(groupObject, childObjects.ToArray());
                    }

                    Destroy(groupObject);
                }
            }

            if (allUngroupedObjects.Count > 0)
            {
                StartCoroutine(DeselectAllNextFrame());
            }
        }

        private IEnumerator DeselectAllNextFrame()
        {
            yield return new WaitForEndOfFrame(); // Wait one frame to ensure all destroys have been processed

            gizmoTools.DeselectAllBlocks();

            gizmoTools.SetControlObjects(new GameObject[0]);

            gizmoTools.UpdateSelectedObjects();

            if (GizmoEvents.Instance != null)
            {
                GizmoEvents.Instance.TriggerSelectionUpdated(new GameObject[0]);
            }
        }
    }
}