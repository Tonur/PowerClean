using System;

namespace PowerClean.Exceptions
{
  internal class NoProjectSelectedException : Exception
  {
    public NoProjectSelectedException() : base("There was no project selected in the Solution Explorer.")
    {

    }
  }
}