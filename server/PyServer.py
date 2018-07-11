from socket import *
import os, time, mimetypes, threading

server_port = 2018
tlist = []
client_message = " ping "


class Server():
    def __init__(self):
        self.clientList = []
        self.clientTList = []

    def serverStart(self):
        try:
            server_socket = socket(AF_INET, SOCK_STREAM)
            server_socket.setsockopt(SOL_SOCKET, SO_REUSEADDR, 1)

            # Bind socket
            server_socket.bind(('', server_port))

            # Listen
            server_socket.listen(15)

            while True:
                connection_socket, addr = server_socket.accept()
                self.clientList.append(connection_socket)

                t = threading.Thread(target=self.clientHandler, args=(connection_socket,))
                t.start()

                self.clientTList.append(t)

        except server_socket.error:
            print("server error")
            sys.exit()