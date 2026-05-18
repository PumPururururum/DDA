using System;
using _ExampleProject.Code.Features.Player.Facade;
using UnityEngine;

namespace _ExampleProject.Code.Features._Core.Behaviours
{
    public sealed class LevelExitTrigger : MonoBehaviour
    {
        private static Sprite _exitSprite;

        private Action _onPlayerEntered;
        private bool _consumed;

        public static Sprite ExitSprite
        {
            get
            {
                if (_exitSprite != null)
                    return _exitSprite;

                var texture = new Texture2D(1, 1)
                {
                    name = "GeneratedLevelExitTexture",
                    filterMode = FilterMode.Point
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();

                _exitSprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, 1f, 1f),
                    new Vector2(0.5f, 0.5f),
                    1f);
                _exitSprite.name = "GeneratedLevelExitSprite";
                return _exitSprite;
            }
        }

        public void Construct(Action onPlayerEntered)
        {
            _onPlayerEntered = onPlayerEntered;
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (_consumed || other == null)
                return;

            if (other.GetComponentInParent<PlayerFacade>() == null)
                return;

            _consumed = true;
            _onPlayerEntered?.Invoke();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(transform.position, transform.lossyScale);
        }
    }
}
