using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace DeviceReader.Abstractions
{   
    public enum MergeConflicAction
    {
        KeepFirst = 0,
        KeepSecond = 1,
        Throw = 99
    }
    public class MergeOptions
    {
        public static  MergeOptions DefaultMergeOptions =  new MergeOptions 
        {
            MergeConflicAction = MergeConflicAction.KeepFirst,
            PrefixFirst = "",
            PrefixSecond = ""
        };
        public MergeConflicAction MergeConflicAction { get; set; } = MergeConflicAction.KeepFirst;
        public string PrefixFirst { get; set; } = "";
        public string PrefixSecond { get; set; } = "";  
    }

  

    public interface IMergeable<T>
    {        
        /// <summary>
        /// Merges current instance with mergeWith.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="mergeWith"></param>
        /// <param name="mergeOptions"></param>
        /// <returns>Merged T</returns>
        T Merge(T mergeWith, MergeOptions mergeOptions = default(MergeOptions)); 
    }


    [Serializable]
    internal class MergeConflictException : Exception
    {
        public MergeConflictException()
        {
        }

        public MergeConflictException(string message) : base(message)
        {
        }

        public MergeConflictException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected MergeConflictException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

}
