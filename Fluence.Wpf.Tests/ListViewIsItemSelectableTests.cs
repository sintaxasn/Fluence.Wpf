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
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Fluence.Wpf;
using FluenceListView = Fluence.Wpf.Controls.ListView;
using WpfListViewItem = System.Windows.Controls.ListViewItem;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class ListViewIsItemSelectableTests
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

        private static ResourceDictionary MergeGenericDictionary(Application application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            var dictionaries = application.Resources.MergedDictionaries;
            var genericDictionary = dictionaries.Count > 0 ? dictionaries[dictionaries.Count - 1] : null;

            var demoShared = new ResourceDictionary
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            };
            application.Resources.MergedDictionaries.Add(demoShared);

            return genericDictionary;
        }

        private static void DrainDispatcher(Dispatcher dispatcher)
        {
            dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(delegate { }));
        }

        [TestMethod]
        public void IsItemSelectable_DefaultIsTrue()
        {
            RunOnFreshStaThread(() =>
            {
                var lv = new FluenceListView();
                Assert.IsTrue(lv.IsItemSelectable);
            });
        }

        [TestMethod]
        public void IsItemSelectable_False_ClearsSelectionWhenSet()
        {
            RunOnFreshStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var lv = new FluenceListView { Width = 260, Height = 120 };
                lv.Items.Add("a");
                lv.Items.Add("b");

                try
                {
                    window.Content = lv;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    lv.SelectedIndex = 0;
                    Assert.AreEqual(0, lv.SelectedIndex);

                    lv.IsItemSelectable = false;
                    Assert.AreEqual(-1, lv.SelectedIndex,
                        "Turning off IsItemSelectable should clear selection.");
                }
                finally
                {
                    window.Close();
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void IsItemSelectable_False_SelectedIndexStaysMinusOne_AfterDirectSet()
        {
            RunOnFreshStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var lv = new FluenceListView
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = false
                };
                lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    lv.SelectedIndex = 0;
                    Assert.AreEqual(-1, lv.SelectedIndex,
                        "Selection must not stick when IsItemSelectable is false.");
                }
                finally
                {
                    window.Close();
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void IsItemSelectable_False_ContainerIsNotFocusable()
        {
            RunOnFreshStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var lv = new FluenceListView
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = false
                };
                lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var container = lv.ItemContainerGenerator.ContainerFromIndex(0) as WpfListViewItem;
                    Assert.IsNotNull(container, "Item container should be generated.");
                    Assert.IsFalse(container.Focusable);
                    Assert.IsFalse(FluenceListView.GetParentIsItemSelectable(container));
                }
                finally
                {
                    window.Close();
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void IsItemSelectable_True_ContainerIsFocusable()
        {
            RunOnFreshStaThread(() =>
            {
                var application = EnsureApplication();
                var genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                var window = new Window();
                var lv = new FluenceListView
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = true
                };
                lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    var container = lv.ItemContainerGenerator.ContainerFromIndex(0) as WpfListViewItem;
                    Assert.IsNotNull(container);
                    Assert.IsTrue(container.Focusable);
                    Assert.IsTrue(FluenceListView.GetParentIsItemSelectable(container));
                }
                finally
                {
                    window.Close();
                    if (genericDictionary != null)
                    {
                        application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void ItemAnimationsEnabled_IndependentOfIsItemSelectable()
        {
            RunOnFreshStaThread(() =>
            {
                var lv = new FluenceListView { IsItemSelectable = false, ItemAnimationsEnabled = true };
                Assert.IsFalse(lv.IsItemSelectable);
                Assert.IsTrue(lv.ItemAnimationsEnabled);

                lv.ItemAnimationsEnabled = false;
                lv.IsItemSelectable = true;
                Assert.IsTrue(lv.IsItemSelectable);
                Assert.IsFalse(lv.ItemAnimationsEnabled);
            });
        }
    }
}
