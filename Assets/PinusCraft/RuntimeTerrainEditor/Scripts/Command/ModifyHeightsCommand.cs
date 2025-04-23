using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTerrainEditor
{   

    public class ModifyHeightsCommand : ICommand
    {
        private class TerrainHeightsState
        {
            public TerrainData data;
            public float[,] heights;
        } 

        private List<TerrainHeightsState> _startStates;
        private List<TerrainHeightsState> _endStates;

        public ModifyHeightsCommand()
        {
            _startStates = new List<TerrainHeightsState>();
            _endStates = new List<TerrainHeightsState>();
        }

        public void AddStartData(TerrainData data)
        {
            _startStates.Add(new TerrainHeightsState(){
                data = data,
                heights = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution)
            });
        }

        public void Complete(TerrainData[] terrainData)
        {
            foreach (var data in terrainData)
            {
                _endStates.Add(new TerrainHeightsState(){
                    data = data,
                    heights = data.GetHeights(0, 0, data.heightmapResolution, data.heightmapResolution)
                });
                
            }
        }

        public void Execute()
        {
            foreach (var state in _endStates)
            {
                state.data.SetHeights(0,0,state.heights);
            }
        }

        public void Undo()
        {
            foreach (var state in _startStates)
            {
                state.data.SetHeights(0,0,state.heights);
            }
        }
    }
}