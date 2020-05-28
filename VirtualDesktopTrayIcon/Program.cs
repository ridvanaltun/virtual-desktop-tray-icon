using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Drawing;
using WindowsDesktop;
using System.Runtime.InteropServices;

namespace VirtualDesktopTrayIcon
{
    static class Program
    {
        /// <summary>
        /// Uygulamanın ana girdi noktası.
        /// </summary>
        [STAThread]
        static void Main()
        {
            string procName = "VirtualDesktopTrayIcon";
            Process[] processNames = Process.GetProcessesByName(procName);

            try
            {
                if (processNames[0].ProcessName == procName)
                {
                    // Do not execute process when a process already running
                    MessageBox.Show("Program already running, look at your tray icons!", "Error");
                }
            }
            catch (System.IndexOutOfRangeException)
            {
                // Start
                Init();
            }
        }

        private static void Init()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MyCustomApplicationContext());
        }

        public class MyCustomApplicationContext : ApplicationContext
        {
            private NotifyIcon trayIcon;

            [DllImport("user32.dll", ExactSpelling = true)]
            static extern IntPtr GetForegroundWindow();

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            static extern bool SetForegroundWindow(IntPtr hWnd);

            private IList<VirtualDesktop> desktops;
            private IntPtr[] activePrograms;

            [DllImport("user32.dll", CharSet = CharSet.Auto)]
            extern static bool DestroyIcon(IntPtr handle);

            public MyCustomApplicationContext()
            {
                // Initialize Tray Icon
                TrayMenuContext();

                handleChangedNumber();

                VirtualDesktop.CurrentChanged += VirtualDesktop_CurrentChanged;
                VirtualDesktop.Created += VirtualDesktop_Added;
                VirtualDesktop.Destroyed += VirtualDesktop_Destroyed;

                int currentDesktopIndex = getCurrentDesktopIndex();
                changeTrayIcon(currentDesktopIndex);
                //trayIcon.BalloonTipTitle = "Running...";
                //trayIcon.BalloonTipText = "Right-click on the tray icon to exit.";
                //trayIcon.ShowBalloonTip(2000);
            }

            private void TrayMenuContext()
            {
                trayIcon = new NotifyIcon()
                {
                    Icon = Properties.Resources.mainIco,
                    ContextMenu = new ContextMenu(new MenuItem[] { new MenuItem("Exit", Exit) }),
                    Visible = true
                };
            }

            private void Exit(object sender, EventArgs e)
            {
                // Hide tray icon, otherwise it will remain shown until user mouses over it
                trayIcon.Visible = false;

                Application.Exit();
            }

            private void VirtualDesktop_Added(object sender, VirtualDesktop e)
            {
                handleChangedNumber();
            }

            private void VirtualDesktop_Destroyed(object sender, VirtualDesktopDestroyEventArgs e)
            {
                handleChangedNumber();
            }

            private void changeTrayIcon(int currentDesktopIndex = -1)
            {
                if (currentDesktopIndex == -1)
                    currentDesktopIndex = getCurrentDesktopIndex();

                var desktopNumber = currentDesktopIndex + 1;
                var desktopNumberString = desktopNumber.ToString();

                var fontSize = 140;
                var xPlacement = 100;
                var yPlacement = 50;

                if (desktopNumber > 9 && desktopNumber < 100)
                {
                    fontSize = 125;
                    xPlacement = 75;
                    yPlacement = 65;
                }
                else if (desktopNumber > 99)
                {
                    fontSize = 80;
                    xPlacement = 90;
                    yPlacement = 100;
                }
                
                Bitmap newIcon = Properties.Resources.mainIcoPng;
                Font desktopNumberFont = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel);

                var gr = Graphics.FromImage(newIcon);
                gr.DrawString(desktopNumberString, desktopNumberFont, Brushes.White, xPlacement, yPlacement);

                Icon numberedIcon = Icon.FromHandle(newIcon.GetHicon());
                trayIcon.Icon = numberedIcon;

                DestroyIcon(numberedIcon.Handle);
                desktopNumberFont.Dispose();
                newIcon.Dispose();
                gr.Dispose();
            }

            private void VirtualDesktop_CurrentChanged(object sender, VirtualDesktopChangedEventArgs e)
            {
                // 0 == first
                int currentDesktopIndex = getCurrentDesktopIndex();
                changeTrayIcon(currentDesktopIndex);
            }

            private void handleChangedNumber()
            {
                desktops = VirtualDesktop.GetDesktops();
                activePrograms = new IntPtr[desktops.Count];
            }

            VirtualDesktop initialDesktopState()
            {
                var desktop = VirtualDesktop.Current;
                int desktopIndex = getCurrentDesktopIndex();

                saveApplicationFocus(desktopIndex);

                return desktop;
            }

            private int getCurrentDesktopIndex()
            {
                return desktops.IndexOf(VirtualDesktop.Current);
            }

            private void saveApplicationFocus(int currentDesktopIndex = -1)
            {
                IntPtr activeAppWindow = GetForegroundWindow();

                if (currentDesktopIndex == -1)
                    currentDesktopIndex = getCurrentDesktopIndex();

                activePrograms[currentDesktopIndex] = activeAppWindow;
            }
        }
    }
}
