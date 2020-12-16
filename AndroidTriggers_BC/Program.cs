using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Timers;
using WebPush;

namespace BC_AndroidTriggers
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
            Console.WriteLine("Hello World!");

            //things to do below
            //need to borrow from two sources
            //1 Apple Trigger webjob
            //2 From the API
            //lets do it.

            GetTriggerData();
            //DataEntry();
            SetTimer();
            ////get the first entry
            if (pushData.Count > 0)
            {
                //get the time difference  
                double diffSeconds = (pushData[0].Times - DateTime.Now.AddHours(10).TimeOfDay).TotalSeconds;
                Console.WriteLine("First seconds diff {0}", diffSeconds);
                SecondTimer(diffSeconds * 1000);
            }

            Console.WriteLine("\nPress the Enter key to exit the application...\n");
            Console.WriteLine("The application started at {0:HH:mm:ss.fff}", DateTime.Now.AddHours(10));
            Console.ReadLine();
            aTimer.Stop();
            aTimer.Dispose();

        }

        public static void SetTimer()
        {
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
                double diffSeconds = (pushData[0].Times - DateTime.Now.AddHours(10).TimeOfDay).TotalSeconds;
                Console.WriteLine("First seconds diff {0}", diffSeconds);
                SecondTimer(diffSeconds * 1000);
            }
        }

        public static void SecondTimer(double seconds)
        {
            bTimer = new System.Timers.Timer(seconds);//Now this will be set to 1 hr
            bTimer.Elapsed += OnSecondTimedEvent;
            bTimer.AutoReset = false;
            bTimer.Enabled = true;//only after this will we get to the elapsed event. Guess!
        }

        private static void OnSecondTimedEvent(object sender, ElapsedEventArgs e)
        {
            //function to send notification called here
            if (pushData[itrCount].Uri != null ||
                pushData[itrCount].Uri != "" ||
                pushData[itrCount].Auth != null ||
                pushData[itrCount].Auth != "" ||
                pushData[itrCount].p256dh != null ||
                pushData[itrCount].p256dh != "") {
                SendNotification(pushData[itrCount].Uri, pushData[itrCount].Auth, pushData[itrCount].p256dh);
            }
            Console.WriteLine("Notification sent at {0:HH:mm:ss}", DateTime.Now.AddHours(10));
            bTimer.Stop();//sanity sake
            bTimer.Dispose();
            Console.WriteLine("bTimer stopped and disposed");
            itrCount++;

            if (itrCount < listCount)
            {
                double diffSecondsII = (pushData[itrCount].Times - DateTime.Now.AddHours(10).TimeOfDay).TotalSeconds;
                Console.WriteLine("Subsequent seconds diff {0}", diffSecondsII);
                if (diffSecondsII < 0)
                {
                    SecondTimer(1 * 1000);
                    //SendNotification(pushData[itrCount].Uri, pushData[itrCount].Auth, pushData[itrCount].p256dh);
                }
                else {
                    SecondTimer(diffSecondsII * 1000);
                }
            }

        }

        public static void SendNotification(string endpoint, string auth, string p256dh) {
            
                var client = new WebPushClient();

                //setting up the data needed to send notifications - for PushAPI
                VapidDetails vapid = new VapidDetails
                {
                    PrivateKey = "76F3YkvkUMTbo_9gdjGHy79rgvDNidLI6sSObTaRUGE",
                    PublicKey = "BB9k2GIlUDKyb9ooe7sGaiCpFdHDdDgDY8gIObSobC6rjTqJ2fOWfmsS2hu5aHCfwP4H2JOBMmCBeq4cKyzDu9M",
                    Subject = "mailto:example@yourdomain.org"
                };

                PushSubscription subObj = new PushSubscription
                {
                    Endpoint = endpoint,//"https://fcm.googleapis.com/fcm/send/d_ENBF2nvtA:APA91bHKEalk9eKes4wo0f8fdlTiUqj6VEHxFZtVW6GPqtAhAqm4WC1qT52xUVUIZpJ8Ld8UeS7P0JloQ4p8iV2Fet6p_gOErirJO2v9ZUJXkQvxVfQ7cVqgepkLGRHilQBkFbK9IAkv",
                    Auth = auth,//"8o6_HBAqyezLNYVTZKuCTA",
                    P256DH = p256dh//"BPsv6DgO3Y9nS0a0g6f7tcdcVDlcOCOJG8SJWwFFAywBRShHUEM0kdzwiZHaNNUm5tf-r7-1f3rxxI0KGNxd5Pc"
                };

                //Now setting up the notification object which will be displayed - for NotificationsAPI
                //(Hard code the values for now, we just need to achieve sending a notification from the API)

                Conversion notifyObject = new Conversion();
                Notification notifyData = new Notification
                {
                    Title = "Brain Changer",
                    Body = "Time to score",
                    Icon = "assets/icons/icon-144x144.png"
                };

                notifyObject.Notification = notifyData;

                var serializedMessage = JsonConvert.SerializeObject(notifyObject);

            //ignore the foreach for now
            //foreach (var pushSubscription in Subscriptions)
            //{
            try
            {
                client.SendNotification(subObj, serializedMessage, vapid);
            }
            catch (Exception ex) {
                Console.WriteLine(ex.Message);
            }
                
            
        }

        public static void GetTriggerData()
        {
            RESTClient rClient = new RESTClient();

            //Call a function to set the URL, or do it here itself.

            string currentTime = string.Format("{0:HH:mm:ss}", DateTime.Now.AddHours(10));
            char[] delimiters = { ' ', ':' };
            string[] timeBreak = currentTime.Split(delimiters);

            int lastMinuteCheck = Convert.ToInt32(timeBreak[1]);
            int hourCheck = Convert.ToInt32(timeBreak[0]);

            string correctedEndTime = "";
            if (lastMinuteCheck > 55)
            {
                correctedEndTime = (hourCheck + 1).ToString();
            }
            else
            {
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

            string URLendPoint = String.Format("https://brainchangeruatapi.azurewebsites.net/androidnotifications/{0}/{1}", minTime, maxTime);

            rClient.endPoint = URLendPoint;
            //"https://brainchangerapi.azurewebsites.net/androidnotifications/11%3A00%3A00/11%3A59%3A59";//"this is the url to call";
            string strJSON = string.Empty;
            strJSON = rClient.makeRequest();
            var modelList = JsonConvert.DeserializeObject<List<PushData>>(strJSON);
            pushData = new List<PushData>();
            pushData = modelList;
            listCount = pushData.Count;
            Console.WriteLine("No of items in list {0}", listCount);
            foreach (PushData item in pushData)
            {
                Console.WriteLine(item.Times);
            }
        }
    }

    public class PushData
    {
        public TimeSpan Times { get; set; }
        public string Uri { get; set; }
        public string Auth { get; set; }
        public string p256dh { get; set; }
    }

    public enum httpVerb
    {
        GET,
        POST,
        PUT,
        DELETE
    }

    public partial class Conversion
    {
        [JsonProperty("notification")]
        public Notification Notification { get; set; }
    }
    public partial class Notification
    {
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("icon")]
        public string Icon { get; set; }

        //[JsonProperty("vibrate")]
        //public long[] Vibrate { get; set; }

        //[JsonProperty("data")]
        //public Data Data { get; set; }

        //[JsonProperty("actions")]
        //public Action[] Actions { get; set; }
    }

    public partial class Action
    {
        [JsonProperty("action")]
        public string ActionAction { get; set; }

        [JsonProperty("title")]
        public string Title { get; set; }
    }

    public partial class Data
    {
        [JsonProperty("dateOfArrival")]
        public string DateOfArrival { get; set; }

        [JsonProperty("primaryKey")]
        public long PrimaryKey { get; set; }
    }
    class RESTClient
    {
        public string endPoint { get; set; }
        public httpVerb httpMethod { get; set; }

        public RESTClient()
        {
            endPoint = "";
            httpMethod = httpVerb.GET;
        }
        public string makeRequest()
        {
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
            finally
            {
                if (response != null)
                {
                    ((IDisposable)response).Dispose();
                }
            }
            return strResponseValue;
        }
    }
}
