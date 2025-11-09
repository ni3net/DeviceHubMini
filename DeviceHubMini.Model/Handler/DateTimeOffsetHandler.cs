using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeviceHubMini.Infrastructure.Handler
{
    public class DateTimeOffsetHandler : SqlMapper.TypeHandler<DateTimeOffset>
    {
        public override void SetValue(IDbDataParameter parameter, DateTimeOffset value)
            => parameter.Value = value.ToString("o"); // store as ISO8601 text

        public override DateTimeOffset Parse(object value)
            => DateTimeOffset.TryParse(value?.ToString(), out var result)
                ? result
                : DateTimeOffset.MinValue;
    }
}
