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
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDER AND CONTRIBUTORS "AS IS"
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

#if NET10_0_OR_GREATER
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Xml;
using System.Xml.Linq;
using Fluence.Wpf.Demo.Mvvm;
using Fluence.Wpf.Demo.Mvvm.ViewModels;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery
{
    /// <summary>
    /// Exercises the Task Manager sample and navigation starter settings through live in-process
    /// UI Automation peers.
    /// </summary>
    public sealed class TaskManagerAccessibilityTests : IAsyncLifetime
    {
        private const string TaskTitle = "Accessible test task";
        private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        [Fact]
        public Task MainWindow_TaskCommandsExposeContextualNamesAndPatternsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                MainWindow window = new()
                {
                    Left = -20000,
                    Top = -20000,
                    Width = 680,
                    Height = 580,
                    ShowInTaskbar = false,
                };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    MainViewModel viewModel = Assert.IsType<MainViewModel>(window.DataContext, exactMatch: true);
                    viewModel.NewTaskText = TaskTitle;
                    viewModel.AddCommand.Execute(parameter: null);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Fluence.Wpf.Controls.TextBox input = Assert.Single(collection: FindVisualChildren<Fluence.Wpf.Controls.TextBox>(window));
                    AutomationPeer inputPeer = Assert.IsAssignableFrom<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(input));
                    AutomationPeer inputLabel = Assert.IsAssignableFrom<AutomationPeer>(inputPeer.GetLabeledBy());
                    Assert.Equal("New task", inputLabel.GetName());
                    Assert.Equal("New task", inputPeer.GetName());
                    Assert.Equal(AutomationControlType.Edit, inputPeer.GetAutomationControlType());

                    Fluence.Wpf.Controls.CheckBox completion = Assert.Single(
                        FindVisualChildren<Fluence.Wpf.Controls.CheckBox>(window),
                        static control => control.DataContext is TaskItemViewModel item && string.Equals(item.Title, TaskTitle, StringComparison.Ordinal));
                    TaskItemViewModel taskItem = Assert.IsType<TaskItemViewModel>(completion.DataContext, exactMatch: true);
                    AutomationPeer completionPeer = Assert.IsAssignableFrom<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(completion));
                    Assert.Equal("Complete " + TaskTitle, completionPeer.GetName());
                    Assert.Equal(TaskTitle, Assert.IsAssignableFrom<AutomationPeer>(completionPeer.GetLabeledBy()).GetName());
                    Assert.Equal(AutomationControlType.CheckBox, completionPeer.GetAutomationControlType());
                    IToggleProvider toggleProvider = Assert.IsAssignableFrom<IToggleProvider>(completionPeer.GetPattern(PatternInterface.Toggle));
                    Assert.Equal(ToggleState.Off, toggleProvider.ToggleState);
                    toggleProvider.Toggle();
                    Assert.Equal(ToggleState.On, toggleProvider.ToggleState);
                    Assert.True(taskItem.IsCompleted);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Fluence.Wpf.Controls.Button delete = Assert.Single(
                        FindVisualChildren<Fluence.Wpf.Controls.Button>(window),
                        static control => control.DataContext is TaskItemViewModel item && string.Equals(item.Title, TaskTitle, StringComparison.Ordinal));
                    AutomationPeer deletePeer = Assert.IsAssignableFrom<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(delete));
                    Assert.Equal("Delete " + TaskTitle, deletePeer.GetName());
                    Assert.Equal(AutomationControlType.Button, deletePeer.GetAutomationControlType());
                    IInvokeProvider invokeProvider = Assert.IsAssignableFrom<IInvokeProvider>(deletePeer.GetPattern(PatternInterface.Invoke));
                    invokeProvider.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.DoesNotContain(viewModel.DisplayedTasks, static item => string.Equals(item.Title, TaskTitle, StringComparison.Ordinal));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NavigationStarter_SettingsSwitchesExposeTheirVisibleLabelsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                string path = DemoTestHost.GetRepositoryFilePath(
                    "templates", "Fluence.Wpf.Templates", "content", "navigation-mvvm", "Views", "SettingsView.xaml");
                XDocument document = XDocument.Load(path);
                document.Root?.Attribute(XamlNamespace + "Class")?.Remove();

                using XmlReader reader = document.CreateReader();
                UserControl settingsView = Assert.IsType<UserControl>(XamlReader.Load(reader));
                Window window = DemoTestHost.CreateHostWindow(settingsView);
                try
                {
                    Fluence.Wpf.Controls.ToggleSwitch[] switches = [.. FindVisualChildren<Fluence.Wpf.Controls.ToggleSwitch>(settingsView)];
                    Assert.Equal(2, switches.Length);
                    string[] expectedLabels = ["Helpful tips preference", "Compact navigation preference"];
                    for (int i = 0; i < switches.Length; i++)
                    {
                        Fluence.Wpf.Controls.ToggleSwitch toggleSwitch = switches[i];
                        AutomationPeer peer = Assert.IsAssignableFrom<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(toggleSwitch));
                        Assert.Equal(expectedLabels[i], peer.GetName());
                        AutomationPeer label = Assert.IsAssignableFrom<AutomationPeer>(peer.GetLabeledBy());
                        Assert.Equal(expectedLabels[i], label.GetName());
                        Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());

                        IToggleProvider toggleProvider = Assert.IsAssignableFrom<IToggleProvider>(peer.GetPattern(PatternInterface.Toggle));
                        Assert.Equal(ToggleState.Off, toggleProvider.ToggleState);
                        toggleProvider.Toggle();
                        Assert.Equal(ToggleState.On, toggleProvider.ToggleState);
                        Assert.True(toggleSwitch.IsChecked);
                    }
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
    }
}
#endif



