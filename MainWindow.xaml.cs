using System;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.Win32;
using SimpleImpersonation;

namespace DailyShutdownApp
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    //https://stackoverflow.com/questions/9491958/registry-getvalue-always-return-null
    //https://www.c-sharpcorner.com/blogs/date-and-time-format-in-c-sharp-programming1
    
    public partial class MainWindow : Window
    {
        const string maschineRoot = "HKEY_LOCAL_MACHINE";
        const string subKey = "SOFTWARE\\ShutdownTool";
        const string keyName = maschineRoot + "\\" + subKey;
        const string domain = "AD";
        const string user = "svc_shutdowntool";
        const string password = "123456";

        /*
        public bool Startup(object sender, StartupEventArgs env)
        {
            bool DailyCheck = false;

            if (env.Args[0] == "/DailyCheck")
            {
                DailyCheck = true;
                //MainWindow.WindowState = WindowState.Minimized;
            }
            return DailyCheck;
        }
        */

        public MainWindow()
        {

            string[] InputValues = Environment.GetCommandLineArgs();

            // Read Registry; If ShutdownDate == CurrentDate show Notification; else close app
            if (InputValues[0] == "/DailyCheck")
            {
                if (WhenIsShutdownDate(ReadRegistryKey("ShutdownDate")) == 0)
                {
                    Notification notificationWindow = new Notification();
                    notificationWindow.Show();
                    notificationWindow.LabelNotification.Content= "Ihr System wird in 10 min heruntergefahren!";
                    Thread.Sleep(1000);
                    notificationWindow.Close();
                    Thread.Sleep(290000);
                    notificationWindow.LabelNotification.Content = "Ihr System wird in 5min heruntergefahren!";
                    Thread.Sleep(1000);
                    notificationWindow.Close();
                    Thread.Sleep(290000);
                    Process.Start("shutdown", "/s /t 0");
                }
                else
                {
                    System.Windows.Application.Current.Shutdown();
                }
                
                
            }

            InitializeComponent();
            
            if (WhenIsShutdownDate(ReadRegistryKey("ShutdownDate")) == -1)
            {
                WriteRegistryKey("ShutdownDate", DateTime.Now.ToString("dd.MM.yyyy"+" 22:00:00"));
                WriteRegistryKey("MaxDaysToDelay", "7");
            }
            SetLabelTextFourLabel1(ReadRegistryKey("ShutdownDate"));
            SetLabelTextFourLabel2(ReadRegistryKey("MaxDaysToDelay"));
        }

        public void DailyRegCheck(string[] args)
        {

        }

        public void SetLabelTextFourLabel1(string labelText)
        {
            var credentials = new UserCredentials(domain, user, password);
            Impersonation.RunAsUser(credentials, LogonType.Batch, () =>
            {
                Label_Text1.Content = labelText;
            });

        }

        public void SetLabelTextFourLabel2(string labelText)
        {
            var credentials = new UserCredentials(domain, user, password);
            Impersonation.RunAsUser(credentials, LogonType.Batch, () =>
            {
                Label_Text2.Content = labelText;
            });
        }

        public void WriteRegistryKey(string valueName, string newValue)
        {
            Registry.SetValue(keyName, valueName, newValue);
        }

        public string ReadRegistryKey(string valueName)
        {
            object Temp = Registry.GetValue(keyName, valueName, 0);
            string keyValue = Convert.ToString(Temp);
            if (string.IsNullOrEmpty(keyValue))
            {
                keyValue = "Fehler - Wert konnte nicht ausgelesen werden!";
                return keyValue;
            }
            else
            {
                return keyValue;
            }
        }

        public int WhenIsShutdownDate(string ShutdownDate)
        {
            //Vergleicht das Datum in Registry mit aktuellem Datum
            int When = 0;
            DateTime Now = DateTime.Now;
            DateTime ToCheck = DateTime.Parse(ShutdownDate);
            int result = DateTime.Compare(ToCheck, Now);
            // Date is in past
            if (result < 0)
            {
                When = -1;
            }
            // Date is current date
            else if (result == 0)
            {
                When = 0;
            }
            // Date is in future
            else
            {
                When = 1;
            }
            return When;
        }

        /*
        public Boolean IsShutDownDateInPast(string ShutdownDate)
        {
            Boolean IsInPast = false;
            DateTime Now = DateTime.Now;
            DateTime ToCheck = DateTime.Parse(ShutdownDate);
            int result = DateTime.Compare(ToCheck, Now);
            if (result < 0)
            {
                IsInPast = true;
            }
            else if (result > 0)
            {
                IsInPast = false;
            }
            else
            {
                IsInPast = false;
            }
            return IsInPast;
        }
        */

        public void SetNewShutdownDate(int Input)
        {
                string NewShutDownDate = Convert.ToString(DateTime.Parse(ReadRegistryKey("ShutdownDate")).AddDays(Input));                
                TextBlock_Info4.Visibility = Visibility.Visible;
                TextBlock_Info4.Text = "Das System wird nun am " + NewShutDownDate + " heruntergefahren";
                WriteRegistryKey("ShutdownDate", NewShutDownDate);
                CalculateNewMaxDaysToDelay(Input);          
        }

        
        public void CalculateNewMaxDaysToDelay(int inputValue)
        {
            int CalculateMaxDaysToDelay = 0;
            CalculateMaxDaysToDelay = int.Parse(ReadRegistryKey("MaxDaysToDelay")) - inputValue;
            if (CalculateMaxDaysToDelay >= 0)
            {
                WriteRegistryKey("MaxDaysToDelay", Convert.ToString(CalculateMaxDaysToDelay)); 
            }
            else
            {
                CalculateMaxDaysToDelay = 0;
            }

        }
        
        private void Button_Ok_Click(object sender, RoutedEventArgs e)
        {
            if (int.Parse(TextBox_DaysToDelay.Text) <= int.Parse(ReadRegistryKey("MaxDaysToDelay"))){
                SetNewShutdownDate(int.Parse(TextBox_DaysToDelay.Text));
            }
            else
            {
                TextBlock_Info4.Visibility = Visibility.Visible;
                TextBlock_Info4.Text = "Bitte gültigen Wert eingeben";
            }
            
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            //System.Windows.Application.Current.Shutdown();
            Notification notificationWindow = new Notification();
            notificationWindow.Show();
            notificationWindow.LabelNotification.Content = "Ihr System wird in 10 min heruntergefahren. Bitte speichern Sie noch alle geöffneten Dokumente";
            //https://stackoverflow.com/questions/35441202/wpf-native-windows-10-toasts/50953028

        }
            {

            }
    //notificationWindow.Close();           
            
        }
    }
}
