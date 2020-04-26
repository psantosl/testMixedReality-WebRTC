// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.MixedReality.WebRTC;

namespace TestNetCoreConsole
{
    class Program
    {
        static async Task Main(string[] args)
        {
            DataChannel dataChannel = null;

            try
            {
                // Create a new peer connection automatically disposed at the end of the program
                using var pc = new PeerConnection();

                // Initialize the connection with a STUN server to allow remote access
                var config = new PeerConnectionConfiguration
                {
                    IceServers = new List<IceServer> {
                            new IceServer{ Urls = { "stun:stun.l.google.com:19302" } }
                        }
                };
                await pc.InitializeAsync(config);
                Console.WriteLine("Peer connection initialized.");

                Console.WriteLine("Opening data channel");
                dataChannel = await pc.AddDataChannelAsync("data", true, true);

                // Setup signaling
                Console.WriteLine("Starting signaling...");
                var signaler = new NamedPipeSignaler.NamedPipeSignaler(pc, "testpipe");
                signaler.SdpMessageReceived += (string type, string sdp) => {
                    pc.SetRemoteDescription(type, sdp);
                    if (type == "offer")
                    {
                        pc.CreateAnswer();
                    }
                };
                signaler.IceCandidateReceived += (string sdpMid, int sdpMlineindex, string candidate) => {
                    pc.AddIceCandidate(sdpMid, sdpMlineindex, candidate);
                };
                await signaler.StartAsync();

                // Start peer connection
                pc.Connected += () => { Console.WriteLine("PeerConnection: connected."); };
                pc.IceStateChanged += (IceConnectionState newState) => { Console.WriteLine($"ICE state: {newState}"); };

                pc.DataChannelAdded += (DataChannel c) =>
                {
                    Console.WriteLine("DataChannel added");

                    c.MessageReceived += (byte[] _msg) =>
                    {
                        Console.WriteLine("received {0} bytes", _msg.Length);
                    };
                };

                if (signaler.IsClient)
                {
                    Console.WriteLine("Connecting to remote peer...");
                    pc.CreateOffer();
                }
                else
                {
                    Console.WriteLine("Waiting for offer from remote peer...");
                }

                Console.WriteLine("Press a 'S' to send data. 'Esc' to exit ...");

                ConsoleKeyInfo key;

                while ( (key = Console.ReadKey(true) ).Key != ConsoleKey.Escape)
                {
                    if (key.Key == ConsoleKey.S)
                    {
                        Console.WriteLine("Sending data");
                        dataChannel.SendMessage(new byte[3000]);
                    }
                }
                signaler.Stop();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            Console.WriteLine("Program termined.");
        }

        private static void Pc_DataChannelAdded(DataChannel channel)
        {
            throw new NotImplementedException();
        }
    }
}