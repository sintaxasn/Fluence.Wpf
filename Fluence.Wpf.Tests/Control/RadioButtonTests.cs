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
using System.Threading.Tasks;
using System.Windows.Automation;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="RadioButton"/> control: Description surfaces as
    /// AutomationProperties.HelpText.
    /// </summary>
    public sealed class RadioButtonTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        // ---------------------------------------------------------------------------
        // RadioButton Description -> HelpText
        // ---------------------------------------------------------------------------

        [Fact]
        public Task RadioButton_Description_SetsAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radioButton = new()
                {
                    Content = "Option A",
                    Description = "Choose this option for better performance.",
                };

                string helpText = AutomationProperties.GetHelpText(radioButton);
                Assert.True(
                    string.Equals("Choose this option for better performance.", helpText, StringComparison.Ordinal),
                    $"RadioButton.Description must be surfaced as AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task RadioButton_DescriptionChanges_UpdatesAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radioButton = new()
                {
                    Content = "Option A",
                    Description = "Initial description.",
                };

                radioButton.Description = "Revised description.";
                string helpText = AutomationProperties.GetHelpText(radioButton);
                Assert.True(
                    string.Equals("Revised description.", helpText, StringComparison.Ordinal),
                    $"RadioButton.Description change must update AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task RadioButton_NullDescription_ClearsAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radioButton = new()
                {
                    Content = "Option A",
                    Description = "Some description.",
                };
                radioButton.Description = null;

                string helpText = AutomationProperties.GetHelpText(radioButton);
                Assert.True(
                    string.IsNullOrWhiteSpace(helpText),
                    $"Null RadioButton.Description must clear AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }
    }
}
