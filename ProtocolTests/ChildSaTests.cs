﻿using NUnit.Framework;
using RawSocketTest;
using RawSocketTest.Crypto;
using RawSocketTest.Helpers;

namespace ProtocolTests;

[TestFixture]
public class ChildSaTests
{
    [Test]
    public void can_decode_ping()
    {
        Log.SetLevel(LogLevel.Everything);
        Settings.CodeModeForDescription = false;
        
        // Key sources --
        //prfId=PRF_HMAC_SHA2_256, integId=AUTH_HMAC_SHA2_256_128, cipherId=ENCR_AES_CBC, keyLength=128
        var theirNonce = new byte[] { 0x13, 0xDE, 0x72, 0xB8, 0xA7, 0xBE, 0x9A, 0xE3, 0x96, 0xDE, 0x52, 0xF8, 0xCA, 0x9D, 0x23, 0xA7, 0xFC, 0x3A, 0xFA, 0x89, 0xA3, 0x05, 0xEE, 0x89, 0xDB, 0x85, 0xBF, 0x48, 0x81, 0x00, 0xBF, 0xAB };
        var myNonce = new byte[] { 0xC5, 0x0E, 0x5D, 0xCE, 0x4C, 0x40, 0x9D, 0xB8, 0x87, 0x6A, 0xFC, 0x54, 0x13, 0xAC, 0xD1, 0x1D, 0x3A, 0x11, 0x60, 0xF7, 0xD3, 0x15, 0x9B, 0x39, 0x57, 0x78, 0xAC, 0xE1, 0x85, 0x2E, 0x7F, 0x46 };
        var sharedSecret = new byte[]
        {
            0xA8, 0xD1, 0x34, 0x94, 0xD2, 0x02, 0x30, 0xE5, 0x57, 0xA3, 0x6A, 0x14, 0x0C, 0x60, 0x87, 0x20, 0xB4, 0x9D, 0x71, 0x0E, 0x42, 0x68, 0x1A, 0xB1, 0x67, 0x33, 0x35, 0x50, 0xA4, 0x88, 0xD9, 0x38, 0x07, 0xC7, 0xCD, 0x07, 0x99, 0x89, 0xE0, 0x7A,
            0x8C, 0x44, 0xCE, 0x14, 0xC9, 0xDF, 0x61, 0xFD, 0x81, 0xF2, 0xFE, 0xD4, 0x37, 0xDA, 0x6B, 0xB5, 0xEB, 0x51, 0xD1, 0x3C, 0x8C, 0x24, 0xAE, 0xAF, 0xED, 0x3F, 0x74, 0x83, 0xF6, 0x99, 0xF2, 0x02, 0xC1, 0x31, 0x82, 0x05, 0xC9, 0x59, 0x5C, 0x10,
            0xEA, 0x0C, 0xAB, 0x13, 0x3D, 0x1B, 0xF2, 0x7B, 0xDD, 0x56, 0x1E, 0xE6, 0xD4, 0x58, 0x22, 0xA1, 0xDB, 0xA5, 0xF7, 0x12, 0x29, 0x9B, 0x41, 0xAF, 0x5A, 0xA7, 0x23, 0x9E, 0x10, 0x5F, 0x41, 0x73, 0x53, 0x13, 0xF7, 0x2A, 0xEC, 0x7C, 0x97, 0x62,
            0x3C, 0x1B, 0x6B, 0xF1, 0xBC, 0x78, 0x06, 0xFC, 0x31, 0x76, 0x02, 0x5D, 0xC5, 0x52, 0x78, 0x67, 0xBE, 0x17, 0x82, 0xCE, 0x53, 0xCA, 0x31, 0x0C, 0xD7, 0x69, 0x5D, 0x4C, 0xD8, 0xB5, 0xCF, 0x73, 0x82, 0x28, 0xD7, 0x91, 0x1A, 0xF3, 0xB3, 0x5E,
            0x29, 0x87, 0xD2, 0xD1, 0x15, 0x64, 0xF1, 0x1D, 0xDF, 0x28, 0xA1, 0x5C, 0xEF, 0x16, 0x7B, 0xCC, 0x51, 0xFD, 0xC7, 0xBA, 0x67, 0x77, 0xB1, 0x18, 0x53, 0x2B, 0xEC, 0x1C, 0x27, 0x29, 0x0B, 0x49, 0x1A, 0x7D, 0x35, 0x66, 0x12, 0x5B, 0xF2, 0x91,
            0x6F, 0xE6, 0x2D, 0xC3, 0x83, 0x9F, 0x34, 0xE5, 0xEC, 0xEE, 0xF7, 0x10, 0xAE, 0xC5, 0x6D, 0xB4, 0x5C, 0x69, 0x92, 0x29, 0x18, 0x83, 0xD9, 0xED, 0x1F, 0x0C, 0xA9, 0x30, 0xF6, 0x42, 0x8D, 0xD2, 0xE5, 0x10, 0x6A, 0x67, 0x60, 0xF5, 0xDB, 0xC5,
            0x12, 0x69, 0xB4, 0xF9, 0x94, 0x6E, 0x79, 0x25, 0xCB, 0x90, 0x44, 0x1E, 0xF1, 0xB9, 0xEB, 0x9E
        };
        var theirSpi = new byte[] { 0x9D, 0xAB, 0x30, 0xE5, 0x83, 0x8D, 0x80, 0xD3 };
        var mySpi = new byte[] { 0x00, 0x5E, 0x4D, 0x48, 0xCD, 0x76, 0x3A, 0x63 };

        // Keys --
        var SKd = new byte[] { 0x56, 0x4F, 0x15, 0xFF, 0x2F, 0xF9, 0x54, 0x27, 0xCC, 0x13, 0x07, 0x79, 0x3E, 0x63, 0x99, 0xCF, 0x83, 0x37, 0xFA, 0x33, 0x02, 0x1A, 0x1E, 0x25, 0xB7, 0xD6, 0x47, 0x71, 0x0A, 0x4B, 0xE0, 0xC4 };
        var skAi = new byte[] { 0xAB, 0x6F, 0xBF, 0x71, 0x01, 0xFA, 0x8C, 0xF4, 0x5B, 0x1C, 0x98, 0xAC, 0x76, 0x8A, 0x46, 0x04, 0x88, 0x57, 0xCF, 0xDA, 0xEA, 0x44, 0x0F, 0xE5, 0xA7, 0x42, 0x2C, 0x4B, 0x78, 0x7C, 0x35, 0x18 };
        var skAr = new byte[] { 0x05, 0xD1, 0x84, 0x23, 0x42, 0x32, 0xF2, 0x5A, 0xE5, 0x5C, 0x68, 0xF5, 0x6D, 0xF0, 0xC0, 0xD9, 0x8B, 0x69, 0xCC, 0x37, 0x1B, 0xB8, 0x75, 0x49, 0x2F, 0x52, 0x85, 0x27, 0x41, 0x82, 0x58, 0x17 };
        var skEi = new byte[] { 0x35, 0xAF, 0x42, 0x3E, 0x36, 0xE5, 0x70, 0x7E, 0xCF, 0xFE, 0xDA, 0xC4, 0x08, 0x99, 0x65, 0x06 };
        var skEr = new byte[] { 0x42, 0x6E, 0xF0, 0x92, 0xD2, 0xA9, 0xEA, 0xB2, 0x8F, 0x89, 0x6D, 0xCA, 0xDC, 0x29, 0x32, 0x73 };
        var skPi = new byte[] { 0x93, 0x5E, 0x33, 0xD5, 0x14, 0x11, 0x30, 0xE7, 0x12, 0xF3, 0xBA, 0x99, 0xF4, 0x96, 0xED, 0x9F, 0xA9, 0x94, 0x6C, 0xE6, 0x1D, 0x5A, 0x8D, 0x43, 0xBD, 0x95, 0xB5, 0x4F, 0xE1, 0x11, 0xC2, 0x41 };
        var skPr = new byte[] { 0xD1, 0xB9, 0x68, 0xCA, 0x13, 0x4A, 0x93, 0xED, 0x6F, 0x40, 0x90, 0x91, 0x44, 0xFA, 0xF3, 0xA6, 0x54, 0xD5, 0x7E, 0xFD, 0xC4, 0xAC, 0xC6, 0xE1, 0x4F, 0x3F, 0x26, 0x08, 0x61, 0x6A, 0x65, 0x36 };
        var keySource = new byte[]
        {
            0x56, 0x4F, 0x15, 0xFF, 0x2F, 0xF9, 0x54, 0x27, 0xCC, 0x13, 0x07, 0x79, 0x3E, 0x63, 0x99, 0xCF, 0x83, 0x37, 0xFA, 0x33, 0x02, 0x1A, 0x1E, 0x25, 0xB7, 0xD6, 0x47, 0x71, 0x0A, 0x4B, 0xE0, 0xC4, 0xAB, 0x6F, 0xBF, 0x71, 0x01, 0xFA, 0x8C, 0xF4,
            0x5B, 0x1C, 0x98, 0xAC, 0x76, 0x8A, 0x46, 0x04, 0x88, 0x57, 0xCF, 0xDA, 0xEA, 0x44, 0x0F, 0xE5, 0xA7, 0x42, 0x2C, 0x4B, 0x78, 0x7C, 0x35, 0x18, 0x05, 0xD1, 0x84, 0x23, 0x42, 0x32, 0xF2, 0x5A, 0xE5, 0x5C, 0x68, 0xF5, 0x6D, 0xF0, 0xC0, 0xD9,
            0x8B, 0x69, 0xCC, 0x37, 0x1B, 0xB8, 0x75, 0x49, 0x2F, 0x52, 0x85, 0x27, 0x41, 0x82, 0x58, 0x17, 0x35, 0xAF, 0x42, 0x3E, 0x36, 0xE5, 0x70, 0x7E, 0xCF, 0xFE, 0xDA, 0xC4, 0x08, 0x99, 0x65, 0x06, 0x42, 0x6E, 0xF0, 0x92, 0xD2, 0xA9, 0xEA, 0xB2,
            0x8F, 0x89, 0x6D, 0xCA, 0xDC, 0x29, 0x32, 0x73, 0x93, 0x5E, 0x33, 0xD5, 0x14, 0x11, 0x30, 0xE7, 0x12, 0xF3, 0xBA, 0x99, 0xF4, 0x96, 0xED, 0x9F, 0xA9, 0x94, 0x6C, 0xE6, 0x1D, 0x5A, 0x8D, 0x43, 0xBD, 0x95, 0xB5, 0x4F, 0xE1, 0x11, 0xC2, 0x41,
            0xD1, 0xB9, 0x68, 0xCA, 0x13, 0x4A, 0x93, 0xED, 0x6F, 0x40, 0x90, 0x91, 0x44, 0xFA, 0xF3, 0xA6, 0x54, 0xD5, 0x7E, 0xFD, 0xC4, 0xAC, 0xC6, 0xE1, 0x4F, 0x3F, 0x26, 0x08, 0x61, 0x6A, 0x65, 0x36
        };
        var seed = new byte[]
        {
            0x13, 0xDE, 0x72, 0xB8, 0xA7, 0xBE, 0x9A, 0xE3, 0x96, 0xDE, 0x52, 0xF8, 0xCA, 0x9D, 0x23, 0xA7, 0xFC, 0x3A, 0xFA, 0x89, 0xA3, 0x05, 0xEE, 0x89, 0xDB, 0x85, 0xBF, 0x48, 0x81, 0x00, 0xBF, 0xAB, 0xC5, 0x0E, 0x5D, 0xCE, 0x4C, 0x40, 0x9D, 0xB8,
            0x87, 0x6A, 0xFC, 0x54, 0x13, 0xAC, 0xD1, 0x1D, 0x3A, 0x11, 0x60, 0xF7, 0xD3, 0x15, 0x9B, 0x39, 0x57, 0x78, 0xAC, 0xE1, 0x85, 0x2E, 0x7F, 0x46, 0x9D, 0xAB, 0x30, 0xE5, 0x83, 0x8D, 0x80, 0xD3, 0x00, 0x5E, 0x4D, 0x48, 0xCD, 0x76, 0x3A, 0x63
        };
        var sKeySeed = new byte[] { 0x6F, 0xC3, 0x95, 0x36, 0x87, 0x28, 0x1A, 0x4B, 0x71, 0x31, 0x2E, 0xF8, 0xA7, 0x5C, 0xDA, 0xF6, 0x0F, 0xA0, 0x02, 0xC4, 0x69, 0xA9, 0xEA, 0x8C, 0x62, 0x06, 0xA4, 0x7E, 0xFD, 0x2B, 0xAF, 0x15 };
        
        // First 'ping' ESP:
        var esp0 = new byte[]
        {
            /*---- spi----------*/  /*-- seq -----------*/ // payload...
            0xCE, 0x53, 0x4C, 0x9E, 0x00, 0x00, 0x00, 0x01, 0xFE, 0x98, 0x42, 0x59, 0x5C, 0x1A, 0x7C, 0x53, 0xF0, 0x33, 0x37, 0x5E, 0x7A, 0x57, 0xEE, 0x77, 0x96, 0x48, 0xEF, 0x50, 0x43, 0xE3, 0x6D, 0x97, 0x6B, 0x87, 0x6E, 0x8C, 0x69, 0x79, 0xC3, 0x33,
            0xFF, 0x21, 0xE1, 0x1D, 0x5C, 0x02, 0xC2, 0x1B, 0x9F, 0xDE, 0x0C, 0x20, 0x88, 0x09, 0xB1, 0xB6, 0x68, 0xC0, 0xF7, 0x96, 0xC5, 0xEA, 0x7A, 0x64, 0x36, 0x10, 0x7E, 0xE5, 0x50, 0x46, 0x8D, 0x9E, 0xBD, 0x55, 0xCC, 0xE6, 0x5D, 0x0A, 0x74, 0xDE,
            0x2F, 0xF9, 0x5D, 0xB3, 0xD0, 0x80, 0xAE, 0x2B, 0xFA, 0xC4, 0x83, 0x33, 0x82, 0xFA, 0x9A, 0xB5, 0x70, 0xC9, 0xE5, 0xD5, 0x61, 0x6B, 0x3B, 0xEA, 0xA4, 0xC3, 0xE8, 0x35, 0x5F, 0xFC, 0x13, 0x94, 0xCD, 0xCC, 0x39, 0x5A, 0xF0, 0x95, 0xAE, 0x2C,
            0x15, 0x70, 0xC5, 0xF4, 0x6E, 0xD4, 0xE1, 0x1A, 0xEF, 0x91, 0x53, 0x3A, 0xB3, 0xE7, 0x79, 0x85
        };
        
        // Second 'ping' ESP: (should be the same except for anti-repeat bits)
        var esp1 = new byte[]
        {
            /*---- spi----------*/  /*-- seq -----------*/  // payload...
            0xCE, 0x53, 0x4C, 0x9E, 0x00, 0x00, 0x00, 0x02, 0xB8, 0x4F, 0x50, 0x4F, 0x65, 0xF9, 0x98, 0x45, 0x23, 0xBB, 0x44, 0x9D, 0x7D, 0x0D, 0xDC, 0xFB, 0xC7, 0x28, 0x5D, 0xB6, 0xAC, 0x05, 0x40, 0x78, 0x28, 0xE7, 0x25, 0x27, 0x84, 0x3F, 0xB8, 0xF5,
            0x6B, 0x84, 0x0F, 0x7C, 0xD9, 0x79, 0x80, 0x51, 0xB8, 0xBE, 0x62, 0x60, 0xE3, 0xF2, 0x6C, 0x91, 0xEC, 0x4F, 0x18, 0x90, 0xC2, 0xD9, 0xC8, 0xBB, 0x2C, 0x36, 0x69, 0x66, 0xE2, 0x86, 0x0C, 0x94, 0xFC, 0xCA, 0xA3, 0xD2, 0x94, 0x6D, 0x16, 0x44,
            0x62, 0x9D, 0x2F, 0xF6, 0xBD, 0x01, 0xA0, 0x81, 0x98, 0x4C, 0x7D, 0x74, 0xB3, 0xE8, 0xB4, 0x64, 0x8C, 0x30, 0x02, 0x62, 0x76, 0xE9, 0x18, 0x84, 0xF6, 0x93, 0x44, 0x06, 0x09, 0x40, 0x1E, 0xAA, 0xF3, 0x4F, 0xB2, 0x25, 0x7F, 0x62, 0x25, 0x86,
            0x06, 0x5A, 0xC3, 0xD3, 0xEC, 0x21, 0xB8, 0x64, 0x53, 0x41, 0x3A, 0xBD, 0x48, 0x1D, 0x33, 0xA0
        };


        var cryptoIn = new IkeCrypto(
            new Cipher(EncryptionTypeId.ENCR_AES_CBC, 128),
            new Integrity(IntegId.AUTH_HMAC_SHA2_256_128),
            new Prf(PrfId.PRF_HMAC_SHA2_256),
            skEr, skAr, skPr, null
            );
        
        var cryptoOut = new IkeCrypto(
            new Cipher(EncryptionTypeId.ENCR_AES_CBC, 128),
            new Integrity(IntegId.AUTH_HMAC_SHA2_256_128),
            new Prf(PrfId.PRF_HMAC_SHA2_256),
            skEi, skAi, skPi, null
        );
        
        var subject = new ChildSa(mySpi, theirSpi, // these are probably the wrong SPIs (should be 4 byte, not 8 byte). If this causes a problem, update the captures
            cryptoIn, cryptoOut);
        
        // quick test...
        var spi = esp0.Take(4).ToArray();
        var seq = Bit.BytesToUInt32(esp0.Skip(4).Take(4).ToArray());
        var payload = esp0.Skip(8).ToArray();
        
        
        // Decode!
        var plain = cryptoOut.DecryptEsp(payload, out var next);
        
        
        
        Console.WriteLine(Bit.Describe("esp0_plain", plain));
        Console.WriteLine($"Declared payload: {next.ToString()}");
        Console.WriteLine($"SPI= {Bit.HexString(spi)}");
        Console.WriteLine($"ESP Sequence = {seq}");
        
        Assert.That(plain.Length, Is.GreaterThan(5), "Decrypted length is invalid");
        
        var idx = 0;
        var versionAndLength = plain[idx++];
        var version = (byte)(versionAndLength >> 4);
        var headerLength = (byte)(versionAndLength & 0x0f);
        Console.WriteLine($"Version = {Bit.BinString(version)} (should be 0000 0100)");
        Console.WriteLine($"Header length = {headerLength}");
        
        var serviceType = plain[idx++];
        Console.WriteLine($"Service type = 0x{serviceType:x2}");
        
        var totalLength = Bit.ReadUInt16(plain, ref idx);
        Console.WriteLine($"Total length = {totalLength}");
        
        var packetId = Bit.ReadUInt16(plain, ref idx);
        Console.WriteLine($"Packet id = 0x{packetId:x4}");
        
        var flagsAndFrags = Bit.ReadUInt16(plain, ref idx);
        var flags = (byte)(flagsAndFrags >> 13);
        var fragmentIndex = flagsAndFrags & 0x1fff;
        Console.WriteLine($"Flags = {Bit.BinString(flags)} (expect 0000 0010 ?)");
        Console.WriteLine($"Fragment index = {fragmentIndex}");
        
        var ttl = plain[idx++];
        Console.WriteLine($"Time to live = {ttl}");
        var protocol = plain[idx++];
        Console.WriteLine($"Protocol = 0x{protocol:x2} ({protocol})");
        
        var checksum = Bit.ReadUInt16(plain, ref idx);
        Console.WriteLine($"Checksum = 0x{checksum:x4}");
        
        var source1 = plain[idx++]; var source2 = plain[idx++]; var source3 = plain[idx++]; var source4 = plain[idx++];
        Console.WriteLine($"Source = {source1}.{source2}.{source3}.{source4}");
        
        var dest1 = plain[idx++]; var dest2 = plain[idx++]; var dest3 = plain[idx++]; var dest4 = plain[idx++];
        Console.WriteLine($"Destination = {dest1}.{dest2}.{dest3}.{dest4}");

        if (idx < headerLength)
        {
            var optionLength = headerLength - idx;
            Console.WriteLine($"IP Options ({optionLength} bytes)");
            idx += optionLength;
        }
        
        var dataLength = plain.Length - idx;
        Console.WriteLine($"Packet data has {dataLength} bytes");
        // todo: test the whole run
        
        Assert.Inconclusive();
    }
}