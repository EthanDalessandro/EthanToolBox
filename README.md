# EthanToolBox

A lightweight utility toolbox for Unity, featuring a simple Dependency Injection system.

## Installation

You can install this package directly from GitHub via the Unity Package Manager.

1. Open your Unity Project.
2. Go to **Window > Package Manager**.
3. Click the **+** icon in the top-left corner.
4. Select **Add package from git URL...**.
5. Enter the following URL:
   ```
   https://github.com/EthanDalessandro/EthanToolBox.git?path=/Assets/EthanToolBox
   ```
   *(Make sure to replace `EthanDalessandro` with your actual GitHub username if different)*

## Features

### Dependency Injection

A lightweight DI system to manage your game's dependencies.

**Quick Start:**

1. **Create a Service:**
   ```csharp
   public class MyService
   {
       public void DoSomething() => Debug.Log("Hello!");
   }
   ```

2. **Create an Installer (Composition Root):**
   Create a script inheriting from `CompositionRoot` and attach it to a GameObject in your scene.
   ```csharp
   using EthanToolBox.Core.DependencyInjection;

   public class GameInstaller : DICompositionRoot
   {
       protected override void Configure(DIContainer container)
       {
           container.RegisterSingleton<MyService>(new MyService());
       }
   }
   ```

3. **Inject into a MonoBehaviour:**
   Add the `[Inject]` attribute to any field you want to populate.
   ```csharp
   public class Player : MonoBehaviour
   {
       [Inject] private MyService _myService;

       private void Start()
       {
           _myService.DoSomething();
       }
   }
   ```

## Requirements

- Unity 2021.3 or higher.
