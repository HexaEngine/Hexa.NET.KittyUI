namespace Kitty.OpenAL
{
    using System;

    public class OpenALException : Exception
    {
        internal OpenALException(string msg) : base(msg)
        {
        }
    }
}