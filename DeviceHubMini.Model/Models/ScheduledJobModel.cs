using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Model.Models
{
    public class ScheduledJobModel
    {
        public int Id { get; set; }
        public string JobName { get; set; }
        public string CronExpression { get; set; }
        public string JobType { get; set; }
        public bool IsActive { get; set; }
        public string? ConnectionName { get; set; }
    }
}
