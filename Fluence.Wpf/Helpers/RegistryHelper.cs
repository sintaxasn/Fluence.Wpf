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
using System.Windows;
using System.Windows.Media;
using Microsoft.Win32;
using Fluence.Wpf.Native;

namespace Fluence.Wpf.Helpers
{
    internal static class RegistryHelper
    {
        public static bool GetAppsUseLightTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(NativeConstants.PersonalizeRegistryPath))
                {
                    var value = key?.GetValue(NativeConstants.AppsUseLightTheme);
                    if (value is int intValue)
                    {
                        return intValue != 0;
                    }
                }
            }
            catch
            {
            }

            return true;
        }

        public static bool GetSystemUsesLightTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(NativeConstants.PersonalizeRegistryPath))
                {
                    var value = key?.GetValue(NativeConstants.SystemUsesLightTheme);
                    if (value is int intValue)
                    {
                        return intValue != 0;
                    }
                }
            }
            catch
            {
            }

            return true;
        }

        public static bool GetColorPrevalence()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(NativeConstants.DwmRegistryPath))
                {
                    var value = key?.GetValue(NativeConstants.ColorPrevalence);
                    if (value is int intValue)
                    {
                        return intValue != 0;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public static bool TryGetAccentPalette(out Color[] palette)
        {
            palette = null;

            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(NativeConstants.AccentRegistryPath))
                {
                    var value = key?.GetValue(NativeConstants.AccentPalette);
                    if (value is byte[] bytes && bytes.Length >= 32)
                    {
                        palette = new Color[8];
                        for (int i = 0; i < 8; i++)
                        {
                            int offset = i * 4;
                            byte r = bytes[offset];
                            byte g = bytes[offset + 1];
                            byte b = bytes[offset + 2];
                            byte a = bytes[offset + 3];
                            palette[i] = Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                        }
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        public static Color GetAccentColor()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(NativeConstants.AccentRegistryPath))
                {
                    var value = key?.GetValue(NativeConstants.AccentColor);
                    if (value is int intValue)
                    {
                        uint color = unchecked((uint)intValue);
                        byte a = (byte)((color >> 24) & 0xFF);
                        byte b = (byte)((color >> 16) & 0xFF);
                        byte g = (byte)((color >> 8) & 0xFF);
                        byte r = (byte)(color & 0xFF);
                        return Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                    }
                }
            }
            catch
            {
            }

            return Color.FromRgb(0x00, 0x78, 0xD4);
        }

        public static bool IsHighContrastEnabled()
        {
            return SystemParameters.HighContrast;
        }

        /// <summary>
        /// Reads DWM AccentColor (ABGR DWORD) used for the active titlebar when ColorPrevalence is on.
        /// </summary>
        public static bool TryGetDwmAccentColor(out Color color)
        {
            color = default;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(NativeConstants.DwmRegistryPath))
                {
                    var value = key?.GetValue(NativeConstants.AccentColor);
                    if (value is int intValue)
                    {
                        uint raw = unchecked((uint)intValue);
                        byte a = (byte)((raw >> 24) & 0xFF);
                        byte b = (byte)((raw >> 16) & 0xFF);
                        byte g = (byte)((raw >> 8) & 0xFF);
                        byte r = (byte)(raw & 0xFF);
                        color = Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Reads DWM AccentColorInactive (ABGR DWORD) for the inactive titlebar.
        /// </summary>
        public static bool TryGetDwmAccentColorInactive(out Color color)
        {
            color = default;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(NativeConstants.DwmRegistryPath))
                {
                    var value = key?.GetValue(NativeConstants.AccentColorInactive);
                    if (value is int intValue)
                    {
                        uint raw = unchecked((uint)intValue);
                        byte a = (byte)((raw >> 24) & 0xFF);
                        byte b = (byte)((raw >> 16) & 0xFF);
                        byte g = (byte)((raw >> 8) & 0xFF);
                        byte r = (byte)(raw & 0xFF);
                        color = Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Reads DWM ColorizationColor (ARGB) and ColorizationColorBalance for Win10 border blending.
        /// </summary>
        public static bool TryGetColorizationBalance(out Color colorizationColor, out int balance)
        {
            colorizationColor = default;
            balance = 0;
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(NativeConstants.DwmRegistryPath))
                {
                    if (key == null)
                    {
                        return false;
                    }

                    var colorVal = key.GetValue(NativeConstants.ColorizationColor);
                    var balanceVal = key.GetValue(NativeConstants.ColorizationColorBalance);

                    if (colorVal is int colorInt && balanceVal is int balanceInt)
                    {
                        uint raw = unchecked((uint)colorInt);
                        byte a = (byte)((raw >> 24) & 0xFF);
                        byte r = (byte)((raw >> 16) & 0xFF);
                        byte g = (byte)((raw >> 8) & 0xFF);
                        byte b = (byte)(raw & 0xFF);
                        colorizationColor = Color.FromArgb(a == 0 ? (byte)255 : a, r, g, b);
                        balance = balanceInt;
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }
    }
}
