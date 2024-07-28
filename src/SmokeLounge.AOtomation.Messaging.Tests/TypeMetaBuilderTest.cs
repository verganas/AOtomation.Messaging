﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TypeMetaBuilderTest.cs" company="SmokeLounge">
//   Copyright © 2013 SmokeLounge.
//   This program is free software. It comes without any warranty, to
//   the extent permitted by applicable law. You can redistribute it
//   and/or modify it under the terms of the Do What The Fuck You Want
//   To Public License, Version 2, as published by Sam Hocevar. See
//   http://www.wtfpl.net/ for more details.
// </copyright>
// <summary>
//   Defines the TypeMetaBuilderTest type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SmokeLounge.AOtomation.Messaging.Tests
{
    using System;
    using System.IO;
    using System.Net;

    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using SmokeLounge.AOtomation.Messaging.GameData;
    using SmokeLounge.AOtomation.Messaging.Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages;
    using SmokeLounge.AOtomation.Messaging.Messages.N3Messages.OrgServerMessages;
    using SmokeLounge.AOtomation.Messaging.Messages.SystemMessages;
    using SmokeLounge.AOtomation.Messaging.Serialization;

    using StreamReader = SmokeLounge.AOtomation.Messaging.Serialization.StreamReader;
    using StreamWriter = SmokeLounge.AOtomation.Messaging.Serialization.StreamWriter;

    [TestClass]
    public class TypeMetaBuilderTest
    {
        #region Public Methods and Operators

        [TestMethod]
        public void CharacterActionMessageTest()
        {
            var expected = new CharacterActionMessage
                               {
                                   Identity =
                                       new Identity
                                           {
                                               Type = IdentityType.CanbeAffected, 
                                               Instance = 12345
                                           }, 
                                   Target = new Identity { Type = IdentityType.None }, 
                                   Parameter1 = 0, 
                                   Parameter2 = 12345
                               };

            var actual = (CharacterActionMessage)this.SerializeDeserialize(expected);

            this.AssertN3Message(expected, actual);
            this.AssertCharacterActionMessage(expected, actual);

            Assert.AreEqual(expected.Target.Type, actual.Target.Type);
            Assert.AreEqual(expected.Target.Instance, actual.Target.Instance);
            Assert.AreEqual(expected.Unknown1, actual.Unknown1);
            Assert.AreEqual(expected.Parameter1, actual.Parameter1);
            Assert.AreEqual(expected.Parameter2, actual.Parameter2);
            Assert.AreEqual(expected.Unknown2, actual.Unknown2);
        }

        [TestMethod]
        public void CharacterListMessageTest()
        {
            var expected = new CharacterListMessage
                               {
                                   Characters =
                                       new[]
                                           {
                                               new LoginCharacterInfo
                                                   {
                                                       Name = "Trolololo", 
                                                       AreaName = "ICC", 
                                                       PlayfieldId = Identity.None, 
                                                       ExitDoorId = Identity.None
                                                   }, 
                                               new LoginCharacterInfo
                                                   {
                                                       Name = "Haiguise", 
                                                       AreaName = "Bore", 
                                                       PlayfieldId = Identity.None, 
                                                       ExitDoorId = Identity.None
                                                   }
                                           }
                               };

            var actual = (CharacterListMessage)this.SerializeDeserialize(expected);

            this.AssertSystemMessage(expected, actual);
            Assert.AreEqual(expected.AllowedCharacters, actual.AllowedCharacters);
            Assert.AreEqual(expected.Characters.Length, actual.Characters.Length);

            var expectedChars = expected.Characters.GetEnumerator();
            var actualChars = actual.Characters.GetEnumerator();

            while (expectedChars.MoveNext())
            {
                actualChars.MoveNext();
                var expectedChar = (LoginCharacterInfo)expectedChars.Current;
                var actualChar = (LoginCharacterInfo)actualChars.Current;

                Assert.AreEqual(expectedChar.AreaName, actualChar.AreaName);
                Assert.AreEqual(expectedChar.Name, actualChar.Name);
            }

            Assert.AreEqual(expected.Expansions, actual.Expansions);
        }

        [TestMethod]
        public void OrgClientMessageTest1()
        {
            var expected = new OrgClientMessage
                               {
                                   Identity =
                                       new Identity
                                           {
                                               Type = IdentityType.CanbeAffected, 
                                               Instance = 12345
                                           }, 
                                   Target =
                                       new Identity
                                           {
                                               Type = IdentityType.CanbeAffected, 
                                               Instance = 12345
                                           }, 
                                   Command = OrgClientCommand.Create, 
                                   CommandArgs = "test"
                               };

            var actual = (OrgClientMessage)this.SerializeDeserialize(expected);

            this.AssertN3Message(expected, actual);
            this.AssertOrgClientMessage(expected, actual);
        }

        [TestMethod]
        public void OrgClientMessageTest2()
        {
            var expected = new OrgClientMessage
                               {
                                   Identity =
                                       new Identity
                                           {
                                               Type = IdentityType.CanbeAffected, 
                                               Instance = 12345
                                           }, 
                                   Target =
                                       new Identity
                                           {
                                               Type = IdentityType.CanbeAffected, 
                                               Instance = 12345
                                           }, 
                                   Command = OrgClientCommand.Ranks, 
                                   CommandArgs = "test"
                               };

            var actual = (OrgClientMessage)this.SerializeDeserialize(expected);

            this.AssertN3Message(expected, actual);
            Assert.IsNull(actual.CommandArgs);
        }

        [TestMethod]
        public void OrgInviteMessageTest()
        {
            var expected = new OrgInviteMessage
                               {
                                   Identity =
                                       new Identity
                                           {
                                               Type = IdentityType.CanbeAffected, 
                                               Instance = 12345
                                           }, 
                                   Organization =
                                       new Identity
                                           {
                                               Type = IdentityType.Organization, 
                                               Instance = 67890
                                           }, 
                                   OrganizationName = "Trollipopz"
                               };

            var actual = (OrgInviteMessage)this.SerializeDeserialize(expected);

            this.AssertN3Message(expected, actual);
            this.AssertOrgServerMessage(expected, actual);
        }

        [TestMethod]
        public void PlayfieldAnarchyFMessageTest()
        {
            var expected = new PlayfieldAnarchyFMessage
                               {
                                   Identity = Identity.None, 
                                   CharacterCoordinates = new Vector3(), 
                                   PlayfieldId1 = Identity.None, 
                                   PlayfieldId2 = Identity.None, 
                                   PlayfieldX = 1, 
                                   PlayfieldZ = 2
                               };

            var actual = (PlayfieldAnarchyFMessage)this.SerializeDeserialize(expected);

            Assert.AreEqual(expected.PlayfieldX, actual.PlayfieldX);
            Assert.AreEqual(expected.PlayfieldZ, actual.PlayfieldZ);
        }

        [TestMethod]
        public void PlayfieldAnarchyFMessageWithVendorTest()
        {
            var expected = new PlayfieldAnarchyFMessage
                               {
                                   Identity = Identity.None, 
                                   CharacterCoordinates = new Vector3(), 
                                   PlayfieldId1 = Identity.None, 
                                   PlayfieldId2 = Identity.None, 
                                   PlayfieldVendorInfo = new PlayfieldVendorInfo(), 
                                   PlayfieldX = 1, 
                                   PlayfieldZ = 2
                               };

            var actual = (PlayfieldAnarchyFMessage)this.SerializeDeserialize(expected);

            Assert.AreEqual(expected.PlayfieldX, actual.PlayfieldX);
            Assert.AreEqual(expected.PlayfieldZ, actual.PlayfieldZ);
        }

        [TestMethod]
        public void StatMessageTest()
        {
            var expected = new StatMessage
                               {
                                   Identity = Identity.None, 
                                   Stats =
                                       new[]
                                           {
                                               new GameTuple<CharacterStat, uint>
                                                   {
                                                       Value1 =
                                                           CharacterStat
                                                           .ACGEntranceStyles, 
                                                       Value2 = 1
                                                   }, 
                                               new GameTuple<CharacterStat, uint>
                                                   {
                                                       Value1 =
                                                           CharacterStat
                                                           .BackMesh, 
                                                       Value2 = 3
                                                   }, 
                                               new GameTuple<CharacterStat, uint>
                                                   {
                                                       Value1 =
                                                           CharacterStat
                                                           .CATAnim, 
                                                       Value2 = 5
                                                   }
                                           }
                               };

            var actual = (StatMessage)this.SerializeDeserialize(expected);

            this.AssertN3Message(expected, actual);

            var expectedChars = expected.Stats.GetEnumerator();
            var actualChars = actual.Stats.GetEnumerator();

            while (expectedChars.MoveNext())
            {
                actualChars.MoveNext();
                var expectedChar = (GameTuple<CharacterStat, uint>)expectedChars.Current;
                var actualChar = (GameTuple<CharacterStat, uint>)actualChars.Current;

                Assert.AreEqual(expectedChar.Value1, actualChar.Value1);
                Assert.AreEqual(expectedChar.Value2, actualChar.Value2);
            }
        }

        [TestMethod]
        public void ZoneRedirectionMessageTest()
        {
            var expected = new ZoneInfoMessage
                               {
                                   CharacterId = 1234567890, 
                                   ServerIpAddress = IPAddress.Loopback, 
                                   ServerPort = 45678
                               };

            var actual = (ZoneInfoMessage)this.SerializeDeserialize(expected);

            this.AssertSystemMessage(expected, actual);
            Assert.AreEqual(expected.CharacterId, actual.CharacterId);
            Assert.AreEqual(expected.ServerIpAddress, actual.ServerIpAddress);
            Assert.AreEqual(expected.ServerPort, actual.ServerPort);
        }

        [TestMethod]
        public void CreateCharacterMessageTest()
        {
            var expected = new CreateCharacterMessage
           {
               Unknown1 = null, // Auto einai to "bug", prepei na kaneis init byte array
               Name = "verganas",
               AreaName = "unknown area",
               Breed = Breed.Nanomage,
               Fatness = Fatness.Fat,
               Gender = Gender.Female,
               HeadMesh = 4711,
               Level = 1,
               MonsterScale = 0x64,
               Profession = Profession.Keeper,
               StarterArea = StarterArea.RubiKa
           };

            var actual = (CreateCharacterMessage)this.SerializeDeserialize(expected);

            this.AssertSystemMessage(expected, actual);
            Assert.AreEqual(expected.Name, actual.Name);
            Assert.AreEqual(expected.AreaName, actual.AreaName);
            Assert.AreEqual(expected.Breed, actual.Breed);
        }

        #endregion

        #region Methods

        private void AssertCharacterActionMessage(CharacterActionMessage expected, CharacterActionMessage actual)
        {
            Assert.AreEqual(expected.Action, actual.Action);
        }

        private void AssertN3Message(N3Message expected, N3Message actual)
        {
            Assert.AreEqual(expected.Identity, actual.Identity);
            Assert.AreEqual(expected.N3MessageType, actual.N3MessageType);
            Assert.AreEqual(expected.PacketType, actual.PacketType);
            Assert.AreEqual(expected.Unknown, actual.Unknown);
        }

        private void AssertOrgClientMessage(OrgClientMessage expected, OrgClientMessage actual)
        {
            Assert.AreEqual(expected.Command, actual.Command);
            Assert.AreEqual(expected.Target, actual.Target);
            Assert.AreEqual(expected.Unknown1, actual.Unknown1);
            Assert.AreEqual(expected.CommandArgs, actual.CommandArgs);
        }

        private void AssertOrgServerMessage(OrgServerMessage expected, OrgServerMessage actual)
        {
            Assert.AreEqual(expected.OrgServerMessageType, actual.OrgServerMessageType);
            Assert.AreEqual(expected.Unknown1, actual.Unknown1);
            Assert.AreEqual(expected.Unknown2, actual.Unknown2);
            Assert.AreEqual(expected.Organization, actual.Organization);
            Assert.AreEqual(expected.OrganizationName, actual.OrganizationName);
        }

        private void AssertSystemMessage(SystemMessage expected, SystemMessage actual)
        {
            Assert.AreEqual(expected.PacketType, actual.PacketType);
            Assert.AreEqual(expected.SystemMessageType, actual.SystemMessageType);
        }

        private object SerializeDeserialize(object obj)
        {
            MemoryStream memoryStream = null;
            
            //var serializerResolver = new DebuggingSerializerResolverBuilder<MessageBody>().Build();
            var serializerResolver = new SerializerResolverBuilder<MessageBody>().Build();
            var serializer = serializerResolver.GetSerializer(obj.GetType());

            try
            {
                memoryStream = new MemoryStream();
                using (var streamWriter = new StreamWriter(memoryStream))
                using (var streamReader = new StreamReader(memoryStream))
                {
                    var serializationContext = new SerializationContext(serializerResolver);
                    serializer.Serialize(streamWriter, serializationContext, obj);
                    var arr = memoryStream.ToArray();
                    Console.WriteLine(BitConverter.ToString(arr));

                    memoryStream.Position = 0;
                    var deserializationContext = new SerializationContext(serializerResolver);
                    var result = serializer.Deserialize(streamReader, deserializationContext);
                    memoryStream = null;
                    return result;
                }
            }
            finally
            {
                if (memoryStream != null)
                {
                    memoryStream.Dispose();
                }
            }
        }

        #endregion
    }
}