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

using System.Collections.Generic;
using System.Windows;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// The materialized dialog window for a <see cref="DialogSpec"/>: a content-sized
    /// <see cref="Controls.FluenceWindow"/> hosting the composed elements, an inline validation
    /// InfoBar, and a ContentDialog-style equal-fill button row. The layout mirrors the PowerShell
    /// module's in-process dialog builder so both paths look and behave identically.
    /// </summary>
    /// <remarks>
    /// Construct via <see cref="SpecMaterializer.Materialize"/> on an STA thread with the theme
    /// slots seeded, then call <see cref="ShowAndCollect"/>. A non-cancel button validates the
    /// declarative rules and keeps the window open on failure; a cancel button (or closing the
    /// window) skips validation and reports the button identity "Cancelled".
    /// </remarks>
    public sealed class SpecDialogWindow : Controls.FluenceWindow
    {
        /// <summary>
        /// The <see cref="SpecDialogResult.Button"/> value reported when the dialog is dismissed
        /// without a button (window close) or by a cancel button without a name.
        /// </summary>
        public const string CancelledButtonIdentity = "Cancelled";

        private readonly List<KeyValuePair<SpecNode, FrameworkElement>> _pairs = [];
        private readonly Controls.InfoBar _validationBar;
        private SpecDialogResult? _result;

        internal SpecDialogWindow(DialogSpec spec)
        {
            Title = spec.Title ?? string.Empty;
            SizeToContent = SizeToContent.WidthAndHeight;
            MinWidth = 380;
            MinHeight = 120;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Root stack inside a padded border; controls resolve their own themed brushes.
            System.Windows.Controls.Border border = new()
            {
                Padding = new Thickness(24),
            };
            System.Windows.Controls.StackPanel root = new()
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
            };
            border.Child = root;

            foreach (SpecNode node in spec.Content)
            {
                FrameworkElement element = SpecMaterializer.CreateElementTracked(node, _pairs);
                element.HorizontalAlignment = HorizontalAlignment.Stretch;
                if (node.Margin is null)
                {
                    element.Margin = new Thickness(0, 0, 0, 4);
                }
                _ = root.Children.Add(element);
            }

            // Inline validation InfoBar, hidden until a rule fails.
            _validationBar = new Controls.InfoBar
            {
                Severity = InfoBarSeverity.Error,
                IsOpen = false,
                Margin = new Thickness(0, 8, 0, 0),
            };
            _ = root.Children.Add(_validationBar);

            _ = root.Children.Add(BuildButtonRow(spec));
            Content = border;
        }

        /// <summary>
        /// Shows the dialog modally and returns the clicked button identity plus the harvested
        /// values of every named input element.
        /// </summary>
        /// <returns>The dialog result; Button is "Cancelled" when no button closed the dialog.</returns>
        public SpecDialogResult ShowAndCollect()
        {
            _ = ShowDialog();
            return _result ?? CreateResult(CancelledButtonIdentity, harvest: true);
        }

        private System.Windows.Controls.Grid BuildButtonRow(DialogSpec spec)
        {
            // Layout order mirrors ContentDialog CommandSpace: default first, then neither, then
            // cancel; a lone button fills only the right of two columns.
            List<ButtonSpec> ordered = [];
            foreach (ButtonSpec button in spec.Buttons)
            {
                if (button.IsDefault)
                {
                    ordered.Add(button);
                }
            }
            foreach (ButtonSpec button in spec.Buttons)
            {
                if (!button.IsDefault && !button.IsCancel)
                {
                    ordered.Add(button);
                }
            }
            foreach (ButtonSpec button in spec.Buttons)
            {
                if (button.IsCancel && !button.IsDefault)
                {
                    ordered.Add(button);
                }
            }

            System.Windows.Controls.Grid grid = new()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(0, 16, 0, 0),
            };
            int count = ordered.Count;
            int columnCount = count == 1 ? 2 : count;
            for (int column = 0; column < columnCount; column++)
            {
                grid.ColumnDefinitions.Add(new System.Windows.Controls.ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star),
                });
            }
            for (int index = 0; index < count; index++)
            {
                ButtonSpec buttonSpec = ordered[index];

                // 4px half-margins produce 8px gaps between adjacent buttons and 0 at the outer
                // edges; a lone button sits in the right column and takes the right-most margin.
                Controls.Button button = new()
                {
                    Content = buttonSpec.Text,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    MinWidth = 0,
                    IsDefault = buttonSpec.IsDefault,
                    IsCancel = buttonSpec.IsCancel,
                    Margin = count == 1
                        ? new Thickness(4, 0, 0, 0)
                        : index == 0
                        ? new Thickness(0, 0, 4, 0)
                        : index == count - 1
                        ? new Thickness(4, 0, 0, 0)
                        : new Thickness(4, 0, 4, 0),
                };
                if (buttonSpec.IsDefault)
                {
                    button.Appearance = ControlAppearance.Accent;
                }
                System.Windows.Controls.Grid.SetColumn(button, count == 1 ? 1 : index);

                ButtonSpec captured = buttonSpec;
                button.Click += (sender, args) => OnDialogButtonClick(captured);
                _ = grid.Children.Add(button);
            }
            return grid;
        }

        private void OnDialogButtonClick(ButtonSpec buttonSpec)
        {
            if (!buttonSpec.IsCancel)
            {
                string? failure = SpecRuleEvaluator.Evaluate(_pairs);
                if (failure is not null)
                {
                    _validationBar.Message = failure;
                    _validationBar.Severity = InfoBarSeverity.Error;
                    _validationBar.IsOpen = true;
                    return;
                }
            }
            string identity = buttonSpec.Name ?? buttonSpec.Text ?? CancelledButtonIdentity;
            _result = CreateResult(identity, harvest: true);
            Close();
        }

        private SpecDialogResult CreateResult(string buttonIdentity, bool harvest)
        {
            SpecDialogResult result = new()
            {
                Button = buttonIdentity,
            };
            if (harvest)
            {
                foreach (KeyValuePair<SpecNode, FrameworkElement> pair in _pairs)
                {
                    SpecMaterializer.HarvestValue(pair.Key, pair.Value, result.Values);
                }
            }
            return result;
        }
    }
}
