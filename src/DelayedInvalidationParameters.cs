using System;
using System.Collections.Generic;

namespace Como.WebApi.Caching
{
    public class DelayedInvalidationParameters
    {
        public IList<MethodInvalidationParameters> Methods { get; set; }
        public DateTime DueTime { get; set; }
    }
}