namespace RuntimeTerrainEditor
{
    public class Constants
    {   
        public static int       MAX_UNDO                            = 100;
        public static float     MAX_BRUSH_STRENGTH                  = 1F;
        public static int       PAINT_STROKE_MULTIPLIER             = 80;
        public static float     RAISE_OR_LOWER_STROKE_MULTIPLIER    = 1F;
        public static float     TREE_SPACE_MULTIPLIER               = 500F;

        public static string    SAVE_FILE_DIRECTORY                 = UnityEngine.Application.persistentDataPath;
        public static string    SAVE_FILE_EXTENSION                 = "RUNTIMEMAP";
        public static string    SAVE_FILE_SEARCH_PATTERN            = "*." + SAVE_FILE_EXTENSION;
    }
}
