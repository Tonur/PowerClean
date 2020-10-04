using System;

namespace PowerClean.Exceptions
{
  internal class NoProjectFoundException : Exception
  {
    public NoProjectFoundException() : base("There was no project found.")
    {

    }
  }
}