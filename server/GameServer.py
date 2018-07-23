import socket
import sys
from threading import Thread, Timer
from utilities import safe_string


class GameServer:
    socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    # Store list of connections
    connections = []

    # Store disconnecting players
    disconnections = []

    # Chat messages
    chat = []

    players = dict()

    def __init__(self, host, port):
        self.host = host
        self.port = port

    def run(self):
        # Bind socket to local host and port
        try:
            self.socket.bind((self.host, self.port))
            self.socket.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
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
        Timer(1.0, self.broadcasting_loop).start()

        if self.players:
            data = ''

            # Send player updates
            for player in self.players:
                data += "player-update,{},{};".format(player, self.players[player]['location'])

            for connection in self.connections:
                connection.sendall(str.encode(data))

        # Send disconnections
        if self.disconnections:
            data = ''

            for player_id in self.disconnections:
                print("* Player " + player_id + " disconnected from server")
                data += "player-disconnect,{};".format(player_id)
                self.disconnections.remove(player_id)

            for connection in self.connections:
                connection.sendall(str.encode(data))

        if self.chat:
            data = ''

            for msg in self.chat:
                data += "player-chat,{};".format(msg)
                self.chat.remove(msg)

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

                    self.disconnections.append(player_id)

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
                        coordinate = "0.0,0.0,0.0,0.0,180.0,0.0"
                        self.players[player_id] = {}
                        self.players[player_id]['location'] = coordinate
                        conn.sendall(str.encode("auth-success,{},{}".format(player_id, coordinate)))

                    # Send all locations of current players
                    else:
                        coordinate = self.players.get(player_id, {}).get('location') or "0.0,0.0,0.0,0.0,180.0,0.0"
                        conn.sendall(str.encode("auth-success,{},{}".format(player_id, coordinate)))

                else:
                    messages = message.split(';')

                    for msg in messages:
                        arr = msg.split(',')

                        # Handle chat message
                        if arr[0] == 'chat':
                            self.chat.append(player_id[:5] + ": " + arr[1] + ";")

                        # Handle position update
                        if arr[0] == 'position':
                            # Position
                            rx = float(arr[1])
                            ry = float(arr[2])
                            rz = float(arr[3])

                            # Rotation
                            px = float(arr[5])
                            py = float(arr[6])
                            pz = float(arr[7])

                            self.players[player_id]['location'] = '{},{},{},{},{},{};'.format(rx, ry, rz, px, py, pz)
                            conn.sendall(str.encode("update-success"))

            except socket.error as e:
                # print("Error! {}".format(e))
                break

        conn.close()


def main():
    print("Python version: " + sys.version)
    server = GameServer('', 8080)
    server.run()


main()
