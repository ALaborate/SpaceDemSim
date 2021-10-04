using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Firebase.Storage;
using UnityEngine;
using JC = Newtonsoft.Json.JsonConvert;

public class SnitchOfNetwork //: IDisposable 
//snitch here is a sysnonimous to 'informant' or 'insider', or 'canary' (if you prefer spy slang)
{
    public const int port = 62476;
    public const string configFileName = "address.ini";
    public const string pendingLogsFileName = "pendingLogs.txt";
    public const string defaultAddress = "192.168.1.5";
    // public const int port = 80;// 61476;

    public static IPAddress targetAddress = IPAddress.Parse(defaultAddress);
    private static IPEndPoint endPoint = new IPEndPoint(targetAddress, port);
    private Socket socket;

    public Session session;

    public SnitchOfNetwork()
    {
        Start();
    }

    private void Start()
    {
        session = new Session();
        RenewAddress();
        Firebase.Auth.FirebaseAuth auth = Firebase.Auth.FirebaseAuth.DefaultInstance;
        auth.SignInAnonymouslyAsync().ContinueWith(task =>
        {
            if (task.IsCanceled)
            {
                Debug.LogError("SignInAnonymouslyAsync was canceled.");
                return;
            }
            if (task.IsFaulted)
            {
                Debug.LogError("SignInAnonymouslyAsync encountered an error: " + task.Exception);
                return;
            }

            user = task.Result;
            Debug.LogFormat("User signed in successfully: {0} ({1})",
                user.DisplayName, user.UserId);

        });
    }
    Firebase.Auth.FirebaseUser user;
    public void SendAndDispose()
    {
        session.endTimeUTC = DateTime.UtcNow;
        session.providerName = PlayerData.instance.runtime.rotationProvider.gameObject.name;

        var pendingLogsPath = Path.Combine(Application.persistentDataPath, pendingLogsFileName);
        var report = JC.SerializeObject(session, Newtonsoft.Json.Formatting.Indented) + ",\n";
        var pendingLogs = string.Empty;
        try
        {
            pendingLogs = File.ReadAllText(pendingLogsPath);
        }
        catch { }
        var reportBytes = Encoding.UTF8.GetBytes("[" + pendingLogs + report + "]");
        // SendDirect(pendingLogsPath, report, reportBytes);
        SendFb(pendingLogsPath, report, reportBytes);
    }
    private void SendFb(string pendingLogsPath, string report, byte[] whatToSend)
    {
        var di = FirebaseStorage.DefaultInstance;
        var uid = user == null ? SystemInfo.deviceUniqueIdentifier : user.UserId;
        var sref = di.RootReference.Child(uid).Child(DateTime.UtcNow.ToString("HHmmssddMyyyy") + "_report.json");
        sref.PutBytesAsync(whatToSend).ContinueWith(task =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                if (File.Exists(pendingLogsPath))
                {
                    var fi = new FileInfo(pendingLogsPath);
                    if (fi.Length > 50_000_000L) //50 Mb
                    {
                        File.Delete(pendingLogsPath);
                    }
                }
                File.AppendAllText(pendingLogsPath, report);
                Debug.Log("Send failure, storing locally.\n" + task.Exception.ToString());
            }
            else
            {
                Debug.Log("Sent text");
                File.WriteAllText(pendingLogsPath, string.Empty);
            }
        });
    }
    private void SendDirect(string pendingLogsPath, string report, byte[] whatToSend)
    {
        var controlThread = new Thread(() =>
        {
            RenewAddress();
            bool success = false;
            var sendingThread = new Thread(() =>
            {
                try
                {
                    socket.Connect(endPoint);
                    socket.Send(whatToSend);
                    success = true;
                }
                catch { }
                finally
                {
                    if (socket.IsBound)
                        socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                }
            });
            sendingThread.Start();
            Thread.Sleep(12000);
            if (!success)
            {
                sendingThread.Abort();
                sendingThread.Join();
                File.AppendAllText(pendingLogsPath, report);
            }
            else
            {
                File.WriteAllText(pendingLogsPath, "\n");
            }
        });
        controlThread.Start();
    }
    private void RenewAddress()
    {
        try
        {
            var path = Path.Combine(Application.persistentDataPath, configFileName);
            if (!File.Exists(path))
            {
                File.WriteAllText(path, defaultAddress);
            }
            else
            {
                var address = File.ReadAllText(path);
                if (IPAddress.TryParse(address, out IPAddress ip))
                {
                    targetAddress = ip;
                    endPoint = new IPEndPoint(targetAddress, port);
                }
                else
                {
                    var he = Dns.GetHostEntry(address);
                    if (he.AddressList.Length > 0)
                    {
                        targetAddress = he.AddressList[0];
                        endPoint = new IPEndPoint(targetAddress, port);
                    }
                }
            }
        }
        catch (SocketException)
        {
            ;
        }
        catch (System.Exception e)
        {
            Debug.Log(e.Message);
        }
        socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
    }

    [System.Serializable]
    public class Test
    {
        public Test(float distanceToTarget, float targetViewportPosX, float targetViewportPosY,
        float targetVelocityProjectionX, float targetVelocityProjectionY, DateTime startTimeUTC)
        {
            this.creationDuration = creationDuration;
            this.distanceToTarget = distanceToTarget;
            this.targetViewportPosX = targetViewportPosX;
            this.targetViewportPosY = targetViewportPosY;
            this.targetVelocityProjectionX = targetVelocityProjectionX;
            this.targetVelocityProjectionY = targetVelocityProjectionY;
            this.startTimeUTC = startTimeUTC;

            crossTimes = new List<DateTime>();
            uncrossTimes = new List<DateTime>();
        }

        //initial conditions
        public TimeSpan creationDuration { get; set; }
        public float distanceToTarget { get; set; }
        public float targetViewportPosX { get; set; }
        public float targetViewportPosY { get; set; }
        public float targetVelocityProjectionX { get; set; }
        public float targetVelocityProjectionY { get; set; }

        // results
        public DateTime startTimeUTC { get; set; }
        public DateTime userActionStartTimeUTC { get; set; }
        public List<DateTime> crossTimes { get; set; }
        public List<DateTime> uncrossTimes { get; set; }
        public DateTime succesTimeUTC { get; set; }

    }
    [System.Serializable]
    public class Session
    {
        public string deviceId { get; set; }
        public string appVersion { get; set; }
        public string providerName { get; set; }
        public PlayerData.TutorialProgress tutorialProgress { get; set; }
        public DateTime startTimeUTC { get; set; }
        public DateTime endTimeUTC { get; set; }

        public List<Test> tests { get; set; }

        public Session()
        {
            deviceId = SystemInfo.deviceUniqueIdentifier;
            tutorialProgress = PlayerData.instance.tutorialProgress;
            startTimeUTC = DateTime.UtcNow;
            appVersion = Application.version;
            tests = new List<Test>();
        }
    }
}
