# Hexa.NET.KittyUI (WIP)

Hexa.NET.KittyUI is a minimalistic UI framework built around the popular **imgui** library, designed to be lightweight and highly portable. It supports cross-platform development and comes with rendering backends for both **Direct3D 11 (D3D11)** and **OpenGL 3**.

## Features

- **Minimalistic Design**: Focused on providing a clean and simple interface for building user interfaces quickly and efficiently.
- **Cross-Platform Support**: Compatible with multiple platforms, allowing you to develop and deploy your applications on different operating systems with ease.
- **Rendering Backends**: Supports **Direct3D 11 (D3D11)** and **OpenGL 3**, giving you flexibility in choosing the right rendering backend for your project.
- **Integration with imgui**: Leverages the powerful **imgui** library, providing an intuitive and efficient way to build UI components.

## Getting Started

### Prerequisites

Before you start using Hexa.NET.KittyUI, ensure you have the following prerequisites installed:

- **.NET SDK**: .NET 8.0 or higher.

### Installation

1. **Install the NuGet Package**:

   You can install the Hexa.NET.KittyUI package via NuGet by adding `Hexa.NET.KittyUI` to your project dependencies.

2. **Integrate with Your Project**:

   After installing the package, simply include it in your project and start building your UI components.

### Usage
```cs
// See https://aka.ms/new-console-template for more information
using Hexa.NET.ImGui;
using Hexa.NET.KittyUI;

AppBuilder builder = new();
builder.AddWindow("Main Window", () =>
{
    ImGui.Text("Hello, World!");
});
builder.Run();
```

## License

This project is licensed under the MIT License - see the [LICENSE](https://github.com/HexaEngine/Hexa.NET.KittyUI/blob/master/LICENSE.txt) file for details.
