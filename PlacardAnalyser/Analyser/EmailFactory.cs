using System.Net;
using System.Net.Mail;
using System.Net.Mime;
using PlacardAnalyser.Configuration;

// TODO
// Select events conditions
// Combinations 

namespace PlacardAnalyser.Analyser
{
    public class EmailFactory
    {
        private SmtpClient client;
        private MailMessage message;

        public EmailFactory(EmailSetts email)
        {
            this.client = new SmtpClient(email.Smtp)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(email.Username, email.Password),
                EnableSsl = true
            };

            this.message = new MailMessage
            {
                From = new MailAddress(email.From)
            };
            this.message.To.Add(email.To);
            this.message.Subject = email.Subject;
            this.message.IsBodyHtml = email.HtmlBody;
        }

        public void AddBet(IBet bet)
        {
            this.message.Body += RenderHtmlBet(bet);
        }

        public void SendEmail(string attachCSVFile = null)
        {
            this.message.Body = RenderHtml(this.message.Body);
            if (attachCSVFile != null)
                this.message.Attachments.Add(new Attachment(attachCSVFile, MediaTypeNames.Text.Plain));
            this.client.Send(this.message);
        }

        private string RenderHtml(string message)
        {
            return string.Format(htmlTemplate,message);
        }
        private string RenderHtmlBet(IBet bet)
        {
            string eventsRender = string.Empty;
            foreach (var eventBet in bet.GetBetEvents())
            {
                eventsRender += RenderHtmlEvent(eventBet);
            }
            return string.Format(htmlBetTemplate,
                bet is SingleBet 
                    ? "Single Bet"
                    : bet is CombineBet
                        ? "Combined Bet"
                        : bet is MultipleBet
                            ? "Multiple Bet"
                            : "Aposta", 
                eventsRender,
                bet.CalcFinalPrice(),
                bet.CalcFinalReturn(),
                bet.CalcTotalReturn(),
                bet.CalcBetProbability(),
                bet.CalcGainRatio(),
                bet is MultipleBet 
                    ? RenderHtmlPartialBet((MultipleBet) bet)
                    : string.Empty);
        }

        private string RenderHtmlPartialBet(MultipleBet bet)
        {
            string result = string.Empty;
            foreach (var partialBet in bet.GetPartialBet())
            {
                result += string.Format(htmlLinesPartialBetTemplate,
                    partialBet.Key, partialBet.Value, partialBet.Value - bet.CalcFinalPrice());
            }
            return string.Format(htmlPartialBetsTemplate, bet.GetCombinationTypeString(), result);            
        }

        private string RenderHtmlEvent(Event eventBet)
        {
            return string.Format(htmlEventTemplate,
                eventBet.Index,
                eventBet.Description,
                eventBet.EventDateTime,
                eventBet.Label,
                eventBet.Price,
                eventBet.Odd,
                eventBet.Probability,
                eventBet.NotProbability,
                eventBet.Hours2Start());
        }

        private readonly string htmlTemplate = 
        @"<!DOCTYPE html>
        <html>
            <head></head>
            <body>
                <h1>Best bets for today</h1>
                {0}
            </body>
        </html>";

        private readonly string htmlEventTemplate =
        @"<div style='background-color:silver'>
            <p><b>Index: </b>{0}</p>
            <p><b>Description: </b>{1}</p>
            <p><b>EventDateTime: </b>{2}</p>
            <p><b>Label: </b>{3}</p>
            <p><b>Price: </b>{4}</p>
            <p><b>Odd: </b>{5}</p>
            <p><b>Probability: </b>{6}</p>
            <p><b>NotProbability: </b>{7}</p>
            <p><b>Hours2Start: </b>{8}</p>
        </div>";

        private readonly string htmlBetTemplate =
        @"<div style='background-color:grey'>
            <h2>{0}</h2>
            {1}
            <p><b>Final Price: </b>{2}</p>
            <p><b>Final return: </b>{3}</p>
            <p><b>Total return: </b>{4}</p>
            <p><b>Bet Probability: </b>{5}</p>
            <p><b>Gain Ratio: </b>{6}</p>
            {7}
        </div>";

        private readonly string htmlPartialBetsTemplate =
        @"<p><b>Combination type: </b>{0}</p>
        <div style='background-color:white'>
            <table>
                <tr>
                    <th>Loses</th>
                    <th>Return</th>
                    <th>Gain</th>
                </tr>
                {1}
            </table
        </div>";

        private readonly string htmlLinesPartialBetTemplate = 
        @"<tr>
            <td>{0}</td>
            <td>{1}</td>
            <td>{2}</td>
        </tr>";
    }
}
