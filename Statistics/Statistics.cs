using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;

using Terraria;
using TerrariaApi;
using TerrariaApi.Server;

using TShockAPI;
using TShockAPI.DB;

using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;

namespace Statistics
{
    [ApiVersion(1, 14)]
    public class Statistics : TerrariaPlugin
    {
        public static List<TimePl> PlayerList = new List<TimePl>();

        public static IDbConnection db;

        public override string Author
        { get { return "WhiteX"; } }

        public override string Description
        { get { return "Time statistics for players"; } }

        public override string Name
        { get { return "Time"; } }

        public override Version Version
        { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                var Hook = ServerApi.Hooks;

                Hook.GameInitialize.Deregister(this, OnInitialize);
                Hook.NetGreetPlayer.Deregister(this, OnGreet);
                Hook.GameUpdate.Deregister(this, OnUpdate);
                Hook.ServerLeave.Deregister(this, OnLeave);
                Hook.ServerChat.Deregister(this, OnChat);
                Hook.NetGetData.Deregister(this, GetData);
                TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= PostLogin;
            }
            base.Dispose(disposing);
        }

        public override void Initialize()
        {
            var Hook = ServerApi.Hooks;

            Hook.GameInitialize.Register(this, OnInitialize);
            Hook.NetGreetPlayer.Register(this, OnGreet);
            Hook.ServerLeave.Register(this, OnLeave);
            Hook.GameUpdate.Register(this, OnUpdate);
            Hook.ServerChat.Register(this, OnChat);
            Hook.NetGetData.Register(this, GetData);
            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += PostLogin;

            GetDataHandlers.InitGetDataHandler();
        }

        #region OnInitialize
        public void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("time.check", Check, "check"));

            DatabaseInit();
        }
        #endregion

        #region PostLogin
        public void PostLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs args)
        {
            using (var reader = db.QueryReader("SELECT * FROM Players WHERE Name = @0", args.Player.UserAccountName))
            {
                if (!reader.Read())
                {
                    db.Query("INSERT INTO Players (Name, Time, FirstLogin, LastSeen) VALUES (@0, @1, @2, @3)",
                        args.Player.UserAccountName, 0, DateTime.Now.ToString("G"), "Now");

                    args.Player.SendInfoMessage("Now tracking your playing time (while not afk)");
                }
            }
        }
        #endregion

        #region OnLeave
        public void OnLeave(LeaveEventArgs args)
        {
            TimePl player = GetPlayer(args.Who);
            UpdatePlayer(player);

            PlayerList.RemoveAll(p => p.Index == args.Who);
        }
        #endregion

        #region OnGreet
        public void OnGreet(GreetPlayerEventArgs args)
        {
            PlayerList.Add(new TimePl(args.Who));
        }
        #endregion

        #region GetData
        public void GetData(GetDataEventArgs args)
        {
            PacketTypes type = args.MsgID;
            var player = TShock.Players[args.Msg.whoAmI];

            if (player == null)
            {
                args.Handled = true;
                return;
            }

            if (!player.ConnectionAlive)
            {
                args.Handled = true;
                return;
            }

            using (var data = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
            {
                try
                {
                    if (GetDataHandlers.HandlerGetData(type, player, data))
                        args.Handled = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
        #endregion

        #region Check
        public void Check(CommandArgs args)
        {
            if (args.Parameters.Count > 2)
            {
                args.Player.SendWarningMessage("Invalid: /check <time/afk> [optional(player)]");
            }
            else
            {
                if (args.Parameters.Count == 1)
                {
                    if (args.Parameters[0] == "time")
                        CheckPlayer(args.Player, args.Player.Name);

                    else if (args.Parameters[0] == "afk")
                    {
                        checkAfk(args.Player, args.Player.Name);
                    }
                }

                else if (args.Parameters.Count > 1)
                {
                    string player = args.Parameters[1];

                    string checkType = args.Parameters[0];

                    switch (checkType)
                    {
                        case "time":
                            {
                                CheckPlayer(args.Player, player);
                                break;
                            }
                        case "afk":
                            {
                                if (GetPlayer(player) != null)
                                    checkAfk(args.Player, player);
                                else
                                    args.Player.SendErrorMessage("Invalid player");
                                break;
                            }
                        case "kills":
                            {
                                CheckPlayer(args.Player, player);
                                break;
                            }
                        case "group":
                            {
                                if (TShock.Utils.GetGroup(args.Parameters[1]) != null)
                                    CheckGroupStats(args.Player, args.Parameters[1]);
                                else
                                    args.Player.SendErrorMessage("Invalid group");
                                break;
                            }
                    }
                }
            }
        }
        #endregion

        #region OnUpdate
        public void OnUpdate(EventArgs args)
        {
            DateTime now = DateTime.Now;
            foreach (TimePl p in PlayerList)
            {
                if ((now - p.lastAfkUpdate).TotalSeconds > 5)
                {
                    if (p.TSPlayer.X == p.lastPosX && p.TSPlayer.Y == p.lastPosY)
                    {
                        p.AFKcount++;
                        if (p.AFKcount > 300)
                        {
                            p.AFK = true;
                            p.TSPlayer.SendInfoMessage("You are afk, so this time will not count towards your rank");
                        }
                    }
                    else
                    {
                        p.TimePlayed = p.TimePlayed + 5;
                        if (p.AFK)
                            p.AFK = false;
                    }
                    p.lastAfkUpdate = DateTime.Now;
                    p.lastPosX = p.TSPlayer.TileX;
                    p.lastPosY = p.TSPlayer.TileY;
                }

                if ((now - p.lastTimeUpdate).TotalSeconds > 2)
                {
                    if (!p.AFK && p.TSPlayer.IsLoggedIn)
                    {
                        p.lastTimeUpdate = DateTime.Now;
                        UpdatePoints(p);
                    }
                }
            }
        }
        #endregion

        #region UpdatePlayer
        public void UpdatePlayer(TimePl player)
        {
            int time = 0;
            int kills = 0;
            int deaths = 0;
            int mobkills = 0;
            int bosskills = 0;

            using (var reader = db.QueryReader("SELECT * FROM Players WHERE Name = @0",
                player.TSPlayer.UserAccountName))
            {
                if (reader.Read())
                {
                    time = reader.Get<int>("Time");
                    kills = reader.Get<int>("Kills");
                    deaths = reader.Get<int>("Deaths");
                    mobkills = reader.Get<int>("MobKills");
                    bosskills = reader.Get<int>("BossKills");
                }
            }
            int finK = kills + player.kills;
            int finD = deaths + player.deaths;
            int finMk = mobkills + player.mobkills;
            int finBk = bosskills + player.bosskills;
            int finT = time + player.TimePlayed;

            string lastSeen = DateTime.UtcNow.ToString("G");
            string Query = "UPDATE Players SET Time = @0, LastSeen = @1, Kills = @2," +
                " Deaths = @3, MobKills = @4, BossKills = @5";

            db.Query(Query, finT, lastSeen, finK, finD, finMk, finBk);

            player.TimePlayed = 0;
            player.kills = 0;
            player.deaths = 0;
            player.mobkills = 0;
            player.bosskills = 0;
        }
        #endregion

        #region GetPlayer
        public static TimePl GetPlayer(int index)
        {
            foreach (TimePl player in PlayerList)
            {
                if (player.Index == index)
                {
                    return player;
                }
            }
            return null;
        }
        public static TimePl GetPlayer(string name)
        {
            foreach (TimePl player in PlayerList)
            {
                if (player.Name == name)
                {
                    return player;
                }
            }
            return null;
        }
        #endregion

        #region OnChat
        public void OnChat(ServerChatEventArgs args)
        {
            if (PlayerList[args.Who].AFKcount > 0)
                PlayerList[args.Who].AFKcount = 0;

            if (PlayerList[args.Who].AFK)
                PlayerList[args.Who].AFK = false;
        }
        #endregion

        #region Database
        public void DatabaseInit()
        {
            if (TShock.Config.StorageType.ToLower() == "sqlite")
            {
                db = new SqliteConnection(string.Format("uri=file://{0},Version=3", Path.Combine(TShock.SavePath, "Time.sqlite")));
            }
            else if (TShock.Config.StorageType.ToLower() == "mysql")
            {
                try
                {
                    var host = TShock.Config.MySqlHost.Split(':');
                    db = new MySqlConnection();
                    db.ConnectionString =
                        String.Format("Server={0}; Port={1}; Database={2}; Uid={3}; Pwd={4}",
                        host[0],
                        host.Length == 1 ? "3306" : host[1],
                        TShock.Config.MySqlDbName,
                        TShock.Config.MySqlUsername,
                        TShock.Config.MySqlPassword
                        );
                }
                catch (MySqlException x)
                {
                    Log.Error(x.ToString());
                    throw new Exception("MySQL not setup correctly.");
                }
            }
            else
            {
                throw new Exception("Invalid storage type.");
            }

            SqlTableCreator SQLCreator = new SqlTableCreator(db,
                                             db.GetSqlType() == SqlType.Sqlite
                                             ? (IQueryBuilder)new SqliteQueryCreator()
                                             : new MysqlQueryCreator());

            var table = new SqlTable("Players",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Name", MySqlDbType.String, 255) { Unique = true },
                new SqlColumn("Time", MySqlDbType.Int32),
                new SqlColumn("FirstLogin", MySqlDbType.String),
                new SqlColumn("LastSeen", MySqlDbType.String),
                new SqlColumn("Kills", MySqlDbType.Int32),
                new SqlColumn("Deaths", MySqlDbType.Int32),
                new SqlColumn("MobKills", MySqlDbType.Int32),
                new SqlColumn("BossKills", MySqlDbType.Int32)
                );
            SQLCreator.EnsureExists(table);
        }
        #endregion

        #region CheckPlayerStats
        public void CheckPlayer(TSPlayer ply, string player)
        {
            int time = 0;
            string lastSeen = "";
            string joinedTime = "";
            using (var reader = db.QueryReader("SELECT * FROM Players WHERE Name = @0", player))
            {
                if (reader.Read())
                {
                    time = reader.Get<int>("Time");
                    lastSeen = reader.Get<string>("LastSeen");
                    joinedTime = reader.Get<string>("FirstLogin");

                    double totalTime = time + GetPlayer(player).TimePlayed;

                    double weeks = Math.Floor(totalTime / 604800);
                    double days = Math.Floor(((totalTime / 604800) - weeks) * 7);
                    TimeSpan ts = new TimeSpan(0, 0, 0, (int)totalTime);
                    string newTime = string.Format("{0}weeks, {1}days, {2}hhours, {3}minutes, {4}seconds", weeks, days, ts.Hours, ts.Minutes, ts.Seconds);


                    if (GetPlayer(player) != null)
                    {
                        ply.SendInfoMessage(player + " is online.");
                        ply.SendInfoMessage(player + " has played for " + newTime + " and has " + totalTime + " total rank points");
                    }
                    else
                    {
                        ply.SendInfoMessage(player + " is offline, and was last seen " + lastSeen);
                        ply.SendInfoMessage(player + " has played for " + newTime + " and has " + totalTime + " total rank points");
                    }
                }
                else
                {
                    ply.SendWarningMessage(player + " does not exist in the database. Make sure you are using their account name");
                }
            }
        }
        #endregion

        #region CheckPlayerKillStats
        public void CheckOtherStats(TSPlayer player, string ply)
        {
            int kills;
            int deaths;
            int mobKills;
            int bossKills;

            using (var reader = db.QueryReader("SELECT * FROM Players WHERE Name = @0", ply))
            {
                if (reader.Read())
                {
                    kills = reader.Get<int>("Kills");
                    deaths = reader.Get<int>("Deaths");
                    mobKills = reader.Get<int>("MobKills");
                    bossKills = reader.Get<int>("BossKills");

                    if (GetPlayer(ply) != null)
                    {
                        var foundPly = GetPlayer(ply);
                        int finKills = kills + foundPly.kills;
                        int finDeaths = deaths + foundPly.deaths;
                        int finMobKills = mobKills + foundPly.mobkills;
                        int finBossKills = bossKills + foundPly.bosskills;

                        player.SendInfoMessage(string.Format("{0} has {1} kills, {2} deaths, {3} mob kills " +
                            "and {4} boss kills", foundPly.Name, finKills, finDeaths,
                            finMobKills, finBossKills));
                    }
                    else
                    {
                        player.SendInfoMessage(string.Format("{0} has {1} kills, {2} deaths, {3} mob kills and " +
                            "{4} boss kills", ply, kills, deaths, mobKills, bossKills));
                    }
                }
                else
                {
                    player.SendErrorMessage("Invalid: Player not found");
                }
            }
        }
        #endregion

        #region CheckPlayerAfk
        public void checkAfk(TSPlayer player, string pl)
        {
            if (GetPlayer(pl) != null)
            {
                var newPlayer = GetPlayer(pl);

                player.SendInfoMessage(newPlayer + " has been afk for " + newPlayer.AFKcount + " seconds");
            }
            else
                player.SendErrorMessage("Invalid. Player cannot be found");

        }
        #endregion

        #region CheckGroupStats
        public void CheckGroupStats(TSPlayer player, string groupName)
        {
            double totalTime = 0;
            using (var reader = db.QueryReader("SELECT * FROM Players"))
            {
                while (reader.Read())
                {
                    var ply = reader.Get<string>("Name");
                    User user = TShock.Users.GetUserByName(ply);
                    if (user.Group == groupName)
                    {
                        using (var otherReader = db.QueryReader("SELECT * FROM Players WHERE Name = @0", ply))
                        {
                            totalTime += reader.Get<int>("Time");
                        }
                    }
                }
            }

            double weeks = Math.Floor(totalTime / 604800);
            double days = Math.Floor(((totalTime / 604800) - weeks) * 7);
            TimeSpan ts = new TimeSpan(0, 0, 0, (int)totalTime);
            string newTime = string.Format("{0} weeks, {1} days, {2} hours, {3} minutes, {4} seconds", weeks, days, ts.Hours, ts.Minutes, ts.Seconds);
            player.SendInfoMessage("Group " + groupName + " has played for a total of " + newTime);
        }
        #endregion

        #region UpdatePoints
        public void UpdatePoints(TimePl player)
        {
            using (var reader = db.QueryReader("SELECT * FROM Players WHERE Name = @0", player.TSPlayer.UserAccountName))
            {
                while (reader.Read())
                {
                    int time = reader.Get<int>("Time");
                    player.totalPoints = time + player.TimePlayed;
                }
            }

        }
        #endregion

        public Statistics(Main game)
            : base(game)
        {
            Order = 100;
        }
    }

    #region PlayerClass
    public class TimePl
    {
        public int Index;
        public float lastPosX { get; set; }
        public float lastPosY { get; set; }
        public DateTime lastTimeUpdate = DateTime.Now;
        public DateTime lastAfkUpdate = DateTime.Now;
        public TSPlayer TSPlayer { get { return TShock.Players[Index]; } }
        public int TimePlayed = 0;
        public string Name { get { return Main.player[Index].name; } }
        public bool AFK = false;
        public int AFKcount = 0;
        public int totalPoints { get; set; }
        public int deaths;
        public int kills;
        public int mobkills;
        public int bosskills;

        public TimePl KillingPlayer = null;

        public TimePl(int index)
        {
            Index = index;
            lastPosX = TShock.Players[Index].X;
            lastPosX = TShock.Players[Index].Y;
        }
    }
    #endregion
}
