using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO.Ports;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Reflection;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Net.NetworkInformation;

//시리얼 모니터링 하는 쓰레드 추가->기존 클라이언트 모니터링에 끼워넣음
//처음 켤때 한번만 시리얼을 오픈해서 상대방 혹은 샌딩 에러로 시리얼이 끊어지는 경우 리딩 쓰레드만 끝내고 재 접속 및 리셋을 하지 않아서 동작되지 않는 문제 해결
namespace CMLib
{
    public class DLL
    {
        [DllImport("kernel32.dll")] //Read CFG File
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder result, int size, string path);

        [DllImport("kernel32.dll")] //Write CFG File
        private static extern int WritePrivateProfileString(string section, string key, string val, string path);

        public class Param
        {
            public static string path = System.Environment.CurrentDirectory + "\\config.cfg";
            //public static string path = "C:\\cusv\\config\\EOIRIS-NEXCOMS.cfg";
        }

        public class API
        {        
            public static void Begin()
            {
                Parameter.CommInfo.NetworkPath = Param.path;

                GetPrivateProfileString("COMMUNICATION", "SERVER", "", Parameter.CommInfo.sbServer_Amount, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                GetPrivateProfileString("COMMUNICATION", "CLIENT", "", Parameter.CommInfo.sbClient_Amount, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                GetPrivateProfileString("COMMUNICATION", "SERIAL", "", Parameter.CommInfo.sbSerial_Amount, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());

                GetPrivateProfileString("COMMUNICATION", "CONNECT", "", Parameter.CommInfo.CommAuto, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());

                Parameter.CommInfo.iServer_Amount = int.Parse(Parameter.CommInfo.sbServer_Amount.ToString());
                Parameter.CommInfo.iClient_Amount = int.Parse(Parameter.CommInfo.sbClient_Amount.ToString());
                Parameter.CommInfo.iSerial_Amount = int.Parse(Parameter.CommInfo.sbSerial_Amount.ToString());

                if (Parameter.CommInfo.iServer_Amount > 0)
                {
                    for (int i = 0; i < Parameter.CommInfo.iServer_Amount; i++)
                    {
                        StringBuilder temp = new StringBuilder(20);

                        GetPrivateProfileString(Parameter.CommInfo.server[i], "NAME", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.Server_NAME[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.server[i], "IP", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.Server_IP[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.server[i], "PORT", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.Server_PORT[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.server[i], "SCLIENT_IP", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.Server_CLIENT_IP[i] = temp.ToString();

                        if (Parameter.CommInfo.CommAuto.ToString() == "AUTO")
                        {
                            Parameter.Socket.Server.Listener[i] = new TcpListener(IPAddress.Parse(Parameter.CommInfo.Server_IP[i].ToString()), int.Parse(Parameter.CommInfo.Server_PORT[i].ToString()));
                            Parameter.Socket.Server.Listener[i].Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            Parameter.Socket.Server.Listener[i].Server.NoDelay = true;

                            try
                            {
                                Parameter.Socket.Server.Listener[i].Start();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Fail to Open Server Socket / Please Check NIC Status OR SW IP Address");
                                Parameter.CommInfo.bCantOpenServer[i] = true;
                                
                            }

                            
                            switch (i)
                            {
                                case 0: { Parameter.Socket.Server.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Server1)); } break;
                                case 1: { Parameter.Socket.Server.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Server2)); } break;
                                case 2: { Parameter.Socket.Server.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Server3)); } break;
                                case 3: { Parameter.Socket.Server.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Server4)); } break;
                                case 4: { Parameter.Socket.Server.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Server5)); } break;
                                case 5: { Parameter.Socket.Server.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Server6)); } break;
                            }

                          //  Parameter.Socket.Server.pingSender[i] = new Ping();
                          //  Parameter.Socket.Server.pingCnt[i] = 0;
                            if (Parameter.CommInfo.bCantOpenServer[i] == false)
                            Parameter.Socket.Server.ReadThread[i].Start();
                        }
                    }
                }

                if (Parameter.CommInfo.iClient_Amount > 0)
                {
                    for (int i = 0; i < Parameter.CommInfo.iClient_Amount; i++)
                    {
                        StringBuilder temp = new StringBuilder(20);

                        GetPrivateProfileString(Parameter.CommInfo.client[i], "NAME", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.Client_NAME[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.client[i], "IP", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.Client_IP[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.client[i], "PORT", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.Client_PORT[i] = temp.ToString();

                        if (Parameter.CommInfo.CommAuto.ToString() == "AUTO")
                        {
                            switch(i)
                            {
                                case 0: { Parameter.Socket.Client.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Client1)); } break;
                                case 1: { Parameter.Socket.Client.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Client2)); } break;
                                case 2: { Parameter.Socket.Client.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Client3)); } break;
                                case 3: { Parameter.Socket.Client.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Client4)); } break;
                                case 4: { Parameter.Socket.Client.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Client5)); } break;
                                case 5: { Parameter.Socket.Client.ReadThread[i] = new Thread(new ThreadStart(Internal_Method.Read_Client6)); } break;
                            }
                            Parameter.Socket.Client.ReadThread[i].Start();
                        }
                    }
                }

                if (Parameter.CommInfo.iSerial_Amount > 0)
                {
                    for (int i = 0; i < Parameter.CommInfo.iSerial_Amount; i++)
                    {
                        StringBuilder temp = new StringBuilder(20);

                        GetPrivateProfileString(Parameter.CommInfo.serial[i], "NAME", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.Serial_NAME[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.serial[i], "PORT", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.Serial_PORT[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.serial[i], "BAUDRATE", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.Serial_BR[i] = temp.ToString();

                        Parameter.Serial.Connector[i] = new SerialPort();
                    }
                }
                if (Parameter.CommInfo.CommAuto.ToString() == "AUTO")
                {
                    Parameter.Socket.Client.MonitoringThread = new Thread(new ThreadStart(Internal_Method.Monitoring_Client));
                    Parameter.Socket.Client.MonitoringThread.Start();                  
                }
                
            }

            public static void End()
            {
                Parameter.Socket.Client.MonitoringThread.Abort();

                for (int i = 0; i < Parameter.CommInfo.iClient_Amount; i++)
                {
                    Internal_Method.Close_Client(i);                    
                }

                for (int i = 0; i < Parameter.CommInfo.iServer_Amount; i++)
                {
                    Internal_Method.Close_Server(i);
                    Internal_Method.Close_SClient(i);
                }

                for (int i = 0; i < Parameter.CommInfo.iSerial_Amount; i++)
                {
                    Internal_Method.Close_Serial(i);
                }
            }

            public static void Set_CFG_Path(string path)
            {
                Parameter.CommInfo.NetworkPath = path;
            }

            public static void Send_SClient(string name, byte[] data, int len)
            {
                int no = 10;

                for (int i = 0; i < Parameter.CommInfo.iServer_Amount; i++)
                {
                    if (name == Parameter.CommInfo.Server_NAME[i])
                    {
                        no = i;
                        break;
                    }
                }

                if (no != 10)
                {
                    if (Parameter.Socket.SClient.occupy[no] == true)
                    {
                        try
                        {
                            Parameter.Socket.SClient.Stream[no].Write(data, 0, len);
                        }
                        catch (Exception ex)
                        {
                            Internal_Method.Close_SClient(no);
                        }                       
                    }
                }
            }

            public static void Send_Client(string name, byte[] data, int len)
            {
                int no = 10;

                for (int i = 0; i < Parameter.CommInfo.iClient_Amount; i++)
                {
                    if (name == Parameter.CommInfo.Client_NAME[i])
                    {
                        no = i;
                        break;
                    }
                }

                if (no != 10)
                {
                    if (Parameter.Socket.Client.occupy[no] == true)
                    {
                        try
                        {
                            Parameter.Socket.Client.Stream[no].Write(data, 0, len);
                        }
                        catch (Exception ex)
                        {
                            Internal_Method.Close_Client(no);
                        }
                    }
                }
            }

            public static void Send_Serial(string name, byte[] data, int len)
            {
                int no = 10;

                for (int i = 0; i < Parameter.CommInfo.iSerial_Amount; i++)
                {
                    if (name == Parameter.CommInfo.Serial_NAME[i])
                    {
                        no = i;
                        break;
                    }
                }

                if (no != 10)
                {
                    if (Parameter.Serial.occupy[no] == true)
                    {
                        try
                        {
                            Parameter.Serial.Connector[no].Write(data, 0, len);
                        }
                        catch (Exception ex)
                        {
                            Internal_Method.Close_Serial(no);
                            Console.WriteLine("Serial[{0}] has Closed", no);
                        }
                    }
                }
                /*
                switch (name)
                {
                    case "FCC":
                        {
                            if (Parameter.Serial.occupy[0] == true)
                            {
                                try
                                {
                                    Parameter.Serial.Connector[0].Write(data, 0, len);
                                }
                                catch (Exception ex)
                                {
                                    Internal_Method.Close_Serial(0);
                                }
                            }
                        }
                        break;
                    

                    case "SCB":
                        {
                            if (Parameter.Serial.occupy[0] == true)
                            {
                                try
                                {
                                    Parameter.Serial.Connector[0].Write(data, 0, len);
                                }
                                catch (Exception ex)
                                {
                                    Internal_Method.Close_Serial(0);
                                }
                            }
                        }
                        break;

                    case "CCB":
                        {
                            if (Parameter.Serial.occupy[1] == true)
                            {
                                try
                                {
                                    Parameter.Serial.Connector[1].Write(data, 0, len);
                                }
                                catch (Exception ex)
                                {
                                    Internal_Method.Close_Serial(1);
                                }
                            }
                        }
                        break;
                }*/
                /*
                int no = 10;

                for (int i = 0; i < Parameter.CommInfo.iSerial_Amount; i++)
                {
                    if (name == Parameter.CommInfo.Serial_NAME[i])
                    {
                        no = i;
                        break;
                    }
                }

                if (no != 10)
                {
                    if (Parameter.Serial.occupy[no] == true)
                    {
                        try
                        {
                            Parameter.Serial.Connector[no].Write(data, 0, len);
                        }
                        catch (Exception ex)
                        {
                            Internal_Method.Close_Serial(no);
                        }                        
                    }
                }*/
            }            

            public static void Close_Client(int no)
            {
                if (Parameter.Socket.Client.occupy[no] == true)
                {
                    Console.WriteLine(string.Format("Client {0} Disconnected", Parameter.CommInfo.Client_NAME[no]));
                    Parameter.Socket.Client.Connector[0].Client.EndConnect(Parameter.Socket.Client.ka_sync.ar[no]);
                    Parameter.Socket.Client.bThreadIsStop[no] = true;
                    Parameter.Socket.Client.Stream[no].Close();
                    Parameter.Socket.Client.Connector[no].Close();
                }

                Parameter.Socket.Client.occupy[no] = false;
                Parameter.Socket.Client.bClosing[no] = false;  
                /*
                if (Parameter.Socket.Client.occupy[no] == true)
                {
                    Console.WriteLine(string.Format("Client {0} Disconnected", Parameter.CommInfo.Client_NAME[no]));
                    Parameter.Socket.Client.Connector[0].Client.EndConnect(Parameter.Socket.Client.ka_sync.ar[no]);
                    Parameter.Socket.Client.occupy[no] = false;
                    Parameter.Socket.Client.Stream[no].Close();
                    Parameter.Socket.Client.Connector[no].Close();
                    Parameter.Socket.Client.bClosing[no] = false;
                    Parameter.Socket.Client.ReadThread[no].Abort();
                }*/
            }

            public static void Close_SClient(int no)
            {
                if (Parameter.Socket.SClient.occupy[no] == true)
                {
                    Console.WriteLine(string.Format("SClient {0} Disconnected", Parameter.CommInfo.Server_NAME[no]));
                    Parameter.Socket.SClient.occupy[no] = false;
                    Parameter.Socket.SClient.Stream[no].Close();
                    Parameter.Socket.SClient.Connector[no].Close();
                    Parameter.Socket.SClient.ReadThread[no].Abort();
                }
            }

            /*
            public static string Get_Serial_Info()
            {
                string list = string.Format("{0},{1},{2},{3},{4},{5},{6}", Parameter.CommInfo.sbSerial_Amount.ToString(),
                    Parameter.CommInfo.Serial_NAME[0], Parameter.CommInfo.Serial_NAME[1], Parameter.CommInfo.Serial_NAME[2],
                    Parameter.CommInfo.Serial_NAME[3], Parameter.CommInfo.Serial_NAME[4], Parameter.CommInfo.Serial_NAME[5]);

                return list;
            }

            public static string Get_Client_Info()
            {
                string list = string.Format("{0},{1},{2},{3},{4},{5},{6}", Parameter.CommInfo.sbClient_Amount.ToString(),
                    Parameter.CommInfo.Client_NAME[0], Parameter.CommInfo.Client_NAME[1], Parameter.CommInfo.Client_NAME[2],
                    Parameter.CommInfo.Client_NAME[3], Parameter.CommInfo.Client_NAME[4], Parameter.CommInfo.Client_NAME[5]);

                return list;
            }

            public static bool Get_Client_Connection_Status(string name)
            {
                bool tf = false;

                for (int i = 0; i < Parameter.CommInfo.iClient_Amount; i++)
                {
                    if (Parameter.CommInfo.Client_NAME[i] == name)
                    {
                        
                    }
                }

                return true;
            }
            */
        }

        public class Internal_Method
        {
            public class CircularQueue
            {
                public int front = 0;
                public int rear = 0;
                public int Q_Size = 400;

                public temp[] byteq;

                public struct temp
                {
                    public byte[] buf;
                    public int length;
                }

                public CircularQueue(int len)
                {
                    byteq = new temp[Q_Size];

                    for (int i = 0; i < Q_Size; i++)
                    {
                        byteq[i].buf = new byte[len];
                    }
                    Console.WriteLine("Initialization Done");
                }

                public bool IsEmpty()
                {
                    return front == rear ? true : false;
                }

                public bool IsFull()
                {
                    return (rear + 1) % Q_Size == front ? true : false;
                }

                public void Enqueue(byte[] data, int len)
                {
                    if (IsFull())
                    {
                        Console.WriteLine("Q is Full");
                    }
                    else
                    {
                        Array.Copy(data, 0, byteq[rear].buf, 0, len);
                        byteq[rear].length = len;
                        rear = (++rear) % Q_Size;

                        //byteq[rear].buf = data;
                    }
                }

                public byte[] Dequeue()
                {
                    int preindex;

                    if (IsEmpty())
                    {
                        Console.WriteLine("Q is Empty");
                        return null;
                    }
                    else
                    {

                        preindex = front;
                        front = (++front) % Q_Size;
                        //지연 측정 시험 시
                        // if (CMA_For.forlog.bDelay == true) front = (++front) % Q_Size;


                    }

                    return byteq[preindex].buf;
                }

                public int GetLength()
                {
                    int preindex;

                    preindex = front;
                    // front = (++front) % Q_Size;
                    //무결성 시험 시
                    //if (CMA_For.forlog.bIntegrity == true) front = (++front) % Q_Size;

                    return byteq[preindex].length;
                }
            }

            #region Server  
            public static void Read_Server1()
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    if (Parameter.Socket.Server.isStop[0] == false)
                    {
                        if (Parameter.Socket.SClient.occupy[0] == false)
                        {
                            Parameter.Socket.Server.tempClient[0] = Parameter.Socket.Server.Listener[0].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.Socket.Server.tempClient[0].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.Server_CLIENT_IP[0].ToString())
                            {
                                var sKA1 = new Parameter.Socket.Server.ka();
                                sKA1.onoff = 1;
                                sKA1.keepalivetime = 1000;
                                sKA1.keepaliveinterval = 500;

                                Parameter.Socket.SClient.occupy[0] = true;
                                Parameter.Socket.SClient.Connector[0] = Parameter.Socket.Server.tempClient[0];
                                Parameter.Socket.SClient.Stream[0] = Parameter.Socket.SClient.Connector[0].GetStream();
                                Parameter.Socket.SClient.Connector[0].Client.IOControl(IOControlCode.KeepAliveValues, sKA1.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.Socket.SClient.Connector[0].Client.RemoteEndPoint, Parameter.CommInfo.Server_NAME[0].ToString()));

                                Parameter.Socket.SClient.ReadThread[0] = new Thread(new ThreadStart(Read_SClient1));
                                Parameter.Socket.SClient.ReadThread[0].Start();
                            }
                            else
                            {
                                Parameter.Socket.Server.tempClient[0].Close();
                            }
                        }
                    }
                }
            }

            public static void Read_Server2()
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    if (Parameter.Socket.Server.isStop[1] == false)
                    {
                        if (Parameter.Socket.SClient.occupy[1] == false)
                        {
                            Parameter.Socket.Server.tempClient[1] = Parameter.Socket.Server.Listener[1].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.Socket.Server.tempClient[1].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.Server_CLIENT_IP[1].ToString())
                            {
                                var sKA2 = new Parameter.Socket.Server.ka();
                                sKA2.onoff = 1;
                                sKA2.keepalivetime = 1000;
                                sKA2.keepaliveinterval = 500;

                                Parameter.Socket.SClient.occupy[1] = true;
                                Parameter.Socket.SClient.Connector[1] = Parameter.Socket.Server.tempClient[1];
                                Parameter.Socket.SClient.Stream[1] = Parameter.Socket.SClient.Connector[1].GetStream();
                                Parameter.Socket.SClient.Connector[1].Client.IOControl(IOControlCode.KeepAliveValues, sKA2.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.Socket.SClient.Connector[1].Client.RemoteEndPoint, Parameter.CommInfo.Server_NAME[1].ToString()));

                                Parameter.Socket.SClient.ReadThread[1] = new Thread(new ThreadStart(Read_SClient2));
                                Parameter.Socket.SClient.ReadThread[1].Start();
                            }
                            else
                            {
                                Parameter.Socket.Server.tempClient[1].Close();
                            }
                        }
                    }
                }
            }

            public static void Read_Server3()
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    if (Parameter.Socket.Server.isStop[2] == false)
                    {
                        if (Parameter.Socket.SClient.occupy[2] == false)
                        {
                            Parameter.Socket.Server.tempClient[2] = Parameter.Socket.Server.Listener[2].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.Socket.Server.tempClient[2].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.Server_CLIENT_IP[2].ToString())
                            {
                                var sKA3 = new Parameter.Socket.Server.ka();
                                sKA3.onoff = 1;
                                sKA3.keepalivetime = 1000;
                                sKA3.keepaliveinterval = 500;

                                Parameter.Socket.SClient.occupy[2] = true;
                                Parameter.Socket.SClient.Connector[2] = Parameter.Socket.Server.tempClient[2];
                                Parameter.Socket.SClient.Stream[2] = Parameter.Socket.SClient.Connector[2].GetStream();
                                Parameter.Socket.SClient.Connector[1].Client.IOControl(IOControlCode.KeepAliveValues, sKA3.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.Socket.SClient.Connector[2].Client.RemoteEndPoint, Parameter.CommInfo.Server_NAME[2].ToString()));

                                Parameter.Socket.SClient.ReadThread[2] = new Thread(new ThreadStart(Read_SClient3));
                                Parameter.Socket.SClient.ReadThread[2].Start();
                            }
                            else
                            {
                                Parameter.Socket.Server.tempClient[2].Close();
                            }
                        }
                    }
                }
            }

            public static void Read_Server4()
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    if (Parameter.Socket.Server.isStop[3] == false)
                    {
                        if (Parameter.Socket.SClient.occupy[3] == false)
                        {
                            Parameter.Socket.Server.tempClient[3] = Parameter.Socket.Server.Listener[3].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.Socket.Server.tempClient[3].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.Server_CLIENT_IP[3].ToString())
                            {
                                var sKA4 = new Parameter.Socket.Server.ka();
                                sKA4.onoff = 1;
                                sKA4.keepalivetime = 1000;
                                sKA4.keepaliveinterval = 500;

                                Parameter.Socket.SClient.occupy[3] = true;
                                Parameter.Socket.SClient.Connector[3] = Parameter.Socket.Server.tempClient[3];
                                Parameter.Socket.SClient.Stream[3] = Parameter.Socket.SClient.Connector[3].GetStream();
                                Parameter.Socket.SClient.Connector[3].Client.IOControl(IOControlCode.KeepAliveValues, sKA4.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.Socket.SClient.Connector[3].Client.RemoteEndPoint, Parameter.CommInfo.Server_NAME[3].ToString()));

                                Parameter.Socket.SClient.ReadThread[3] = new Thread(new ThreadStart(Read_SClient4));
                                Parameter.Socket.SClient.ReadThread[3].Start();
                            }
                            else
                            {
                                Parameter.Socket.Server.tempClient[3].Close();
                            }
                        }
                    }
                }
            }

            public static void Read_Server5()
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    if (Parameter.Socket.Server.isStop[4] == false)
                    {
                        if (Parameter.Socket.SClient.occupy[4] == false)
                        {
                            Parameter.Socket.Server.tempClient[4] = Parameter.Socket.Server.Listener[4].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.Socket.Server.tempClient[4].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.Server_CLIENT_IP[4].ToString())
                            {
                                var sKA5 = new Parameter.Socket.Server.ka();
                                sKA5.onoff = 1;
                                sKA5.keepalivetime = 1000;
                                sKA5.keepaliveinterval = 500;

                                Parameter.Socket.SClient.occupy[4] = true;
                                Parameter.Socket.SClient.Connector[4] = Parameter.Socket.Server.tempClient[4];
                                Parameter.Socket.SClient.Stream[4] = Parameter.Socket.SClient.Connector[4].GetStream();
                                Parameter.Socket.SClient.Connector[4].Client.IOControl(IOControlCode.KeepAliveValues, sKA5.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.Socket.SClient.Connector[4].Client.RemoteEndPoint, Parameter.CommInfo.Server_NAME[4].ToString()));

                                Parameter.Socket.SClient.ReadThread[4] = new Thread(new ThreadStart(Read_SClient5));
                                Parameter.Socket.SClient.ReadThread[4].Start();
                            }
                            else
                            {
                                Parameter.Socket.Server.tempClient[4].Close();
                            }
                        }
                    }
                }
            }

            public static void Read_Server6()
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    if (Parameter.Socket.Server.isStop[5] == false)
                    {
                        if (Parameter.Socket.SClient.occupy[5] == false)
                        {
                            Parameter.Socket.Server.tempClient[5] = Parameter.Socket.Server.Listener[5].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.Socket.Server.tempClient[5].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.Server_CLIENT_IP[5].ToString())
                            {
                                var sKA6 = new Parameter.Socket.Server.ka();
                                sKA6.onoff = 1;
                                sKA6.keepalivetime = 1000;
                                sKA6.keepaliveinterval = 500;

                                Parameter.Socket.SClient.occupy[5] = true;
                                Parameter.Socket.SClient.Connector[5] = Parameter.Socket.Server.tempClient[5];
                                Parameter.Socket.SClient.Stream[5] = Parameter.Socket.SClient.Connector[5].GetStream();
                                Parameter.Socket.SClient.Connector[5].Client.IOControl(IOControlCode.KeepAliveValues, sKA6.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.Socket.SClient.Connector[5].Client.RemoteEndPoint, Parameter.CommInfo.Server_NAME[5].ToString()));

                                Parameter.Socket.SClient.ReadThread[5] = new Thread(new ThreadStart(Read_SClient6));
                                Parameter.Socket.SClient.ReadThread[5].Start();
                            }
                            else
                            {
                                Parameter.Socket.Server.tempClient[5].Close();
                            }
                        }
                    }
                }
            }

            public static void Close_Server(int no)
            {
                Parameter.Socket.Server.Listener[no].Server.Close();
                Parameter.Socket.Server.ReadThread[no].Abort();
            }
            #endregion

            #region SClient           
            public static void Read_SClient1()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.SClient.Stream[0].CanRead)
                    {
                        if (Parameter.Socket.SClient.occupy[0] == true)
                        {
                            try
                            {
                                int len = Parameter.Socket.SClient.Stream[0].Read(Parameter.Socket.SClient.SC_buf1, 0, Parameter.Socket.SClient.SC_buf1.Length);

                                if (len != 0 && len > 0)
                                {
                                    Parameter.Socket.SClient.Rcv_SCQ1.Enqueue(Parameter.Socket.SClient.SC_buf1,len);         
                                    Array.Clear(Parameter.Socket.SClient.SC_buf1, 0, Parameter.Socket.SClient.SC_buf1.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_SClient(0);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_SClient(0);
                            }
                        }
                    }
                }
            }

            public static void Read_SClient2()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.SClient.Stream[1].CanRead)
                    {
                        if (Parameter.Socket.SClient.occupy[1] == true)
                        {
                            try
                            {
                                int len = Parameter.Socket.SClient.Stream[1].Read(Parameter.Socket.SClient.SC_buf2, 0, Parameter.Socket.SClient.SC_buf2.Length);

                                if (len != 0 && len > 0)
                                {
                                    Parameter.Socket.SClient.Rcv_SCQ2.Enqueue(Parameter.Socket.SClient.SC_buf2,len);
                                    Array.Clear(Parameter.Socket.SClient.SC_buf2, 0, Parameter.Socket.SClient.SC_buf2.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_SClient(1);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_SClient(1);
                            }
                        }
                    }
                }
            }

            public static void Read_SClient3()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.SClient.Stream[2].CanRead)
                    {
                        if (Parameter.Socket.SClient.occupy[2] == true)
                        {
                            try
                            {
                                int len = Parameter.Socket.SClient.Stream[2].Read(Parameter.Socket.SClient.SC_buf3, 0, Parameter.Socket.SClient.SC_buf3.Length);

                                if (len != 0 && len > 0)
                                {
                                    Parameter.Socket.SClient.Rcv_SCQ3.Enqueue(Parameter.Socket.SClient.SC_buf3,len);
                                    Array.Clear(Parameter.Socket.SClient.SC_buf3, 0, Parameter.Socket.SClient.SC_buf3.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_SClient(2);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_SClient(2);
                            }
                        }
                    }
                }
            }

            public static void Read_SClient4()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.SClient.Stream[3].CanRead)
                    {
                        if (Parameter.Socket.SClient.occupy[3] == true)
                        {
                            try
                            {
                                int len = Parameter.Socket.SClient.Stream[3].Read(Parameter.Socket.SClient.SC_buf4, 0, Parameter.Socket.SClient.SC_buf4.Length);

                                if (len != 0 && len > 0)
                                {
                                    Parameter.Socket.SClient.Rcv_SCQ4.Enqueue(Parameter.Socket.SClient.SC_buf4,len);
                                    Array.Clear(Parameter.Socket.SClient.SC_buf4, 0, Parameter.Socket.SClient.SC_buf4.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_SClient(3);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_SClient(3);
                            }
                        }
                    }
                }
            }

            public static void Read_SClient5()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.SClient.Stream[4].CanRead)
                    {
                        if (Parameter.Socket.SClient.occupy[4] == true)
                        {
                            try
                            {
                                int len = Parameter.Socket.SClient.Stream[4].Read(Parameter.Socket.SClient.SC_buf5, 0, Parameter.Socket.SClient.SC_buf5.Length);

                                if (len != 0 && len > 0)
                                {
                                    Parameter.Socket.SClient.Rcv_SCQ5.Enqueue(Parameter.Socket.SClient.SC_buf5,len);
                                    Array.Clear(Parameter.Socket.SClient.SC_buf5, 0, Parameter.Socket.SClient.SC_buf5.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_SClient(4);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_SClient(4);
                            }
                        }
                    }
                }
            }

            public static void Read_SClient6()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.SClient.Stream[5].CanRead)
                    {
                        if (Parameter.Socket.SClient.occupy[5] == true)
                        {
                            try
                            {
                                int len = Parameter.Socket.SClient.Stream[5].Read(Parameter.Socket.SClient.SC_buf6, 0, Parameter.Socket.SClient.SC_buf6.Length);

                                if (len != 0 && len > 0)
                                {
                                    Parameter.Socket.SClient.Rcv_SCQ6.Enqueue(Parameter.Socket.SClient.SC_buf6,len);
                                    Array.Clear(Parameter.Socket.SClient.SC_buf6, 0, Parameter.Socket.SClient.SC_buf6.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_SClient(5);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_SClient(5);
                            }
                        }
                    }
                }
            }
            
            public static void Close_SClient(int no)
            {
                if (Parameter.Socket.SClient.occupy[no] == true)
                {
                    Console.WriteLine(string.Format("SClient {0} Disconnected", Parameter.CommInfo.Server_NAME[no]));
                    Parameter.Socket.SClient.occupy[no] = false;
                    Parameter.Socket.SClient.Stream[no].Close();
                    Parameter.Socket.SClient.Connector[no].Close();
                    Parameter.Socket.SClient.ReadThread[no].Abort();
                }
            }
            #endregion

            #region Client
            public static void Read_Client1()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.Client.bThreadIsStop[0] == false)
                    {
                        if (Parameter.Socket.Client.occupy[0] == true)
                        {
                            if (Parameter.Socket.Client.Stream[0].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.Socket.Client.Stream[0].Read(Parameter.Socket.Client.C_buf1, 0, Parameter.Socket.Client.C_buf1.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        Parameter.Socket.Client.Rcv_CCQ1.Enqueue(Parameter.Socket.Client.C_buf1, len);
                                        Array.Clear(Parameter.Socket.Client.C_buf1, 0, Parameter.Socket.Client.C_buf1.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.Socket.Client.bClosing[0] = true;
                                        Close_Client(0);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.Socket.Client.bClosing[0] = true;
                                    Close_Client(0);
                                }
                            }
                        }
                    }
                }
            }

            public static void Read_Client2()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.Client.bThreadIsStop[1] == false)
                    {
                        if (Parameter.Socket.Client.occupy[1] == true)
                        {
                            if (Parameter.Socket.Client.Stream[1].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.Socket.Client.Stream[1].Read(Parameter.Socket.Client.C_buf2, 0, Parameter.Socket.Client.C_buf2.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        Parameter.Socket.Client.Rcv_CCQ2.Enqueue(Parameter.Socket.Client.C_buf2, len);
                                        Array.Clear(Parameter.Socket.Client.C_buf2, 0, Parameter.Socket.Client.C_buf2.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.Socket.Client.bClosing[1] = true;
                                        Close_Client(1);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.Socket.Client.bClosing[1] = true;
                                    Close_Client(1);
                                }
                            }
                        }
                    }       
                }
            }

            public static void Read_Client3()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.Client.bThreadIsStop[2] == false)
                    {
                        if (Parameter.Socket.Client.occupy[2] == true)
                        {
                            if (Parameter.Socket.Client.Stream[2].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.Socket.Client.Stream[2].Read(Parameter.Socket.Client.C_buf3, 0, Parameter.Socket.Client.C_buf3.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        Parameter.Socket.Client.Rcv_CCQ3.Enqueue(Parameter.Socket.Client.C_buf3, len);
                                        Array.Clear(Parameter.Socket.Client.C_buf3, 0, Parameter.Socket.Client.C_buf3.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.Socket.Client.bClosing[2] = true;
                                        Close_Client(2);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.Socket.Client.bClosing[2] = true;
                                    Close_Client(2);
                                }
                            }
                        }
                    }
                }
            }

            public static void Read_Client4()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.Client.bThreadIsStop[3] == false)
                    {
                        if (Parameter.Socket.Client.occupy[3] == true)
                        {
                            if (Parameter.Socket.Client.Stream[3].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.Socket.Client.Stream[3].Read(Parameter.Socket.Client.C_buf4, 0, Parameter.Socket.Client.C_buf4.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        Parameter.Socket.Client.Rcv_CCQ4.Enqueue(Parameter.Socket.Client.C_buf4, len);
                                        Array.Clear(Parameter.Socket.Client.C_buf4, 0, Parameter.Socket.Client.C_buf4.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.Socket.Client.bClosing[3] = true;
                                        Close_Client(3);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.Socket.Client.bClosing[3] = true;
                                    Close_Client(3);
                                }
                            }
                        }
                    }                    
                }
            }

            public static void Read_Client5()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.Client.bThreadIsStop[4] == false)
                    {
                        if (Parameter.Socket.Client.occupy[4] == true)
                        {
                            if (Parameter.Socket.Client.Stream[4].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.Socket.Client.Stream[4].Read(Parameter.Socket.Client.C_buf5, 0, Parameter.Socket.Client.C_buf5.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        Parameter.Socket.Client.Rcv_CCQ5.Enqueue(Parameter.Socket.Client.C_buf5, len);
                                        Array.Clear(Parameter.Socket.Client.C_buf5, 0, Parameter.Socket.Client.C_buf5.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.Socket.Client.bClosing[4] = true;
                                        Close_Client(4);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.Socket.Client.bClosing[4] = true;
                                    Close_Client(4);
                                }
                            }
                        }
                    }                    
                }
            }

            public static void Read_Client6()
            {
                while (true)
                {
                    Thread.Sleep(30);

                    if (Parameter.Socket.Client.bThreadIsStop[5] == false)
                    {
                        if (Parameter.Socket.Client.occupy[5] == true)
                        {
                            if (Parameter.Socket.Client.Stream[5].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.Socket.Client.Stream[5].Read(Parameter.Socket.Client.C_buf6, 0, Parameter.Socket.Client.C_buf6.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        Parameter.Socket.Client.Rcv_CCQ6.Enqueue(Parameter.Socket.Client.C_buf6, len);
                                        Array.Clear(Parameter.Socket.Client.C_buf6, 0, Parameter.Socket.Client.C_buf6.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.Socket.Client.bClosing[5] = true;
                                        Close_Client(5);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.Socket.Client.bClosing[5] = true;
                                    Close_Client(5);
                                }
                            }
                        }
                    }                    
                }
            }

            public static void Close_Client(int no)
            {
               if (Parameter.Socket.Client.occupy[no] == true)
                {
                    Console.WriteLine(string.Format("Client {0} Disconnected", Parameter.CommInfo.Client_NAME[no]));
                    Parameter.Socket.Client.Connector[0].Client.EndConnect(Parameter.Socket.Client.ka_sync.ar[no]);  
                    Parameter.Socket.Client.bThreadIsStop[no] = true;
                    Parameter.Socket.Client.Stream[no].Close();
                    Parameter.Socket.Client.Connector[no].Close();  
                }

               Parameter.Socket.Client.occupy[no] = false;
               Parameter.Socket.Client.bClosing[no] = false;  
            }

            public static void Connect_Client(int no, string ip, int port)
            {
                Parameter.Socket.Client.Connector[no] = new TcpClient();

                var cKA = new Parameter.Socket.Client.ka();
                cKA.onoff = 1;
                cKA.keepalivetime = 1000;
                cKA.keepaliveinterval = 500; // 클라이언트 접속상태(랜선뽑힘 등)를 확인하기 위해 IOControl을 위한 설정 

                Parameter.Socket.Client.ka_sync.ar[no] = Parameter.Socket.Client.Connector[no].BeginConnect(ip, port, null, null);
                System.Threading.WaitHandle wh = Parameter.Socket.Client.ka_sync.ar[no].AsyncWaitHandle;

                try
                {
                    if (!Parameter.Socket.Client.ka_sync.ar[no].AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1), false))
                    {
                        Parameter.Socket.Client.Connector[no].Close();
                    }

                    if (Parameter.Socket.Client.ka_sync.ar[no].AsyncState != null)
                        Parameter.Socket.Client.Connector[no].EndConnect(Parameter.Socket.Client.ka_sync.ar[no]);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fail to connect server Catched");
                }
                finally
                {
                    wh.Close();
                }

                if (Parameter.Socket.Client.Connector[no].Client != null)
                {
                    if (Parameter.Socket.Client.Connector[no].Client.Connected == true)
                    {
                        Console.WriteLine(string.Format("Client {0}/{1} Occupied", Parameter.CommInfo.Client_NAME[no], ip));
                        Parameter.Socket.Client.Connector[no].Client.IOControl(IOControlCode.KeepAliveValues, cKA.Buffer, null); //구미사격장에서는 시리얼하고 이더넷하고 두가지상태가있어서 하트비트로 체크해야되기때문에 이거 빼야됨
                        Parameter.Socket.Client.occupy[no] = true;
                        Parameter.Socket.Client.Stream[no] = Parameter.Socket.Client.Connector[no].GetStream();
                        Parameter.Socket.Client.Connector[no].NoDelay = true;
                        Parameter.Socket.Client.bThreadIsStop[no] = false;                        
                    }               
                }      
            }

            public static void Monitoring_Client()
            {
                while (true)
                {
                    Thread.Sleep(1000);

                    for (int i = 0; i < Parameter.CommInfo.iClient_Amount; i++)
                    {
                        if(Parameter.Socket.Client.occupy[i] == false && Parameter.Socket.Client.bClosing[i] == false) Connect_Client(i, Parameter.CommInfo.Client_IP[i], int.Parse(Parameter.CommInfo.Client_PORT[i].ToString()));
                    }

                    Internal_Method.Monitoring_Serial();
                }
            }           
            #endregion

            #region Serial           
            public static void Read_Serial1()
            {
                while (true)
                {
                    Thread.Sleep(50);

                    try
                    {
                        byte[] cmd = new byte[Parameter.Serial.Connector[0].BytesToRead];

                        int byteLen = Parameter.Serial.Connector[0].Read(cmd, 0, cmd.Length);

                        if (byteLen != 0)
                        {
                            Parameter.Serial.Rcv_SQ1.Enqueue(cmd);
                              
                        }

                    }
                    catch (Exception ex)
                    {                        
                    }

                  //  Console.WriteLine("rcv sq1 input");
                }
            }

            public static void Read_Serial2()
            {
                while (true)
                {
                    Thread.Sleep(50);

                    try
                    {
                        byte[] cmd = new byte[Parameter.Serial.Connector[1].BytesToRead];
                        int byteLen = Parameter.Serial.Connector[1].Read(cmd, 0, cmd.Length);

                        if (byteLen != 0)
                        {
                            Parameter.Serial.Rcv_SQ2.Enqueue(cmd);
                            //Console.WriteLine(string.Format("rcv sq2 : {0}", byteLen));
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            public static void Read_Serial3()
            {
                while (true)
                {
                    Thread.Sleep(50);

                    try
                    {
                        byte[] cmd = new byte[Parameter.Serial.Connector[2].BytesToRead];
                        int byteLen = Parameter.Serial.Connector[2].Read(cmd, 0, cmd.Length);

                        if (byteLen != 0)
                        {
                            Parameter.Serial.Rcv_SQ3.Enqueue(cmd);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            public static void Read_Serial4()
            {
                while (true)
                {
                    Thread.Sleep(50);

                    try
                    {
                        byte[] cmd = new byte[Parameter.Serial.Connector[3].BytesToRead];
                        int byteLen = Parameter.Serial.Connector[3].Read(cmd, 0, cmd.Length);

                        if (byteLen != 0)
                        {
                            Parameter.Serial.Rcv_SQ4.Enqueue(cmd);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            public static void Read_Serial5()
            {
                while (true)
                {
                    Thread.Sleep(50);

                    try
                    {
                        byte[] cmd = new byte[Parameter.Serial.Connector[4].BytesToRead];
                        int byteLen = Parameter.Serial.Connector[4].Read(cmd, 0, cmd.Length);

                        Parameter.Serial.Rcv_SQ5.Enqueue(cmd);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            public static void Read_Serial6()
            {
                while (true)
                {
                    Thread.Sleep(50);

                    try
                    {
                        byte[] cmd = new byte[Parameter.Serial.Connector[5].BytesToRead];
                        int byteLen = Parameter.Serial.Connector[5].Read(cmd, 0, cmd.Length);

                        Parameter.Serial.Rcv_SQ6.Enqueue(cmd);
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }

            public static void Close_Serial(int no)
            {
                if (Parameter.Serial.occupy[no] == true)
                {
                    Parameter.Serial.ReadThread[no].Abort();
                    Parameter.Serial.occupy[no] = false;
                    Parameter.Serial.Connector[no].Close();
                }
            }

            public static void Monitoring_Serial()
            {
                for (int i = 0; i < Parameter.CommInfo.iSerial_Amount; i++)
                {
                    if (Parameter.Serial.occupy[i] == false && Parameter.Serial.Connector[i].IsOpen == false)
                    {
                        Parameter.Serial.Connector[i].BaudRate = int.Parse(Parameter.CommInfo.Serial_BR[i]);
                        Parameter.Serial.Connector[i].PortName = Parameter.CommInfo.Serial_PORT[i];

                        

                        try
                        {                            
                            Parameter.Serial.Connector[i].Open();
                            if (Parameter.Serial.Connector[i].IsOpen == true)
                            {
                                Console.WriteLine("Serial No.[{0}] has connected", i);
                                Parameter.Serial.occupy[i] = true;
                                switch (i)
                                {
                                    case 0: { Parameter.Serial.ReadThread[i] = new Thread(new ThreadStart(Read_Serial1)); } break;
                                    case 1: { Parameter.Serial.ReadThread[i] = new Thread(new ThreadStart(Read_Serial2)); } break;
                                    case 2: { Parameter.Serial.ReadThread[i] = new Thread(new ThreadStart(Read_Serial3)); } break;
                                    case 3: { Parameter.Serial.ReadThread[i] = new Thread(new ThreadStart(Read_Serial4)); } break;
                                    case 4: { Parameter.Serial.ReadThread[i] = new Thread(new ThreadStart(Read_Serial5)); } break;
                                    case 5: { Parameter.Serial.ReadThread[i] = new Thread(new ThreadStart(Read_Serial6)); } break;
                                }

                                Parameter.Serial.ReadThread[i].Start();
                            }
                        }
                        catch (Exception ex)
                        {
                        }
                    }
                }
            }            
            #endregion
        }
    
        public class Parameter
        {
            public class Socket
            {
                public class Server
                {
                    public static TcpListener[] Listener = new TcpListener[6];
                    public static Thread[] ReadThread = new Thread[6];
                    public static TcpClient[] tempClient = new TcpClient[6];
                                       

                    public struct ka
                    {
                        public int onoff;
                        public int keepalivetime;
                        public int keepaliveinterval;

                        public unsafe byte[] Buffer
                        {
                            get
                            {
                                var buf = new byte[sizeof(ka)];
                                fixed (void* p = &this) Marshal.Copy(new IntPtr(p), buf, 0, buf.Length);
                                return buf;
                            }
                        }
                    }                    

                    public static bool[] isStop = new bool[6] { false, false, false, false, false, false };
                }               

                public class SClient
                {
                    public static TcpClient[] Connector = new TcpClient[6];
                    public static NetworkStream[] Stream = new NetworkStream[6];
                    public static Thread[] ReadThread = new Thread[6];
                    public static bool[] occupy = new bool[6] { false, false, false, false, false, false };

                    public static ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

                    public static Internal_Method.CircularQueue Rcv_SCQ1 = new Internal_Method.CircularQueue(1024);
                    public static Internal_Method.CircularQueue Rcv_SCQ2 = new Internal_Method.CircularQueue(1024);
                    public static Internal_Method.CircularQueue Rcv_SCQ3 = new Internal_Method.CircularQueue(1024);
                    public static Internal_Method.CircularQueue Rcv_SCQ4 = new Internal_Method.CircularQueue(1024);
                    public static Internal_Method.CircularQueue Rcv_SCQ5 = new Internal_Method.CircularQueue(1024);
                    public static Internal_Method.CircularQueue Rcv_SCQ6 = new Internal_Method.CircularQueue(1024);
                    /*
                    public static ConcurrentQueue<byte[]> Rcv_SCQ1 = new ConcurrentQueue<byte[]>();  
                    public static ConcurrentQueue<byte[]> Rcv_SCQ2 = new ConcurrentQueue<byte[]>();
                    public static ConcurrentQueue<byte[]> Rcv_SCQ3 = new ConcurrentQueue<byte[]>();
                    public static ConcurrentQueue<byte[]> Rcv_SCQ4 = new ConcurrentQueue<byte[]>();
                    public static ConcurrentQueue<byte[]> Rcv_SCQ5 = new ConcurrentQueue<byte[]>();
                    public static ConcurrentQueue<byte[]> Rcv_SCQ6 = new ConcurrentQueue<byte[]>();
                    */
                    public static byte[] SC_buf1 = new byte[1024];
                    public static byte[] SC_buf2 = new byte[1024];
                    public static byte[] SC_buf3 = new byte[1024];
                    public static byte[] SC_buf4 = new byte[1024];
                    public static byte[] SC_buf5 = new byte[1024];
                    public static byte[] SC_buf6 = new byte[1024];
                }

                public class Client
                {
                    public static Thread MonitoringThread = null;
                    public static TcpClient[] Connector = new TcpClient[6];
                    public static NetworkStream[] Stream = new NetworkStream[6];
                    public static Thread[] ReadThread = new Thread[6];
                    public static bool[] occupy = new bool[6] { false, false, false, false, false, false };
                    public static bool[] bClosing = new bool[6] { false, false, false, false, false, false };
                    public static bool[] bThreadIsStop = new bool[6] { false, false, false, false, false, false };

                    public static Internal_Method.CircularQueue Rcv_CCQ1 = new Internal_Method.CircularQueue(1024);
                    public static Internal_Method.CircularQueue Rcv_CCQ2 = new Internal_Method.CircularQueue(1024);
                    public static Internal_Method.CircularQueue Rcv_CCQ3 = new Internal_Method.CircularQueue(1024);
                    public static Internal_Method.CircularQueue Rcv_CCQ4 = new Internal_Method.CircularQueue(1024);
                    public static Internal_Method.CircularQueue Rcv_CCQ5 = new Internal_Method.CircularQueue(1024);
                    public static Internal_Method.CircularQueue Rcv_CCQ6 = new Internal_Method.CircularQueue(1024);

                    //public static ConcurrentQueue<byte[]> Rcv_CQ1 = new ConcurrentQueue<byte[]>();
                    //public static ConcurrentQueue<byte[]> Rcv_CQ2 = new ConcurrentQueue<byte[]>();
                    //public static ConcurrentQueue<byte[]> Rcv_CQ3 = new ConcurrentQueue<byte[]>();
                    //public static ConcurrentQueue<byte[]> Rcv_CQ4 = new ConcurrentQueue<byte[]>();
                    //public static ConcurrentQueue<byte[]> Rcv_CQ5 = new ConcurrentQueue<byte[]>();
                    //public static ConcurrentQueue<byte[]> Rcv_CQ6 = new ConcurrentQueue<byte[]>();

                    public static byte[] C_buf1 = new byte[1024];
                    public static byte[] C_buf2 = new byte[1024];
                    public static byte[] C_buf3 = new byte[1024];
                    public static byte[] C_buf4 = new byte[1024];
                    public static byte[] C_buf5 = new byte[1024];
                    public static byte[] C_buf6 = new byte[1024];

                    public class ka_sync
                    {
                        public static IAsyncResult[] ar = new IAsyncResult[6];
                        public static bool[] bIC = new bool[6] { false, false, false, false, false, false };
                    }

                    public struct ka
                    {
                        public int onoff;
                        public int keepalivetime;
                        public int keepaliveinterval;

                        public unsafe byte[] Buffer
                        {
                            get
                            {
                                var buf = new byte[sizeof(ka)];
                                fixed (void* p = &this) Marshal.Copy(new IntPtr(p), buf, 0, buf.Length);
                                return buf;
                            }
                        }
                    }        
                }
            }

            public class Serial
            {
                public static SerialPort[] Connector = new SerialPort[6];

                public static ConcurrentQueue<byte[]> Rcv_SQ1 = new ConcurrentQueue<byte[]>();
                public static ConcurrentQueue<byte[]> Rcv_SQ2 = new ConcurrentQueue<byte[]>();
                public static ConcurrentQueue<byte[]> Rcv_SQ3 = new ConcurrentQueue<byte[]>();
                public static ConcurrentQueue<byte[]> Rcv_SQ4 = new ConcurrentQueue<byte[]>();
                public static ConcurrentQueue<byte[]> Rcv_SQ5 = new ConcurrentQueue<byte[]>();
                public static ConcurrentQueue<byte[]> Rcv_SQ6 = new ConcurrentQueue<byte[]>();

                public static Thread[] ReadThread = new Thread[6];
                public static bool[] occupy = new bool[6] { false, false, false, false, false, false };
            }

            public class CommInfo
            {
                public static bool[] bCantOpenServer = new bool[6] { false, false, false, false, false, false }; 

                public static StringBuilder sbServer_Amount = new StringBuilder(20);
                public static StringBuilder sbClient_Amount = new StringBuilder(20);
                public static StringBuilder sbSerial_Amount = new StringBuilder(20);

                public static int iServer_Amount = 0;
                public static int iClient_Amount = 0;
                public static int iSerial_Amount = 0;

                public static string[] Server_NAME = new string[6];
                public static string[] Server_IP = new string[6];
                public static string[] Server_PORT = new string[6];
                public static string[] Server_CLIENT_IP = new string[6];

              //  public static string[] SClient_NAME = new string[6];
              //  public static string[] SClient_IP = new string[6];

                public static string[] Client_NAME = new string[6];
                public static string[] Client_IP = new string[6];
                public static string[] Client_PORT = new string[6];

                public static string[] Serial_NAME = new string[6];
                public static string[] Serial_PORT = new string[6];
                public static string[] Serial_BR = new string[6];

                public static StringBuilder CommAuto = new StringBuilder(20);

                public static string NetworkPath = null; //= "C:\\CUSV\\config\\MPS.cfg";
                public static int Length = 20;

                public static string[] server = new string[6] { "Server1", "Server2", "Server3", "Server4", "Server5", "Server6" };               
                public static string[] client = new string[6] { "Client1", "Client2", "Client3", "Client4", "Client5", "Client6" };
                public static string[] serial = new string[6] { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6" };       
            }
        }
    }
}
