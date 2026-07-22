using System;

namespace Appinventario.Services.BorradorDev
{
    public class BorradorDevApiException : Exception
    {
        public int StatusCode { get; private set; }
        public string RawBody { get; private set; }

        public BorradorDevApiException(int statusCode, string message, string rawBody = null)
            : base(message)
        {
            StatusCode = statusCode;
            RawBody = rawBody;
        }
    }
}

