/*
 * Copyright 2026 Dan Cunningham
 *
 * Portions adapted from PSAppDeployToolkit's PSADT.ClientServer transport
 * (anonymous-pipe process launch and framing pattern), Copyright Mitch Richters (Devicie).
 * Reused under a direct grant from Mitch Richters permitting BSD-3 licensing with attribution.
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
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace Fluence.Wpf.Specs.Host
{
    /// <summary>
    /// The standalone same-user Fluence UI host. The controlling process launches this executable
    /// with two anonymous pipe handles on the command line, then sends length-prefixed frames over
    /// the command pipe; a background task reads them and marshals dialog work onto the WPF
    /// dispatcher, while the STA main thread pumps messages in <see cref="Application.Run()"/>.
    /// When the controller disappears (clean exit or crash), the OS closes the pipe, the read loop
    /// observes end-of-stream, and the host shuts itself down instead of lingering.
    /// </summary>
    internal static class Program
    {
        [STAThread]
        private static int Main(string[] args)
        {
            string? commandHandle = GetArgumentValue(args, "--command-pipe");
            string? responseHandle = GetArgumentValue(args, "--response-pipe");
            if (commandHandle is null || responseHandle is null)
            {
                Console.Error.WriteLine("Usage: Fluence.Wpf.Specs.Host --command-pipe <handle> --response-pipe <handle>");
                Console.Error.WriteLine("This executable is launched by FluenceRemoteHostController; it is not meant to run standalone.");
                return 2;
            }

            // Directions are from this process's point of view: the controller writes commands
            // (its Out pipe is our In) and reads responses (its In pipe is our Out).
            using AnonymousPipeClientStream commandPipe = new(PipeDirection.In, commandHandle);
            using AnonymousPipeClientStream responsePipe = new(PipeDirection.Out, responseHandle);

            Application app = new()
            {
                ShutdownMode = ShutdownMode.OnExplicitShutdown,
            };
            Dispatcher dispatcher = app.Dispatcher;
            Task<int> pipeLoop = Task.Run(() => RunPipeLoopAsync(commandPipe, responsePipe, dispatcher));
            _ = app.Run();
            return pipeLoop.GetAwaiter().GetResult();
        }

        private static async Task<int> RunPipeLoopAsync(AnonymousPipeClientStream commandPipe, AnonymousPipeClientStream responsePipe, Dispatcher dispatcher)
        {
            try
            {
                while (true)
                {
                    RemotePipeFrame frame = await RemotePipeFraming.ReadFrameAsync(commandPipe, CancellationToken.None).ConfigureAwait(false);
                    switch (frame.Command)
                    {
                        case RemotePipeCommand.Handshake:
                            byte[] version = Encoding.UTF8.GetBytes(SpecSerialization.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture));
                            await RemotePipeFraming.WriteFrameAsync(responsePipe, RemotePipeCommand.Handshake, version, CancellationToken.None).ConfigureAwait(false);
                            break;
                        case RemotePipeCommand.Ping:
                            await RemotePipeFraming.WriteFrameAsync(responsePipe, RemotePipeCommand.Ping, [], CancellationToken.None).ConfigureAwait(false);
                            break;
                        case RemotePipeCommand.ShowDialog:
                            await HandleShowDialogAsync(responsePipe, dispatcher, frame.Payload).ConfigureAwait(false);
                            break;
                        case RemotePipeCommand.Shutdown:
                            return 0;
                        case RemotePipeCommand.Error:
                            // The controller never legitimately sends Error frames; discard the
                            // frame rather than answering an answer.
                            break;
                        default:
                            byte[] message = Encoding.UTF8.GetBytes("The Fluence remote host does not understand command " + ((byte)frame.Command).ToString(CultureInfo.InvariantCulture) + ".");
                            await RemotePipeFraming.WriteFrameAsync(responsePipe, RemotePipeCommand.Error, message, CancellationToken.None).ConfigureAwait(false);
                            break;
                    }
                }
            }
            catch (EndOfStreamException)
            {
                // The controller went away (clean exit or crash); exiting quietly is the contract.
                return 0;
            }
            catch (Exception exception) when (exception.Message is not null)
            {
                await Console.Error.WriteLineAsync(exception.ToString()).ConfigureAwait(false);
                return 1;
            }
            finally
            {
                dispatcher.BeginInvokeShutdown(DispatcherPriority.Normal);
            }
        }

        private static async Task HandleShowDialogAsync(AnonymousPipeClientStream responsePipe, Dispatcher dispatcher, byte[] payload)
        {
            RemotePipeCommand command;
            byte[] response;
            try
            {
                RemoteDialogRequest request = SpecSerialization.DeserializeRemoteRequest(payload);
                SpecDialogResult result = await dispatcher.InvokeAsync(() => ShowDialogOnUiThread(request)).Task.ConfigureAwait(false);
                command = RemotePipeCommand.ShowDialog;
                response = SpecSerialization.SerializeResult(result);
            }
            catch (Exception exception) when (exception.Message is not null)
            {
                // A request-level fault (bad payload, materialization failure) must answer the
                // caller instead of hanging it; the process itself stays healthy.
                command = RemotePipeCommand.Error;
                response = Encoding.UTF8.GetBytes(exception.Message);
            }
            await RemotePipeFraming.WriteFrameAsync(responsePipe, command, response, CancellationToken.None).ConfigureAwait(false);
        }

        // Mirrors the module's Initialize-FluenceApplication: apply theme and backdrop (seeding the
        // three theme slots), then pin a custom accent or reset to the system accent, per request.
        private static SpecDialogResult ShowDialogOnUiThread(RemoteDialogRequest request)
        {
            ApplicationTheme theme = request.Theme is string themeName && Enum.TryParse(themeName, ignoreCase: true, out ApplicationTheme parsedTheme)
                ? parsedTheme
                : ApplicationTheme.Auto;
            BackdropType backdrop = request.Backdrop is string backdropName && Enum.TryParse(backdropName, ignoreCase: true, out BackdropType parsedBackdrop)
                ? parsedBackdrop
                : BackdropType.Mica;
            ApplicationThemeManager.Apply(theme, backdrop);
            if (request.AccentColorText is string accentText && !string.IsNullOrWhiteSpace(accentText))
            {
                object accent = ColorConverter.ConvertFromString(accentText) ?? throw new InvalidOperationException("The accent color '" + accentText + "' could not be parsed.");
                ApplicationAccentColorManager.ApplyCustomAccent((Color)accent);
            }
            else
            {
                ApplicationAccentColorManager.ApplySystemAccent();
            }

            if (request.SpecBase64 is not string specBase64 || string.IsNullOrWhiteSpace(specBase64))
            {
                throw new InvalidOperationException("The remote dialog payload carries no serialized spec.");
            }
            DialogSpec spec = SpecSerialization.DeserializeFromBase64(specBase64);
            SpecDialogWindow window = SpecMaterializer.Materialize(spec);
            window.Topmost = request.Topmost;
            if (request.TimeoutSeconds is int seconds && seconds > 0)
            {
                DispatcherTimer timer = new()
                {
                    Interval = TimeSpan.FromSeconds(seconds),
                };
                timer.Tick += (sender, eventArgs) =>
                {
                    timer.Stop();

                    // A dialog already closed by a button click must not be closed twice.
                    if (window.IsVisible)
                    {
                        window.Close();
                    }
                };
                timer.Start();
            }
            return window.ShowAndCollect();
        }

        private static string? GetArgumentValue(string[] args, string name)
        {
            for (int index = 0; index < args.Length - 1; index++)
            {
                if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                {
                    return args[index + 1];
                }
            }
            return null;
        }
    }
}
