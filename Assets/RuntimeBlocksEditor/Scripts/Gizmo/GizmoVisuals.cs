using UnityEngine;

namespace RuntimeBlocksEditor.Gizmo
{
    /// <summary>
    /// Manages visual elements of Gizmo tools.
    /// </summary>
    public class GizmoVisuals : MonoBehaviour
    {
        [Header("Move Elements")]
        public GameObject xArrow;
        public GameObject yArrow;
        public GameObject zArrow;
        public GameObject xPlane;
        public GameObject yPlane;
        public GameObject zPlane;
        
        [Header("Rotation Elements")]
        public GameObject xRot;
        public GameObject yRot;
        public GameObject zRot;
        public GameObject looker;
        
        [Header("Scale Elements")]
        // Uniform scaling handle
        public GameObject allScale;
        
        // Directional scale handles (positive and negative directions)
        [Header("Directional Scale Elements")]
        public GameObject xScalePos; // Positive X scale handle 
        public GameObject xScaleNeg; // Negative X scale handle
        public GameObject yScalePos; // Positive Y scale handle
        public GameObject yScaleNeg; // Negative Y scale handle
        public GameObject zScalePos; // Positive Z scale handle
        public GameObject zScaleNeg; // Negative Z scale handle
        
        [Header("Materials")]
        [SerializeField] private Material highlightMaterial;
        
        private Material originalMaterial;
        private GameObject highlightedObject;
        
        /// <summary>
        /// Sets the visibility of Gizmo elements based on the active tool.
        /// </summary>
        public void SetGizmoVisibility(bool showMove, bool showRotate, bool showScale)
        {
            // Move elements
            SetObjectActive(xArrow, showMove);
            SetObjectActive(yArrow, showMove);
            SetObjectActive(zArrow, showMove);
            SetObjectActive(xPlane, showMove);
            SetObjectActive(yPlane, showMove);
            SetObjectActive(zPlane, showMove);
            
            // Rotate elements
            SetObjectActive(xRot, showRotate);
            SetObjectActive(yRot, showRotate);
            SetObjectActive(zRot, showRotate);
            SetObjectActive(looker, showRotate);
            
            // Scale elements
            SetObjectActive(allScale, showScale);
            SetObjectActive(xScalePos, showScale);
            SetObjectActive(xScaleNeg, showScale);
            SetObjectActive(yScalePos, showScale);
            SetObjectActive(yScaleNeg, showScale);
            SetObjectActive(zScalePos, showScale);
            SetObjectActive(zScaleNeg, showScale);
        }
        
        private void SetObjectActive(GameObject obj, bool active)
        {
            if (obj != null)
                obj.SetActive(active);
        }
        
        /// <summary>
        /// Highlights a Gizmo element.
        /// </summary>
        public void HighlightGizmoElement(GameObject element)
        {
            if (element == null) return;
            
            
            highlightedObject = element;
            Renderer renderer = element.GetComponent<Renderer>();
            
            if (renderer != null)
            {
                originalMaterial = renderer.material;
                renderer.material = highlightMaterial;
            }
            else
            {
                Debug.LogWarning("No renderer found on element: " + element.name);
            }
        }
        
        /// <summary>
        /// Reverts the highlight of the currently highlighted element.
        /// </summary>
        public void RevertHighlight()
        {
            if (highlightedObject == null) return;
            
            Renderer renderer = highlightedObject.GetComponent<Renderer>();
            
            if (renderer != null && originalMaterial != null)
            {
                renderer.material = originalMaterial;
            }
            
            highlightedObject = null;
        }
        
        /// <summary>
        /// Checks if the given object is a move element.
        /// </summary>
        public bool IsMoveElement(GameObject obj)
        {
            bool result = obj == xArrow || obj == yArrow || obj == zArrow || 
                   obj == xPlane || obj == yPlane || obj == zPlane;
            
            Debug.Log("IsMoveElement check for " + obj.name + ": " + result);
            Debug.Log("xArrow: " + (xArrow != null ? xArrow.name : "null"));
            Debug.Log("yArrow: " + (yArrow != null ? yArrow.name : "null"));
            Debug.Log("zArrow: " + (zArrow != null ? zArrow.name : "null"));
            
            return result;
        }
        
        /// <summary>
        /// Checks if the given object is a rotate element.
        /// </summary>
        public bool IsRotateElement(GameObject obj)
        {
            bool result = obj == xRot || obj == yRot || obj == zRot;
            
            return result;
        }
        
        /// <summary>
        /// Checks if the given object is a scale element.
        /// </summary>
        public bool IsScaleElement(GameObject obj)
        {
            return obj == allScale ||
                   obj == xScalePos || obj == xScaleNeg || 
                   obj == yScalePos || obj == yScaleNeg || 
                   obj == zScalePos || obj == zScaleNeg;
        }
        
        /// <summary>
        /// Gets the axis associated with a Gizmo element.
        /// </summary>
        public Vector3 GetElementAxis(GameObject element)
        {
            if (element == xArrow || element == xPlane || element == xRot || element == xScalePos || element == xScaleNeg)
                return Vector3.right;
            else if (element == yArrow || element == yPlane || element == yRot || element == yScalePos || element == yScaleNeg)
                return Vector3.up;
            else if (element == zArrow || element == zPlane || element == zRot || element == zScalePos || element == zScaleNeg)
                return Vector3.forward;
            else
                return Vector3.zero;
        }
    }
}