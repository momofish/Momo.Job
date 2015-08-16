using Microsoft.Practices.EnterpriseLibrary.Data;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data.SQLite;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Momo.Job.ConMon
{
    public class Executer
    {
        public static Dao _dao = Dao.Get("ConMon");

        public static Dictionary<string, List<Article>> Execute()
        {
            var result = new Dictionary<string, List<Article>>();

            var entries = _dao.List<Entry>(new { Enabled = true }, "select * from Entry where Enabled=@Enabled");
            foreach (var entry in entries)
            {
                var items = ExecuteEntry(entry);
                if (items.Count > 0)
                    result.Add(entry.EntryId, items);
            }

            if (result.Count == 0)
                return null;

            var mailTo = ConfigurationManager.AppSettings["conmon_mail_to"];
            var subject = string.Format("抓取信息推送[{0}]", result.SelectMany(e => e.Value).Count());
            StringBuilder body = new StringBuilder(@"<table border=1 style='border-collapse: collapse; font-size: 12pt;'>");
            foreach (var entry in result)
            {
                body.AppendFormat("<tr style='background-color:#eee;text-align:left;'><th style='text-align:left;line-height:1.5'>{0}[{1}]</th></tr>", entry.Key, entry.Value.Count);
                foreach (var article in entry.Value)
                {
                    body.AppendFormat("<tr><td><a href='{1}'>{0}</a> [{2:yyyy-MM-dd}]<div>{3}</div></td></tr>", article.Title, article.Url, article.PubTime, article.Content);
                }
            }
            body.Append("</table>");
            MessageHelper.SendMail(mailTo, subject, body.ToString());

            return result;
        }

        private static List<Article> ExecuteEntry(Entry entry)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.WorkingDirectory = @"casperjs";
            psi.FileName = System.Environment.CurrentDirectory + @"\casperjs\bin\casperjs.bat";
            psi.UseShellExecute = false;
            psi.RedirectStandardOutput = true;
            dynamic config = JsonConvert.DeserializeObject(entry.Config);
            config.latestUrl = entry.LatestUrl;
            string configText = JsonConvert.SerializeObject(config);
            psi.Arguments = "crawl.js --config='" + configText + "'";

            var articles = new List<Article>();
            try
            {
                Process p = Process.Start(psi);
                while (!p.StandardOutput.EndOfStream)
                {
                    string line = p.StandardOutput.ReadLine();
                    var article = JsonConvert.DeserializeObject<Article>(line);
                    article.EntryId = entry.EntryId;
                    article.ArticleId = Guid.NewGuid().ToString();
                    article.CreatedTime = DateTime.Now;
                    articles.Add(article);
                }
                articles.Reverse();
                if (articles.Count > 0)
                {
                    articles.ForEach(a => _dao.Insert<Article>(a));
                    entry.LatestUrl = articles.Last().Url;
                    _dao.Update<Entry>(new { EntryId = entry.EntryId, LatestUrl = entry.LatestUrl });
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Fatal: " + e.Message);
            }

            return articles;
        }
    }
}
