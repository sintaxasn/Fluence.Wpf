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
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */
using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Shell;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Native;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class FluenceWindowTitleBarTests
    {
        private static void RunOnFreshStaThread(Action action)
        {
            Exception capturedException = null;
            WpfTestSta.Dispatcher.Invoke(new Action(delegate
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    capturedException = exception;
                }
            }));

            if (capturedException != null)
            {
                ExceptionDispatchInfo.Capture(capturedException).Throw();
            }
        }

        private static Application EnsureApplication()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static ResourceDictionary MergeTheme(Application application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            var dictionaries = application.Resources.MergedDictionaries;
            return dictionaries.Count > 0 ? dictionaries[dictionaries.Count - 1] : null;
        }

        private static void RunWithWindow(Action<FluenceWindow> testBody)
        {
            RunOnFreshStaThread(() =>
            {
                var app = EnsureApplication();
                var dict = MergeTheme(app);
                FluenceWindow window = null;

                try
                {
                    window = new FluenceWindow();
                    testBody(window);
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        /// <summary>
        /// Shows a FluenceWindow off-screen so template parts (caption buttons) exist for hit-testing.
        /// </summary>
        private static void RunWithShownWindow(Action<FluenceWindow> testBody)
        {
            RunOnFreshStaThread(() =>
            {
                var app = EnsureApplication();
                var dict = MergeTheme(app);
                FluenceWindow window = null;

                try
                {
                    window = new FluenceWindow
                    {
                        Width = 520,
                        Height = 360,
                        Left = -20000,
                        Top = -20000,
                        ExtendsContentIntoTitleBar = true,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false
                    };
                    window.Show();
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded);
                    testBody(window);
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        private static int InvokeHitTestTitleBar(FluenceWindow window, IntPtr lParam)
        {
            var method = typeof(FluenceWindow).GetMethod(
                "HitTestTitleBar",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method, "HitTestTitleBar must exist for caption hit-test tests.");
            return (int)method.Invoke(window, new object[] { lParam });
        }

        private static IntPtr MakeLParamScreen(double screenX, double screenY)
        {
            int x = (int)screenX;
            int y = (int)screenY;
            return (IntPtr)((y << 16) | (x & 0xffff));
        }

        private static System.Windows.Controls.Button GetCaptionButtonField(FluenceWindow window, string fieldName)
        {
            var field = typeof(FluenceWindow).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, "Caption button field must exist: " + fieldName);
            return field.GetValue(window) as System.Windows.Controls.Button;
        }

        #region 1. ExtendsContentIntoTitleBar default

        [TestMethod]
        public void ExtendsContentIntoTitleBar_DefaultIsFalse()
        {
            RunWithWindow(w =>
            {
                Assert.IsFalse(w.ExtendsContentIntoTitleBar,
                    "ExtendsContentIntoTitleBar should default to false.");
            });
        }

        #endregion

        #region 2. TitleBarHeight default

        [TestMethod]
        public void TitleBarHeight_DefaultIs48()
        {
            RunWithWindow(w =>
            {
                Assert.AreEqual(48d, w.TitleBarHeight,
                    "TitleBarHeight should default to 48.");
            });
        }

        #endregion

        #region 3. ShowIcon and ShowTitle defaults

        [TestMethod]
        public void ShowIcon_DefaultIsTrue()
        {
            RunWithWindow(w =>
            {
                Assert.IsTrue(w.ShowIcon, "ShowIcon should default to true.");
            });
        }

        [TestMethod]
        public void ShowTitle_DefaultIsTrue()
        {
            RunWithWindow(w =>
            {
                Assert.IsTrue(w.ShowTitle, "ShowTitle should default to true.");
            });
        }

        #endregion

        #region 4. Caption button visibility defaults

        [TestMethod]
        public void CaptionButtonVisibility_DefaultsAreVisible()
        {
            RunWithWindow(w =>
            {
                Assert.AreEqual(Visibility.Visible, w.MinimizeButtonVisibility);
                Assert.AreEqual(Visibility.Visible, w.MaximizeButtonVisibility);
                Assert.AreEqual(Visibility.Visible, w.CloseButtonVisibility);
            });
        }

        [TestMethod]
        public void CaptionButtonEnabled_DefaultsAreTrue()
        {
            RunWithWindow(w =>
            {
                Assert.IsTrue(w.IsMinimizable);
                Assert.IsTrue(w.IsMaximizable);
                Assert.IsTrue(w.IsClosable);
            });
        }

        #endregion

        #region 5. HasShadow and WindowBorder defaults

        [TestMethod]
        public void HasShadow_DefaultIsTrue()
        {
            RunWithWindow(w =>
            {
                Assert.IsTrue(w.HasShadow, "HasShadow should default to true.");
            });
        }

        [TestMethod]
        public void BorderThickness_DefaultIsOne()
        {
            RunWithWindow(w =>
            {
                Assert.AreEqual(new Thickness(1), w.BorderThickness,
                    "BorderThickness should default to 1 (window chrome stroke from default style).");
            });
        }

        #endregion

        #region 6. SetTitleBar method

        [TestMethod]
        public void SetTitleBar_SetsTitleBarProperty()
        {
            RunWithWindow(w =>
            {
                var customElement = new System.Windows.Controls.TextBlock { Text = "Custom Title" };
                w.SetTitleBar(customElement);
                Assert.AreSame(customElement, w.TitleBar,
                    "SetTitleBar should assign the element to the TitleBar property.");
            });
        }

        [TestMethod]
        public void SetTitleBar_NullReverts()
        {
            RunWithWindow(w =>
            {
                var customElement = new System.Windows.Controls.TextBlock { Text = "Custom Title" };
                w.SetTitleBar(customElement);
                Assert.IsNotNull(w.TitleBar);

                w.SetTitleBar(null);
                Assert.IsNull(w.TitleBar,
                    "SetTitleBar(null) should revert the TitleBar to null.");
            });
        }

        #endregion

        #region 7. WindowChrome updates

        [TestMethod]
        public void CaptionHeight_AlwaysZero_RegardlessOfExtendsContentIntoTitleBar()
        {
            RunWithWindow(w =>
            {
                var chrome = WindowChrome.GetWindowChrome(w);
                Assert.IsNotNull(chrome, "FluenceWindow should have a WindowChrome attached.");
                Assert.AreEqual(0d, chrome.CaptionHeight,
                    "CaptionHeight must always be 0 — drag region is handled by WM_NCHITTEST.");

                w.ExtendsContentIntoTitleBar = true;

                Assert.AreEqual(0d, chrome.CaptionHeight,
                    "CaptionHeight must remain 0 when content extends into title bar.");
            });
        }

        [TestMethod]
        public void HasShadow_False_SetsGlassFrameToZero()
        {
            RunWithWindow(w =>
            {
                var chrome = WindowChrome.GetWindowChrome(w);
                Assert.AreEqual(new Thickness(-1), chrome.GlassFrameThickness,
                    "Default GlassFrameThickness should be -1 for shadow.");

                w.HasShadow = false;

                Assert.AreEqual(new Thickness(0), chrome.GlassFrameThickness,
                    "GlassFrameThickness should be 0 when HasShadow is false.");
            });
        }

        #endregion

        #region Bug Fix Tests — Title Bar Flash and Theme Switching

        [TestMethod]
        public void CaptionHeight_IsZero_EvenBeforeExtendsContentIntoTitleBar()
        {
            RunWithWindow(w =>
            {
                var chrome = WindowChrome.GetWindowChrome(w);
                Assert.IsNotNull(chrome);
                Assert.AreEqual(0d, chrome.CaptionHeight,
                    "CaptionHeight must be 0 from construction — WM_NCHITTEST handles all drag regions.");
            });
        }

        [TestMethod]
        public void WindowChrome_AppliedInConstructor()
        {
            RunWithWindow(w =>
            {
                var chrome = WindowChrome.GetWindowChrome(w);
                Assert.IsNotNull(chrome,
                    "WindowChrome must be attached during FluenceWindow construction, not deferred to Loaded.");
            });
        }

        [TestMethod]
        public void DefaultBorderThickness_IsOne()
        {
            RunWithWindow(w =>
            {
                Assert.AreEqual(new Thickness(1), w.BorderThickness,
                    "FluenceWindow default BorderThickness must be 1 (chrome border).");
            });
        }

        [TestMethod]
        public void ThemeSwitch_UpdatesWindowBackground()
        {
            RunOnFreshStaThread(() =>
            {
                var app = EnsureApplication();
                var dict = MergeTheme(app);
                FluenceWindow window = null;

                try
                {
                    window = new FluenceWindow();
                    var lightBg = window.Background;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    var darkBg = window.Background;

                    Assert.AreNotEqual(lightBg, darkBg,
                        "Window background must change after theme switch from Light to Dark.");
                }
                finally
                {
                    if (window != null)
                    {
                        window.Close();
                    }

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void ThemeChanged_FiresOnApply()
        {
            RunOnFreshStaThread(() =>
            {
                var app = EnsureApplication();
                var dict = MergeTheme(app);
                int fireCount = 0;
                EventHandler<ThemeChangedEventArgs> handler = (s, e) => fireCount++;

                try
                {
                    ApplicationThemeManager.Changed += handler;
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    Assert.AreEqual(1, fireCount,
                        "ApplicationThemeManager.Changed must fire exactly once per Apply call.");
                }
                finally
                {
                    ApplicationThemeManager.Changed -= handler;
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void FluenceWindowXaml_NoStaticResourceForThemeBrushes()
        {
            var xamlUri = new System.Uri(
                "pack://application:,,,/Fluence.Wpf;component/Themes/Controls/FluenceWindow.xaml",
                System.UriKind.Absolute);
            var rd = new ResourceDictionary { Source = xamlUri };

            var xamlPath = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Fluence.Wpf\Themes\Controls\FluenceWindow.xaml");

            if (System.IO.File.Exists(xamlPath))
            {
                string xaml = System.IO.File.ReadAllText(xamlPath);
                string[] themeBrushKeys = new[]
                {
                    "ApplicationBackgroundBrush",
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                    "TextFillColorDisabledBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                    "CardStrokeColorDefaultSolidBrush"
                };

                foreach (var key in themeBrushKeys)
                {
                    string staticPattern = "StaticResource " + key;
                    Assert.IsFalse(xaml.Contains(staticPattern),
                        "FluenceWindow.xaml must not use StaticResource for theme brush: " + key);
                }
            }
        }

        [TestMethod]
        public void FullThemeCycle_KeyBrushesResolve()
        {
            RunOnFreshStaThread(() =>
            {
                var app = EnsureApplication();
                var dict = MergeTheme(app);

                try
                {
                    var themes = new[] { ApplicationTheme.Dark, ApplicationTheme.Light };
                    foreach (var theme in themes)
                    {
                        ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                        var bg = app.TryFindResource("ApplicationBackgroundBrush");
                        Assert.IsNotNull(bg,
                            "ApplicationBackgroundBrush must resolve after switching to " + theme);
                        var fg = app.TryFindResource("TextFillColorPrimaryBrush");
                        Assert.IsNotNull(fg,
                            "TextFillColorPrimaryBrush must resolve after switching to " + theme);
                    }
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [TestMethod]
        public void MergedDictionaries_CountStableAfterMultipleSwitches()
        {
            RunOnFreshStaThread(() =>
            {
                var app = EnsureApplication();
                var dict = MergeTheme(app);

                try
                {
                    int initialCount = app.Resources.MergedDictionaries.Count;

                    for (int i = 0; i < 5; i++)
                    {
                        var theme = i % 2 == 0 ? ApplicationTheme.Dark : ApplicationTheme.Light;
                        ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    }

                    Assert.AreEqual(initialCount, app.Resources.MergedDictionaries.Count,
                        "MergedDictionaries count must remain stable after 5 theme switches.");
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);

                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        #endregion

        #region Caption button hit-test (WM_NCHITTEST vs WPF commands)

        [TestMethod]
        public void FluenceWindowXaml_CaptionButtonsUseSystemCommands()
        {
            var xamlPath = System.IO.Path.Combine(
                System.AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Fluence.Wpf\Themes\Controls\FluenceWindow.xaml");
            xamlPath = System.IO.Path.GetFullPath(xamlPath);

            Assert.IsTrue(System.IO.File.Exists(xamlPath),
                "FluenceWindow.xaml should be readable at: " + xamlPath);

            string xaml = System.IO.File.ReadAllText(xamlPath);
            Assert.IsTrue(
                xaml.IndexOf("MinimizeWindowCommand", StringComparison.Ordinal) >= 0,
                "Minimize button should bind MinimizeWindowCommand.");
            Assert.IsTrue(
                xaml.IndexOf("MaximizeWindowCommand", StringComparison.Ordinal) >= 0,
                "Maximize button should bind MaximizeWindowCommand.");
            Assert.IsTrue(
                xaml.IndexOf("CloseWindowCommand", StringComparison.Ordinal) >= 0,
                "Close button should bind CloseWindowCommand.");
        }

        [TestMethod]
        public void HitTestTitleBar_MinimizeButton_ReturnsZero_NotHtMinButton()
        {
            RunWithShownWindow(w =>
            {
                var btn = GetCaptionButtonField(w, "_minimizeButton");
                Assert.IsNotNull(btn, "Minimize template part should exist after Show.");
                Assert.AreEqual(Visibility.Visible, btn.Visibility);

                var center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                int hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.AreEqual(0, hit,
                    "Minimize area must return 0 so WPF receives client clicks (not HTMINBUTTON).");
                Assert.AreNotEqual(NativeConstants.HTMINBUTTON, hit);
            });
        }

        [TestMethod]
        public void HitTestTitleBar_CloseButton_ReturnsZero_NotHtClose()
        {
            RunWithShownWindow(w =>
            {
                var btn = GetCaptionButtonField(w, "_closeButton");
                Assert.IsNotNull(btn);

                var center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                int hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.AreEqual(0, hit,
                    "Close area must return 0 so WPF receives client clicks (not HTCLOSE).");
                Assert.AreNotEqual(NativeConstants.HTCLOSE, hit);
            });
        }

        [TestMethod]
        public void HitTestTitleBar_MaximizeButton_ReturnsHtMaxButton()
        {
            RunWithShownWindow(w =>
            {
                Assert.AreEqual(WindowState.Normal, w.WindowState);
                var btn = GetCaptionButtonField(w, "_maximizeButton");
                Assert.IsNotNull(btn);
                Assert.AreEqual(Visibility.Visible, btn.Visibility);

                var center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                int hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.AreEqual(NativeConstants.HTMAXBUTTON, hit,
                    "Maximize area should return HTMAXBUTTON for snap layout support.");
            });
        }

        [TestMethod]
        public void HitTestTitleBar_TitleBarDragArea_ReturnsHtCaption()
        {
            RunWithShownWindow(w =>
            {
                w.UpdateLayout();
                var clientMidTitle = new Point(Math.Max(40, w.ActualWidth / 2), Math.Max(1, w.TitleBarHeight / 2));
                var screen = w.PointToScreen(clientMidTitle);
                int hit = InvokeHitTestTitleBar(w, MakeLParamScreen(screen.X, screen.Y));
                Assert.AreEqual(NativeConstants.HTCAPTION, hit,
                    "Title bar drag strip should return HTCAPTION.");
            });
        }

        [TestMethod]
        public void NativeConstants_DefinesWmNcLButtonUp()
        {
            Assert.AreEqual(0x00A2, NativeConstants.WM_NCLBUTTONUP);
        }

        #endregion

        #region 8. PasswordBox.SelectAll

        [TestMethod]
        public void PasswordBox_SelectAll_DoesNotThrowWithoutTemplate()
        {
            RunOnFreshStaThread(() =>
            {
                var app = EnsureApplication();
                var dict = MergeTheme(app);

                try
                {
                    var passwordBox = new Fluence.Wpf.Controls.PasswordBox();
                    passwordBox.SelectAll();
                }
                finally
                {
                    if (dict != null)
                    {
                        app.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        #endregion

        #region WM_GETMINMAXINFO

        [TestMethod]
        public void MinMaxInfo_StructLayout_HasCorrectSize()
        {
            // MINMAXINFO must be 5 POINTs = 5 * 8 bytes = 40 bytes.
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MINMAXINFO));
            Assert.AreEqual(40, size);
        }

        [TestMethod]
        public void MonitorInfo_StructLayout_HasCorrectSize()
        {
            // MONITORINFO = int + 3 RECTs (16 bytes each) + uint = 4 + 16 + 16 + 16 + 4 = 40 bytes.
            // Actually: cbSize(4) + rcMonitor(16) + rcWork(16) + dwFlags(4) = 40 bytes.
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MONITORINFO));
            Assert.AreEqual(40, size);
        }

        [TestMethod]
        public void MinMaxInfo_RoundTrip_PreservesValues()
        {
            var mmi = new MINMAXINFO
            {
                ptMaxPosition = new POINT { X = 10, Y = 20 },
                ptMaxSize = new POINT { X = 1920, Y = 1040 },
                ptMaxTrackSize = new POINT { X = 3840, Y = 2160 },
                ptMinTrackSize = new POINT { X = 200, Y = 150 }
            };

            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MINMAXINFO));
            var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
            try
            {
                System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, ptr, false);
                var result = (MINMAXINFO)System.Runtime.InteropServices.Marshal.PtrToStructure(ptr, typeof(MINMAXINFO));

                Assert.AreEqual(10, result.ptMaxPosition.X);
                Assert.AreEqual(20, result.ptMaxPosition.Y);
                Assert.AreEqual(1920, result.ptMaxSize.X);
                Assert.AreEqual(1040, result.ptMaxSize.Y);
                Assert.AreEqual(3840, result.ptMaxTrackSize.X);
                Assert.AreEqual(2160, result.ptMaxTrackSize.Y);
                Assert.AreEqual(200, result.ptMinTrackSize.X);
                Assert.AreEqual(150, result.ptMinTrackSize.Y);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
            }
        }

        [TestMethod]
        public void NativeConstants_WmGetMinMaxInfo_HasCorrectValue()
        {
            Assert.AreEqual(0x0024, NativeConstants.WM_GETMINMAXINFO);
        }

        [TestMethod]
        public void NativeConstants_MonitorDefaultToNearest_HasCorrectValue()
        {
            Assert.AreEqual(2u, NativeConstants.MONITOR_DEFAULTTONEAREST);
        }

        #endregion
    }
}
