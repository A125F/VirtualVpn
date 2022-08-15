# VirtualVpn
A IKEv2 VPN gateway that presents an application as if it was a private network

This is currently not even slightly working

If your ports are in use on Windows, use `netstat -ano` to get the
process id. It's probably the IKE service, which you'll need to turn off.

Parts based on, or derived from:

https://github.com/dschoeffm/go-ikev2
https://github.com/alejandro-perez/pyikev2
https://github.com/qwj/python-vpn

https://datatracker.ietf.org/doc/html/rfc7296
https://www.omnisecu.com/tcpip/ikev2-phase-1-and-phase-2-message-exchanges.php
https://security.stackexchange.com/questions/56434/understanding-the-details-of-spi-in-ike-and-ipsec

http://unixwiz.net/techtips/iguide-ipsec.html

https://www.secfu.net/2017/12/23/the-ikev2-header-and-the-security-association-payload/

## Current issues & work-face

- Need to complete SA transaction and try to get to first 'ping' payload
- Need to be able to start a SA from this side