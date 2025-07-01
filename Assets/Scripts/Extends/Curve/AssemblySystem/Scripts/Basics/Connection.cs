using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssemblySystem
{
    public class Connection : MonoBehaviour
    {
        public Port[] ports = new Port[2];
        public Port this[int index]
        {
            get
            {
                if(index < 0)
                {
                    index = -index;
                }
                index = index % 2;
                return ports[index];
            }
            set
            {
                if (index < 0)
                {
                    index = -index;
                }
                index = index % 2;
                ports[index] = value;
            }
        }
        
        public void Connect(int index, Port port)
        {
            if(this[index] != null)
            {
                Disconnect(index);
            }
            this[index] = port;
            this[index].index = index;
            this[index].connection = this;
        }
        public void Disconnect(int index)
        {
            if(this[index] == null)
            {
                return;
            }
            Port port = this[index];
            port.connection = null;
            this[index] = null;
        }
    }

}
