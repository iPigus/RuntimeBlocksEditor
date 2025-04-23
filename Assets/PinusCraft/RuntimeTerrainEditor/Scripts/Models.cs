using System;
using System.Collections.Generic;

namespace RuntimeTerrainEditor
{
    [Serializable]
    public class TerrainFileData
    {
        public List<TerrainSaveData> terrains;

        public TerrainFileData()
        {
            terrains = new List<TerrainSaveData>();
        }
    }

    [Serializable]
    public class TerrainSaveData
    {
        public float posX;
        public float posZ;
        public int mapSize;
        public float [,,] splatMap;
        public float [,]  heightMap;
        public List<TreeInstanceData> treeInstanceData;
    }

    [Serializable]
    public class TreeInstanceData
    {
        public float posX;
        public float posY;
        public float posZ;
        public float widthScale;
        public float heightScale;
        public float rotation;
        public int prototypeIndex;
    }
}