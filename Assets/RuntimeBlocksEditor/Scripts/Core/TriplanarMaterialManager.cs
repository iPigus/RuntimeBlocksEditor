using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace RuntimeBlocksEditor.Core
{
    /// <summary>
    /// Manages triplanar mapping and material settings for blocks
    /// </summary>
    [ExecuteInEditMode]
    public class TriplanarMaterialManager : MonoBehaviour
    {
        [Header("Textures")]
        [SerializeField] private Texture2D m_BaseTexture;
        [SerializeField] private Color m_BaseColor = Color.white;
        [SerializeField] private Texture2D m_HeightMap;
        [SerializeField] private Texture2D m_NormalMap;
        [SerializeField] private Texture2D m_OcclusionMap;
        [SerializeField] private Texture2D m_EmissionMap;

        [Header("Material Settings")]
        [SerializeField] private Material m_Material;
        [Range(0, 1)]
        [SerializeField] private float m_Blend = 0.5f;
        [Range(0.1f, 1)]
        [SerializeField] private float m_HeightScale = 0f;
        [Range(0, 1)]
        [SerializeField] private float m_Smoothness = 0.5f;
        [Range(0, 1)]
        [SerializeField] private float m_Metallic = 0f;
        [Range(0, 1)]
        [SerializeField] private float m_AmbientOcclusion = 1f;
        [Range(0, 2)]
        [SerializeField] private float m_NormalStrength = 1f;
        [SerializeField] private Color m_EmissionColor = Color.black;
        [Range(0, 20)]
        [SerializeField] private float m_EmissionStrength = 1f;

        [Header("Tiling Settings")]
        [SerializeField] private Vector2 m_TilingXZ = Vector2.one;
        [SerializeField] private Vector2 m_TilingY = Vector2.one;

        #region Public Methods
        public Material GetMaterial() => m_Material;
        public void SetMaterial(Material material) => m_Material = material;

        public void SetBaseTexture(Texture2D texture) => m_BaseTexture = texture;
        public void SetBaseColor(Color color) => m_BaseColor = color;
        public void SetHeightMap(Texture2D texture) => m_HeightMap = texture;
        public void SetNormalMap(Texture2D texture) => m_NormalMap = texture;
        public void SetOcclusionMap(Texture2D texture) => m_OcclusionMap = texture;
        public void SetEmissionMap(Texture2D texture) => m_EmissionMap = texture;

        public void SetBlend(float value) => m_Blend = Mathf.Clamp01(value);
        public void SetHeightScale(float value) => m_HeightScale = Mathf.Clamp01(value);
        public void SetSmoothness(float value) => m_Smoothness = Mathf.Clamp01(value);
        public void SetMetallic(float value) => m_Metallic = Mathf.Clamp01(value);
        public void SetAmbientOcclusion(float value) => m_AmbientOcclusion = Mathf.Clamp01(value);
        public void SetNormalStrength(float value) => m_NormalStrength = Mathf.Clamp(value, 0f, 2f);
        public void SetEmissionStrength(float value) => m_EmissionStrength = Mathf.Clamp(value, 0f, 20f);
        public void SetEmissionColor(Color color) => m_EmissionColor = color;

        public void SetTilingXZ(Vector4 value) => m_TilingXZ = new Vector2(value.x, value.y);
        public void SetTilingY(Vector4 value) => m_TilingY = new Vector2(value.x, value.y);
        #endregion

        private void OnEnable()
        {
            UpdateMaterial();
        }

        private void OnValidate()
        {
            UpdateMaterial();
        }

        private void UpdateMaterial()
        {
            if (m_Material == null)
            {
                Debug.LogError("Block material is not assigned!");
                return;
            }

            // Aktualizuj macierz transformacji dla stabilnego mapowania
            m_Material.SetMatrix("_ObjectToWorldPrev", transform.localToWorldMatrix);

            // Enable required keywords
            m_Material.EnableKeyword("_NORMALMAP");
            m_Material.EnableKeyword("_OCCLUSIONMAP");
            m_Material.EnableKeyword("_EMISSION");
            m_Material.EnableKeyword("_PARALLAXMAP");

            // Set textures
            if (m_BaseTexture != null)
                m_Material.SetTexture("_BaseMap", m_BaseTexture);
            
            if (m_HeightMap != null)
                m_Material.SetTexture("_HeightMap", m_HeightMap);
            
            if (m_NormalMap != null)
                m_Material.SetTexture("_NormalMap", m_NormalMap);

            if (m_OcclusionMap != null)
                m_Material.SetTexture("_OcclusionMap", m_OcclusionMap);

            if (m_EmissionMap != null)
                m_Material.SetTexture("_EmissionMap", m_EmissionMap);

            // Set material properties
            m_Material.SetColor("_BaseColor", m_BaseColor);
            m_Material.SetFloat("_Blend", m_Blend);
            m_Material.SetFloat("_HeightScale", m_HeightScale);
            m_Material.SetFloat("_Smoothness", m_Smoothness);
            m_Material.SetFloat("_Metallic", m_Metallic);
            m_Material.SetFloat("_AmbientOcclusion", m_AmbientOcclusion);
            m_Material.SetFloat("_NormalStrength", m_NormalStrength);
            m_Material.SetFloat("_EmissionStrength", m_EmissionStrength);
            m_Material.SetColor("_EmissionColor", m_EmissionColor);

            // Set tiling
            m_Material.SetVector("_TilingXZ", m_TilingXZ);
            m_Material.SetVector("_TilingY", m_TilingY);

            #if UNITY_EDITOR
            // Apply changes in editor
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(m_Material);
            }
            #endif
        }

        private void Update()
        {
            // Aktualizuj pozycję obiektu w każdej klatce (tylko w trybie Play)
            if (Application.isPlaying && m_Material != null)
            {
                m_Material.SetMatrix("_ObjectToWorldPrev", transform.localToWorldMatrix);
            }
        }

        private void LateUpdate()
        {
            #if UNITY_EDITOR
            // Aktualizuj materiał w trybie edytora dla podglądu na żywo
            if (!Application.isPlaying)
            {
                UpdateMaterial();
            }
            #endif
        }
    }
} 