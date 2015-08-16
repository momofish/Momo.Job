using Common.Logging;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Momo.Job
{
    public class InvokeJob : IJob
    {
        public void Execute(IJobExecutionContext context)
        {
            ILog log = LogManager.GetLogger(context.JobDetail.Key.Name);

            var typeName = context.JobDetail.JobDataMap.GetString("typeName");
            string methodName = context.JobDetail.JobDataMap.GetString("methodName");
            var reportToOnFail = context.JobDetail.JobDataMap.GetString("reportToOnFail");
            var reportToOnGain = context.JobDetail.JobDataMap.GetString("reportToOnGain");

            var type = Type.GetType(typeName);
            var methodInfo = type.GetMethod(methodName ?? "Execute", BindingFlags.Static|BindingFlags.Public);

            string result = null;
            try
            {
                result = JsonConvert.SerializeObject(methodInfo.Invoke(null, new object[] { }));
                if (!string.IsNullOrEmpty(reportToOnGain) && result != "null")
                {
                    var subject = string.Format("Job[{0}]执行结果", context.JobDetail.Key);
                    var body = string.Format(@"
<table border='1' style='font-size: 10pt; border-collapse: collapse; border-spacing: 0;'>
    <tr><th>typeName</th><td>{1}</td></tr>
    <tr><th>result</th><td><pre>{2}</pre></td></tr>
</table>
", context.JobDetail.Key, typeName, result);
                    MessageHelper.SendMail(reportToOnGain, subject, body);
                }
            }
            catch (Exception ex)
            {
                if (!string.IsNullOrEmpty(reportToOnFail))
                {
                    var subject = string.Format("Job[{0}]执行失败", context.JobDetail.Key);
                    var body = string.Format(@"
<table border='1' style='font-size: 10pt; border-collapse: collapse; border-spacing: 0;'>
    <tr><th>typeName</th><td>{1}</td></tr>
    <tr><th>response</th><td><pre>{2}</pre></td></tr>
</table>
", context.JobDetail.Key, typeName, ex);
                    MessageHelper.SendMail(reportToOnFail, subject, body);
                }
                throw;
            }
            log.InfoFormat("Job[{0}]执行结果:{1}", context.JobDetail.Key, result);
        }
    }
}
