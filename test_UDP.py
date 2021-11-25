#!/usr/bin/env python

import socket
import time

msgFromClient       = "Hello UDP Server"

bytesToSend         = str.encode(msgFromClient)

serverAddressPort   = ("127.0.0.1", 5047)

bufferSize          = 1024



# Create a UDP socket at client side

UDPClientSocket = socket.socket(family=socket.AF_INET, type=socket.SOCK_DGRAM)



# Send to server using created UDP socket
while 1:
    UDPClientSocket.sendto(bytesToSend, serverAddressPort)
    time.sleep(0.1)
