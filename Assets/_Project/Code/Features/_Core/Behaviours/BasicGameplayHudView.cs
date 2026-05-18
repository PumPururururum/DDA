using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Behaviours
{
    public sealed class BasicGameplayHudView : MonoBehaviour
    {
        private readonly Rect _boxRect = new Rect(10f, 10f, 300f, 92f);

        private GUIStyle _style;
        private string _text = "HP: -\nAmmo: -";

        public void SetPlayerStats(int currentHealth, int maxHealth, int magazineAmmo, int reserveAmmo)
        {
            int totalAmmo = Mathf.Max(0, magazineAmmo + reserveAmmo);
            _text = $"HP: {currentHealth}/{maxHealth}\nAmmo: {totalAmmo}  ({magazineAmmo}/{reserveAmmo})";
        }

        public void SetNoPlayer()
        {
            _text = "HP: -\nAmmo: -";
        }

        private void OnGUI()
        {
            if (_style == null)
            {
                _style = new GUIStyle(GUI.skin.box)
                {
                    alignment = TextAnchor.UpperLeft,
                    fontSize = 18,
                    padding = new RectOffset(12, 12, 10, 10)
                };
            }

            GUI.Box(_boxRect, _text, _style);
        }
    }
}
