using System;
using MedievalKingdomUI.Scripts.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace MedievalKingdomUI.Scripts.Window
{
    public class NationPickController : MonoBehaviour
    {
        [SerializeField]
        private Text nationNameTextLabel;
        [SerializeField]
        private NationProperties[] nations;

        private int _pickedCharacter;

        private void Start()
        {
            if (nations.Length < 1)
            {
                gameObject.SetActive(false);
            }
            _pickedCharacter = 0;
        }

        public void Next()
        {
            ChangeNationInternal(1);
        }

        public void Previous()
        {
            ChangeNationInternal(-1);
        }

        public void ChangeNation(int index)
        {
            if (index < 0)
            {
                throw new ArgumentException();
            }
            ChangeNationInternal(index - _pickedCharacter);
        }

        private void ChangeNationInternal(int step)
        {
            nations[_pickedCharacter].Character.SetActive(false);
            nations[_pickedCharacter].DescriptionEmblem.SetActive(false);
            _pickedCharacter += step;
            if (_pickedCharacter >= nations.Length)
            {
                _pickedCharacter = 0;
            } else if (_pickedCharacter < 0)
            {
                _pickedCharacter = nations.Length - 1;
            }

            nations[_pickedCharacter].Character.SetActive(true);
            nations[_pickedCharacter].DescriptionEmblem.SetActive(true);
            nationNameTextLabel.text = nations[_pickedCharacter].Name;
        }
    }
}
