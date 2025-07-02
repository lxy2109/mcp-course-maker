using System;
using System.Collections.Generic;

namespace ModelParameterLib.Data
{
    [Serializable]
    public class AIPlacementLog
    {
        public List<AIPlacementData> items;
        public string request;
        public string aiResult;
        public string error;
    }
} 