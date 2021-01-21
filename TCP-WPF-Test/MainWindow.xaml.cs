using System;
//using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
//using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net;
using System.Threading;
using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;
using System.Security.Principal;
using System.Windows.Controls;

namespace TCP_WPF_Test
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //AsynchronousClient asyncClient = new AsynchronousClient();
        LoginViewModel loginViewModel;
        public MainWindow()
        {
            InitializeComponent();
            //client = new TcpClient(hostName, portNum);
            //ConnectToTCP();
            loginViewModel = (LoginViewModel)this.DataContext;
        }

        private const int portNum = 11000;
        private const string hostName = "127.0.0.1";
        //private const int portNum = 26606;
        //private const string hostName = "naqad51.corp.lairdtech.com";
        const char STX = '\u0002';
        const char ETX = '\u0003';
        public TcpClient client;

        //private const int portNum = 26606;
        //private const string hostName = "naqad.corp.lairdtech.com";

        public static int ConnectToTCP()
        {
            byte[] bytes = new byte[1024];

            IPHostEntry host = Dns.GetHostEntry("localhost");
            IPAddress ipAddress = host.AddressList[0];
            IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

            // Create a TCP/IP  socket.    
            //Socket sender = new Socket(ipAddress.AddressFamily,
            //    SocketType.Stream, ProtocolType.Tcp);

            //sender.Connect(remoteEP);

            //Console.WriteLine("Socket connected to {0}",
            //        sender.RemoteEndPoint.ToString());

            //// Encode the data string into a byte array.    
            byte[] msg = Encoding.ASCII.GetBytes("This is a test<EOF>");

            //// Send the data through the socket.    
            //int bytesSent = sender.Send(msg);

            //// Receive the response from the remote device.    
            //int bytesRec = sender.Receive(bytes);
            //Console.WriteLine("Echoed test = {0}",
            //    Encoding.ASCII.GetString(bytes, 0, bytesRec));

            //// Release the socket.    
            //sender.Shutdown(SocketShutdown.Both);
            //sender.Close();


            //try
            //{
            var client = new TcpClient(host.AddressList[1].ToString(), portNum);

            NetworkStream ns = client.GetStream();
            ns.Write(msg, 0, msg.Length);

            int bytesRead = ns.Read(bytes, 0, bytes.Length);

            Debug.WriteLine(Encoding.ASCII.GetString(bytes, 0, bytesRead));

            client.Close();

            //}
            //catch (Exception e)
            //{
            //    Debug.WriteLine(e.ToString());
            //}
            return 0;
        }

        bool CmdResponse = false;
        string CmdMessage = "";
        string CmdCodes = "";
        string TCPresponse = "";

        private bool Login_Click()
        {
            client = new TcpClient(hostName, portNum);
            bool loginsuccess = false;
            string dbName = "QAD DB";

            if (UserTB.Text.Length <= 0)
            {
                Debug.Print("Username is blank.");
                return loginsuccess;
            }

            string qadprogram = "ttromt02.p";
            string runoptions = "SetUser" + "\t" + UserTB.Text + "\t" + PassTB.Password + "\t" + "14.13.1";
            string fullmsgstr = "_RUN_" + STX + qadprogram + ETX + runoptions + "\n";

            byte[] msg = Encoding.ASCII.GetBytes(fullmsgstr);

            if (SendCmd(msg) && CmdResponse == true)
            {
                string qadprogram2 = "ttdbname.p";
                string runoptions2 = UserTB.Text + "\t" + PassTB.Password + "\t" + "14.13.1";
                string fullmsgstr2 = "_RUN_" + STX + qadprogram2 + STX + runoptions2 + "\n";
                byte[] msg2 = Encoding.ASCII.GetBytes(fullmsgstr2);
                SendCmd(msg2);
                dbName = CmdMessage;
                Debug.Print("Logged into: " + dbName);

                Thread.Sleep(1000);
                fullmsgstr2 = "_RUN_" + STX + "ttromt02.p" + STX + "getRoutingData" + "\t" +
                    "4001" + "\t" + "4001" + "\t" + "11/15/2020" + "\t" + "\t" + "\t" + "\t" + "\t" + "\t" + '\u0010';
                byte[] msg3 = Encoding.ASCII.GetBytes(fullmsgstr2);
                SendCmd(msg3);
                Thread.Sleep(1000);
                string routingresult = TCPresponse;

                Debug.Print("Routing Result: " + routingresult);
                if (routingresult != "")
                {
                    List<Step> steplist = new List<Step>();
                    String[] split2steps = routingresult.Split('\u0014');
                    foreach (string eachstep in split2steps)
                    {
                        Debug.Print("Parsing Steps.");
                        if (eachstep != "" && !eachstep.Contains("\u0003") && !eachstep.Contains("\u0002"))
                        {
                            Step steptest = new Step();
                            steptest.ReadfromQAD(eachstep);
                            steplist.Add(steptest);
                            Debug.WriteLine(steptest.Operation);
                            Debug.WriteLine(steptest.Comments);
                        }
                    }
                    Debug.Print(split2steps.Count().ToString());
                    
                }
                Thread.Sleep(1000);
                byte[] quitmsg = Encoding.ASCII.GetBytes("_QUIT_");
                SendCmd(quitmsg);
            }
            else
            {
                Debug.Print("Can't Login.");
            }

            
            client.Close();
            return loginsuccess;
        }
        private bool SendCmd(byte[] msg)
        {
            bool TCPwriteSuccess = false;
            TCPresponse = "";
            CmdResponse = false;
            CmdMessage = "";
            CmdCodes = "";
            bool TCPreadSuccess = false;
            byte[] bytes = new byte[8192];


            if (client.Connected)
            {
                NetworkStream ns = client.GetStream();
                ns.WriteTimeout = 3000;
                ns.ReadTimeout = 3000;
                try
                {
                    ns.Write(msg, 0, msg.Length);
                    TCPwriteSuccess = true;

                    if (TCPwriteSuccess == true)
                    {
                        Debug.WriteLine("Sent. Awaiting Response.");
                        Thread.Sleep(200);
                        // Possible issues: 
                        // Data is too long
                        // 200 ms may not be long enough
                        // Client disconnect
                        //int bytesRead = ns.Read(bytes, 0, bytes.Length);

                        if (ns.DataAvailable)
                        {
                            int bytesRead = ns.Read(bytes, 0, bytes.Length);
                            Debug.WriteLine("Response: " + Encoding.ASCII.GetString(bytes, 0, bytesRead));
                            TCPresponse = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                            if (TCPresponse == "")
                            {
                                return false;
                            }
                            if (TCPresponse.Contains("\t"))
                            {
                                int TabPos = TCPresponse.IndexOf("\t");
                                if (TCPresponse.Substring(TCPresponse.Length - 1, 1) == "\n")
                                {
                                    TCPresponse = TCPresponse.Substring(0, TCPresponse.Length - 1);
                                }
                                if (TCPresponse.Contains("true"))
                                {
                                    CmdResponse = true;
                                }
                                TCPreadSuccess = true;
                                string vStr = TCPresponse.Substring(TabPos + 1); //from Tab position to end
                                int vPos = vStr.IndexOf(ETX);
                                if (vPos > 0)
                                {
                                    CmdMessage = vStr.Substring(0, vPos - 1);
                                    CmdCodes = vStr.Substring(vPos + 1);
                                }
                                else
                                {
                                    CmdMessage = vStr;
                                }
                            }
                        }
                    }
                    else
                    {
                        ns.Close();
                        return false;
                    }
                }
                catch
                {
                    ns.Close();
                    Debug.Print("Disconnected.");
                }
            }

            //client.Close();
            return TCPreadSuccess;
        }

        private void Button_Test(object sender, RoutedEventArgs e)
        {
            //ConnectToTCP();
            //asyncClient.SendData(UserTB.Text + "<EOF>");
            Login_Click();
        }
        //private void Button_Click1(object sender, RoutedEventArgs e)
        //{
        //    byte[] bytes = new byte[1024];

        //    IPHostEntry host = Dns.GetHostEntry("localhost");
        //    IPAddress ipAddress = host.AddressList[0];
        //    IPAddress ip2 = IPAddress.Parse("127.0.0.1");
        //    IPEndPoint remoteEP = new IPEndPoint(ipAddress, 11000);

        //    byte[] msg = Encoding.ASCII.GetBytes(UserTB.Text + "\t" + "<EOF>");

        //    var client = new TcpClient(host.AddressList[0].ToString(), portNum);

        //    NetworkStream ns = client.GetStream();
        //    ns.WriteTimeout = 3000;
        //    ns.ReadTimeout = 3000;
        //    ns.Write(msg, 0, msg.Length);

        //    Debug.WriteLine("Sent. Awaiting Response.");
        //    int bytesRead = ns.Read(bytes, 0, bytes.Length);

        //    Debug.WriteLine("Response: " + Encoding.ASCII.GetString(bytes, 0, bytesRead));
        //}


    }
    public class LoginViewModel : ViewBase
    {
        private string _Username;
        [Required]
        [RegularExpression("^[A-Za-z0-9.]*$")]
        public string Username
        {
            get { return _Username; }
            set
            {
                _Username = value;
                ClearErrors(nameof(Username));
                ValidateProperty(value);
                OnPropertyChanged();
            }
        }

        //private const int portNum = 26606;
        //private const string hostName = "naqad51.corp.lairdtech.com";
        private const int portNum = 11000;
        private const string hostName = "127.0.0.1";
        const char STX = '\u0002';
        const char ETX = '\u0003';
        public TcpClient client;

        //private User _CurrentUser;
        //public User CurrentUser
        //{
        //    get { return _CurrentUser; }
        //    set { _CurrentUser = value; OnPropertyChanged(); }
        //}

        private string _Message;
        public string Message
        {
            get { return _Message; }
            set { _Message = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get { return LoginCmd; } }
        public ICommand LoginCmd = null;  // This will be setup to point to a RelayCommand object.
        public bool CanLogin(object z)
        {
            bool LoginEnabled = this.HasErrors == false;
            if (LoginEnabled == true)
            {
                //PreviewMeasFile();
            }
            return LoginEnabled;
        }
        public void Login(object z)
        {
            client = new TcpClient(hostName, portNum);
            string dbName = "QAD DB";

            if (Username.Length <= 0)
            {
                Debug.Print("Username is blank.");
            }

            string qadprogram = "ttromt02.p";
            string runoptions = "SetUser" + "\t" + Username + "\t" + (z as PasswordBox).Password + "\t" + "14.13.1";
            string fullmsgstr = "_RUN_" + STX + qadprogram + ETX + runoptions + "\n";

            byte[] msg = Encoding.ASCII.GetBytes(fullmsgstr);

            if (SendCmd(msg) && CmdResponse == true)
            {
                string qadprogram2 = "ttdbname.p";
                string runoptions2 = Username + "\t" + (z as PasswordBox).Password + "\t" + "14.13.1";
                string fullmsgstr2 = "_RUN_" + STX + qadprogram2 + STX + runoptions2 + "\n";
                byte[] msg2 = Encoding.ASCII.GetBytes(fullmsgstr2);
                SendCmd(msg2);
                dbName = CmdMessage;
                Debug.Print("Logged into: " + dbName);

                Thread.Sleep(1000);
                fullmsgstr2 = "_RUN_" + STX + "ttromt02.p" + STX + "getRoutingData" + "\t" +
                    "4001" + "\t" + "4001" + "\t" + "11/15/2020" + "\t" + "\t" + "\t" + "\t" + "\t" + "\t" + '\u0010';
                byte[] msg3 = Encoding.ASCII.GetBytes(fullmsgstr2);
                try
                {
                    SendCmd(msg3);
                    Thread.Sleep(1000);
                    string routingresult = TCPresponse;

                    Debug.Print("Routing Result: " + routingresult);
                    if (routingresult != "")
                    {
                        List<Step> steplist = new List<Step>();
                        String[] split2steps = routingresult.Split('\u0014');
                        foreach (string eachstep in split2steps)
                        {
                            if (eachstep != "" && !eachstep.Contains("\u0003") && !eachstep.Contains("\u0002"))
                            {
                                Step steptest = new Step();
                                steptest.ReadfromQAD(eachstep);
                                steplist.Add(steptest);
                                Debug.WriteLine(steptest.Operation);
                                Debug.WriteLine(steptest.Comments);
                            }
                        }
                        Debug.Print(split2steps.Count().ToString());

                    }
                    Thread.Sleep(1000);
                    byte[] quitmsg = Encoding.ASCII.GetBytes("_QUIT_");
                    SendCmd(quitmsg);
                }
                catch
                {
                    byte[] quitmsg = Encoding.ASCII.GetBytes("_QUIT_");
                    SendCmd(quitmsg);
                }
            }
            else
            {
                byte[] quitmsg = Encoding.ASCII.GetBytes("_QUIT_");
                SendCmd(quitmsg);
                Debug.Print("Can't Login.");
            }


            client.Close();
        }


        bool CmdResponse = false;
        string CmdMessage = "";
        string CmdCodes = "";
        string TCPresponse = "";

        private bool SendCmd(byte[] msg)
        {
            bool TCPwriteSuccess = false;
            TCPresponse = "";
            CmdResponse = false;
            CmdMessage = "";
            CmdCodes = "";
            bool TCPreadSuccess = false;
            byte[] bytes = new byte[8192];


            if (client.Connected)
            {
                NetworkStream ns = client.GetStream();
                ns.WriteTimeout = 3000;
                ns.ReadTimeout = 3000;
                try
                {
                    ns.Write(msg, 0, msg.Length);
                    TCPwriteSuccess = true;

                    if (TCPwriteSuccess == true)
                    {
                        Debug.WriteLine("Sent. Awaiting Response.");
                        Thread.Sleep(200);
                        // Possible issues: 
                        // Data is too long
                        // 200 ms may not be long enough
                        // Client disconnect
                        //int bytesRead = ns.Read(bytes, 0, bytes.Length);

                        if (ns.DataAvailable)
                        {
                            int bytesRead = ns.Read(bytes, 0, bytes.Length);
                            Debug.WriteLine("Response: " + Encoding.ASCII.GetString(bytes, 0, bytesRead));
                            TCPresponse = Encoding.ASCII.GetString(bytes, 0, bytesRead);
                            if (TCPresponse == "")
                            {
                                return false;
                            }
                            if (TCPresponse.Contains("\t"))
                            {
                                int TabPos = TCPresponse.IndexOf("\t");
                                if (TCPresponse.Substring(TCPresponse.Length - 1, 1) == "\n")
                                {
                                    TCPresponse = TCPresponse.Substring(0, TCPresponse.Length - 1);
                                }
                                if (TCPresponse.Contains("true"))
                                {
                                    CmdResponse = true;
                                }
                                TCPreadSuccess = true;
                                string vStr = TCPresponse.Substring(TabPos + 1); //from Tab position to end
                                int vPos = vStr.IndexOf(ETX);
                                if (vPos > 0)
                                {
                                    CmdMessage = vStr.Substring(0, vPos - 1);
                                    CmdCodes = vStr.Substring(vPos + 1);
                                }
                                else
                                {
                                    CmdMessage = vStr;
                                }
                            }
                        }
                    }
                    else
                    {
                        ns.Close();
                        return false;
                    }
                }
                catch
                {
                    ns.Close();
                    Debug.Print("Disconnected.");
                }
            }

            //client.Close();
            return TCPreadSuccess;
        }

        //https://stackoverflow.com/questions/5309988/how-to-get-the-groups-of-a-user-in-active-directory-c-asp-net/61656557#61656557
        private List<string> GetGroups(string userName)
        {
            List<string> result = new List<string>();
            WindowsIdentity wi = new WindowsIdentity(userName);

            foreach (IdentityReference group in wi.Groups)
            {
                try
                {
                    result.Add(group.Translate(typeof(NTAccount)).ToString());
                }
                catch { }
            }
            result.Sort();
            return result;
        }

        public void DummyLogin(string Uname, string Pass)
        {
            //CurrentUser = new User();
            CurrentError = "";
            //AllRoles allRoles = new AllRoles();
            //if (Uname == "TestEngineer" && Pass == "Test")
            //{
            //    Message = "Welcome " + Username;
            //    CurrentUser.Name = Username;
            //    CurrentUser.Roles.Add(allRoles.EngRole);
            //    //PartStepView partStepView = new PartStepView();
            //    partStepView.Show();
            //    partStepView.partsViewModel.Roles.Add(allRoles.EngRole);
            //    partStepView.partsViewModel.Roles.Add(allRoles.OperatorRole);
            //    partStepView.partsViewModel.CurrentRole = allRoles.EngRole;
            //    partStepView.partsViewModel.CurrentUser = CurrentUser;
            //}
            //if (CurrentUser.Name == null)
            //{
            //    CurrentError = "Login Failed.";
            //}
        }

        public LoginViewModel()
        {
            Username = "";
            LoginCmd = new RelayCommand(Login, param => this.CanLogin(null));
        }

    }

    public class Step
    {
        public string RoutingCode { get; set; }
        public int Operation { get; set; }
        private Nullable<DateTime> StartDate { get; set; }
        private Nullable<DateTime> EndDate { get; set; }
        public string StandOp { get; set; }
        public string WorkCenter { get; set; }
        public string Machine { get; set; }
        public string Description { get; set; }
        private int MachPerOp { get; set; }
        private int OverlapUnits { get; set; }
        private float QueueTime { get; set; }
        private float WaitTime { get; set; }
        private bool Milestone { get; set; }
        private int SubcontractLT { get; set; }
        private float SetupCrew { get; set; }
        private float RunCrew { get; set; }
        public double SetupTime { get; set; }
        public double RunTime { get; set; }
        private float MoveTime { get; set; }
        private float YieldPerc { get; set; }
        private string ToolCode { get; set; }
        private string Supplier { get; set; }
        private float InvValue { get; set; }
        public double SubCost { get; set; }
        public string Comments { get; set; }
        private string WIP { get; set; }
        private string PurchaseOrder { get; set; }
        private int Line { get; set; }
        private bool MoveNext { get; set; }
        private bool AutoLaborReport { get; set; }
        private Nullable<DateTime> OrigStartDate { get; set; }
        public string Status { get; set; }
        public string BuyerPlanner { get; set; }
        public string ProdLine { get; set; }
        public string FormatforQAD()
        {
            const char MFLD = '\u0001';
            string mstr = RoutingCode + "\v" + "S" + MFLD;
            mstr = mstr + Operation.ToString() + "\v" + "I" + MFLD;
            mstr = mstr + StartDate.ToString() + "\v" + "D" + MFLD;
            mstr = mstr + EndDate.ToString() + "\v" + "D" + MFLD;
            mstr = mstr + StandOp + "\v" + "S" + MFLD;
            mstr = mstr + WorkCenter + "\v" + "S" + MFLD;
            mstr = mstr + Machine + "\v" + "S" + MFLD;
            mstr = mstr + Description + "\v" + "S" + MFLD;
            mstr = mstr + MachPerOp.ToString() + "\v" + "I" + MFLD;
            mstr = mstr + OverlapUnits.ToString() + "\v" + "I" + MFLD;
            mstr = mstr + QueueTime.ToString() + "\v" + "G" + MFLD;
            mstr = mstr + WaitTime.ToString() + "\v" + "G" + MFLD;
            mstr = mstr + "yes" + "\v" + "L" + MFLD;
            mstr = mstr + SubcontractLT.ToString() + "\v" + "I" + MFLD;
            mstr = mstr + SetupCrew.ToString() + "\v" + "G" + MFLD;
            mstr = mstr + RunCrew.ToString() + "\v" + "G" + MFLD;
            mstr = mstr + SetupTime.ToString() + "\v" + "G" + MFLD;
            mstr = mstr + RunTime.ToString() + "\v" + "F" + MFLD;
            mstr = mstr + MoveTime.ToString() + "\v" + "G" + MFLD;
            mstr = mstr + YieldPerc.ToString("000") + "\v" + "E" + MFLD;
            mstr = mstr + ToolCode + "\v" + "S" + MFLD;
            mstr = mstr + Supplier + "\v" + "S" + MFLD;
            mstr = mstr + InvValue.ToString() + "\v" + "G" + MFLD;
            mstr = mstr + SubCost.ToString() + "\v" + "G" + MFLD;
            mstr = mstr + Comments + "\v" + "X" + MFLD;
            mstr = mstr + WIP + "\v" + "S" + MFLD;
            mstr = mstr + PurchaseOrder + "\v" + "S" + MFLD;
            mstr = mstr + Line.ToString() + "\v" + "I" + MFLD;
            mstr = mstr + "yes" + "\v" + "L" + MFLD;
            mstr = mstr + "no" + "\v" + "L" + MFLD;
            mstr = mstr + OrigStartDate.ToString() + "\v" + "D" + MFLD;
            mstr = mstr + '\u0014';
            return mstr;
        }
        public void ReadfromQAD(string mstr)
        {
            const char MFLD = '\u0001';
            string[] splitstr = mstr.Split(MFLD);
            RoutingCode = splitstr[0].Split('\v')[0];
            Operation = Convert.ToInt32(splitstr[1].Split('\v')[0]);
            if (splitstr[2].Split('\v')[0] == "")
            {
                StartDate = null;
            }
            if (splitstr[3].Split('\v')[0] == "")
            {
                EndDate = null;
            }
            StandOp = splitstr[4].Split('\v')[0];
            WorkCenter = splitstr[5].Split('\v')[0];
            Machine = splitstr[6].Split('\v')[0];
            Description = splitstr[7].Split('\v')[0];
            MachPerOp = Convert.ToInt32(splitstr[8].Split('\v')[0]);
            OverlapUnits = Convert.ToInt32(splitstr[9].Split('\v')[0]);
            QueueTime = float.Parse(splitstr[10].Split('\v')[0]);
            SetupTime = Convert.ToDouble(splitstr[16].Split('\v')[0]);
            RunTime = Convert.ToDouble(splitstr[17].Split('\v')[0]);
            SubCost = Convert.ToDouble(splitstr[23].Split('\v')[0]);
            Comments = splitstr[24].Split('\v')[0];
        }
        public Step()
        {
            MachPerOp = 1;
            OverlapUnits = 0;
            QueueTime = 0;
            WaitTime = 0;
            Milestone = true;
            SubcontractLT = 0;
            SetupCrew = 0;
            RunCrew = 1;
            MoveTime = 0;
            YieldPerc = 100;
            ToolCode = "";
            Supplier = "";
            InvValue = 0;
            SubCost = 0;
            WIP = "";
            PurchaseOrder = "";
            Line = 0;
            MoveNext = true;
            AutoLaborReport = false;
        }
    }
}

