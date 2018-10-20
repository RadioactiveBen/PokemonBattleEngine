﻿using Ether.Network.Packets;
using Kermalis.PokemonBattleEngine.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Kermalis.PokemonBattleEngine.Packets
{
    public sealed class PStatus1EndedPacket : INetPacket
    {
        public const short Code = 0x12;
        public IEnumerable<byte> Buffer { get; }

        public readonly Guid PokemonId;
        public readonly PStatus1 Status1;

        public PStatus1EndedPacket(PPokemon pkmn)
        {
            var bytes = new List<byte>();
            bytes.AddRange(BitConverter.GetBytes(Code));
            bytes.AddRange((PokemonId = pkmn.Id).ToByteArray());
            bytes.Add((byte)(Status1 = pkmn.Status1));
            Buffer = BitConverter.GetBytes((short)bytes.Count).Concat(bytes);
        }
        public PStatus1EndedPacket(byte[] buffer)
        {
            Buffer = buffer;
            using (var r = new BinaryReader(new MemoryStream(buffer)))
            {
                r.ReadInt16(); // Skip Code
                PokemonId = new Guid(r.ReadBytes(0x10));
                Status1 = (PStatus1)r.ReadByte();
            }
        }

        public void Dispose() { }
    }
}
