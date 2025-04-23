using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace RuntimeBlocksEditor.Gizmo
{
    public class BlockToEdit : MonoBehaviour
    {
        // List of all editable objects
        public static List<BlockToEdit> AllObjects = new List<BlockToEdit>();
        
    [Header("Materials")]
    [SerializeField] private Material originalMaterial; // Serialized field primarily for non-LOD or as fallback
    [SerializeField] private Material errorMaterial;
    [SerializeField] private bool grayColoring = true;
    [SerializeField] private Color baseColor = Color.white;
    [SerializeField] [Range(0, 1)] private float colorIntensity = 1.0f;
    
    [Header("Outline Settings")]
    [SerializeField] private Color selectionOutlineColor = new Color(0.2f, 0.4f, 1f, 1f); // Blue for selected objects
    [SerializeField] private Color hoverOutlineColor = new Color(0.5f, 0.8f, 1f, 0.8f); // Light blue for hover state
    [SerializeField] [Range(0.01f, 0.2f)] private float primaryOutlineWidth = 0.05f;
    [SerializeField] [Range(0.01f, 0.2f)] private float secondaryOutlineWidth = 0.025f;
    [SerializeField] [Range(0.0f, 180.0f)] private float outlineAngle = 89f;
    [SerializeField] private GameObject outlineObject; // Backup outline object for non-shader materials
    
    private MeshRenderer meshRenderer; // Main renderer (LOD0 or single)
    private Material currentMaterial; // Instance of the main renderer's material
    private LODGroup lodGroup;
    private List<MeshRenderer> lodRenderers = new List<MeshRenderer>();
    private List<Material> originalLodSharedMaterials = new List<Material>(); // Store original shared materials for LODs
    
    private MaterialPropertyBlock propertyBlock; // Reusable property block
    
    public bool selected = false;
    public bool hovered = false; // Track if the object is being hovered
    
    // Property IDs for shader parameters (cached for performance)
    private static readonly int FirstOutlineColorProperty = Shader.PropertyToID("_FirstOutlineColor");
    private static readonly int FirstOutlineWidthProperty = Shader.PropertyToID("_FirstOutlineWidth");
    private static readonly int SecondOutlineColorProperty = Shader.PropertyToID("_SecondOutlineColor");
    private static readonly int SecondOutlineWidthProperty = Shader.PropertyToID("_SecondOutlineWidth");
    private static readonly int AngleProperty = Shader.PropertyToID("_Angle");
    private static readonly int NoGrayScaleColoringProperty = Shader.PropertyToID("_NoGrayScaleColoring");
    private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorIntensityProperty = Shader.PropertyToID("_ColorIntensity");
    
    private void Awake()
    {
        // Initialize the property block
        propertyBlock = new MaterialPropertyBlock();

        // Check if this object has a LOD Group
        lodGroup = GetComponent<LODGroup>();
        
        if (lodGroup != null)
        {
            // Initialize LOD renderers and store original shared materials
            InitializeLODRenderers();
        }
        else
        {
            // Standard initialization for a single renderer
            meshRenderer = GetComponent<MeshRenderer>();
            if (meshRenderer != null)
            {
                // Store the original shared material if not already assigned via Inspector
                if (this.originalMaterial == null)
                {
                     this.originalMaterial = meshRenderer.sharedMaterial;
                }
                // Get the current material instance from the renderer
                currentMaterial = meshRenderer.material;
                
                // Apply grayscale setting to the current material instance
                ApplyGrayColoringToMaterialInstance(currentMaterial);
            }
        }
        
        AllObjects.Add(this);
    }
    
    private void OnDestroy()
    {
        AllObjects.Remove(this);
        
        // Restore original shared materials for LODs if they were changed
        // Note: Material instances created by accessing .material are auto-cleaned by Unity usually,
        // but restoring sharedMaterial ensures the scene/prefab state is clean.
        if (lodGroup != null)
        {
            for (int i = 0; i < lodRenderers.Count && i < originalLodSharedMaterials.Count; i++)
            {
                if (lodRenderers[i] != null && originalLodSharedMaterials[i] != null)
                {
                    // Check if the current material is different from the original shared one
                    if (lodRenderers[i].sharedMaterial != originalLodSharedMaterials[i])
                    {
                        lodRenderers[i].sharedMaterial = originalLodSharedMaterials[i];
                    }
                }
            }
        }
        else if (meshRenderer != null && this.originalMaterial != null)
        {
            // Restore for single renderer
            if (meshRenderer.sharedMaterial != this.originalMaterial)
            {
                meshRenderer.sharedMaterial = this.originalMaterial;
            }
        }

        // Clear lists
        lodRenderers.Clear();
        originalLodSharedMaterials.Clear();
    }
    
    /// <summary>
    /// Initialize renderers and store original shared materials for LOD Groups
    /// </summary>
    private void InitializeLODRenderers()
    {
        if (lodGroup == null) return;
        
        lodRenderers.Clear();
        originalLodSharedMaterials.Clear();
        
        LOD[] lods = lodGroup.GetLODs();
        
        for (int lodIndex = 0; lodIndex < lods.Length; lodIndex++)
        {
            foreach (Renderer renderer in lods[lodIndex].renderers)
            {
                if (renderer != null && renderer is MeshRenderer)
                {
                    MeshRenderer meshRend = (MeshRenderer)renderer;
                    lodRenderers.Add(meshRend);
                    
                    // Store original shared material
                    originalLodSharedMaterials.Add(meshRend.sharedMaterial);
                }
                else // Add null placeholder if renderer is invalid
                {
                    lodRenderers.Add(null);
                    originalLodSharedMaterials.Add(null);
                }
            }
        }
        
        // Initialize the main renderer and material reference with LOD0 if available
        if (lodRenderers.Count > 0 && lodRenderers[0] != null)
        {
            meshRenderer = lodRenderers[0];
            // Use LOD0's shared material as the reference originalMaterial if not set
            if (this.originalMaterial == null)
            {
                this.originalMaterial = originalLodSharedMaterials[0];
            }
            // Get the current material instance for the main renderer
            currentMaterial = meshRenderer.material; 
        }
        
        // Apply initial color and grayscale settings to all material instances
        ApplyColorSettingsToAllMaterials();
    }
    
    /// <summary>
    /// Changes the object's material to the error material.
    /// </summary>
    public void ChangeMaterialToRed()
    {
        if (lodGroup != null && lodRenderers.Count > 0)
        {
            // For LOD Group, change all renderers to error material
            if (errorMaterial != null)
            {
                foreach (MeshRenderer renderer in lodRenderers)
                {
                    if (renderer != null)
                    {
                        renderer.material = errorMaterial; // Assign directly, Unity handles instancing if needed
                    }
                }
                // Update the main currentMaterial reference if applicable
                if (meshRenderer != null) currentMaterial = meshRenderer.material;
            }
        }
        else if (meshRenderer != null && errorMaterial != null)
        {
            // For single renderer
            meshRenderer.material = errorMaterial;
            currentMaterial = meshRenderer.material;
        }
        
        // Disable outline after changing material
        DisableOutline();
    }
    
    /// <summary>
    /// Restores the object's original shared material(s).
    /// </summary>
    public void ChangeToOriginalMaterial()
    {
        if (lodGroup != null && lodRenderers.Count > 0)
        {
            // Handle material restoration for LOD Group
            for (int i = 0; i < lodRenderers.Count && i < originalLodSharedMaterials.Count; i++)
            {
                MeshRenderer renderer = lodRenderers[i];
                Material originalSharedMat = originalLodSharedMaterials[i];
                
                if (renderer != null && originalSharedMat != null)
                {
                    renderer.sharedMaterial = originalSharedMat; 
                }
            }
            // Update the main currentMaterial reference to the new instance on LOD0
            if (meshRenderer != null) currentMaterial = meshRenderer.material;
        }
        else if (meshRenderer != null && this.originalMaterial != null)
        {
            // Restore original shared material for single renderer
            meshRenderer.sharedMaterial = this.originalMaterial;
            // Update currentMaterial reference
            currentMaterial = meshRenderer.material;
        }
        
        // Re-apply settings like color and grayscale to the new material instances
        ApplyColorSettingsToAllMaterials();
    }
    
    // --- Outline and Hover Methods --- 

    public void EnableHoverOutline()
    {
        if (selected) return; 
        hovered = true;
        ApplyOutlineSettings(true, false); 
    }

    public void DisableHoverOutline()
    {
        if (selected) return;
        hovered = false;
        ApplyOutlineSettings(false, false); 
    }

    public void EnableOutline()
    {
        hovered = false; 
        selected = true;
        ApplyOutlineSettings(false, true); 
        NotifySelectionManagerAdd();
    }

    public void DisableOutline()
    {
        // Only deselect if actually selected
        bool wasSelected = selected;
        selected = false;
        hovered = false; // Ensure hover is also off
        ApplyOutlineSettings(false, false);
        if (wasSelected) NotifySelectionManagerRemove();
    }

    /// <summary>
    /// Central method to apply outline settings based on hover and selection state.
    /// </summary>
    private void ApplyOutlineSettings(bool isHovered, bool isSelected)
    {
        float targetFirstWidth = 0f;
        Color targetFirstColor = Color.clear;
        float targetSecondWidth = 0f;
        Color targetSecondColor = Color.clear;
        bool activateOutlineObject = false;

        if (isSelected)
        {
            targetFirstWidth = primaryOutlineWidth;
            targetFirstColor = selectionOutlineColor;
            targetSecondWidth = secondaryOutlineWidth;
            targetSecondColor = selectionOutlineColor;
            activateOutlineObject = true;
        }
        else if (isHovered) // Only apply hover if not selected
        {
            targetFirstWidth = primaryOutlineWidth * 0.7f;
            targetFirstColor = new Color(hoverOutlineColor.r, hoverOutlineColor.g, hoverOutlineColor.b, 0.8f);
            targetSecondWidth = secondaryOutlineWidth * 0.7f;
            targetSecondColor = hoverOutlineColor;
            activateOutlineObject = true;
        }

        // Apply using MaterialPropertyBlock
        if (lodGroup != null && lodRenderers.Count > 0)
        {
            foreach (MeshRenderer renderer in lodRenderers)
            {
                if (renderer != null)
                {
                     // It's generally safer to get the block fresh each time
                     // or ensure it's cleared if reusing the member variable.
                    renderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetFloat(FirstOutlineWidthProperty, targetFirstWidth);
                    propertyBlock.SetColor(FirstOutlineColorProperty, targetFirstColor);
                    propertyBlock.SetFloat(SecondOutlineWidthProperty, targetSecondWidth);
                    propertyBlock.SetColor(SecondOutlineColorProperty, targetSecondColor);
                    propertyBlock.SetFloat(AngleProperty, outlineAngle);
                    renderer.SetPropertyBlock(propertyBlock);
                }
            }
        }
        else if (meshRenderer != null)
        {
            meshRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetFloat(FirstOutlineWidthProperty, targetFirstWidth);
            propertyBlock.SetColor(FirstOutlineColorProperty, targetFirstColor);
            propertyBlock.SetFloat(SecondOutlineWidthProperty, targetSecondWidth);
            propertyBlock.SetColor(SecondOutlineColorProperty, targetSecondColor);
            propertyBlock.SetFloat(AngleProperty, outlineAngle);
            meshRenderer.SetPropertyBlock(propertyBlock);
        }

        // Handle the backup outline object (logic remains similar, checks if the *shader* based outline would work)
        if (outlineObject != null)
        {
            bool shaderOutlineApplicable = false;
            if (meshRenderer != null) // Check the main renderer (LOD0 or single)
            {
                 // Get the material currently assigned to check its shader
                Material matToCheck = meshRenderer.sharedMaterial; // Use sharedMaterial to avoid instancing just for check
                if (matToCheck != null && matToCheck.shader != null && matToCheck.shader.name.StartsWith("RuntimeBlocksEditor/"))
                {
                    shaderOutlineApplicable = true;
                }
            }
            outlineObject.SetActive(activateOutlineObject && !shaderOutlineApplicable);
        }
    }
    
    // --- Color Methods ---

    /// <summary>
    /// Sets the base color for this block.
    /// </summary>
    public void SetBaseColor(Color color)
    {
        baseColor = color;
        ApplyColorSettingsToAllMaterials();
    }

    /// <summary>
    /// Gets the current base color.
    /// </summary>
    public Color GetBaseColor()
    {
        return baseColor;
    }

    /// <summary>
    /// Sets the color intensity (0-1).
    /// </summary>
    public void SetColorIntensity(float intensity)
    {
        colorIntensity = Mathf.Clamp01(intensity);
        ApplyColorSettingsToAllMaterials();
    }

    /// <summary>
    /// Gets the current color intensity.
    /// </summary>
    public float GetColorIntensity()
    {
        return colorIntensity;
    }

    /// <summary>
    /// Applies current color settings to all materials.
    /// </summary>
    private void ApplyColorSettingsToAllMaterials()
    {
        if (lodGroup != null && lodRenderers.Count > 0)
        {
            // Apply color settings to all LOD material instances
            foreach (MeshRenderer renderer in lodRenderers)
            {
                if (renderer != null)
                {
                    ApplyColorSettingsToMaterialInstance(renderer.material);
                }
            }
        }
        else if (meshRenderer != null)
        {
            // Apply to single renderer's material instance
            ApplyColorSettingsToMaterialInstance(meshRenderer.material);
        }
    }

    /// <summary>
    /// Applies color settings to a specific material instance.
    /// </summary>
    private void ApplyColorSettingsToMaterialInstance(Material material)
    {
        if (material == null) return;
        
        // Apply base color
        if (material.HasProperty(BaseColorProperty))
        {
            material.SetColor(BaseColorProperty, baseColor);
        }
        
        // Apply color intensity
        if (material.HasProperty(ColorIntensityProperty))
        {
            material.SetFloat(ColorIntensityProperty, colorIntensity);
        }
        
        // Apply grayscale setting
        ApplyGrayColoringToMaterialInstance(material);
    }
    
    // --- Grayscale Methods --- 

    /// <summary>
    /// Applies the current grayscale coloring setting to all active material instances.
    /// </summary>
    private void ApplyGrayColoringToAllMaterials()
    {
        if (lodGroup != null && lodRenderers.Count > 0)
        {
            // Apply grayscale setting to all LOD material instances
            foreach (MeshRenderer renderer in lodRenderers)
            {
                if (renderer != null)
                {
                    ApplyGrayColoringToMaterialInstance(renderer.material); // Get current instance
                }
            }
        }
        else if (meshRenderer != null)
        {
            // Apply to single renderer's material instance
            ApplyGrayColoringToMaterialInstance(meshRenderer.material); // Get current instance
        }
    }
    
    /// <summary>
    /// Applies grayscale coloring to a specific material instance.
    /// </summary>
    private void ApplyGrayColoringToMaterialInstance(Material material)
    {
        if (material == null) return;
        
        float noGrayScaleValue = grayColoring ? 0f : 1f;
        
        if (material.HasProperty(NoGrayScaleColoringProperty))
        {
            material.SetFloat(NoGrayScaleColoringProperty, noGrayScaleValue);
        }
    }
    
    public void ToggleGrayColoring()
    {
        grayColoring = !grayColoring;
        ApplyGrayColoringToAllMaterials();
    }
    
    public void SetGrayColoring(bool useGrayColoring)
    {
        if (grayColoring != useGrayColoring)
        {
            grayColoring = useGrayColoring;
            ApplyGrayColoringToAllMaterials();
        }
    }
    
    public bool GetGrayColoring()
    {
        return grayColoring;
    }

    // --- Group Handling Methods --- 

    public bool SelectGroupParentIfPartOfGroup()
    {
        Transform parentTrans = transform.parent;
        if (parentTrans != null)
        {
            BlockToEdit parentBlock = parentTrans.GetComponent<BlockToEdit>();
            if (parentBlock != null)
            {
                Debug.Log(gameObject.name + " is part of group: " + parentTrans.name);
                parentBlock.EnableOutline(); // Select the parent

                // Select all siblings (including self implicitly via parent)
                foreach (Transform child in parentTrans)
                {
                    BlockToEdit childBlock = child.GetComponent<BlockToEdit>();
                    // Ensure child is also selected visually if it's not the parent
                    if (childBlock != null && childBlock != parentBlock)
                    {
                         childBlock.EnableOutline(); 
                    }
                }
                // Update the GizmoTools - Parent selection handles this
                return true; 
            }
        }
        return false; 
    }

    public bool ApplyHoverOutlineToGroup()
    {
        Transform parentTrans = transform.parent;
        if (parentTrans != null)
        {
            BlockToEdit parentBlock = parentTrans.GetComponent<BlockToEdit>();
            if (parentBlock != null)
            {
                // Apply hover to all members of the group if they are not selected
                foreach (Transform child in parentTrans)
                {
                    BlockToEdit childBlock = child.GetComponent<BlockToEdit>();
                    if (childBlock != null && !childBlock.selected)
                    {
                        childBlock.hovered = true; // Set state
                        childBlock.ApplyOutlineSettings(true, false); // Apply visuals
                    }
                }
                return true;
            }
        }
        return false;
    }

    public bool RemoveHoverOutlineFromGroup()
    {
        Transform parentTrans = transform.parent;
        if (parentTrans != null)
        {
            BlockToEdit parentBlock = parentTrans.GetComponent<BlockToEdit>();
            if (parentBlock != null)
            {
                 // Remove hover from all members of the group if they are not selected
                foreach (Transform child in parentTrans)
                {
                    BlockToEdit childBlock = child.GetComponent<BlockToEdit>();
                    if (childBlock != null && !childBlock.selected)
                    {
                         childBlock.hovered = false; // Set state
                         childBlock.ApplyOutlineSettings(false, false); // Apply visuals (which removes hover)
                    }
                }
                return true;
            }
        }
        return false;
    }
    
    // --- Utility & Helper Methods --- 

    private void NotifySelectionManagerAdd()
    {
         if (RuntimeBlocksEditor.Gizmo.RuntimeBlocksManager.Instance != null)
        {
            RuntimeBlocksEditor.Gizmo.RuntimeBlocksManager.Instance.AddSelectedObject(gameObject);
        }
    }

    private void NotifySelectionManagerRemove()
    {
         if (RuntimeBlocksEditor.Gizmo.RuntimeBlocksManager.Instance != null)
        {
            RuntimeBlocksEditor.Gizmo.RuntimeBlocksManager.Instance.RemoveSelectedObject(gameObject);
        }
    }

    public static void LogAllObjectsSelectionStatus()
    {
        Debug.Log($"--- BlockToEdit.AllObjects Status ({AllObjects.Count} objects) ---");
        int selectedCount = 0;
        int lodGroupCount = 0;
        
        foreach (BlockToEdit block in AllObjects)
        {
            if (block != null)
            {
                bool isSelected = block.selected;
                bool hasLodGroup = block.lodGroup != null;
                int rendererCount = block.GetTotalRendererCount();
                
                Debug.Log($"Object: {block.gameObject.name}, Selected: {isSelected}, Tag: {block.gameObject.tag}, HasLODGroup: {hasLodGroup}, Renderers: {rendererCount}");
                
                if (isSelected)
                    selectedCount++;
                
                if (hasLodGroup)
                    lodGroupCount++;
            }
            else
            {
                Debug.LogError("Null BlockToEdit found in AllObjects list!");
            }
        }
        
        Debug.Log($"Total selected objects: {selectedCount}");
        Debug.Log($"Total objects with LOD Groups: {lodGroupCount}");
        Debug.Log("-------------------------------------------");
    }

    public void GetAllLODRenderersRecursively(List<MeshRenderer> renderers)
    {
        if (lodGroup != null && lodRenderers.Count > 0)
        {
            foreach (MeshRenderer renderer in lodRenderers)
            {
                if (renderer != null && !renderers.Contains(renderer))
                {
                    renderers.Add(renderer);
                }
            }
        }
        else if (meshRenderer != null && !renderers.Contains(meshRenderer))
        {
            renderers.Add(meshRenderer);
        }
        
        foreach (Transform child in transform)
        {
            BlockToEdit childBlock = child.GetComponent<BlockToEdit>();
            if (childBlock != null)
            {
                childBlock.GetAllLODRenderersRecursively(renderers);
            }
        }
    }
    
    public void ApplyGrayColoringToHierarchy(bool useGrayColoring)
    {
        SetGrayColoring(useGrayColoring);
        
        foreach (Transform child in transform)
        {
            BlockToEdit childBlock = child.GetComponent<BlockToEdit>();
            if (childBlock != null)
            {
                // No need to call SetGrayColoring again, recursive call handles it
                childBlock.ApplyGrayColoringToHierarchy(useGrayColoring);
            }
        }
    }
    
    public int GetTotalRendererCount()
    {
        if (lodGroup != null)
        {
             // Return count of non-null renderers we found
            int count = 0;
            foreach(var r in lodRenderers) if (r != null) count++;
            return count;
        }
        else if (meshRenderer != null)
        {
            return 1;
        }
        return 0;
    }
}
}