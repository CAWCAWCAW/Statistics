﻿using System;
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
    public class sTools
    {
        public static IDbConnection db;
        public static List<sPlayer> splayers = new List<sPlayer>();
        public static List<storedPlayer> storedPlayers = new List<storedPlayer>();

        public static subCommandHandler handler = new subCommandHandler();

        #region Database
        public static void DatabaseInit()
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
                new SqlColumn("Name", MySqlDbType.VarChar, 50) { Unique = true },
                new SqlColumn("Time", MySqlDbType.Int32),
                new SqlColumn("FirstLogin", MySqlDbType.Text),
                new SqlColumn("LastSeen", MySqlDbType.Text),
                new SqlColumn("Kills", MySqlDbType.Int32),
                new SqlColumn("Deaths", MySqlDbType.Int32),
                new SqlColumn("MobKills", MySqlDbType.Int32),
                new SqlColumn("BossKills", MySqlDbType.Int32),
                new SqlColumn("KnownAccounts", MySqlDbType.Text),
                new SqlColumn("KnownIPs", MySqlDbType.Text),
                new SqlColumn("LoginCount", MySqlDbType.Int32)
                );
            SQLCreator.EnsureExists(table);


            /*
            var graphTable = new SqlTable("Graphs",
                new SqlColumn("ID", MySqlDbType.Int32) { Primary = true, AutoIncrement = true },
                new SqlColumn("PointX", MySqlDbType.Int32),
                new SqlColumn("PointY", MySqlDbType.Int32),
                new SqlColumn("Type", MySqlDbType.String),
                new SqlColumn("TopRightPoint", MySqlDbType.Int32)
                );
            SQLCreator.EnsureExists(graphTable);

            var graphData = new SqlTable("GraphData",
                new SqlColumn("Monday", MySqlDbType.Int32),
                new SqlColumn("Tuesday", MySqlDbType.Int32),
                new SqlColumn("Wednesday", MySqlDbType.Int32),
                new SqlColumn("Thursday", MySqlDbType.Int32),
                new SqlColumn("Friday", MySqlDbType.Int32),
                new SqlColumn("Saturday", MySqlDbType.Int32),
                new SqlColumn("Sunday", MySqlDbType.Int32)
            );
            SQLCreator.EnsureExists(graphData);
            */
        }
        #endregion

        public static void postInitialize(EventArgs args)
        {
            int count = 0;
            using (var reader = db.QueryReader("SELECT * FROM Stats"))
            {
                while (reader.Read())
                {
                    string name = reader.Get<string>("Name");
                    int totalTime = reader.Get<int>("Time");
                    string firstLogin = reader.Get<string>("FirstLogin");
                    string lastSeen = reader.Get<string>("LastSeen");

                    string knownAccounts = reader.Get<string>("KnownAccounts");
                    string knownIPs = reader.Get<string>("KnownIPs");
                    int loginCount = reader.Get<int>("LoginCount");

                    int kills = reader.Get<int>("Kills");
                    int deaths = reader.Get<int>("Deaths");
                    int mobKills = reader.Get<int>("MobKills");
                    int bossKills = reader.Get<int>("BossKills");

                    storedPlayers.Add(new storedPlayer(name, firstLogin, lastSeen, totalTime, loginCount, knownAccounts, knownIPs,
                        kills, deaths, mobKills, bossKills));
                    count++;
                }
            }
            Console.WriteLine("Populated {0} stored player{1}", count, suffix(count));

            Stat_Timers.Start(args);
        }


        #region Find player methods
        /// <summary>
        /// Returns an sPlayer through index matching
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static sPlayer GetPlayer(int index)
        {
            foreach (sPlayer player in sTools.splayers)
                if (player.Index == index)
                    return player;

            return null;
        }
        /// <summary>
        /// Returns an sPlayer through UserAccountName matching
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static List<sPlayer> GetPlayer(string name)
        {
            var matches = new List<sPlayer>();
            foreach (sPlayer player in sTools.splayers)
                if (player.TSPlayer.IsLoggedIn)
                {
                    if (player.TSPlayer.UserAccountName.ToLower().Contains(name.ToLower()) && !matches.Contains(player))
                        matches.Add(player);
                    if (player.TSPlayer.UserAccountName.ToLower() == name.ToLower())
                        return new List<sPlayer> { player };
                }

            return matches;
        }

        public static List<sPlayer> GetPlayerByIP(string IP)
        {
            var matches = new List<sPlayer>();
            foreach (sPlayer player in sTools.splayers)
            {
                if (player.TSPlayer.IP == IP)
                    return new List<sPlayer> { player };
                if (player.TSPlayer.IP.Contains(IP))
                    matches.Add(player);
            }

            return matches;
        }

        public static List<storedPlayer> GetstoredPlayerByIP(string IP)
        {
            var matches = new List<storedPlayer>();
            foreach (storedPlayer storedplayer in sTools.storedPlayers)
            {
                if (storedplayer.knownIPs.Contains(IP))
                    matches.Add(storedplayer);
                if (storedplayer.knownIPs == IP)
                    return new List<storedPlayer> { storedplayer };
            }

            return matches;
        }

        public static List<storedPlayer> GetstoredPlayer(string name)
        {
            var matches = new List<storedPlayer>();
            foreach (storedPlayer storedplayer in storedPlayers)
            {
                if (storedplayer.name.ToLower() == name.ToLower())
                    return new List<storedPlayer> { storedplayer };
                if (storedplayer.name.ToLower().Contains(name.ToLower()) && !matches.Contains(storedplayer))
                    matches.Add(storedplayer);
            }

            return matches;
        }

        public static storedPlayer GetstoredPlayer(string AccountName, string AccountIP)
        {
            foreach (storedPlayer storedplayer in storedPlayers)
                if (storedplayer.knownAccounts.Contains(AccountName) && storedplayer.knownAccounts.Contains(AccountIP))
                    return storedplayer;

            return null;
        }

        #endregion

        /// <summary>
        /// Fills out a player's stats
        /// </summary>
        /// <param name="player"></param>
        /// <param name="storedplayer"></param>
        public static void populatePlayerStats(sPlayer player, storedPlayer storedplayer)
        {
            if (storedplayer != null && player != null)
            {
                player.TimePlayed = storedplayer.totalTime;
                player.firstLogin = storedplayer.firstLogin;
                player.lastSeen = DateTime.UtcNow.ToString("G");
                player.loginCount = storedplayer.loginCount + 1;
                player.knownAccounts = storedplayer.knownAccounts;
                player.knownIPs = storedplayer.knownIPs;

                player.kills = storedplayer.kills;
                player.deaths = storedplayer.deaths;
                player.mobkills = storedplayer.mobkills;
                player.bosskills = storedplayer.bosskills;
            }
        }

        /// <summary>
        /// Updates a stored player's stats
        /// </summary>
        /// <param name="player"></param>
        /// <param name="storedplayer"></param>
        public static void populateStoredStats(sPlayer player, storedPlayer storedplayer)
        {
            if (player != null && storedplayer != null)
            {
                storedplayer.totalTime = player.TimePlayed;
                storedplayer.firstLogin = player.firstLogin;
                storedplayer.lastSeen = DateTime.Now.ToString("G");
                storedplayer.loginCount = player.loginCount;
                storedplayer.knownAccounts = player.knownAccounts;
                storedplayer.knownIPs = player.knownIPs;

                storedplayer.kills = player.kills;
                storedplayer.deaths = player.deaths;
                storedplayer.mobkills = player.mobkills;
                storedplayer.bosskills = player.bosskills;
            }
        }

        /// <summary>
        /// Updates a player
        /// </summary>
        /// <param name="player"></param>
        public static void UpdatePlayer(sPlayer player)
        {
            try
            {
                populateStoredStats(player, GetstoredPlayer(player.TSPlayer.UserAccountName)[0]);
            }
            catch (Exception x)
            {
                Log.ConsoleError(x.ToString());
            }
        }

        public static void registerSubs()
        {
            handler.RegisterSubcommand("time", sCommands.check_Time, "stats.time", "stats.*");
            handler.RegisterSubcommand("afk", sCommands.check_Afk, "stats.afk", "stats.*");
            handler.RegisterSubcommand("kills", sCommands.check_Kills, "stats.kills", "stats.*");
            handler.RegisterSubcommand("name", sCommands.check_Name, "stats.name", "stats.*");

            handler.HelpText = "Valid subcommands of /check:|[time \\ afk \\ kills]|Syntax: /check [option] [playerName \\ self]";
        }

        public static void saveDatabase()
        {
            foreach (sPlayer player in splayers)
                if (player.TSPlayer.IsLoggedIn)
                    populateStoredStats(player, GetstoredPlayer(player.TSPlayer.UserAccountName)[0]);

            foreach (storedPlayer storedplayer in storedPlayers)
            {
                db.Query("UPDATE Stats SET Time = @0, LastSeen = @1, Kills = @2, Deaths = @3, MobKills = @4, " +
                    "BossKills = @5, LoginCount = @6 WHERE Name = @7",
                    storedplayer.totalTime, DateTime.Now.ToString("G"), storedplayer.kills, storedplayer.deaths,
                    storedplayer.mobkills, storedplayer.bosskills, storedplayer.loginCount, storedplayer.name);
            }
            Log.ConsoleInfo("Database save complete");
        }
        

        public static string suffix(int number)
        {
            return number > 1 || number == 0 ? "s" : "";
        }

        public static string timePlayed(int number)
        {
            double totalTime = (double)number;

            double weeks = Math.Floor(totalTime / 604800);
            double days = Math.Floor(((totalTime / 604800) - weeks) * 7);

            TimeSpan ts = new TimeSpan(0, 0, 0, (int)totalTime);

            return string.Format("{0} week{5} {1} day{6} {2} hour{7} {3} minute{8} {4} second{9}",
            weeks, days, ts.Hours, ts.Minutes, ts.Seconds, suffix((int)weeks), suffix((int)days), suffix(ts.Hours), 
            suffix(ts.Minutes), suffix(ts.Seconds));



            /*  Broken format
             return string.Format("{0}{5}{1}{6}{2}{7}{3}{8}{10}{4}{9}",
                weeks > 0 ? weeks.ToString() + " week" : "",
                ts.Days > 0 ? ts.Days.ToString() + " day" : "",
                ts.Hours > 0 ? ts.Hours.ToString() + " hour" : "",
                ts.Minutes > 0 ? ts.Minutes.ToString() + " minute" : "",
                ts.Seconds > 0 ? " " + ts.Seconds.ToString() + " second" : "",

                weeks > 1 ? "s " : "",
                ts.Days > 1 ? "s " : (ts.Days == 0 || ts.Days == 1) && weeks != 0 ? " " : "",
                ts.Hours > 1 ? "s " : (ts.Hours == 0 || ts.Hours == 1) && (ts.Days != 0 || weeks != 0) ? " " : "",
                ts.Minutes > 1 ? "s " : (ts.Minutes == 0 || ts.Minutes == 1) && (ts.Hours != 0 || ts.Days != 0 || weeks != 0 || ts.Seconds > 0) ? " " : "",
                ts.Seconds > 1 ? "s " : (ts.Seconds == 0 || ts.Seconds == 1) && (ts.Minutes != 0 || ts.Hours != 0 || ts.Days != 0 || weeks != 0) ? " " : "",
                ts.Seconds > 0 && (weeks != 0 || ts.Days != 0 || ts.Minutes != 0) ? "and" : "").Trim();*/
        }

        public static string timeSpanPlayed(TimeSpan ts)
        {
            return string.Format("{0}{4}{1}{5}{2}{6}{8}{3}{7}",
                ts.Days > 0 ? ts.Days.ToString() + " day" : "",
                ts.Hours > 0 ? ts.Hours.ToString() + " hour" : "",
                ts.Minutes > 0 ? ts.Minutes.ToString() + " minute" : "",
                ts.Seconds > 0 ? " " + ts.Seconds.ToString() + " second" : "",

                ts.Days > 1 ? "s " : (ts.Days == 0 || ts.Days == 1) ? " " : "",
                ts.Hours > 1 ? "s " : (ts.Hours == 0 || ts.Hours == 1) && (ts.Days != 0) ? " " : "",
                ts.Minutes > 1 ? "s " : (ts.Minutes == 0 || ts.Minutes == 1) && (ts.Hours != 0 || ts.Days != 0 || ts.Seconds > 0) ? " " : "",
                ts.Seconds > 1 ? "s " : (ts.Seconds == 0 || ts.Seconds == 1) && (ts.Minutes != 0 || ts.Hours != 0 || ts.Days != 0) ? " " : "",
                ts.Seconds > 0 && (ts.Days != 0 || ts.Minutes != 0) ? "and" : "").Trim();
        }
    }

    public class subCommandHandler
    {
        private List<subCommand> subCommands = new List<subCommand>();

        public string HelpText;

        public subCommandHandler()
        {
            RegisterSubcommand("help", DisplayHelpText);
        }

        private void DisplayHelpText(CommandArgs args)
        {
            foreach (string item in HelpText.Split('|'))
                args.Player.SendInfoMessage(item);
        }

        public void RegisterSubcommand(string command, Action<CommandArgs> func, params string[] permissions)
        {
            subCommands.Add(new subCommand(command, func, permissions));
        }
        public void RegisterSubcommand(string command, Action<CommandArgs> func, string permission)
        {
            subCommands.Add(new subCommand(command, func, permission));
        }

        public void RunSubcommand(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                CommandArgs newargs = new CommandArgs(args.Message, args.Player, args.Parameters.GetRange(1, args.Parameters.Count - 1));
                try
                {
                    int count = 0;
                    foreach (string perm in subCommands.Find(command => command.name == args.Parameters[0]).permissions)
                        if (!args.Player.Group.HasPermission(perm))
                        {
                            count++;
                        }
                    if (count == subCommands.Find(command => command.name == args.Parameters[0]).permissions.Count)
                        args.Player.SendErrorMessage("You do not have permission to use that command");
                    else
                        subCommands.Find(command => command.name == args.Parameters[0]).func.Invoke(args);
                }
                catch (Exception e)
                {
                    args.Player.SendErrorMessage("Command failed.");
                    Log.Error(e.Message);
                    subCommands.Find(command => command.name == "help").func.Invoke(newargs);
                }
            }
            else
                subCommands.Find(command => command.name == "help").func.Invoke(args);
        }
    }

    public class subCommand
    {
        public List<string> permissions;
        public string name;
        public Action<CommandArgs> func;

        public subCommand(string name, Action<CommandArgs> func, params string[] permissions)
        {
            this.permissions = new List<string>(permissions);
            this.name = name;
            this.func = func;
        }
        public subCommand(string name, Action<CommandArgs> func, string permission)
        {
            this.permissions.Add(permission);
            this.name = name;
            this.func = func;
        }
        public subCommand(string name, Action<CommandArgs> func)
        {
            this.name = name;
            this.func = func;
        }
    }
}
