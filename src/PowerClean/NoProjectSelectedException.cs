using System;

namespace PowerClean
{
  internal class NoProjectSelectedException : Exception
  {
    public NoProjectSelectedException() : base("There was no project selected in the Solution Explorer.")
    {

    }
  }
}