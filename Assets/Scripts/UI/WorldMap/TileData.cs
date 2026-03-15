using UnityEngine;

namespace WoodLand3D.WorldMap
{
    [System.Serializable]
    public class TileData
    {
        public TileType type;
        public bool isLocked;
        public int level;
        public string displayName;
        public int resourceValue;
        public string resourceLabel;

        public static TileData Create(TileType t, bool locked, int lv, string name, int value, string label = "Wood")
        {
            return new TileData
            {
                type = t,
                isLocked = locked,
                level = lv,
                displayName = name,
                resourceValue = value,
                resourceLabel = label
            };
        }
    }
}
