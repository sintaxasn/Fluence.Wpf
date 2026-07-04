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
using System.Collections;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Xml;
using Fluence.Wpf.Specs;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class SpecSerializationTests
    {
        private static DialogSpec BuildUserFlowSpec()
        {
            DialogSpec dialog = new()
            {
                Title = "Contoso IT",
            };
            dialog.Content.Add(new TextBlockSpec { Text = "Before we upgrade, tell us where you sit." });
            TextBoxSpec desk = new() { Name = "Desk", PlaceholderText = "Desk number" };
            desk.Rules.Add(new NotEmptyRule());
            dialog.Content.Add(desk);
            ComboBoxSpec site = new() { Name = "Site", SelectedItem = "Sydney" };
            site.Items.Add("Sydney");
            site.Items.Add("Melbourne");
            site.Items.Add("Auckland");
            dialog.Content.Add(site);
            dialog.Content.Add(new CheckBoxSpec { Name = "Vpn", Content = "I use VPN daily" });
            dialog.Buttons.Add(new ButtonSpec { Text = "Continue", IsDefault = true });
            dialog.Buttons.Add(new ButtonSpec { Text = "Defer", IsCancel = true });
            return dialog;
        }

        [TestMethod]
        public void Serialize_RoundTrips_ByteStable()
        {
            DialogSpec dialog = BuildUserFlowSpec();

            byte[] first = SpecSerialization.Serialize(dialog);
            DialogSpec back = SpecSerialization.Deserialize(first);
            byte[] second = SpecSerialization.Serialize(back);

            CollectionAssert.AreEqual(first, second, "serialize(deserialize(bytes)) must be byte-identical");
            Assert.AreEqual("Contoso IT", back.Title);
            Assert.AreEqual(4, back.Content.Count);
            Assert.AreEqual(2, back.Buttons.Count);
            Assert.AreEqual(1, back.Content[1].Rules.Count);
            _ = Assert.IsInstanceOfType<NotEmptyRule>(back.Content[1].Rules[0]);
            ComboBoxSpec site = (ComboBoxSpec)back.Content[2];
            Assert.AreEqual(3, site.Items.Count);
            Assert.AreEqual("Sydney", site.SelectedItem);
            Assert.IsTrue(back.Buttons[0].IsDefault);
            Assert.IsTrue(back.Buttons[1].IsCancel);
        }

        [TestMethod]
        public void SerializeToBase64_RoundTrips()
        {
            DialogSpec dialog = BuildUserFlowSpec();

            string envelope = SpecSerialization.SerializeToBase64(dialog);
            DialogSpec back = SpecSerialization.DeserializeFromBase64(envelope);

            Assert.AreEqual(dialog.Title, back.Title);
            Assert.AreEqual(dialog.Content.Count, back.Content.Count);
        }

        [TestMethod]
        public void ImageSpec_RoundTrips_PathAndBase64Forms()
        {
            string base64 = Convert.ToBase64String([1, 2, 3, 4]);
            DialogSpec dialog = new()
            {
                Title = "Brand",
            };
            dialog.Content.Add(new ImageSpec
            {
                Source = @"C:\brand\banner.png",
                Stretch = SpecStretch.UniformToFill,
                CornerRadius = "8",
            });
            dialog.Content.Add(new ImageSpec { SourceBase64 = base64 });
            dialog.Buttons.Add(new ButtonSpec { Text = "OK" });

            byte[] first = SpecSerialization.Serialize(dialog);
            DialogSpec back = SpecSerialization.Deserialize(first);
            byte[] second = SpecSerialization.Serialize(back);

            CollectionAssert.AreEqual(first, second, "serialize(deserialize(bytes)) must be byte-identical");
            ImageSpec pathForm = (ImageSpec)back.Content[0];
            Assert.AreEqual(@"C:\brand\banner.png", pathForm.Source);
            Assert.AreEqual(SpecStretch.UniformToFill, pathForm.Stretch);
            Assert.AreEqual("8", pathForm.CornerRadius);
            ImageSpec bytesForm = (ImageSpec)back.Content[1];
            Assert.AreEqual(base64, bytesForm.SourceBase64);
        }

        [TestMethod]
        public void ImageSpec_DictionaryConstructor_AutoEncodesByteArray()
        {
            byte[] bytes = [1, 2, 3, 4];
            Hashtable properties = new()
            {
                ["SourceBase64"] = bytes,
                ["Stretch"] = "Fill",
                ["CornerRadius"] = "4",
            };

            ImageSpec spec = new(properties);

            Assert.AreEqual(Convert.ToBase64String(bytes), spec.SourceBase64, "a byte array auto-encodes to Base64");
            Assert.AreEqual(SpecStretch.Fill, spec.Stretch);
            Assert.AreEqual("4", spec.CornerRadius);
        }

        [TestMethod]
        public void Deserialize_RejectsNewerSchemaVersion_WithActionableMessage()
        {
            // A contract-shaped stand-in lets the test author a future-versioned envelope without
            // access to the internal SpecEnvelope type: DataContract matching is by name/namespace.
            byte[] data = SerializeFutureEnvelope();

            NotSupportedException exception = Assert.ThrowsExactly<NotSupportedException>(() => SpecSerialization.Deserialize(data));
            StringAssert.Contains(exception.Message, "99", StringComparison.Ordinal);
            StringAssert.Contains(exception.Message, "matching or newer build", StringComparison.Ordinal);
        }

        [TestMethod]
        public void Deserialize_RejectsEmptyData()
        {
            _ = Assert.ThrowsExactly<ArgumentException>(() => SpecSerialization.Deserialize([]));
        }

        [TestMethod]
        public void DictionaryConstructor_RejectsUnknownKey_ListingValidProperties()
        {
            Hashtable properties = new() { ["Nope"] = 1 };

            ArgumentException exception = Assert.ThrowsExactly<ArgumentException>(() => _ = new TextBoxSpec(properties));
            StringAssert.Contains(exception.Message, "Nope", StringComparison.Ordinal);
            StringAssert.Contains(exception.Message, "PlaceholderText", StringComparison.Ordinal);
        }

        [TestMethod]
        public void Validator_RejectsSharedNodeInstance()
        {
            TextBlockSpec shared = new() { Text = "shared" };
            DialogSpec dialog = new();
            dialog.Content.Add(shared);
            dialog.Content.Add(shared);
            dialog.Buttons.Add(new ButtonSpec { Text = "OK" });

            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => SpecSerialization.Serialize(dialog));
            StringAssert.Contains(exception.Message, "more than once", StringComparison.Ordinal);
        }

        [TestMethod]
        public void Validator_RejectsCycle()
        {
            StackPanelSpec stack = new();
            BorderSpec border = new() { Child = stack };
            stack.Children.Add(border);
            DialogSpec dialog = new();
            dialog.Content.Add(border);
            dialog.Buttons.Add(new ButtonSpec { Text = "OK" });

            _ = Assert.ThrowsExactly<InvalidOperationException>(() => SpecTreeValidator.Validate(dialog));
        }

        [TestMethod]
        public void Validator_RejectsDuplicateInputNames_CaseInsensitively()
        {
            DialogSpec dialog = new();
            dialog.Content.Add(new TextBoxSpec { Name = "Desk" });
            dialog.Content.Add(new TextBoxSpec { Name = "desk" });
            dialog.Buttons.Add(new ButtonSpec { Text = "OK" });

            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => SpecTreeValidator.Validate(dialog));
            StringAssert.Contains(exception.Message, "Duplicate input name", StringComparison.Ordinal);
        }

        [TestMethod]
        public void Validator_RequiresAtLeastOneButton_AndButtonText()
        {
            DialogSpec noButtons = new();
            _ = Assert.ThrowsExactly<InvalidOperationException>(() => SpecTreeValidator.Validate(noButtons));

            DialogSpec blankButton = new();
            blankButton.Buttons.Add(new ButtonSpec { Text = "   " });
            _ = Assert.ThrowsExactly<InvalidOperationException>(() => SpecTreeValidator.Validate(blankButton));
        }

        [TestMethod]
        public void Validator_RejectsRulesOnUnnamedElement()
        {
            TextBoxSpec unnamed = new();
            unnamed.Rules.Add(new NotEmptyRule());
            DialogSpec dialog = new();
            dialog.Content.Add(unnamed);
            dialog.Buttons.Add(new ButtonSpec { Text = "OK" });

            InvalidOperationException exception = Assert.ThrowsExactly<InvalidOperationException>(() => SpecTreeValidator.Validate(dialog));
            StringAssert.Contains(exception.Message, "no Name", StringComparison.Ordinal);
        }

        [TestMethod]
        public void SpecKnownTypes_CoverEveryPublicConcreteSpecType()
        {
            Type[] expected =
            [
                .. typeof(SpecNode).Assembly
                    .GetTypes()
                    .Where(static type => type.IsPublic && !type.IsAbstract
                        && (typeof(SpecNode).IsAssignableFrom(type) || typeof(SpecRule).IsAssignableFrom(type)))
                    .OrderBy(static type => type.FullName, StringComparer.Ordinal),
            ];

            foreach (Type type in expected)
            {
                CollectionAssert.Contains(SpecKnownTypes.All, type, $"{type.FullName} is missing from SpecKnownTypes.All");
            }
        }

        private static byte[] SerializeFutureEnvelope()
        {
            DataContractSerializer serializer = new(typeof(FutureEnvelope), new DataContractSerializerSettings
            {
                SerializeReadOnlyTypes = true,
            });
            using MemoryStream stream = new();
            using (XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream))
            {
                serializer.WriteObject(writer, new FutureEnvelope());
            }
            return stream.ToArray();
        }

        [DataContract(Name = "SpecEnvelope", Namespace = "http://schemas.fluencewpf.com/specs/2026/07")]
        private sealed class FutureEnvelope
        {
            [DataMember(Name = "SchemaVersion", Order = 0)]
            internal int SchemaVersion { get; set; } = 99;

            [DataMember(Name = "SpecsAssemblyVersion", Order = 1)]
            internal string SpecsAssemblyVersion { get; set; } = "9.9.9.9";

            [DataMember(Name = "Payload", Order = 2)]
            internal byte[] Payload { get; set; } = [1, 2, 3];
        }
    }
}
