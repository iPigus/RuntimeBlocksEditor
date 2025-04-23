using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeTerrainEditor
{
    public class RuntimeTerrainEditor_Demo : MonoBehaviour
    {
        public RuntimeTerrain runtimeTerrain;
        public CameraController cameraController;
        public ViewController viewController;

        private InputController _inputController;

        private void Start()
        {
            Application.targetFrameRate = 60;

            runtimeTerrain.Init(Camera.main);
            cameraController.Init(Camera.main);
            viewController.Init(runtimeTerrain);
            
            _inputController = new InputController(runtimeTerrain);

            runtimeTerrain.OnTerrainCreated += OnTerrainCreated;
            
            //  get default size from settings
            int columnCount = runtimeTerrain.settings.startColumnCount;
            int rowCount    = runtimeTerrain.settings.startRowCount;
            int size        = (int)runtimeTerrain.settings.size;

            //  create the grid
            runtimeTerrain.SetTerrainSize(size);
            runtimeTerrain.CreateGrid(columnCount, rowCount);
        }

        private void OnTerrainCreated(Terrain terrain)
        {
            //  set terrain custom properties here
            //  name, material, etc.
            terrain.name = "Terrain_" + runtimeTerrain.Terrains.Count;
            if (terrain.gameObject.TryGetComponent<TerrainCollider>(out TerrainCollider c))
            {
                Debug.Log($"collider: " + c.enabled);
            }
        }

        private void FixedUpdate()
        {
            //  check if player is interacting with a ui object(input field etc.)
            if (EventSystem.current.currentSelectedGameObject != null)
            {
                return;
            }

            _inputController.ListenInputs();
        }

        private void LateUpdate()
        {
            cameraController.ListenInputs();
            viewController.ListenInputs(Time.deltaTime);
        }
    }
}