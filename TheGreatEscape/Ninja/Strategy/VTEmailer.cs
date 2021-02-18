#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;

// For sending screen dumps on email
using System.Windows.Forms;
using System.Net.Mail;
using System.Net.Mime;
using System.Drawing.Imaging;
#endregion

namespace valutatrader
{
    public class VTEmailer
    {
        private string emailSmtp = "smtp.online.no";
        private string emailFrom = "steinar@currencytrader.no";
        private string emailTo = "sre@fjordbyen-invest.no";
        StrategyBase strategy = null;

        public VTEmailer(StrategyBase _strategy, string smtp, string from, string to)
        {
            strategy = _strategy;
            emailSmtp = smtp;
            emailFrom = from;
            emailTo = to;
        }

        public void SendMailChart(ChartControl chart, string Subject, string Body)
        {

            if (chart.ParentForm.WindowState == FormWindowState.Minimized)
            {
               // SendMail(From, To, "SendMailChart error", "Chart form window state must be maximized or normal to send. Email not sent.");
                return;
            }
            try
            {

                // get bitmap of chart panel
                System.Drawing.Bitmap bmp = new System.Drawing.Bitmap(chart.ChartPanel.Width, chart.ChartPanel.Height, PixelFormat.Format16bppRgb555);
                chart.ChartPanel.DrawToBitmap(bmp, chart.ChartPanel.ClientRectangle);

                // save to stream (as jpg to reduce size)
                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                bmp.Save(stream, ImageFormat.Jpeg);
                stream.Position = 0;

                // create mail
                MailMessage theMail = new MailMessage(emailFrom, emailTo, Subject, Body);
                System.Net.Mail.Attachment attachment = new System.Net.Mail.Attachment(stream, "image.jpg");
                theMail.Attachments.Add(attachment);

                // create the smtp client using your outgoing server, outgoing port, and credentials (see Outlook account advanced settings)
                // and http://www.davidmillington.net/news/index.php/2005/12/18/using_smtpclient_credentials
                SmtpClient smtp = new SmtpClient(emailSmtp, 25);

                smtp.Credentials = new System.Net.NetworkCredential("", "");
                string token = strategy.Instrument.MasterInstrument.Name + strategy.ToDay(strategy.Time[0]) + " " + strategy.ToTime(strategy.Time[0]) + strategy.CurrentBar.ToString();

                smtp.SendAsync(theMail, token);
                // handle failed sends if you like by adding a callback (see http://msdn.microsoft.com/en-us/library/system.net.mail.smtpclient.aspx)


            }
            catch (Exception ex)
            {

               // Print(" -------- SendMailChart Exception: " + ex);
                //SendMail(From, To, "SendMailChart Exception", "SendMailChart Exception");
            }
        }		
    }
}
