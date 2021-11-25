#!/usr/bin/env python

import socket
import struct


this_ip = "192.168.2.3"
portRcv = 5454

# Create a UDP socket
s = socket.socket(socket.AF_INET, socket.SOCK_DGRAM)
server_address = (this_ip, portRcv)
try:
    s.bind(server_address)
except socket.error as msg:
    print 'Bind failed. Error Code : ' + str(msg[0]) + ' Message ' + msg[1]
    sys.exit()

data, address = s.recvfrom(4096)
print data
