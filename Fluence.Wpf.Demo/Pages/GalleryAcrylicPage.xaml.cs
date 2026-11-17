/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE
 * POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Demonstrates direct and hosted in-app acrylic with a live same-window visual source.
    /// </summary>
    public partial class GalleryAcrylicPage : Page
    {
        private const string DefaultRecipeKey = "AcrylicInAppDefaultRecipe";
        private const string FallbackRecipeKey = "GalleryAcrylicFallbackRecipe";
        private readonly DispatcherTimer _sourceMotionTimer;
        private Popup? _popup;

        /// <summary>
        /// Initializes the sample page and attaches its live source and sample-code tabs.
        /// </summary>
        public GalleryAcrylicPage()
        {
            InitializeComponent();
            DemoSamplePageWiring.Apply(
                PageRoot,
                new DemoSampleSource(1, DirectAndHostXamlSource, DirectAndHostCSharpSource),
                new DemoSampleSource(2, PopupXamlSource, PopupCSharpSource));

            _sourceMotionTimer = new DispatcherTimer(DispatcherPriority.Render, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(40),
            };
            _sourceMotionTimer.Tick += SourceMotionTimer_Tick;
            Loaded += Page_Loaded;
            Unloaded += Page_Unloaded;
        }

        private static string DirectAndHostXamlSource => DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Acrylic.InApp",
            "    <UserControl.Resources>\n" +
            "        <fluence:AcrylicMaterialRecipe x:Key=\"GalleryAcrylicFixedRecipe\" TintColor=\"White\" TintOpacity=\"0.5\" FallbackColor=\"White\" />\n" +
            "    </UserControl.Resources>\n" +
            "    <Grid>\n" +
            "        <Grid.ColumnDefinitions>\n" +
            "            <ColumnDefinition Width=\"*\" />\n" +
            "            <ColumnDefinition Width=\"*\" />\n" +
            "            <ColumnDefinition Width=\"*\" />\n" +
            "        </Grid.ColumnDefinitions>\n" +
            "        <Grid x:Name=\"BackdropSource\" Grid.ColumnSpan=\"3\" ClipToBounds=\"True\">\n" +
            "            <Ellipse Fill=\"Gold\" />\n" +
            "        </Grid>\n" +
            "        <!-- Direct Border.Background uses its fixed recipe. -->\n" +
            "        <Border Grid.Column=\"1\" Background=\"{fluence:AcrylicBrush BackdropSourceName=BackdropSource, Recipe={StaticResource GalleryAcrylicFixedRecipe}}\">\n" +
            "            <TextBlock Foreground=\"#1F1F1F\" Text=\"Sharp text\" />\n" +
            "        </Border>\n" +
            "        <fluence:AcrylicElement Grid.Column=\"2\" BackdropSource=\"{Binding ElementName=BackdropSource}\"\n" +
            "                                 Recipe=\"{DynamicResource AcrylicInAppDefaultRecipe}\">\n" +
            "            <TextBlock Text=\"Sharp text\" />\n" +
            "        </fluence:AcrylicElement>\n" +
            "    </Grid>\n");

        private const string DirectAndHostCSharpSource =
            "using System.Windows.Controls;\n" +
            "using Fluence.Wpf.Controls;\n\n" +
            "namespace Fluence.Wpf.Demo.Pages.Acrylic\n" +
            "{\n" +
            "    public partial class InApp : UserControl\n" +
            "    {\n" +
            "        public InApp()\n" +
            "        {\n" +
            "            InitializeComponent();\n" +
            "        }\n\n" +
            "        private static void SelectThemeRecipe(AcrylicElement host, string recipeKey)\n" +
            "        {\n" +
            "            host.SetResourceReference(AcrylicElement.RecipeProperty, recipeKey);\n" +
            "        }\n" +
            "    }\n" +
            "}\n";

        private static string PopupXamlSource => DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.Acrylic.PopupBoundary",
            "    <Grid>\n" +
            "        <Grid x:Name=\"MainWindowBackdropSource\">\n" +
            "            <Ellipse Fill=\"DodgerBlue\" />\n" +
            "        </Grid>\n" +
            "        <Button Click=\"OpenPopup_Click\" Content=\"Open Popup\" />\n" +
            "        <TextBlock x:Name=\"PopupFallbackReason\" />\n" +
            "    </Grid>\n");

        private const string PopupCSharpSource =
            "using System.Windows;\n" +
            "using System.Windows.Controls;\n" +
            "using System.Windows.Controls.Primitives;\n" +
            "using Fluence.Wpf.Controls;\n\n" +
            "namespace Fluence.Wpf.Demo.Pages.Acrylic\n" +
            "{\n" +
            "    public partial class PopupBoundary : UserControl\n" +
            "    {\n" +
            "        public PopupBoundary()\n" +
            "        {\n" +
            "            InitializeComponent();\n" +
            "        }\n\n" +
            "        private void OpenPopup_Click(object sender, RoutedEventArgs e)\n" +
            "        {\n" +
            "            AcrylicElement popupHost = new AcrylicElement { BackdropSource = MainWindowBackdropSource };\n" +
            "            Popup popup = new Popup { Child = popupHost, PlacementTarget = this, Placement = PlacementMode.Bottom };\n" +
            "            popup.Opened += (_, _) => PopupFallbackReason.Text = popupHost.IsUsingFallback ? \"Fallback\" : \"Material\";\n" +
            "            popup.IsOpen = true;\n" +
            "        }\n" +
            "    }\n" +
            "}\n";

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            DynamicAcrylicHost.FallbackReasonChanged += Host_FallbackReasonChanged;
            UpdateHostReason();
            _sourceMotionTimer.Start();
        }

        private void Page_Unloaded(object sender, RoutedEventArgs e)
        {
            DynamicAcrylicHost.FallbackReasonChanged -= Host_FallbackReasonChanged;
            _sourceMotionTimer.Stop();
            ClosePopup();
        }

        private void RecipeSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DynamicAcrylicHost is null || RecipeSelector.SelectedItem is not ComboBoxItem item || item.Tag is not string key)
            {
                return;
            }

            DynamicAcrylicHost.SetResourceReference(Fluence.Wpf.Controls.AcrylicElement.RecipeProperty, key);
            if (FallbackCheckBox?.IsChecked is true)
            {
                DynamicAcrylicHost.SetResourceReference(Fluence.Wpf.Controls.AcrylicElement.RecipeProperty, FallbackRecipeKey);
            }
        }

        private void FallbackCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (DynamicAcrylicHost is null || RecipeSelector is null)
            {
                return;
            }

            string key = FallbackCheckBox.IsChecked is true
                ? FallbackRecipeKey
                : (RecipeSelector.SelectedItem as ComboBoxItem)?.Tag as string ?? DefaultRecipeKey;
            DynamicAcrylicHost.SetResourceReference(Fluence.Wpf.Controls.AcrylicElement.RecipeProperty, key);
        }

        private void Host_FallbackReasonChanged(object? sender, EventArgs e)
        {
            UpdateHostReason();
        }

        private void UpdateHostReason()
        {
            FallbackReasonText.Text = DynamicAcrylicHost.FallbackReason.ToString();
        }

        private void SourceMotionTimer_Tick(object? sender, EventArgs e)
        {
            double width = Math.Max(0, BackdropSource.ActualWidth - MovingColorOne.ActualWidth);
            double height = Math.Max(0, BackdropSource.ActualHeight - MovingColorTwo.ActualHeight);
            double phase = DateTime.UtcNow.TimeOfDay.TotalSeconds * 0.22d % 1d;
            MovingTransformOne.X = width * phase;
            MovingTransformOne.Y = height * (1d - phase);
            MovingTransformTwo.X = -width * phase;
            MovingTransformTwo.Y = -height * (1d - phase);

            double popupWidth = Math.Max(0, PopupBackdropSource.ActualWidth - PopupMovingColor.ActualWidth);
            PopupMovingTransform.X = popupWidth * ((phase + 0.35d) % 1d);
        }

        private void OpenPopup_Click(object sender, RoutedEventArgs e)
        {
            ClosePopup();
            Fluence.Wpf.Controls.AcrylicElement popupHost = new()
            {
                Width = 320,
                Height = 100,
                BackdropSource = PopupBackdropSource,
                Content = new TextBlock
                {
                    Text = "Popup content remains sharp",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            string recipeKey = (RecipeSelector.SelectedItem as ComboBoxItem)?.Tag as string ?? DefaultRecipeKey;
            popupHost.SetResourceReference(Fluence.Wpf.Controls.AcrylicElement.RecipeProperty, recipeKey);
            popupHost.FallbackReasonChanged += PopupHost_FallbackReasonChanged;
            _popup = new Popup
            {
                AllowsTransparency = true,
                Child = popupHost,
                Placement = PlacementMode.Bottom,
                PlacementTarget = OpenPopupButton,
                StaysOpen = false,
            };
            _popup.Opened += Popup_Opened;
            _popup.IsOpen = true;
        }

        private void Popup_Opened(object? sender, EventArgs e)
        {
            if (_popup?.Child is Fluence.Wpf.Controls.AcrylicElement host)
            {
                PopupFallbackReasonText.Text = host.FallbackReason.ToString();
            }
        }

        private void PopupHost_FallbackReasonChanged(object? sender, EventArgs e)
        {
            if (sender is Fluence.Wpf.Controls.AcrylicElement host)
            {
                PopupFallbackReasonText.Text = host.FallbackReason.ToString();
            }
        }

        private void ClosePopup()
        {
            if (_popup?.Child is Fluence.Wpf.Controls.AcrylicElement host)
            {
                host.FallbackReasonChanged -= PopupHost_FallbackReasonChanged;
            }

            if (_popup is not null)
            {
                _popup.Opened -= Popup_Opened;
                _popup.IsOpen = false;
                _popup.Child = null;
                _popup = null;
            }
        }
    }
}
