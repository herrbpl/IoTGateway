using System;
using System.Collections.Generic;
using System.Text;

namespace DeviceReader.Models
{
    public static class MeasurementInfoExtensions
    {
        public static void Measure(this ObservationData observationData, MeasurementMetadata measurement)
        {
            if (measurement != null && observationData != null) measurement[observationData.TagName] = observationData.Value;
        }

        public static void Measure(this Observation observation, MeasurementMetadata measurement)
        {
            if (measurement != null && observation != null)
            {
                foreach (var item in observation.Data)
                {
                    if (item != null)
                    {
                        measurement[item.TagName] = item.Value;
                    }
                }
            }                
        }

    }
}
