using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TShockAPI;
using Terraria;

namespace Statistics
{
    public class StatPl
    {
        public int Index;


        public DateTime lastTimeUpdate = DateTime.Now;
        public DateTime lastAfkUpdate = DateTime.Now;

        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public string Name { get { return Main.player[Index].name; } }

        public bool AFK = false;
        public int AFKcount = 0;
        public float lastPosX { get; set; }
        public float lastPosY { get; set; }

        public int totalPoints { get; set; }
        public int TimePlayed = 0;

        public int deaths;
        public int kills;
        public int mobkills;
        public int bosskills;

        public Vector2 posPoint;

        public StatPl KillingPlayer = null;

        public StatPl(int index)
        {
            Index = index;
            lastPosX = TShock.Players[Index].X;
            lastPosX = TShock.Players[Index].Y;
        }
    }
}
