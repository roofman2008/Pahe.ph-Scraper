using System;
using System.Runtime.Serialization;

namespace PaheScrapper.Exceptions
{
    public class ScrapperDownloaderException : Exception
    {
        public ScrapperDownloaderException()
        {
        }

        public ScrapperDownloaderException(string message) : base(message)
        {
        }

        public ScrapperDownloaderException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected ScrapperDownloaderException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}