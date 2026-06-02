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

using System.ComponentModel;
using System.Windows;

namespace Fluence.Wpf
{
    /// <summary>
    /// A <see cref="ResourceDictionary"/> that seeds all Fluence theme resources
    /// (colors, brushes, typography, and control templates) when the XAML parser
    /// calls <see cref="ISupportInitialize.EndInit"/>. Declarative consumers add
    /// this to <c>App.xaml MergedDictionaries</c> to receive theme resources before
    /// any window is shown -- a structural guarantee rather than a code-behind convention.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The resource initialization is equivalent to calling
    /// <see cref="ApplicationThemeManager.Apply(ApplicationTheme, BackdropType, bool)"/> with
    /// <see cref="ApplicationTheme.Auto"/>. Because the engine's pipeline is
    /// idempotent, a later explicit call to <c>Apply</c> (for example to pin a
    /// specific theme or backdrop) coexists without duplication. The first call
    /// seeds all three merge slots; later calls replace only slot <c>[0]</c>.
    /// </para>
    /// <para>
    /// PSADT is unaffected -- it has no <c>App.xaml</c> and already calls
    /// <c>Apply</c> in the dialog constructor; <see cref="FluenceResources"/> is a
    /// convenience for library consumers that do have an application startup file.
    /// </para>
    /// <para>
    /// Usage in <c>App.xaml</c>:
    /// <code><![CDATA[
    /// <Application
    ///     xmlns:fluence="clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf">
    ///   <Application.Resources>
    ///     <ResourceDictionary>
    ///       <ResourceDictionary.MergedDictionaries>
    ///         <fluence:FluenceResources />
    ///       </ResourceDictionary.MergedDictionaries>
    ///     </ResourceDictionary>
    ///   </Application.Resources>
    /// </Application>
    /// ]]></code>
    /// </para>
    /// </remarks>
    public sealed class FluenceResources : ResourceDictionary, ISupportInitialize
    {
        /// <summary>
        /// Called by the XAML parser or by an explicit <see cref="ISupportInitialize"/>
        /// host to signal the start of the initialization batch. Delegates to the base
        /// <see cref="ResourceDictionary.BeginInit"/> implementation.
        /// </summary>
        void ISupportInitialize.BeginInit()
        {
            BeginInit();
        }

        /// <summary>
        /// Called by the XAML parser or by an explicit <see cref="ISupportInitialize"/>
        /// host after all properties have been set. Delegates to the base
        /// <see cref="ResourceDictionary.EndInit"/> implementation and then seeds all
        /// Fluence theme resources by calling
        /// <see cref="ApplicationThemeManager.Apply(ApplicationTheme, BackdropType, bool)"/> with
        /// <see cref="ApplicationTheme.Auto"/>. The call is idempotent; if the engine
        /// has already been initialized, it re-applies the current theme without
        /// resetting state.
        /// </summary>
        void ISupportInitialize.EndInit()
        {
            EndInit();
            ApplicationThemeManager.Apply(ApplicationTheme.Auto);
        }
    }
}
