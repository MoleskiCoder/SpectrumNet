using SDL3;
using SpectrumNet;

[SDL.GenerateMain]
internal sealed partial class Game : SDL.IMainCallbacks<Game>
{
    private readonly Configuration _configuration = new();

    private readonly Cabinet _computer;

    public Game()
    {
        this._computer = new(this._configuration, this._configuration.Timings);
    }

    private void LoadROM()
    {
        var romDirectory = this._configuration.RomDirectory;
        //this._computer.Plug(romDirectory + "\\G12R_ROM.bin");	// Planetoids (Asteroids)
        //this._computer.Plug(romDirectory + "\\G24R_ROM.bin");	// Horace and the Spiders
        //this._computer.Plug(romDirectory + "\\G9R_ROM.bin");	// Space Raiders (Space Invaders)
        //this._computer.Plug(romDirectory + "\\Jet Pac (1983)(Sinclair Research)(GB).rom");	// Jet Pac

        //this._computer.Plug(romDirectory + "\\System_Test_ROM.bin");  // Sinclair test ROM by Dr. Ian Logan
        //this._computer.Plug(romDirectory + "\\Release-v0.37\\testrom.bin");

        //this._computer.Plug(romDirectory + "\\smart\\ROMs\\Old Versions\\old_diagroms\\DiagROM.v28");

        //this._computer.Plug(romDirectory + "\\smart\\ROMs\\DiagROM.v41");
        //this._computer.Plug(romDirectory + "\\diagrom\\DiagROMv.173");
    }

    private void LoadProgram()
    {
        var programDirectory = this._configuration.ProgramDirectory;
        //this._computer.LoadSna(programDirectory + "\\ant_attack.sna");	// 3D ant attack
        //this._computer.LoadSna(programDirectory + "\\zexall.sna");

        //this._computer.LoadZ80(programDirectory + "\\Manic.z80");
        //this._computer.LoadZ80(programDirectory + "\\Jet_Set_Willy_1984_Software_Projects_cr.z80");
        //this._computer.LoadZ80(programDirectory + "\\Jetpac (1983)(Ultimate Play The Game)[a][16K].z80");
        //this._computer.LoadZ80(programDirectory + "\\Helichopper (1985)(Firebird)[a].z80");
        //this._computer.LoadZ80(programDirectory + "\\TFF4.Z80");
        //this._computer.LoadZ80(programDirectory + "\\BABY.Z80");
        //this._computer.LoadZ80(programDirectory + "\\ATARI2.Z80");    // works
        //this._computer.LoadZ80(programDirectory + "\\HEDGEHOG.Z80"); // Not V1 (128k IF1)
        //this._computer.LoadZ80(programDirectory + "\\Knight Lore (1984)(Ultimate).z80");
        //this._computer.LoadZ80(programDirectory + "\\R-Type (1988)(Activision).z80");		// v3
        //this._computer.LoadZ80(programDirectory + "\\Maziacs (1983)(DK'Tronics).z80"); // z80 v3 
        //this._computer.LoadZ80(programDirectory + "\\Mercenary - Escape From Targ (1987)(Novagen)[aka Mercenary I].z80"); //works
        //this._computer.LoadZ80(programDirectory + "\\Bubble Bobble (1987)(Firebird)(48K-128K).z80");    // works
        //this._computer.LoadZ80(programDirectory + "\\Druid (1986)(Firebird).z80");
        //this._computer.LoadZ80(programDirectory + "\\Head Over Heels (1987)(Ocean Software).z80"); // works
        //this._computer.LoadZ80(programDirectory + "\\Alien 8 (1985)(Ultimate).z80");
        //this._computer.LoadZ80(programDirectory + "\\Cobra (1986)(Ocean Software)[a2].z80");  // works
        //this._computer.LoadZ80(programDirectory + "\\HALLSTHI.Z80");
        //this._computer.LoadZ80(programDirectory + "\\Rommels_Revenge_1983_Crystal_Computing.z80");    // works
        //this._computer.LoadZ80(programDirectory + "\\Elite (1986)(Firebird).z80");    // hangs
        //this._computer.LoadZ80(programDirectory + "\\Arkanoid (1987)(Imagine Software).z80"); // works
        //this._computer.LoadZ80(programDirectory + "\\Ballblazer (1985)(Activision).z80"); // works
        this._computer.LoadZ80(programDirectory + "\\Boulder Dash (1984)(First Star Software).z80");
        //this._computer.LoadZ80(programDirectory + "\\Spectrum Musicmaker (1983)(Robert Newman).z80");
        //this._computer.LoadZ80(programDirectory + "\\DK'Tronics Sound Effects (19xx)(DK'Tronics)[a].z80");
        //this._computer.LoadZ80(programDirectory + "\\Spectrum Sound FX (1983)(Marolli Soft).z80");	// Too big index out of range
        //this._computer.LoadZ80(programDirectory + "\\Sound Demo 01 (1992)(Theo Devil).z80");	// Too big index out of range
        //this._computer.LoadZ80(programDirectory + "\\Sounds 2 (19xx)(The Champ).z80");	// Too big index out of range
        //this._computer.LoadZ80(programDirectory + "\\Synthesizer KX-5 (1987)(Claus Jahn)(UNK-LANG).z80");

        //this._computer.InsertTape(programDirectory + "\\Heli Chopper.tzx");
    }

    public static SDL.AppResult AppInit(out Game? appState, string[] args)
    {
        appState = new Game();
        appState._computer.RaisePOWER();

        appState._computer.Plug(new KempstonJoystick(appState._computer.Motherboard));
        appState.LoadROM();
        appState.LoadProgram();

        SDL.LogInfo(SDL.LogCategory.Application, "Completed application initialisation");

        return SDL.AppResult.Continue;
    }

    public void AppQuit(SDL.AppResult result)
    {
        SDL.LogInfo(SDL.LogCategory.Application, "Terminating application");
        this._computer.LowerPOWER();
    }

    public SDL.AppResult AppIterate()
    {
        SDL.LogDebug(SDL.LogCategory.Application, "Executing application frame");
        return this._computer.RunFrame();
    }

    public SDL.AppResult AppEvent(ref SDL.Event @event)
    {
        SDL.LogDebug(SDL.LogCategory.Application, "Handling application event");
        return this._computer.HandleEvent(@event);
    }
}
