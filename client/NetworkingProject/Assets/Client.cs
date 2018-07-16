using System.Net;
using System.Net.Sockets;
using System;
using UnityEngine;

public class Client : MonoBehaviour {
    // TCP client connection and data buffer
    readonly internal TcpClient client = new TcpClient();
    readonly internal byte[] buffer = new byte[5000];

    // Connection options
    readonly internal IPAddress address = IPAddress.Parse("127.0.0.1");
    readonly internal int port = 8080;

    // Create unique player ID on each initialization
    internal String playerId = Guid.NewGuid().ToString("N");

    // Whether client and server are ready
    internal bool isAuthenticated = false;

    // Minimize redudant server requests
    internal String lastCoordinate = "";

    // Convenience getter
    internal NetworkStream Stream {
        get {
            return client.GetStream();
        }
    }

    // Unity initialization.
    void Start() {
        client.NoDelay = true;
        client.BeginConnect(address, port, HandleConnect, null);

        Debug.Log("I should have connected to the server...");
    }

    // Unity update is called once per frame.
    void Update() {
        if (isAuthenticated) {
            SendPlayerUpdate();
        }
    }

    // Client is reading data from server.
    internal void OnRead(IAsyncResult a) {
        int length = Stream.EndRead(a);
        if (length == 0) {
            Debug.Log("No length!");
            return;
        }

        string msg = System.Text.Encoding.UTF8.GetString(buffer, 0, length);

        // Split messages
        string[] messages = msg.Split(';');

        // Handle each server message
        foreach (string message in messages) {
            string[] arr = message.Split(',');

            // Server is requesting authentication
            if (arr[0].Equals("auth-request")) {
                Debug.Log("auth request control block");
                SendAuthIdentity();
                continue;
            }

            // Client is successfully authenticated
            if (arr[0].Equals("auth-success")) {
                Debug.Log("auth success control block");

                if (!arr[1].Equals(playerId)) {
                    Debug.LogWarning("Player ID sent to server was changed by server.");
                }

                double x = Double.Parse(arr[2]);
                double y = Double.Parse(arr[3]);
                double z = Double.Parse(arr[4]);

                Debug.Log("Coordinates are " + x + ", " + y + ", " + z);

                playerId = arr[1];
                isAuthenticated = true;

                continue;
            }

            // Only run further commands if authenticated
            if (isAuthenticated) {
                if (arr[0].Equals("update-success")) {
                    continue;
                }

                // Handle player update
                if (arr[0].Equals("player-update")) {
                    Debug.Log("Player update");
                }
            }
        }

        Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
    }

    // Client did finish connecting asynchronously.
    internal void HandleConnect(IAsyncResult a) {
        Debug.Log("I finished connecting!");
        Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
    }

    // Send player ID to server
    internal void SendAuthIdentity() {
        SendMessage(playerId);
    }

    // Send current coordinates to server
    internal void SendPlayerUpdate() {
        string position = "0,0,0";

        if (!position.Equals(lastCoordinate)) {
            SendMessage("position,0,0,0");
        }

        lastCoordinate = position;
    }

    // Send a message to the server.
    internal void SendMessage(string message) {
        byte[] b = System.Text.Encoding.UTF8.GetBytes(message);
        Stream.Write(b, 0, b.Length);
    }

    // Called when Unity application is closed, make sure to close connections.
    private void OnApplicationQuit() {
        Debug.Log("I am quitting...");
        client.Close();
    }
}
