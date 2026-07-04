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

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// The command byte carried in every remote-host pipe frame. The set is deliberately tiny: one
    /// dialog verb plus the lifecycle verbs the controller needs, not a general RPC surface.
    /// </summary>
    internal enum RemotePipeCommand
    {
        /// <summary>
        /// Version parity check. The payload is the UTF-8 spec schema version; both sides fail fast
        /// on mismatch instead of surfacing a confusing deserialization error later.
        /// </summary>
        Handshake = 0,

        /// <summary>
        /// Show one dialog. The request payload is a serialized <see cref="RemoteDialogRequest"/>
        /// envelope; the response payload is a serialized <see cref="SpecDialogResult"/> envelope.
        /// </summary>
        ShowDialog = 1,

        /// <summary>
        /// Health check. Empty payload both ways.
        /// </summary>
        Ping = 2,

        /// <summary>
        /// Request graceful host exit. No response frame; the controller waits on process exit.
        /// </summary>
        Shutdown = 3,

        /// <summary>
        /// Failure response. The payload is a UTF-8 error message describing why the host could not
        /// satisfy the request, so a host-side fault surfaces to the caller instead of hanging it.
        /// </summary>
        Error = 4,
    }
}
