using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SpectrumNet
{
    class Program
    {
        static void Main(string[] args)
        {
            var configuration = new Configuration();

#if DEBUG
            configuration.DebugMode = true;
#endif

            using (var game = new Cabinet(configuration))
            {
                game.Run();
            }
        }
    }
}
