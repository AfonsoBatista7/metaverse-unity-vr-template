using System.Net;
using UnityEngine;
using Unity.Netcode;
using System.Net.Sockets;
using Unity.Netcode.Transports.UTP;
using System.Collections;


public class GameManager : MonoBehaviour {

    [SerializeField] private string _serverAddress = "127.0.0.1";
    [SerializeField] private ushort _port = 7777;

    void Start() {
#if UNITY_SERVER
        StartServer();
#else
        StartClient();

        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback += ReconectClient;
#endif
    }


    void OnApplicationQuit() {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= ReconectClient;
    }

    void StartServer() {
        // Start the server
        NetworkManager.Singleton.StartServer();
    }

    void StartClient() {

        string serverIP = IPAddress.TryParse(_serverAddress, out _) ?
            _serverAddress :
            ResolveHostname(_serverAddress);

        if (serverIP == "") Debug.Log("No IP specified");

        ConnectAsIp(serverIP);
    }

    void ConnectAsIp(string ip) {
        try {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = ip;

            // Optionally set a default port, or fetch it from user input
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = _port; // Use your server's port

            // Start the client
            NetworkManager.Singleton.StartClient();

            Debug.Log($"Client started and connected to server at {NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address}:{NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port}");
        } catch (System.Exception ex) {
            Debug.LogError($"An unexpected error occurred: {ex.Message}");
        }
    }

    string ResolveHostname(string hostname) {
        try {
            // Get IP addresses from the hostname
            IPHostEntry hostEntry = Dns.GetHostEntry(hostname);

            string firstValidIp = "";

            // Getting the first valid IP address
            foreach (IPAddress ip in hostEntry.AddressList) {
                if (ip.AddressFamily == AddressFamily.InterNetwork) {
                    // Set the resolved IP address for UnityTransport
                    firstValidIp = ip.ToString();
                    break; // Exit the loop once we find the first valid IPv4 address
                }
            }

            return firstValidIp;

        } catch (SocketException ex) {
            Debug.LogError($"Error resolving hostname '{hostname}': {ex.Message}");
            return "";
        }
    }
    IEnumerator WaitReconectClient() {
        yield return new WaitForSeconds(1);

        Debug.Log("Reconnecting Client..."); 
        NetworkManager.Singleton.StartClient();
    }

    void ReconectClient(ulong clientId) {
        StartCoroutine(WaitReconectClient());
    }
}
