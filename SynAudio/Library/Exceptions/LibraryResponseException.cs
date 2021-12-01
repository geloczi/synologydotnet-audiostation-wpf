using System;

namespace SynAudio.Library.Exceptions
{
    public class LibraryResponseException : Exception
    {
        public int ErrorCode { get; }
        public LibraryResponseException(int errorCode) : base($"Connection failed. Error code: {errorCode}")
        {
            ErrorCode = errorCode;
        }
    }
}
