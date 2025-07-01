using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AssemblySystem
{
    public class Device : MonoBehaviour
    {
        virtual public float TakeEnergy(float value, List<Device> routingRecord , string description = "") {
            return 0;

        }//return how much energy has been taken
        
        protected List<Device> GetRoutingRecordWithThis(List<Device> routingRecord, bool createNew = false)
        {
            if (createNew)
            {
                List<Device> result = new List<Device>(routingRecord);
                result.Add(this);
                return result;
            }
            else
            {
                routingRecord.Add(this);
                return routingRecord;

            }
        }
    }
}
