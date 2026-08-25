//using SDL3;

//[SDL.GenerateMain]
//internal sealed partial class Game : SDL.IMainCallbacks<Game>
//{
//    private IntPtr _window;
//    private IntPtr _renderer;
//    private float _direction = 1.0f;
//    private float _squareX = 0f;

//    /// <summary>
//    /// Runs once at startup. Use this to initialize subsystems and create objects.
//    /// </summary>
//    public static SDL.AppResult AppInit(out Game? appState, string[] args)
//    {
//        // Initialize SDL Video subsystem
//        if (!SDL.Init(SDL.InitFlags.Video))
//        {
//            SDL.Log($"Failed to initialize SDL: {SDL.GetError()}");
//            appState = null;
//            return SDL.AppResult.Failure;
//        }

//        // Create an instance of this class to store application state
//        appState = new Game();

//        // Create a Window and a Renderer
//        if (!SDL.CreateWindowAndRenderer("SDL3-CS AppIterate Demo", 640, 480, SDL.WindowFlags.Resizable, out appState._window, out appState._renderer))
//        {
//            SDL.Log($"Failed to create window/renderer: {SDL.GetError()}");
//            return SDL.AppResult.Failure;
//        }

//        return SDL.AppResult.Continue; // Tells SDL3 to start the iteration loop
//    }

//    /// <summary>
//    /// Called repeatedly by SDL3. This replaces the traditional 'while(running)' game loop.
//    /// Handle your game logic and rendering here.
//    /// </summary>
//    public SDL.AppResult AppIterate()
//    {
//        // --- 1. UPDATE STATE ---
//        // Move a small square back and forth across the screen
//        _squareX += 200.0f * _direction * (1.0f / 60.0f); // Approximating 60 FPS step
//        if (_squareX > 540f || _squareX < 0f)
//        {
//            _direction *= -1.0f; // Reverse direction on boundary hit
//        }

//        // --- 2. RENDER STAGE ---
//        // Clear screen with a dark blue background
//        SDL.SetRenderDrawColor(_renderer, 20, 40, 80, 255);
//        SDL.RenderClear(_renderer);

//        // Draw the moving red square
//        SDL.FRect square = new SDL.FRect
//        {
//            X = _squareX,
//            Y = 215f,
//            W = 100f,
//            H = 100f
//        };
//        SDL.SetRenderDrawColor(_renderer, 230, 40, 40, 255);
//        SDL.RenderFillRect(_renderer, ref square);

//        // Present the backbuffer onto the screen
//        SDL.RenderPresent(_renderer);

//        // Return Continue to keep running, or Success/Failure to terminate
//        return SDL.AppResult.Continue;
//    }

//    /// <summary>
//    /// Called automatically whenever a hardware, window, or input event occurs.
//    /// </summary>
//    public SDL.AppResult AppEvent(ref SDL.Event @event)
//    {
//        // Cleanly exit if the user presses the 'X' button or hits Alt+F4
//        if ((SDL.EventType)@event.Type == SDL.EventType.Quit)
//        {
//            return SDL.AppResult.Success;
//        }

//        return SDL.AppResult.Continue;
//    }

//    /// <summary>
//    /// Runs once when the application is shutting down. Clean up unmanaged assets here.
//    /// </summary>
//    public void AppQuit(SDL.AppResult result)
//    {
//        if (_renderer != IntPtr.Zero) SDL.DestroyRenderer(_renderer);
//        if (_window != IntPtr.Zero) SDL.DestroyWindow(_window);
//        SDL.Quit();
//    }
//}





using SpectrumNet;

var configuration = new Configuration();

#if DEBUG
configuration.DebugMode = true;
#endif

var computer = new Cabinet(configuration);

computer.Plug(new KempstonJoystick(computer.Motherboard));
//computer.Plug(new Interface2Joystick(computer.Motherboard));
computer.RaisePOWER();
LoadROM(configuration, computer);
LoadProgram(configuration, computer);
computer.RunLoop();

static void LoadROM(Configuration configuration, Cabinet computer)
{
    var romDirectory = configuration.RomDirectory;
    //computer.Plug(romDirectory + "\\G12R_ROM.bin");	// Planetoids (Asteroids)
    //computer.Plug(romDirectory + "\\G24R_ROM.bin");	// Horace and the Spiders
    //computer.Plug(romDirectory + "\\G9R_ROM.bin");	// Space Raiders (Space Invaders)
    //computer.Plug(romDirectory + "\\Jet Pac (1983)(Sinclair Research)(GB).rom");	// Jet Pac

    //computer.Plug(romDirectory + "\\System_Test_ROM.bin");  // Sinclair test ROM by Dr. Ian Logan
    //computer.Plug(romDirectory + "\\Release-v0.37\\testrom.bin");

    //computer.Plug(romDirectory + "\\smart\\ROMs\\Old Versions\\old_diagroms\\DiagROM.v28");

    //computer.Plug(romDirectory + "\\smart\\ROMs\\DiagROM.v41");
    //computer.Plug(romDirectory + "\\diagrom\\DiagROMv.173");
}

static void LoadProgram(Configuration configuration, Cabinet computer)
{
    var programDirectory = configuration.ProgramDirectory;
    //computer.LoadSna(programDirectory + "\\ant_attack.sna");	// 3D ant attack
    //computer.LoadSna(programDirectory + "\\zexall.sna");

    //computer.LoadZ80(programDirectory + "\\Manic.z80");
    //computer.LoadZ80(programDirectory + "\\Jet_Set_Willy_1984_Software_Projects_cr.z80");
    computer.LoadZ80(programDirectory + "\\Jetpac (1983)(Ultimate Play The Game)[a][16K].z80");
    //computer.LoadZ80(programDirectory + "\\Helichopper (1985)(Firebird)[a].z80");
    //computer.LoadZ80(programDirectory + "\\TFF4.Z80");
    //computer.LoadZ80(programDirectory + "\\BABY.Z80");
    //computer.LoadZ80(programDirectory + "\\ATARI2.Z80");    // works
    //computer.LoadZ80(programDirectory + "\\HEDGEHOG.Z80"); // Not V1 (128k IF1)
    //computer.LoadZ80(programDirectory + "\\Knight Lore (1984)(Ultimate).z80");
    //computer.LoadZ80(programDirectory + "\\R-Type (1988)(Activision).z80");		// v3
    //computer.LoadZ80(programDirectory + "\\Maziacs (1983)(DK'Tronics).z80"); // z80 v3
    //computer.LoadZ80(programDirectory + "\\Mercenary - Escape From Targ (1987)(Novagen)[aka Mercenary I].z80");
    //computer.LoadZ80(programDirectory + "\\Bubble Bobble (1987)(Firebird)(48K-128K).z80");
    //computer.LoadZ80(programDirectory + "\\Druid (1986)(Firebird).z80");
    //computer.LoadZ80(programDirectory + "\\Head Over Heels (1987)(Ocean Software).z80"); // works
    //computer.LoadZ80(programDirectory + "\\Alien 8 (1985)(Ultimate).z80");
    //computer.LoadZ80(programDirectory + "\\Cobra (1986)(Ocean Software)[a2].z80");  // works
    //computer.LoadZ80(programDirectory + "\\HALLSTHI.Z80");
    //computer.LoadZ80(programDirectory + "\\Rommels_Revenge_1983_Crystal_Computing.z80");    // works
    //computer.LoadZ80(programDirectory + "\\Elite (1986)(Firebird).z80");    // hangs
    //computer.LoadZ80(programDirectory + "\\Arkanoid (1987)(Imagine Software).z80"); // works
    //computer.LoadZ80(programDirectory + "\\Ballblazer (1985)(Activision).z80"); // Not V1
    //computer.LoadZ80(programDirectory + "\\Boulder Dash (1984)(First Star Software).z80");
    //computer.LoadZ80(programDirectory + "\\Spectrum Musicmaker (1983)(Robert Newman).z80");
    //computer.LoadZ80(programDirectory + "\\DK'Tronics Sound Effects (19xx)(DK'Tronics)[a].z80");
    //computer.LoadZ80(programDirectory + "\\Spectrum Sound FX (1983)(Marolli Soft).z80");	// Too big index out of range
    //computer.LoadZ80(programDirectory + "\\Sound Demo 01 (1992)(Theo Devil).z80");	// Too big index out of range
    //computer.LoadZ80(programDirectory + "\\Sounds 2 (19xx)(The Champ).z80");	// Too big index out of range
    //computer.LoadZ80(programDirectory + "\\Synthesizer KX-5 (1987)(Claus Jahn)(UNK-LANG).z80");

    //computer.InsertTape(programDirectory + "\\Heli Chopper.tzx");
}
