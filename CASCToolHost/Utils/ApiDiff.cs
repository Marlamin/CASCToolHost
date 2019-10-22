using System.Collections.Generic;
using System.Linq;

namespace CASCToolHost.Utils
{
    public struct DiffEntry
    {
        public string action;
        public string filename;
        public string id;
        public string type;
        public string md5;
    }

    public struct ApiDiff
    {
        public IEnumerable<DiffEntry> added;
        public IEnumerable<DiffEntry> removed;
        public IEnumerable<DiffEntry> modified;

        public IEnumerable<DiffEntry> all
        {
            get
            {
                return added.Concat(removed.Concat(modified));
            }
        }
    }
}