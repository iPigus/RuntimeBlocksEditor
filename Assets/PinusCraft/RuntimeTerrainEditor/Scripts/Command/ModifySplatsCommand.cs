using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTerrainEditor
{    
    public class ModifySplatsCommand : ICommand
    {
        private class TerrainSplatsState
        {
            public TerrainData data;
            public float[,,] splats;
        } 

        private List<TerrainSplatsState> _startStates;
        private List<TerrainSplatsState> _endStates;

        public ModifySplatsCommand()
        {
            _startStates = new List<TerrainSplatsState>();
            _endStates = new List<TerrainSplatsState>();
        }

        public void AddStartData(TerrainData terrainData)
        {
            _startStates.Add(new TerrainSplatsState(){
                data = terrainData,
                splats = terrainData.GetAlphamaps(0, 0, terrainData.alphamapResolution, terrainData.alphamapResolution)
            });
        }

        public void Complete(TerrainData[] terrainData)
        {
            foreach (var data in terrainData)
            {
                _endStates.Add(new TerrainSplatsState(){
                    data = data,
                    splats = data.GetAlphamaps(0, 0, data.alphamapResolution, data.alphamapResolution)
                });
            }
        }

        public void Execute()
        {
            foreach (var state in _endStates)
            {
                state.data.SetAlphamaps(0,0,state.splats);
            }
        }

        public void Undo()
        {
            foreach (var state in _startStates)
            {
                state.data.SetAlphamaps(0,0,state.splats);
            }
        }
    }
}