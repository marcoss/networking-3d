using System.Net;
using System.Net.Sockets;
using System;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
    // TCP client connection and data buffer
    readonly internal TcpClient client = new TcpClient();
    readonly internal byte[] buffer = new byte[5000];

    // Pre-fab for player's car (automatically moves)
    public GameObject prefabPlayerCar;

    // Other player's cars
    public GameObject prefabCar;
 
    // Connection options
    readonly internal IPAddress address = IPAddress.Parse("127.0.0.1");
    readonly internal int port = 8080;

    // All players cars
    readonly internal Dictionary<String, GameObject> players = new Dictionary<String, GameObject>();

    // Current player's car
    internal GameObject playerCar;

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

    // Queue for player spawn updates
    internal Queue<PlayerInfo> spawnQueue = new Queue<PlayerInfo>();

    // Queue for player movement updates
    internal Queue<PlayerInfo> updateQueue = new Queue<PlayerInfo>();

    // Info on a player
    internal struct PlayerInfo {
        public String playerId;
        public Vector3 position;
        //public Vector3 rotation;

        public PlayerInfo(String playerId, Vector3 position) {
            this.playerId = playerId;
            this.position = position;
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
            // Player car should be spawned
            if (playerCar == null) {
                playerCar = Instantiate(prefabPlayerCar, RandomizePosition(), transform.rotation);
            }

            // Pending spawns
            while (spawnQueue.Count > 0) {
                PlayerInfo p = spawnQueue.Dequeue();
                GameObject obj = Instantiate(prefabCar, p.position, transform.rotation);
                players.Add(p.playerId, obj);
            }

            while (updateQueue.Count > 0) {
                PlayerInfo p = updateQueue.Dequeue();

                // Update position
                //players[p.playerId].transform.position = p.position;
            }

            // Send self update
            SendPlayerUpdate();
        }
    }

    internal Vector3 RandomizePosition() {
        int x = UnityEngine.Random.Range(-5, 5);
        int y = UnityEngine.Random.Range(-5, 5);

        Vector3 position = transform.position;

        position.x += x;
        position.y += y;

        return position;
    }

    // Spawn a car
    internal void SpawnCar() {
        //pendingSpawns++;
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
                    Debug.LogWarning("Player ID created by client was changed by server.");
                }

                double x = Double.Parse(arr[2]);
                double y = Double.Parse(arr[3]);
                double z = Double.Parse(arr[4]);

                Debug.Log("Coordinates are " + x + ", " + y + ", " + z);

                SpawnCar();

                playerId = arr[1];
                isAuthenticated = true;

                continue;
            }

            // Only run further commands if authenticated
            if (isAuthenticated) {
                // Do nothing
                if (arr[0].Equals("update-success")) continue;

                // Handle player update
                if (arr[0].Equals("player-update")) {
                    // Update player
                    string pid = arr[1];

                    // Skip self update (may introduce lag)
                    //if (pid.Equals(playerId)) continue;

                    float x = float.Parse(arr[2]);
                    float y = float.Parse(arr[3]);
                    float z = float.Parse(arr[4]);

                    Vector3 location = new Vector3(x, y, z);

                    // Player information
                    PlayerInfo player = new PlayerInfo(pid, location);

                    if (!players.ContainsKey(pid)) {
                        Debug.Log("New player!");
                        spawnQueue.Enqueue(player);
                    } else {
                        Debug.Log("Existing player!");
                        updateQueue.Enqueue(player);
                    }
                }
            }
        }

        Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
    }

    public void InstantiatePlayer(string pid)
    {
        Debug.Log("Adding player with id: " + pid);

        //GameObject player = SpawnCar();

        //players.Add(pid, player);
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
