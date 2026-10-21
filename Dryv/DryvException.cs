using System;

namespace Dryv
{
    public abstract class DryvException(string message, Exception innerException) : Exception(message, innerException)
    {
        protected DryvException(string message) : this(message, null)
        {
        }
    }
}