using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Momo.Job.ConMon
{
    public class Article
    {
        public string ArticleId { get; set; }
        public string EntryId { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public DateTime PubTime { get; set; }
        public string Content { get; set; }
        public string ContentText { get; set; }
        public DateTime CreatedTime { get; set; }
    }
}
