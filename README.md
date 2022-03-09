# [TITLE]

[TITLE] provides a set of building blocks for graphics engines in .NET. It's dependency on [Veldrid](https://github.com/mellinoe/veldrid) allows it to be graphics API-agnostic. The library/framework is cross-platform but provides currently only bootstrapping methods for Desktop thought SDL2.

https://user-images.githubusercontent.com/20086318/157331044-37514dbe-ebfa-4677-8a9f-bce4f591dc18.mov

## Dependencies

- This graphics library/framework is based on [Veldrid](https://github.com/mellinoe/veldrid) (Veldrid is a cross-platform, graphics API-agnostic rendering and compute library for .NET). ([MIT License](https://github.com/mellinoe/veldrid/blob/master/LICENSE))
  - [Vortice.Windows](https://github.com/amerkoleci/Vortice.Windows) (Vortice.Windows is a collection of Win32 and UWP libraries with bindings support for DXGI, WIC, DirectWrite, Direct2D, Direct3D9, Direct3D11, Direct3D12, XInput, XAudio2, X3DAudio and DirectInput). ([MIT License](https://github.com/amerkoleci/Vortice.Windows/blob/main/LICENSE))
  - [vk](https://github.com/mellinoe/vk) (This repository contains low-level bindings for the Vulkan graphics and compute API.) ([MIT License](https://github.com/mellinoe/vk/blob/master/LICENSE.md))
  - [nativelibraryloader](https://github.com/mellinoe/nativelibraryloader) ([MIT License](https://github.com/mellinoe/nativelibraryloader/blob/master/LICENSE))
- The physics integration is based on [BEPUphysics2](https://github.com/bepu/bepuphysics2) (BEPUphysics is a pure C# 3D physics library by BEPU). ([Apache License 2.0](https://github.com/bepu/bepuphysics2/blob/master/LICENSE.md))
- Text and other image related features are implemented with [SixLabors.ImageSharp](https://github.com/SixLabors/ImageSharp) (ImageSharp is a new, fully featured, fully managed, cross-platform, 2D graphics library). ([Apache License 2.0](https://github.com/SixLabors/ImageSharp/blob/main/LICENSE))
- Shader cross compilation is supported by [SPIRV-Cross](https://github.com/KhronosGroup/SPIRV-Cross) (SPIRV-Cross is a tool designed for parsing and converting SPIR-V to other shader languages). ([Apache License 2.0](https://github.com/KhronosGroup/SPIRV-Cross/blob/master/LICENSE))
  - .NET wrapper: [Veldrid.SPIRV](https://github.com/mellinoe/veldrid-spirv) ([MIT License](https://github.com/mellinoe/veldrid-spirv/blob/master/LICENSE))
- The default GUI integrated is [ImGui](https://github.com/ocornut/imgui) (Dear ImGui is a bloat-free graphical user interface library for C++).
  - .NET wrapper: [ImGui.NET](https://github.com/mellinoe/ImGui.NET) ([MIT License](https://github.com/mellinoe/ImGui.NET/blob/master/LICENSE))
- For debugging this library integrates into [RenderDoc](https://github.com/baldurk/renderdoc) (RenderDoc is a stand-alone graphics debugging tool). ([MIT License](https://github.com/baldurk/renderdoc))
- Some features are impleted by using [AssimpNet](https://github.com/assimp/assimp-net)/[Assimp](https://github.com/assimp/assimp) (A library to import and export various 3d-model-formats including scene-post-processing to generate missing render data). ([LICENSE](https://github.com/assimp/assimp/blob/master/LICENSE))
- Input handling and audio functionality is provided by [SDL2](https://www.libsdl.org/download-2.0.php) (Simple DirectMedia Layer is a cross-platform development library designed to provide low level access to audio, keyboard, mouse, joystick, and graphics hardware via OpenGL and Direct3D). ([zlib license](https://www.libsdl.org/license.php))
  - .NET wrapper: [SDL2-CS](https://github.com/flibitijibibo/SDL2-CS) (This is SDL2#, a C# wrapper for SDL2). ([LICENSE](https://github.com/flibitijibibo/SDL2-CS/blob/master/LICENSE))


Features:

 - [ ] Audio
   - [x] basic 3d audio
   - [ ] sdl audio
     - [x] play wav
     - [ ] play different sample rates/bit rates/channels at the same time
     - [ ] other audio formats
 - [ ] Camera
   - [x] basic infrastructure
   - [x] basic movable cameras
   - [ ] more examples
 - [ ] Input
   - [x] basic desktop input handler
   - [ ] console
   - [ ] mobile
 - [ ] MeshRenderer
   - [x] textured
   - [x] instanced
   - [ ] animated
   - [ ] mesh properties (physics etc)
 - [ ] Light
   - [ ] phong
     - [x] directional lights
     - [ ] point lights
     - [ ] spot lights
   - [ ] emissive light
   - [ ] deffered
   - [ ] pbr
   - [ ] shadows
     - [x] basic with cascades
     - [ ] configurable cascades
     - [ ] stabelized
 - [ ] material system
   - [x] basic
   - [ ] multiple input setup
 - [ ] import
   - [x] obj file loader
   - [x] simple assimp loader
   - [x] simple dae loader
   - [ ] more formats
   - [ ] complex inputs
 - [ ] physics
   - [x] basic bepu2 integration
   - [ ] ...
 - [x] text
 - [x] textures
 - [ ] particles
   - [x] basic
   - [ ] generic particle types
 - [x] ...
 - [ ] ...

TODO:

 - Cleanup and decoupling

## Architecture

You can use this library as a framework if you overwrite the `Game` type and use one of the described startup methods.

```
public class SampleGame : Game 
{ 
  protected override void Setup(IShell shell, ILoggerFactory loggerFactory)
  {
    EnableImGui = Shell.IsDebug;
    AudioSystemType = AudioSystemType.Sdl2;
    EnableBepuSimulation = true;

    var scene = new Scene(shell.IsDebug);
    scene.LightSystem.Value = new LightSystem();
    scene.Camera.Value = new MovableCamera(shell.Width, shell.Height); ;
    await ChangeSceneAsync(scene);

    await base.SetupAsync(shell, loggerFactory);
  }
  ...
}
```

The goal is that you can use the graphics pipeline independently from other components as well. Currently most of the logic for that can be found in the graphics system class but it needs to be decoupled from the Scene first.

## Startup

### Desktop

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

### Android (not working)

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

## Basics

### Drawing

```
var lineMeshRenderer = LineModel.Create(start: Vector3.Zero, end: Vector3.UnitX * lineLength);
var qubeMeshRenderer = QubeModel.Create(sideLength: qubeSideLength);
...
```

```
await CurrentScene.AddCullRenderablesAsync(qubeMeshRenderer);
await CurrentScene.AddFreeRenderablesAsync(qubeMeshRenderer);
CurrentScene.AddUpdateables(qubeMeshRenderer);
```

### Audio

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

## Meshes

### BinaryMeshDataProvider

Raw buffer of positions, normals, texture coordinates, colors and indices.


### MeshDataProvider<TVertex, TIndex>

This provider is faster to create device buffers from, it provides structs for the vertex and index type. The clas `MeshDataExtensions` contains some usefull extension methods to modify the vertex and index data. The following are the valid vertex and index types, of corse you can create your own if you want.

 - VertexPositionNormalTextureColor
 - VertexPositionNormalTexture
 - VertexPositionNormal
 - VertexPosition

 - Index32
 - Index16


Custom meshes
```
var vertices = new VertexPosition[] { Vector3.Zero, Vector3.One };
var indices = new Index16[] { 0, 1 };
var mesh = new DefinedMeshData<VertexPosition, Index16>(vertices, indices, PrimitiveTopology.LineList);
var provider = new StaticMeshDataProvider(mesh);
var renderer = MeshRenderer.CreateAsync(provider);
```

## ShaderPrecompiler

### Supported syntax:

 - #if
 - #elseif
 - #else
 - #endif
 - not
 - !
 - #include
 - #{VARIABLE}

### Examples:

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
```

Standard variable
```
layout(location = #{boneWeightsLocation}) in vec4 BoneWeights;
```

Inline if
```
#if hasInstances #include ./math.shader #endif
```

-----------------------------------


## Importers

 - ObjModelImporter
 - AssimpDaeModelImporter
   - Supports BoneAnimations
 - DaeModelImporter
   - Is based on DaeFileReader (an almost decoupled dae file reader)
   - Supports Nodes, Scenes, Meshes, Materials partatly

## Animation (not working)

 - AssimpBoneAnimationProvider

## TODO

 - [TODO list](./TODO.txt)
