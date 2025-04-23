using UnityEngine;
using UnityEngine.UI;

namespace RuntimeTerrainEditor
{
    public class SavePanelView : MonoBehaviour
    {
        public InputField   nameField;
        public Button       saveButton;
        public Button       refreshButton;

        public Transform    loadItemHolder;
        public GameObject   loadItemPrefab;

        public LoadItem CreateLoadItem()
        {
            return Instantiate(loadItemPrefab, loadItemHolder).GetComponent<LoadItem>();
        }
    }
}