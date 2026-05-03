# Fluence.Wpf.Demo

This folder contains the gallery application for visually exercising Fluence.Wpf controls. It is a WPF executable that targets `net472` and `net10.0-windows10.0.26100.0`, uses a project reference to the library, and is the primary manual verification surface for control behavior, theme switching, accent changes, and window chrome.

## What Lives Here

- `MainWindow.xaml` / `MainWindow.xaml.cs` - the gallery shell, title-bar search, direct page navigation, and theme watcher setup.
- `Pages/` - concrete gallery pages grouped by control family or design area. Each page owns its live samples and source snippets.
- `Resources/` - app icon, banner images, control screenshots, shared demo styles, and icon catalog data.
- `DemoNavigationCatalog.cs` - flat navigation metadata used by `MainWindow`.

## Run

From the repository root:

```powershell
dotnet run --project Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Debug
```

Use the gallery to check Light, Dark, High Contrast, accent changes, Mica/Acrylic/Tabbed/None backdrops, keyboard focus, and representative controls after visual or interaction changes.

## Maintenance Notes

The gallery intentionally owns navigation through `NavigationView` selection and `DemoNavigationCatalog` metadata; it does not maintain a page back stack. `MainWindow` maps each route to a concrete `Pages/Gallery*Page.xaml` control, and `DemoSampleControl` displays page-local `XamlSource` / `CSharpSource` strings instead of reading copied sample files from disk.