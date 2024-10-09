using Shared;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace Server
{
    public class BaseServer : INetworkBehaviour
    {
        public const int Port = 5252;
        public int LogLevel = 1;
        public NetworkDriver Driver;
        protected NativeList<NetworkConnection> Connections;
        
        public virtual void Init()
        {
            Driver = NetworkDriver.Create();
            NetworkEndpoint endpoint = NetworkEndpoint.AnyIpv4.WithPort(Port);
            int bindingResult = Driver.Bind(endpoint);
            if (bindingResult != 0)
            {
                Debug.LogError($"Error (code {bindingResult}) when binding port {endpoint.Port}");
            }
            else
            {
                Driver.Listen();
            }

            Connections = new NativeList<NetworkConnection>(4, Allocator.Persistent);
            
            if (LogLevel > 0)
            {
                Debug.Log("Server created");
            }
        }
        public virtual void UpdateServer()
        {
            Driver.ScheduleUpdate().Complete();
            CleanupConnections();
            AcceptNewConnections();
            UpdateMessagePump();
        }
        private void CleanupConnections()
        {
            for (int i = 0; i < Connections.Length; i++)
            {
                if (!Connections[i].IsCreated)
                {
                    if (LogLevel > 0)
                    {
                        Debug.Log($"Disconnected ({i})");
                    }
                    Connections.RemoveAtSwapBack(i);
                    i--;
                }
            }
        }
        private void AcceptNewConnections()
        {
            NetworkConnection connection;
            while ((connection = Driver.Accept()) != default)
            {
                Connections.Add(connection);
                if (LogLevel > 0)
                {
                    Debug.Log("Accepted a connection");
                }
            }
        }
        protected virtual void UpdateMessagePump()
        {
            for (int i = 0; i < Connections.Length; i++)
            {
                NetworkEvent.Type cmd;

                while ((cmd = Driver.PopEventForConnection(Connections[i], out var stream)) != NetworkEvent.Type.Empty)
                {
                    if (cmd == NetworkEvent.Type.Data)
                    {
                        Debug.Log(stream.ReadByte());
                    }
                    else if (cmd == NetworkEvent.Type.Disconnect)
                    {
                        if (LogLevel > 0)
                        {
                            Debug.Log("Client disconnected from server");
                        }
                        Connections[i] = default;
                    }
                }
            }
        }

        ~BaseServer()
        {
            if (!Connections.IsEmpty)
            {
                Connections.Dispose();
            }
        }
        public virtual void Shutdown()
        {
            for (int i = 0; i < Connections.Length; i++)
            {
                if (Connections[i].IsCreated)
                {
                    Connections[i].Disconnect(Driver);
                }
            }
            Driver.Dispose();
            Connections.Dispose();
        }
    }
}
