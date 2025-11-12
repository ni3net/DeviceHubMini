using DeviceHubMini.Jobs;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.JobsService
{
    public class RegisterService
    {
        public static void Add(IServiceCollection serviceCollection) {

    //        var jobTypes = typeof(IJobHandler).Assembly
    //.GetTypes()
    //.Where(t => typeof(IJobHandler).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);

    //        foreach (var jobType in jobTypes)
    //        {
    //            serviceCollection.AddTransient(typeof(IJobHandler), jobType);
    //        }
            //// Register job classes for DI
            //serviceCollection.AddTransient<Job1>();
            //serviceCollection.AddTransient<Job2>();
        }
    }
}
