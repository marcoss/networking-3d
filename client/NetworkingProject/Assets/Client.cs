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
    readonly internal String playerId = Guid.NewGuid().ToString("N");

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
        //Debug.Log("Hello world!");
	}

    // Client is reading data from server.
    internal void OnRead(IAsyncResult a) {
        int length = Stream.EndRead(a);
        if (length == 0) {
            Debug.Log("No length!");
            return;
        }

        string msg = System.Text.Encoding.UTF8.GetString(buffer, 0, length);

        Debug.Log(msg);

        // Server is asking us to identify
        if (msg.Equals("identify")) {
            Send(playerId);
        }

        // ???

        Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
    }

    // Client did finish connecting asynchronously.
    internal void HandleConnect(IAsyncResult a) {
        Debug.Log("I finished connecting!");
        Stream.BeginRead(buffer, 0, buffer.Length, OnRead, null);
    }

    // Send a message to the server.
    internal void Send(string message) {
        byte[] b = System.Text.Encoding.UTF8.GetBytes(message);
        Stream.Write(b, 0, b.Length);
    }

    // Called when Unity application is closed, make sure to close connections.
    private void OnApplicationQuit() {
        Debug.Log("I am quitting...");
        client.Close();
    }
}
