using System.IO;

namespace RebellionCodeChallenge.Services {
   public sealed class Logger {
      private static StreamWriter sw;
      private static readonly Logger instance = new Logger();
      private Logger() {
         sw = new StreamWriter("log.txt");
      }
      public static Logger Instance() {
         return instance;
      }
      public void Log(string _log) {
         sw.WriteLine(_log);
         sw.Flush();
      }

   }
}
