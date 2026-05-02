# Fluence.Wpf.Demo

This folder contains the gallery application for visually exercising Fluence.Wpf controls. It is a WPF executable that targets `net472` and `net10.0-windows10.0.26100.0`, uses a project reference to the library, and is the primary manual verification surface for control behavior, theme switching, accent changes, and window chrome.

## What Lives Here

- `MainWindow.xaml` / `MainWindow.xaml.cs` - the gallery shell, title-bar search, navigation tree, compact navigation flyout, and theme watcher setup.
- `Pages/` - gallery pages grouped by control area.
- `Samples/` - XAML and code-behind snippets copied to output so inline source tabs can display runnable examples.
- `Resources/` - app icon, banner images, control screenshots, shared demo styles, and icon catalog data.
- `Demo*Pages.cs` and `DemoNavigationCatalog.cs` - programmatic gallery page factories and navigation metadata.

## Run

From the repository root:

```powershell
dotnet run --project Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Debug
```

Use the gallery to check Light, Dark, High Contrast, accent changes, Mica/Acrylic/Tabbed/None backdrops, keyboard focus, and representative controls after visual or interaction changes.

## Maintenance Notes

The gallery intentionally owns navigation through `NavigationView` selection and `DemoNavigationCatalog` metadata; it does not maintain a page back stack. Samples under `Samples/**` are content files and are copied to output by the project file, so adding a sample XAML/code-behind pair normally does not require a project-file edit.
