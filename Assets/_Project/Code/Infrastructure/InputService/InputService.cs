using _Project.Code.Core.Abstractions.Contracts;
using UnityEngine;

namespace _ExampleProject.Code.Infrastructure.InputService
{
    public sealed class InputService : IInputService, ITick
    {
        public Vector2 MoveDirection => _moveDirection;
        public Vector2 AimWorldPosition => _aimWorldPosition;
        public bool IsShootHeld => Input.GetMouseButton(0);
        public bool IsReloadPressed => Input.GetKeyDown(KeyCode.R);

        private Vector2 _moveDirection;
        private Vector2 _aimWorldPosition;

        public void Tick()
        {
            float x = 0f;
            float y = 0f;

            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) x += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) y -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) y += 1f;

            var rawMove = new Vector2(x, y);
            _moveDirection = rawMove.sqrMagnitude > 1f ? rawMove.normalized : rawMove;

            var camera = Camera.main;
            if (camera == null)
                return;

            var mouse = Input.mousePosition;
            mouse.z = Mathf.Abs(camera.transform.position.z);
            _aimWorldPosition = camera.ScreenToWorldPoint(mouse);
        }
    }
}
