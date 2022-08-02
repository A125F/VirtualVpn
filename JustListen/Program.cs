﻿// See https://aka.ms/new-console-template for more information

using System.Net;
using System.Net.Sockets;
using System.Text;

Console.WriteLine("I will listen for UDP messages on ports 500 and 4500 and list them...");


var commsThreadIke = new Thread(IkeLoop){IsBackground = true};
var commsThreadSpe = new Thread(SpeLoop){IsBackground = true};
commsThreadIke.Start();
commsThreadSpe.Start();

while (true)
{
    Console.WriteLine("Listening...");
    Thread.Sleep(5000);
    Console.WriteLine("...Listening");
    Thread.Sleep(5000);
}


void IkeLoop()
{
    var localEp = new IPEndPoint(IPAddress.Any, 500);
    using var client = new UdpClient(localEp);

    Console.WriteLine("Listen on 500...");

    var sender = new IPEndPoint(IPAddress.Any, 0);

    while(true)
    {
        var buffer = client.Receive(ref sender);

        Console.WriteLine($"Expected=500 Real Port={sender.Port}  Caller={sender.Address} Data={Encoding.UTF8.GetString(buffer, 0, buffer.Length)}");
        Console.Write("Echoing back to sender...");
        
        var returnToSender = new IPEndPoint(sender.Address, 500);
        var sent = client.Send(buffer, buffer.Length, returnToSender);
        Console.WriteLine($"Done ({sent} bytes).");
    }
}
    
void SpeLoop()
{
    var localEp = new IPEndPoint(IPAddress.Any, 4500);
    using var client = new UdpClient(localEp);

    Console.WriteLine("Listen on 4500...");

    var sender = new IPEndPoint(IPAddress.Any, 0);

    while(true)
    {
        var buffer = client.Receive(ref sender);

        Console.WriteLine($"Expected=4500 Real Port={sender.Port}  Caller={sender.Address} Data={Encoding.UTF8.GetString(buffer, 0, buffer.Length)}");
        Console.Write("Echoing back to sender...");
        
        var returnToSender = new IPEndPoint(sender.Address, 4500);
        var sent = client.Send(buffer, buffer.Length, returnToSender);
        Console.WriteLine($"Done ({sent} bytes).");
    }
}