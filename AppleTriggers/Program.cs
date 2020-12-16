using System;
using System.Security.Cryptography.X509Certificates;
using System.Net.Http;
using System.Security.Authentication;
using System.Timers;
using System.Collections.Generic;
using System.Net;
using System.IO;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

namespace PushNet3
{
    public class Program
    {

        private static System.Timers.Timer aTimer, bTimer;
        private static int listCount = 0;
        private static int itrCount = 0;
        private static List<PushData> pushData;
        private static TimeSpan TimerStart, TimerEnd;
        
        static void Main(string[] args)
        {
            //get the data from the db
            
            GetTriggerData();
            //DataEntry();
            SetTimer();
            ////get the first entry
            if (pushData.Count > 0)
            {
                //get the time difference  
                double diffSeconds = (pushData[0].Times - DateTime.Now.AddHours(11).TimeOfDay).TotalSeconds;
                Console.WriteLine("First seconds diff {0}", diffSeconds);
                SecondTimer(diffSeconds * 1000);
            }

            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now.AddHours(11));
            Console.ReadLine();
            aTimer.Stop();
            aTimer.Dispose();

            //Console.WriteLine("Terminating the application...");
            //foreach (PushData item in pushData) {
            //    Console.WriteLine(item.PushToken);
            //    Console.WriteLine(item.Times.ToString());
            //}
        }

        public static void GetTriggerData() {
            RESTClient rClient = new RESTClient();

            //Call a function to set the URL, or do it here itself.
            
            string currentTime = string.Format("{0:HH:mm:ss}", DateTime.Now.AddHours(11));
            char[] delimiters = { ' ', ':' };
            string[] timeBreak = currentTime.Split(delimiters);

            int lastMinuteCheck = Convert.ToInt32(timeBreak[1]);
            int hourCheck = Convert.ToInt32(timeBreak[0]);

            string correctedEndTime = "";
            if (lastMinuteCheck > 55)
            {
                correctedEndTime = (hourCheck + 1).ToString();
            }
            else {
                correctedEndTime = timeBreak[0];
            }

            //need to set proper times here

            string minTime = String.Format("{0}%3A{1}%3A00", timeBreak[0], timeBreak[1]); //ex: 01:00:00
            string maxTime = String.Format("{0}%3A59%3A00", correctedEndTime); //ex: 01:59:00

            string strTimeMin = String.Format("{0}:{1}:00", timeBreak[0], timeBreak[1]);
            string strTimeMax = String.Format("{0}:59:00", correctedEndTime);

            //setting up to timer sake
            TimerStart = TimeSpan.Parse(strTimeMin);
            TimerEnd = TimeSpan.Parse(strTimeMax);

            string URLendPoint = String.Format("https://brainchangeruatapi.azurewebsites.net/triggerdata/{0}/{1}", minTime, maxTime);

            rClient.endPoint = URLendPoint;
            //"https://brainchangeruatapi.azurewebsites.net/triggerdata/11%3A00%3A00/11%3A59%3A59";//"this is the url to call";
            string strJSON = string.Empty;
            strJSON = rClient.makeRequest();
            var modelList = JsonConvert.DeserializeObject<List<PushData>>(strJSON);
            pushData = new List<PushData>();
            pushData = modelList;
            listCount = pushData.Count;
            Console.WriteLine("No of items in list {0}", listCount);
            foreach (PushData item in pushData) {
                Console.WriteLine(item.Times);
            }
        }

        //the below function will be called every time the timer resets.
        //public static void DataEntry() {
            //Console.WriteLine("Setting Data");
            //pushData = new List<PushData>();
            
            //for (int i = 3; i < 7; i++) {
            //    PushData indData = new PushData();
            //    DateTime temp = DateTime.Now;//now plus 1 + 2 + 3
            //    indData.Time = temp.AddMinutes(i);
            //    indData.PushToken = "hithe12thpushBCtoken";
            //    pushData.Add(indData);
            //}
            //listCount = pushData.Count;

            //foreach (PushData item in pushData)
            //{
            //    Console.WriteLine(item.Time);
            //}
        //}

        public static void SetTimer() {
            //get the seconds needed
            double timerSpan = ((TimerEnd - TimerStart).TotalSeconds) * 1000;
            Console.WriteLine("Setting aTimer to {0}", timerSpan);
            aTimer = new System.Timers.Timer(timerSpan);//Now this will be set to 1 hr --  it is set to 10 mins now.
            aTimer.Elapsed += OnTimedEvent;
            aTimer.AutoReset = false;
            aTimer.Enabled = true;
        }

        private static void OnTimedEvent(object sender, ElapsedEventArgs e)
        {
            aTimer.Stop();
            aTimer.Dispose();
            //bTimer.Stop();
            //bTimer.Dispose();
            //DataEntry();
            Console.WriteLine("Stopped and Disposed aTimer");
            GetTriggerData();
            Console.WriteLine("List refreshed");
            itrCount = 0;
            SetTimer();

            if (pushData.Count > 0)
            {
                //get the time difference  
                double diffSeconds = (pushData[0].Times - DateTime.Now.AddHours(11).TimeOfDay).TotalSeconds;
                Console.WriteLine("First seconds diff {0}", diffSeconds);
                SecondTimer(diffSeconds * 1000);
            }
        }

        public static void SecondTimer(double seconds) {
            bTimer = new System.Timers.Timer(seconds);//Now this will be set to 1 hr
            bTimer.Elapsed += OnSecondTimedEvent;
            bTimer.AutoReset = false;
            bTimer.Enabled = true;//only after this will we get to the elapsed event. Guess!
        }

        private static void OnSecondTimedEvent(object sender, ElapsedEventArgs e)
        {
            //function to send notification called here
            SendNotification(pushData[itrCount].PushToken);
            Console.WriteLine("Notification sent at {0:HH:mm:ss}", DateTime.Now.AddHours(11));
            bTimer.Stop();//sanity sake
            bTimer.Dispose();
            Console.WriteLine("bTimer stopped and disposed");
            itrCount++;
            
            if (itrCount < listCount) {
                double diffSecondsII = (pushData[itrCount].Times - DateTime.Now.AddHours(11).TimeOfDay).TotalSeconds;
                Console.WriteLine("Subsequent seconds diff {0}", diffSecondsII);
                SecondTimer(diffSecondsII*1000);
            }
            
        }

        /// <summary>
        /// Code snippet to send notifications from webjob.
        /// </summary>
        public static void SendNotification(string PushToken)
        {
            Console.WriteLine("Sending Notification to {0}", PushToken);

            var url = string.Format("https://api.push.apple.com/3/device/{0}", PushToken);//[deviceToken]//"fddf54151c3f1a03872f66d61db12779c010f9d19bc01963a3c152033abc35d3"
            var certData = System.IO.File.ReadAllBytes("Certificates.p12");
            var certificate = new X509Certificate2(certData, "Clearz14/11", X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.Exportable);
            var topic = "pass.app.brainchanger.io"; // App bundle Id
            using var httpHandler = new HttpClientHandler { SslProtocols = SslProtocols.Tls12 };
            httpHandler.ClientCertificates.Add(certificate);

            using var httpClient = new HttpClient(httpHandler, true);
            using var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("apns-id", Guid.NewGuid().ToString("D"));
            request.Headers.Add("apns-push-type", "alert");
            //request.Headers.Add("apns-priority", "10");
            request.Headers.Add("apns-topic", topic);

            var payload = new Newtonsoft.Json.Linq.JObject{
                        {
                            "aps", new Newtonsoft.Json.Linq.JObject
                            {
                            }
                        }
                    };

            request.Content = new StringContent(payload.ToString());
            request.Version = new Version(2, 0);
            try
            {
                using var httpResponseMessage = httpClient.SendAsync(request).GetAwaiter().GetResult();
                var responseContent = httpResponseMessage.Content.ReadAsStringAsync();
                //return $"status: {(int)httpResponseMessage.StatusCode} content: {responseContent}";
                Console.WriteLine((int)httpResponseMessage.StatusCode);
                Console.WriteLine(responseContent);
            }
            catch (Exception e)
            {
                //this._logger.LogError("send push notification error", e);
                Console.WriteLine("send push notification error::: " + e.Message);
                throw;
            }
        }
    }

    public class PushData {
        public TimeSpan Times {get;set;}
        public string PushToken { get; set; }
    }

    public enum httpVerb { 
        GET,
        POST,
        PUT,
        DELETE
    }
    class RESTClient { 
        public string endPoint { get; set; }
        public httpVerb httpMethod { get; set; }

        public RESTClient() {
            endPoint = "";
            httpMethod = httpVerb.GET;
        }
        public string makeRequest() {
            string strResponseValue = string.Empty;
            var request = (HttpWebRequest)WebRequest.Create(endPoint);
            request.Method = httpMethod.ToString();
            HttpWebResponse response = null;
            try
            {
                response = (HttpWebResponse)request.GetResponse();
                using (Stream responseStream = response.GetResponseStream())
                {
                    if (responseStream != null)
                    {
                        using (StreamReader reader = new StreamReader(responseStream))
                        {
                            strResponseValue = reader.ReadToEnd();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message.ToString());
            }
            finally {
                if (response != null) { 
                    ((IDisposable)response).Dispose();
                }
            }
            return strResponseValue;
        }
    }
}
