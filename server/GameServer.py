import socket
import sys
from threading import Thread


class GameServer:
    socket = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
    clients = []

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
        self.socket.recvfrom(1024)
        # self.socket.listen(10)

        if not self.host:
            self.host = "localhost"

        print("Game server is running at {}:{}".format(self.host, self.port))

        # Run server
        try:
            while True:
                data, address = self.socket.recvfrom(self.port)

                print(data)

                thread = Thread(target=self.handler, args=(data, address))
                thread.daemon = True
                thread.start()
                # self.clients.append(conn)

        except KeyboardInterrupt:
            self.socket.close()

        finally:
            self.socket.close()

    def handler(self, data, address):
        print("* {}:{} connected...".format(address[0], address[1]))

        # Send welcome message
        # data.sendall(str.encode("Hello"))

        self.socket.sendto("ok", address)

        # while True:
        #     try:
        #         data = c.recv(1024)
        #         message = data.decode('UTF-8')
        #         message = message.replace('\n', '')
        #
        #         if not data:
        #             print("* {}:{} disconnected...".format(a[0], a[1]))
        #             self.clients.remove(c)
        #             c.close()
        #             break
        #
        #         if len(message) <= 1:
        #             continue
        #
        #         # TODO: response
        #
        #         if message is None:
        #             c.shutdown(socket.SHUT_RDWR)
        #             break
        #
        #         c.sendall(str.encode("Received: {}\n".format(message)))
        #
        #     except socket.error as e:
        #         print('Error: {}'.format(e))

        c.close()


def main():
    print("Python version: " + sys.version)
    server = GameServer('', 8080)
    server.run()


main()
