using System;
using System.ServiceProcess;

namespace DeviceHubMini.Tray.Services
{
    public class ServiceHelper
    {
        private readonly string _serviceName;

        public ServiceHelper(string serviceName)
        {
            _serviceName = serviceName;
        }

        public string GetStatus()
        {
            using var controller = new ServiceController(_serviceName);
            controller.Refresh();
            return controller.Status.ToString();
        }

        public void StartService()
        {
            using var controller = new ServiceController(_serviceName);
            controller.Refresh();
            if (controller.Status == ServiceControllerStatus.Stopped)
            {
                controller.Start();
                controller.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
            }
        }

        public void StopService()
        {
            using var controller = new ServiceController(_serviceName);
            controller.Refresh();
            if (controller.Status == ServiceControllerStatus.Running)
            {
                controller.Stop();
                controller.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
            }
        }

        public void RestartService()
        {
            StopService();
            StartService();
        }
    }
}
