using System;
using System.Linq;
using Serilog.Core;
using Serilog.Events;

#if NETFULL
using Serilog.Enrichers.CorrelationId.Accessors;
using Serilog.Enrichers.CorrelationId.Extensions;
#else
using Microsoft.AspNetCore.Http;
#endif

namespace Serilog.Enrichers
{
    public class CorrelationIdHeaderEnricher : ILogEventEnricher
    {
        private const string CorrelationIdPropertyName = "CorrelationId";
        private readonly string _headerKey;
        private readonly IHttpContextAccessor _contextAccessor;

        public CorrelationIdHeaderEnricher(string headerKey) : this(headerKey, new HttpContextAccessor())
        {
        }

        internal CorrelationIdHeaderEnricher(string headerKey, IHttpContextAccessor contextAccessor)
        {
            _headerKey = headerKey;
            _contextAccessor = contextAccessor;
        }

        public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
        {
            if (_contextAccessor.HttpContext == null)
                return;

            var correlationId = GetCorrelationId();

            var correlationIdProperty = new LogEventProperty(CorrelationIdPropertyName, new ScalarValue(correlationId));

            logEvent.AddOrUpdateProperty(correlationIdProperty);
        }

        private string GetCorrelationId()
        {
            string correlationId;

            if (_contextAccessor.HttpContext.Items["CorrelationId"] != null)
            {
                correlationId = _contextAccessor.HttpContext.Items["CorrelationId"].ToString();
            }
            else if (_contextAccessor.HttpContext.Request.Headers.TryGetValue(_headerKey, out var values))
            {
                correlationId = values.FirstOrDefault();
            }
            else if (_contextAccessor.HttpContext.Response.Headers.TryGetValue(_headerKey, out values))
            {
                correlationId = values.FirstOrDefault();
            }
            else
            {
                correlationId = Guid.NewGuid().ToString();
            }

            _contextAccessor.HttpContext.Items["CorrelationId"] = correlationId;

#if NETFULL
            if (!_contextAccessor.HttpContext.Response.HeadersWritten &&
                !_contextAccessor.HttpContext.Response.Headers.AllKeys.Contains(_headerKey))
#else
            if (!_contextAccessor.HttpContext.Response.Headers.ContainsKey(_headerKey))
#endif
            {
                _contextAccessor.HttpContext.Response.Headers.Add(_headerKey, correlationId);
            }

            return correlationId;
        }
    }
}
