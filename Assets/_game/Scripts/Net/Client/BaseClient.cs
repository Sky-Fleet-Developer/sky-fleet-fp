using Shared;
using Unity.Collections;
using Unity.Networking.Transport;
using UnityEngine;

namespace Client
{
    public class BaseClient : INetworkBehaviour
    {
        public const int Port = 5252;
        public int LogLevel = 1;
        public NetworkDriver Driver;
        protected NetworkConnection Connection;
        
        public virtual void Init()
        {
            Driver = NetworkDriver.Create();
            NetworkEndpoint endpoint = NetworkEndpoint.LoopbackIpv4.WithPort(Port);
            Connection = Driver.Connect(endpoint);
        }
        public virtual void UpdateServer()
        {
            Driver.ScheduleUpdate().Complete();
            CheckAlive();
            UpdateMessagePump();
        }
        private void CheckAlive()
        {
            if (!Connection.IsCreated)
            {
                Debug.LogError("Lost connection to server");
            }
        }

        protected virtual void UpdateMessagePump()
        {
            NetworkEvent.Type cmd;
            while ((cmd = Connection.PopEvent(Driver, out var stream)) != NetworkEvent.Type.Empty)
            {
                if (cmd == NetworkEvent.Type.Connect)
                {
                    if (LogLevel > 0)
                    {
                        Debug.Log("Connected to server");
                    }
                }
                if (cmd == NetworkEvent.Type.Data)
                {
                    Debug.Log(stream.ReadByte());
                }
                else if (cmd == NetworkEvent.Type.Disconnect)
                {
                    if (LogLevel > 0)
                    {
                        Debug.Log("Disconnected from server");
                    }

                    Connection = default;
                }
            }
        }

        public virtual void Shutdown()
        {
            if (Connection.IsCreated)
            {
                Connection.Disconnect(Driver);
            }

            Driver.Dispose();
        }
    }
}
