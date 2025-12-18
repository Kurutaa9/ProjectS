using System;
using UnityEngine;

namespace MedievalKingdomUI.Scripts.Domain
{
    [Serializable]
    public class NationProperties
    {
        [SerializeField]
        private string name;
        [SerializeField]
        private GameObject character;
        [SerializeField]
        private GameObject descriptionEmblem;

        public string Name => name;

        public GameObject Character => character;

        public GameObject DescriptionEmblem => descriptionEmblem;
    }
}