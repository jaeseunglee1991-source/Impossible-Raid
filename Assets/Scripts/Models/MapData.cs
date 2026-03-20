using UnityEngine;
using System;

namespace BossRaid.Models
{
    [Serializable]
    public class MapData
    {
        public int stageIndex;
        public string mapName;
        public string description;
        public Sprite previewSprite; // 인스펙터에서 할당
        public string attributeInfo; // 화염, 냉기 등 속성 정보

        public MapData(int index, string name, string desc, string attr)
        {
            stageIndex = index;
            mapName = name;
            description = desc;
            attributeInfo = attr;
        }
    }
}
