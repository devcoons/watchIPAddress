using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
namespace WatchIpAddress
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : System.Windows.Application
    {
        private Thread IPCheckThread;
        private NotifyIcon notifyIcon = null;
        private ContextMenuStrip contextMenu = new ContextMenuStrip();
        public bool IsRunning = false;
        private string currentIP = "Unknown";
        private string currentIPdetails = "None";
        public App()
        {
            var assemblies = new Dictionary<string, Assembly>();
            var executingAssembly = Assembly.GetExecutingAssembly();
            var resources = executingAssembly.GetManifestResourceNames().Where(n => n.EndsWith(".dll"));


            foreach (string resource in resources)
            {
                using (var stream = executingAssembly.GetManifestResourceStream(resource))
                {
                    if (stream == null)
                        continue;

                    var bytes = new byte[stream.Length];
                    stream.Read(bytes, 0, bytes.Length);
                    try
                    {
                        assemblies.Add(resource, Assembly.Load(bytes));
                    }
                    catch (Exception)
                    {
                    }
                }
            }

            AppDomain.CurrentDomain.AssemblyResolve += (s, e) =>
            {
                var assemblyName = new AssemblyName(e.Name);
                var path = string.Format("{0}.dll", assemblyName.Name);
                return assemblies.ContainsKey(path) == true ? assemblies[path] : null;
            };

            notifyIcon = new System.Windows.Forms.NotifyIcon();
            notifyIcon.Icon = WatchIpAddress.Properties.Resources.favicon;
            notifyIcon.MouseUp += NotifyIcon_MouseUp;
            notifyIcon.ContextMenuStrip = contextMenu;
            contextMenu.Items.Add("Close",null, notifyIconExit_Click);
            IPCheckThread = new Thread(new ThreadStart(IPCheck));
            IPCheckThread.Priority = ThreadPriority.AboveNormal;
            IsRunning = true;
            IPCheckThread.Start();
        }

        private void NotifyIcon_MouseUp(object sender, MouseEventArgs e)
        {
            if(e.Button == MouseButtons.Left)
                notifyIcon.ShowBalloonTip(5000, "Current public IP: "+ currentIP, currentIPdetails, ToolTipIcon.Info);
        }

        void App_Startup(object sender, StartupEventArgs e)
        {
            notifyIcon.Visible = true;
        }

        void notifyIconExit_Click(object sender, EventArgs e)
        {
            try
            {
                IsRunning = false;
                try
                {
                    IPCheckThread.Abort();
                }
                catch (Exception)
                { }

                notifyIcon.Visible = false;
                System.Windows.Application.Current.Shutdown();
            }
            catch (Exception)
            { }
        }

        void IPCheck()
        {
            while (IsRunning)
            {
                HTTPResult result =  HttpRequest.Instance.Query(new HTTPRequestParameters() { Url = "https://ipinfo.io/json", Method = "GET", Timeout = 5000 });

                if (result.Status==HTTPRequestStatus.OK)
                {
                    IPInfo obj = Newtonsoft.Json.JsonConvert.DeserializeObject<IPInfo>(result.Value.ToString());
                    if(obj.ip!= currentIP)
                    {
                        currentIP = obj.ip;
                        currentIPdetails = obj.city+", "+obj.region + " (" + obj.country + ") " + obj.postal + "\n" + obj.timezone +"\n"+obj.hostname;
                        notifyIcon.ShowBalloonTip(3000, "Current public IP: " + currentIP, currentIPdetails, ToolTipIcon.Info);
                    }
                }        
                Thread.Sleep(10000);
            }
        }
    }
}
