using Graph.World;
using UnityEngine;

namespace Utils.Cameras {
    public class CameraController : MonoBehaviour {
        [field: SerializeField] private float CameraSpeed { get; set; } = 10f;
        [field: SerializeField] private float ZoomSpeed { get; set; } = 5f;
        [field: SerializeField] private float ZoomSpeedMultiplicator { get; set; } = 10f;
        [field: SerializeField] private float MinZoom { get; set; } = 5f;

        [field: Tooltip("If set to < MinZoom, the max zoom will be just the size of the world.")]
        [field: SerializeField]
        private float MaxZoom { get; set; } = -1f;

        [field: SerializeField] private float AllowedBounds { get; set; } = 2f;
        [field: SerializeField] private bool StartInWorldCenter { get; set; } = true;
        [field: SerializeField] private bool AllowMovementOutOfBounds { get; set; }

        private WorldGameObject world;
        private UnityEngine.Camera Camera { get; set; }

        private void Awake() {
            Camera = GetComponent<UnityEngine.Camera>();
            world = FindObjectOfType<WorldGameObject>();
        }

        private void Start() {
            SetCameraWorldCenter();
        }

        private void Update() {
            var horizontal = Input.GetAxis("Horizontal");
            var vertical = Input.GetAxis("Vertical");
            var zoom = Input.GetAxis("Mouse ScrollWheel");
            var ctrl = Input.GetKey(KeyCode.LeftControl);

            if (MaxZoom < 0) MaxZoom = CalculateMaxZoom(Camera.aspect, world.Width, world.Height);

            var size = Camera.orthographicSize;
            size -= zoom * ZoomSpeed * (ctrl ? ZoomSpeedMultiplicator : 1);

            size = Mathf.Clamp(size, MinZoom, MaxZoom);
            Camera.orthographicSize = size;

            var position = transform.position;
            position.x += horizontal * CameraSpeed * Time.deltaTime * size / 10f;
            position.y += vertical * CameraSpeed * Time.deltaTime * size / 10f;
            var minX = AllowMovementOutOfBounds ? 0 : Camera.orthographicSize * Camera.aspect - 0.5f - AllowedBounds;
            var minY = AllowMovementOutOfBounds ? 0 : Camera.orthographicSize - 0.5f - AllowedBounds;
            var maxX = AllowMovementOutOfBounds ? world.Width : world.Width - minX < minX ? minX : world.Width - minX;
            var maxY = AllowMovementOutOfBounds ? world.Height :
                world.Height - minY < minY ? minY : world.Height - minY;
            position.x = Mathf.Clamp(position.x, minX, maxX);
            position.y = Mathf.Clamp(position.y, minY, maxY);
            transform.position = position;
        }

        private void SetCameraWorldCenter() {
            if (!StartInWorldCenter) return;
            var position = transform.position;
            position.x = world.Width / 2f;
            position.y = world.Height / 2f;
            transform.position = position;
        }

        private static float CalculateMaxZoom(float aspectRatio, float width, float height) {
            // is width or height the limiting factor?
            var widthBounds = 1f * width / height > aspectRatio;
            // set max zoom to half of the world size minus 0.5f with respect to the aspect ratio
            return widthBounds ? width / 2f - 0.5f : height / (2f * aspectRatio) - 0.5f;
        }
    }
}