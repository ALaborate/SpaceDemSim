using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class Server : MonoBehaviour
{
    public Text lastReceivedDisplayText;
    int port = SnitchOfNetwork.port;
    public const string logFileName = "SessionLog.txt";

    Thread listenThread;

    string lastReceivedText;
    DateTime lastReceivedTime;
    DateTime lastUpdateTextTime;
    string originalText;
    Vector3 originalPos = Vector3.negativeInfinity;
    string pdp;
    // Start is called before the first frame update
    void Start()
    {
        originalText = lastReceivedDisplayText?.text ?? string.Empty;
        pdp = Application.persistentDataPath;
    }

    // Update is called once per frame
    bool eventFlag = false;
    void Update()
    {
        if (lastReceivedDisplayText.isActiveAndEnabled)
        {
            if (!Input.GetMouseButton(0))
            {
                originalPos = lastReceivedDisplayText.transform.position;
            }

            if (lastReceivedTime > lastUpdateTextTime)
            {
                lastReceivedDisplayText.text = lastReceivedText;
                lastUpdateTextTime = DateTime.Now;
            }

            if (lastReceivedDisplayText.transform.position.y - originalPos.y < -.1f)
            {
                if (!eventFlag)
                {
                    eventFlag = true;
                    if (listenThread == null)
                    {
                        Run();
                    }
                    else
                    {
                        Dispose();
                    }
                }
            }
            else
            {
                eventFlag = false;
            }
        }
    }

    private void OnDisable()
    {
        Dispose();
    }
    private void Dispose()
    {
        if (listenThread != null)
        {
            listenThread.Abort();
            listenThread.Join();
            listenThread = null;
        }
        if (listenSocket != null)
        {
            listenSocket.Shutdown(SocketShutdown.Both);
            listenSocket.Close();
            listenSocket = null;
        }
        lastReceivedDisplayText.text = originalText;
    }

    Socket listenSocket;
    void Run()
    {
        listenThread = new Thread(Listen);
        listenThread.Start();
    }
    void Listen()
    {
        try
        {
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Any, port);
            Socket listenSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listenSocket.Bind(ipPoint);
            listenSocket.Listen(10);
            lastReceivedText = $"Listening on port {port}...";
            lastReceivedTime = DateTime.Now;
            while (true)
            {
                Socket handler = listenSocket.Accept();
                Debug.Log("Receiving from: " + handler.RemoteEndPoint);
                StringBuilder builder = new StringBuilder();
                int bytes = 0;
                byte[] data = new byte[2048];

                do
                {
                    bytes = handler.Receive(data);
                    builder.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (handler.Available > 0);

                lastReceivedText = builder.ToString();
                lastReceivedTime = DateTime.Now;

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

                try
                {
                    File.AppendAllText(Path.Combine(pdp, logFileName), lastReceivedText);
                }
                catch (IOException e) { Debug.LogError(e.Message); }
            }
        }
        catch (System.Exception ex)
        {
            if (!(ex is ThreadAbortException))
            {
                Debug.LogError(ex.Message);
            }
        }
    }
}
