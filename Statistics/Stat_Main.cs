using System;
using System.IO;
using System.Data;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading;
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
        public override string Author
        { get { return "WhiteX"; } }

        public override string Description
        { get { return "Statistics for players"; } }

        public override string Name
        { get { return "Statistics"; } }

        public override Version Version
        { get { return Assembly.GetExecutingAssembly().GetName().Version; } }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                try
                {
                    Thread dispose = new Thread(new ThreadStart(disposeThread));

                    dispose.Start();
                    dispose.Join();
                }
                catch (Exception x)
                {
                    Log.ConsoleError(x.ToString());
                }
                ServerApi.Hooks.GameInitialize.Deregister(this, OnInitialize);
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreet);
                ServerApi.Hooks.ServerLeave.Deregister(this, OnLeave);
                ServerApi.Hooks.GamePostInitialize.Register(this, sTools.postInitialize);
                ServerApi.Hooks.ServerChat.Deregister(this, OnChat);
                ServerApi.Hooks.NetGetData.Deregister(this, GetData);
                TShockAPI.Hooks.PlayerHooks.PlayerPostLogin -= PostLogin;

            }
            base.Dispose(disposing);
        }

      public static void disposeThread()
      {
          sTools.saveDatabase();
          Thread.Sleep(600);
      }

        public override void Initialize()
        {
            ServerApi.Hooks.GameInitialize.Register(this, OnInitialize);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreet);
            ServerApi.Hooks.ServerLeave.Register(this, OnLeave);
            ServerApi.Hooks.GamePostInitialize.Register(this, sTools.postInitialize);
            ServerApi.Hooks.ServerChat.Register(this, OnChat);
            ServerApi.Hooks.NetGetData.Register(this, GetData);
            TShockAPI.Hooks.PlayerHooks.PlayerPostLogin += PostLogin;

            GetDataHandlers.InitGetDataHandler();
        }

        #region OnInitialize
        public void OnInitialize(EventArgs args)
        {
            Commands.ChatCommands.Add(new Command("stats.check", stat_Check, "check")
            {
                HelpText = "Base command for the Statistics plugin. Allows you to view statistics about players"
            });

            Commands.ChatCommands.Add(new Command("stats.uix", sCommands.UI_Extended, "uix")
            {
                HelpText = "Extended user information"
            });

            Commands.ChatCommands.Add(new Command("stats.uic", sCommands.UI_Character, "uic")
            {
                HelpText = "Provides information about a player's character"
            });

            sTools.registerSubs();
            /*Commands.ChatCommands.Add(new Command("graph.set", _Graph.graphCommand, "graph") { AllowServer = false });
            Commands.ChatCommands.Add(new Command("graph.add", _Graph.createGraph, "cgraph") { AllowServer = false });*/

            sTools.DatabaseInit();
        }
        #endregion

        #region PostLogin
        public void PostLogin(TShockAPI.Hooks.PlayerPostLoginEventArgs args)
        {
            if (sTools.GetPlayer(args.Player.Index) != null)
            {
                sPlayer player = sTools.GetPlayer(args.Player.Index);

                if (sTools.GetstoredPlayer(args.Player.UserAccountName).Count == 1)
                {
                    storedPlayer storedplayer = sTools.GetstoredPlayer(args.Player.UserAccountName)[0];

                    if (storedplayer.knownIPs.Length > 0)
                    {
                        if (!storedplayer.knownIPs.Contains(args.Player.IP))
                            storedplayer.knownIPs += ", " + args.Player.IP;
                    }
                    else
                        storedplayer.knownIPs = args.Player.IP;

                    if (storedplayer.knownAccounts.Length > 0)
                    {
                        if (!storedplayer.knownAccounts.Contains(args.Player.UserAccountName))
                            storedplayer.knownAccounts += ", " + args.Player.UserAccountName;
                    }
                    else
                        storedplayer.knownAccounts = args.Player.UserAccountName;

                    sTools.populatePlayerStats(player, storedplayer);
                    Log.ConsoleInfo("Successfully linked account {0} with stored player {1}",
                        args.Player.UserAccountName, storedplayer.name);
                    return;
                }
                else if (sTools.GetstoredPlayer(args.Player.UserAccountName).Count > 1)
                {
                    Log.ConsoleError("Multiple match error! --Attempting to obtain stored player for {0} resulted in" +
                        " {1} matches: {2}", args.Player.UserAccountName, sTools.GetstoredPlayer(args.Player.UserAccountName).Count,
                        sTools.GetstoredPlayer(args.Player.UserAccountName).Select(p => p.name));
                }
                else
                {
                    Log.ConsoleInfo("New stored player named {0} has been added", args.Player.UserAccountName);
                    sTools.storedPlayers.Add(new storedPlayer(args.Player.UserAccountName, DateTime.Now.ToString("G"), DateTime.Now.ToString("G"),
                        0, 1, args.Player.UserAccountName, args.Player.IP, 0, 0, 0, 0));

                    sTools.db.Query("INSERT INTO Stats (Name, FirstLogin, LastSeen, Time, LoginCount, KnownAccounts, KnownIPs, Kills, Deaths, MobKills, BossKills)" +
                        " VALUES (@0, @1, @2, @3, @4, @5, @6, @7, @8, @9, @10)", args.Player.UserAccountName, DateTime.Now.ToString("G"),
                        DateTime.Now.ToString("G"), 0, 1, args.Player.UserAccountName, args.Player.IP, 0, 0, 0, 0);

                    sTools.populatePlayerStats(player, sTools.storedPlayers[sTools.storedPlayers.Count - 1]);
                    Log.ConsoleInfo("Successfully linked account {0} with stored player {1}",
                         args.Player.UserAccountName, sTools.storedPlayers[sTools.storedPlayers.Count - 1].name);
                }
            }
            else
            {
                sTools.splayers.Add(new sPlayer(args.Player.Index));
                PostLogin(new TShockAPI.Hooks.PlayerPostLoginEventArgs(args.Player));

                /*Log.ConsoleError("Error encountered while checking '" + args.Player.Name + "'");
                Log.ConsoleError("Unable to find a stat player with index '" + args.Player.Index + "'");
                Log.ConsoleError("Player at statistic index " + args.Player.Index + " is " + sTools.splayers[args.Player.Index]);
                */
           }
        }
        #endregion

        #region OnLeave
        public void OnLeave(LeaveEventArgs args)
        {
            if (sTools.GetPlayer(args.Who) != null)
            {
                sTools.UpdatePlayer(sTools.GetPlayer(args.Who));

                sTools.splayers.RemoveAll(p => p.Index == args.Who);
            }
        }
        #endregion

        #region OnGreet
        public void OnGreet(GreetPlayerEventArgs args)
        {
            if (sTools.GetPlayer(args.Who) == null)
            {
                sTools.splayers.Add(new sPlayer(args.Who));

                if (!TShock.Config.DisableUUIDLogin)
                    if (TShock.Players[args.Who].IsLoggedIn)
                        PostLogin(new TShockAPI.Hooks.PlayerPostLoginEventArgs(TShock.Players[args.Who]));
            }
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
                    Log.ConsoleError(ex.ToString());
                }
            }
        }
        #endregion

        #region OnChat
        public void OnChat(ServerChatEventArgs args)
        {
            try
            {
                var player = sTools.GetPlayer(args.Who);
                if (player != null)
                {
                    if (!args.Text.StartsWith("/check"))
                    {
                        if (player.AFKcount > 0)
                            player.AFKcount = 0;

                        if (player.AFK)
                        {
                            player.AFK = false;
                        }
                    }
                }
            }
            catch { }
        }
        #endregion

        public Statistics(Main game)
            : base(game)
        {
            Order = 100;
        }

        public void stat_Check(CommandArgs args)
        {
            sTools.handler.RunSubcommand(args);
        }
    }
}
