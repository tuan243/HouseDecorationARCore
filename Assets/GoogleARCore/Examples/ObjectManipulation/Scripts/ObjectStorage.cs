using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class ObjectStorage : MonoBehaviour
{
    [Serializable]
    public struct FurnitureGroup
    {
        public string categoryName;
        public List<GameObject> furnitures;
        public List<Sprite> icons;
    }
    public List<FurnitureGroup> allFurnitures;
}
