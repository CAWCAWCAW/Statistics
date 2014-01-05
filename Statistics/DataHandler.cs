using System;
using System.Collections.Generic;
using System.Xml;
using System.Reflection;
using Terraria;
using MySql.Data.MySqlClient;
using System.Threading;
using System.ComponentModel;
using System.IO;
using TShockAPI;
using TShockAPI.DB;
using TShockAPI.Net;
using System.IO.Streams;

namespace Statistics
{
    internal delegate bool GetDataHandlerDelegate(GetDataHandlerArgs args);
    internal class GetDataHandlerArgs : EventArgs
    {
        public TSPlayer Player { get; private set; }
        public MemoryStream Data { get; private set; }

        public Player TPlayer
        {
            get { return Player.TPlayer; }
        }

        public GetDataHandlerArgs(TSPlayer player, MemoryStream data)
        {
            Player = player;
            Data = data;
        }
    }
    internal static class GetDataHandlers
    {
        private static Dictionary<PacketTypes, GetDataHandlerDelegate> GetDataHandlerDelegates;

        public static void InitGetDataHandler()
        {
            GetDataHandlerDelegates = new Dictionary<PacketTypes, GetDataHandlerDelegate>
            {
                {PacketTypes.PlayerKillMe, HandlePlayerKillMe},             
                {PacketTypes.PlayerDamage, HandlePlayerDamage},
                {PacketTypes.NpcStrike, HandleNPCEvent},
            };
        }

        public static bool HandlerGetData(PacketTypes type, TSPlayer player, MemoryStream data)
        {
            GetDataHandlerDelegate handler;
            if (GetDataHandlerDelegates.TryGetValue(type, out handler))
            {
                try
                {
                    return handler(new GetDataHandlerArgs(player, data));
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
            return false;
        }

        private static bool HandleNPCEvent(GetDataHandlerArgs args)
        {
            int index = args.Player.Index;
            byte npcID = (byte)args.Data.ReadByte();
            byte hitDirection = (byte)args.Data.ReadByte();
            Int16 Damage = (Int16)args.Data.ReadInt16();
            bool Crit = args.Data.ReadBoolean();
            var player = sTools.GetPlayer(index);

            if (Main.npc[npcID].target < 255)
            {
                int critical = 1;
                if (Crit)
                    critical = 2;
                int hitDamage = (Damage - Main.npc[npcID].defense / 2) * critical;

                if (hitDamage > Main.npc[npcID].life && Main.npc[npcID].active && Main.npc[npcID].life > 0)
                {
                    if (!Main.npc[npcID].boss)
                        player.mobkills++;
                    else
                        player.bosskills++;
                }
            }
            else
            {
                return true;
            }

            return false;
        }

        private static bool HandlePlayerKillMe(GetDataHandlerArgs args)
        {
            int index = args.Player.Index;
            byte PlayerID = (byte)args.Data.ReadByte();
            byte hitDirection = (byte)args.Data.ReadByte();
            Int16 Damage = (Int16)args.Data.ReadInt16();
            bool PVP = args.Data.ReadBoolean();
            var player = sTools.GetPlayer(PlayerID);

            if (player.KillingPlayer != null)
            {
                if (PVP == true)
                {
                    player.KillingPlayer.kills++;
                    player.deaths++;
                }
                player.KillingPlayer = null;
            }
            else
            {
                player.deaths++;
            }

            return false;
        }

        private static bool HandlePlayerDamage(GetDataHandlerArgs args)
        {
            int index = args.Player.Index;
            byte PlayerID = (byte)args.Data.ReadByte();
            byte hitDirection = (byte)args.Data.ReadByte();
            Int16 Damage = (Int16)args.Data.ReadInt16();
            var player = sTools.GetPlayer(PlayerID);
            bool PVP = args.Data.ReadBoolean();
            byte Crit = (byte)args.Data.ReadByte();

            if (index != PlayerID)
            {
                player.KillingPlayer = sTools.GetPlayer(index);
            }
            else
            {
                player.KillingPlayer = null;
            }

            return false;
        }
    }
}