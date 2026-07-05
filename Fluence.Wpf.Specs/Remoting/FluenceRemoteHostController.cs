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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Fluence.Wpf.Specs.Remoting
{
    /// <summary>
    /// Launches and talks to the standalone Fluence UI host process (Fluence.Wpf.RemoteHost.exe)
    /// over two anonymous pipes, so a caller can show composed spec dialogs out of process. The
    /// host is started lazily, reused across calls, and torn down by <see cref="Shutdown"/> or
    /// <see cref="Dispose"/>. The protocol carries one request at a time; calls on one controller
    /// serialize behind an internal lock rather than multiplexing.
    /// </summary>
    public sealed class FluenceRemoteHostController : IDisposable
    {
        private static readonly TimeSpan HandshakeTimeout = TimeSpan.FromSeconds(15);

        private readonly object _gate = new();
        private Process? _process;
        private AnonymousPipeServerStream? _commandPipe;
        private AnonymousPipeServerStream? _responsePipe;
        private Task<RemotePipeFrame>? _orphanedResponseRead;
        private bool _disposed;
        private bool _inFlight;

        /// <summary>
        /// Gets a value indicating whether a host process is currently running.
        /// </summary>
        public bool IsRunning
        {
            get
            {
                lock (_gate)
                {
                    return _process?.HasExited == false;
                }
            }
        }

        /// <summary>
        /// Starts the host process when none is running (or the previous one exited), passing the
        /// two anonymous pipe handles on the command line, then performs the schema-version
        /// handshake. A healthy running host makes this a no-op.
        /// </summary>
        /// <param name="hostExecutablePath">The full path to Fluence.Wpf.RemoteHost.exe.</param>
        /// <exception cref="ArgumentException">Thrown when the path is null or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown when the host executable does not exist.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the host fails to start or the schema versions mismatch.</exception>
        public void EnsureRunning(string hostExecutablePath)
        {
            if (string.IsNullOrWhiteSpace(hostExecutablePath))
            {
                throw new ArgumentException("The host executable path is null or empty.", nameof(hostExecutablePath));
            }
            if (!File.Exists(hostExecutablePath))
            {
                throw new FileNotFoundException("The Fluence remote host executable was not found.", hostExecutablePath);
            }
            lock (_gate)
            {
                ThrowIfDisposed();
                if (_process?.HasExited == false)
                {
                    return;
                }
                CleanupCore();
                LaunchCore(hostExecutablePath);
                try
                {
                    HandshakeCore();
                }
                catch
                {
                    KillCore();
                    CleanupCore();
                    throw;
                }
            }
        }

        /// <summary>
        /// Sends one show-dialog request and blocks until the host returns the result (the dialog
        /// closed) or the timeout elapses. On timeout the host is killed, because the pipe is left
        /// mid-frame and no further request could be trusted.
        /// </summary>
        /// <param name="request">The dialog request to send.</param>
        /// <param name="timeout">The longest time to wait for the dialog to close; use
        /// <see cref="Timeout.InfiniteTimeSpan"/> to wait indefinitely.</param>
        /// <returns>The dialog result returned by the host.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when no host is running, the host reports a failure, or the pipe closes unexpectedly.</exception>
        /// <exception cref="TimeoutException">Thrown when no response arrives within <paramref name="timeout"/>.</exception>
        public SpecDialogResult ShowDialog(RemoteDialogRequest request, TimeSpan timeout)
        {
            if (request is null)
            {
                throw new ArgumentNullException(nameof(request));
            }

            // Acquire the gate only long enough to validate state, take the single in-flight slot, and
            // write the request frame; then release it so a dialog with a long or infinite timeout does
            // not block teardown (Shutdown/Dispose) or a health check on another thread. The blocking
            // response read runs outside the lock against the pipe reference captured here, so a
            // concurrent Shutdown can kill the host and fault this read instead of waiting for it. A
            // single try/finally spans both the under-lock write and the unlocked read so that a fault
            // in either one (serialize or write throwing, or the read failing) clears the in-flight slot
            // via the finally; without this, a throwing write would leave _inFlight stuck true forever.
            bool acquired = false;
            try
            {
                AnonymousPipeServerStream responsePipe;
                lock (_gate)
                {
                    ThrowIfDisposed();
                    EnsureConnectedCore();
                    if (_inFlight)
                    {
                        throw new InvalidOperationException("A Fluence remote host dialog is already in progress.");
                    }
                    responsePipe = _responsePipe ?? throw new InvalidOperationException("The Fluence remote host is not running. Call EnsureRunning first.");
                    _inFlight = true;
                    acquired = true;
                    byte[] payload = SpecSerialization.SerializeRemoteRequest(request);
                    WriteFrameCore(RemotePipeCommand.ShowDialog, payload);
                }
                RemotePipeFrame frame = ReadFrameCore(responsePipe, timeout, killOnTimeout: true);
                return frame.Command == RemotePipeCommand.Error
                    ? throw new InvalidOperationException("The Fluence remote host failed to show the dialog: " + Encoding.UTF8.GetString(frame.Payload))
                    : frame.Command != RemotePipeCommand.ShowDialog
                    ? throw new InvalidOperationException("The Fluence remote host answered a ShowDialog call with an unexpected '" + frame.Command.ToString() + "' frame.")
                    : SpecSerialization.DeserializeResult(frame.Payload);
            }
            finally
            {
                if (acquired)
                {
                    lock (_gate)
                    {
                        _inFlight = false;
                    }
                }
            }
        }

        /// <summary>
        /// Sends a health-check frame and reports whether the host answered within the timeout.
        /// Never throws; any transport failure reports false.
        /// </summary>
        /// <param name="timeout">The longest time to wait for the reply.</param>
        /// <returns>True when a running host answered the ping; otherwise false.</returns>
        public bool Ping(TimeSpan timeout)
        {
            // Unlike ShowDialog, Ping holds _gate across its whole exchange (including the blocking
            // read). This is deliberate: a health check is short and low-timeout, so the simpler
            // fully-locked path is fine, and it keeps Ping from ever racing another writer. The
            // tradeoff is that a long-timeout Ping to an alive-but-slow host can delay a concurrent
            // Shutdown by up to that timeout; callers should keep the ping timeout short.
            lock (_gate)
            {
                // When a dialog call is in flight its response read runs outside the gate and owns the
                // pipe. Writing a Ping frame now would interleave a second frame on that same pipe, so
                // report false without touching the pipe rather than corrupting the pending exchange.
                if (_disposed || _inFlight || _process?.HasExited != false || _commandPipe is null || _responsePipe is null)
                {
                    return false;
                }
                AnonymousPipeServerStream responsePipe = _responsePipe;
                try
                {
                    WriteFrameCore(RemotePipeCommand.Ping, []);
                    // A missed ping deadline does not mean the host is unhealthy, only that it did not
                    // answer within this caller's chosen window, so a ping timeout must not tear the
                    // host down the way a stuck dialog or handshake read does.
                    RemotePipeFrame frame = ReadFrameCore(responsePipe, timeout, killOnTimeout: false);
                    return frame.Command == RemotePipeCommand.Ping;
                }
                catch (InvalidOperationException)
                {
                    return false;
                }
                catch (TimeoutException)
                {
                    return false;
                }
                catch (IOException)
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Requests graceful host exit and waits up to the grace period, then kills the process if
        /// it is still alive. Safe to call when no host is running.
        /// </summary>
        /// <param name="gracePeriod">How long to wait for a graceful exit before killing.</param>
        public void Shutdown(TimeSpan gracePeriod)
        {
            lock (_gate)
            {
                Process? process = _process;
                if (process is null)
                {
                    CleanupCore();
                    return;
                }
                if (!process.HasExited)
                {
                    if (_inFlight)
                    {
                        // A dialog request is in flight: its response read is blocked outside the gate,
                        // so a graceful Shutdown frame would interleave with the pending response, and
                        // waiting for the grace period would defeat the point of a non-blocking teardown.
                        // Kill out of band; closing the pipe faults the in-flight read (surfacing a
                        // host-failure to that caller, the correct "torn down mid-call" outcome).
                        KillCore();
                    }
                    else
                    {
                        // Track whether the graceful frame actually went out. On a failed write
                        // WriteFrameCore has already killed and cleaned up the host (disposing and
                        // nulling _process), so the captured process handle is dead: touching it via
                        // WaitForExit would throw InvalidOperationException straight out of Shutdown
                        // (and Dispose), both of which are documented never to throw. Only run the
                        // wait-then-kill fallback when the write succeeded and the host is still
                        // connected; otherwise the teardown is already done and the final CleanupCore
                        // below is an idempotent no-op.
                        bool gracefulWriteSucceeded = false;
                        try
                        {
                            WriteFrameCore(RemotePipeCommand.Shutdown, []);
                            gracefulWriteSucceeded = true;
                        }
                        catch (InvalidOperationException)
                        {
                            // The pipe is already gone; the failed write already tore the host down.
                        }
                        catch (IOException)
                        {
                            // Same: an unreachable pipe means the failed write already tore it down.
                        }
                        if (gracefulWriteSucceeded && !process.WaitForExit(ToWaitMilliseconds(gracePeriod)))
                        {
                            KillCore();
                        }
                    }
                }
                CleanupCore();
            }
        }

        /// <summary>
        /// Shuts the host down (five-second grace period) and releases the pipes.
        /// </summary>
        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            Shutdown(TimeSpan.FromSeconds(5));
            lock (_gate)
            {
                _disposed = true;
            }
        }

        private static int ToWaitMilliseconds(TimeSpan period)
        {
            if (period == Timeout.InfiniteTimeSpan)
            {
                return Timeout.Infinite;
            }
            double milliseconds = period.TotalMilliseconds;
            return milliseconds <= 0 ? 0 : (int)Math.Min(milliseconds, int.MaxValue);
        }

        private void LaunchCore(string hostExecutablePath)
        {
            // Deliberate v1 simplification (same-user, same-machine): the two pipe handles are marked
            // inheritable and the child is launched with a plain Process.Start (no STARTUPINFOEX
            // PROC_THREAD_ATTRIBUTE_HANDLE_LIST), so on Windows the child inherits every currently
            // inheritable handle in this process, not only these two. That is acceptable here (the
            // host is a trusted, same-user child); launching across a trust boundary would require an
            // explicit inherited-handle list. Likewise stderr is redirected but only drained in
            // CreateHostFailure after the host exits; if per-frame host logging is ever added it must
            // be drained asynchronously (BeginErrorReadLine) or a full stderr buffer could block the
            // host. See KNOWN_ISSUES.md.
            AnonymousPipeServerStream commandPipe = new(PipeDirection.Out, HandleInheritability.Inheritable);
            AnonymousPipeServerStream responsePipe = new(PipeDirection.In, HandleInheritability.Inheritable);
            try
            {
                // Pipe handle strings are plain integers, so a flat argument string needs no quoting.
                ProcessStartInfo startInfo = new()
                {
                    FileName = hostExecutablePath,
                    Arguments = "--command-pipe " + commandPipe.GetClientHandleAsString() + " --response-pipe " + responsePipe.GetClientHandleAsString(),
                    UseShellExecute = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                };
                _process = Process.Start(startInfo) ?? throw new InvalidOperationException("Failed to start the Fluence remote host process at '" + hostExecutablePath + "'.");

                // Release the local copies so a broken host is observable as pipe EOF instead of a hang.
                commandPipe.DisposeLocalCopyOfClientHandle();
                responsePipe.DisposeLocalCopyOfClientHandle();
                _commandPipe = commandPipe;
                _responsePipe = responsePipe;
            }
            catch
            {
                commandPipe.Dispose();
                responsePipe.Dispose();
                throw;
            }
        }

        private void HandshakeCore()
        {
            AnonymousPipeServerStream responsePipe = _responsePipe ?? throw new InvalidOperationException("The Fluence remote host is not running. Call EnsureRunning first.");
            byte[] version = Encoding.UTF8.GetBytes(SpecSerialization.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture));
            WriteFrameCore(RemotePipeCommand.Handshake, version);
            RemotePipeFrame frame = ReadFrameCore(responsePipe, HandshakeTimeout, killOnTimeout: true);
            if (frame.Command != RemotePipeCommand.Handshake)
            {
                throw new InvalidOperationException("The Fluence remote host answered the handshake with an unexpected '" + frame.Command.ToString() + "' frame.");
            }
            string hostVersion = Encoding.UTF8.GetString(frame.Payload);
            string localVersion = SpecSerialization.CurrentSchemaVersion.ToString(CultureInfo.InvariantCulture);
            if (!string.Equals(hostVersion, localVersion, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The Fluence remote host speaks spec schema version " + hostVersion
                    + " but this caller speaks version " + localVersion
                    + ". Update Fluence.Wpf.Specs and the host executable to a matching build.");
            }
        }

        private void EnsureConnectedCore()
        {
            if (_process is null || _commandPipe is null || _responsePipe is null)
            {
                throw new InvalidOperationException("The Fluence remote host is not running. Call EnsureRunning first.");
            }
            if (_process.HasExited)
            {
                throw CreateHostFailure(inner: null);
            }
        }

        private void WriteFrameCore(RemotePipeCommand command, byte[] payload)
        {
            AnonymousPipeServerStream commandPipe = _commandPipe ?? throw new InvalidOperationException("The Fluence remote host is not running. Call EnsureRunning first.");
            try
            {
                RemotePipeFraming.WriteFrameAsync(commandPipe, command, payload, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (IOException exception)
            {
                // A failed write leaves the connection in an unknown state (the frame may be half
                // written), and the process can still be alive on the other end of a half-broken
                // pipe, so IsRunning would otherwise keep reporting healthy. Tear the host down here
                // so the next EnsureRunning sees no process and relaunches instead of reusing a
                // connection that will fail the same way forever. WriteFrameCore is always called
                // with _gate held (ShowDialog, Ping, Shutdown, and HandshakeCore under EnsureRunning),
                // matching KillCore/CleanupCore's caller-holds-the-gate convention.
                //
                // Build the failure first, while _process is still alive: CreateHostFailure reads the
                // exit code and stderr off _process, and CleanupCore is about to dispose and null it.
                // When the write failed because the host died, _process.HasExited is already true here,
                // so those diagnostics are captured; when the host is still alive (a disposed local
                // pipe), CreateHostFailure reads nothing and does not block on stderr.
                InvalidOperationException failure = CreateHostFailure(exception);
                KillCore();
                CleanupCore();
                throw failure;
            }
            catch (ObjectDisposedException exception)
            {
                InvalidOperationException failure = CreateHostFailure(exception);
                KillCore();
                CleanupCore();
                throw failure;
            }
        }

        private RemotePipeFrame ReadFrameCore(AnonymousPipeServerStream responsePipe, TimeSpan timeout, bool killOnTimeout)
        {
            // A prior non-destructive Ping timeout (killOnTimeout: false) may have left its own read
            // still running against this same pipe: the host answers commands strictly one at a time
            // (see Fluence.Wpf.RemoteHost's read-process-write loop), so that reply is not lost, only
            // unread. Starting a second, independent read on the same stream before that one finishes
            // would race an unsynchronized Stream and, even if it happened not to corrupt anything,
            // would hand this call the stale reply instead of its own. Join it first so reads against
            // one pipe are always strictly sequential.
            if (!DrainOrphanedResponseRead(timeout))
            {
                // The orphaned read still has not completed after a second full timeout window: the
                // host is not merely slow, it is not making progress at all. The backlog is already
                // bounded to a single orphan (you can only stash one after draining the previous), so
                // this is a liveness/recovery fallback, not backlog-bounding: we cannot safely start
                // our own read while the prior one is unresolved, so kill the wedged host rather than
                // leave a latent desync for the next call.
                lock (_gate)
                {
                    KillCore();
                    CleanupCore();
                }
                throw new TimeoutException("The Fluence remote host did not respond within " + timeout.ToString() + "; the host process was terminated.");
            }

            Task<RemotePipeFrame> readTask = RemotePipeFraming.ReadFrameAsync(responsePipe, CancellationToken.None);
            if (!WaitForCompletion(readTask, timeout))
            {
                if (!killOnTimeout)
                {
                    // Leave the host and the pipe alone. The read keeps running in the background
                    // against the live pipe; the next call on this pipe joins it above (via
                    // DrainOrphanedResponseRead) before issuing its own read, keeping frames ordered.
                    _orphanedResponseRead = readTask;
                    throw new TimeoutException("The Fluence remote host did not respond within " + timeout.ToString() + ".");
                }
                // Anonymous pipe reads cannot be cancelled portably; killing the host closes the
                // pipe, which faults the pending read instead of leaking a forever-blocked thread.
                // The gate is taken here because this read may be running outside it (ShowDialog),
                // so tearing the host down must not race a concurrent Shutdown's teardown. The lock
                // is reentrant, so callers that already hold it (HandshakeCore) are unaffected.
                ObserveFault(readTask);
                lock (_gate)
                {
                    KillCore();
                    CleanupCore();
                }
                throw new TimeoutException("The Fluence remote host did not respond within " + timeout.ToString() + "; the host process was terminated.");
            }
            try
            {
                return readTask.GetAwaiter().GetResult();
            }
            catch (EndOfStreamException exception)
            {
                throw CreateHostFailure(exception);
            }
            catch (IOException exception)
            {
                throw CreateHostFailure(exception);
            }
            catch (ObjectDisposedException exception)
            {
                throw CreateHostFailure(exception);
            }
            catch (InvalidDataException exception)
            {
                // A corrupt frame header means the pipe is desynchronized; the host cannot be
                // trusted to resume framing correctly, so kill it instead of reusing the pipe. Take
                // the gate for the same reason as the timeout branch (this may run outside it).
                lock (_gate)
                {
                    KillCore();
                    CleanupCore();
                }
                throw CreateHostFailure(exception);
            }
        }

        /// <summary>
        /// Waits for a response read orphaned by an earlier non-destructive Ping timeout to finish, so
        /// that no two reads ever run concurrently against the same response pipe and every read
        /// consumes the frame that actually answers it, not a leftover one.
        /// </summary>
        /// <param name="timeout">The longest time to wait for the orphaned read to finish.</param>
        /// <returns>True once there is nothing left to drain (either there was no orphan, or it
        /// finished in time); false when an orphan is still outstanding after waiting the full
        /// <paramref name="timeout"/>, which the caller treats as proof the host has stopped making
        /// progress.</returns>
        private bool DrainOrphanedResponseRead(TimeSpan timeout)
        {
            Task<RemotePipeFrame>? orphan;
            lock (_gate)
            {
                orphan = _orphanedResponseRead;
                _orphanedResponseRead = null;
            }
            if (orphan is null)
            {
                return true;
            }
            if (!WaitForCompletion(orphan, timeout))
            {
                // Still not done; put it back so a subsequent call keeps trying to join it, unless the
                // caller instead kills the host, in which case CleanupCore discards it below.
                lock (_gate)
                {
                    _orphanedResponseRead = orphan;
                }
                return false;
            }
            // Discard the frame or exception; it belongs to the timed-out call that abandoned it, not
            // to this one.
            ObserveFault(orphan);
            return true;
        }

        private static bool WaitForCompletion(Task task, TimeSpan timeout)
        {
            try
            {
                return task.Wait(ToWaitMilliseconds(timeout));
            }
            catch (AggregateException)
            {
                // Faulted counts as completed; the caller rethrows the unwrapped exception.
                return true;
            }
        }

        private static void ObserveFault(Task task)
        {
            _ = task.ContinueWith(
                static completed => _ = completed.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default);
        }

        private InvalidOperationException CreateHostFailure(Exception? inner)
        {
            string details = string.Empty;
            try
            {
                // Called from the response-read catch blocks, which for ShowDialog run outside the
                // gate; a concurrent Shutdown may dispose _process while diagnostics are gathered.
                // Treat that race as "no extra detail" rather than letting it throw over the real fault.
                Process? process = _process;
                if (process?.HasExited == true)
                {
                    details = " The host process exited with code " + process.ExitCode.ToString(CultureInfo.InvariantCulture) + ".";
                    string standardError = ReadStandardError(process);
                    if (!string.IsNullOrWhiteSpace(standardError))
                    {
                        details += " Host stderr: " + standardError.Trim();
                    }
                }
            }
            catch (InvalidOperationException)
            {
                details = string.Empty;
            }
            return new InvalidOperationException("The Fluence remote host pipe closed unexpectedly." + details, inner);
        }

        private static string ReadStandardError(Process process)
        {
            try
            {
                return process.StandardError.ReadToEnd();
            }
            catch (InvalidOperationException)
            {
                return string.Empty;
            }
            catch (IOException)
            {
                return string.Empty;
            }
        }

        private void KillCore()
        {
            Process? process = _process;
            if (process is null)
            {
                return;
            }
            try
            {
                if (!process.HasExited)
                {
                    process.Kill();
                    _ = process.WaitForExit(5000);
                }
            }
            catch (InvalidOperationException)
            {
                // The process exited between the check and the kill.
            }
            catch (Win32Exception)
            {
                // The process is already terminating.
            }
        }

        private void CleanupCore()
        {
            _commandPipe?.Dispose();
            _commandPipe = null;
            _responsePipe?.Dispose();
            _responsePipe = null;
            _process?.Dispose();
            _process = null;
            if (_orphanedResponseRead is Task<RemotePipeFrame> orphan)
            {
                // The pipe it was reading from is gone (or about to be); disposing the stream while
                // the read is pending faults it, so observe that fault instead of letting it surface
                // as an unobserved task exception later.
                _orphanedResponseRead = null;
                ObserveFault(orphan);
            }
        }

        private void ThrowIfDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(FluenceRemoteHostController));
            }
        }
    }
}
