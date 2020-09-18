using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.ServiceProcess;
using System.Text;
using System.Threading.Tasks;

namespace ToolsNetProxy
{
    public partial class ToolsNetProxy : ServiceBase
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        Configuration config;

        public ToolsNetProxy()
        {
            InitializeComponent();
        }

        protected override void OnStart(string[] args)
        {
            //UNCOMMENT TO ENABLE DEBUGGER:
            //System.Diagnostics.Debugger.Launch();

            log.Info("***********************************************************************************************");
            log.Info("OnStart event - Version: " + Assembly.GetExecutingAssembly().GetName().Version);

            //Load Forwarding configuration:
            try
            {
                config = new Configuration();
            }
            catch (Exception exc)
            {
                log.Error("Error on Load Configuration MXL file", exc);

                return;
            }

            log.Debug("Starting forwarders...");
            //TODO
            foreach (var a in config.forwarders.Forwarder)
            {
                a.StartForwarder();
            }
        }

        protected override void OnStop()
        {
            foreach (var a in config.forwarders.Forwarder)
            {
                a.StopForwarder();
            }

            log.Info("OnStop event");
            log.Info("***********************************************************************************************");
        }

        protected override void OnShutdown()
        {
            foreach (var a in config.forwarders.Forwarder)
            {
                a.StopForwarder();
            }

            log.Info("OnShutdown event");
            log.Info("***********************************************************************************************");
        }

        protected override bool OnPowerEvent(PowerBroadcastStatus powerStatus)
        {
            foreach (var a in config.forwarders.Forwarder)
            {
                a.StopForwarder();
            }

            log.Info("OnPowerEvent event - cause: " + powerStatus.ToString());
            log.Info("***********************************************************************************************");

            return base.OnPowerEvent(powerStatus);
        }
    }
}
