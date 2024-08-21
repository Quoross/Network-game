using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkManagerUI : MonoBehaviour
{
    [SerializeField] NetworkManager networkManager;
    bool done = false;

    private void Start()
    {
        if (!done)
        {
            ColorManager.ResetColors();
           done = true;
        }
    }

    private void OnGUI()
    {
        // Check if NetworkManager is assigned
        if (networkManager == null)
        {
            Debug.LogError("NetworkManager is not assigned.");
            return;
        }

        // Display the current state of the NetworkManager
        if (networkManager.IsHost)
        {
            GUILayout.Label("Status: Hosting");
            
        }
        else if (networkManager.IsClient)
        {
            GUILayout.Label("Status: Joined as Client");
        }
        else
        {
            GUILayout.Label("Status: Not Connected");
        }

        // Host button
        if (GUILayout.Button("Host"))
        {
            if (networkManager.IsHost || networkManager.IsClient)
            {
                Debug.LogWarning("Cannot start Host. An instance is already running.");
            }
            else
            {
                networkManager.StartHost();
                Debug.Log("Hosting started.");
            }
        }

        // Join button
        if (GUILayout.Button("Join"))
        {
            if (networkManager.IsHost || networkManager.IsClient)
            {
                Debug.LogWarning("Cannot start Client. An instance is already running.");
            }
            else
            {
                networkManager.StartClient();
                Debug.Log("Client started.");
            }
        }

        // Quit button
        if (GUILayout.Button("Quit"))
        {
            if (networkManager.IsHost || networkManager.IsClient)
            {
                networkManager.Shutdown();
                Debug.Log("Network Manager shutdown.");
            }
            Application.Quit();
            Debug.Log("Application quit.");
        }
    }
}