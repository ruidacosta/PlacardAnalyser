using System;
using System.Net;
using System.Net.Mail;
using log4net.Appender;
using log4net.Core;

namespace PlacardAnalyser
{
    public class SmtpHtmlAppender : AppenderSkeleton
    {
        public string To { get; set; }
        public string From { get; set; }
        public string Subject { get; set; }
        public string SmtpHost { get; set; }
        public int Port { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public bool HtmlFormat { get; set; }

        protected override void Append(LoggingEvent loggingEvent)
        {
            string message = string.Empty;
            if (HtmlFormat)
            {
                message = RenderHTML(loggingEvent);
            }
            else
            {
                message = string.Format(
                    "Domain: {1}{0}Identity: {2}{0}LoggetName: {3}{0}MessageObject: {4}{0}RenderedMessage: {5}{0}ThreadName: {6}{0}" +
                    "Username: {7}{0}ExceptionObject: {}{0}Fix: {}{0}Level: {}{0}LoacationInformation: {}{0}Properties: {}{0}" +
                    "Repository: {}{0}Timestamp: {}{0}TimeStampUtc: {}{0}",
                    Environment.NewLine,
                    loggingEvent.Domain,
                    loggingEvent.Identity,
                    loggingEvent.LoggerName,
                    loggingEvent.MessageObject,
                    loggingEvent.RenderedMessage,
                    loggingEvent.ThreadName,
                    loggingEvent.UserName,
                    loggingEvent.ExceptionObject,
                    loggingEvent.Fix,
                    loggingEvent.Level,
                    loggingEvent.LocationInformation,
                    loggingEvent.Properties,
                    loggingEvent.Repository,
                    loggingEvent.TimeStamp,
                    loggingEvent.TimeStampUtc);

            }

            SendEmail(message);
        }

        void SendEmail(string message)
        {
            SmtpClient client = new SmtpClient(SmtpHost)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(Username, Password),
                EnableSsl = true
            };

            MailMessage mailMessage = new MailMessage
            {
                From = new MailAddress(From)
            };
            mailMessage.To.Add(To);
            mailMessage.Body = message;
            mailMessage.Subject = Subject;
            mailMessage.IsBodyHtml = HtmlFormat;
            client.Send(mailMessage);
        }

        string RenderHTML(LoggingEvent loggingEvent)
        {
            return string.Format(htmlTemplate,
                                 loggingEvent.TimeStamp, loggingEvent.MessageObject,loggingEvent.ExceptionObject);
        }


        readonly string htmlTemplate =
            @"<!DOCTYPE html>
            <html>
                <head></head>
                <body>
                    <h1>Error Report</h1>
                    <p><strong>Time:</strong> {0}</p>
                    <p><span style='background-color: #ff0000; color: #ffff00;'><strong>Error: </strong>{1}</span></p>
                    <hr/>
                    <p>{2}</p>
                </body>
            </html>";
    }
}
