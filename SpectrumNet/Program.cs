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
                computer.Plug(new KempstonJoystick(computer.Motherboard));
                computer.Plug(new Interface2Joystick(computer.Motherboard));
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
            computer.Plug(romDirectory + "\\Jet Pac (1983)(Sinclair Research)(GB).rom");	// Jet Pac

            //computer.Plug(romDirectory + "\\System_Test_ROM.bin");	// Sinclair test ROM by Dr. Ian Logan
            //computer.Plug(romDirectory + "\\Release-v0.37\\testrom.bin");
            //computer.Plug(romDirectory + "\\smart\\ROMs\\DiagROM.v41");
            //computer.Plug(romDirectory + "\\DiagROMv.171");
            

            var programDirectory = configuration.ProgramDirectory;
            //computer.LoadSna(programDirectory + "\\ant_attack.sna");	// 3D ant attack
            //computer.LoadSna(programDirectory + "\\zexall.sna");

            //computer.LoadZ80(programDirectory + "\\Manic.z80");
            //computer.LoadZ80(programDirectory + "\\Jet_Set_Willy_1984_Software_Projects_cr.z80");
            //computer.LoadZ80(programDirectory + "\\Jetpac (1983)(Ultimate Play The Game)[a][16K].z80");
            //computer.LoadZ80(programDirectory + "\\Helichopper (1985)(Firebird)[a].z80");
            //computer.LoadZ80(programDirectory + "\\TFF4.Z80");
            //computer.LoadZ80(programDirectory + "\\BABY.Z80");
            //computer.LoadZ80(programDirectory + "\\ATARI2.Z80");    // hangs
            //computer.LoadZ80(programDirectory + "\\HEDGEHOG.Z80"); // Not V1
            //computer.LoadZ80(programDirectory + "\\Knight Lore (1984)(Ultimate).z80");
            //computer.LoadZ80(programDirectory + "\\R-Type (1988)(Activision).z80");		// v3
            //computer.LoadZ80(programDirectory + "\\Head Over Heels (1987)(Ocean Software).z80"); // Not V1
            //computer.LoadZ80(programDirectory + "\\Alien 8 (1985)(Ultimate).z80");
            //computer.LoadZ80(programDirectory + "\\Cobra (1986)(Ocean Software)[a2].z80");  // Not V1
            //computer.LoadZ80(programDirectory + "\\HALLSTHI.Z80");
            //computer.LoadZ80(programDirectory + "\\Rommels_Revenge_1983_Crystal_Computing.z80");    // Loads, then slowly crashes!
            //computer.LoadZ80(programDirectory + "\\Elite (1986)(Firebird).z80");    // Doesn't respond to keyboard
            //computer.LoadZ80(programDirectory + "\\Arkanoid (1987)(Imagine Software).z80"); // Runs for a while, then crashes
            //computer.LoadZ80(programDirectory + "\\Ballblazer (1985)(Activision).z80"); // Not V1
            //computer.LoadZ80(programDirectory + "\\Boulder Dash (1984)(First Star Software).z80");
            //computer.LoadZ80(programDirectory + "\\Spectrum Musicmaker (1983)(Robert Newman).z80");
            //computer.LoadZ80(programDirectory + "\\DK'Tronics Sound Effects (19xx)(DK'Tronics)[a].z80");
            //computer.LoadZ80(programDirectory + "\\Spectrum Sound FX (1983)(Marolli Soft).z80");	// Too big index out of range
            //computer.LoadZ80(programDirectory + "\\Sound Demo 01 (1992)(Theo Devil).z80");	// Too big index out of range
            //computer.LoadZ80(programDirectory + "\\Sounds 2 (19xx)(The Champ).z80");	// Too big index out of range
            //computer.LoadZ80(programDirectory + "\\Synthesizer KX-5 (1987)(Claus Jahn)(UNK-LANG).z80");
        }
    }
}
