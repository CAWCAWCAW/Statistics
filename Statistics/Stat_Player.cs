using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using TShockAPI;
using Terraria;

namespace Statistics
{
    public class sPlayer
    {
        public int Index;

        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public string Name { get { return Main.player[Index].name; } }

        public bool AFK = false;
        public int AFKcount = 0;
        public float lastPosX { get; set; }
        public float lastPosY { get; set; }

        public int totalPoints { get; set; }
        public int TimePlayed = 0;

        public string firstLogin;
        public string lastSeen;
        public int loginCount;
        public string knownAccounts;
        public string knownIPs;

        public int deaths;
        public int kills;
        public int mobkills;
        public int bosskills;

        public Vector2 posPoint;

        public sPlayer KillingPlayer = null;

        public sPlayer(int index)
        {
            Index = index;
            lastPosX = TShock.Players[Index].X;
            lastPosY = TShock.Players[Index].Y;
        }
    }

    public class storedPlayer
    {
        public string name;
        public string firstLogin;
        public string lastSeen;
        public int totalTime;
        public int loginCount;
        public string knownAccounts;
        public string knownIPs;

        public int kills;
        public int deaths;
        public int mobkills;
        public int bosskills;

        public storedPlayer(string name, string firstLogin, string lastSeen, int totalTime, int loginCount,
            string knownAccounts, string knownIPs, int kills, int deaths, int mobkills, int bosskills)
        {
            this.name = name;
            this.firstLogin = firstLogin;
            this.lastSeen = lastSeen;
            this.totalTime = totalTime;
            this.loginCount = loginCount;
            this.knownAccounts = knownAccounts;
            this.knownIPs = knownIPs;
            this.kills = kills;
            this.deaths = deaths;
            this.mobkills = mobkills;
            this.bosskills = bosskills;
        }
    }
}
