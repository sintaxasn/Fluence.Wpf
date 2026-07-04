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
    /// One decoded remote-host pipe frame: the command byte and its payload.
    /// </summary>
    /// <param name="command">The command byte.</param>
    /// <param name="payload">The payload bytes; empty for command-only frames.</param>
    internal sealed class RemotePipeFrame(RemotePipeCommand command, byte[] payload)
    {
        /// <summary>
        /// Gets the command byte of the frame.
        /// </summary>
        internal RemotePipeCommand Command { get; } = command;

        /// <summary>
        /// Gets the payload bytes of the frame; empty for command-only frames.
        /// </summary>
        internal byte[] Payload { get; } = payload;
    }
}
