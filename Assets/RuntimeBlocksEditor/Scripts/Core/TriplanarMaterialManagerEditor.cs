using UnityEngine;
using UnityEditor;

namespace RuntimeBlocksEditor.Core
{
    [CustomEditor(typeof(TriplanarMaterialManager))]
    public class TriplanarMaterialManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            TriplanarMaterialManager manager = (TriplanarMaterialManager)target;
            
            // Rysuj domyślny inspektor
            DrawDefaultInspector();
            
            EditorGUILayout.Space(10);

            // Jeden przycisk do pobrania materiału i ustawień
            if (GUILayout.Button("Download Material Settings", GUILayout.Height(30)))
            {
                Material material = manager.GetMaterial();
                
                // Jeśli nie ma materiału, spróbuj go pobrać z MeshRenderer
                if (material == null)
                {
                    var renderer = manager.GetComponent<MeshRenderer>();
                    if (renderer != null && renderer.sharedMaterial != null)
                    {
                        material = renderer.sharedMaterial;
                        manager.SetMaterial(material);
                    }
                }

                if (material != null)
                {
                    Undo.RecordObject(manager, "Pobrano materiał i ustawienia");
                    
                    // Pobierz tekstury
                    manager.SetBaseTexture(material.GetTexture("_BaseMap") as Texture2D);
                    manager.SetBaseColor(material.GetColor("_BaseColor"));
                    manager.SetHeightMap(material.GetTexture("_HeightMap") as Texture2D);
                    manager.SetNormalMap(material.GetTexture("_NormalMap") as Texture2D);
                    manager.SetOcclusionMap(material.GetTexture("_OcclusionMap") as Texture2D);
                    manager.SetEmissionMap(material.GetTexture("_EmissionMap") as Texture2D);
                    
                    // Pobierz ustawienia materiału
                    manager.SetBlend(material.GetFloat("_Blend"));
                    manager.SetHeightScale(material.GetFloat("_HeightScale"));
                    manager.SetSmoothness(material.GetFloat("_Smoothness"));
                    manager.SetMetallic(material.GetFloat("_Metallic"));
                    manager.SetAmbientOcclusion(material.GetFloat("_AmbientOcclusion"));
                    manager.SetNormalStrength(material.GetFloat("_NormalStrength"));
                    manager.SetEmissionStrength(material.GetFloat("_EmissionStrength"));
                    manager.SetEmissionColor(material.GetColor("_EmissionColor"));
                    
                    // Pobierz ustawienia tilingu
                    manager.SetTilingXZ(material.GetVector("_TilingXZ"));
                    manager.SetTilingY(material.GetVector("_TilingY"));
                    
                    EditorUtility.SetDirty(manager);
                }
                else
                {
                    Debug.LogError("Nie znaleziono materiału na obiekcie!");
                }
            }
        }
    }
} 