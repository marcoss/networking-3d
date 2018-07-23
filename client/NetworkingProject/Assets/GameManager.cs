using System.Net;
using System.Net.Sockets;
using System;
using UnityEngine;
using System.Collections.Generic;

public class GameManager : MonoBehaviour {
    // Static var to hold player location
    static public Transform PLAYER_TRANSFORM;

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

    // Queue for player disconnects
    internal Queue<String> removalQueue = new Queue<String>();

    // Info on a player
    internal struct PlayerInfo {
        public String playerId;
        public Vector3 position;
        public Vector3 rotation;

        public PlayerInfo(String playerId, Vector3 position, Vector3 rotation) {
            this.playerId = playerId;
            this.position = position;
            this.rotation = rotation;
        }
    }

    // Unity initialization.
    void Start() {
        client.NoDelay = true;
        client.BeginConnect(address, port, HandleConnect, null);

        // Init
        PLAYER_TRANSFORM = transform;

        Debug.Log("I should have connected to the server...");
    }

    // Unity update is called once per frame.
    void Update() {
        if (isAuthenticated) {
            // Send current update
            HandlePlayerUpdate();

            // Spawn queue
            while (spawnQueue.Count > 0) {
                PlayerInfo p = spawnQueue.Dequeue();
                SpawnPlayer(p);
            }

            // Update queue
            while (updateQueue.Count > 0) {
                PlayerInfo p = updateQueue.Dequeue();
                UpdatePlayer(p);
            }

            // Removal queue
            while (removalQueue.Count > 0)
            {
                String p = removalQueue.Dequeue();
                RemovePlayer(p);
            }
        }
    }

    // Prevent all players from spawning on same point
    internal Vector3 RandomizePosition() {
        int x = UnityEngine.Random.Range(-5, 5);
        int z = UnityEngine.Random.Range(-5, 5);

        Vector3 position = transform.position;

        position.x += x;
        position.z += z;

        return position;
    }

    // Spawn player in game
    internal void SpawnPlayer(PlayerInfo p) {
        GameObject obj = Instantiate(prefabCar, p.position, transform.rotation);
        players.Add(p.playerId, obj);
    }

    // Update player in game
    internal void UpdatePlayer(PlayerInfo p) {
        players[p.playerId].transform.position = p.position;
        players[p.playerId].transform.eulerAngles = p.rotation;
    }

    // Remove player from map
    internal void RemovePlayer(string playerId) {
        if (players.ContainsKey(playerId)) {
            Destroy(players[playerId]);
            players.Remove(playerId);
        }
    }

    // Client is reading data from server
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
                SendAuthIdentity();
                continue;
            }

            // Client is successfully authenticated
            if (arr[0].Equals("auth-success")) {
                if (!arr[1].Equals(playerId)) {
                    Debug.LogWarning("Player ID created by client was changed by server.");
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
                // Do nothing
                if (arr[0].Equals("update-success")) continue;

                if (arr[0].Equals("player-disconnect")) {
                    string pid = arr[1];
                    removalQueue.Enqueue(pid);
                }

                // Handle player update
                if (arr[0].Equals("player-update")) {
                    // Update player
                    string pid = arr[1];

                    // Skip self update (may introduce lag)
                    if (pid.Equals(playerId)) continue;

                    // Position coords
                    float px = float.Parse(arr[2]);
                    float py = float.Parse(arr[3]);
                    float pz = float.Parse(arr[4]);

                    // Rotation coords
                    float rx = float.Parse(arr[5]);
                    float ry = float.Parse(arr[6]);
                    float rz = float.Parse(arr[7]);

                    Vector3 location = new Vector3(px, py, pz);
                    Vector3 rotation = new Vector3(rx, ry, rz);

                    // Player information
                    PlayerInfo player = new PlayerInfo(pid, location, rotation);

                    if (!players.ContainsKey(pid)) {
                        Debug.Log("New player!");
                        spawnQueue.Enqueue(player);
                    } else {
                        updateQueue.Enqueue(player);
                    }
                }
            }
        }

        Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
    }

    // Client did finish connecting asynchronously.
    internal void HandleConnect(IAsyncResult a) {
        Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
    }

    // Send player ID to server
    internal void SendAuthIdentity() {
        SendMessage(playerId);
    }

    // Send current coordinates to server
    internal void HandlePlayerUpdate() {
        if (playerCar == null) {
            playerCar = Instantiate(prefabPlayerCar, RandomizePosition(), transform.rotation);
        }
        else {
            PLAYER_TRANSFORM = playerCar.transform;
        }

        string position = PLAYER_TRANSFORM.position.x + "," + PLAYER_TRANSFORM.position.y + "," + PLAYER_TRANSFORM.position.z;

        string rotation = PLAYER_TRANSFORM.eulerAngles.x + "," + PLAYER_TRANSFORM.eulerAngles.y + "," + PLAYER_TRANSFORM.eulerAngles.z;

        if (!position.Equals(lastCoordinate)) {
            SendMessage("position," + position + ",rotation," + rotation);
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
