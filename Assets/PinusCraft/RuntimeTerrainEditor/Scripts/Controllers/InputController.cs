using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace RuntimeTerrainEditor
{    
    public class InputController
    {
        private RuntimeTerrain _runtimeTerrain;
        private ICommand _modifyCommand;

        public InputController(RuntimeTerrain runtimeTerrain)
        {
            _runtimeTerrain = runtimeTerrain;
            _modifyCommand = null;

            _runtimeTerrain.OnTerrainModificationStart += OnTerrainModified;
        }

        private void OnTerrainModified(TerrainData data)
        {
            _modifyCommand.AddStartData(data);
        }

        public void ListenInputs()
        {
            //  do not start to modify if its over a ui object
            if (EventSystem.current.IsPointerOverGameObject() == false)
            {
                if (Input.GetMouseButton(0))
                {
                    if (_modifyCommand==null)
                    {
                        TerrainModifyStarted();
                    }
                    else
                    {
                        _runtimeTerrain.UseBrushAtPointerPosition();
                    }
                }
            }

            if (Input.GetMouseButtonUp(0))
            {
                TerrainModifyEnded();
            }

            //  Listen for undo shortcut
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Z))
            {
                CommandHistory.Undo();
            }

            //  Listen for redo shortcut
            if (Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(KeyCode.Y))
            {
                CommandHistory.Redo();
            }
        }

        private void TerrainModifyEnded()
        {
            if (_modifyCommand != null)
            {
                _modifyCommand.Complete(_runtimeTerrain.GetModifiedTerrains());
                CommandHistory.Register(_modifyCommand);
            }

            _modifyCommand = null;
            _runtimeTerrain.ClearModifiedData();
        }

        private void TerrainModifyStarted()
        {
            switch (_runtimeTerrain.BrushMode)
            {
                case BrushMode.RAISE:
                case BrushMode.LOWER:
                case BrushMode.SMOOTH:
                case BrushMode.FLATTEN:
                {
                    _modifyCommand = new ModifyHeightsCommand();
                }
                break;
                case BrushMode.PAINT_TEXTURE:
                {
                    _modifyCommand = new ModifySplatsCommand();
                }
                break;
                case BrushMode.PAINT_OBJECT:
                case BrushMode.REMOVE_OBJECT:
                {
                    _modifyCommand = new ModifyTreesCommand();
                }
                break;
            }
        }

    }
}