using UnityEngine;

namespace RuntimeTerrainEditor
{    
    public interface ICommand
    {
        void AddStartData(TerrainData terrainData);
        void Complete(TerrainData[] terrainData);
        void Execute();
        void Undo();
    }
}