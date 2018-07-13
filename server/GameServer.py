import socket
import sys
from threading import Thread
import re


class GameServer:
    socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)

    connections = []
    clients = dict()

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

    def handler(self, c, a):
        print("* {}:{} connected...".format(a[0], a[1]))

        # Send a message asking client to identify client UUID
        c.sendall(str.encode('identify'))

        client_identity = None

        while True:
            try:
                data = c.recv(1024)
                message = data.decode('UTF-8')
                message = message.replace('\n', '')

                if not data:
                    print("* {}:{} disconnected...".format(a[0], a[1]))
                    self.connections.remove(c)
                    c.close()
                    break

                if len(message) <= 1:
                    continue

                # TODO: response

                if message is None:
                    c.shutdown(socket.SHUT_RDWR)
                    break

                if not client_identity:
                    print("Identifying user!")

                    # Strip any illegal input
                    temp_id = re.sub(r'\W+', '', message)

                    if len(temp_id) < 1:
                        continue

                    client_identity = temp_id

                    print("original id was {}, temp id is now {}".format(message, client_identity))

                    if client_identity not in self.clients:
                        c.sendall(str.encode("welcome new user {}\n".format(client_identity)))
                    #     Send all locations of current players
                    else:
                        c.sendall(str.encode("welcome back returning user {}\n".format(client_identity)))

                else:
                    c.sendall(str.encode("you have an identity but i can't process your msg"))

            except socket.error as e:
                print('Error: {}'.format(e))

        c.close()


def main():
    print("Python version: " + sys.version)
    server = GameServer('', 8080)
    server.run()


main()
