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
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using Fluence.Wpf.Specs.Remoting;

namespace Fluence.Wpf.Specs
{
    /// <summary>
    /// Serializes dialog specs to an opaque, versioned binary blob and back. The wire format is
    /// DataContract binary XML inside a stable <c>SpecEnvelope</c>, mirroring the transport-proven
    /// pipeline PSADT uses, with a closed known-types set (<see cref="SpecKnownTypes"/>). Object
    /// references are not preserved: specs serialize as a strict tree, enforced up front by
    /// <see cref="SpecTreeValidator"/>.
    /// </summary>
    public static class SpecSerialization
    {
        /// <summary>
        /// The payload schema version this build writes and the highest version it reads. Bump on
        /// any breaking data-contract change; Fluence.Wpf.Specs and Fluence.Wpf ship as a matched
        /// pair, so both sides of a transport must carry the same pair version.
        /// </summary>
        public const int CurrentSchemaVersion = 1;

        // Comfortably above the deepest legitimate dialog's binary-XML nesting, well below
        // stack-overflow territory; replaces the unbounded XmlDictionaryReaderQuotas.Max depth.
        private const int MaxReaderDepth = 128;

        // 64 MB, aligned with the transport frame limit. Must stay large enough for legitimate
        // embedded image payloads (ImageSpec.SourceBase64 / the byte[] envelope Payload can be
        // several MB); the goal is bounding "unbounded", not constraining real specs.
        private const int MaxReaderPayloadBytes = 64 * 1024 * 1024;

        // Generous headroom for interned XML names; removes "unbounded" without rejecting legit specs.
        private const int MaxReaderNameTableChars = 16 * 1024 * 1024;

        private static readonly XmlDictionaryReaderQuotas ReaderQuotas = new()
        {
            MaxDepth = MaxReaderDepth,
            MaxStringContentLength = MaxReaderPayloadBytes,
            MaxArrayLength = MaxReaderPayloadBytes,
            MaxBytesPerRead = MaxReaderPayloadBytes,
            MaxNameTableCharCount = MaxReaderNameTableChars,
        };

        private static readonly DataContractSerializerSettings SpecSettings = new()
        {
            PreserveObjectReferences = false,
            SerializeReadOnlyTypes = true,
            KnownTypes = SpecKnownTypes.All,
        };

        private static readonly DataContractSerializerSettings EnvelopeSettings = new()
        {
            PreserveObjectReferences = false,
            SerializeReadOnlyTypes = true,
        };

        /// <summary>
        /// Validates and serializes a dialog spec into a versioned envelope blob.
        /// </summary>
        /// <param name="spec">The dialog spec to serialize.</param>
        /// <returns>The envelope bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="spec"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the spec fails structural validation.</exception>
        public static byte[] Serialize(DialogSpec spec)
        {
            if (spec is null)
            {
                throw new ArgumentNullException(nameof(spec));
            }
            SpecTreeValidator.Validate(spec);
            return SerializeEnveloped(spec, typeof(DialogSpec), SpecSettings);
        }

        /// <summary>
        /// Deserializes a versioned envelope blob back into a dialog spec, failing loudly on a
        /// newer-than-supported schema version.
        /// </summary>
        /// <param name="data">The envelope bytes produced by <see cref="Serialize"/>.</param>
        /// <returns>The dialog spec.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is empty.</exception>
        /// <exception cref="NotSupportedException">Thrown when the envelope schema version is newer than <see cref="CurrentSchemaVersion"/>.</exception>
        /// <exception cref="SerializationException">Thrown when the envelope or payload is malformed.</exception>
        public static DialogSpec Deserialize(byte[] data)
        {
            return (DialogSpec)DeserializeEnveloped(data, typeof(DialogSpec), SpecSettings);
        }

        /// <summary>
        /// Serializes a remote dialog request into a versioned envelope blob for the host pipe.
        /// </summary>
        /// <param name="request">The remote dialog request to serialize.</param>
        /// <returns>The envelope bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
        public static byte[] SerializeRemoteRequest(RemoteDialogRequest request)
        {
            return request is null
                ? throw new ArgumentNullException(nameof(request))
                : SerializeEnveloped(request, typeof(RemoteDialogRequest), EnvelopeSettings);
        }

        /// <summary>
        /// Deserializes a versioned envelope blob back into a remote dialog request, failing loudly
        /// on a newer-than-supported schema version.
        /// </summary>
        /// <param name="data">The envelope bytes produced by <see cref="SerializeRemoteRequest"/>.</param>
        /// <returns>The remote dialog request.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is empty.</exception>
        /// <exception cref="NotSupportedException">Thrown when the envelope schema version is newer than <see cref="CurrentSchemaVersion"/>.</exception>
        /// <exception cref="SerializationException">Thrown when the envelope or payload is malformed.</exception>
        public static RemoteDialogRequest DeserializeRemoteRequest(byte[] data)
        {
            return (RemoteDialogRequest)DeserializeEnveloped(data, typeof(RemoteDialogRequest), EnvelopeSettings);
        }

        /// <summary>
        /// Serializes a dialog result into a versioned envelope blob for the host pipe.
        /// </summary>
        /// <param name="result">The dialog result to serialize.</param>
        /// <returns>The envelope bytes.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
        public static byte[] SerializeResult(SpecDialogResult result)
        {
            return result is null
                ? throw new ArgumentNullException(nameof(result))
                : SerializeEnveloped(result, typeof(SpecDialogResult), EnvelopeSettings);
        }

        /// <summary>
        /// Deserializes a versioned envelope blob back into a dialog result, failing loudly on a
        /// newer-than-supported schema version.
        /// </summary>
        /// <param name="data">The envelope bytes produced by <see cref="SerializeResult"/>.</param>
        /// <returns>The dialog result.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="data"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is empty.</exception>
        /// <exception cref="NotSupportedException">Thrown when the envelope schema version is newer than <see cref="CurrentSchemaVersion"/>.</exception>
        /// <exception cref="SerializationException">Thrown when the envelope or payload is malformed.</exception>
        public static SpecDialogResult DeserializeResult(byte[] data)
        {
            return (SpecDialogResult)DeserializeEnveloped(data, typeof(SpecDialogResult), EnvelopeSettings);
        }

        /// <summary>
        /// Validates and serializes a dialog spec into a Base64 envelope string (string-transport form).
        /// </summary>
        /// <param name="spec">The dialog spec to serialize.</param>
        /// <returns>The Base64 envelope string.</returns>
        public static string SerializeToBase64(DialogSpec spec)
        {
            return Convert.ToBase64String(Serialize(spec));
        }

        /// <summary>
        /// Deserializes a Base64 envelope string back into a dialog spec.
        /// </summary>
        /// <param name="data">The Base64 envelope string produced by <see cref="SerializeToBase64"/>.</param>
        /// <returns>The dialog spec.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="data"/> is null or whitespace.</exception>
        public static DialogSpec DeserializeFromBase64(string data)
        {
            return string.IsNullOrWhiteSpace(data)
                ? throw new ArgumentException("The spec envelope string is null or empty.", nameof(data))
                : Deserialize(Convert.FromBase64String(data));
        }

        private static byte[] SerializeEnveloped(object graph, Type rootType, DataContractSerializerSettings settings)
        {
            byte[] payload = SerializeCore(graph, rootType, settings);
            string version = typeof(SpecSerialization).Assembly.GetName().Version?.ToString() ?? "0.0.0.0";
            SpecEnvelope envelope = new(CurrentSchemaVersion, version, payload);
            return SerializeCore(envelope, typeof(SpecEnvelope), EnvelopeSettings);
        }

        private static object DeserializeEnveloped(byte[] data, Type rootType, DataContractSerializerSettings settings)
        {
            if (data is null)
            {
                throw new ArgumentNullException(nameof(data));
            }
            if (data.Length == 0)
            {
                throw new ArgumentException("The spec envelope data is empty.", nameof(data));
            }
            SpecEnvelope envelope = (SpecEnvelope)DeserializeCore(data, typeof(SpecEnvelope), EnvelopeSettings);
            return envelope.SchemaVersion < 1
                ? throw new SerializationException(FormattableString.Invariant($"The spec envelope declares an invalid schema version ({envelope.SchemaVersion})."))
                : envelope.SchemaVersion > CurrentSchemaVersion
                ? throw new NotSupportedException(FormattableString.Invariant($"The spec envelope declares schema version {envelope.SchemaVersion} (written by Fluence.Wpf.Specs {envelope.SpecsAssemblyVersion}), which is newer than the highest version this build supports ({CurrentSchemaVersion}). Update Fluence.Wpf and Fluence.Wpf.Specs to a matching or newer build."))
                : envelope.Payload is not byte[] payload || payload.Length == 0
                ? throw new SerializationException("The spec envelope payload is missing or empty.")
                : DeserializeCore(payload, rootType, settings);
        }

        private static byte[] SerializeCore(object graph, Type rootType, DataContractSerializerSettings settings)
        {
            using MemoryStream stream = new();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                DataContractSerializer serializer = new(rootType, settings);
                serializer.WriteObject(writer, graph);
            }
            return stream.ToArray();
        }

        private static object DeserializeCore(byte[] data, Type rootType, DataContractSerializerSettings settings)
        {
            using XmlDictionaryReader reader = XmlDictionaryReader.CreateBinaryReader(data, ReaderQuotas);
            DataContractSerializer serializer = new(rootType, settings);
            return serializer.ReadObject(reader) ?? throw new SerializationException($"Deserialization of '{rootType.Name}' returned a null result.");
        }
    }
}
