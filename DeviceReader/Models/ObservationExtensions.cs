using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DeviceReader.Models
{
    public static class ObservationExtensions
    {

        public static IList<Tuple<string, string>> GetRenameList( string input) {
            string[] lines = input.Replace("\r\n", "\n").Split("\n");

            
            // Get the position of the = sign within each line
            return lines.
                Where(l => l.Trim().FirstOrDefault() != '#' && l.Trim() != "" && l.Trim() != "=" && l.Contains("=")). // exclude comments
                Select(l =>
                {
                    var p = l.Split("=", 2);
                    var key = p[0].Trim();
                    string value = "";
                    if (p.Length > 1) { value = p[1].Trim(); }
                    return new Tuple<string, string>(key, value);
                }).Where(t => (
                    !(t.Item1 == "" && t.Item2 == "")  // exclude empty rows
                    )
                ).ToList();
            
        }


        /// <summary>        
        /// tag renaming logic. This could be cached if object instance
        /// existingtagname=notexistingname  - rename existing
        /// notexistingname=existingname  - add duplicate with new name
        /// existingname=empty  - remove existing
        /// notexistingname=empty - ignore
        /// existingname=anotherexistingname - copy another existingname value to existingvalue position
        /// </summary>
        /// <param name="observation"></param>
        /// <param name="renamemap"></param>
        /// <returns></returns>
        /// 
        public static Observation RenameTags(this Observation observation, IList<Tuple<string,string>> renamemap)
        {
            if (observation == null ||observation.Data == null) return null;
            if (renamemap == null) return observation;

            // current list of tag names
            var currentTagNames = observation.Data.Select(
                    o => o.TagName
                ).ToList();
            

            foreach (var item in renamemap)
            {
                // no change
                if (item.Item1 == item.Item2) continue;

                int index1 = currentTagNames.IndexOf(item.Item1);
                int index2 = currentTagNames.IndexOf(item.Item2);

                // ignore
                if (index1 < 0 && (index2 < 0 || item.Item2 == "")) continue;

                // existing tag
                if (index1 >= 0)
                {
                    // remove item
                    if (item.Item2 == "")
                    {
                        observation.Data.RemoveAt(index1);
                        currentTagNames.RemoveAt(index1);
                    } else
                    {
                        // overwrite existing observation with specified?
                        if (index2 >= 0)
                        {
                            var cp = (ObservationData)observation.Data[index2].Clone();
                            // replace
                            currentTagNames[index1] = cp.TagName;
                            observation.Data[index1] = cp;
                        } else
                        // rename item
                        {
                            currentTagNames[index1] = item.Item2;
                            observation.Data[index1].TagName = item.Item2;
                        }

                    }                    
                } else
                // currently not existing name refers to existing name
                {
                    var cp = (ObservationData)observation.Data[index2].Clone();
                    cp.TagName = item.Item1;
                    observation.Data.Add(cp);
                    currentTagNames.Add(item.Item1);
                }
            }

            return observation;
        }
    }
}
