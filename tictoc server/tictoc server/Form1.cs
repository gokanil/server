using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace tictoc_server
{
    public partial class Form1 : Form
    {
        MySqlConnection connection;
        static Thread port120;  //dış dünyayı dinlemek için thred
        public static string data = null; //gelen ve gidecek veri
        IPAddress ipAddress;
        int port = 120;  //dilenilecek port
        int sayacBaslangic = 5;
        int sayac;  //timerin sayacı 30 dan geri doğru
        static int k = 1;
        string[] hostkadi = new string[100];
        string[] katilanakadi = new string[100];
        string[] hostamesaj = new string[100];
        string[] katilanamesaj = new string[100];
        string[] hostamesajvarmi = new string[100];
        string[] katilanamesajvarmi = new string[100];

        string version;

        public Form1()
        {
            InitializeComponent();
        }

    private void Form1_Load(object sender, EventArgs e)
        {
            /////////////////////////////////////////////////////////////////////////////////////////////
            //////////////////////////////////////////////////////////////////////////////////////////////
            //--------------------------- database yi değiştirmeniz yeterli olacaktır---------------------
            connection = new MySqlConnection("server=127.0.0.1;port = 3306; database=armut;UID=root;PWD=");
            ////////////////////////////////////////////////////////////////////////////////////////////////
            ///////////////////////////////////////////////////////////////////////////////////////////////
            try
            {
                ipAddress = Dns.GetHostByName(Dns.GetHostName()).AddressList[0];
                if (connection.State == ConnectionState.Closed)
                    connection.Open();
                Control.CheckForIllegalCrossThreadCalls = false;
                port120 = new Thread(voidPort120);
                port120.Start();
                lblTime.Text = sayac.ToString();
                timer1.Start();
            }
           catch { Process.GetCurrentProcess().Kill(); }


        }
        void voidPort120()
        {
            ric("voidPort120 başladı");
            byte[] bytes = new Byte[1024];
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, port);
            ric(ipAddress+":"+port+" dinlenmeye eklendi");
            Socket listener = new Socket(ipAddress.AddressFamily,
                SocketType.Stream, ProtocolType.Tcp); //socket dinlemeyi açar

            try
            {
                listener.Bind(localEndPoint);
                listener.Listen(1);

                while (true)
                {
                    //ric("bağlantı bekleniyor...");
                    Socket handler = listener.Accept();
                    data = null;
                    while (true)
                    {
                        int bytesRec = handler.Receive(bytes);
                        data += Encoding.ASCII.GetString(bytes, 0, bytesRec);
                        if (data.IndexOf("<TC>") > -1)
                        {
                            break;
                        }
                    }
                    
                    data = islem(data);
                    if (data != "") ric("giden mesaj: " + data);
                    byte[] msg = Encoding.ASCII.GetBytes(data);
                    handler.Send(msg);
                    handler.Shutdown(SocketShutdown.Both);
                    handler.Close();
                    //Thread.Sleep(100);
                }

            }
            catch (Exception e)
            {
                richTextBox1.Text += e.ToString();
            }
        }
        string  islem(string geldi)
        {
            string data1 = deger(geldi);//<TC> den ayırır
            string gelen = index(data);//<  > ayırır
            if (gelen!="<online>"&&gelen !="<bekliyor>"&& gelen != "<katilanamesajvarmi>" && gelen != "<hostamesajvarmi>") ric("Alınan mesaj: " + geldi);//texboxa gelen mesajı yazar
            //ric("Alınan mesaj: " + geldi);//texboxa gelen mesajı yazar
            string gidecek= "";
            string[] parcalar = data1.Split(' ');

            if (gelen == "<version>")//oyunun versiyonunu gönderrir karşı taraf konyrol eder
            {
                gidecek = "test";
            }
            if (gelen == "<aktif>")
                gidecek = "evet";
            if (gelen == "<online>")
            {
                vdOnline(data1);
            }
            if (gelen == "<kullanicivarmi>")
            {
                int sayac = 0;
                //MessageBox.Show(parcalar[0]);
                if (listView1.Items.Count == 0)
                    gidecek = "yok";
                foreach (ListViewItem itemRow in listView1.Items)//listviewdeki itemlerde dolanıyor
                {
                    if (itemRow.SubItems[1].Text==parcalar[0])
                    {
                        sayac++;
                        break;
                    }
                }
                if(sayac==0)
                    gidecek = "yok";
            }
            if (gelen == "<sql>")
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                //gln(@"SELECT COUNT(*) FROM users where kadi='" + txtKadi.Text + "'") 
                gidecek = gln(data1);
            }
            if (gelen == "<sqlexecute>")
            {
                if (connection.State == ConnectionState.Closed)
                {
                    connection.Open();
                }
                MySqlCommand comm = new MySqlCommand(data1, connection);
                comm.ExecuteNonQuery();
                gidecek = "";
            }
            //-------ODAMODA---------//
            if (gelen == "<odakur>")
            {
                foreach (string par in parcalar)
                {
                    richTextBox2.Text += par + "\n";
                }
                ListViewItem item1 = listView2.FindItemWithText(parcalar[0]);
                if (item1 == null)
                {
                    string[] row = { k.ToString(), parcalar[2], "1/2", parcalar[0] };
                    var listViewItem = new ListViewItem(row);
                    listView2.Items.Add(listViewItem);
                    k++;
                    gidecek = "kur";
                }
            }
            if (gelen == "<odasil>")//kurulan odayı siler
            {
                foreach (ListViewItem itemRow in listView2.Items)//listviewdeki itemlerde dolanıyor
                {
                    if(itemRow.SubItems[3].Text == parcalar[0]) 
                        listView2.Items.Remove(itemRow);//listviewden forecdeki seçili itemi siler
                }
            }
        
            if (gelen == "<odalar>")
            {
                gidecek = vdOdalar("");
            }
            if (gelen == "<katil>")//client katila bastığında
            {
                gidecek = vdKatil(parcalar);//clientin mesaj attığını boşluklara parçalayıp parçalar dizisini gönderir
            }
            if (gelen == "<hostamesajvarmi>")
            {
                vdOnline(data1);
                gidecek = vdhostamesajvarmi(parcalar);
            }
            if (gelen == "<katilanamesajvarmi>")
            {
                //id kadi 1/2
                vdOnline(data1);
                gidecek = vdkatilanamesajvarmi(parcalar);
            }
            
            if (gelen == "<hostamesaj>")
            {
                //parcalar= id kadi hostadi <mesaj> mesaj
                string msgnin = index(data1);//<mesaj>
                
                if (msgnin == "<cikar>")
                {
                    ListViewItem item1 = listView2.FindItemWithText(parcalar[2]);//arama yapar
                    if (item1 != null)
                        listView2.Items[listView2.Items.IndexOf(item1)].SubItems[2].Text = "1/2";
                }
                //MessageBox.Show(msgnin+"a" +data1);
                //MessageBox.Show(geldi+" "+ parcalar[0]+" "+parcalar[1]+" "+parcalar[2]);
                vdHostamesaj(parcalar, msgnin, parcalar[2], data1);//katilanın kullanıcı adı
            }
            if (gelen == "<katilanamesaj>")
            {
                //parcalar= id kadi katilaninadi <mesaj> mesaj
                string msgnin = index(data1);
                if (msgnin == "<hostol>")
                {
                    ListViewItem item1 = listView2.FindItemWithText(parcalar[1]);//arama yapar
                    if (item1 != null)
                    {
                        //MessageBox.Show(parcalar[0]+"\n"+parcalar[1] + "\n" + parcalar[2] + "\n" + parcalar[3]);
                        // listView1.Items[listView1.Items.IndexOf(item1)].Remove();
                        listView2.Items[listView2.Items.IndexOf(item1)].SubItems[2].Text = "1/2";
                        listView2.Items[listView2.Items.IndexOf(item1)].SubItems[3].Text = parcalar[2];//odanın eski sahibinin ismini silip yeni sahibinin ismi yazıldı
                        
                    }
                }
                //MessageBox.Show(parcalar[0] + " "+ parcalar[1]+" "+ parcalar[2]);
                vdKatilanamesaj(parcalar, msgnin, parcalar[2],data1);
            }
            return gidecek;
        }//clientten gelen mesajı işleyecek ve karşılık verecek

        void vdKatilanamesaj(string[] data1, string mesaj = "", string kadi = "",string realmesaj="")//oda kurucusuna mesaj atmak isterse katılan
        {
            int i = 0;
            while (katilanamesaj[i] != null)
            {
                i++;
            }
            string message="";
            try { message = degermessage(realmesaj + "<TC>", mesaj); }
            catch { }
           // MessageBox.Show(mesaj+" "+message);
            katilanakadi[i] = kadi;
            katilanamesaj[i] = mesaj+message + " <TC>";
           // MessageBox.Show("katilana:\n " + katilanakadi[i] + "\n" + katilanamesaj[i]);
        }
        void vdHostamesaj(string[] data1, string mesaj = "",string hadi="", string realmesaj = "")//oda kurucusuna mesaj atmak isterse katılan
        {
                int i = 0;
                while (hostamesaj[i] != null)
                {
                    i++;
                }
            string message = "";
            
            try { message = degermessage(realmesaj + "<TC>", mesaj); }
            catch { }
            hostkadi[i] = hadi;
                hostamesaj[i] = mesaj + message + " <TC>";
           // MessageBox.Show("a"+mesaj+"a"+message+"a");
           // MessageBox.Show("hostamesaj:\n " + hostkadi[i] +"\n"+ hostamesaj[i]);
        }
        string vdkatilanamesajvarmi(string[] data2)
        {
            //id kadi 1/2
            if (katilanakadi.Contains(data2[1]))//hostkadi dizisinde hostun kullanıcı adı varmı
            {
                var index = Array.FindIndex(katilanakadi, x => x == data2[1]);//katilanın idsinin değeri
                //MessageBox.Show(index.ToString()+"a");
                string gidecek = katilanamesaj[Convert.ToUInt32(index)];
                katilanakadi[Convert.ToUInt32(index)] = null;
                katilanamesaj[Convert.ToUInt32(index)] = null;
               // MessageBox.Show("katilanamesaj varmi:\n "+index.ToString()+"\n"+gidecek);
                return gidecek;
            }
            return "";
        }
        string vdhostamesajvarmi(string[] data2)//oda kurucusu sürekli mesaj varmı diye kontrol eder
        {
            /*
            //oda kurucusu her bekliyor diye mesaj attığında yanında odadaki kişi sayısını atar listviewede o kişi sayısı işlenir
            ListViewItem item1 = listView2.FindItemWithText(data2[1]);//katila basanın idsi listviewde varmı varsa 2/2 güncelle
            listView2.Items[listView2.Items.IndexOf(item1)].SubItems[2].Text = data2[2];
            */
            if (hostkadi.Contains(data2[1]))//hostkadi dizisinde hostun kullanıcı adı varmı
            {
                var index = Array.FindIndex(hostkadi, x => x == data2[1]);//katilanın idsinin değeri
                //MessageBox.Show(index.ToString()+"a");
                string gidecek = hostamesaj[Convert.ToUInt32(index)];
                hostkadi[Convert.ToUInt32(index)] = null;
                hostamesaj[Convert.ToUInt32(index)] = null;
                // MessageBox.Show(gidecek);
                return gidecek;
            }
            return "";
        }
        string vdKatil(string[] gelen) //client katil tusuna bastığında katilacak dizisine kendini ve katılacağı yeri ekler
        {
            ListViewItem item1 = listView2.FindItemWithText(gelen[2]);//odanın nosunu list view2de ona denk gelen odanın kurucusunu alır
            if (item1.SubItems[2].Text == "1/2")
            {
                listView2.Items[listView2.Items.IndexOf(item1)].SubItems[2].Text = "2/2";
                // MessageBox.Show("1"+gelen[1]+"1"+"2"+ item1.SubItems[3].Text+"2");
                vdHostamesaj(gelen,"<ekle>"+gelen[1],item1.SubItems[3].Text);//hostun kullanıcı adı 3
                vdKatilanamesaj(gelen, "<ekle>"+ item1.SubItems[3].Text, gelen[1]);//katil tuşuna basanın kullanıcı adı 3//host
                return "katil";
            }
            return "";
            
        }
        string vdOdalar(string odaInfo)
        {
            foreach (ListViewItem itemRow in listView2.Items)//listviewdeki itemlerde dolanıyor
            {
                odaInfo += itemRow.SubItems[0].Text+" "+ itemRow.SubItems[1].Text+" "+ itemRow.SubItems[2].Text+" ";
            }
            return odaInfo;
            
        }
        void vdOnline(string data2)//giriş yapan clientleri listviewe ekler
        {
            string[] parcalar = data2.Split(' ');
            ListViewItem item1 = listView1.FindItemWithText(parcalar[0]);
            if (item1 == null)
            {
                string[] row = { parcalar[0], parcalar[1], sayacBaslangic.ToString() };
                var listViewItem = new ListViewItem(row);
                listView1.Items.Add(listViewItem);
            }
            else
            {
                // listView1.Items[listView1.Items.IndexOf(item1)].Remove();
                listView1.Items[listView1.Items.IndexOf(item1)].SubItems[2].Text = sayacBaslangic.ToString();
            }
        }//birisi onlineyim diye mesaj attığında onun süresini sıfırlar
        public string index(string metin)
        {
            string basla = "<";
            string bitir = ">";
            string sonuc;
            try
            {
                int IcerikBaslangicIndex = metin.IndexOf(basla) + basla.Length;
                int IcerikBitisIndex = metin.Substring(IcerikBaslangicIndex).IndexOf(bitir);
                sonuc = metin.Substring(IcerikBaslangicIndex-1, IcerikBitisIndex+2);
            }
            catch (Exception)
            {
                sonuc = null;
            }
            return sonuc;
        }//gelen mesajı < ten > ye kadar ayırır
        public string deger(string metin)
        {
            string basla = ">";
            string bitir = "<TC>";
            string sonuc;
            try
            {
                int IcerikBaslangicIndex = metin.IndexOf(basla) + basla.Length;
                int IcerikBitisIndex = metin.Substring(IcerikBaslangicIndex).IndexOf(bitir);
                sonuc = metin.Substring(IcerikBaslangicIndex, IcerikBitisIndex);
            }
            catch (Exception)
            {
                sonuc = null;
            }

            return sonuc;
        }//gelen mesajı > ten <TC> ye kadar ayırır
        public string degermessage(string metin,string mesaj)//geleni içinden önce <> ayrılır ondan sonra tekrar <mes> den ayrılır tcye kadar
        {
            //MessageBox.Show("a"+mesaj+"a");
            string basla = mesaj;
            string bitir = "<TC>";
            string sonuc;
            try
            {
                int IcerikBaslangicIndex = metin.IndexOf(basla) + basla.Length+1;//ilginç bir şekilde +1dersem boşulk sorunu ortadan kalkıyor
                int IcerikBitisIndex = metin.Substring(IcerikBaslangicIndex).IndexOf(bitir);
                sonuc = metin.Substring(IcerikBaslangicIndex, IcerikBitisIndex);
                //MessageBox.Show("b"+sonuc+"b");
            }
            catch (Exception)
            {
                sonuc = null;
            }

            return sonuc;
        }//gelen mesajı > ten <TC> ye kadar ayırır
        private void timer1_Tick(object sender, EventArgs e)
        {
            try
            {
                sayac--;
                foreach (ListViewItem itemRow in listView1.Items)//listviewdeki itemlerde dolanıyor
                {
                    int deger = Convert.ToInt32(itemRow.SubItems[2].Text);
                    deger--;
                    itemRow.SubItems[2].Text = deger.ToString();
                    if (deger.ToString() == "0")
                    {
                        listView1.Items.Remove(itemRow);//listviewden forecdeki seçili itemi siler

                        ListViewItem item1 = listView2.FindItemWithText(itemRow.SubItems[1].Text);
                        if (item1 != null)
                            listView2.Items[listView2.Items.IndexOf(item1)].Remove();
                    }
                }
                lblTime.Text = sayac.ToString();
                if (sayac == 0)
                {
                    sayac = sayacBaslangic;
                }
            }
            catch { ric("errortimer"); }
        }//oyuncular için oline süresi
        void ric(string gelen)//richtexboxa yazı yazar
        {
            if (gelen != "<TC>" || gelen != "" || gelen != " " || gelen != "<aktif><TC>")
            {
                richTextBox1.Text += gelen + "\n";
                richTextBox1.SelectionStart = richTextBox1.Text.Length;
                richTextBox1.ScrollToCaret();
            }
            if (richTextBox1.Lines.Length > 12)
            {
                richTextBox1.Select(0, richTextBox1.GetFirstCharIndexFromLine(richTextBox1.Lines.Length - 12));
                richTextBox1.SelectedText = "";
            }
        }
        string gln(string sorgu)
        {
            MySqlCommand comm = new MySqlCommand(sorgu, connection);
            comm.ExecuteNonQuery();
            String count = comm.ExecuteScalar().ToString();
            return count;
        }
        public static string GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString();
                }
            }
            throw new Exception("Sistemde IPv4 adresine sahip ağ adaptörü yok!");
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            Process.GetCurrentProcess().Kill();
        }
        private void button1_Click(object sender, EventArgs e)
        {
                string[] row = { "1", "asd", "1/2", "t" };
                var listViewItem = new ListViewItem(row);
                listView2.Items.Add(listViewItem);
            button1.Text = listView2.Items.Count.ToString();

        }

        private void button2_Click(object sender, EventArgs e)
        {
            ListViewItem item = listView2.SelectedItems[0];
            listView2.Items[listView2.Items.IndexOf(item)].Remove();
        }

        private void button3_Click(object sender, EventArgs e)
        {
            hostkadi = null;
           katilanakadi = null;
            hostamesaj = null;
            katilanamesaj = null;
            hostamesajvarmi = null;
            katilanamesajvarmi = null;
        }
    }
}
