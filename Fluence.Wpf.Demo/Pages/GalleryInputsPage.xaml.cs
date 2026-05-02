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
using System.Globalization;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryInputsPage : UserControl
    {
        public GalleryInputsPage()
        {
            InitializeComponent();

            DemoSourceAction.Replace(TextBoxInputSourceLink, "Inputs/TextBoxInput.xaml");
            DemoSourceAction.Replace(TextBoxValidationSourceLink, "Inputs/TextBoxValidation.xaml");
            DemoSourceAction.Replace(PasswordBoxInputSourceLink, "Inputs/PasswordBoxInput.xaml");
            DemoSourceAction.Replace(NumberBoxInputSourceLink, "Inputs/NumberBoxInput.xaml");
            DemoSourceAction.Replace(SliderInputSourceLink, "Inputs/SliderInput.xaml");
        }

        private void CharCountTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (CharCountLabel == null || CharCountTextBox == null)
            {
                return;
            }

            var len = CharCountTextBox.Text != null ? CharCountTextBox.Text.Length : 0;
            CharCountLabel.Text = string.Format(CultureInfo.CurrentCulture, "Characters: {0}", len);
        }
    }
}
