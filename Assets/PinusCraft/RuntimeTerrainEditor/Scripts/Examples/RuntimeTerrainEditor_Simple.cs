using UnityEngine;

namespace RuntimeTerrainEditor
{
    public class RuntimeTerrainEditor_Simple : MonoBehaviour
    {
        //  contains terrain configurations
        public GlobalSettings settings; //  assigned from the scene

        private RuntimeTerrain _rt;

        private void Start()
        {
            //  add component
            _rt = gameObject.AddComponent<RuntimeTerrain>();
            
            //  assign settings and initiliaze
            _rt.settings = settings;
            _rt.Init(Camera.main);

            //  configure brush
            _rt.SetMode(BrushMode.RAISE);
            _rt.SetBrushSize(40);
            _rt.SetBrushStrength(Constants.MAX_BRUSH_STRENGTH);

            //  terrain created callback
            _rt.OnTerrainCreated += OnCreated;

            //  create a grid of terrain by size  
            _rt.SetTerrainSize((int)TerrainSize.Size128);

            //  create a 4x4 grid (total 512 x 512)
            _rt.CreateGrid(4, 4);
            
            //  or create a single terrain 512 x 512
            // _rt.SetTerrainSize((int)TerrainSize.Size512);
            // _rt.CreateTerrain(0,0);
        }

        private void OnCreated(Terrain terrain)
        {
            terrain.name = "Terrain " + _rt.Terrains.Count;
        }

        private void FixedUpdate()
        {
            //  left mouse click
            if (Input.GetMouseButton(0))
            {
                //  stroke brush
                _rt.UseBrushAtPointerPosition();
                
                //  or provide which terrain to modify 
                //  and the modify position(as world position)
                // _rt.UseBrush(someTerrain, somePosition);
            }
        }
    }
}
