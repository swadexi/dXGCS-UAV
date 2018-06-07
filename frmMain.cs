using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO.Ports;
using System.Threading;
using GroundCS.Module;
using System.Drawing.Imaging;
using System.IO;
using System.Net;

using dxJoystick;
using dxSateliteView;
using subFunction;
using GEPlugin;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Xml;
using SimpleUdpReciever;
using System.Net.Sockets;
namespace GroundCS
{
  //  [ComVisibleAttribute(true)]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    delegate void SetTextCallback(string text);
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    public partial class frmMain : Form
    {   dxJoy Joys = new dxJoy ();
    dxSatView satelitView = new dxSatView();
    string apiURL;
    subFunction.Class1 dxSub = new subFunction.Class1();
    string EarthpathWebData = AppDomain.CurrentDomain.BaseDirectory + "Data\\splotchy_box.dae";
    private const string PLUGIN_URL =
        @"http://earth-api-samples.googlecode.com/svn/trunk/demos/desktop-embedded/pluginhost.html";
    
        WebBrowser webdocuments = new WebBrowser() ;
        private const string Model_URL =
            @"http://swadexi.googlecode.com/files/EasyStar.dae';";
        
        KmlModelCoClass model;
        KmlLookAtCoClass la ;
       // KmlLookAtCoClass lookAt ;           
        KmlPlacemarkCoClass placemark ;            
        KmlLocationCoClass loc ;
        StringBuilder output = new StringBuilder();
        KmlOrientationCoClass planeOrient;
        bool isJoyUpdate = true;
        private IGEPlugin mge = null; bool isGEReady;
        private Bitmap image = null; int j; private Bitmap imageServoAil = null; private Bitmap imageServoElv = null; private Bitmap imageServoRud = null;
        private float angle = 0.0f; bool freeView;
        int wpCnt; Int32 dataLogCounter; int NumberDatalogFile; Int32 playDatalogCounter; string[] dataLogC = new string[65000]; string[] apiLog = new string[65000];
        String RxString; string[] InfoMap = new string[11]; string[] WebInfoMap = new string[15];
        Class2 SubF = new Class2(); String pathWebData; string[] BufferData = new string[1000];
        int[] bufferInt = new int[1000]; string[] waypoints = new string[2000]; UInt32 TrackCNT;
        int[] bufferTrackX = new int[10000]; int[] bufferTrackY = new int[10000];
        int curTop, lastTop, curLeft, lastLeft; int srt;
        
        const int Lat = 1; const int Lon = 2; byte[] sData = new byte[52]; 
        //Declare All Memory Buffer Address
        const int preUrl = 99; const int postUrl = 100; const int fixUrl = 98;
        const int MapX = 1; const int MapY = 2; string varX, varY;
       // UInt32 TrackCNT = 0; 
        private  PictureBox[] imgWP;
        private System.Drawing.Pen[] cLine;
        double[] dataEarth = new double[20];
        byte[] dxData = new byte[10];
        double[] dtEarth = new double[5]; bool isRecording = false;
        double servoVal;
        public frmMain()
        {
            
            InitializeComponent();
            
            imgWP = new PictureBox[300];
            cLine = new System.Drawing.Pen[300];
                           
        }
        
        private void cmdOpenMao_Click(object sender, EventArgs e)
        {   
            OpenD.Filter = "dxMap files|*.bmp";
            if (OpenD.ShowDialog() == DialogResult.OK)
            {   
                Map.ImageLocation = OpenD.FileName;                  
                ramMap.Text =  SubF.OpenData(OpenD.FileName + ".info");                
            }
        }

        private void UDP_data()
        {
            int localPort = 6100;
            IPEndPoint remoteSender = new IPEndPoint(IPAddress.Any, 0);
           

            UdpClient client = new UdpClient(localPort);
            UdpState state = new UdpState(client, remoteSender);
            // Start async receiving
            client.BeginReceive(new AsyncCallback(DataReceived), state);

            // Wait for any key to terminate application
           
        }

        private  void DataReceived(IAsyncResult ar)
        {
            subFunction.Class1 dxSub = new subFunction.Class1();
            UdpClient c = (UdpClient)((UdpState)ar.AsyncState).c;
            IPEndPoint wantedIpEndPoint = (IPEndPoint)((UdpState)(ar.AsyncState)).e;
            IPEndPoint receivedIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
            Byte[] receiveBytes = c.EndReceive(ar, ref receivedIpEndPoint);
            double dataBytes, dataBytes2, dataBytes3;
            // Check sender
            bool isRightHost = (wantedIpEndPoint.Address.Equals(receivedIpEndPoint.Address)) || wantedIpEndPoint.Address.Equals(IPAddress.Any);
            bool isRightPort = (wantedIpEndPoint.Port == receivedIpEndPoint.Port) || wantedIpEndPoint.Port == 0;
            if (isRightHost && isRightPort)
            {
                dataBytes = BitConverter.ToDouble(receiveBytes,0);
                dataBytes2 = BitConverter.ToDouble(receiveBytes, 8);
                dataBytes3 = BitConverter.ToDouble(receiveBytes, 16);
                // Convert data to ASCII and print in console
                string receivedText = ASCIIEncoding.ASCII.GetString(receiveBytes);
                string receivedText2 = Encoding.ASCII.GetString(receiveBytes);
                //Console.Write(receivedText);
                //string RxString = (dxSub.ToAscii(receivedText)).ToString("G");     
                string All = Convert.ToString(dataBytes) + "||" + Convert.ToString(dataBytes2) + "||" + Convert.ToString(dataBytes3);    
                SetText(All);
            }

            // Restart listening for udp data packages
            c.BeginReceive(new AsyncCallback(DataReceived), ar.AsyncState);

        }

       

        private void SetText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            if (this.RawData.InvokeRequired)
            {
                SetTextCallback d = new SetTextCallback(SetText);
                this.Invoke(d, new object[] { text });
            }
            else
            {
                this.RawData.Text = text;
            }
        }

        private static string GetValue(string[] args, ref int i)
        {
            string value = String.Empty;
            if (args.Length >= i + 2)
            {
                i++;
                value = args[i];
            }
            return value;
        }
        private void Serial_DataReceived(object sender,  System.IO.Ports.SerialDataReceivedEventArgs e)
        {
         try
            {
                RxString = "";
                Serial.Encoding = Encoding.GetEncoding(28591);
                RxString = Serial.ReadExisting();
                this.Invoke(new EventHandler(DisplayText));
            }
            catch { ;}
         /*   try
            {
               RxString = "";
               byte[] buffer = new byte[Serial.BytesToRead];
               Serial.Read(buffer, 0, Serial.BytesToRead);

                 if (buffer.Count() == 53)
                 {
                     for (int cnter = 0; cnter < 53; cnter++)
                     {
                         sData[cnter] = buffer[cnter];
                         RxString += Convert.ToString(Convert.ToChar(sData[cnter]));
                     }
                     this.Invoke(new EventHandler(DisplayText));
                 }
             
            }
            catch { ;}*/
          /*   try
            {
                RxString = "";
                Serial.Encoding = Encoding.GetEncoding(28591);
                RxString = Serial.ReadExisting();
                this.Invoke(new EventHandler(DisplayText2));
            }
            catch { ;}*/

        }

    
       

        private void DisplayText(object sender, EventArgs e)
        {
            
             RawData.Text = RxString;
             if (RxString.Length == 53) 
              { 
                  if (isRecording == true)
                  {
                      dataLogCounter++;
                      if (dataLogCounter == 1)
                      {
                          System.IO.File.WriteAllText(dlgSaveRec.FileName, RxString);
                      }
                      else if (dataLogCounter == 100000)
                      {
                          dataLogCounter = 0;
                          NumberDatalogFile++;
                      }
                      else
                      {
                          using (System.IO.StreamWriter file = new System.IO.StreamWriter(dlgSaveRec.FileName , true))
                          {
                              file.WriteLine(RxString);
                          }
                      }
                  } 
                 
                 ParseData(RxString);  }
             else
             { ; }
         }
        private void ParseData(String Data)
        {
           
            Int32 CRC=0;bool CRCOK;
            CRCOK = false;
            
            if (Data.Substring(0, 5) == "dxURO")
            {
                for (int cx = 5; cx < Data.Length-2; cx++)
                {
                   CRC = CRC^sData[cx];                    
                }
                Int32 tempCRC = sData[50];             
                
              //  textBox3.Text = Data.Substring(30, 21);
              //  textBox4.Text = Convert.ToString(tempCRC);
          //      textBox5.Text = Convert.ToString(CRC); ; 
                if (CRC == tempCRC)
                { CRCOK = true; textBox3.Text = "CRC OK"; }
                else { ;}
                
            }
            if (CRCOK == true )
            {
                string parser;
                parser = Data.Substring(5, 1);
                Action(1, parser);
                parser = Data.Substring(6, 2);
                Action(2, parser);
                     parser = Data.Substring(8, 9);
                     Action(3, parser);
                     parser = Data.Substring(18, 10);
                     Action(4, parser);
                     parser = Data.Substring(29, 2);
                     Action(5, parser);
                     parser = Data.Substring(31, 5);
                     Action(6, parser);
                     parser = Data.Substring(36, 3);
                     Action(7, parser);   
                     parser = Data.Substring(40, 3);
                     Action(8, parser);
                     parser = Data.Substring(43, 6);
                     Action(9, parser);
                     parser = Data.Substring(49, 1);
                     Action(10, parser);
                     parser = Data.Substring(50, 1);
                     Action(11, parser);
                     parser = Data.Substring(51, 1);
                     Action(12, parser);
            }           


        }
        public void Action(int Act, String Data2)
        {

          try
            {
                decimal floatData;
                switch (Act)
                {
                    case 1:
                        if (Data2 == "1") { FMode.Text = "RC"; }
                        else if (Data2 == "3") { FMode.Text = "Auto";}
                        else { FMode.Text = "Joystick"; }
                        break;
                    case 2:
                        String A = Data2.Substring(0, 1);
                        Int32 dRoll = Convert.ToChar(A);
                        textBox3.Text = dRoll.ToString("G");
                        A = Data2.Substring(1, 1);
                        Int32 dPitch = Convert.ToChar(A);
                        textBox4.Text = Convert.ToString(dPitch);


                        attitudeIndicatorInstrumentControl1.SetAttitudeIndicatorParameters((dPitch - 150) * 0.705, (dRoll - 150) * 1.41);
                       // txtRoll.Text = Convert.ToString((dRoll - 150) * 1.41);
                      //  txtPitch.Text = Convert.ToString((dPitch - 150) * 0.705);
                        chart1.Series[0].Points.Add(Convert.ToDouble(dRoll));
                        dataEarth[4] = Convert.ToInt32((dRoll - 128) * 1.41);
                        dataEarth[3] = Convert.ToInt32((dPitch - 128) * 0.705);
                        
                       
                        if (chart1.Series[0].Points.Count > 20)
                        {
                            chart1.Series[0].Points.RemoveAt(0);
                        }

                        chart1.Series[1].Points.Add(Convert.ToDouble(dPitch));
                        if (chart1.Series[1].Points.Count > 20)
                        {
                            chart1.Series[1].Points.RemoveAt(0);
                        }   
                        break;
                    case 3:
                        try
                        {
                            lastTop = curTop;
                            dtEarth[3] = dtEarth[1];
                            BufferData[20] = Data2.Substring(0, 2);
                            BufferData[21] = Convert.ToString(Convert.ToDecimal(Data2.Substring(2, 7)) / 60);
                            floatData = (Convert.ToDecimal(BufferData[20]) + Convert.ToDecimal(BufferData[21]));
                            BufferData[20] = Convert.ToString(System.Math.Round(floatData, 6));

                            dtEarth[1] =Convert.ToDouble( System.Math.Round(floatData, 6) * -1);
                            GPSLat.Text = "LAT :" + dtEarth[1];
                            floatData = (floatData + Convert.ToDecimal(InfoMap[Lat])) / Convert.ToDecimal(InfoMap[3]);
                            floatData = floatData - (UAV.Height / 2);
                            UAV.Top = Convert.ToInt32(floatData);
                            curTop = UAV.Top + (UAV.Height / 2);                            
                        }
                        catch
                        { ;}
                        break;
                    case 4:
                        try
                        {

                            lastLeft = curLeft;
                            dtEarth[4] = dtEarth[2];
                            BufferData[20] = Data2.Substring(0, 3);
                            BufferData[21] = Convert.ToString(Convert.ToDecimal(Data2.Substring(3, 7)) / 60);
                            floatData = (Convert.ToDecimal(BufferData[20]) + Convert.ToDecimal(BufferData[21]));
                            BufferData[20] = Convert.ToString(System.Math.Round(floatData, 6));
                            GPSLong.Text = "LON :" + BufferData[20];
                            dtEarth[2] = Convert.ToDouble(System.Math.Round(floatData, 6));
                            floatData = (floatData - Convert.ToDecimal(InfoMap[Lon])) / Convert.ToDecimal(InfoMap[4]);
                            floatData = floatData - (UAV.Width / 2);
                            UAV.Left = Convert.ToInt32(floatData);
                            curLeft = UAV.Left + (UAV.Width / 2);

                                                     
                                TrackCNT++;
                                bufferTrackX[TrackCNT] = curLeft;
                                bufferTrackY[TrackCNT] = curTop;
                            
                        }
                        catch
                        { ;}
                        break;
                    case 5:
                        lblSat.Text = "STLT : " + Data2;
                        dxSatView1.setView = Convert.ToInt16(Data2);
                        break;
                    case 6:
                        floatData = Convert.ToDecimal(Data2);
                        lblAlt.Text = "ALT : " + Convert.ToString(floatData);
                        altimeterInstrumentControl1.SetAlimeterParameters(Convert.ToInt16(Convert.ToInt16(floatData)));
                        chart2.Series[0].Points.Add (Convert.ToDouble (floatData ));
                        dataEarth[5] = Convert.ToInt32(floatData);
                        if (chart2.Series[0].Points.Count > 20)
                        {
                            chart2.Series[0].Points.RemoveAt(0);
                        }        
                        break;
                    case 7://Speed
                        Int32 Asz = Convert.ToInt32(Convert.ToDecimal(Data2) * 10);
                        airSpeedIndicatorInstrumentControl1.SetAirSpeedIndicatorParameters(Asz);
                        lblSpeed.Text = "Vms :" + Data2 + " m/s";
                        break;
                    case 8:
                        headingIndicatorInstrumentControl1.SetHeadingIndicatorParameters(Convert.ToInt16(Data2));
                        angle = (float)Convert.ToInt16(Data2);
                        lblHDG.Text = "HDG : " + Convert.ToString(angle);                        
                        RotateImage(UAV, image, angle);
                        dataEarth[2] = Convert.ToInt32(angle);
                       // updateAttitude(dtEarth[1], dtEarth[2], dataEarth[2], dataEarth[3], dataEarth[4], dataEarth[5]);
                    //   webMap.Document.InvokeScript("setLine", new object[] { dtEarth[3], dtEarth[4], dtEarth[1], dtEarth[2] });
                       // webMap.Document.InvokeScript("setLine", new object[] {});
                     webEarth.Document.InvokeScript("updateAttitude",new object[] { dtEarth[1], dtEarth[2], dataEarth[2], dataEarth[3], dataEarth[4], dataEarth[5]});
                    
                        break;
                    case 9:
                        for (int i = 0; i < 6; i++)
                        {
                            double xServo = dxSub.ToAscii (Data2.Substring(i, 1));
                            if (xServo < 90)
                            { xServo = 90; }                           
                            switch (i)
                            {                                   
                                case 0:
                                    Servo1.Value = Convert.ToInt16(xServo) * 10;
                                    servoVal =(xServo-90)*1.0/120  * 180;
                                    RotateServo(AilServo, imageServoAil, servoVal);                                   
                                    break;
                                case 1:
                                    Servo2.Value = Convert.ToInt16(xServo) * 10;
                                    servoVal =(xServo-90)*1.0/120  * 180;
                                    RotateServo(RudServo, imageServoRud, servoVal);                                    break;
                                case 2:
                                    Servo3.Value = Convert.ToInt16(xServo) * 10;
                                    break;
                                case 3:
                                    Servo4.Value =Convert.ToInt16(xServo) * 10;
                                    servoVal =(xServo-90)*1.0/120  * 180;
                                    RotateServo(ElvServo, imageServoElv, servoVal);
                                    break;                                    
                            }
                            
                        }
                        break;
                    case 10:
                        Int32 Battery = Convert.ToChar(Data2);
                        if (Battery > 215)
                        {
                            Bat.BarColor = Color.LightGreen;
                            Bat.Value = Battery;
                            floatData = System.Math.Round(Convert.ToDecimal(Battery) / 21, 2);
                            baterai.ForeColor = Color.LightGreen;
                            baterai.Text = Convert.ToString(floatData);
                        }
                        else
                        {
                            Bat.BarColor = Color.Red;
                            Bat.Value = Battery;
                            floatData = System.Math.Round(Convert.ToDecimal(Battery) / 21, 2);
                            baterai.ForeColor = Color.Red;
                            baterai.Text = Convert.ToString(floatData);
                        }
                        break;
                    case 11:
                        if ((dxSub.ToAscii(Data2)-48) == 0)
                        {   statusBar.Items[0].Text = "Target Waypoint = Home ";     }
                        else
                        {   statusBar.Items[0].Text = " Target Waypoint : " + (dxSub.ToAscii(Data2) - 48).ToString("G") ;   }
                        break;
                    case 12:                        
                        statusBar.Items[1].Text = "Distance From Waypoint : " + (dxSub.ToAscii(Data2) - 10).ToString ("G");                       
                        break;

                       

                }
            }
            catch
            { ;}
        }
        void frmMain_FormClosing(object sender, System.Windows.Forms.FormClosingEventArgs e)
        {
          

                if (DialogResult.Yes != MessageBox.Show(

                    "Serial Port is Still Avtive",

                    "Close Application?",

                     MessageBoxButtons.YesNo,

                     MessageBoxIcon.Question,

                     MessageBoxDefaultButton.Button2))
                {

                    e.Cancel = true;

                }
            
        }
        
        private void frmMain_Load(object sender, EventArgs e)
        {
           
            VideoCap.CaptureHeight = VideoBox.Height;
            VideoCap.CaptureWidth = VideoBox.Width;
            lblSat.Parent = Map;
            lblSat.Top = 40;
            lblSat.Left = 390;
            lblHDG.Parent = Map;
            lblHDG.Left = 390;
            lblHDG.Top = 20;
            lblSpeed.Parent = Map;
            lblSpeed.Left = 250;
            lblSpeed.Top = 20;
            lblAlt.Parent = Map;
            lblAlt.Left = 250;
            lblAlt.Top = 40;
            isJoyUpdate = true;
            image = new Bitmap(UAV.Image );
            imageServoAil = new Bitmap(AilServo.Image);
            imageServoRud = new Bitmap(RudServo.Image);
            imageServoElv = new Bitmap(ElvServo.Image);
            //textBox5.Text = Convert.ToString ( Convert.ToChar(255)); to chr
            UAV.Parent = Map;
            lblcurLat.Parent = Map;
            lblcurLon.Parent = Map;
            GPSLong.Parent = Map;
            GPSLat.Parent = Map;
            GPSLong.Left = 20;
            GPSLong.Top = 40;
            GPSLat.Left = 20;
            GPSLat.Top = 20;
            lblcurLat.Left = 60;
            lblcurLat.Top = Map.Height - 60;
            lblcurLon.Left = 250;
            lblcurLon.Top = Map.Height - 60;     
            Serial.DataReceived += new SerialDataReceivedEventHandler(Serial_DataReceived);
            GetSerialDevice();
            string webMapURL = AppDomain.CurrentDomain.BaseDirectory + "Data\\map.html";
         

            
        }
        public void GetSerialDevice()
        {
            
	    // Get a list of serial port names.        
        String[] ports = SerialPort.GetPortNames();
        foreach ( String portT in ports) {PortList.Items.Add(portT);}
        PortList.SelectedIndex  = PortList.Items.Count - 1;
        }
        
        private void OpenD_FileOk(object sender, CancelEventArgs e)
        {

        }

        private void ramMap_TextChanged(object sender, EventArgs e)
        {

        }

        private void Map_Click(object sender, EventArgs e)
        {
            
        }
        private void Map_MouseDown(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            bufferInt[MapX] = e.X;
            bufferInt[MapY] = e.Y;
            if (e.Button == MouseButtons.Left || e.Button == MouseButtons.Right)
            { MenuX.Show(this, new Point(e.X + Map.Left , e.Y + Map.Top)); }
        }

        private void Map_MouseMove(object sender, MouseEventArgs e)
        {
            if (Map.Image  == null) { ;}
            else
            { getMapInfo();
         
            decimal DataX =System .Math .Round ( Convert.ToDecimal(InfoMap[Lat]) - (e.Y * Convert.ToDecimal(InfoMap[3])),8);
            lblcurLat.Text = " Lat : " + DataX.ToString("G");
            DataX = System .Math .Round (Convert.ToDecimal(InfoMap[Lon]) + (e.X * Convert.ToDecimal(InfoMap[4])),8);
            lblcurLon.Text = " Long : " + DataX.ToString("G");
            }
        }

        private void getMapInfo()
        {
            try
            {
                String MapData = ramMap.Text;
                InfoMap[6] = MapData.Substring(0, 2);
                InfoMap[7] = MapData.Substring(2, 2);
                InfoMap[8] = MapData.Substring(4, 2);
                InfoMap[9] = MapData.Substring(6, 2);
                InfoMap[10] = MapData.Substring(8, 2);

                InfoMap[1] = MapData.Substring(14, int.Parse(InfoMap[6]));  //Latitude
                InfoMap[2] = MapData.Substring((14 + int.Parse(InfoMap[6])), int.Parse(InfoMap[7]));  //Longitude
                InfoMap[3] = MapData.Substring((14 + int.Parse(InfoMap[6]) + int.Parse(InfoMap[7])), int.Parse(InfoMap[8])); //pxLat
                InfoMap[4] = MapData.Substring((14 + int.Parse(InfoMap[6]) + int.Parse(InfoMap[7]) + int.Parse(InfoMap[8])), int.Parse(InfoMap[9])); // pxLong
                InfoMap[5] = MapData.Substring((14 + int.Parse(InfoMap[6]) + int.Parse(InfoMap[7]) + int.Parse(InfoMap[8]) + int.Parse(InfoMap[9])), int.Parse(InfoMap[10])); // Zoom
            }
            catch {; }

        }

        private void cmdGo_Click(object sender, EventArgs e)
        {
            pathWebData = AppDomain.CurrentDomain.BaseDirectory + "Data\\GEC.html";
            BufferData[preUrl] = SubF.OpenData(AppDomain.CurrentDomain.BaseDirectory + "\\Data\\PC2L.Dat");
            BufferData[postUrl] = SubF.OpenData(AppDomain.CurrentDomain.BaseDirectory + "\\Data\\PC3L.Dat");
            BufferData[fixUrl] = BufferData[preUrl] + "map.setCenter(new GLatLng(" + Latx.Text +  "," + Longx.Text + "),10);" + BufferData [postUrl ];
            SubF.SaveData(BufferData[fixUrl], pathWebData);
            webx.Navigate(pathWebData);

        }

       
        private void webx_StatusTextChanged(object sender, EventArgs e)
        {  
            if (L1.Text != webx.StatusText)
            L1.Text = webx.StatusText;
            String MapData = L1.Text;
           
               try
               {
                   WebInfoMap[6] = MapData.Substring(0, 2);
                   WebInfoMap[7] = MapData.Substring(2, 2);
                   WebInfoMap[8] = MapData.Substring(4, 2);
                   WebInfoMap[9] = MapData.Substring(6, 2);
                   WebInfoMap[10] = MapData.Substring(8, 2);

                   WebInfoMap[1] = MapData.Substring(14, int.Parse(WebInfoMap[6]));  //Latitude
                   WebInfoMap[2] = MapData.Substring((14 + int.Parse(WebInfoMap[6])), int.Parse(WebInfoMap[7]));  //Longitude
                   WebInfoMap[3] = MapData.Substring((14 + int.Parse(WebInfoMap[6]) + int.Parse(WebInfoMap[7])), int.Parse(WebInfoMap[8])); //pxLat
                   WebInfoMap[4] = MapData.Substring((14 + int.Parse(WebInfoMap[6]) + int.Parse(WebInfoMap[7]) + int.Parse(WebInfoMap[8])), int.Parse(WebInfoMap[9])); // pxLong
                   WebInfoMap[5] = MapData.Substring((14 + int.Parse(WebInfoMap[6]) + int.Parse(WebInfoMap[7]) + int.Parse(WebInfoMap[8]) + int.Parse(WebInfoMap[9])), int.Parse(WebInfoMap[10])); // Zoom
                   curLat.Text = "Lat : " + WebInfoMap[1];
                   curLong.Text = "Long : " + WebInfoMap[2];
               }
               catch { ;}
           
        }

      

        private void cmdSave_Click(object sender, EventArgs e)
        {   
            saveD.Filter = "dxMap files|*.bmp";
            if (saveD.ShowDialog() == DialogResult.OK)
            {
                SubF.SaveData(fixText.Text, saveD.FileName + ".Info");
                String MapData = fixText.Text;
                WebInfoMap[6] = MapData.Substring(0, 2);
                WebInfoMap[7] = MapData.Substring(2, 2);
                WebInfoMap[8] = MapData.Substring(4, 2);
                WebInfoMap[9] = MapData.Substring(6, 2);
                WebInfoMap[10] = MapData.Substring(8, 2);
                WebInfoMap[11] = MapData.Substring(10, 2);   //Get Digit Center Of map Latitude
                WebInfoMap[12] = MapData.Substring(12, 2);   //Get Digit Center Of map Longitude

                WebInfoMap[1] = MapData.Substring(14, int.Parse(WebInfoMap[6]));  //Latitude
                WebInfoMap[2] = MapData.Substring((14 + int.Parse(WebInfoMap[6])), int.Parse(WebInfoMap[7]));  //Longitude
                WebInfoMap[3] = MapData.Substring((14 + int.Parse(WebInfoMap[6]) + int.Parse(WebInfoMap[7])), int.Parse(WebInfoMap[8])); //pxLat
                WebInfoMap[4] = MapData.Substring((14 + int.Parse(WebInfoMap[6]) + int.Parse(WebInfoMap[7]) + int.Parse(WebInfoMap[8])), int.Parse(WebInfoMap[9])); // pxLong
                WebInfoMap[5] = MapData.Substring((14 + int.Parse(WebInfoMap[6]) + int.Parse(WebInfoMap[7]) + int.Parse(WebInfoMap[8]) + int.Parse(WebInfoMap[9])), int.Parse(WebInfoMap[10])); // Zoom
                WebInfoMap[13]= MapData.Substring((14 + int.Parse(WebInfoMap[6]) + int.Parse(WebInfoMap[7]) + int.Parse(WebInfoMap[8]) + int.Parse(WebInfoMap[9])+ int.Parse(WebInfoMap[10])),int.Parse(WebInfoMap[11])); // Center Map Lat
                WebInfoMap[14]= MapData.Substring((14 + int.Parse(WebInfoMap[6]) + int.Parse(WebInfoMap[7]) + int.Parse(WebInfoMap[8]) + int.Parse(WebInfoMap[9]) + int.Parse(WebInfoMap[10])+ int.Parse(WebInfoMap[11])), int.Parse(WebInfoMap[12])); // Center Map Longitude
                              
                string wUrl = "http://maps.googleapis.com/maps/api/staticmap?center=" + WebInfoMap[13] + "," + WebInfoMap[14] + "&zoom=" + WebInfoMap[5] + "&size=800x600&maptype=satellite&sensor=false";
                SubF.Download(wUrl);
                SubF.SaveImage(saveD.FileName, System.Drawing.Imaging.ImageFormat.Png);
                
            
               
            }
        }
       
        private void L1_TextChanged(object sender, EventArgs e)
        {
            if (L1.Text.Length  > 70 )
            { fixText.Text = L1.Text ;}    
        }

        private void RawData_TextChanged(object sender, EventArgs e)
        {
            if (RawData.Text.Length > 50)
            {  
                ;
            }
            else
            {
                ;
            }
        }

        private void tabPage4_Click(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            PortList.Items.Clear();
            GetSerialDevice();
        }

        private void StatusBar_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void RotateImage(PictureBox pb, Image img, float angle)
        {
            if (img == null || pb.Image == null)
                return;

            Image oldImage = pb.Image;
            pb.Image = Utilities.RotateImage(img, angle);
            if (oldImage != null)
            {
                oldImage.Dispose();
            }
        }

        private void RotateServo(PictureBox pb, Image img, double angle)
        {
            if (img == null || pb.Image == null)
                return;

            Image oldImage = pb.Image;
            pb.Image = Utilities.RotateImage(img,(float) angle);
            if (oldImage != null)
            {
                oldImage.Dispose();
            }
        }

        private void AltGraph_Click(object sender, EventArgs e)
        {

        }

        private void tabPage1_Click(object sender, EventArgs e)
        {
            
        }       

        

        private void mnuAct_Click(object sender, EventArgs e)
        {
            MenuX.Close();
            wpCnt++;
            BufferData[887] = "A";
            imgWP[wpCnt ] = new PictureBox();
            imgWP[wpCnt].Parent = Map;
            imgWP[wpCnt].Left = bufferInt[MapX ];
            imgWP[wpCnt].Top = bufferInt[MapY ];
            imgWP[wpCnt].Image = mnuAct.Image;
            imgWP[wpCnt].Size = pictureBox1.Size;
            imgWP[wpCnt].SizeMode = PictureBoxSizeMode.StretchImage;
            imgWP[wpCnt].BackColor = Color.Transparent;
            flasher();

          }

        private void mnuCam_Click(object sender, EventArgs e)
        {
            MenuX.Hide();
            BufferData[887] = "C";
            wpCnt++;          
            imgWP[wpCnt] = new PictureBox();
            imgWP[wpCnt].Parent = Map;        
            imgWP[wpCnt].Left = bufferInt[MapX];
            imgWP[wpCnt].Top = bufferInt[MapY];
            imgWP[wpCnt].Image = mnuCam.Image;
            imgWP[wpCnt].Size = pictureBox1.Size;
            imgWP[wpCnt].SizeMode = PictureBoxSizeMode.StretchImage ;
            imgWP[wpCnt].BackColor = Color.Transparent;
            flasher();
          
        }

        private void mnuWP_Click(object sender, EventArgs e)
        {
            MenuX.Hide();
            BufferData[887] = "W";
            wpCnt++;            
            imgWP[wpCnt] = new PictureBox();
            imgWP[wpCnt].Parent = Map;
            imgWP[wpCnt].Left = bufferInt[MapX];
            imgWP[wpCnt].Top = bufferInt[MapY];
            imgWP[wpCnt].Image = mnuWP.Image;
            imgWP[wpCnt].Size  = pictureBox1 .Size ;            
            imgWP[wpCnt].SizeMode = PictureBoxSizeMode.AutoSize  ;
            imgWP[wpCnt].BackColor = Color.Transparent;
            flasher();

        }

        private void flasher()
        {
            
            double DtFlash = Convert.ToDouble(InfoMap[Lat]);
            FlashData.Text = toHex(DtFlash);            
            DtFlash = Convert.ToDouble(InfoMap[3]);
            FlashData.Text += toHex(DtFlash);
            DtFlash = Convert.ToDouble(InfoMap[Lon]);
            FlashData.Text += toHex(DtFlash);
            DtFlash = Convert.ToDouble(InfoMap[4]);
            FlashData.Text += toHex(DtFlash);
            if (bufferInt[MapX] > 99)
            { varX = bufferInt[MapX].ToString("G"); }
            else if (bufferInt[MapX] > 9)
            { varX = "0" + bufferInt[MapX].ToString("G"); }
            else if (bufferInt[MapX] < 10)
            { varX = "00" + bufferInt[MapX].ToString("G"); }
            if (bufferInt[MapY] > 99)
            { varY = bufferInt[MapY].ToString("G"); }
            else if (bufferInt[MapY] > 9)
            { varY = "0" + bufferInt[MapY].ToString("G"); }
            else if (bufferInt[MapY] < 10)
            { varY = "00" + bufferInt[MapY].ToString("G"); }
            autodata.Text = autodata.Text + varY + varX + BufferData[887];
        }

        private string toHex(double Dt)
        {
            try
            {

                float flt = (float)Dt;
                byte[] bytes = BitConverter.GetBytes(flt);
                int i = BitConverter.ToInt32(bytes, 0);
                string hex = i.ToString("X");

                BufferData[888] = hex.Substring(6, 2);
                BufferData[888] += hex.Substring(4, 2);
                BufferData[888] += hex.Substring(2, 2);
                BufferData[888] += hex.Substring(0, 2);
            }
            catch
            {
                MessageBox.Show("Please Open Map First", "Error");
            }
            return BufferData[888];
        }


        private void dLine(int _urut)
        {
            if (_urut > 1)
            {

                cLine[_urut] = new System.Drawing.Pen(System.Drawing.Color.Red);
                System.Drawing.Graphics MapLine = Map.CreateGraphics();
                cLine[_urut].DashStyle = System.Drawing.Drawing2D.DashStyle.DashDotDot ;

                MapLine.DrawLine(cLine[_urut],  imgWP[_urut - 1].Left + (imgWP[_urut - 1].Width / 2), imgWP[_urut - 1].Top + (imgWP[_urut - 1].Height / 2), imgWP[_urut].Left + (imgWP[_urut].Width / 2), imgWP[_urut].Top + (imgWP[_urut].Height  / 2));

            }
        }
        private void removeLine(int _urut)
        {
            if (_urut > 1)
            {

                cLine[_urut] = new System.Drawing.Pen(System.Drawing.Color.Red);
                System.Drawing.Graphics MapLine = Map.CreateGraphics();
               
                MapLine.DrawLine(cLine[_urut], 1, 1, 1, 1);
                

            }
        }
        private void trackLine(UInt32  _urut)
        {
            if (_urut > 1)
            {
                System.Drawing.Pen kLine;
                kLine= new System.Drawing.Pen(System.Drawing.Color.LightGreen);
                System.Drawing.Graphics MapLine = Map.CreateGraphics();
            //    kLine.DashStyle = System.Drawing.Drawing2D.DashStyle;
                kLine.Width = 2;
                MapLine.DrawLine(kLine, bufferTrackX[_urut - 1], bufferTrackY[_urut - 1], bufferTrackX[_urut ], bufferTrackY[_urut ]);
                kLine.Dispose();
                MapLine.Dispose();
            }
        }
        
            
        
        private void Map_Paint(object sender, PaintEventArgs e)
        {
          
            for (int l = 1; l <= wpCnt; l++)
            {
                dLine(l);
            }
           
            lblcurLat.Left = 60;
            lblcurLat.Top = Map.Height - 30;
            lblcurLon.Left = 250;
            lblcurLon.Top = Map.Height - 30;
            BufferData[888] = "1";
        }


        private void tmrPaintEven_Tick(object sender, EventArgs e)
        {
            for (UInt32 l = 1; l <= TrackCNT; l++)
            {
                trackLine(l);
            }
            

        }

        private void tabPage3_Click(object sender, EventArgs e)
        {
            
        }

        private void JoyTimer_Tick(object sender, EventArgs e)
        {
            Joys.PollJoystick();
            for (int i = 0; i < 14; i++)
            {
                if (Joys.joybuttons[i] == true)
                {
                    BufferData[i+70] = "B";                   
                    JoystickMaping.Refresh();
                }
                else
                {
                    BufferData[i + 70] = "R";
                    JoystickMaping.Refresh();
                }
            }
            PbX.Value = Joys.CurrentJoyX;
            pbY.Value = Joys.CurrentJoyY;
            pbZ.Value = Joys.CurrentJoyZ;

            bufferInt[31] = servoBar1.Minimum;
            bufferInt[32] = servoBar2.Minimum;
            bufferInt[33] = servoBar3.Minimum;
            bufferInt[34] = servoBar4.Minimum;
            bufferInt[35] = servoBar5.Minimum;
            bufferInt[36] = servoBar6.Minimum;
           
            
            try
            {
                if (Joys.joybuttons[1] == true)
                {
                    servoBar4.Value += 1;
                }
                if (Joys.joybuttons[2] == true)
                {
                    servoBar4.Value -= 1;
                }
                if (Joys.joybuttons[3] == true)
                {
                    servoBar5.Value += 1;
                }
                if (Joys.joybuttons[4] == true)
                {
                    servoBar5.Value -= 1;
                }
            }
            catch { ;}
            try
            {

                    servoBar1.Value = (bufferInt[31] + (Joys.CurrentJoyX * 130 / 65532));
                    servoBar2.Value = (bufferInt[32] + (Joys.CurrentJoyY * 130 / 65532));
                    servoBar3.Value = (bufferInt[33] + (Joys.CurrentJoyZ * 130 / 65532));
               
              
            }
            catch
            { ; }
            
            if (isJoyUpdate == true)
            {
                
                dxJoyValue.Text = dxSub.ToBiner(servoBar1.Value) + dxSub.ToBiner(servoBar2.Value) + dxSub.ToBiner(servoBar3.Value);
            }
        }

       

        private void JoystickMaping_Paint(object sender, PaintEventArgs e)
        {
            
            for (int i = 0; i < 11; i++)
            {
               
                if (BufferData[i+70] == "R")
                {   
                        
                    e.Graphics.FillEllipse (Brushes.Blue , i * 30, 2, 20, 20);
                }
                else if(BufferData[i+70] == "B")
                {   
                    e.Graphics.FillEllipse (Brushes.Red , i * 30, 2, 20, 20);
                }
             
            }
        }

       

        private void rels_Paint(object sender, PaintEventArgs e)
        {           
            
            srt = srt +=1;
            if (srt == 1)
            {
                webx.Navigate(AppDomain.CurrentDomain.BaseDirectory + "Data\\Site\\index.html");
            }
        }

        private void cmdStop_Click(object sender, EventArgs e)
        {
            Serial.Close();
            timer1.Enabled = true;
            cmdStart.Enabled = true;
            cmdStop.Enabled = false;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Serial .IsOpen == true )
            { Serial.Close(); }
            
        }
        private void VideoCap_ImageCaptured(object source, WebCam_Capture.WebcamEventArgs e)
        {
            // set the picturebox picture
            if (BufferData[888] == "1")
            {
                //cam2.Image = e.WebCamImage;
                //VideoCap.CaptureHeight = cam2.Height;
                //VideoCap.CaptureWidth = cam2.Width;
            }
            else
            {                
                VideoBox.Image = e.WebCamImage;
                VideoCap.CaptureHeight = VideoBox.Height;
                VideoCap.CaptureWidth = VideoBox.Width;
            }
        }

        private void VideoBox_Click(object sender, EventArgs e)
        {

        }
        private void VideoBox_Paint(object sender, PaintEventArgs e)
        {
            BufferData[888] = "2";
            e.Graphics.DrawLine(Pens.Red, VideoBox.Width / 4, VideoBox.Height / 2,( VideoBox.Width /2) - 20, VideoBox.Height / 2);
            e.Graphics.DrawLine(Pens.Red, (VideoBox.Width / 2 ) + 20, VideoBox.Height / 2, (VideoBox.Width*3 / 4) , VideoBox.Height / 2);
            e.Graphics.DrawLine(Pens.Red, VideoBox.Width / 2, VideoBox.Height / 4, (VideoBox.Width / 2), (VideoBox.Height /2) - 20);
            e.Graphics.DrawLine(Pens.Red, (VideoBox.Width / 2) , (VideoBox.Height / 2) + 20, (VideoBox.Width/2), VideoBox.Height*3 / 4);
                       
        }

        private void startcam_Click(object sender, EventArgs e)
        {
            
            camSpeed.Parent= VideoBox;
            AltCAm.Parent  = VideoBox;
            HDGCam.Parent  = VideoBox;
            VideoCap.TimeToCapture_milliseconds = 20;
            // start the video capture. let the control handle the
            // frame numbers.
            VideoCap.Start(0);
            
        }

        private void lblSpeed_Change(object sender, EventArgs e)
        {
            camSpeed.Text = lblSpeed.Text;
        }

        private void lblHDG_Change(object sender, EventArgs e)
        {
            HDGCam.Text = lblHDG.Text;
        }

        private void lblAlt_Change(object sender, EventArgs e)
        {
            AltCAm.Text = lblAlt.Text;
        }

       
        private void cmdStart_Click(object sender, EventArgs e)
        {
            try
            {
                UDP_data();
               // timer1.Enabled = false;
              //  Serial.RtsEnable = true;
              //  Serial.PortName = PortList.Text;
              //  Serial.BaudRate = int.Parse(BaudList.Text);
              //  Serial.Open();
                cmdStart.Enabled = false;
                cmdStop.Enabled = true;
            }
            catch
            {
                MessageBox.Show("Port Not Available Or Port Already Open,Please Check Port Setting Tab ");
            }
        }

        private void cmdFlash_Click(object sender, EventArgs e)
        {
            Int32 CRCraw;            
            BufferData[888] =wpCnt.ToString("G");
            if (BufferData[888].Length == 1)
            {
                BufferData[888] = "00" + BufferData[888];
             }
            else if (BufferData[888].Length == 2)
            {
                BufferData[888] = "0" + BufferData[888];
            }
            CRCraw = 0;
            for(int i=0;i<=31;i++)
            {
                waypoints[i] = FlashData.Text.Substring(i,1);
            }
            for (int i = 32; i < autodata.Text.Length+32; i++)
            {
                waypoints[i] = autodata.Text.Substring(i - 32, 1);
            }
            for (int i = 0; i < autodata.Text.Length + 32; i++)
            {
                CRCraw = CRCraw ^ Convert.ToChar(waypoints[i]);
            }
            try
            {
                Serial.Write(dxSub.ToBiner(1));
                Serial.Write("A");
                Serial.Write(BufferData[888]);
                for (int i = 0; i < autodata.Text.Length + 32; i++)
                {
                    Serial.Write(waypoints[i]);
                    RTFlash.Text += waypoints[i];
                }
                if (CRCraw == 1)
                {
                    MessageBox.Show("CRC return Error, Choose another WP coordinats", "Error");
                    
                }
                else
                {
                    Serial.Write(dxSub.ToBiner(CRCraw));
                    Serial.Write(dxSub.ToBiner(2));
                }
                    
            }
            catch
            {
                MessageBox.Show("Serial Port Closed", "Error");
            }
        }

        private void cmdUp_Click(object sender, EventArgs e)
        {
            if (servoChanel.Text == "1")
            {
                servoBar1.Minimum += 1;
                servoBar1.Maximum += 1;
                label1.Text = servoBar1.Minimum.ToString("G") + ":" + servoBar1.Maximum.ToString("G");
                
            }
            else if (servoChanel.Text == "2")
            {
                servoBar2.Minimum += 1;
                servoBar2.Maximum += 1;
                label2.Text = servoBar2.Minimum.ToString("G") + ":" + servoBar2.Maximum.ToString("G");
                bufferInt[32] = servoBar2.Minimum;
            }
            else if (servoChanel.Text == "3")
            {
                servoBar3.Minimum += 1;
                servoBar3.Maximum += 1;
                label3.Text = servoBar3.Minimum.ToString("G") + ":" + servoBar3.Maximum.ToString("G");
                bufferInt[33] = servoBar3.Minimum;
            }
            else if (servoChanel.Text == "4")
            {
                servoBar4.Minimum += 1;
                servoBar4.Maximum += 1;
                label4.Text = servoBar4.Minimum.ToString("G") + ":" + servoBar4.Maximum.ToString("G");
            }
            else if (servoChanel.Text == "5")
            {
                servoBar5.Minimum += 1;
                servoBar5.Maximum += 1;
                label5.Text = servoBar5.Minimum.ToString("G") + ":" + servoBar5.Maximum.ToString("G");
            }
            else if (servoChanel.Text == "6")
            {
                servoBar6.Minimum += 1;
                servoBar6.Maximum += 1;
                label6.Text = servoBar6.Minimum.ToString("G") + ":" + servoBar6.Maximum.ToString("G");
            }
        }

        private void cmdDown_Click(object sender, EventArgs e)
        {
            if (servoChanel.Text == "1")
            {
                servoBar1.Minimum -= 1;
                servoBar1.Maximum -= 1;
                label1.Text = servoBar1.Minimum.ToString("G") + ":" + servoBar1.Maximum.ToString("G");
          
            }
            else if (servoChanel.Text == "2")
            {
                servoBar2.Minimum -= 1;
                servoBar2.Maximum -= 1;
                label2.Text = servoBar2.Minimum.ToString("G") + ":" + servoBar2.Maximum.ToString("G");
       
            }
            else if (servoChanel.Text == "3")
            {
                servoBar3.Minimum -= 1;
                servoBar3.Maximum -= 1;
                label3.Text = servoBar3.Minimum.ToString("G") + ":" + servoBar3.Maximum.ToString("G");
       
            }
            else if (servoChanel.Text == "4")
            {
                servoBar4.Minimum -= 1;
                servoBar4.Maximum -= 1;
                label4.Text = servoBar4.Minimum.ToString("G") + ":" + servoBar4.Maximum.ToString("G");
       
            }
            else if (servoChanel.Text == "5")
            {
                servoBar5.Minimum -= 1;
                servoBar5.Maximum -= 1;
                label5.Text = servoBar5.Minimum.ToString("G") + ":" + servoBar5.Maximum.ToString("G");
           
            }
            else if (servoChanel.Text == "6")
            {
                servoBar6.Minimum -= 1;
                servoBar6.Maximum -= 1;
                label6.Text = servoBar6.Minimum.ToString("G") + ":" + servoBar6.Maximum.ToString("G");
            }
        }

        private void mnuClear_Click(object sender, EventArgs e)
        {
            for (int l = 1; l <= wpCnt; l++)
            {
                Map.Controls.Remove(imgWP[l]);
                removeLine(l);
            }
            Map.Hide();
            Map.Show();
            wpCnt = 0;
            autodata.Text = "";
            FlashData.Text = "";
        }

        void servoBar1_ValueChanged(object sender, System.EventArgs e)
        {
            preJoyData();
        }

        void servoBar2_ValueChanged(object sender, System.EventArgs e)
        {
            preJoyData();
        }

        void servoBar3_ValueChanged(object sender, System.EventArgs e)
        {
           preJoyData();
        }

        void servoBar4_ValueChanged(object sender, System.EventArgs e)
        {

            preJoyData();
        }
        void servoBar5_ValueChanged(object sender, System.EventArgs e)
        {
            preJoyData();
        }
        void servoBar6_ValueChanged(object sender, System.EventArgs e)
        {
            preJoyData();
        }
        void preJoyData()
        {
            textBox13.Text = servoBar1.Value.ToString("G");
            textBox14.Text = servoBar2.Value.ToString("G");
            textBox15.Text = servoBar3.Value.ToString("G");

            Serial.Encoding = Encoding.GetEncoding(1252);
                int CRCJoy;
                
                if (Serial.IsOpen == true)
                {
                    Serial.Write(dxSub.ToBiner(1));
                    Serial.Write("M");
                        CRCJoy = 0;
                        Serial.Write(dxSub.ToBiner(servoBar1.Value));
                        CRCJoy = CRCJoy ^ servoBar1.Value;
                        Serial.Write(dxSub.ToBiner(servoBar2.Value));
                        CRCJoy = CRCJoy ^ servoBar2.Value;
                        Serial.Write(dxSub.ToBiner(servoBar3.Value));
                        CRCJoy = CRCJoy ^ servoBar3.Value;
                        Serial.Write(dxSub.ToBiner(servoBar4.Value));
                        CRCJoy = CRCJoy ^ servoBar4.Value;
                        Serial.Write(dxSub.ToBiner(servoBar5.Value));
                        CRCJoy = CRCJoy ^ servoBar5.Value;
                   
                    if (CRCJoy < 3) { Serial.Write(dxSub.ToBiner(5)); }
                    else { Serial.Write(dxSub.ToBiner(CRCJoy)); }
                    textBox12.Text = CRCJoy.ToString("G");
                    textBox13.Text = servoBar1.Value.ToString("G");
                    textBox14.Text = servoBar2.Value.ToString("G");
                    textBox15.Text = servoBar3.Value.ToString("G");
                    textBox16.Text = bufferInt[131].ToString("G");                 
                     Serial.Write(dxSub.ToBiner(2));
            }
        }

        public void JSInitSuccessCallback_(object pluginInstance)
        {
            mge = (IGEPlugin)pluginInstance;
            isGEReady = true;
        //    webEarth.Document = null;
         }
        public void JSInitFailureCallback_(string error)
        {
            MessageBox.Show("Error: " + error, "Plugin Load Error", MessageBoxButtons.OK,
                MessageBoxIcon.Exclamation);
           // isGEReady = false;            
        }

           
        private void StartView_Click(object sender, EventArgs e)
        {
            ScriptCreateEarth();
        }
        
        private void startEarth_Click(object sender, EventArgs e)
        {
           
            string pathWebData2 = AppDomain.CurrentDomain.BaseDirectory + "Data\\earth.html";
           webEarth.Navigate(pathWebData2);

            webEarth.ObjectForScripting = this;
            webEarth.ScriptErrorsSuppressed = false;

        
       
         
        }

        private void ScriptCreateEarth()
        {           
                isGEReady = false;
                int nScale = 1;
                if (mge != null)
                {
                     mge.getOptions().setStatusBarVisibility(1);
                      mge.getOptions().setScaleLegendVisibility(1);
                      mge.getOptions().setFlyToSpeed(mge.SPEED_TELEPORT);
                     mge.getNavigationControl().setVisibility(mge.VISIBILITY_AUTO);
                      mge.getWindow().setVisibility(1);

                    model = mge.createModel("");
                    string pathWebData3 = "http://swadexi.googlecode.com/files/unilAV1.dae";
                    la = mge.createLookAt("");
                    //  lookAt = mge.getView().copyAsLookAt(mge.ALTITUDE_RELATIVE_TO_GROUND);

                    placemark = mge.createPlacemark("");
                    placemark.setName("model");
                    model = mge.createModel("");
                    mge.getFeatures().appendChild(placemark);
                    loc = mge.createLocation("");
                    model.setLocation(loc);
                    IKmlLink link = mge.createLink("");
                    planeOrient = mge.createOrientation("");
                    model.setOrientation(planeOrient);
                    model.setAltitudeMode(mge.ALTITUDE_RELATIVE_TO_GROUND);

                    model.getScale().setX(nScale);
                    model.getScale().setY(nScale);
                    model.getScale().setZ(nScale);

                    link.setHref(pathWebData3);
                    model.setLink(link);
                    model.setAltitudeMode(mge.ALTITUDE_RELATIVE_TO_GROUND);
                    la = mge.getView().copyAsLookAt(mge.ALTITUDE_RELATIVE_TO_GROUND);
                    loc.setLatitude(-5.363980);
                    loc.setLongitude(105.239775);
                    loc.setAltitude(0);

                    placemark.setGeometry(model);

                    la.setRange(300);
                    la.setTilt(45);
                    mge.getView().setAbstractView(la);
                }
            
        }

        private void updateAttitude(double posLat, double posLon, double heading, double pitch, double roll, double altx)
        {
            if (isGEReady == true )
            {
                isGEReady = false;
                if (mge != null)
                {
                    planeOrient.setTilt(pitch);
                    planeOrient.setRoll(roll);
                    planeOrient.setHeading(heading);

                    loc.setLatitude(posLat);
                    loc.setLongitude(posLon);
                    loc.setAltitude(altx);
                    model.setLocation(loc);
                    placemark.setGeometry(model);                   
                    if (freeView == false)
                    {
                       viewCam(posLat, posLon, heading, pitch, altx);
                    }
                  
                    webEarth.Document.InvokeScript("isReady", new object[] {});
                    // createLine(posLat, posLon);
                    // planeOrient.setTilt(0);
                    //  planeOrient.setRoll(0);
                    // planeOrient.setHeading(0);

                  
                    //  createLine(-5.363980, 105.239775);
                }
            }
            
        }
        private void viewCam(double posLat, double posLon, double heading, double pitch, double altx)
        {          
                if (mge != null)
                {
                    la = mge.createLookAt("");
                    la.set(posLat, posLon,
                    altx, // altitude
                    mge.ALTITUDE_RELATIVE_TO_GROUND,
                    heading, // heading
                    20, // straight-down tilt
                    20// range (inverse of zoom)
                    );
                    mge.getView().setAbstractView(la);
                   
                }
            
        }

        private void createLine(double posLat, double posLon)
        {
            
            if (mge != null)
            {
                IKmlLineString line1 = mge.createLineString("");
                if (dataEarth[9] != 0)
                {
                    dtEarth[3] = dataEarth[9];
                    dtEarth[4] = dataEarth[10];
                    line1.getCoordinates().pushLatLngAlt(dtEarth[3], dtEarth[4], 0);
                }
                dataEarth[9] = posLat;
                dataEarth[10] = posLon;

                line1.getCoordinates().pushLatLngAlt(posLat, posLon, 2);
                line1.setTessellate(1);
                line1.setAltitudeMode(mge.ALTITUDE_CLAMP_TO_GROUND);

                KmlMultiGeometryCoClass multiGeometry = mge.createMultiGeometry("");
                multiGeometry.getGeometries().appendChild(line1);

                KmlPlacemarkCoClass multGeoPlacemark = mge.createPlacemark("");
                multGeoPlacemark.setGeometry(multiGeometry);
                multGeoPlacemark.setStyleSelector(mge.createStyle(""));
                mge.getFeatures().appendChild(multGeoPlacemark);
            }
        }
        private void button4_Click(object sender, EventArgs e)
        {
            viewCam(-5.363980, 105.239775,0,0,10);
            updateAttitude(-5.363980, 105.239775, 180, 0, 0, 10);
            freeView = false;
        }

        private void cmdSetGround_Click(object sender, EventArgs e)
        {
            txtSetAltitude.Text = dataEarth[5].ToString ("G");
        }

        private void cmdRecord_Click(object sender, EventArgs e)
        {
            cmdStopRecord.Enabled = true;
            dlgSaveRec.Filter = "dxDataLog Files (*.dxl)| *.dxl";
            if (dlgSaveRec.ShowDialog() == DialogResult.OK)
            {
                txtRecordFile.Text =dlgSaveRec.FileName;
                isRecording = true;
            }
            cmdRecord.Enabled = false;
        }

        private void cmdStopRecord_Click(object sender, EventArgs e)
        {
            isRecording = false;
            cmdRecord.Enabled = true;
            cmdStopRecord.Enabled = false;
            tmrPlayDatalog.Enabled = false;
            btnPlayRecordFile.Enabled = true;
        }
        private void btnBrowSe_Click(object sender, EventArgs e)
        {
            dlgOpenFileRecord.Filter = "dxLog Files (*.dxl)| *.dxl";
            if (dlgOpenFileRecord.ShowDialog() == DialogResult.OK)
            {
                txtRecordFile.Text = dlgOpenFileRecord.FileName;                
            }
        }

        private void btnPlayRecordFile_Click(object sender, EventArgs e)
        {
            j = 0;
            playDatalogCounter++;
            isRecording = false;
            using (var sr = new StreamReader(txtRecordFile.Text))
            {
                while ((dataLogC[j] = sr.ReadLine()) != null)
                {
                    j++;
                }

            }   
            if (txtRecordFile != null)
            {
                tmrPlayDatalog.Enabled = true;
                tmrPlayDatalog.Interval= 500;
            }
            btnPlayRecordFile.Enabled = false;
            cmdRecord.Enabled = false;
            cmdStopRecord.Enabled = true;

        }

        private void tmrPlayDatalog_Tick(object sender, EventArgs e)
        {
            playDatalogCounter++;
            RxString = dataLogC[playDatalogCounter];
            if (RxString != null)
            {
                this.Invoke(new EventHandler(DisplayText));
            }
            if (playDatalogCounter == j)
            {
                playDatalogCounter = 0;
            }
        }

        private void btnFreeView_Click(object sender, EventArgs e)
        {
            freeView = true;
        }

        private void tabPage7_Click(object sender, EventArgs e)
        {
           
        }

        private void button2_Click(object sender, EventArgs e)
        {
            apiURL = "http://api.wunderground.com/api/a4e40ed2d36f25af/forecast/conditions/forecast/q/-5.363980,105.239775.xml";            
            webforecast.Navigate(apiURL);
            SubF.SaveData(apiURL,AppDomain.CurrentDomain.BaseDirectory + "ApiURL.tmp");

        }

        private void webforecast_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            textBox17.Text = webforecast.Document.ActiveElement.InnerText; ;
            SubF.SaveData(textBox17.Text, AppDomain.CurrentDomain.BaseDirectory + "ApiURL.tmp");
            getAllText();
            getText();
         }

           
        private void getAllText()
        {
            j = 0;
            string xApi = AppDomain.CurrentDomain.BaseDirectory + "apiurl.tmp";
            using (var srx = new StreamReader(xApi))
            {
                while ((apiLog[j] = srx.ReadLine()) != null)
                {
                    j++;                    
                }

            }   
        }


        private void getText()
        {
           
            try
            {   parseXML(apiLog[14], "full", "City :");
                parseXML(apiLog[33], "elevation", "elevation :");
                parseXML(apiLog[40], "observation_time", "Last Update :");
                parseXML(apiLog[51], "temp_c", "Temperature (C) :");
                parseXML(apiLog[58], "wind_kph", "Wind (kph) :");
                parseXML(apiLog[60], "pressure_mb", "Presure (Bar) :");
                city.Text += apiLog[132].Substring(12, apiLog[132].Length - 12);
            }
            catch {
                try
                {
                    parseXML(apiLog[37], "observation_time", "Last Update :");
                    parseXML(apiLog[48], "temp_c", "Temperature (C) :");
                    parseXML(apiLog[55], "wind_kph", "Wind (kph) :");
                    parseXML(apiLog[57], "pressure_mb", "Presure (Bar) :");
                    city.Text += apiLog[132].Substring(12, apiLog[132].Length - 12);
                }
                catch { ;}
               }

        }

   
        private void parseXML(string XMLData,string kata,string intro)
        {
            using (XmlReader reader = XmlReader.Create(new StringReader(XMLData)))
            {
                reader.ReadToFollowing(kata);
                output.AppendLine(intro + reader.ReadElementContentAsString());
            }
            city.Text = output.ToString();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            getAllText();
            getText();
        }

        private void tabPage6_Click(object sender, EventArgs e)
        {

        }
       
    }
}



