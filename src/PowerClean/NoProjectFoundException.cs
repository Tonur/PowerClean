using System;

namespace PowerClean
{
  internal class NoProjectFoundException : Exception
  {
    public NoProjectFoundException() : base("There was no project found.")
    {

    }
  }
}