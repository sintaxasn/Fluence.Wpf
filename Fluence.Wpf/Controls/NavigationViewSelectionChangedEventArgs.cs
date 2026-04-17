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

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// Provides data for the <see cref="NavigationView.NavSelectionChanged"/> event.
    /// </summary>
    public class NavigationViewSelectionChangedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="NavigationViewSelectionChangedEventArgs"/> class.
        /// </summary>
        /// <param name="selectedItem">The newly selected item, if any.</param>
        /// <param name="isSettingsSelected">Reserved for future use (settings entry).</param>
        public NavigationViewSelectionChangedEventArgs(object selectedItem, bool isSettingsSelected)
        {
            SelectedItem = selectedItem;
            IsSettingsSelected = isSettingsSelected;
        }

        /// <summary>
        /// Gets the selected navigation item (typically a <see cref="NavigationViewItem"/>).
        /// </summary>
        public object SelectedItem { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the settings item is selected; reserved for future use.
        /// </summary>
        public bool IsSettingsSelected { get; private set; }
    }
}
