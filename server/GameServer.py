import socket
import sys
from threading import Thread, Timer
from utilities import safe_string
from time import time, sleep


class GameServer:
    socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    connections = []
    players = dict()

    def __init__(self, host, port):
        self.host = host
        self.port = port

    def run(self):
        # Bind socket to local host and port
        try:
            self.socket.bind((self.host, self.port))
        except socket.error as err:
            print("Error binding to server: {}".format(err))
            sys.exit()

        # Start listening on socket
        self.socket.listen(10)

        if not self.host:
            self.host = "localhost"

        print("Game server is running at {}:{}".format(self.host, self.port))

        # Run server
        try:
            thread = Thread(target=self.broadcasting_loop())
            thread.daemon = True
            thread.start()

            while True:
                conn, address = self.socket.accept()
                thread = Thread(target=self.handler, args=(conn, address))
                thread.daemon = True
                thread.start()
                self.connections.append(conn)

        except KeyboardInterrupt:
            self.socket.close()

        finally:
            self.socket.close()

    # Broadcast player movements to all clients
    def broadcasting_loop(self):
        Timer(5.0, self.broadcasting_loop).start()

        if self.players:
            data = "player-update,"

            for player in self.players:
                data += "{},{}".format(player, self.players[player]['location'])

            print(data)

            for connection in self.connections:
                connection.sendall(str.encode(data))

    def handler(self, conn, a):
        print("* {}:{} connected...".format(a[0], a[1]))

        # Send a message asking client to identify client UUID
        conn.sendall(str.encode('auth-request'))

        player_id = None

        while True:
            try:
                data = conn.recv(1024)
                message = data.decode('UTF-8')
                message = message.replace('\n', '')

                if not data:
                    print("* {}:{} disconnected...".format(a[0], a[1]))

                    # Remove from connections
                    self.connections.remove(conn)

                    # Remove from players
                    self.players.pop(player_id, None)

                    # Prepare to close connection
                    conn.shutdown(socket.SHUT_RDWR)
                    break

                if len(message) <= 1:
                    continue

                # If user has not identified
                if not player_id:
                    # Strip any illegal input
                    player_id = safe_string(message)

                    if len(player_id) < 1:
                        continue

                    if player_id not in self.players:
                        # Set coordinate
                        coordinate = "0,0,0"
                        self.players[player_id] = {}
                        self.players[player_id]['location'] = coordinate
                        print("New user @ location {}".format(coordinate))
                        conn.sendall(str.encode("auth-success,{},{}".format(player_id, coordinate)))

                    # Send all locations of current players
                    else:
                        coordinate = self.players.get(player_id, {}).get('location') or "0,0,0"
                        print("Existing user @ location {}".format(coordinate))
                        conn.sendall(str.encode("auth-success,{},{}".format(player_id, coordinate)))

                else:
                    messages = message.split(';')

                    for msg in messages:
                        arr = msg.split(',')

                        if arr[0] == 'position':
                            self.players[player_id]['location'] = '{},{},{}'.format(arr[1], arr[2], arr[3])
                            print("new loc is {}".format(self.players[player_id]['location']))
                            conn.sendall(str.encode("update-success"))

            except socket.error as e:
                print("Error! {}".format(e))
                break

        conn.close()


def main():
    print("Python version: " + sys.version)
    server = GameServer('', 8080)
    server.run()


main()
