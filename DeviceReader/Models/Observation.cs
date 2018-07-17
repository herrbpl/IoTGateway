using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Models
{
    public class ObservationLocation
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }
        public string Srs { get; set; }
    }

    public class ObservationData
    {
        public DateTime Timestamp { get; set; }
        public string TagName { get; set; }
        public dynamic Value { get; set; }
        public string Source { get; set; }
        public string Measure { get; set; }
        public string Code { get; set; }
        public double Height { get; set; }
        public string Unit { get; set; }
        public double QualityLevel { get; set; }
        public double QualityValue { get; set; }
        public string StatName { get; set; }
        public string StatPeriod { get; set; }        
    }

    public class Observation
    {
        public string DeviceId { get; set; }
        public ObservationLocation GeoPositionPoint { get; set; }
        public List<ObservationData> Data { get; set; }
    }
}
