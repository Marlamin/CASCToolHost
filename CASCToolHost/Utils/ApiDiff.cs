using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace CASCToolHost.Utils
{
    public struct DiffEntry
    {
        public string action;
        public string filename;
        public string id;
        public string content_hash;
        public string type;
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