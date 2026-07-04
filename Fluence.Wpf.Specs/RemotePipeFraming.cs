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
using System.Threading;
using System.Threading.Tasks;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// Length-prefixed frame read/write helpers for the remote-host pipes. Every frame is a 5-byte
    /// header (1 command byte, 4-byte little-endian payload length) followed by the payload bytes,
    /// adapted from the length-prefix pattern in PSADT's pipe transport minus its encryption layer
    /// (anonymous pipes cannot be opened by an unrelated process).
    /// </summary>
    internal static class RemotePipeFraming
    {
        private const int HeaderLength = 5;

        /// <summary>
        /// The largest payload a frame may carry (64 MB). A frame declaring more is treated as
        /// corrupt rather than allocating unbounded memory.
        /// </summary>
        internal const int MaxPayloadLength = 64 * 1024 * 1024;

        /// <summary>
        /// Writes one complete frame (header plus payload) and flushes the stream.
        /// </summary>
        /// <param name="stream">The pipe to write to.</param>
        /// <param name="command">The command byte.</param>
        /// <param name="payload">The payload bytes; may be empty.</param>
        /// <param name="cancellationToken">Cancels the write.</param>
        /// <returns>A task that completes when the frame is flushed.</returns>
        /// <exception cref="ArgumentException">Thrown when the payload is larger than <see cref="MaxPayloadLength"/>.</exception>
        internal static Task WriteFrameAsync(PipeStream stream, RemotePipeCommand command, byte[] payload, CancellationToken cancellationToken)
        {
            return payload.Length > MaxPayloadLength
                ? throw new ArgumentException(
                    "The frame payload is " + payload.Length.ToString(CultureInfo.InvariantCulture)
                    + " bytes, above the " + MaxPayloadLength.ToString(CultureInfo.InvariantCulture) + " byte frame limit.",
                    nameof(payload))
                : WriteFrameCoreAsync(stream, command, payload, cancellationToken);
        }

        private static async Task WriteFrameCoreAsync(PipeStream stream, RemotePipeCommand command, byte[] payload, CancellationToken cancellationToken)
        {
            byte[] header = new byte[HeaderLength];
            header[0] = (byte)command;
            int length = payload.Length;
            header[1] = (byte)(length & 0xFF);
            header[2] = (byte)((length >> 8) & 0xFF);
            header[3] = (byte)((length >> 16) & 0xFF);
            header[4] = (byte)((length >> 24) & 0xFF);
            await stream.WriteAsync(header, 0, header.Length, cancellationToken).ConfigureAwait(false);
            if (payload.Length > 0)
            {
                await stream.WriteAsync(payload, 0, payload.Length, cancellationToken).ConfigureAwait(false);
            }
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Reads one complete frame, looping until the exact header and payload byte counts arrive.
        /// </summary>
        /// <param name="stream">The pipe to read from.</param>
        /// <param name="cancellationToken">Cancels the read.</param>
        /// <returns>The command byte and payload of the frame.</returns>
        /// <exception cref="EndOfStreamException">Thrown when the pipe closes before a full frame is read.</exception>
        /// <exception cref="InvalidDataException">Thrown when the header declares an impossible payload length.</exception>
        internal static async Task<RemotePipeFrame> ReadFrameAsync(PipeStream stream, CancellationToken cancellationToken)
        {
            byte[] header = new byte[HeaderLength];
            await ReadExactAsync(stream, header, cancellationToken).ConfigureAwait(false);
            RemotePipeCommand command = (RemotePipeCommand)header[0];
            int length = header[1] | (header[2] << 8) | (header[3] << 16) | (header[4] << 24);
            if (length is < 0 or > MaxPayloadLength)
            {
                throw new InvalidDataException(
                    "The frame header declares a payload of " + length.ToString(CultureInfo.InvariantCulture)
                    + " bytes, outside the valid range (0 to " + MaxPayloadLength.ToString(CultureInfo.InvariantCulture) + ").");
            }
            byte[] payload = new byte[length];
            if (length > 0)
            {
                await ReadExactAsync(stream, payload, cancellationToken).ConfigureAwait(false);
            }
            return new RemotePipeFrame(command, payload);
        }

        // PipeStream.ReadAsync can legally return fewer bytes than requested (a short read), so a
        // single call must never be assumed to fill the buffer; loop until it is exactly full.
        private static async Task ReadExactAsync(PipeStream stream, byte[] buffer, CancellationToken cancellationToken)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, total, buffer.Length - total, cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    throw new EndOfStreamException("The Fluence remote host pipe closed before a full frame was read.");
                }
                total += read;
            }
        }
    }
}
