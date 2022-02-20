This is a graphics library/framework based on [Veldrid](https://github.com/mellinoe/veldrid) (Veldrid is a cross-platform, graphics API-agnostic rendering and compute library for .NET).
It provides a physics integration based on [BEPUphysics2](https://github.com/bepu/bepuphysics2) (BEPUphysics is a pure C# 3D physics library by BEPU).
Text and other image related features are implemented with [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) (ImageSharp is a new, fully featured, fully managed, cross-platform, 2D graphics library).
Shader cross compilation is supported by [SPIRV-Cross](https://github.com/KhronosGroup/SPIRV-Cross) (SPIRV-Cross is a tool designed for parsing and converting SPIR-V to other shader languages).
The default GUI integrated is [ImGui](https://github.com/ocornut/imgui)(Dear ImGui is a bloat-free graphical user interface library for C++).
For debugging this library integrates into [RenderDoc](https://github.com/baldurk/renderdoc) (RenderDoc is a stand-alone graphics debugging tool).
Some features are impleted by using [AssimpNet](https://github.com/assimp/assimp-net) [Assimp](https://github.com/assimp/assimp) (A library to import and export various 3d-model-formats including scene-post-processing to generate missing render data).
Interoperability for SDL2 is partialy provided by [SDL2-CS](https://github.com/flibitijibibo/SDL2-CS) (This is SDL2#, a C# wrapper for SDL2).

**Architecture**

You can use this library as a framework if you overwrite the `Game` type and use one of the described startup methods.

```
public class SampleGame : Game 
{ 
  public SampleGame()
  {
    EnableImGui = Shell.IsDebug;
    AudioSystemType = AudioSystemType.Sdl2;
    EnableBepuSimulation = true;
  }
}
```

**Startup**

*Desktop*

Desktop environments can be bootstrapped by using the `DesktopShell` and the default InputHandler based on SLD2.

```
static async Task Main(string[] args)
{
  var shell = new DesktopShell(new WindowCreateInfo()
  {
    X = 100,
    Y = 100,
    WindowWidth = 960,
    WindowHeight = 540,
    WindowTitle = Assembly.GetEntryAssembly().FullName
  }, isDebug: true);
  await Game.RunAsync<SampleGame>(shell);
}
```

*Android* (not working)

Android environments can be bootstrapped by using the `AndroidShell` and the default `AndroidInputSnapshot`.

```
[Activity(Label = "@string/app_name", MainLauncher = true, ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation | ConfigChanges.ScreenSize)]
public class MainActivity : AndroidActivity
{
    private AndroidShell shell;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);

        shell = new AndroidShell(this, isDebug: true);
        Game.SetupShell<SampleGame>(shell);

        SetContentView(shell.View);
    }

    protected override void OnPause()
    {
        base.OnPause();
        shell.OnPause();
    }

    protected override void OnResume()
    {
        base.OnResume();
        shell.OnResume();
    }
}
```

**Basics**

*Audio*

Playing and manipulating audio.
```
var context = AudioSystem.PlayWav(@"resources/audio/Dash Runner.wav", volume: 50, loop: false);
context.IsPaused = true;
context.IsStopped = true;
context.Loop = true;
context.Volume = 0;
```

Place and play a 3d sound source.
```
AudioSystem.PlaceWav(@"resources/audio/explosion.wav", intensity: impactForce, position: Vector3.Zero);
```

```
AudioSystem.StopAll();
AudioSystem.PreLoadWav(file: @"resources/audio/explosion.wav");
```

*Drawing*

```
var lineMeshRenderer = LineModel.Create(GraphicsDevice, ResourceFactory, GraphicsSystem, start: Vector3.Zero, end: Vector3.UnitX * lineLength);
var qubeMeshRenderer = QubeModel.Create(graphicsDevice, resourceFactory, graphicsSystem, sideLength: qubeSideLength);
...
```

```
CurrentScene.AddCullRenderables(qubeMeshRenderer);
CurrentScene.AddFreeRenderables(qubeMeshRenderer);
CurrentScene.AddUpdateables(qubeMeshRenderer);
```

**Meshes**

*BinaryMeshDataProvider*

Raw buffer of positions, normals, texture coordinates, colors and indices.


*MeshDataProvider<TVertex, TIndex>*

This provider is faster to create device buffers from, it provides structs for the vertex and index type. The clas `MeshDataExtensions` contains some usefull extension methods to modify the vertex and index data. The following are the valid vertex and index types, of corse you can create your own if you want.

 - VertexPositionNormalTextureColor
 - VertexPositionNormalTexture
 - VertexPositionNormal
 - VertexPosition

 - Index32
 - Index16

**ShaderPrecompiler**

*Supported syntax:*

 - #if
 - #elseif
 - #else
 - #endif
 - not
 - !
 - #include
 - #{VARIABLE}

*Examples:*

Standard include
```
#include ./math.shader
```

Standard if/elseif/else
```
#if hasInstances
  vec3 color = mix(instanceColor, baseColor, alpha);
#elseif hasTint
  vec3 color = mix(tint, baseColor, alpha);
#else
  vec3 color = baseColor;
#endif


Standard variable
```
layout(location = #{boneWeightsLocation}) in vec4 BoneWeights;
```

Inline if
```
#if hasInstances #include ./math.shader #endif
```

-----------------------------------


**Importers**

 - ObjModelImporter
 - AssimpDaeModelImporter
  - Supports BoneAnimations
 - DaeModelImporter
  - Is based on DaeFileReader (an almost decoupled dae file reader)
  - Supports Nodes, Scenes, Meshes, Materials partatly

**Animation** (not working)

 - AssimpBoneAnimationProvider