#nullable enable

using System;
using System.Collections.Generic;
using Simulation;
using UnityEngine;

namespace Utils {
    public class CameraHandler : MonoBehaviour {
        private readonly List<SelectableCamera> allCameras = new();
        private int currentCameraIndex;

        private FlyCamera flyCamera = null!;
        private SimulationManager simulationManager = null!;

        private void Start() {
            flyCamera = FindObjectOfType<FlyCamera>() ?? throw new Exception("No FlyCamera found in scene.");
            simulationManager = FindObjectOfType<SimulationManager>() ??
                                throw new Exception("No SimulationManager found in scene.");

            allCameras.Add(flyCamera.Cam);

            simulationManager.PlayerVehicleChanged += (sender, args) => OnPlayerVehicleChanged(args.NewPlayerVehicle);
            OnPlayerVehicleChanged(simulationManager.PlayerVehicle);
        }

        private void Update() {
            if (Input.GetKeyDown(KeyCode.C)) NextCamera();
        }

        private void OnPlayerVehicleChanged(Vehicle? newPlayerVehicle) {
            foreach (var cam in allCameras) cam.SetActive(false);
            allCameras.Clear();
            allCameras.Add(flyCamera.Cam);

            if (newPlayerVehicle == null) {
                allCameras[0].SetActive(true);
                return;
            }

            foreach (var cam in newPlayerVehicle.VehicleCameras) allCameras.Add(cam);

            currentCameraIndex = 0;

            NextCamera();
        }

        private void NextCamera() {
            Debug.Log($"Switching camera from {currentCameraIndex} to {(currentCameraIndex + 1) % allCameras.Count}");
            allCameras[currentCameraIndex].SetActive(false);
            currentCameraIndex = (currentCameraIndex + 1) % allCameras.Count;
            allCameras[currentCameraIndex].SetActive(true);
        }
    }
}