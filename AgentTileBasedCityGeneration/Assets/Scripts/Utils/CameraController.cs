using System.Collections;
using System.Collections.Generic;
using Simulation;
using UnityEngine;

public class CameraController : MonoBehaviour {
    
    private Camera Camera { get; set; }
    [field: SerializeField] private float CameraSpeed { get; set; } = 10f;
    [field: SerializeField] private float ZoomSpeed { get; set; } = 5f;
    [field: SerializeField] private float MinZoom { get; set; } = 5f;
    [field: SerializeField] private float MaxZoom { get; set; } = 15f;
    [field: SerializeField] private float AllowedBounds { get; set; } = 2f;
    
    private void Awake() {
        Camera = GetComponent<Camera>();
    }

    private void Update() {
        var horizontal = Input.GetAxis("Horizontal");
        var vertical = Input.GetAxis("Vertical");
        var zoom = Input.GetAxis("Mouse ScrollWheel");
        
        var size = Camera.orthographicSize;
        size -= zoom * ZoomSpeed;
        size = Mathf.Clamp(size, MinZoom, MaxZoom);
        Camera.orthographicSize = size;
        
        var position = transform.position;
        position.x += horizontal * CameraSpeed * Time.deltaTime * size / 10f;
        position.y += vertical * CameraSpeed * Time.deltaTime * size / 10f;
        var minX = Camera.orthographicSize * Camera.aspect - 0.5f - AllowedBounds;
        var minY = Camera.orthographicSize - 0.5f - AllowedBounds;
        var maxX = World.Instance.Width - minX;
        var maxY = World.Instance.Height - minY;
        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.y = Mathf.Clamp(position.y, minY, maxY);
        transform.position = position;
        
        
    }
    
}