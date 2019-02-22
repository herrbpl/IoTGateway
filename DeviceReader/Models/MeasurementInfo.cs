using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace DeviceReader.Models
{    
    public class MeasurementMetadataRecord
    {
        public string Name { get; set; }
        public Type Type { get; set; }
        public DateTime? LastUpdated { get; set; }
        public long Count { get; set; } = 0;
        public MeasurementMetadataRecord Clone() => (MeasurementMetadataRecord)MemberwiseClone();       
    }

    public class MeasurementMetadata: IEnumerable<MeasurementMetadataRecord>
    {
        // should we keep this readonly? Ie, 
        protected Dictionary<string, MeasurementMetadataRecord> _measurements = new Dictionary<string, MeasurementMetadataRecord>();

        public object this[string name]
        {
            get
            {
                if (_measurements.ContainsKey(name))
                {
                    return _measurements[name].Clone();
                }
                return null;
            }
            set
            {                
                if (_measurements.ContainsKey(name))
                {
                                        
                    var r = _measurements[name];
                    var t = GetValueType(value);
                    if (r.Type !=  t && t != null)
                    {
                        // Do something? Type has changed
                        r.Count = 1;
                    }
                    else
                    {
                        r.Count++;                        
                    }

                    if (t != null) r.Type = t;

                    r.LastUpdated = DateTime.Now;
                    

                }
                else
                {
                    
                    var r = new MeasurementMetadataRecord()
                    {
                        Name = name,
                        Type = GetValueType(value),
                        LastUpdated = DateTime.Now,
                        Count = 1
                    };
                    _measurements.Add(name, r);
                    
                }
            }
        }
        
        private Type GetValueType(object value)
        {
            if (value == null) return null;
            return value.GetType();
        }

        public void Unset(string name)
        {
            if (_measurements.ContainsKey(name))
            {

                _measurements.Remove(name);
            }
        }

        public IEnumerator<MeasurementMetadataRecord> GetEnumerator()
        {
            return new MeasurementMetadataRecordEnumerator(_measurements);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }

    internal class MeasurementMetadataRecordEnumerator: IEnumerator<MeasurementMetadataRecord>
    {
        IEnumerator<MeasurementMetadataRecord> _enumerator;
        public MeasurementMetadataRecordEnumerator(Dictionary<string, MeasurementMetadataRecord> datadict)
        {
            if (datadict == null) throw new ArgumentNullException();
            _enumerator = datadict.Values.GetEnumerator();            
        }
        public MeasurementMetadataRecord Current
        {
            get
            {
                return _enumerator.Current.Clone();
            }
        }

        object IEnumerator.Current => this.Current;

        public void Dispose()
        {            
        }

        public bool MoveNext()
        {
            return _enumerator.MoveNext();
        }

        public void Reset()
        {
            _enumerator.Reset();
        }

       


    }

}
