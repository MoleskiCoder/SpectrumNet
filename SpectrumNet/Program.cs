namespace SpectrumNet
{
    using System;

    class Program
    {

        static void Main(string[] args)
        {
            var configuration = new Configuration();

#if DEBUG
            configuration.DebugMode = true;
#endif

            using (var computer = new Cabinet(configuration))
            {
                computer.Initialized += Computer_Initialized;
                computer.Run();
            }
        }

        private static void Computer_Initialized(object sender, EventArgs e)
        {
            var computer = (Cabinet)sender;
            var configuration = computer.Settings;

            var romDirectory = configuration.RomDirectory;
            //computer.Plug(romDirectory + "\\G12R_ROM.bin");	// Planetoids (Asteroids)
            //computer.Plug(romDirectory + "\\G24R_ROM.bin");	// Horace and the Spiders
            //computer.Plug(romDirectory + "\\G9R_ROM.bin");	// Space Raiders (Space Invaders)
            //computer.Plug(romDirectory + "\\Jet Pac (1983)(Sinclair Research)(GB).rom");	// Jet Pac

            //computer.Plug(romDirectory + "\\System_Test_ROM.bin");	// Sinclair test ROM by Dr. Ian Logan
            //computer.Plug(romDirectory + "\\Release-v0.37\\testrom.bin");
            computer.Plug(romDirectory + "\\smart\\ROMs\\DiagROM.v41");

            var programDirectory = configuration.ProgramDirectory;
        }
    }
}
