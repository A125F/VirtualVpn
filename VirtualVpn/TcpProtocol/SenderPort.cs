﻿using VirtualVpn.Helpers;

namespace VirtualVpn.TcpProtocol;

/// <summary>
/// Combine remote address and local port into a hashable key
/// </summary>
public readonly struct SenderPort
{
    /// <summary>
    /// Address of the remote system as an internal representation.
    /// If zero, this is invalid.
    /// </summary>
    public readonly ulong Address;
    
    /// <summary>
    /// Port on local system being requested.
    /// If zero, this is invalid.
    /// </summary>
    public readonly ushort Port;

    public override bool Equals(object? obj)
    {
        if (obj is SenderPort other)
        {
            return Port == other.Port
                && Address   == other.Address;
        }
        return false;
    }

    public SenderPort(byte[] senderAddress, int destinationPort)
    {
        Address = Bit.BytesToUInt64Msb(senderAddress);
        Port = (ushort)destinationPort;
    }

    public override int GetHashCode()
    {
        var h = Port + (Port << 16);
        h ^= (int)(Address >>  0);
        h ^= (int)(Address >> 32);
        return h;
    }

    public static bool operator ==(SenderPort left, SenderPort right) => left.Equals(right);

    public static bool operator !=(SenderPort left, SenderPort right) => !(left == right);
}