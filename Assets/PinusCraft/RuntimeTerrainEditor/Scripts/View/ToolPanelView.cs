using System;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeTerrainEditor
{
    public class ToolPanelView : MonoBehaviour
    {
        public Transform brushSelectionParent;
        public Transform paintLayerSelectionParent;
        public Transform objectSelectionParent;
        
        public GameObject selectionItemPrefab;
        
        public Dropdown modeDropdown;
        public Slider sizeSlider;
        public Slider strengthSlider;
        public Slider flattenSlider;
        
        public InputField columnField;
        public InputField rowField;
        public Dropdown terrainSizeDropdown;
        public Button generateButton;

        public Button undoButton;
        public Button redoButton;


        public GameObject paintGroup;
        public GameObject objectGroup;
        public GameObject flattenGroup;

        public SelectionItem CreateBrushSelectionItem()
        {
            return Instantiate(selectionItemPrefab, brushSelectionParent).GetComponent<SelectionItem>();
        }

        public SelectionItem CreatePaintLayerSelectionItem()
        {
            return Instantiate(selectionItemPrefab, paintLayerSelectionParent).GetComponent<SelectionItem>();
        }

        public SelectionItem CreateObjectSelectionItem()
        {
            return Instantiate(selectionItemPrefab, objectSelectionParent).GetComponent<SelectionItem>();
        }
    }
}