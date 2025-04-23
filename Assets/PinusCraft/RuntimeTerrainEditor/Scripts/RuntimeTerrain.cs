using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RuntimeTerrainEditor
{
    public class RuntimeTerrain : MonoBehaviour
    {
        public List<Terrain>        Terrains                { get; private set; }

        public BrushMode            BrushMode               { get { return _brushMode; } }
        public int                  BrushSize               { get { return _brushSize; } }
        public int                  BrushIndex              { get { return _brushIndex; } }
        public int                  PaintLayerIndex         { get { return _paintLayerIndex; } }
        public int                  ObjectIndex             { get { return _objectIndex; } }
        public float                FlattenHeight           { get { return _flattenHeight; } }
        public int                  PatchSize               { get { return _terrainSize; } }

        public GlobalSettings       settings;

        private Terrain             _targetTerrain;
        private float               _terrainInitialHeight;
        private int                 _terrainSize;
        
        //  brush
        private Texture2D[]         _brushTextures;         //  This will allow you to switch brushes
        private int                 _brushSize;             
        private int                 _brushIndex;            
        private float               _brushStrength;         
        private BrushMode           _brushMode;
        private static float[,]     _brush;                 //  this stores the brush textures pixel data
        private static Rect         _brushRect;

        //  flatten
        private float               _flattenHeight;         //  the height to which the flatten mode will go
        
        //  paint
        private int                 _paintLayerIndex;       

        //  object                 
        private TreePrototype[]     _objectPrototypes;      //  object prefabs will be registered as TreePrototypes in target terrain
        private int                 _objectIndex;      
        private float               _objectDensity;         
        private float               _objectHeightMin;
        private float               _objectHeightMax;
        private float               _objectWidthMin;
        private float               _objectWidthMax;

        //  raycasting
        private static Camera       _cam;
        private static Ray          _ray;
        private static RaycastHit   _hit;

        private HashSet<TerrainData> _modifiedTerrains;

        public event Action<TerrainData> OnTerrainModificationStart;
        public event Action<Terrain>     OnTerrainCreated;

        //  API
        public void Init(Camera cam)
        {
            if (settings==null)
            {
                throw new Exception("Settings is null. Provide a settings object before calling Init.");
            }

            Terrains = new List<Terrain>();
            _modifiedTerrains = new HashSet<TerrainData>();
            _cam = cam;
            _terrainSize            = (int)settings.size;
            _terrainInitialHeight   = settings.defaultTerrainHeight;

            _paintLayerIndex        = 0;
            _brushStrength          = settings.brushStrengthDefault;
            _brushSize              = settings.brushSizeDefault;
            _brushIndex             = 0;
            _brushTextures          = settings.brushTextures;
            SetFlattenHeight(settings.flattenHeightDefault);
            SetBrushSize(_brushSize);

            _objectHeightMin        = settings.randomObjectHeightMin;
            _objectHeightMax        = settings.randomObjectHeightMax;
            _objectWidthMin         = settings.randomObjectWidthMin;
            _objectWidthMax         = settings.randomObjectWidthtMax;
            _objectPrototypes       = new TreePrototype[settings.objectPrefabs.Length];

            //  create tree prototypes
            for (int i = 0; i < settings.objectPrefabs.Length; i++)
            {
                var prototype = new TreePrototype();
                prototype.prefab = settings.objectPrefabs[i];
                _objectPrototypes[i] = prototype;
            }
        }

        public TerrainData[] GetModifiedTerrains()
        {
            return _modifiedTerrains.ToArray();
        }

        public void ClearModifiedData()
        {
            _modifiedTerrains.Clear();
        }

        public void UseBrushAtPointerPosition()
        {
            //  get a ray at desired screen position
            _ray = _cam.ScreenPointToRay(Input.mousePosition);
            
            //  raycast through the ray 
            if(Physics.Raycast (_ray, out _hit))
            {
                //  hit anything?
                if (_hit.transform != null)
                {
                    //  has a Terrain component?
                    if (_hit.transform.TryGetComponent<Terrain>(out Terrain terrain))
                    {
                        //  we hit a terrain
                        UseBrush(terrain, _hit.point);
                    }
                }
            }
        }

        public void UseBrush(Terrain terrain, Vector3 worldPosition)
        {
            SetTarget(terrain);

            _hit.point = worldPosition;
            
            //  set brushRect center to hit point
            _brushRect.center = new Vector2(worldPosition.x, worldPosition.z);

            Modify();               //  modify the target terrain
            TryModifyNeighbors();   //  check the brush size and see if any neighbor needs modification
        }

        public void SetBrushSize(int value)
        {
            _brushSize = Mathf.Clamp(value, 1, _terrainSize);
            
            _brush = GenerateBrush(_brushTextures[_brushIndex], _brushSize);
            _brushRect = new Rect(Vector2.zero, Vector2.one*_brushSize);
        }

        public void SetBrushStrength(float value)
        {
            _brushStrength = value;
        }

        public void SetFlattenHeight(float value)
        {
            _flattenHeight = Mathf.Clamp01(value / (float)_terrainSize);;
        }

        public void SetBrushIndex(int index)
        {
            _brushIndex = index;

            //  refresh brush data with new index
            _brush = GenerateBrush(_brushTextures[_brushIndex], _brushSize);
        }

        public void SetPaintLayerIndex(int index)
        {
            _paintLayerIndex = index;
        }

        public void SetObjectIndex(int index)
        {
            _objectIndex = index;
        }

        public void SetObjectDensity(float density)
        {
            _objectDensity = density;
        }

        public void SetMode(BrushMode mode)
        {
            _brushMode = mode;
        }

        public void SetTerrainSize(int size)
        {
            _terrainSize = size;
        }

        public void SetInitialHeight(float height)
        {
            _terrainInitialHeight = height;
        }

        public TerrainFileData Export()
        {
            //  create a file data
            var file = new TerrainFileData();

            //  iterate all created terrains
            foreach (var terrain in Terrains)
            {
                TerrainData terrainData = terrain.terrainData;

                //  populate data
                var save         = new TerrainSaveData();
                save.mapSize     = (int)terrainData.size.x;
                save.splatMap    = terrainData.GetAlphamaps  (0, 0, terrainData.alphamapWidth,       terrainData.alphamapHeight);
                save.heightMap   = terrainData.GetHeights    (0, 0, terrainData.heightmapResolution, terrainData.heightmapResolution);
                save.posX        = terrain.transform.position.x;
                save.posZ        = terrain.transform.position.z;

                save.treeInstanceData = new List<TreeInstanceData>();
                float treeCount = terrainData.treeInstanceCount;
                for (int i = 0; i < treeCount; i++)
                {
                    var treeInstance    = terrainData.GetTreeInstance(i);
                    var treeSaveData    = new TreeInstanceData()
                    {
                        posX           = treeInstance.position.x,
                        posY           = treeInstance.position.y,
                        posZ           = treeInstance.position.z,
                        widthScale     = treeInstance.widthScale,
                        heightScale    = treeInstance.heightScale,
                        rotation       = treeInstance.rotation,
                        prototypeIndex = treeInstance.prototypeIndex,
                    };
                    
                    save.treeInstanceData.Add(treeSaveData);
                }

                //  add to file
                file.terrains.Add(save);
            }

            return file;
        }

        public void Import(TerrainFileData file)
        {
            Clear();

            //  Iterate terrains in the file
            foreach (var save in file.terrains)
            {
                //  create a terrain from the data
                TerrainData terrainData             = new TerrainData();
                terrainData.terrainLayers           = settings.paintLayers;
                terrainData.treePrototypes          = _objectPrototypes;

                terrainData.heightmapResolution     = save.mapSize;
                terrainData.alphamapResolution      = save.mapSize;
                terrainData.baseMapResolution       = save.mapSize;
                terrainData.size                    = new Vector3(save.mapSize, save.mapSize, save.mapSize);
                
                TreeInstance[] treeInstances = new TreeInstance[save.treeInstanceData.Count];

                for (int i = 0; i < treeInstances.Length; i++)
                {
                    treeInstances[i] = new TreeInstance()
                    {
                        position        = new Vector3(save.treeInstanceData[i].posX, save.treeInstanceData[i].posY, save.treeInstanceData[i].posZ),
                        heightScale     = save.treeInstanceData[i].widthScale,
                        widthScale      = save.treeInstanceData[i].heightScale,
                        rotation        = save.treeInstanceData[i].rotation,
                        prototypeIndex  = save.treeInstanceData[i].prototypeIndex,
                    };
                }

                terrainData.SetHeights(0,0,save.heightMap);
                terrainData.SetAlphamaps(0,0,save.splatMap);
                terrainData.SetTreeInstances(treeInstances, true);

                CreateTerrainInternal(terrainData, save.posX, save.posZ);
            }
        
        }

        public void CreateGrid(int columCount, int rowCount)
        {
            Clear();
            
            for (int x = 0; x < columCount; x++)
            {
                for (int z = 0; z < rowCount; z++)
                {
                    //  Create a terrain with a offset
                    float posX = x * _terrainSize;
                    float posZ = z * _terrainSize;
                    
                    CreateTerrain(posX, posZ);
                }
            }
        }

        public void CreateTerrain(float posX, float posZ)
        {
            //  populate a default terrainData
            TerrainData terrainData = new TerrainData();
            
            //  set size
            terrainData.heightmapResolution = _terrainSize;
            terrainData.alphamapResolution  = _terrainSize;
            terrainData.baseMapResolution   = _terrainSize;
            terrainData.size                = Vector3.one * _terrainSize;

            //  init initial heights
            int resolution = terrainData.heightmapResolution;

            //  height should be between 0..1
            float height = Mathf.Clamp01(_terrainInitialHeight / _terrainSize);

            float[,] heights = new float[resolution, resolution];

            for (int i = 0; i < resolution; i++)
            {
                for (int j = 0; j < resolution; j++)
                {
                    heights[i, j] = height;
                }
            }
            terrainData.SetHeights(0, 0, heights);

            terrainData.terrainLayers   = settings.paintLayers;
            terrainData.treePrototypes  = _objectPrototypes;

            CreateTerrainInternal(terrainData, posX, posZ);
        }

        public void Clear()
        {
            //  destroy current terrains
            foreach (var t in Terrains)
            {
                Destroy(t.gameObject);
            }
            Terrains.Clear();

            //  command history values are invalid now, let's clear
            CommandHistory.Clear();
        }

        //  Internal
        private void SetTarget(Terrain terrain)
        {
            _targetTerrain = terrain;
        }

        private void TryModifyNeighbors()
        {
            /*
                #   #   #

                #   +   #

                #   #   #
            */
            var centerTerrain = _targetTerrain;

            /*
                #   ?   #
                    |
                #   +   #

                #   #   #
            */
            //  check top
            var topNeighbor = centerTerrain.topNeighbor;
            if (topNeighbor != null)
            {
                ModifyIfOverlaps(topNeighbor);
                /*
                    ? - #   #
                        |
                    #   +   #

                    #   #   #
                */
                //  check top left
                if (topNeighbor.leftNeighbor != null)
                {
                    ModifyIfOverlaps(topNeighbor.leftNeighbor);
                }
            }

            /*
                #   #   #
                        
                #   + - ?

                #   #   #
            */
            //  check right
            var rightNeighbor = centerTerrain.rightNeighbor;
            if (rightNeighbor != null)
            {
                ModifyIfOverlaps(rightNeighbor);
                /*
                    #   #   ?
                            |
                    #   + - #

                    #   #   #
                */
                //  check right top
                if (rightNeighbor.topNeighbor != null)
                {
                    ModifyIfOverlaps(rightNeighbor.topNeighbor);
                }
            }

            /*
                #   #   #
                        
                #   +   #
                    |
                #   ?   #
            */
            //  check bottom
            var bottomNeighbor = centerTerrain.bottomNeighbor;
            if (bottomNeighbor != null)
            {
                ModifyIfOverlaps(bottomNeighbor);
                /*
                    #   #   #
                            
                    #   +   #
                        |
                    #   # - ?
                */
                //  check bottom right
                if (bottomNeighbor.rightNeighbor != null)
                {
                    ModifyIfOverlaps(bottomNeighbor.rightNeighbor);
                }
            }

            /*
                #   #   #
                        
                ? - +   #
                     
                #   #   #
            */
            //  check left
            var leftNeighbor = centerTerrain.leftNeighbor;
            if (leftNeighbor != null)
            {
                ModifyIfOverlaps(leftNeighbor);
                /*
                    #   #   #
                        
                    # - +   #
                    |    
                    ?   #   #
                */
                //  check left botton
                if (leftNeighbor.bottomNeighbor != null)
                {
                    ModifyIfOverlaps(leftNeighbor.bottomNeighbor);
                }
            }
        
            
            /*
                # - #   #
                    |   |
                # - + - #
                |   |
                #   # - #
            */
            //  Finally we check every possible neigbors
        }

        private void Modify()
        {
            //  try add to modified terrains
            if (_modifiedTerrains.Add(_targetTerrain.terrainData))
            {
                //  added to list, invoke event
                OnTerrainModificationStart?.Invoke(_targetTerrain.terrainData);
            }

            switch (_brushMode)
            {
                case BrushMode.RAISE:
                case BrushMode.LOWER:
                case BrushMode.FLATTEN:
                case BrushMode.SMOOTH:
                    ModifyHeights();
                break;
                case BrushMode.PAINT_TEXTURE:
                    ModifyPaint();
                break;
                case BrushMode.PAINT_OBJECT:
                case BrushMode.REMOVE_OBJECT:
                    ModifyObject();
                break;
            }
        }

        private void ModifyIfOverlaps(Terrain terrain)
        {
            Rect targetTerrainRect = GetRectByHeightMap(terrain);
            if (_brushRect.Overlaps(targetTerrainRect))
            {
                SetTarget(terrain);
                Modify();
            }
        }

        private void FixBordersVertical(TerrainData lowerTD, TerrainData upperTD)
        {
            //  get border heights of two neighbor terrain 
            int height      = 1;
            int width       = _targetTerrain.terrainData.heightmapResolution;
            
            int lowerXBase  = 0;
            int lowerYBase  = lowerTD.heightmapResolution-1;

            float[,] upperHeights = lowerTD.GetHeights(lowerXBase, lowerYBase, width, height);
            
            int upperXBase = 0;
            int upperYBase = 0;
            
            float[,] lowerHeights = upperTD.GetHeights(upperXBase, upperYBase, width, height);

            //  calculate average of heights
            for (int i = 0; i < width; i++)
            {
                float avgHeight = (upperHeights[0,i] + lowerHeights[0,i])/2f;
                upperHeights[0,i] = avgHeight;
                lowerHeights[0,i] = avgHeight;
            }

            //  set calculated heights
            lowerTD.SetHeights(lowerXBase, lowerYBase, lowerHeights);
            upperTD.SetHeights(upperXBase, upperYBase, upperHeights);
        }

        private void FixBordersHorizontal(TerrainData leftTD, TerrainData rightTD)
        {
            //  get border heights of two neighbor terrain 
            int height = _targetTerrain.terrainData.heightmapResolution;
            int width = 1;
            
            int leftXBase = leftTD.heightmapResolution-1;
            int leftYBase = 0;

            float[,] leftHeights = leftTD.GetHeights(leftXBase, leftYBase, width, height);
            
            int rightXBase = 0;
            int rightYBase = 0;
            
            float[,] rightHeights = rightTD.GetHeights(rightXBase, rightYBase, width, height);
            
            //  calculate average of heights
            for (int i = 0; i < height; i++)
            {
                float avgHeight = (leftHeights[i,0] + rightHeights[i,0])/2f;
                leftHeights[i,0] = avgHeight;
                rightHeights[i,0] = avgHeight;
            }

            //  set calculated heights
            leftTD.SetHeights(leftXBase, leftYBase, leftHeights);
            rightTD.SetHeights(rightXBase, rightYBase, rightHeights);
        }

        private void ModifyHeights()
        {
            TerrainData terrainData = _targetTerrain.terrainData;
            //  create a rect in actual terrain dimensions
            Rect terrainRect        = GetRectByHeightMap(_targetTerrain);
            
            //  intersect the brush size by terrain rect
            //  so we can safely get heights of the target terrain 
            Rect modifyArea         = Intersect(_brushRect, terrainRect);
            
            int modifyAreaSizeX     = Mathf.RoundToInt(modifyArea.size.x);
            int modifyAreaSizeY     = Mathf.RoundToInt(modifyArea.size.y);

            //  position relative to target terrain world position
            int terrainXBase        = Mathf.RoundToInt(modifyArea.x - _targetTerrain.transform.position.x); 
            int terrainYBase        = Mathf.RoundToInt(modifyArea.y - _targetTerrain.transform.position.z); 

            //  if modifyArea is smaller than the brushSize (think terrain left and bottom edge cases)
            //  then offset modify position 
            int brushOffsetX = 0;
            if (_brushRect.x < terrainRect.x)
            {
                brushOffsetX = _brushSize - modifyAreaSizeX;    
            }
            
            int brushOffsetY = 0;
            if (_brushRect.y < terrainRect.y)
            {
                brushOffsetY = _brushSize - modifyAreaSizeY;
            }
            
            // finally get heights of the desired area of target terrain
            float[,] heights = terrainData.GetHeights(terrainXBase, terrainYBase, modifyAreaSizeX, modifyAreaSizeY);

            // normalize modify speed by actual terrain size
            float speedBySizeMultiplier = ((float)TerrainSize.Size128/(float)terrainData.heightmapResolution);

            switch (_brushMode)
            {
                case BrushMode.RAISE: 
                {
                    for (int yy = 0; yy < modifyAreaSizeY; yy++)
                    {
                        for (int xx = 0; xx < modifyAreaSizeX; xx++)
                        {
                            //for each point we raise the value  by the value of brush at the coords * the strength modifier
                            heights[yy, xx] += _brush[brushOffsetY+yy, brushOffsetX+xx] * _brushStrength * Constants.RAISE_OR_LOWER_STROKE_MULTIPLIER * speedBySizeMultiplier; 
                        }
                    }


                    // This bit of code will save the change to the Terrain data file, 
                    // this means that the changes will persist out of play mode into the edit mode
                    terrainData.SetHeights(terrainXBase, terrainYBase, heights);
                }
                break;
                case BrushMode.LOWER: 
                {
                    for (int yy = 0; yy < modifyAreaSizeY; yy++)
                    {
                        for (int xx = 0; xx < modifyAreaSizeX; xx++)
                        {
                            //for each point we raise the value  by the value of brush at the coords * the strength modifier
                            heights[yy, xx] -= _brush[brushOffsetY+yy, brushOffsetX+xx] * _brushStrength * Constants.RAISE_OR_LOWER_STROKE_MULTIPLIER * speedBySizeMultiplier; 
                        }
                    }

                    // set changes 
                    terrainData.SetHeights(terrainXBase, terrainYBase, heights);
                }
                break;
                case BrushMode.FLATTEN: 
                {
                    for (int yy = 0; yy < modifyAreaSizeY; yy++)
                    {
                        for (int xx = 0; xx < modifyAreaSizeX; xx++)
                        {
                            // moves the points towards their targets
                            heights[yy, xx] = Mathf.MoveTowards(heights[yy, xx], _flattenHeight * speedBySizeMultiplier, _brush[brushOffsetY + yy, brushOffsetX + xx] * _brushStrength);
                        }
                    }

                    // set changes
                    terrainData.SetHeights(terrainXBase, terrainYBase, heights);
                }
                break;
                case BrushMode.SMOOTH: 
                {
                    for (int yy = 0; yy < modifyAreaSizeY; yy++)
                    {
                        for (int xx = 0; xx < modifyAreaSizeX; xx++)
                        {
                            float avg = 0f;
                            float maxHeight = 1f / terrainData.size.y;

                            //  get avarage value of surrounding heights
                            avg += terrainData.GetHeight(terrainXBase + xx-1, terrainYBase + yy  ) * maxHeight;
                            avg += terrainData.GetHeight(terrainXBase + xx+1, terrainYBase + yy  ) * maxHeight;
                            avg += terrainData.GetHeight(terrainXBase + xx,   terrainYBase + yy-1) * maxHeight;
                            avg += terrainData.GetHeight(terrainXBase + xx,   terrainYBase + yy+1) * maxHeight;

                            avg = avg / 4;

                            heights[yy, xx] = Mathf.MoveTowards(heights[yy, xx], avg, _brush[brushOffsetY+yy, brushOffsetX+xx] * _brushStrength); 
                        }
                    }
                
                    // set changes
                    terrainData.SetHeights(terrainXBase, terrainYBase, heights);

                    //  check top border
                    if (terrainYBase + modifyAreaSizeY >= terrainData.heightmapResolution)
                    {
                        if (_targetTerrain.topNeighbor != null)
                        {
                            FixBordersVertical(_targetTerrain.terrainData, _targetTerrain.topNeighbor.terrainData);
                        }
                    }

                    //  check bottom border
                    if (terrainYBase == 0)
                    {
                        if (_targetTerrain.bottomNeighbor != null)
                        {
                            FixBordersVertical(_targetTerrain.bottomNeighbor.terrainData, _targetTerrain.terrainData);
                        }
                    }

                    //  check left border
                    if (terrainXBase == 0)
                    {
                        if (_targetTerrain.leftNeighbor != null)
                        {
                            FixBordersHorizontal(_targetTerrain.leftNeighbor.terrainData, _targetTerrain.terrainData);
                        }
                    }

                    //  check right border
                    if (terrainXBase + modifyAreaSizeX >= terrainData.heightmapResolution)
                    {
                        if (_targetTerrain.rightNeighbor != null)
                        {
                            FixBordersHorizontal(_targetTerrain.terrainData, _targetTerrain.rightNeighbor.terrainData);
                        }
                    }
                }
                break;


            }
        }

        private void ModifyObject()
        {
            TerrainData terrainData = _targetTerrain.terrainData;
            switch (_brushMode)
            {
                case BrushMode.PAINT_OBJECT:
                {
                    Vector2 randomOffset    = 0.5f * UnityEngine.Random.insideUnitCircle;
                    randomOffset.x         *= _brushSize / terrainData.size.x;
                    randomOffset.y         *= _brushSize / terrainData.size.z;

                    Vector3 pos             = Vector3.zero;
                    Vector3 hitPosOffset    = _hit.point - _targetTerrain.transform.position;
                    pos.x = hitPosOffset.x/terrainData.size.x + randomOffset.x;
                    pos.z = hitPosOffset.z/terrainData.size.z + randomOffset.y;
                    pos.y = hitPosOffset.y/terrainData.size.y;

                    float spacing = 1/(_brushStrength * Constants.TREE_SPACE_MULTIPLIER);
                    if (pos.x >= 0 && pos.x <= 1 && pos.z >= 0 && pos.z <= 1 && CanPlaceTreeAtPosition(pos, spacing))
                    {
                        var instance = new TreeInstance()
                        {
                            position        = pos,
                            heightScale     = UnityEngine.Random.Range(_objectHeightMin, _objectHeightMax),
                            widthScale      = UnityEngine.Random.Range(_objectWidthMin,  _objectWidthMax ),
                            rotation        = UnityEngine.Random.Range(0, 2 * Mathf.PI),
                            color           = Color.white,
                            lightmapColor   = Color.white,
                            prototypeIndex  = _objectIndex
                        };

                        
                        _targetTerrain.AddTreeInstance(instance);
                    }

                }
                break;

                case BrushMode.REMOVE_OBJECT: 
                {
                    Vector3 pos             = Vector3.zero;
                    Vector3 hitPosOffset    = _hit.point - _targetTerrain.transform.position;
                    pos.x = hitPosOffset.x/terrainData.heightmapResolution;
                    pos.z = hitPosOffset.z/terrainData.heightmapResolution;
                    pos.y = hitPosOffset.y/terrainData.size.y;

                    float range = (float)_brushSize / terrainData.heightmapResolution;

                    var instances = new List<TreeInstance>(terrainData.treeInstances);
                    int total = terrainData.treeInstanceCount;
                    for (int i = 0; i < total; i++)
                    {
                        TreeInstance instance = terrainData.GetTreeInstance(i);
                        if (Vector3.Distance(instance.position, pos) < range)
                        {
                            instances.RemoveAt(i);
                            terrainData.SetTreeInstances(instances.ToArray(), true);
                            break;
                        }
                    }

                }
                break;
            }
        }

        private void ModifyPaint()
        {
            switch (_brushMode)
            {
                case(BrushMode.PAINT_TEXTURE):
                {
                    TerrainData terrainData = _targetTerrain.terrainData;
                    //  create a rect in actual terrain dimensions
                    Rect terrainRect = GetRectByAlphaMap(_targetTerrain);

                    //  intersect the brush size by terrain rect
                    //  so we can safely get heights of the target terrain 
                    Rect modifyArea  = Intersect(_brushRect, terrainRect);
                    
                    int modifyAreaSizeX     = Mathf.RoundToInt(modifyArea.size.x);
                    int modifyAreaSizeY     = Mathf.RoundToInt(modifyArea.size.y);
                    
                    modifyAreaSizeX         = Mathf.Max(modifyAreaSizeX, 0);
                    modifyAreaSizeY         = Mathf.Max(modifyAreaSizeY, 0);
                    
                    //  position relative to target terrain world position
                    int terrainXBase        = Mathf.RoundToInt(modifyArea.x - _targetTerrain.transform.position.x); 
                    int terrainYBase        = Mathf.RoundToInt(modifyArea.y - _targetTerrain.transform.position.z); 
                    
                    //  make sure that base position is in alphamap range
                    terrainXBase            = Mathf.Clamp(terrainXBase, 0, terrainData.alphamapResolution);
                    terrainYBase            = Mathf.Clamp(terrainYBase, 0, terrainData.alphamapResolution);

                    //  if modifyArea is smaller than the brushSize (think terrain left and bottom edge cases)
                    //  then offset modify position 
                    int brushOffsetX = 0;
                    if (_brushRect.x < terrainRect.x)
                    {
                        brushOffsetX = _brushSize - modifyAreaSizeX;    
                    }
                    
                    int brushOffsetY = 0;
                    if (_brushRect.y < terrainRect.y)
                    {
                        brushOffsetY = _brushSize - modifyAreaSizeY;
                    }

                    //grabs the splat map data for our brush area
                    float[,,] splat = terrainData.GetAlphamaps(terrainXBase, terrainYBase, modifyAreaSizeX, modifyAreaSizeY); 

                    for (int yy = 0; yy < modifyAreaSizeY; yy++)
                    {
                        for (int xx = 0; xx < modifyAreaSizeX; xx++)
                        {
                            //creates a float array and sets the size to be the number of paints your terrain has
                            float[] weights = new float[terrainData.alphamapLayers]; 
                            for (int zz = 0; zz < splat.GetLength(2); zz++)
                            {
                                //grabs the weights from the terrains splat map
                                int k = Mathf.Clamp(yy, 0, splat.GetLength(0));
                                int l = Mathf.Clamp(xx, 0, splat.GetLength(1));
                                int m = Mathf.Clamp(zz, 0, splat.GetLength(2));

                                weights[zz] = splat[k, l, m];
                            }
                            // adds weight to the paint currently selected with the int paint variable
                            weights[_paintLayerIndex] += _brush[brushOffsetY+yy, brushOffsetX+xx] * _brushStrength * Constants.PAINT_STROKE_MULTIPLIER; 
                            //this next bit normalizes all the weights so that they will add up to 1
                            float sum = weights.Sum();
                            for (int ww = 0; ww < weights.Length; ww++)
                            {
                                if (yy < splat.GetLength(0) && xx < splat.GetLength(1))
                                {
                                    weights[ww] /= sum;
                                    splat[yy,xx,ww] = weights[ww];
                                }
                            }
                        }
                    }
                    //applies the changes to the terrain, they will also persist
                    terrainData.SetAlphamaps(terrainXBase, terrainYBase, splat);
                }
                break;
            }
        }
        
        private void CreateTerrainInternal(TerrainData terrainData, float posX, float posZ)
        {
            var terrainGameObject = Terrain.CreateTerrainGameObject(terrainData);
            terrainGameObject.transform.position = new Vector3(posX, 0, posZ);
            terrainGameObject.transform.SetParent(this.transform);

            var terrain = terrainGameObject.GetComponent<Terrain>();
            
            //  Ensure terrain neighbor connection
            //  Unity will connect adjacent terrains by default 
            terrain.allowAutoConnect = true;


            Terrains.Add(terrain);
            OnTerrainCreated?.Invoke(terrain);
        }

        //  Utility
        private static Rect GetRectByHeightMap(Terrain terrain)
        {
            return new Rect(terrain.transform.position.x,
                                            terrain.transform.position.z,
                                            terrain.terrainData.heightmapResolution,
                                            terrain.terrainData.heightmapResolution);
        }

        private static Rect GetRectByAlphaMap(Terrain terrain)
        {
            return new Rect(terrain.transform.position.x,
                                            terrain.transform.position.z,
                                            terrain.terrainData.alphamapResolution,
                                            terrain.terrainData.alphamapResolution);
        }

        private static float[,] GenerateBrush(Texture2D texture, int size)
        {
            //  Creates a 2d array which will store our brush
            float[,] heightMap = new float[size,size];
            Texture2D scaledBrush = ResizeBrush(texture,size,size); // this calls a function which we will write next, and resizes the brush image
            //  This will iterate over the entire re-scaled image and convert the pixel color into a value between 0 and 1
            for (int x = 0; x < size; x++)
            {
                for(int y = 0; y < size; y++)
                {
                    Color pixelValue = scaledBrush.GetPixel(x, y);
                    heightMap[x, y] = pixelValue.grayscale / 255F;
                }
            }
            
            return heightMap;
        }
        
        private static Texture2D ResizeBrush(Texture2D src, int width, int height, FilterMode mode = FilterMode.Trilinear)
        {
            Rect texR = new Rect(0, 0, width, height);
            ScaleTexture(src, width, height, mode);
            //  Get rendered data back to a new texture
            Texture2D result = new Texture2D(width, height, TextureFormat.ARGB32, true);
            result.Reinitialize(width, height);
            result.ReadPixels(texR, 0, 0, true);
            return result;
        }
        
        private static void ScaleTexture(Texture2D src, int width, int height, FilterMode fmode)
        {
            //  We need the source texture in VRAM because we render with it
            src.filterMode = fmode;
            src.Apply(true);
            //  Using RTT for best quality and performance.
            RenderTexture rtt = new RenderTexture(width, height, 32);
            //  Set the RTT in order to render to it
            Graphics.SetRenderTarget(rtt);
            //  Setup 2D matrix in range 0..1, so nobody needs to care about sized
            GL.LoadPixelMatrix(0, 1, 1, 0);
            //  Then clear & draw the texture to fill the entire RTT.
            GL.Clear(true, true, new Color(0, 0, 0, 0));
            Graphics.DrawTexture(new Rect(0, 0, 1, 1), src);
        }

        private bool CanPlaceTreeAtPosition(Vector3 pos, float spacing)
        {
            TerrainData terrainData = _targetTerrain.terrainData;

            int totalCount = terrainData.treeInstanceCount;
            for (int i = 0; i < totalCount; i++)
            {
                TreeInstance instance   = terrainData.GetTreeInstance(i);
                float dist              = Vector3.Distance(instance.position, pos);
                if (dist < spacing)
                {
                    return false;
                }
            }

            return true;
        }
        
        private static Rect Intersect(Rect rect1, Rect rect2)
        {
            Rect result = rect1;

            if (rect1.xMin > rect2.xMin)
            {
                result.xMin = rect1.xMin;
            } 
            else 
            {
                result.xMin = rect2.xMin;
            }

            if (rect1.xMax < rect2.xMax)
            {
                result.xMax = rect1.xMax;
            } 
            else
            {
                result.xMax = rect2.xMax;
            }

            if (rect1.yMax < rect2.yMax)
            {
                result.yMax = rect1.yMax;
            } 
            else 
            {
                result.yMax = rect2.yMax;
            }

            if (rect1.yMin > rect2.yMin) 
            { 
                result.yMin = rect1.yMin;
            } 
            else 
            { 
                result.yMin = rect2.yMin;
            }

            return result;
        }

    }
}