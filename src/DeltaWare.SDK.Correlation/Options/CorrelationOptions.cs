﻿using DeltaWare.SDK.Correlation.Context;

namespace DeltaWare.SDK.Correlation.Options
{
    public class CorrelationOptions : IOptions<CorrelationContext>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <remarks><b>Default value:</b> x-correlation-id</remarks>
        public string Key { get; set; } = "x-correlation-id";

        public bool AttachToResponse { get; set; } = false;
        public bool IsRequired { get; set; } = false;
        public bool AttachToLoggingScope { get; set; }
        public string LoggingScopeKey { get; set; } = "correlation-id";
    }
}
