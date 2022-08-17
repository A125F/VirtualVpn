﻿using System.Diagnostics.CodeAnalysis;
using RawSocketTest.Enums;
using RawSocketTest.Helpers;

namespace RawSocketTest.InternetProtocol;

/// <summary>
/// IPv4 packet.
/// https://en.wikipedia.org/wiki/IPv4
/// </summary>
[ByteLayout]
public class IpV4Packet
{
    [BigEndianPartial(bits: 4, order:0)]
    public IpV4Version Version;
    
    /// <summary>
    /// Length of header, as a count of 32-bit words.
    /// Byte length is this times 4
    /// </summary>
    [BigEndianPartial(bits: 4, order:1)]
    public byte Length;
    
    [BigEndian(bytes: 1, order:2)]
    public int ServiceType;
    
    [BigEndian(bytes: 2, order:3)]
    public int TotalLength;
    
    [BigEndian(bytes: 2, order:4)]
    public int PacketId;
    
    [BigEndianPartial(bits: 3, order:5)]
    public IpV4HeaderFlags Flags;
    
    [BigEndianPartial(bits: 13, order:6)]
    public int FragmentIndex;
    
    [BigEndian(bytes: 1, order:7)]
    public int Ttl;
    
    [BigEndian(bytes: 1, order:8)]
    public IpV4Protocol Protocol;
    
    [BigEndian(bytes: 2, order:9)]
    public int Checksum;
    
    [ByteLayoutChild(order:10)]
    public IpV4Address Source = new();

    [ByteLayoutChild(order:11)]
    public IpV4Address Destination = new();
    
    [VariableByteString(source: nameof(OptionsLength), order:12)]
    public byte[] Options = Array.Empty<byte>();
    
    [RemainingBytes(order: 13)]
    public byte[] Payload = Array.Empty<byte>();

    
    /// <summary>
    /// Calculate how many bytes of 'options' we have, based
    /// on length field (usually zero)
    /// </summary>
    public int OptionsLength() => Length * 4 - 20;

    /// <summary>
    /// Calculate the checksum of the raw data.
    /// If the checksum is in place and correct, this will return zero.
    /// If the checksum is zeroed, this will give the correct value to inject
    /// </summary>
    public static ushort CalculateChecksum(byte[] raw)
    {
        if (raw.Length < 2) throw new Exception($"Invalid IPv4 packet length {raw.Length}");
        
        ulong sum = 0;
        var i = 0;
        // main words
        for (; i+1 < raw.Length; i+=2)
        {
            var word = (uint)((raw[i] << 8) | raw[i+1]);
            sum += word;
        }

        // trailing byte
        if (i < raw.Length - 1)
        {
            sum += raw[i];
        }

        // folding
        while ((sum >> 16) > 0)
        {
            sum = (sum & 0xffff) + (sum >> 16);
        }

        // bitwise flip
        return (ushort)~((ushort)sum);
    }
}


[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum IpV4Version
{
    Invalid = 0,
    Version4 = 4,
    Version6 = 6
}

[Flags]
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public enum IpV4HeaderFlags
{
    None = 0,
    
    /// <summary>
    /// Must not be set
    /// </summary>
    Reserved = 0x04,
    
    /// <summary>
    /// Packet should be dropped if fragmentation would occur.
    /// This can be used when sending packets to a host that does not have resources to perform reassembly of fragments.
    /// </summary>
    DontFragment = 0x02,
    
    /// <summary>
    /// For un-fragmented packets, the MF flag is cleared.
    /// For fragmented packets, all fragments except the last have the MF flag set.
    /// The last fragment has a non-zero Fragment Offset field, differentiating it from an un-fragmented packet.
    /// </summary>
    MoreFragments = 0x01
}

[ByteLayout]
public class IpV4Address
{
    [ByteString(bytes:4, order:0)]
    public byte[] Value = Array.Empty<byte>();

    public string AsString => ToString();
    
    public override string ToString()
    {
        if (Value.Length < 4) return "<empty>";
        return $"{Value[0]}.{Value[1]}.{Value[2]}.{Value[3]}";
    }
}