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

/* 2020-06-10 UDP, Multicast 전송 시, 자신의 Port에도 송신 데이터가 수신되면서, 해당 Queue를 읽지 않으면 Q is Full이 발생함. cfg파일에 SENDONLY 파라미터를 둬서 읽을 필요가 없는 경우는 Queue에 안 쌓는걸로 추가함
 * 
 * 
 * 
 */
 
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

        private class Param
        {
            public static string path = System.Environment.CurrentDirectory + "\\config.cfg";
            public static string filename = "\\config.cfg";
            public static bool bEndProcess = false;
        }

        public class APIManager
        {
            public class Status
            {
                public static bool[] bCantOpenTCPServer = new bool[6] { false, false, false, false, false, false };
                public static bool[] bCantOpenUDPSocket = new bool[6] { false, false, false, false, false, false };

                public class TCPSClient
                {
                    public static bool[] occupy = new bool[6] { false, false, false, false, false, false };
                }
            }

            public class Q
            {
                public class CircularQueue
                {
                    public int front = 0;
                    public int rear = 0;
                    public int Q_Size = 5;
                    public int now_no = 1;
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

                    public void Enqueue(byte[] data, int len, string name)
                    {
                        if (IsFull())
                        {
                            Console.WriteLine(string.Format("{0}, Q is Full", name));
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
                            now_no = front;
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

                        preindex = now_no;
                        // front = (++front) % Q_Size;
                        //무결성 시험 시
                        //if (CMA_For.forlog.bIntegrity == true) front = (++front) % Q_Size;

                        return byteq[preindex].length;
                    }
                }

                public class TCPSClient
                {
                    public static CircularQueue Rcv_SCQ1 = new CircularQueue(1024);
                    public static CircularQueue Rcv_SCQ2 = new CircularQueue(1024);
                    public static CircularQueue Rcv_SCQ3 = new CircularQueue(1024);
                    public static CircularQueue Rcv_SCQ4 = new CircularQueue(1024);
                    public static CircularQueue Rcv_SCQ5 = new CircularQueue(1024);
                    public static CircularQueue Rcv_SCQ6 = new CircularQueue(1024);
                }

                public class TCPClient
                {
                    public static CircularQueue Rcv_CCQ1 = new CircularQueue(1024);
                    public static CircularQueue Rcv_CCQ2 = new CircularQueue(1024);
                    public static CircularQueue Rcv_CCQ3 = new CircularQueue(1024);
                    public static CircularQueue Rcv_CCQ4 = new CircularQueue(1024);
                    public static CircularQueue Rcv_CCQ5 = new CircularQueue(1024);
                    public static CircularQueue Rcv_CCQ6 = new CircularQueue(1024);
                }

                public class UDPSClient
                {
                    public static CircularQueue Rcv_SCQ1 = new CircularQueue(4096);
                    public static CircularQueue Rcv_SCQ2 = new CircularQueue(4096);
                    public static CircularQueue Rcv_SCQ3 = new CircularQueue(4096);
                    public static CircularQueue Rcv_SCQ4 = new CircularQueue(4096);
                    public static CircularQueue Rcv_SCQ5 = new CircularQueue(4096);
                    public static CircularQueue Rcv_SCQ6 = new CircularQueue(4096);
                }

                public class Serial
                {
                    public static CircularQueue Rcv_SQ1 = new CircularQueue(4096);
                    public static CircularQueue Rcv_SQ2 = new CircularQueue(4096);
                    public static CircularQueue Rcv_SQ3 = new CircularQueue(4096);
                    public static CircularQueue Rcv_SQ4 = new CircularQueue(4096);
                    public static CircularQueue Rcv_SQ5 = new CircularQueue(4096);
                    public static CircularQueue Rcv_SQ6 = new CircularQueue(4096);
                    //public static ConcurrentQueue<byte[]> Rcv_SQ1 = new ConcurrentQueue<byte[]>();
                    //public static ConcurrentQueue<byte[]> Rcv_SQ2 = new ConcurrentQueue<byte[]>();
                    //public static ConcurrentQueue<byte[]> Rcv_SQ3 = new ConcurrentQueue<byte[]>();
                    //public static ConcurrentQueue<byte[]> Rcv_SQ4 = new ConcurrentQueue<byte[]>();
                    //public static ConcurrentQueue<byte[]> Rcv_SQ5 = new ConcurrentQueue<byte[]>();
                    //public static ConcurrentQueue<byte[]> Rcv_SQ6 = new ConcurrentQueue<byte[]>();
                }
            }

            public static void Set_CFG_Path(string path) //기본은 실행파일 폴더에 config.cfg파일이 있음, CMLib.Begin 전에 경로 설정해주면 사용가능
            {
                Parameter.CommInfo.NetworkPath = path+Param.filename;
            }

            public static void Begin()
            {
                Parameter.CommInfo.NetworkPath = Param.path;

                InformationManager.ReadInfo();
            }

            public static void End()
            {
                Param.bEndProcess = true;
                Parameter.TCPSocket.Client.MonitoringThread.Join(); //Abort();

                for (int i = 0; i < Parameter.CommInfo.iTCPServer_Amount; i++)
                {
                    DataManager.Close_TCP_Server(i);
                    DataManager.Close_TCP_SClient(i);
                }

                for (int i = 0; i < Parameter.CommInfo.iTCPClient_Amount; i++)
                {
                    DataManager.Close_TCP_Client(i);                    
                }
            
                for (int i = 0; i < Parameter.CommInfo.iSerial_Amount; i++)
                {
                    DataManager.Close_Serial(i);
                }
            }            

            public static void Send_TCP_SClient(string name, byte[] data, int len)
            {
                int no = 10;

                for (int i = 0; i < Parameter.CommInfo.iTCPServer_Amount; i++)
                {
                    if (name == Parameter.CommInfo.TCPServer_NAME[i])
                    {
                        no = i;
                        break;
                    }
                }

                if (no != 10)
                {
                    if (APIManager.Status.TCPSClient.occupy[no] == true)
                    {
                        try
                        {
                            Parameter.TCPSocket.SClient.Stream[no].Write(data, 0, len);
                        }
                        catch (Exception ex)
                        {
                            DataManager.Close_TCP_SClient(no);

                            Console.WriteLine("TCP SClient Send Error");
                        }                       
                    }
                }
            } //TCP Server에 연결된 Client로 데이터 전송

            public static void Send_TCP_Client(string name, byte[] data, int len)
            {
                int no = 10;

                for (int i = 0; i < Parameter.CommInfo.iTCPClient_Amount; i++)
                {
                    if (name == Parameter.CommInfo.TCPClient_NAME[i])
                    {
                        no = i;
                        break;
                    }
                }

                if (no != 10)
                {
                    if (Parameter.TCPSocket.Client.occupy[no] == true)
                    {
                        try
                        {                            
                            Parameter.TCPSocket.Client.Stream[no].Write(data, 0, len);
                        }
                        catch (Exception ex)
                        {
                         //   DataManager.Close_TCP_Client(no);

                            Console.WriteLine("TCP Client Send Error");
                        }
                    }
                }
            } //TCP Client에서 Server로 데이터전송

            public static void Send_UDP_Socket(string name, byte[] data, int len) //UDP 데이터 전송
            {
                int no = 10;

                for (int i = 0; i < Parameter.CommInfo.iUDPSocket_Amount; i++)
                {
                    if (name == Parameter.CommInfo.UDPSocket_NAME[i])
                    {
                        no = i;
                        break;
                    }
                }

                if (no != 10)
                {/*
                    if (Parameter.CommInfo.UDPSocket_MULTICAST[no] == "TRUE")
                     {
                            Parameter.UDPSocket.Server.ipepMC[no] = new IPEndPoint(IPAddress.Parse(Parameter.CommInfo.UDPSocket_RECEIVER_IP[no].ToString()), int.Parse(Parameter.CommInfo.UDPSocket_RECEIVER_PORT[no].ToString())); 
                            Parameter.UDPSocket.Server.Connector[no].Send(data, len, Parameter.UDPSocket.Server.ipepMC[no]);
                        }
                        else Parameter.UDPSocket.Server.Connector[no].Send(data, len, Parameter.UDPSocket.Server.ipepSend[no]);
                    */
                    try
                    {
                        if (Parameter.CommInfo.UDPSocket_MULTICAST[no] == "TRUE")
                        {
                            Parameter.UDPSocket.Server.ipepMC[no] = new IPEndPoint(IPAddress.Parse(Parameter.CommInfo.UDPSocket_RECEIVER_IP[no].ToString()), int.Parse(Parameter.CommInfo.UDPSocket_RECEIVER_PORT[no].ToString())); 
                            Parameter.UDPSocket.Server.Connector[no].Send(data, len, Parameter.UDPSocket.Server.ipepMC[no]);
                        }
                        else Parameter.UDPSocket.Server.Connector[no].Send(data, len, Parameter.UDPSocket.Server.ipepSend[no]);
                    }
                    catch (SocketException ex)
                    {
                        Console.WriteLine("UDP Send Error");
                    }
                }
            }

            public static void Send_Serial(string name, byte[] data, int len) //Serial 데이터 전송
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
                            DataManager.Close_Serial(no);
                            Console.WriteLine("Serial[{0}] has Closed", no);
                        }
                    }
                }                
            }

            public static void Close_TCP_SClient(int no)
            {
                if (APIManager.Status.TCPSClient.occupy[no] == true)
                {
                    Console.WriteLine(string.Format("SClient {0} Disconnected", Parameter.CommInfo.TCPServer_NAME[no]));
                    APIManager.Status.TCPSClient.occupy[no] = false;
                    Parameter.TCPSocket.SClient.Stream[no].Close();
                    Parameter.TCPSocket.SClient.Connector[no].Close();
                    Parameter.TCPSocket.SClient.ReadThread[no].Join(); //Abort();
                }
            }           

            public static void Close_TCP_Client(int no)
            {
                if (Parameter.TCPSocket.Client.occupy[no] == true)
                {
                    Console.WriteLine(string.Format("Client {0} Disconnected", Parameter.CommInfo.TCPClient_NAME[no]));
                    //if (Parameter.TCPSocket.Client.Connector[no].Client != null)
                //    {
                        Parameter.TCPSocket.Client.Connector[no].Client.EndConnect(Parameter.TCPSocket.Client.ka_sync.ar[no]);
                        Parameter.TCPSocket.Client.bThreadIsStop[no] = true;
                        Parameter.TCPSocket.Client.Stream[no].Close();
                        Parameter.TCPSocket.Client.Connector[no].Close();
                 //   }
                }

                Parameter.TCPSocket.Client.occupy[no] = false;
                Parameter.TCPSocket.Client.bClosing[no] = false;                 
            }          
        }

        private class InformationManager
        {
            internal static void ReadInfo()
            {

                GetPrivateProfileString("COMMUNICATION", "CONNECT", "", Parameter.CommInfo.CommAuto, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString()); //자동/수동 통신 연결 선택을 위한 파라미터인데 수동 커넥션 부분 구현 안 되어 있음

                #region TCPServer

                GetPrivateProfileString("COMMUNICATION", "TCPSERVER", "", Parameter.CommInfo.sbTCPServer_Amount, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString()); //본 SW에서 사용할 TCPServer 개수 읽어오기
                Parameter.CommInfo.iTCPServer_Amount = int.Parse(Parameter.CommInfo.sbTCPServer_Amount.ToString()); //cfg에서 읽은 TCPServer 갯수

                if (Parameter.CommInfo.iTCPServer_Amount > 0)
                {
                    for (int i = 0; i < Parameter.CommInfo.iTCPServer_Amount; i++)
                    {
                        StringBuilder temp = new StringBuilder(20); //사용할 Server 이름,IP,Port,클라이언트 IP 읽어오기

                        GetPrivateProfileString(Parameter.CommInfo.TCPserver[i], "NAME", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.TCPServer_NAME[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.TCPserver[i], "IP", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.TCPServer_IP[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.TCPserver[i], "PORT", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.TCPServer_PORT[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.TCPserver[i], "SCLIENT_IP", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.TCPServer_CLIENT_IP[i] = temp.ToString();

                        if (Parameter.CommInfo.CommAuto.ToString() == "AUTO") //통신 자동 선택 시, Server 관련 설정 후, Server Listen Start & Read Client Data
                        {
                            Parameter.TCPSocket.Server.Listener[i] = new TcpListener(IPAddress.Parse(Parameter.CommInfo.TCPServer_IP[i].ToString()), int.Parse(Parameter.CommInfo.TCPServer_PORT[i].ToString()));
                            Parameter.TCPSocket.Server.Listener[i].Server.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                            Parameter.TCPSocket.Server.Listener[i].Server.NoDelay = true;

                            try
                            {
                                Parameter.TCPSocket.Server.Listener[i].Start();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Fail to Open Server Socket / Please Check NIC Status OR SW IP Address");
                                APIManager.Status.bCantOpenTCPServer[i] = true;
                            }

                            switch (i) // TCP Server - 클라이언트 데이터 수신 쓰레드 인스턴스화
                            {
                                case 0: { Parameter.TCPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Server1)); } break;
                                case 1: { Parameter.TCPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Server2)); } break;
                                case 2: { Parameter.TCPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Server3)); } break;
                                case 3: { Parameter.TCPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Server4)); } break;
                                case 4: { Parameter.TCPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Server5)); } break;
                                case 5: { Parameter.TCPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Server6)); } break;
                            }

                            if (APIManager.Status.bCantOpenTCPServer[i] == false) // 수신 쓰레드 시작
                                Parameter.TCPSocket.Server.ReadThread[i].Start();
                        }
                    }
                }
                #endregion

                #region TCPClient

                GetPrivateProfileString("COMMUNICATION", "TCPCLIENT", "", Parameter.CommInfo.sbTCPClient_Amount, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                Parameter.CommInfo.iTCPClient_Amount = int.Parse(Parameter.CommInfo.sbTCPClient_Amount.ToString());

                if (Parameter.CommInfo.iTCPClient_Amount > 0)
                {
                    for (int i = 0; i < Parameter.CommInfo.iTCPClient_Amount; i++)
                    {
                        StringBuilder temp = new StringBuilder(20);

                        GetPrivateProfileString(Parameter.CommInfo.TCPclient[i], "NAME", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.TCPClient_NAME[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.TCPclient[i], "IP", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.TCPClient_IP[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.TCPclient[i], "PORT", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.TCPClient_PORT[i] = temp.ToString();

                        if (Parameter.CommInfo.CommAuto.ToString() == "AUTO")
                        {
                            switch (i)
                            {
                                case 0: { Parameter.TCPSocket.Client.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Client1)); } break;
                                case 1: { Parameter.TCPSocket.Client.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Client2)); } break;
                                case 2: { Parameter.TCPSocket.Client.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Client3)); } break;
                                case 3: { Parameter.TCPSocket.Client.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Client4)); } break;
                                case 4: { Parameter.TCPSocket.Client.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Client5)); } break;
                                case 5: { Parameter.TCPSocket.Client.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_TCP_Client6)); } break;
                            }
                            Parameter.TCPSocket.Client.ReadThread[i].Start();
                        }
                    }
                }
                #endregion

                #region UDPSocket

                GetPrivateProfileString("COMMUNICATION", "UDPSOCKET", "", Parameter.CommInfo.sbUDPSocket_Amount, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                Parameter.CommInfo.iUDPSocket_Amount = int.Parse(Parameter.CommInfo.sbUDPSocket_Amount.ToString());

                if (Parameter.CommInfo.iUDPSocket_Amount > 0)
                {
                    for (int i = 0; i < Parameter.CommInfo.iUDPSocket_Amount; i++)
                    {
                        StringBuilder temp = new StringBuilder(20);

                        GetPrivateProfileString(Parameter.CommInfo.UDPsocket[i], "NAME", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.UDPSocket_NAME[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.UDPsocket[i], "SENDER_IP", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.UDPSocket_SENDER_IP[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.UDPsocket[i], "SENDER_PORT", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.UDPSocket_SENDER_PORT[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.UDPsocket[i], "RECEIVER_IP", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.UDPSocket_RECEIVER_IP[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.UDPsocket[i], "RECEIVER_PORT", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.UDPSocket_RECEIVER_PORT[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.UDPsocket[i], "MULTICAST", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.UDPSocket_MULTICAST[i] = temp.ToString();

                        GetPrivateProfileString(Parameter.CommInfo.UDPsocket[i], "SENDONLY", "", temp, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                        Parameter.CommInfo.UDPSocket_SENDONLY[i] = temp.ToString();

                        if (Parameter.CommInfo.CommAuto.ToString() == "AUTO")
                        {
                            if (Parameter.CommInfo.UDPSocket_MULTICAST[i] == "TRUE") //UDP의 멀티캐스트 사용 시, Multicast 그룹을 위한 ip및 port 정의
                            {
                                Parameter.UDPSocket.Server.Connector[i] = new UdpClient(int.Parse(Parameter.CommInfo.UDPSocket_RECEIVER_PORT[i].ToString()));
                                IPAddress ipMulti = IPAddress.Parse(Parameter.CommInfo.UDPSocket_RECEIVER_IP[i].ToString());
                                Parameter.UDPSocket.Server.Connector[i].JoinMulticastGroup(ipMulti, 33);
                                Parameter.UDPSocket.Server.Connector[i].MulticastLoopback = false; //Multicast 사용 시, 동일 포트를 계속해서 읽고 있기 때문에 loopback 옵션을 끄지 않으면 송신 때마다 송신 데이터가 수신에 걸리게 됨
                               
                                Parameter.UDPSocket.Server.ipepMC[i] = new IPEndPoint(ipMulti, int.Parse(Parameter.CommInfo.UDPSocket_RECEIVER_PORT[i].ToString()));                               
                            }
                            else //UDP의 유니캐스트 사용 시, Sender와 Receiver 객체를 모두 생성 및 ip, port 각각 정의
                            {
                                Parameter.UDPSocket.Server.ipepSend[i] = new IPEndPoint(IPAddress.Parse(Parameter.CommInfo.UDPSocket_RECEIVER_IP[i].ToString()), int.Parse(Parameter.CommInfo.UDPSocket_RECEIVER_PORT[i].ToString()));
                                Parameter.UDPSocket.Server.ipepRead[i] = new IPEndPoint(IPAddress.Parse(Parameter.CommInfo.UDPSocket_SENDER_IP[i].ToString()), int.Parse(Parameter.CommInfo.UDPSocket_SENDER_PORT[i].ToString()));
                                Parameter.UDPSocket.Server.Connector[i] = new UdpClient(Parameter.UDPSocket.Server.ipepRead[i]);
                            }

                            switch (i)
                            {
                                case 0: { Parameter.UDPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_UDP_SClient1)); } break;
                                case 1: { Parameter.UDPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_UDP_SClient2)); } break;
                                case 2: { Parameter.UDPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_UDP_SClient3)); } break;
                                case 3: { Parameter.UDPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_UDP_SClient4)); } break;
                                case 4: { Parameter.UDPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_UDP_SClient5)); } break;
                                case 5: { Parameter.UDPSocket.Server.ReadThread[i] = new Thread(new ThreadStart(DataManager.Read_UDP_SClient6)); } break;
                            }

                            // if (Parameter.CommInfo.bCantOpenTCPServer[i] == false)
                            Parameter.UDPSocket.Server.ReadThread[i].Start();
                        }
                    }
                }
                #endregion

                #region Serial Comm

                GetPrivateProfileString("COMMUNICATION", "SERIAL", "", Parameter.CommInfo.sbSerial_Amount, Parameter.CommInfo.Length, Parameter.CommInfo.NetworkPath.ToString());
                Parameter.CommInfo.iSerial_Amount = int.Parse(Parameter.CommInfo.sbSerial_Amount.ToString());

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
                    Parameter.TCPSocket.Client.MonitoringThread = new Thread(new ThreadStart(DataManager.Monitoring_Client));
                    Parameter.TCPSocket.Client.MonitoringThread.Start();
                }
                #endregion
            }
        }

        private class DataManager
        {
            #region TCPServer
            public static void Read_TCP_Server1()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(1000);

                    if (Parameter.TCPSocket.Server.isStop[0] == false)
                    {
                        if (APIManager.Status.TCPSClient.occupy[0] == false)
                        {
                            Parameter.TCPSocket.Server.tempClient[0] = Parameter.TCPSocket.Server.Listener[0].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.TCPSocket.Server.tempClient[0].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.TCPServer_CLIENT_IP[0].ToString())
                            {
                                var sKA1 = new Parameter.TCPSocket.Server.ka();
                                sKA1.onoff = 1;
                                sKA1.keepalivetime = 1000;
                                sKA1.keepaliveinterval = 500;

                                APIManager.Status.TCPSClient.occupy[0] = true;
                                Parameter.TCPSocket.SClient.Connector[0] = Parameter.TCPSocket.Server.tempClient[0];
                                Parameter.TCPSocket.SClient.Stream[0] = Parameter.TCPSocket.SClient.Connector[0].GetStream();
                                Parameter.TCPSocket.SClient.Connector[0].Client.IOControl(IOControlCode.KeepAliveValues, sKA1.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.TCPSocket.SClient.Connector[0].Client.RemoteEndPoint, Parameter.CommInfo.TCPServer_NAME[0].ToString()));

                                Parameter.TCPSocket.SClient.ReadThread[0] = new Thread(new ThreadStart(Read_TCP_SClient1));
                                Parameter.TCPSocket.SClient.ReadThread[0].Start();
                            }
                            else
                            {
                                Parameter.TCPSocket.Server.tempClient[0].Close();
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_Server2()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(1000);

                    if (Parameter.TCPSocket.Server.isStop[1] == false)
                    {
                        if (APIManager.Status.TCPSClient.occupy[1] == false)
                        {
                            Parameter.TCPSocket.Server.tempClient[1] = Parameter.TCPSocket.Server.Listener[1].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.TCPSocket.Server.tempClient[1].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.TCPServer_CLIENT_IP[1].ToString())
                            {
                                var sKA2 = new Parameter.TCPSocket.Server.ka();
                                sKA2.onoff = 1;
                                sKA2.keepalivetime = 1000;
                                sKA2.keepaliveinterval = 500;

                                APIManager.Status.TCPSClient.occupy[1] = true;
                                Parameter.TCPSocket.SClient.Connector[1] = Parameter.TCPSocket.Server.tempClient[1];
                                Parameter.TCPSocket.SClient.Stream[1] = Parameter.TCPSocket.SClient.Connector[1].GetStream();
                                Parameter.TCPSocket.SClient.Connector[1].Client.IOControl(IOControlCode.KeepAliveValues, sKA2.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.TCPSocket.SClient.Connector[1].Client.RemoteEndPoint, Parameter.CommInfo.TCPServer_NAME[1].ToString()));

                                Parameter.TCPSocket.SClient.ReadThread[1] = new Thread(new ThreadStart(Read_TCP_SClient2));
                                Parameter.TCPSocket.SClient.ReadThread[1].Start();
                            }
                            else
                            {
                                Parameter.TCPSocket.Server.tempClient[1].Close();
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_Server3()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(1000);

                    if (Parameter.TCPSocket.Server.isStop[2] == false)
                    {
                        if (APIManager.Status.TCPSClient.occupy[2] == false)
                        {
                            Parameter.TCPSocket.Server.tempClient[2] = Parameter.TCPSocket.Server.Listener[2].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.TCPSocket.Server.tempClient[2].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.TCPServer_CLIENT_IP[2].ToString())
                            {
                                var sKA3 = new Parameter.TCPSocket.Server.ka();
                                sKA3.onoff = 1;
                                sKA3.keepalivetime = 1000;
                                sKA3.keepaliveinterval = 500;

                                APIManager.Status.TCPSClient.occupy[2] = true;
                                Parameter.TCPSocket.SClient.Connector[2] = Parameter.TCPSocket.Server.tempClient[2];
                                Parameter.TCPSocket.SClient.Stream[2] = Parameter.TCPSocket.SClient.Connector[2].GetStream();
                                Parameter.TCPSocket.SClient.Connector[1].Client.IOControl(IOControlCode.KeepAliveValues, sKA3.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.TCPSocket.SClient.Connector[2].Client.RemoteEndPoint, Parameter.CommInfo.TCPServer_NAME[2].ToString()));

                                Parameter.TCPSocket.SClient.ReadThread[2] = new Thread(new ThreadStart(Read_TCP_SClient3));
                                Parameter.TCPSocket.SClient.ReadThread[2].Start();
                            }
                            else
                            {
                                Parameter.TCPSocket.Server.tempClient[2].Close();
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_Server4()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(1000);

                    if (Parameter.TCPSocket.Server.isStop[3] == false)
                    {
                        if (APIManager.Status.TCPSClient.occupy[3] == false)
                        {
                            Parameter.TCPSocket.Server.tempClient[3] = Parameter.TCPSocket.Server.Listener[3].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.TCPSocket.Server.tempClient[3].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.TCPServer_CLIENT_IP[3].ToString())
                            {
                                var sKA4 = new Parameter.TCPSocket.Server.ka();
                                sKA4.onoff = 1;
                                sKA4.keepalivetime = 1000;
                                sKA4.keepaliveinterval = 500;

                                APIManager.Status.TCPSClient.occupy[3] = true;
                                Parameter.TCPSocket.SClient.Connector[3] = Parameter.TCPSocket.Server.tempClient[3];
                                Parameter.TCPSocket.SClient.Stream[3] = Parameter.TCPSocket.SClient.Connector[3].GetStream();
                                Parameter.TCPSocket.SClient.Connector[3].Client.IOControl(IOControlCode.KeepAliveValues, sKA4.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.TCPSocket.SClient.Connector[3].Client.RemoteEndPoint, Parameter.CommInfo.TCPServer_NAME[3].ToString()));

                                Parameter.TCPSocket.SClient.ReadThread[3] = new Thread(new ThreadStart(Read_TCP_SClient4));
                                Parameter.TCPSocket.SClient.ReadThread[3].Start();
                            }
                            else
                            {
                                Parameter.TCPSocket.Server.tempClient[3].Close();
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_Server5()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(1000);

                    if (Parameter.TCPSocket.Server.isStop[4] == false)
                    {
                        if (APIManager.Status.TCPSClient.occupy[4] == false)
                        {
                            Parameter.TCPSocket.Server.tempClient[4] = Parameter.TCPSocket.Server.Listener[4].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.TCPSocket.Server.tempClient[4].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.TCPServer_CLIENT_IP[4].ToString())
                            {
                                var sKA5 = new Parameter.TCPSocket.Server.ka();
                                sKA5.onoff = 1;
                                sKA5.keepalivetime = 1000;
                                sKA5.keepaliveinterval = 500;

                                APIManager.Status.TCPSClient.occupy[4] = true;
                                Parameter.TCPSocket.SClient.Connector[4] = Parameter.TCPSocket.Server.tempClient[4];
                                Parameter.TCPSocket.SClient.Stream[4] = Parameter.TCPSocket.SClient.Connector[4].GetStream();
                                Parameter.TCPSocket.SClient.Connector[4].Client.IOControl(IOControlCode.KeepAliveValues, sKA5.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.TCPSocket.SClient.Connector[4].Client.RemoteEndPoint, Parameter.CommInfo.TCPServer_NAME[4].ToString()));

                                Parameter.TCPSocket.SClient.ReadThread[4] = new Thread(new ThreadStart(Read_TCP_SClient5));
                                Parameter.TCPSocket.SClient.ReadThread[4].Start();
                            }
                            else
                            {
                                Parameter.TCPSocket.Server.tempClient[4].Close();
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_Server6()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(1000);

                    if (Parameter.TCPSocket.Server.isStop[5] == false)
                    {
                        if (APIManager.Status.TCPSClient.occupy[5] == false)
                        {
                            Parameter.TCPSocket.Server.tempClient[5] = Parameter.TCPSocket.Server.Listener[5].AcceptTcpClient();

                            if (((IPEndPoint)Parameter.TCPSocket.Server.tempClient[5].Client.RemoteEndPoint).Address.ToString() == Parameter.CommInfo.TCPServer_CLIENT_IP[5].ToString())
                            {
                                var sKA6 = new Parameter.TCPSocket.Server.ka();
                                sKA6.onoff = 1;
                                sKA6.keepalivetime = 1000;
                                sKA6.keepaliveinterval = 500;

                                APIManager.Status.TCPSClient.occupy[5] = true;
                                Parameter.TCPSocket.SClient.Connector[5] = Parameter.TCPSocket.Server.tempClient[5];
                                Parameter.TCPSocket.SClient.Stream[5] = Parameter.TCPSocket.SClient.Connector[5].GetStream();
                                Parameter.TCPSocket.SClient.Connector[5].Client.IOControl(IOControlCode.KeepAliveValues, sKA6.Buffer, null);

                                Console.WriteLine(string.Format("IP : {0}/{1} occupied", Parameter.TCPSocket.SClient.Connector[5].Client.RemoteEndPoint, Parameter.CommInfo.TCPServer_NAME[5].ToString()));

                                Parameter.TCPSocket.SClient.ReadThread[5] = new Thread(new ThreadStart(Read_TCP_SClient6));
                                Parameter.TCPSocket.SClient.ReadThread[5].Start();
                            }
                            else
                            {
                                Parameter.TCPSocket.Server.tempClient[5].Close();
                            }
                        }
                    }
                }
            }

            public static void Close_TCP_Server(int no)
            {
                Parameter.TCPSocket.Server.Listener[no].Server.Close();
                Parameter.TCPSocket.Server.ReadThread[no].Join();//Abort();
            }
            #endregion

            #region TCPSClient
            public static void Read_TCP_SClient1()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (APIManager.Status.TCPSClient.occupy[0] == true)
                    {
                        if (Parameter.TCPSocket.SClient.Stream[0].CanRead)
                        {
                            try
                            {
                                int len = Parameter.TCPSocket.SClient.Stream[0].Read(Parameter.TCPSocket.SClient.SC_buf1, 0, Parameter.TCPSocket.SClient.SC_buf1.Length);

                                if (len != 0 && len > 0)
                                {
                                    APIManager.Q.TCPSClient.Rcv_SCQ1.Enqueue(Parameter.TCPSocket.SClient.SC_buf1, len, Parameter.CommInfo.TCPServer_NAME[0]);
                                    Array.Clear(Parameter.TCPSocket.SClient.SC_buf1, 0, Parameter.TCPSocket.SClient.SC_buf1.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_TCP_SClient(0);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_TCP_SClient(0);
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_SClient2()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);

                    if (APIManager.Status.TCPSClient.occupy[1] == true)
                    {
                        if (Parameter.TCPSocket.SClient.Stream[1].CanRead)
                        {
                            try
                            {
                                int len = Parameter.TCPSocket.SClient.Stream[1].Read(Parameter.TCPSocket.SClient.SC_buf2, 0, Parameter.TCPSocket.SClient.SC_buf2.Length);

                                if (len != 0 && len > 0)
                                {
                                    APIManager.Q.TCPSClient.Rcv_SCQ2.Enqueue(Parameter.TCPSocket.SClient.SC_buf2, len, Parameter.CommInfo.TCPServer_NAME[1]);
                                    Array.Clear(Parameter.TCPSocket.SClient.SC_buf2, 0, Parameter.TCPSocket.SClient.SC_buf2.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_TCP_SClient(1);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_TCP_SClient(1);
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_SClient3()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);

                    if (APIManager.Status.TCPSClient.occupy[2] == true)
                    {
                        if (Parameter.TCPSocket.SClient.Stream[2].CanRead)
                        {
                            try
                            {
                                int len = Parameter.TCPSocket.SClient.Stream[2].Read(Parameter.TCPSocket.SClient.SC_buf3, 0, Parameter.TCPSocket.SClient.SC_buf3.Length);

                                if (len != 0 && len > 0)
                                {
                                    APIManager.Q.TCPSClient.Rcv_SCQ3.Enqueue(Parameter.TCPSocket.SClient.SC_buf3, len, Parameter.CommInfo.TCPServer_NAME[2]);
                                    Array.Clear(Parameter.TCPSocket.SClient.SC_buf3, 0, Parameter.TCPSocket.SClient.SC_buf3.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_TCP_SClient(2);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_TCP_SClient(2);
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_SClient4()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);

                    if (APIManager.Status.TCPSClient.occupy[3] == true)
                    {
                        if (Parameter.TCPSocket.SClient.Stream[3].CanRead)
                        {
                            try
                            {
                                int len = Parameter.TCPSocket.SClient.Stream[3].Read(Parameter.TCPSocket.SClient.SC_buf4, 0, Parameter.TCPSocket.SClient.SC_buf4.Length);

                                if (len != 0 && len > 0)
                                {
                                    APIManager.Q.TCPSClient.Rcv_SCQ4.Enqueue(Parameter.TCPSocket.SClient.SC_buf4, len, Parameter.CommInfo.TCPServer_NAME[3]);
                                    Array.Clear(Parameter.TCPSocket.SClient.SC_buf4, 0, Parameter.TCPSocket.SClient.SC_buf4.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_TCP_SClient(3);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_TCP_SClient(3);
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_SClient5()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);

                    if (APIManager.Status.TCPSClient.occupy[4] == true)
                    {
                        if (Parameter.TCPSocket.SClient.Stream[4].CanRead)
                        {
                            try
                            {
                                int len = Parameter.TCPSocket.SClient.Stream[4].Read(Parameter.TCPSocket.SClient.SC_buf5, 0, Parameter.TCPSocket.SClient.SC_buf5.Length);

                                if (len != 0 && len > 0)
                                {
                                    APIManager.Q.TCPSClient.Rcv_SCQ5.Enqueue(Parameter.TCPSocket.SClient.SC_buf5, len, Parameter.CommInfo.TCPServer_NAME[4]);
                                    Array.Clear(Parameter.TCPSocket.SClient.SC_buf5, 0, Parameter.TCPSocket.SClient.SC_buf5.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_TCP_SClient(4);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_TCP_SClient(4);
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_SClient6()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);

                    if (APIManager.Status.TCPSClient.occupy[5] == true)
                    {
                        if (Parameter.TCPSocket.SClient.Stream[5].CanRead)
                        {
                            try
                            {
                                int len = Parameter.TCPSocket.SClient.Stream[5].Read(Parameter.TCPSocket.SClient.SC_buf6, 0, Parameter.TCPSocket.SClient.SC_buf6.Length);

                                if (len != 0 && len > 0)
                                {
                                    APIManager.Q.TCPSClient.Rcv_SCQ6.Enqueue(Parameter.TCPSocket.SClient.SC_buf6, len, Parameter.CommInfo.TCPServer_NAME[5]);
                                    Array.Clear(Parameter.TCPSocket.SClient.SC_buf6, 0, Parameter.TCPSocket.SClient.SC_buf6.Length);
                                }
                                else if (len == 0 || len == -1)
                                {
                                    Close_TCP_SClient(5);
                                }
                            }
                            catch (Exception ex)
                            {
                                Close_TCP_SClient(5);
                            }
                        }
                    }
                }
            }

            public static void Close_TCP_SClient(int no)
            {
                if (APIManager.Status.TCPSClient.occupy[no] == true)
                {
                    Console.WriteLine(string.Format("SClient {0} Disconnected", Parameter.CommInfo.TCPServer_NAME[no]));
                    APIManager.Status.TCPSClient.occupy[no] = false;
                    Parameter.TCPSocket.SClient.Stream[no].Close();
                    Parameter.TCPSocket.SClient.Connector[no].Close();
                    Parameter.TCPSocket.SClient.ReadThread[no].Join();//Abort();
                }
            }
            #endregion

            #region TCPClient
            public static void Read_TCP_Client1()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.TCPSocket.Client.bThreadIsStop[0] == false)
                    {
                        if (Parameter.TCPSocket.Client.occupy[0] == true)
                        {
                            if (Parameter.TCPSocket.Client.Stream[0].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.TCPSocket.Client.Stream[0].Read(Parameter.TCPSocket.Client.C_buf1, 0, Parameter.TCPSocket.Client.C_buf1.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        APIManager.Q.TCPClient.Rcv_CCQ1.Enqueue(Parameter.TCPSocket.Client.C_buf1, len, Parameter.CommInfo.TCPClient_NAME[0]);
                                        Array.Clear(Parameter.TCPSocket.Client.C_buf1, 0, Parameter.TCPSocket.Client.C_buf1.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.TCPSocket.Client.bClosing[0] = true;
                                        Close_TCP_Client(0);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.TCPSocket.Client.bClosing[0] = true;
                                    Close_TCP_Client(0);
                                }
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_Client2()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.TCPSocket.Client.bThreadIsStop[1] == false)
                    {
                        if (Parameter.TCPSocket.Client.occupy[1] == true)
                        {
                            if (Parameter.TCPSocket.Client.Stream[1].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.TCPSocket.Client.Stream[1].Read(Parameter.TCPSocket.Client.C_buf2, 0, Parameter.TCPSocket.Client.C_buf2.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        APIManager.Q.TCPClient.Rcv_CCQ2.Enqueue(Parameter.TCPSocket.Client.C_buf2, len, Parameter.CommInfo.TCPClient_NAME[1]);
                                        Array.Clear(Parameter.TCPSocket.Client.C_buf2, 0, Parameter.TCPSocket.Client.C_buf2.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.TCPSocket.Client.bClosing[1] = true;
                                        Close_TCP_Client(1);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.TCPSocket.Client.bClosing[1] = true;
                                    Close_TCP_Client(1);
                                }
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_Client3()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.TCPSocket.Client.bThreadIsStop[2] == false)
                    {
                        if (Parameter.TCPSocket.Client.occupy[2] == true)
                        {
                            if (Parameter.TCPSocket.Client.Stream[2].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.TCPSocket.Client.Stream[2].Read(Parameter.TCPSocket.Client.C_buf3, 0, Parameter.TCPSocket.Client.C_buf3.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        APIManager.Q.TCPClient.Rcv_CCQ3.Enqueue(Parameter.TCPSocket.Client.C_buf3, len, Parameter.CommInfo.TCPClient_NAME[2]);
                                        Array.Clear(Parameter.TCPSocket.Client.C_buf3, 0, Parameter.TCPSocket.Client.C_buf3.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.TCPSocket.Client.bClosing[2] = true;
                                        Close_TCP_Client(2);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.TCPSocket.Client.bClosing[2] = true;
                                    Close_TCP_Client(2);
                                }
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_Client4()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.TCPSocket.Client.bThreadIsStop[3] == false)
                    {
                        if (Parameter.TCPSocket.Client.occupy[3] == true)
                        {
                            if (Parameter.TCPSocket.Client.Stream[3].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.TCPSocket.Client.Stream[3].Read(Parameter.TCPSocket.Client.C_buf4, 0, Parameter.TCPSocket.Client.C_buf4.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        APIManager.Q.TCPClient.Rcv_CCQ4.Enqueue(Parameter.TCPSocket.Client.C_buf4, len, Parameter.CommInfo.TCPClient_NAME[3]);
                                        Array.Clear(Parameter.TCPSocket.Client.C_buf4, 0, Parameter.TCPSocket.Client.C_buf4.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.TCPSocket.Client.bClosing[3] = true;
                                        Close_TCP_Client(3);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.TCPSocket.Client.bClosing[3] = true;
                                    Close_TCP_Client(3);
                                }
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_Client5()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.TCPSocket.Client.bThreadIsStop[4] == false)
                    {
                        if (Parameter.TCPSocket.Client.occupy[4] == true)
                        {
                            if (Parameter.TCPSocket.Client.Stream[4].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.TCPSocket.Client.Stream[4].Read(Parameter.TCPSocket.Client.C_buf5, 0, Parameter.TCPSocket.Client.C_buf5.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        APIManager.Q.TCPClient.Rcv_CCQ5.Enqueue(Parameter.TCPSocket.Client.C_buf5, len, Parameter.CommInfo.TCPClient_NAME[4]);
                                        Array.Clear(Parameter.TCPSocket.Client.C_buf5, 0, Parameter.TCPSocket.Client.C_buf5.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.TCPSocket.Client.bClosing[4] = true;
                                        Close_TCP_Client(4);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.TCPSocket.Client.bClosing[4] = true;
                                    Close_TCP_Client(4);
                                }
                            }
                        }
                    }
                }
            }

            public static void Read_TCP_Client6()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.TCPSocket.Client.bThreadIsStop[5] == false)
                    {
                        if (Parameter.TCPSocket.Client.occupy[5] == true)
                        {
                            if (Parameter.TCPSocket.Client.Stream[5].CanRead)
                            {
                                try
                                {
                                    int len = Parameter.TCPSocket.Client.Stream[5].Read(Parameter.TCPSocket.Client.C_buf6, 0, Parameter.TCPSocket.Client.C_buf6.Length);

                                    if (len != 0 && len > 0)
                                    {
                                        APIManager.Q.TCPClient.Rcv_CCQ6.Enqueue(Parameter.TCPSocket.Client.C_buf6, len, Parameter.CommInfo.TCPClient_NAME[5]);
                                        Array.Clear(Parameter.TCPSocket.Client.C_buf6, 0, Parameter.TCPSocket.Client.C_buf6.Length);
                                    }
                                    else if (len == 0 || len == -1)
                                    {
                                        Parameter.TCPSocket.Client.bClosing[5] = true;
                                        Close_TCP_Client(5);
                                    }
                                }
                                catch (Exception ex)
                                {
                                    Parameter.TCPSocket.Client.bClosing[5] = true;
                                    Close_TCP_Client(5);
                                }
                            }
                        }
                    }
                }
            }

            public static void Close_TCP_Client(int no)
            {
                if (Parameter.TCPSocket.Client.bPending[no] == false)
                {
                    Parameter.TCPSocket.Client.bPending[no] = true;

                    if (Parameter.TCPSocket.Client.occupy[no] == true)
                    {
                        Parameter.TCPSocket.Client.occupy[no] = false;
                        Console.WriteLine(string.Format("Client {0} Disconnected", Parameter.CommInfo.TCPClient_NAME[no]));

                        //  if(Parameter.TCPSocket.Client.Connector[no] != null && Parameter.TCPSocket.Client.Connector[no].Client != null && Parameter.TCPSocket.Client.ka_sync.ar[no].AsyncState != null)
                       
                         Parameter.TCPSocket.Client.Connector[no].Client.EndConnect(Parameter.TCPSocket.Client.ka_sync.ar[no]);
                        Parameter.TCPSocket.Client.bThreadIsStop[no] = true;
                        Parameter.TCPSocket.Client.Stream[no].Close();
                        Parameter.TCPSocket.Client.Connector[no].Close();

                    }

                    Parameter.TCPSocket.Client.occupy[no] = false;
                    Parameter.TCPSocket.Client.bClosing[no] = false;

                    Parameter.TCPSocket.Client.bPending[no] = false;
                }
            }

            public static void Connect_TCP_Client(int no, string ip, int port)
            {
                Parameter.TCPSocket.Client.Connector[no] = new TcpClient();

                Parameter.TCPSocket.Client.Connector[no].SendBufferSize = 1024000;

                var cKA = new Parameter.TCPSocket.Client.ka();
                cKA.onoff = 1;
                cKA.keepalivetime = 1000;
                cKA.keepaliveinterval = 500; // 클라이언트 접속상태(랜선뽑힘 등)를 확인하기 위해 IOControl을 위한 설정 

                Parameter.TCPSocket.Client.ka_sync.ar[no] = Parameter.TCPSocket.Client.Connector[no].BeginConnect(ip, port, null, null);
                System.Threading.WaitHandle wh = Parameter.TCPSocket.Client.ka_sync.ar[no].AsyncWaitHandle;

                try
                {

                    if (!Parameter.TCPSocket.Client.ka_sync.ar[no].AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(1), false)) Parameter.TCPSocket.Client.Connector[no].Close();

                   if (Parameter.TCPSocket.Client.ka_sync.ar[no].AsyncState != null)
                    Parameter.TCPSocket.Client.Connector[no].EndConnect(Parameter.TCPSocket.Client.ka_sync.ar[no]);
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fail to connect server Catched");
                }
                finally
                {
                    wh.Close();
                }

                /*
                if (success == true) Parameter.TCPSocket.Client.Connector[no].EndConnect(Parameter.TCPSocket.Client.ka_sync.ar[no]);
                else
                {
                    Parameter.TCPSocket.Client.Connector[no].Close();
                    Console.WriteLine("Fail to connect server");
                }
                
                */

          //      Parameter.TCPSocket.Client.Connector[no].EndConnect(Parameter.TCPSocket.Client.ka_sync.ar[no]);
//
                // }

            //    if (Parameter.TCPSocket.Client.ka_sync.ar[no].AsyncState != null)
           //         Parameter.TCPSocket.Client.Connector[no].EndConnect(Parameter.TCPSocket.Client.ka_sync.ar[no]);
                /*
                try
                {
                    //if (!)
                    // {
                    //    Parameter.TCPSocket.Client.Connector[no].Close();
                  //  Parameter.TCPSocket.Client.Connector[no].EndConnect(Parameter.TCPSocket.Client.ka_sync.ar[no]);

                    // }

                    if (Parameter.TCPSocket.Client.ka_sync.ar[no].AsyncState != null)
                        Parameter.TCPSocket.Client.Connector[no].EndConnect(Parameter.TCPSocket.Client.ka_sync.ar[no]);

                }
                catch (Exception ex)
                {
                    Console.WriteLine("Fail to connect server Catched");
                }
                finally
                {
                    wh.Close();
                }
                */
                if (Parameter.TCPSocket.Client.Connector[no].Client != null)
                {
                    if (Parameter.TCPSocket.Client.Connector[no].Client.Connected == true)
                    {
                        Console.WriteLine(string.Format("Client {0}/{1} Occupied", Parameter.CommInfo.TCPClient_NAME[no], ip));
                        Parameter.TCPSocket.Client.Connector[no].Client.IOControl(IOControlCode.KeepAliveValues, cKA.Buffer, null); //구미사격장에서는 시리얼하고 이더넷하고 두가지상태가있어서 하트비트로 체크해야되기때문에 이거 빼야됨
                                              
                        Parameter.TCPSocket.Client.Connector[no].NoDelay = true;
                        Parameter.TCPSocket.Client.bThreadIsStop[no] = false;
                        Parameter.TCPSocket.Client.Stream[no] = Parameter.TCPSocket.Client.Connector[no].GetStream();
                        Parameter.TCPSocket.Client.occupy[no] = true;  
                    }
                }

            }

            public static void Monitoring_Client()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(1000);

                    for (int i = 0; i < Parameter.CommInfo.iTCPClient_Amount; i++)
                    {
                        if (Parameter.TCPSocket.Client.occupy[i] == false && Parameter.TCPSocket.Client.bClosing[i] == false) Connect_TCP_Client(i, Parameter.CommInfo.TCPClient_IP[i], int.Parse(Parameter.CommInfo.TCPClient_PORT[i].ToString()));
                    }

                    DataManager.Monitoring_Serial();
                }
            }
            #endregion

            #region UDPServer
            public static void Read_UDP_SClient1()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(8);

                    byte[] buf;

                    if (Parameter.CommInfo.UDPSocket_MULTICAST[0] == "TRUE")
                    {
                        buf = Parameter.UDPSocket.Server.Connector[0].Receive(ref Parameter.UDPSocket.Server.ipepMC[0]);
                    }
                    else buf = Parameter.UDPSocket.Server.Connector[0].Receive(ref Parameter.UDPSocket.Server.ipepRead[0]);
                 //   Console.WriteLine("{0}", buf.Length);

                    if (Parameter.CommInfo.UDPSocket_SENDONLY[0] != "TRUE")
                    {
                        APIManager.Q.UDPSClient.Rcv_SCQ1.Enqueue(buf, buf.Length, Parameter.CommInfo.UDPSocket_NAME[0]);
                       // Array.Clear(Parameter.UDPSocket.SClient.SC_buf1, 0, Parameter.UDPSocket.SClient.SC_buf1.Length);
                    }

                    /*
                    if (Parameter.CommInfo.UDPSocket_MULTICAST[0] == "TRUE") Parameter.UDPSocket.SClient.SC_buf1 = Parameter.UDPSocket.Server.Connector[0].Receive(ref Parameter.UDPSocket.Server.ipepMC[0]);
                    else Parameter.UDPSocket.SClient.SC_buf1 = Parameter.UDPSocket.Server.Connector[0].Receive(ref Parameter.UDPSocket.Server.ipepRead[0]);
                    //Console.WriteLine("hrrr");
                    if (Parameter.CommInfo.UDPSocket_SENDONLY[0] != "TRUE")
                    {
                        APIManager.Q.UDPSClient.Rcv_SCQ1.Enqueue(Parameter.UDPSocket.SClient.SC_buf1, Parameter.UDPSocket.SClient.SC_buf1.Length, Parameter.CommInfo.UDPSocket_NAME[0]);
                        Array.Clear(Parameter.UDPSocket.SClient.SC_buf1, 0, Parameter.UDPSocket.SClient.SC_buf1.Length);
                    }*/
                }
            }

            public static void Read_UDP_SClient2()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.CommInfo.UDPSocket_MULTICAST[1] == "TRUE") Parameter.UDPSocket.SClient.SC_buf2 = Parameter.UDPSocket.Server.Connector[1].Receive(ref Parameter.UDPSocket.Server.ipepMC[1]);
                    else Parameter.UDPSocket.SClient.SC_buf2 = Parameter.UDPSocket.Server.Connector[1].Receive(ref Parameter.UDPSocket.Server.ipepRead[1]);

                    if (Parameter.UDPSocket.SClient.SC_buf2[0] != 0 && Parameter.CommInfo.UDPSocket_SENDONLY[1] != "TRUE")
                    {
                        APIManager.Q.UDPSClient.Rcv_SCQ2.Enqueue(Parameter.UDPSocket.SClient.SC_buf2, Parameter.UDPSocket.SClient.SC_buf2.Length, Parameter.CommInfo.UDPSocket_NAME[1]);
                        Array.Clear(Parameter.UDPSocket.SClient.SC_buf2, 0, Parameter.UDPSocket.SClient.SC_buf2.Length);
                    }
                }
            }

            public static void Read_UDP_SClient3()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.CommInfo.UDPSocket_MULTICAST[2] == "TRUE") Parameter.UDPSocket.SClient.SC_buf3 = Parameter.UDPSocket.Server.Connector[2].Receive(ref Parameter.UDPSocket.Server.ipepMC[2]);
                    else Parameter.UDPSocket.SClient.SC_buf3 = Parameter.UDPSocket.Server.Connector[2].Receive(ref Parameter.UDPSocket.Server.ipepRead[2]);

                    if (Parameter.UDPSocket.SClient.SC_buf3[0] != 0 && Parameter.CommInfo.UDPSocket_SENDONLY[2] != "TRUE")
                    {
                        APIManager.Q.UDPSClient.Rcv_SCQ3.Enqueue(Parameter.UDPSocket.SClient.SC_buf3, Parameter.UDPSocket.SClient.SC_buf3.Length, Parameter.CommInfo.UDPSocket_NAME[2]);
                        Array.Clear(Parameter.UDPSocket.SClient.SC_buf3, 0, Parameter.UDPSocket.SClient.SC_buf3.Length);
                    }
                }
            }

            public static void Read_UDP_SClient4()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.CommInfo.UDPSocket_MULTICAST[3] == "TRUE") Parameter.UDPSocket.SClient.SC_buf4 = Parameter.UDPSocket.Server.Connector[3].Receive(ref Parameter.UDPSocket.Server.ipepMC[3]);
                    else Parameter.UDPSocket.SClient.SC_buf4 = Parameter.UDPSocket.Server.Connector[3].Receive(ref Parameter.UDPSocket.Server.ipepRead[3]);

                    if (Parameter.UDPSocket.SClient.SC_buf4[0] != 0 && Parameter.CommInfo.UDPSocket_SENDONLY[3] != "TRUE")
                    {
                        APIManager.Q.UDPSClient.Rcv_SCQ4.Enqueue(Parameter.UDPSocket.SClient.SC_buf4, Parameter.UDPSocket.SClient.SC_buf4.Length, Parameter.CommInfo.UDPSocket_NAME[3]);
                        Array.Clear(Parameter.UDPSocket.SClient.SC_buf4, 0, Parameter.UDPSocket.SClient.SC_buf4.Length);
                    }
                }
            }

            public static void Read_UDP_SClient5()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.CommInfo.UDPSocket_MULTICAST[4] == "TRUE") Parameter.UDPSocket.SClient.SC_buf5 = Parameter.UDPSocket.Server.Connector[4].Receive(ref Parameter.UDPSocket.Server.ipepMC[4]);
                    else Parameter.UDPSocket.SClient.SC_buf5 = Parameter.UDPSocket.Server.Connector[4].Receive(ref Parameter.UDPSocket.Server.ipepRead[4]);

                    if (Parameter.UDPSocket.SClient.SC_buf5[0] != 0 && Parameter.CommInfo.UDPSocket_SENDONLY[4] != "TRUE")
                    {
                        APIManager.Q.UDPSClient.Rcv_SCQ5.Enqueue(Parameter.UDPSocket.SClient.SC_buf5, Parameter.UDPSocket.SClient.SC_buf5.Length, Parameter.CommInfo.UDPSocket_NAME[4]);
                        Array.Clear(Parameter.UDPSocket.SClient.SC_buf5, 0, Parameter.UDPSocket.SClient.SC_buf5.Length);
                    }
                }
            }

            public static void Read_UDP_SClient6()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(20);

                    if (Parameter.CommInfo.UDPSocket_MULTICAST[5] == "TRUE") Parameter.UDPSocket.SClient.SC_buf5 = Parameter.UDPSocket.Server.Connector[5].Receive(ref Parameter.UDPSocket.Server.ipepMC[5]);
                    else Parameter.UDPSocket.SClient.SC_buf6 = Parameter.UDPSocket.Server.Connector[5].Receive(ref Parameter.UDPSocket.Server.ipepRead[5]);

                    if (Parameter.UDPSocket.SClient.SC_buf6[0] != 0 && Parameter.CommInfo.UDPSocket_SENDONLY[5] != "TRUE")
                    {
                        APIManager.Q.UDPSClient.Rcv_SCQ6.Enqueue(Parameter.UDPSocket.SClient.SC_buf6, Parameter.UDPSocket.SClient.SC_buf6.Length, Parameter.CommInfo.UDPSocket_NAME[5]);
                        Array.Clear(Parameter.UDPSocket.SClient.SC_buf6, 0, Parameter.UDPSocket.SClient.SC_buf6.Length);
                    }
                }
            }
            #endregion

            #region Serial
            public static void Read_Serial1()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);

                    byte[] cmd = new byte[Parameter.Serial.Connector[0].BytesToRead];
                    int byteLen = Parameter.Serial.Connector[0].Read(cmd, 0, cmd.Length);
                    if (byteLen != 0) APIManager.Q.Serial.Rcv_SQ1.Enqueue(cmd, cmd.Length, Parameter.CommInfo.Serial_NAME[0]);
                    /*
                    int byteLen = Parameter.Serial.Connector[0].Read(Parameter.Serial.Serial_buf1, 0, Parameter.Serial.Serial_buf1.Length);
                    if (byteLen != 0)
                    {
                       // Console.WriteLine(byteLen);
                        byte[] cmd = new byte[byteLen];
                        APIManager.Q.Serial.Rcv_SQ1.Enqueue(cmd, cmd.Length, Parameter.CommInfo.Serial_NAME[0]);
                     //   Array.Clear(Parameter.Serial.Serial_buf1, 0, Parameter.Serial.Serial_buf1.Length);
                    }
                    */
                    
                    //  APIManager.Q.Serial.Rcv_SQ1.Enqueue(cmd, cmd.Length, Parameter.CommInfo.Serial_NAME[0]);

                      //  Console.WriteLine("[{0:x2}][{1:x2}][{2:x2}][{3:x2}]", cmd[0], cmd[1], cmd[2], cmd[3]);
                    
                        // APIManager.Q.Serial.Rcv_SQ1.Enqueue(cmd);
                    
                    /*
                    try
                    {
                      //  byte[] cmd = new byte[Parameter.Serial.Connector[0].BytesToRead];

                      //  int byteLen = Parameter.Serial.Connector[0].Read(cmd, 0, cmd.Length);
                        int byteLen = Parameter.Serial.Connector[0].Read(Parameter.Serial.Serial_buf1, 0, Parameter.Serial.Serial_buf1.Length);

                        if (byteLen != 0)
                        {
                            APIManager.Q.Serial.Rcv_SQ1.Enqueue(Parameter.Serial.Serial_buf1);

                        }

                    }
                    catch (Exception ex)
                    {
                    }
                    */
                    //  Console.WriteLine("rcv sq1 input");
                }
            }

            public static void Read_Serial2()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);

                    byte[] cmd = new byte[Parameter.Serial.Connector[1].BytesToRead];
                    int byteLen = Parameter.Serial.Connector[1].Read(cmd, 0, cmd.Length);
                    if (byteLen != 0) APIManager.Q.Serial.Rcv_SQ2.Enqueue(cmd, cmd.Length, Parameter.CommInfo.Serial_NAME[1]); 
                        // APIManager.Q.Serial.Rcv_SQ2.Enqueue(cmd);
                    /*
                    int byteLen = Parameter.Serial.Connector[1].Read(Parameter.Serial.Serial_buf2, 0, Parameter.Serial.Serial_buf2.Length);
                    if (byteLen != 0)
                    {
                        Console.WriteLine(byteLen);
                        byte[] cmd = new byte[byteLen];
                        APIManager.Q.Serial.Rcv_SQ2.Enqueue(cmd, cmd.Length, Parameter.CommInfo.Serial_NAME[1]);
                        //Array.Clear(Parameter.Serial.Serial_buf2, 0, Parameter.Serial.Serial_buf2.Length);
                    }*/
                    /*
                    try
                    {
                       // byte[] cmd = new byte[Parameter.Serial.Connector[1].BytesToRead];
                       // int byteLen = Parameter.Serial.Connector[1].Read(cmd, 0, cmd.Length);

                        int byteLen = Parameter.Serial.Connector[1].Read(Parameter.Serial.Serial_buf2, 0, Parameter.Serial.Serial_buf2.Length);

                        if (byteLen != 0)
                        {
                            APIManager.Q.Serial.Rcv_SQ2.Enqueue(Parameter.Serial.Serial_buf2);
                            //Console.WriteLine(string.Format("rcv sq2 : {0}", byteLen));
                        }
                    }
                    catch (Exception ex)
                    {
                    }*/
                }
            }

            public static void Read_Serial3()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);

                    /*
                    int byteLen = Parameter.Serial.Connector[2].Read(Parameter.Serial.Serial_buf3, 0, Parameter.Serial.Serial_buf3.Length);
                    if (byteLen != 0) APIManager.Q.Serial.Rcv_SQ3.Enqueue(Parameter.Serial.Serial_buf3, Parameter.Serial.Serial_buf3.Length, Parameter.CommInfo.Serial_NAME[2]);
                    Array.Clear(Parameter.Serial.Serial_buf3, 0, Parameter.Serial.Serial_buf3.Length);
                    */

                    byte[] cmd = new byte[Parameter.Serial.Connector[2].BytesToRead];
                    int byteLen = Parameter.Serial.Connector[2].Read(cmd, 0, cmd.Length);
                    if (byteLen != 0) APIManager.Q.Serial.Rcv_SQ3.Enqueue(cmd, cmd.Length, Parameter.CommInfo.Serial_NAME[2]); 

                      //  APIManager.Q.Serial.Rcv_SQ3.Enqueue(cmd);
                    
                    /*
                    try
                    {
                        //byte[] cmd = new byte[Parameter.Serial.Connector[2].BytesToRead];
                        //int byteLen = Parameter.Serial.Connector[2].Read(cmd, 0, cmd.Length);
                        int byteLen = Parameter.Serial.Connector[2].Read(Parameter.Serial.Serial_buf3, 0, Parameter.Serial.Serial_buf3.Length);

                        if (byteLen != 0)
                        {
                            APIManager.Q.Serial.Rcv_SQ3.Enqueue(Parameter.Serial.Serial_buf3);
                        }
                    }
                    catch (Exception ex)
                    {
                    }*/
                }
            }

            public static void Read_Serial4()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);
                    /*
                    int byteLen = Parameter.Serial.Connector[3].Read(Parameter.Serial.Serial_buf4, 0, Parameter.Serial.Serial_buf4.Length);
                    if (byteLen != 0) APIManager.Q.Serial.Rcv_SQ4.Enqueue(Parameter.Serial.Serial_buf4, Parameter.Serial.Serial_buf4.Length, Parameter.CommInfo.Serial_NAME[3]);
                    Array.Clear(Parameter.Serial.Serial_buf4, 0, Parameter.Serial.Serial_buf4.Length);
                    */
                    byte[] cmd = new byte[Parameter.Serial.Connector[3].BytesToRead];
                    int byteLen = Parameter.Serial.Connector[3].Read(cmd, 0, cmd.Length);//
                    if (byteLen != 0) APIManager.Q.Serial.Rcv_SQ4.Enqueue(cmd, cmd.Length, Parameter.CommInfo.Serial_NAME[3]); 
                        //APIManager.Q.Serial.Rcv_SQ4.Enqueue(cmd);
                    
                    /*
                    try
                    {
                        //byte[] cmd = new byte[Parameter.Serial.Connector[3].BytesToRead];
                       // int byteLen = Parameter.Serial.Connector[3].Read(cmd, 0, cmd.Length);
                        int byteLen = Parameter.Serial.Connector[3].Read(Parameter.Serial.Serial_buf4, 0, Parameter.Serial.Serial_buf4.Length);

                        if (byteLen != 0)
                        {
                            APIManager.Q.Serial.Rcv_SQ4.Enqueue(Parameter.Serial.Serial_buf4);
                        }
                    }
                    catch (Exception ex)
                    {
                    }*/
                }
            }

            public static void Read_Serial5()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);

/*
                    int byteLen = Parameter.Serial.Connector[4].Read(Parameter.Serial.Serial_buf5, 0, Parameter.Serial.Serial_buf5.Length);
                    if (byteLen != 0) APIManager.Q.Serial.Rcv_SQ5.Enqueue(Parameter.Serial.Serial_buf5, Parameter.Serial.Serial_buf5.Length, Parameter.CommInfo.Serial_NAME[4]);
                    Array.Clear(Parameter.Serial.Serial_buf5, 0, Parameter.Serial.Serial_buf5.Length);
                    */
                    byte[] cmd = new byte[Parameter.Serial.Connector[4].BytesToRead];
                    int byteLen = Parameter.Serial.Connector[4].Read(cmd, 0, cmd.Length);
                    if (byteLen != 0) APIManager.Q.Serial.Rcv_SQ5.Enqueue(cmd, cmd.Length, Parameter.CommInfo.Serial_NAME[4]); 
                       // APIManager.Q.Serial.Rcv_SQ5.Enqueue(cmd);
                    
                    /*
                    try
                    {
                      //  byte[] cmd = new byte[Parameter.Serial.Connector[4].BytesToRead];
                      //  int byteLen = Parameter.Serial.Connector[4].Read(cmd, 0, cmd.Length);
                        int byteLen = Parameter.Serial.Connector[4].Read(Parameter.Serial.Serial_buf5, 0, Parameter.Serial.Serial_buf5.Length);

                        if (byteLen != 0)
                        {
                            APIManager.Q.Serial.Rcv_SQ5.Enqueue(Parameter.Serial.Serial_buf5);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                     */ 
                }
            }

            public static void Read_Serial6()
            {
                while (!Param.bEndProcess)
                {
                    Thread.Sleep(30);

                    /*
                    int byteLen = Parameter.Serial.Connector[5].Read(Parameter.Serial.Serial_buf6, 0, Parameter.Serial.Serial_buf6.Length);
                    if (byteLen != 0) APIManager.Q.Serial.Rcv_SQ6.Enqueue(Parameter.Serial.Serial_buf6, Parameter.Serial.Serial_buf6.Length, Parameter.CommInfo.Serial_NAME[5]);
                    Array.Clear(Parameter.Serial.Serial_buf6, 0, Parameter.Serial.Serial_buf6.Length);
                    */
                    byte[] cmd = new byte[Parameter.Serial.Connector[5].BytesToRead];
                    int byteLen = Parameter.Serial.Connector[5].Read(cmd, 0, cmd.Length);
                    if (byteLen != 0) APIManager.Q.Serial.Rcv_SQ6.Enqueue(cmd, cmd.Length, Parameter.CommInfo.Serial_NAME[5]); 
                        //APIManager.Q.Serial.Rcv_SQ6.Enqueue(cmd);
                    
                    /*
                    try
                    {
                       // byte[] cmd = new byte[Parameter.Serial.Connector[5].BytesToRead];
                       // int byteLen = Parameter.Serial.Connector[5].Read(cmd, 0, cmd.Length);

                        int byteLen = Parameter.Serial.Connector[5].Read(Parameter.Serial.Serial_buf6, 0, Parameter.Serial.Serial_buf6.Length);

                        if (byteLen != 0)
                        {
                            APIManager.Q.Serial.Rcv_SQ6.Enqueue(Parameter.Serial.Serial_buf6);
                        }
                    }
                    catch (Exception ex)
                    {
                    }
                    */
                }
            }

            public static void Close_Serial(int no)
            {
                if (Parameter.Serial.occupy[no] == true)
                {
                    Parameter.Serial.ReadThread[no].Join();//Abort();
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

        internal class Parameter
        {           
            public class TCPSocket
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
            

                    public static ReaderWriterLockSlim cacheLock = new ReaderWriterLockSlim();

                    
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
                    public static bool[] bPending = new bool[6] { false, false, false, false, false, false };                   

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

            public class UDPSocket
            {
                public class Server
                {
                    public static IPEndPoint[] ipepRead = new IPEndPoint[6];
                    public static IPEndPoint[] ipepSend = new IPEndPoint[6];
                    public static IPEndPoint[] ipepMC = new IPEndPoint[6];
                    public static UdpClient[] Connector = new UdpClient[6];
                    public static Thread[] ReadThread = new Thread[6];
                    public static bool[] occupy = new bool[6] { false, false, false, false, false, false };
                }

                public class SClient
                {            
                    public static byte[] SC_buf1 = new byte[1024];
                    public static byte[] SC_buf2 = new byte[1024];
                    public static byte[] SC_buf3 = new byte[1024];
                    public static byte[] SC_buf4 = new byte[1024];
                    public static byte[] SC_buf5 = new byte[1024];
                    public static byte[] SC_buf6 = new byte[1024];
                }                
            }

            public class Serial
            {
                public static SerialPort[] Connector = new SerialPort[6];

                public static byte[] Serial_buf1 = new byte[1024];
                public static byte[] Serial_buf2 = new byte[1024];
                public static byte[] Serial_buf3 = new byte[1024];
                public static byte[] Serial_buf4 = new byte[1024];
                public static byte[] Serial_buf5 = new byte[1024];
                public static byte[] Serial_buf6 = new byte[1024];
                

                public static Thread[] ReadThread = new Thread[6];
                public static bool[] occupy = new bool[6] { false, false, false, false, false, false };
            }

            public class CommInfo
            {
                

                public static StringBuilder sbTCPServer_Amount = new StringBuilder(20);
                public static StringBuilder sbTCPClient_Amount = new StringBuilder(20);
                public static StringBuilder sbUDPSocket_Amount = new StringBuilder(20);
                public static StringBuilder sbSerial_Amount = new StringBuilder(20);
                

                public static int iTCPServer_Amount = 0;
                public static int iTCPClient_Amount = 0;
                public static int iUDPSocket_Amount = 0;
                public static int iSerial_Amount = 0;

                public static string[] TCPServer_NAME = new string[6];
                public static string[] TCPServer_IP = new string[6];
                public static string[] TCPServer_PORT = new string[6];
                public static string[] TCPServer_CLIENT_IP = new string[6];

                public static string[] UDPSocket_NAME = new string[6];
                public static string[] UDPSocket_SENDER_IP = new string[6];
                public static string[] UDPSocket_SENDER_PORT = new string[6];
                public static string[] UDPSocket_RECEIVER_IP = new string[6];
                public static string[] UDPSocket_RECEIVER_PORT = new string[6];
                public static string[] UDPSocket_MULTICAST = new string[6];
                public static string[] UDPSocket_SENDONLY = new string[6];
              //  public static string[] SClient_NAME = new string[6];
              //  public static string[] SClient_IP = new string[6];

                public static string[] TCPClient_NAME = new string[6];
                public static string[] TCPClient_IP = new string[6];
                public static string[] TCPClient_PORT = new string[6];

                public static string[] Serial_NAME = new string[6];
                public static string[] Serial_PORT = new string[6];
                public static string[] Serial_BR = new string[6];

                public static StringBuilder CommAuto = new StringBuilder(20);

                public static string NetworkPath = null; //= "C:\\CUSV\\config\\MPS.cfg";
                public static int Length = 20;

                public static string[] TCPserver = new string[6] { "TCPServer1", "TCPServer2", "TCPServer3", "TCPServer4", "TCPServer5", "TCPServer6" };               
                public static string[] TCPclient = new string[6] { "TCPClient1", "TCPClient2", "TCPClient3", "TCPClient4", "TCPClient5", "TCPClient6" };
                public static string[] UDPsocket = new string[6] { "UDPSocket1", "UDPSocket2", "UDPSocket3", "UDPSocket4", "UDPSocket5", "UDPSocket6" };
                public static string[] serial = new string[6] { "COM1", "COM2", "COM3", "COM4", "COM5", "COM6" };       
            }
        }
    }
}
