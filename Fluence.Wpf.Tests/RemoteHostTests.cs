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
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;
using Fluence.Wpf.Specs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Covers the remote-host pipe framing (in-process, over a real anonymous pipe pair) and the
    /// end-to-end controller lifecycle against the actually built Fluence.Wpf.RemoteHost.exe.
    /// The end-to-end tests require the solution to have been built first (the standard
    /// build-then-test flow); they fail with an actionable message otherwise.
    /// </summary>
    [TestClass]
    public class RemoteHostTests
    {
        private const string HostTargetFramework = "net10.0-windows10.0.26100.0";

        [TestMethod]
        public void Framing_RoundTrips_PayloadLargerThanPipeBuffer()
        {
            using AnonymousPipeServerStream server = new(PipeDirection.Out, HandleInheritability.None);
            using AnonymousPipeClientStream client = new(PipeDirection.In, server.GetClientHandleAsString());

            // Well above the OS pipe buffer, so the reader must loop over short reads.
            byte[] payload = new byte[512 * 1024];
            for (int index = 0; index < payload.Length; index++)
            {
                payload[index] = (byte)(index % 251);
            }

            Task writeTask = Task.Run(() => RemotePipeFraming.WriteFrameAsync(server, RemotePipeCommand.ShowDialog, payload, CancellationToken.None));
            RemotePipeFrame frame = RemotePipeFraming.ReadFrameAsync(client, CancellationToken.None).GetAwaiter().GetResult();
            writeTask.GetAwaiter().GetResult();

            Assert.AreEqual(RemotePipeCommand.ShowDialog, frame.Command);
            CollectionAssert.AreEqual(payload, frame.Payload);
        }

        [TestMethod]
        public void Framing_EmptyPayload_RoundTrips()
        {
            using AnonymousPipeServerStream server = new(PipeDirection.Out, HandleInheritability.None);
            using AnonymousPipeClientStream client = new(PipeDirection.In, server.GetClientHandleAsString());

            Task writeTask = Task.Run(() => RemotePipeFraming.WriteFrameAsync(server, RemotePipeCommand.Ping, [], CancellationToken.None));
            RemotePipeFrame frame = RemotePipeFraming.ReadFrameAsync(client, CancellationToken.None).GetAwaiter().GetResult();
            writeTask.GetAwaiter().GetResult();

            Assert.AreEqual(RemotePipeCommand.Ping, frame.Command);
            Assert.AreEqual(0, frame.Payload.Length);
        }

        [TestMethod]
        public void Framing_ClosedPipe_ThrowsEndOfStream()
        {
            AnonymousPipeServerStream server = new(PipeDirection.Out, HandleInheritability.None);
            using AnonymousPipeClientStream client = new(PipeDirection.In, server.GetClientHandleAsString());
            server.Dispose();

            _ = Assert.ThrowsExactly<EndOfStreamException>(
                () => RemotePipeFraming.ReadFrameAsync(client, CancellationToken.None).GetAwaiter().GetResult());
        }

        [TestMethod]
        public void Framing_DeclaredLengthOutOfRange_ThrowsInvalidData()
        {
            using AnonymousPipeServerStream server = new(PipeDirection.Out, HandleInheritability.None);
            using AnonymousPipeClientStream client = new(PipeDirection.In, server.GetClientHandleAsString());
            byte[] header = [1, 0xFF, 0xFF, 0xFF, 0xFF]; // command=1, length = 0xFFFFFFFF (huge / negative)
            server.Write(header, 0, header.Length);
            server.Flush();

            _ = Assert.ThrowsExactly<InvalidDataException>(
                () => RemotePipeFraming.ReadFrameAsync(client, CancellationToken.None).GetAwaiter().GetResult());
        }

        [TestMethod]
        public void Controller_LaunchPingShowShutdown_EndToEnd()
        {
            using FluenceRemoteHostController controller = new();
            controller.EnsureRunning(GetHostExecutablePath());
            Assert.IsTrue(controller.IsRunning, "the host process must be running after EnsureRunning");
            Assert.IsTrue(controller.Ping(TimeSpan.FromSeconds(15)), "a healthy host must answer a ping");

            SpecDialogResult result = controller.ShowDialog(BuildTimeoutRequest(), TimeSpan.FromSeconds(60));

            Assert.AreEqual("Cancelled", result.Button, "a timeout-dismissed dialog reports the Cancelled identity");
            Assert.IsTrue(result.Values.ContainsKey("Desk"), "named inputs harvest even on a timeout dismissal");
            Assert.IsTrue(controller.IsRunning, "the host must survive a dialog cycle");

            controller.Shutdown(TimeSpan.FromSeconds(10));
            Assert.IsFalse(controller.IsRunning, "Shutdown must end the host process");
            controller.Shutdown(TimeSpan.FromSeconds(1));
        }

        [TestMethod]
        public void Controller_RepeatedCycles_NeverHang()
        {
            using FluenceRemoteHostController controller = new();
            controller.EnsureRunning(GetHostExecutablePath());
            const int cycles = 10;
            Stopwatch stopwatch = Stopwatch.StartNew();
            for (int cycle = 0; cycle < cycles; cycle++)
            {
                SpecDialogResult result = controller.ShowDialog(BuildTimeoutRequest(), TimeSpan.FromSeconds(30));
                Assert.AreEqual("Cancelled", result.Button);
            }
            stopwatch.Stop();

            // Each cycle self-dismisses after 1 second; a generous per-cycle bound still catches a
            // hang (the automated form of the PRD's "repeated cycles never hang" reliability metric).
            Assert.IsTrue(
                stopwatch.Elapsed < TimeSpan.FromSeconds(cycles * 15),
                $"{cycles} timeout-dismissed cycles took {stopwatch.Elapsed}; a hang is the only way to get here");
            Assert.IsTrue(controller.IsRunning, "one host process must serve every cycle");
        }

        [TestMethod]
        public void Controller_HostFault_SurfacesErrorAndHostStaysHealthy()
        {
            using FluenceRemoteHostController controller = new();
            controller.EnsureRunning(GetHostExecutablePath());
            RemoteDialogRequest malformed = new()
            {
                SpecBase64 = Convert.ToBase64String([1, 2, 3]),
            };

            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
                () => controller.ShowDialog(malformed, TimeSpan.FromSeconds(30)));

            StringAssert.Contains(exception.Message, "failed to show the dialog", StringComparison.Ordinal);
            Assert.IsTrue(controller.Ping(TimeSpan.FromSeconds(15)), "a request-level fault must not kill the host");
        }

        [TestMethod]
        public void Controller_ShowDialog_RequiresEnsureRunning()
        {
            using FluenceRemoteHostController controller = new();

            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
                () => controller.ShowDialog(BuildTimeoutRequest(), TimeSpan.FromSeconds(1)));

            StringAssert.Contains(exception.Message, "EnsureRunning", StringComparison.Ordinal);
            Assert.IsFalse(controller.Ping(TimeSpan.FromSeconds(1)), "pinging a never-started host reports false");
        }

        [TestMethod]
        public void Shutdown_WhileDialogShowing_ReturnsPromptlyAndKillsHost()
        {
            using FluenceRemoteHostController controller = new();
            controller.EnsureRunning(GetHostExecutablePath());

            // A spec with no TimeoutSeconds stays open until the host is torn down.
            RemoteDialogRequest request = BuildOpenRequest();
            Task showTask = Task.Run(() =>
            {
                try
                {
                    _ = controller.ShowDialog(request, Timeout.InfiniteTimeSpan);
                }
                catch (InvalidOperationException)
                {
                    // Expected: the host is killed mid-call, faulting the pending response read.
                }
            });

            // Give the host time to bring the window up and start blocking on the response read.
            Thread.Sleep(1500);

            Stopwatch stopwatch = Stopwatch.StartNew();
            controller.Shutdown(TimeSpan.FromSeconds(10));
            stopwatch.Stop();

            Assert.IsTrue(
                stopwatch.Elapsed < TimeSpan.FromSeconds(8),
                $"Shutdown blocked behind the shown dialog: {stopwatch.Elapsed}");
            Assert.IsFalse(controller.IsRunning, "Shutdown must end the host process even mid-dialog");
            Assert.IsTrue(showTask.Wait(TimeSpan.FromSeconds(5)), "the in-flight ShowDialog must unblock once the host is killed");
        }

        [TestMethod]
        public void ShowDialog_WhileRequestInFlight_ThrowsSingleInFlight()
        {
            using FluenceRemoteHostController controller = new();
            controller.EnsureRunning(GetHostExecutablePath());

            RemoteDialogRequest openRequest = BuildOpenRequest();
            Task showTask = Task.Run(() =>
            {
                try
                {
                    _ = controller.ShowDialog(openRequest, Timeout.InfiniteTimeSpan);
                }
                catch (InvalidOperationException)
                {
                    // Expected: the host is killed mid-call during teardown below.
                }
            });

            // Let the first request take the single in-flight slot.
            Thread.Sleep(1500);

            try
            {
                InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(
                    () => controller.ShowDialog(BuildOpenRequest(), TimeSpan.FromSeconds(5)));
                StringAssert.Contains(exception.Message, "already in progress", StringComparison.Ordinal);
                Assert.IsFalse(controller.Ping(TimeSpan.FromSeconds(1)), "Ping must not interleave a frame while a request is in flight");
            }
            finally
            {
                controller.Shutdown(TimeSpan.FromSeconds(10));
                _ = showTask.Wait(TimeSpan.FromSeconds(5));
            }
        }

        private static RemoteDialogRequest BuildTimeoutRequest()
        {
            return BuildRequest(timeoutSeconds: 1);
        }

        private static RemoteDialogRequest BuildOpenRequest()
        {
            return BuildRequest(timeoutSeconds: null);
        }

        private static RemoteDialogRequest BuildRequest(int? timeoutSeconds)
        {
            DialogSpec dialog = new()
            {
                Title = "Remote host test",
            };
            dialog.Content.Add(new TextBlockSpec { Text = "Self-dismissing remote dialog" });
            dialog.Content.Add(new TextBoxSpec { Name = "Desk" });
            dialog.Buttons.Add(new ButtonSpec { Text = "OK", IsDefault = true });
            return new RemoteDialogRequest
            {
                SpecBase64 = SpecSerialization.SerializeToBase64(dialog),
                Theme = "Light",
                Backdrop = "None",
                TimeoutSeconds = timeoutSeconds,
            };
        }

        private static string GetHostExecutablePath()
        {
            // BaseDirectory is <repo>\Fluence.Wpf.Tests\bin\<Configuration>\<tfm>\; the host exe
            // lives in its own project's bin for the same configuration, single modern TFM.
            DirectoryInfo tfmDirectory = new(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            DirectoryInfo? configurationDirectory = tfmDirectory.Parent;
            DirectoryInfo? repoRootDirectory = configurationDirectory?.Parent?.Parent?.Parent;
            if (configurationDirectory is null || repoRootDirectory is null)
            {
                throw new InvalidOperationException($"Could not derive the repository root from '{AppContext.BaseDirectory}'.");
            }
            string hostPath = Path.Combine(repoRootDirectory.FullName, "Fluence.Wpf.RemoteHost", "bin", configurationDirectory.Name, HostTargetFramework, "Fluence.Wpf.RemoteHost.exe");
            return File.Exists(hostPath)
                ? hostPath
                : throw new FileNotFoundException("Fluence.Wpf.RemoteHost.exe is not built; build Fluence.Wpf.sln before running the remote host tests.", hostPath);
        }
    }
}
