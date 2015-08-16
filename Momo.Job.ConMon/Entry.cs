using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Momo.Job.ConMon
{
    public class Entry
    {
        public string EntryId { get; set; }
        public string Config { get; set; }
        public bool Enabled { get; set; }
        public string LatestUrl { get; set; }
    }
}
