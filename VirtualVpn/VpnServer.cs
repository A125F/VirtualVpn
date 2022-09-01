﻿using System.Net;
using System.Net.Sockets;
using SkinnyJson;
using VirtualVpn.Enums;
using VirtualVpn.EspProtocol;
using VirtualVpn.Helpers;
using VirtualVpn.InternetProtocol;

// ReSharper disable BuiltInTypeReferenceStyle

namespace VirtualVpn;

public interface ISessionHost
{
    void AddChildSession(ChildSa childSa);
    void RemoveChildSession(uint spi);
    void RemoveSession(ulong localSpi);
}

public class VpnServer : ISessionHost, IDisposable
{
    private const int NonEspHeader = 0; // https://docs.strongswan.org/docs/5.9/features/natTraversal.html

    private readonly Thread _eventPumpThread;
    private readonly UdpServer _server;
    private readonly Dictionary<UInt64, VpnSession> _sessions = new();
    private readonly Dictionary<UInt32, ChildSa> _childSessions = new();
    private volatile bool _running;
    private long _espCount;
    private int _messageCount;

    public VpnServer()
    {
        try
        {
            _server = new UdpServer(IkeResponder, SpeResponder);
        }
        catch (SocketException ex)
        {
            Log.Critical("Could not start VirtualVPN UDP server. Do you have other VPN software running? Try 'ipsec stop'");
            Log.Error("Failed to start core network interface", ex);
            throw;
        }

        _eventPumpThread = new Thread(EventPumpLoop) { IsBackground = true };
    }

    public void Run()
    {
        Log.Debug("Setup");
        Json.DefaultParameters.EnableAnonymousTypes = true;
        Console.CancelKeyPress += StopRunning;

        _running = true;
        
        _server.Start();
        _eventPumpThread.Start();


        while (_running)
        {
            // wait for local commands
            var cmd = Console.ReadLine();

            try
            {
                var prefix = Min2(cmd?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
                Console.WriteLine($"CMD: `{prefix[0]}` ({prefix[1]})\r\n");

                HandleCommands(prefix);
            }
            catch (Exception ex)
            {
                Log.Error("Failure in interactive command 'cmd'", ex);
            }
        }
    }

    private void HandleCommands(string[] prefix)
    {
        switch (prefix[0])
        {
            case "crypto": // very detailed logging, plus crypto details (INSECURE)
            {
                Log.SetLevel(LogLevel.Crypto);
                return;
            }
            case "trace": // very detailed logging
            {
                Log.SetLevel(LogLevel.Trace);
                return;
            }
            case "loud": // detailed logging
            {
                Log.SetLevel(LogLevel.Debug);
                return;
            }
            case "less": // informational logging
            {
                Log.SetLevel(LogLevel.Info);
                return;
            }

            case "quit": // exit VirtualVPN session
            {
                _running = false;
                return;
            }

            case "kill": // stop an association
            {
                Log.Error("Not implemented yet");
                return;
            }
            case "list": // list open SAs
            {
                ListGateways();
                return;
            }
            case "start": // connect to a gateway and open a new SA
            {
                try
                {
                    TryStartVpnIfNotAlreadyConnected(prefix[1]);
                }
                catch (Exception ex)
                {
                    Log.Error("Could not start VPN connection: ", ex);
                }

                return;
            }
            case "capture":
            {
                Settings.CaptureTraffic = !Settings.CaptureTraffic;
                Console.WriteLine($"Traffic capture now {(Settings.CaptureTraffic ? "on" : "off")}. Capture location '{Settings.FileBase}'");
                break;
            }
            case "airlift":
            {
                Settings.RunAirliftSite = true;
                // TODO: start the airlift loop
                break;
            }
            case "notify":
            {
                var bits = Min2(prefix[1].Split(",", StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
                var gateway = IpV4Address.FromString(bits[0]);
                var session = _sessions.Values.SingleOrDefault(cs => cs.Gateway == gateway);
                if (session is null)
                {
                    Console.WriteLine("Could not find an established session to that gateway");
                    return;
                }

                var networkLoc = IpV4Address.FromString(bits[1]);
                session.NotifyIpAddresses(networkLoc);
                break;
            }
            case "psk":
            {
                Settings.PreSharedKeyString = prefix[1]; // TODO: this should be on a per-gateway basis
                break;
            }
            default:
                Console.WriteLine("Known commands:");
                Console.WriteLine("    Logging:");
                Console.WriteLine("        trace, loud, less ... crypto (INSECURE)");
                Console.WriteLine("    Connection:");
                Console.WriteLine("        list, start [gateway], kill [sa-id],");
                Console.WriteLine("        notify [gateway-ip, network-location-ip]");
                Console.WriteLine("        psk [psk-string]");
                Console.WriteLine("    General:");
                Console.WriteLine("        quit, capture");
                return;
        }
    }

    private void ListGateways()
    {
        Console.WriteLine("\r\nEstablished connections:");
        foreach (var session in _childSessions)
        {
            Console.WriteLine($"    {session.Value.Gateway} [{session.Key}]");
        }
    }

    private void TryStartVpnIfNotAlreadyConnected(string gatewayAddress)
    {
        // Assume gateway address is IPv4 decimals for now.
        Console.WriteLine($"Requested connection to [{gatewayAddress}], searching for existing connections");
        
        var requestedGateway = IpV4Address.FromString(gatewayAddress);
        
        // first, see if we've already got a connection up:
        foreach (var childSession in _childSessions)
        {
            if (childSession.Value.Gateway == requestedGateway)
            {
                Console.WriteLine($"A VPN session is already open with {requestedGateway} as {childSession.Key}.\r\nTry 'kill {childSession.Key}' if you want to restart");
                return;
            }
        }
        
        // next, see if we've already got a connection pending:
        foreach (var vpnSession in _sessions)
        {
            if (vpnSession.Value.Gateway == requestedGateway)
            {
                Console.WriteLine($"A VPN session is in progress with {requestedGateway} as {vpnSession.Key}.\r\nTry 'kill {vpnSession.Key}' if you want to restart");
                return;
            }
        }
        
        Console.WriteLine("Starting contact with gateway");
        StartVpnSession(requestedGateway);
    }

    /// <summary>
    /// Start a new session with a remote gateway.
    /// </summary>
    private void StartVpnSession(IpV4Address gateway)
    {
        // Start a new IKEv2 session, with an ID of our choosing.
        // Add that to the ongoing sessions (we might need to map
        // both sides of SPI on session lookup?)
        
        var newSession = new VpnSession(gateway, _server, this, weAreInitiator:true, 0);
        Log.Debug($"Starting new session with SPI {newSession.LocalSpi:x16}");
        _sessions.Add(newSession.LocalSpi, newSession);
        
        newSession.RequestNewSession(gateway.MakeEndpoint(port:500));
    }

    /// <summary>
    /// Return at least two items from the array,
    /// even if the array is null or shorter than
    /// two items.
    /// </summary>
    private string[] Min2(string[]? split)
    {
        if (split is null) return new string[2];
        if (split.Length <= 0) return new string[2];
        if (split.Length == 1) return new[]{split[0], ""};
        return split;
    }

    public void Dispose()
    {
        _server.Dispose();
        GC.SuppressFinalize(this);
    }

    public void AddChildSession(ChildSa childSa)
    {
        if (childSa.WeAreInitiator)
        {
            _childSessions.Add(childSa.SpiOut, childSa);
        }
        else
        {
            _childSessions.Add(childSa.SpiIn, childSa);
        }
    }

    public void RemoveSession(ulong spi)
    {
        if (_sessions.ContainsKey(spi)) _sessions.Remove(spi);
    }

    public void RemoveChildSession(uint spi)
    {
        if (_childSessions.ContainsKey(spi)) _childSessions.Remove(spi);
    }

    private void StopRunning(object? sender, ConsoleCancelEventArgs e)
    {
        _running = false;
    }

    /// <summary>
    /// Responds to port 500 traffic
    /// </summary>
    private void IkeResponder(byte[] data, IPEndPoint sender)
    {
        // write capture to file for easy testing
        Log.Info($"Got a 500 packet, {data.Length} bytes");

        if (data.Length == 1)
        {
            Log.Warn("Received a keep-alive packet during IKE process. This probably means there is a network problem, or a fault in the handshake process.");
            return;
        }

        IkeSessionResponder(data, sender, sendZeroHeader: false);
    }

    private void IkeSessionResponder(byte[] data, IPEndPoint sender, bool sendZeroHeader)
    {
        if (Settings.CaptureTraffic)
        {
            var name = Settings.FileBase + $"IKEv2-{_messageCount++}_Port-{sender.Port}_IKE.bin";
            File.WriteAllBytes(name, data);
        }

        // read the message to figure out session data
        var ikeMessage = IkeMessage.FromBytes(data, 0);

        Log.Info($"Got a 500 packet, {data.Length} bytes, ex={ikeMessage.Exchange}");
        Log.Debug($"IKE flags {ikeMessage.MessageFlag.ToString()}, message id={ikeMessage.MessageId}, first payload={ikeMessage.FirstPayload.ToString()}");

        if (ikeMessage.Exchange == ExchangeType.IDENTITY_1) // start of an IkeV1 session
        {
            Log.Warn("    Message is for IKEv1. Not supported, not replying");
            return;
        }

        if (_sessions.ContainsKey(ikeMessage.SpiI))
        {
            var session = _sessions[ikeMessage.SpiI];
            if (session.WeStarted)
            {
                Log.Info($"    it's for an existing session we started {ikeMessage.SpiI:x16} (us) => {ikeMessage.SpiR:x16} (them)");
            }
            else
            {
                Log.Info($"    it's for an existing session started by the peer {ikeMessage.SpiI:x16} (them) => {ikeMessage.SpiR:x16} (us)");
            }

            // Pass message to existing session
            session.HandleIke(ikeMessage, sender, sendZeroHeader);
            return;
        }

        Log.Info($"    it's for a new session  {ikeMessage.SpiI:x16} => {ikeMessage.SpiR:x16}");
            
        // Start a new session and store it, keyed by the initiator id
        var newSession = new VpnSession(IpV4Address.FromEndpoint(sender), _server, this, weAreInitiator:false, ikeMessage.SpiI);
        _sessions.Add(ikeMessage.SpiI, newSession);
            
        // Pass message to new session
        newSession.HandleIke(ikeMessage, sender, sendZeroHeader); 
    }

    /// <summary>
    /// Responds to port 4500 traffic
    /// </summary>
    private void SpeResponder(byte[] data, IPEndPoint sender)
    {
        if (data.Length < 1) return; // junk message

        Log.Info($"Got a 4500 packet, {data.Length} bytes");

        // Check for keep-alive ping?
        if (data.Length < 4 && data[0] == 0xff)
        {
            Log.Info("    Looks like a keep-alive ping. Sending pong");
            _server.SendRaw(data, sender);
            return;
        }

        if (data.Length < 8)
        {
            Log.Warn($"    Malformed SPE/ESP from {sender.Address}. Not responding");
            return;
        }

        // Check for "IKE header" (prefix of 4 zero bytes)
        var idx = 0;
        var header = Bit.ReadInt32(data, ref idx); // if not zero, it's an ESP packet

        // If the IKE header is there, pass back to the ike handler.
        // We strip the padding off, and pass a flag to say it should be sent with a response
        if (header == NonEspHeader)
        {
            Log.Info("    SPI zero on 4500 -- sending to 500 (IKE) responder");
            var offsetData = data.Skip(4).ToArray();
            IkeSessionResponder(offsetData, sender, sendZeroHeader: true);
            return;
        }

        // There is no Non-ESP marker, so the first 4 bytes are the ESP "Security Parameters Index".
        // This is 32 bits, and not the 64 bits of the IKE SPI.
        // (see https://docs.strongswan.org/docs/5.9/features/natTraversal.html , https://en.wikipedia.org/wiki/Security_Parameter_Index )
        // "The SPI (as per RFC 2401) is a required part of an Ipsec Security Association (SA)"
        // https://en.wikipedia.org/wiki/IPsec has notes and diagrams of the ESP headers
        // ESP uses different encryption modes compared to IKEv2
        idx = 0;
        var spi = Bit.ReadUInt32(data, ref idx);

        if (Settings.CaptureTraffic)
        {
            File.WriteAllText(Settings.FileBase+$"ESP_{_espCount}.txt", Bit.Describe($"esp_{_espCount}", data));
            _espCount++;
        }

        // reject unknown sessions
        if (!_childSessions.ContainsKey(spi))
        {
            // TODO: check for a child session with reversed key?
            Log.Warn($"    Unknown session: 0x{spi:x8} -- not replying");
            Log.Debug("    Expected sessions=", ListKnownSpis);
            return;
        }

        // if we get here, we have a new message (with encryption) for an existing session
        var childSa = _childSessions[spi];

        try
        {
            // check, decrypt, route, etc.
            childSa.HandleSpe(data, sender);
        }
        catch (Exception ex)
        {
            Log.Warn($"Failed to handle SPE message from {sender.Address}: {ex.Message}");
            Log.Debug(ex.ToString());
        }
    }

    private IEnumerable<string> ListKnownSpis()
    {
        foreach (var key in _sessions.Keys)
        {
            yield return $"IKE {key:x16}; ";
        }

        foreach (var child in _childSessions)
        {
            yield return $"ESP {child.Key:x8} (in={child.Value.SpiIn:x8}, out={child.Value.SpiOut:x8}); ";
        }
    }

    /// <summary>
    /// This calls the "EventPump" method on all VpnSession
    /// and ChildSA objects that are currently active.
    ///
    /// This is our co-operative multi-tasking setup that
    /// doesn't require us to juggle threads.
    /// This is done with the expectation that we will
    /// usually have 1 connected gateway at a time.
    /// </summary>
    private void EventPumpLoop()
    {
        while (!_running)
        {
            Log.Debug("Event pump waiting for run flag");
            Thread.Sleep(Settings.EventPumpRate);
        }

        while (_running)
        {
            try
            {
                var goFaster = false;
                //Log.Trace("Triggering event pump");

                foreach (var session in _sessions.Values)
                {
                    try
                    {
                        session.EventPump();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Event pump failure, VPN", ex);
                    }
                }

                foreach (var childSa in _childSessions.Values)
                {
                    try
                    {
                        goFaster |= childSa.EventPump();
                    }
                    catch (Exception ex)
                    {
                        Log.Error("Event pump failure, ChildSA", ex);
                    }
                }
                
                // Wait before looping, unless any of the ChildSA are active,
                // in which case we go full speed.
                if (!goFaster) Thread.Sleep(Settings.EventPumpRate);
                
                // If there aren't any connections, run slower
                if (_childSessions.Count < 1) Thread.Sleep(Settings.EventPumpRate);
            }
            catch (Exception ex)
            {
                Log.Warn($"Outer event pump failure: {ex.Message}"); // most likely a thread conflict?
                Thread.Sleep(Settings.EventPumpRate);
            }
        }
    }
}