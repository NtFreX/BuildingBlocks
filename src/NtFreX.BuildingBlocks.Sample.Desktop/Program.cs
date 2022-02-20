using NtFreX.BuildingBlocks.Sample;
using NtFreX.BuildingBlocks.Shell;
using System.Reflection;
using System.Threading.Tasks;
using Veldrid.StartupUtilities;

namespace NtFreX.BuildingBlocks.Desktop
{
    /*
     * TODO: 
     * 
     * Pipeline pool
     *   render all resource sets (resource set pool?)
     *     render all bufffers
     */

    // TODO: https://github.com/KhronosGroup/SPIRV-Tools (offline combined shaders?) and precompile models as well
    // TODO: fix light pos materials, shadows
    // TODO: ocapacy and material tint
    // TODO: fix textures (allow objects without texture, currently if non is set the last is used)
    // TODO: fix plane vertices or texture strange when noise
    // TODO: when moving to a top view changing direction is strange
    // TODO: do not draw out of screen
    // TODO: animations and file improrts
    // TODO: android host
    // TODO: particles
    // TODO: sound and music
    // TODO: ui
    // TODO: multitheading (taks, synccontext?)
    // TODO: drag and drop
    class Program
    {

        static async Task Main(string[] args)
        {
            // sdl c# import or own bindings (licence both wrapper and main lib)
            await Game.SetupShellAndRunAsync<SampleGame>(new DesktopShell(new WindowCreateInfo()
            {
                X = 100,
                Y = 100,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowTitle = Assembly.GetEntryAssembly().FullName
            }, isDebug: ApplicationContext.IsDebug), ApplicationContext.LoggerFactory);
        }
    }

}