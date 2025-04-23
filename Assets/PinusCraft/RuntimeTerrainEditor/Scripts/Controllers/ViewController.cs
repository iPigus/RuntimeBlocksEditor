using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace RuntimeTerrainEditor
{
    public class ViewController : MonoBehaviour
    {
        public ToolPanelView toolView;
        public SavePanelView savePanelView;
        public Button toolPanelButton;
        public Button savePanelButton;
        public GameObject brushProjector;

        private RuntimeTerrain          _runtimeTerrain;

        private int[]                   _terrainSizes;
        private BrushMode[]             _modes;
        private SelectionItem[]         _brushSelections;
        private SelectionItem[]         _paintLayerSelections;
        private SelectionItem[]         _objectSelections;
        private LoadItem[]              _loadItems;
        private Projector               _projector;

        public void Init(RuntimeTerrain editor)
        {
            _runtimeTerrain = editor;
            _modes = Enum.GetValues(typeof(BrushMode)) as BrushMode[];
            _terrainSizes = Enum.GetValues(typeof(TerrainSize)) as int[];
            _loadItems = Array.Empty<LoadItem>();

            _projector = Instantiate(brushProjector).GetComponent<Projector>();
            _projector.orthographic=true;
            
            ClosePanels();

            SetupToolPanel();
            SetupSavePanel();
            ReloadMapFiles();

        }

        private void ClosePanels()
        {
            toolView.gameObject.SetActive(false);
            savePanelView.gameObject.SetActive(false);
        }

        private void SetupToolPanel()
        {
            //  brush
            _brushSelections = new SelectionItem[_runtimeTerrain.settings.brushTextures.Length];
            for (int i = 0; i < _brushSelections.Length; i++)
            {
                var item                = toolView.CreateBrushSelectionItem();
                item.index              = i;
                item.image.texture      = _runtimeTerrain.settings.brushTextures[i];
                item.selectButton.onClick.AddListener(()=>SetBrushSelected(item));

                _brushSelections[i] = item;
            }

            //  paint
            _paintLayerSelections = new SelectionItem[_runtimeTerrain.settings.paintLayers.Length];
            for (int i = 0; i < _paintLayerSelections.Length; i++)
            {
                var item                = toolView.CreatePaintLayerSelectionItem();
                item.index              = i;
                item.image.texture      = _runtimeTerrain.settings.paintLayers[i].diffuseTexture;
                item.selectButton.onClick.AddListener(()=>SetPaintLayerSelected(item));

                _paintLayerSelections[i] = item;
            }

            //  object
            _objectSelections = new SelectionItem[_runtimeTerrain.settings.objectPrefabs.Length];
            for (int i = 0; i < _objectSelections.Length; i++)
            {
                var item                = toolView.CreateObjectSelectionItem();
                item.index              = i;
                item.image.texture      = _runtimeTerrain.settings.GetThumbnailAtIndex(i);
                item.selectButton.onClick.AddListener(()=>SetObjectSelected(item));

                _objectSelections[i] = item;
            }

            //  mode
            toolView.modeDropdown.ClearOptions();
            for (int i = 0; i < _modes.Length; i++)
            {
                var od = new Dropdown.OptionData(_modes[i].ToString());
                toolView.modeDropdown.options.Add(od);
            }
            toolView.modeDropdown.onValueChanged.AddListener(OnModeSelected);
            toolView.modeDropdown.RefreshShownValue();

            //  terrain size
            toolView.terrainSizeDropdown.ClearOptions();
            for (int i = 0; i < _terrainSizes.Length; i++)
            {
                var od = new Dropdown.OptionData(_terrainSizes[i].ToString());
                toolView.terrainSizeDropdown.options.Add(od);
            }
            toolView.terrainSizeDropdown.RefreshShownValue();

            //  brush size
            toolView.sizeSlider.minValue        = _runtimeTerrain.settings.brushSizeMin;
            toolView.sizeSlider.maxValue        = _runtimeTerrain.settings.brushSizeMax;
            toolView.sizeSlider.value           = _runtimeTerrain.BrushSize;
            toolView.sizeSlider.onValueChanged.AddListener(OnBrushSizeChanged);
            _projector.orthographicSize         = _runtimeTerrain.BrushSize;

            //  strength
            toolView.strengthSlider.minValue    = 0;
            toolView.strengthSlider.maxValue    = Constants.MAX_BRUSH_STRENGTH;
            toolView.strengthSlider.value       = _runtimeTerrain.settings.brushStrengthDefault;
            toolView.strengthSlider.onValueChanged.AddListener(OnBrushStrengthChanged);

            //  flatten
            toolView.flattenSlider.minValue     = _runtimeTerrain.settings.flattenHeightMin;
            toolView.flattenSlider.maxValue     = _runtimeTerrain.settings.flattenHeightMax;
            toolView.flattenSlider.value        = _runtimeTerrain.FlattenHeight;
            toolView.flattenSlider.onValueChanged.AddListener(OnFlattenValueChanged);

            //  generate
            toolView.generateButton.onClick.AddListener(OnGenerate);
            
            //  column
            toolView.columnField.contentType = InputField.ContentType.Alphanumeric;
            toolView.columnField.onValueChanged.AddListener(OnColumValueChanged);
            
            //  row
            toolView.rowField.contentType = InputField.ContentType.Alphanumeric;
            toolView.rowField.onValueChanged.AddListener(OnRowValueChanged);

            //  undo redo
            toolView.undoButton.onClick.AddListener(OnUndo);
            toolView.redoButton.onClick.AddListener(OnRedo);

            //  set initial values to view
            SetBrushSelected(_brushSelections[_runtimeTerrain.BrushIndex]);
            SetPaintLayerSelected(_paintLayerSelections[_runtimeTerrain.PaintLayerIndex]);
            SetObjectSelected(_objectSelections[_runtimeTerrain.ObjectIndex]);
            OnModeSelected((int)_runtimeTerrain.BrushMode);

            toolPanelButton.onClick.AddListener(()=>{
                ClosePanels();
                toolView.gameObject.SetActive(true);
            });

            toolView.rowField.text =  _runtimeTerrain.settings.startRowCount.ToString();
            toolView.columnField.text =  _runtimeTerrain.settings.startColumnCount.ToString();
        }

        private void SetupSavePanel()
        {
            savePanelView.saveButton.onClick.AddListener(OnSave);

            savePanelButton.onClick.AddListener(()=>{
                ClosePanels();
                savePanelView.gameObject.SetActive(true);
            });

            savePanelView.refreshButton.onClick.AddListener(ReloadMapFiles);
        }

        private void OnBrushStrengthChanged(float value)
        {
            _runtimeTerrain.SetBrushStrength(value);
        }

        private void OnFlattenValueChanged(float value)
        {
            _runtimeTerrain.SetFlattenHeight(value);
        }

        private void OnBrushSizeChanged(float value)
        {
            float maxBrushSize = _runtimeTerrain.PatchSize;
        
            //  don't allow brush size more than a terrain patch
            if (value > maxBrushSize)
            {
                Debug.Log("Brush size must be lower than a terrain patch");

                toolView.sizeSlider.SetValueWithoutNotify(maxBrushSize);
                _runtimeTerrain.SetBrushSize((int)maxBrushSize);
            }
            else
            {
                _runtimeTerrain.SetBrushSize((int)value);
            }
            _projector.orthographicSize         = _runtimeTerrain.BrushSize;
        }

        private void OnModeSelected(int index)
        {
            _runtimeTerrain.SetMode(_modes[index]);

            toolView.flattenGroup.SetActive(false);
            toolView.paintGroup.SetActive(false);
            toolView.objectGroup.SetActive(false);
        
            switch (_runtimeTerrain.BrushMode)
            {
                case BrushMode.FLATTEN:
                {
                    toolView.flattenGroup.SetActive(true);
                }
                break;
                case BrushMode.PAINT_TEXTURE:
                {
                    toolView.paintGroup.SetActive(true);
                }
                break;
                case BrushMode.PAINT_OBJECT:
                {
                    toolView.objectGroup.SetActive(true);
                }
                break;
            }
        }

        private void OnGenerate()
        {
            int columnCount = int.Parse(toolView.columnField.text);
            int rowCount    = int.Parse(toolView.rowField.text);

            int terrainSize = _terrainSizes[toolView.terrainSizeDropdown.value];
            _runtimeTerrain.SetTerrainSize(terrainSize);
            _runtimeTerrain.CreateGrid(columnCount, rowCount);
        }

        private void OnColumValueChanged(string value)
        {
            if (IsSizeInputValid(value))
            {
                toolView.columnField.SetTextWithoutNotify(1.ToString());
                return;
            }

            int count = int.Parse(value);
            if (count < 1)
            {
                toolView.columnField.SetTextWithoutNotify(1.ToString());
            }
        }

        private void OnRowValueChanged(string value)
        {
            if (IsSizeInputValid(value))
            {
                toolView.rowField.SetTextWithoutNotify(1.ToString());
                return;
            }

            int count = int.Parse(value);
            if (count < 1)
            {
                toolView.rowField.SetTextWithoutNotify(1.ToString());
            }
        }

        private bool IsSizeInputValid(string value)
        {
            return (string.IsNullOrEmpty(value) || string.IsNullOrWhiteSpace(value));
        }

        private void OnRedo()
        {
            CommandHistory.Redo();
        }

        private void OnUndo()
        {
            CommandHistory.Undo();
        }

        private void SetBrushSelected(SelectionItem selection)
        {
            foreach (var item in _brushSelections)
            {
                item.ClearSelection();
            }

            _brushSelections[selection.index].Select();
            _runtimeTerrain.SetBrushIndex(selection.index);
        }

        private void SetPaintLayerSelected(SelectionItem selection)
        {
            foreach (var item in _paintLayerSelections)
            {
                item.ClearSelection();
            }

            _paintLayerSelections[selection.index].Select();
            _runtimeTerrain.SetPaintLayerIndex(selection.index);
        }

        private void SetObjectSelected(SelectionItem selection)
        {
            foreach (var item in _objectSelections)
            {
                item.ClearSelection();
            }

            _objectSelections[selection.index].Select();
            _runtimeTerrain.SetObjectIndex(selection.index);
        }

        private void OnSave()
        {
            var path = GetSavePath(savePanelView.nameField.text);
            var data = _runtimeTerrain.Export();
            var bytes = Serialization.SerializeCompressed<TerrainFileData>(data);
            
            FileUtility.SaveToPath(path, bytes);

            ReloadMapFiles();
        }

        private void OnLoad(FileInfo info)
        {
            var bytes = FileUtility.LoadFromPath(info.FullName);
            var map = Serialization.DeserializeCompressed<TerrainFileData>(bytes);
            
            _runtimeTerrain.Import(map);
        }

        private void OnDelete(FileInfo info)
        {
            FileUtility.DeleteFileAtPath(info.FullName);
            
            ReloadMapFiles();
        }
    
        private void ReloadMapFiles()
        {
            var path = Application.persistentDataPath;
            var fileInfos = FileUtility.ReadFilesFromDirectory(path, Constants.SAVE_FILE_SEARCH_PATTERN);
            fileInfos = fileInfos.OrderByDescending(f => f.LastWriteTime).ToArray();

            foreach (var item in _loadItems)
            {
                Destroy(item.gameObject);
            }

            _loadItems = new LoadItem[fileInfos.Length];

            for (int i = 0; i < fileInfos.Length; i++)
            {
                var info = fileInfos[i];

                var loadItem = savePanelView.CreateLoadItem();
                loadItem.nameText.text = FileUtility.GetFileName(info);
                loadItem.loadButton.onClick.AddListener(() => OnLoad(info));
                loadItem.removeButton.onClick.AddListener(() => OnDelete(info));

                _loadItems[i] = loadItem;
            }
        }
    
        private string GetSavePath(string fileName)
        {
            return Constants.SAVE_FILE_DIRECTORY + "/" + fileName + "." + Constants.SAVE_FILE_EXTENSION;
        }

        public void ListenInputs(float dt)
        {
            var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.transform.TryGetComponent<Terrain>(out _))
                {
                    var point = hit.point;
                    point.y+=20f;
                    _projector.transform.position = point;
                }
                
            }
        }
    }
}
