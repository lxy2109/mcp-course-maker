using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssemblySystem
{
    public class Port : MonoBehaviour
    {
        public Device master;
        public Connection connection;
        public int index;
        public string description;
        public bool isInPort;

        public bool connected
        {
            get
            {
                return connection != null;
            }
        }
        public void Connect(Connection connection, int index)
        {
            if(connection[index] != null)
            {
                connection.Disconnect(index);
            }
            connection[index] = this;
            this.index = index;
            this.connection = connection;
        }
        public void Disconnect()
        {
            if(connection == null)
            {
                return;
            }
            else
            {
                connection[index] = null;
                this.connection = null;
                this.index = -1;
            }
        }

        public Port targetPort
        {
            get
            {
                return GetTargetPort();
            }
        }
        public Port GetTargetPort()
        {
            if (connected)
            {
                return connection[index + 1];
            }
            else
            {
                return null;
            }
        }
        public float PushEnergy(float value,List<Device> routingRecord)
        {
            if (!isInPort)
            {
                if (targetPort != null)
                {
                    return targetPort.TakeEnergy(value,routingRecord);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }
        public float TakeEnergy(float value, List<Device> routingRecord)
        {
            if (routingRecord.Contains(master))
            {
                Debug.Log("Circle Routing Error");
                return 0;
            }

            if (isInPort)
            {
                if (master != null)
                {
                    return master.TakeEnergy(value,routingRecord, description);
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
            
        }

        public Vector3 position
        {
            get
            {
                return this.transform.position;
            }
        }
        public Vector3 up
        {
            get
            {
                return this.transform.up;
            }
        }
        public Quaternion rotation
        {
            get
            {
                return this.transform.rotation;
            }
        }
    }

}
