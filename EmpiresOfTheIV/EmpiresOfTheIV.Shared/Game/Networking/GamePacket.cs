﻿using EmpiresOfTheIV.Game.Commands;
using EmpiresOfTheIV.Game.Enumerators;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmpiresOfTheIV.Game.Networking
{
    public class GamePacket : EotIVPacket
    {
        public GamePacketID ID { get; set; }
        public List<Command> Commands { get; set; }


        public GamePacket(bool requiresAck, List<Command> commands, GamePacketID id)
            : base(requiresAck, PacketType.GameData)
        {
            ID = id;
            Commands = commands;
        }

        public void SetFromOtherPacket(GamePacket o)
        {
            base.SetFromOtherPacket(o);
            ID = o.ID;
            Commands = o.Commands;
        }

        public override string ThisToJson()
        {
            return JsonConvert.SerializeObject(this);
        }

        public override void JsonToThis(string json)
        {
            JObject jObject = JObject.Parse(json);
            GamePacket packet = JsonConvert.DeserializeObject<GamePacket>(jObject.ToString());

            SetFromOtherPacket(packet);
        }
    }
}
