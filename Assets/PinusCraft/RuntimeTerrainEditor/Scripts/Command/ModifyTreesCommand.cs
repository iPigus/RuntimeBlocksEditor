using System.Collections.Generic;
using UnityEngine;

namespace RuntimeTerrainEditor
{    
    public class ModifyTreesCommand : ICommand
    {
        private class TerrainTreesState
        {
            public TerrainData data;
            public List<TreeInstance> trees;
        }

        private List<TerrainTreesState> _startStates;
        private List<TerrainTreesState> _endStates;

        public ModifyTreesCommand()
        {
            _startStates = new List<TerrainTreesState>();
            _endStates = new List<TerrainTreesState>();
        }

        public void AddStartData(TerrainData terrainData)
        {
            _startStates.Add(new TerrainTreesState(){
                data = terrainData,
                trees = new List<TreeInstance>(terrainData.treeInstances)
            });
        }

        public void Complete(TerrainData[] terrainData)
        {
            foreach (var td in terrainData)
            {
                _endStates.Add(new TerrainTreesState(){
                    data = td,
                    trees = new List<TreeInstance>(td.treeInstances)
                });
            }

        }

        public void Execute()
        {
            foreach (var state in _endStates)
            {
                state.data.treeInstances = state.trees.ToArray();
            }
        }

        public void Undo()
        {
            foreach (var state in _startStates)
            {
                state.data.treeInstances = state.trees.ToArray();
            }
        }
    }
}