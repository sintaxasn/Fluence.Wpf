# Fluence.Wpf.Demo

This folder contains the gallery application for visually exercising Fluence.Wpf controls. It is a WPF executable that targets `net472` and `net10.0-windows10.0.26100.0`, uses a project reference to the library, and is the primary manual verification surface for control behavior, theme switching, accent changes, and window chrome.

## What Lives Here

- `App.xaml.cs` - initial theme/accent setup and the single merge point for `Resources/DemoSharedStyles.xaml`.
- `MainWindow.xaml` / `MainWindow.xaml.cs` - the gallery shell, title-bar search, direct page navigation, lightweight Back history, and theme watcher setup.
- `Pages/` - concrete gallery pages grouped by control family or design area. Sample pages own their live samples and source snippets; named sample content is handed to `DemoSampleControl` through `DemoSamplePageWiring`. Direct reference pages such as Typography render catalog content without a trailing source expander.
- `Resources/` - app icon, banner images, control screenshots, shared demo styles, and icon catalog data.
- `DemoNavigationCatalog.cs` - flat navigation metadata used by `MainWindow`.

## Run

From the repository root:

```powershell
dotnet run --project Fluence.Wpf.Demo/Fluence.Wpf.Demo.csproj -c Debug
```

Add `-f net472` or `-f net10.0-windows10.0.26100.0` when validating a specific target framework.

Use the gallery to check Light, Dark, High Contrast, accent changes, Mica/Acrylic/Tabbed/None backdrops, keyboard focus, and representative controls after visual or interaction changes.

## Maintenance Notes

The gallery intentionally owns navigation through `NavigationView` selection and `DemoNavigationCatalog` metadata. `MainWindow` keeps a lightweight visited-page stack for the shell Back button and maps each route to a concrete `Pages/Gallery*Page.xaml` control. `DemoSampleControl` owns the reusable sample chrome, source tabs, and copy action; `DemoSamplePageWiring` owns page-local slot discovery, content transfer, and typed `DemoSampleSource` registration.
