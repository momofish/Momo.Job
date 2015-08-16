using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace Momo.Job
{
    public class MessageHelper
    {
        public static void SendMail(string to, string subject, string body)
        {
            var host = ConfigurationManager.AppSettings["report_mail_host"];
            var account = ConfigurationManager.AppSettings["report_mail_account"];
            var password = ConfigurationManager.AppSettings["report_mail_password"];
            var from = ConfigurationManager.AppSettings["report_mail_from"];
            SmtpClient smtpClient = new SmtpClient();
            smtpClient.DeliveryMethod = SmtpDeliveryMethod.Network;
            smtpClient.Host = host;
            smtpClient.Credentials = new System.Net.NetworkCredential(account, password);

            MailMessage mailMessage = new MailMessage(from, to);
            mailMessage.Subject = subject;
            mailMessage.Body = body;
            mailMessage.BodyEncoding = System.Text.Encoding.UTF8;
            mailMessage.IsBodyHtml = true;
            mailMessage.Priority = MailPriority.High;

            try
            {
                smtpClient.Send(mailMessage);
            }
            catch
            {
                throw;
            }
        }
    }
}
