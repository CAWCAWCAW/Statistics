using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using System.Timers;
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
    public class Timers
    {
        #region Timers
        public static Timer aTimer;
        public static Timer uTimer;

        public static void Start()
        { 
            uTimer = new Timer(300000);
            uTimer.Elapsed += new ElapsedEventHandler(updateTimer);
            uTimer.Enabled = true;

            aTimer = new Timer(5000);
            aTimer.Elapsed += new ElapsedEventHandler(afkTimer);
            aTimer.Enabled = true;
        }

        #region afkTimer
        public static void afkTimer(object sender, ElapsedEventArgs args)
        {
            foreach (StatPl p in Statistics.PlayerList)
            {
                if (p.TSPlayer.X == p.lastPosX && p.TSPlayer.Y == p.lastPosY)
                {
                    p.AFKcount++;
                    if (p.AFKcount > 60)   //60 * 5 = 300 = 5 minutes  (because timer triggers every 5 seconds)
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
        }
        #endregion

        #region updateTimer
        static void updateTimer(object sender, ElapsedEventArgs args)
        {
            lock (Statistics.PlayerList)
            {
                foreach (StatPl p in Statistics.PlayerList)
                {
                    if (!p.AFK && p.TSPlayer.IsLoggedIn)
                    {
                        p.lastTimeUpdate = DateTime.Now;
                        Statistics.UpdatePlayer(p);
                    }
                }
            }
        }
        #endregion
        #endregion
    }

    [ApiVersion(1, 14)]
    public class Statistics : TerrariaPlugin
    {
        public static List<StatPl> PlayerList = new List<StatPl>();

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
                //Hook.GameUpdate.Deregister(this, OnUpdate);
                Hook.ServerLeave.Deregister(this, OnLeave);
                Hook.GamePostInitialize.Register(this, StartTimers);
                Hook.ServerChat.Deregister(this, OnChat);
                //Hook.NetGetData.Deregister(this, GetData);
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
            Hook.GamePostInitialize.Register(this, StartTimers);
            //Hook.GameUpdate.Register(this, OnUpdate);
            Hook.ServerChat.Register(this, OnChat);
            //Hook.NetGetData.Register(this, GetData);
            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += PostLogin;

            GetDataHandlers.InitGetDataHandler();
        }

        //Setup and intialization
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
            using (var reader = db.QueryReader("SELECT * FROM Stats WHERE Name = @0", args.Player.UserAccountName))
            {
                if (!reader.Read())
                {
                    db.Query("INSERT INTO Stats (Name, Time, FirstLogin, LastSeen) VALUES (@0, @1, @2, @3)",
                        args.Player.UserAccountName, 0, DateTime.Now.ToString("G"), "Now");

                    args.Player.SendInfoMessage("Now tracking your playing time (while not afk)");
                }
            }
        }
        #endregion

        public void StartTimers(EventArgs args)
        {
            Timers.Start();
        }

        #region OnLeave
        public void OnLeave(LeaveEventArgs args)
        {
            StatPl player = GetPlayer(args.Who);
            if (player != null)
            {
                UpdatePlayer(player);
            }

            PlayerList.RemoveAll(p => p.Index == args.Who);
        }
        #endregion

        #region OnGreet
        public void OnGreet(GreetPlayerEventArgs args)
        {
            PlayerList.Add(new StatPl(args.Who));
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
                    if (DataHandler.GetDataHandlers.HandlerGetData(type, player, data))
                        args.Handled = true;
                }
                catch (Exception ex)
                {
                    Log.Error(ex.ToString());
                }
            }
        }
        #endregion

        #region Database
        public void DatabaseInit()
        {
            if (TShock.Config.StorageType.ToLower() == "sqlite")
            {
                db = new SqliteConnection(string.Format("uri=file://{0},Version=3", Path.Combine(TShock.SavePath, "Statistics.sqlite")));
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

            var table = new SqlTable("Stats",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("Name", MySqlDbType.String, 255) { Unique = true },
                new SqlColumn("Time", MySqlDbType.Int32),
                new SqlColumn("FirstLogin", MySqlDbType.String, 255),
                new SqlColumn("LastSeen", MySqlDbType.String, 255),
                new SqlColumn("Kills", MySqlDbType.Int32),
                new SqlColumn("Deaths", MySqlDbType.Int32),
                new SqlColumn("MobKills", MySqlDbType.Int32),
                new SqlColumn("BossKills", MySqlDbType.Int32)
                );
            SQLCreator.EnsureExists(table);

            var graphTable = new SqlTable("Graphs",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("PointX", MySqlDbType.Int32),
                new SqlColumn("PointY", MySqlDbType.Int32),
                new SqlColumn("Type", MySqlDbType.String)
                );
            SQLCreator.EnsureExists(graphTable);
        }
        #endregion

        //Command
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
                    else if (args.Parameters[0] == "kills")
                    {
                        CheckOtherStats(args.Player, args.Player.Name);
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
                                CheckOtherStats(args.Player, player);
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

        //Gone
        #region OnUpdate
        //public void OnUpdate(EventArgs args)
        //{
        //    DateTime now = DateTime.Now;
        //    foreach (StatPl p in PlayerList)
        //    {
        //        if ((now - p.lastAfkUpdate).TotalSeconds > 5)
        //        {
        //            if (p.TSPlayer.X == p.lastPosX && p.TSPlayer.Y == p.lastPosY)
        //            {
        //                p.AFKcount++;
        //                if (p.AFKcount > 300)
        //                {
        //                    p.AFK = true;
        //                    p.TSPlayer.SendInfoMessage("You are afk, so this time will not count towards your rank");
        //                }
        //            }
        //            else
        //            {
        //                p.TimePlayed = p.TimePlayed + 5;
        //                if (p.AFK)
        //                    p.AFK = false;
        //            }
        //            p.lastAfkUpdate = DateTime.Now;
        //            p.lastPosX = p.TSPlayer.TileX;
        //            p.lastPosY = p.TSPlayer.TileY;
        //        }

        //        if ((now - p.lastTimeUpdate).TotalSeconds > 2)
        //        {
        //            if (!p.AFK && p.TSPlayer.IsLoggedIn)
        //            {
        //                p.lastTimeUpdate = DateTime.Now;
        //                UpdateTime(p);
        //            }
        //        }
        //    }
        //}
        #endregion
        //Gone

        #region OnChat
        public void OnChat(ServerChatEventArgs args)
        {
            if (PlayerList[args.Who] != null)
            {
                if (PlayerList[args.Who].AFKcount > 0)
                    PlayerList[args.Who].AFKcount = 0;

                if (PlayerList[args.Who].AFK)
                    PlayerList[args.Who].AFK = false;
            }
        }
        #endregion


        //Player and statistics functions
        #region UpdatePlayer
        public static void UpdatePlayer(StatPl player)
        {
            try
            {
                var lastSeen = DateTime.UtcNow.ToString("G");
                db.Query("UPDATE Stats SET Kills = Kills + @0, Deaths = Deaths + @1, MobKills = MobKills + @2, "
                    + "BossKills = BossKills + @3, LastSeen = @4 WHERE Name = @5", player.kills, player.deaths, player.mobkills,
                    player.bosskills, lastSeen, player.TSPlayer.UserAccountName);
            }
            catch (Exception x)
            {
                Log.ConsoleError(x.ToString());
            }
        }
        #endregion

        #region GetPlayer
        public static StatPl GetPlayer(int index)
        {
            foreach (StatPl player in PlayerList)
            {
                if (player.Index == index)
                {
                    return player;
                }
            }
            return null;
        }
        public static StatPl GetPlayer(string name)
        {
            foreach (StatPl player in PlayerList)
            {
                if (player.Name == name)
                {
                    return player;
                }
            }
            return null;
        }
        #endregion

        #region CheckPlayerStats
        public void CheckPlayer(TSPlayer ply, string player)
        {
            int time = 0;
            string lastSeen = "";
            string joinedTime = "";
            using (var reader = db.QueryReader("SELECT * FROM Stats WHERE Name = @0", player))
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

            using (var reader = db.QueryReader("SELECT * FROM Stats WHERE Name = @0", ply))
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

        #region UpdateTime
        public void UpdateTime(StatPl player)
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
}
