using System;
using System.IO;
using System.Net;
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

namespace Statistics
{
    public class sCommands
    {
        /* Yama's suggestions */
        
        #region UI_Extended
        public static void UI_Extended(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                if (args.Parameters[0] == "self")
                {
                    if (sTools.GetPlayer(args.Player.Index) != null)
                    {
                        sPlayer player = sTools.GetPlayer(args.Player.Index);

                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;
                        else
                        {
                            var uixInfo = new List<string>();
                            var time_1 = DateTime.Now.Subtract(DateTime.Parse(player.firstLogin));

                            uixInfo.Add(string.Format("UIX info for {0}", player.Name));

                            uixInfo.Add(string.Format("{0} is a member of group {1}", player.Name, player.TSPlayer.Group.Name));

                            uixInfo.Add(string.Format("First login: {0} ({1}ago)",
                                player.firstLogin, sTools.timeSpanPlayed(time_1)));

                            uixInfo.Add("Last seen: Now");

                            uixInfo.Add(string.Format("Logged in {0} times since registering", player.loginCount));
                            try
                            {
                                uixInfo.Add(string.Format("Known accounts: {0}", player.knownAccounts));
                            }
                            catch { uixInfo.Add("No known accounts found"); }
                            try
                            {
                                uixInfo.Add(string.Format("Known IPs: {0}", player.knownIPs));
                            }
                            catch { uixInfo.Add("No known IPs found"); }

                            PaginationTools.SendPage(args.Player, pageNumber, uixInfo, new PaginationTools.Settings
                            {
                                HeaderFormat = "Extended User Information [Page {0} of {1}]",
                                HeaderTextColor = Color.Lime,
                                LineTextColor = Color.White,
                                FooterFormat = string.Format("/uix {0} {1} for more", args.Parameters[0], pageNumber + 1),
                                FooterTextColor = Color.Lime
                            });
                        }
                    }
                    else
                    {
                        args.Player.SendErrorMessage("Something broke. Please try again later");
                    }
                }
                else
                {
                    string name = "";
                    if (args.Parameters.Count > 1)
                        name = string.Join(" ", args.Parameters).Substring(0, string.Join(" ", args.Parameters).Length - 2);
                    else
                        name = string.Join(" ", args.Parameters).Substring(0, string.Join(" ", args.Parameters).Length);

                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, string.Join(" ", args.Parameters).Length - 1,
                        args.Player, out pageNumber))
                        return;

                    IPAddress IP;
                    if (IPAddress.TryParse(name, out IP))
                    {
                        if (sTools.GetPlayerByIP(IP.ToString()).Count == 1)
                        {
                            sPlayer player = sTools.GetPlayerByIP(IP.ToString())[0];

                            var uixInfo = new List<string>();
                            var time_1 = DateTime.Now.Subtract(DateTime.Parse(player.firstLogin));

                            uixInfo.Add(string.Format("UIX info for {0}", player.Name));
                            uixInfo.Add(string.Format("{0} is a member of group {1}", player.Name, player.TSPlayer.Group.Name));
                            uixInfo.Add(string.Format("First login: {0} ({1} ago)",
                                player.firstLogin, sTools.timeSpanPlayed(time_1)));

                            uixInfo.Add("Last seen: Now");

                            uixInfo.Add(string.Format("Logged in {0} times since registering", player.loginCount));
                            try
                            {
                                uixInfo.Add(string.Format("Known accounts: {0}", string.Join(", ", player.knownAccounts.Split(','))));
                            }
                            catch { uixInfo.Add("No known accounts found"); }
                            try
                            {
                                uixInfo.Add(string.Format("Known IPs: {0}", string.Join(", ", player.knownIPs.Split(','))));
                            }
                            catch { uixInfo.Add("No known IPs found"); }

                            PaginationTools.SendPage(args.Player, pageNumber, uixInfo, new PaginationTools.Settings
                            {
                                HeaderFormat = "Extended User Information [Page {0} of {1}]",
                                HeaderTextColor = Color.Lime,
                                LineTextColor = Color.White,
                                FooterFormat = string.Format("/uix {0} {1} for more", player.Name, pageNumber + 1),
                                FooterTextColor = Color.Lime
                            });

                        }
                        else if (sTools.GetPlayerByIP(IP.ToString()).Count > 1)
                            TShock.Utils.SendMultipleMatchError(args.Player,
                                sTools.GetPlayerByIP(IP.ToString()).Select(p => p.Name));
                        else
                            if (sTools.GetstoredPlayerByIP(IP.ToString()).Count == 1)
                            {
                                storedPlayer storedplayer = sTools.GetstoredPlayerByIP(IP.ToString())[0];
                                var uixInfo = new List<string>();
                                var time_1 = DateTime.Now.Subtract(DateTime.Parse(storedplayer.firstLogin));

                                uixInfo.Add(string.Format("UIX info for {0}", storedplayer.name));
                                uixInfo.Add(string.Format("{0} is a member of group {1}", storedplayer.name, TShock.Users.GetUserByName(storedplayer.name).Group));
                                uixInfo.Add(string.Format("First login: {0} ({1} ago)",
                                    storedplayer.firstLogin, sTools.timeSpanPlayed(time_1)));

                                uixInfo.Add(string.Format("Last seen: {0} ({1} ago).", storedplayer.lastSeen,
                                    sTools.timeSpanPlayed(DateTime.Now.Subtract(DateTime.Parse(storedplayer.lastSeen)))));

                                uixInfo.Add(string.Format("Overall play time: {0}", sTools.timePlayed(storedplayer.totalTime)));

                                uixInfo.Add(string.Format("Logged in {0} times since registering", storedplayer.loginCount));
                                try
                                {
                                    uixInfo.Add(string.Format("Known accounts: {0}", string.Join(", ", storedplayer.knownAccounts.Split(','))));
                                }
                                catch { uixInfo.Add("No known accounts found"); }
                                try
                                {
                                    uixInfo.Add(string.Format("Known IPs: {0}", string.Join(", ", storedplayer.knownIPs.Split(','))));
                                }
                                catch { uixInfo.Add("No known IPs found"); }

                                PaginationTools.SendPage(args.Player, pageNumber, uixInfo, new PaginationTools.Settings
                                {
                                    HeaderFormat = "Extended User Information [Page {0} of {1}]",
                                    HeaderTextColor = Color.Lime,
                                    LineTextColor = Color.White,
                                    FooterFormat = string.Format("/uix {0} {1} for more", storedplayer.name, pageNumber + 1),
                                    FooterTextColor = Color.Lime
                                });
                            }

                            else if (sTools.GetstoredPlayerByIP(IP.ToString()).Count > 1)
                                TShock.Utils.SendMultipleMatchError(args.Player,
                                    sTools.GetstoredPlayerByIP(IP.ToString()).Select(p => p.name));

                            else
                                args.Player.SendErrorMessage("Invalid IP address. Try /check ip {0} to make sure you're using the right IP address",
                            name);
                    }

                    else
                    {
                        if (sTools.GetPlayer(name).Count == 1)
                        {
                            sPlayer player = sTools.GetPlayer(name)[0];

                            var uixInfo = new List<string>();
                            var time_1 = DateTime.Now.Subtract(DateTime.Parse(player.firstLogin));

                            uixInfo.Add(string.Format("UIX info for {0}", player.Name));
                            uixInfo.Add(string.Format("{0} is a member of group {1}", player.Name, player.TSPlayer.Group.Name));

                            uixInfo.Add(string.Format("First login: {0} ({1} ago)",
                                player.firstLogin, sTools.timeSpanPlayed(time_1)));

                            uixInfo.Add("Last seen: Now");

                            uixInfo.Add(string.Format("Logged in {0} times since registering", player.loginCount));
                            try
                            {
                                uixInfo.Add(string.Format("Known accounts: {0}", string.Join(", ", player.knownAccounts.Split(','))));
                            }
                            catch { uixInfo.Add("No known accounts found"); }
                            try
                            {
                                uixInfo.Add(string.Format("Known IPs: {0}", string.Join(", ", player.knownIPs.Split(','))));
                            }
                            catch { uixInfo.Add("No known IPs found"); }

                            PaginationTools.SendPage(args.Player, pageNumber, uixInfo, new PaginationTools.Settings
                            {
                                HeaderFormat = "Extended User Information [Page {0} of {1}]",
                                HeaderTextColor = Color.Lime,
                                LineTextColor = Color.White,
                                FooterFormat = string.Format("/uix {0} {1} for more", player.Name, pageNumber + 1),
                                FooterTextColor = Color.Lime
                            });
                        }
                        else if (sTools.GetPlayer(name).Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(args.Player, sTools.GetPlayer(name).Select(p => p.Name));
                        }
                        else
                        {
                            if (sTools.GetstoredPlayer(name).Count == 1)
                            {
                                storedPlayer storedplayer = sTools.GetstoredPlayer(name)[0];
                                var uixInfo = new List<string>();
                                var time_1 = DateTime.Now.Subtract(DateTime.Parse(storedplayer.firstLogin));

                                uixInfo.Add(string.Format("UIX info for {0}", storedplayer.name));
                                uixInfo.Add(string.Format("{0} is a member of group {1}", storedplayer.name, TShock.Users.GetUserByName(storedplayer.name).Group));
                                uixInfo.Add(string.Format("First login: {0} ({1} ago)",
                                    storedplayer.firstLogin, sTools.timeSpanPlayed(time_1)));

                                uixInfo.Add(string.Format("Last seen: {0} ({1} ago).", storedplayer.lastSeen,
                                    sTools.timeSpanPlayed(DateTime.Now.Subtract(DateTime.Parse(storedplayer.lastSeen)))));

                                uixInfo.Add(string.Format("Overall play time: {0}", sTools.timePlayed(storedplayer.totalTime)));

                                uixInfo.Add(string.Format("Logged in {0} times since registering", storedplayer.loginCount));
                                try
                                {
                                    uixInfo.Add(string.Format("Known accounts: {0}", string.Join(", ", storedplayer.knownAccounts.Split(','))));
                                }
                                catch { uixInfo.Add("No known accounts found"); }
                                try
                                {
                                    uixInfo.Add(string.Format("Known IPs: {0}", string.Join(", ", storedplayer.knownIPs.Split(','))));
                                }
                                catch { uixInfo.Add("No known IPs found"); }

                                PaginationTools.SendPage(args.Player, pageNumber, uixInfo, new PaginationTools.Settings
                                {
                                    HeaderFormat = "Extended User Information [Page {0} of {1}]",
                                    HeaderTextColor = Color.Lime,
                                    LineTextColor = Color.White,
                                    FooterFormat = string.Format("/uix {0} {1} for more", storedplayer.name, pageNumber + 1),
                                    FooterTextColor = Color.Lime
                                });
                            }
                            else if (sTools.GetstoredPlayer(name).Count > 1)
                            {
                                TShock.Utils.SendMultipleMatchError(args.Player, sTools.GetstoredPlayer(name).Select(
                                    p => p.name));
                            }
                            else
                                args.Player.SendErrorMessage("Invalid player. Try /check name {0} to make sure you're using the right username",
                                name);
                        }
                    }
                }
            }
            else
                args.Player.SendErrorMessage("Invalid syntax. Try /uix [playerName]");
        }
        #endregion

        #region UI_Character
        public static void UI_Character(CommandArgs args)
        {
            if (args.Parameters.Count > 0)
            {
                if (args.Parameters[0] == "self")
                {
                    if (sTools.GetPlayer(args.Player.Index) != null)
                    {
                        sPlayer player = sTools.GetPlayer(args.Player.Index);

                        int pageNumber;
                        if (!PaginationTools.TryParsePageNumber(args.Parameters, 1, args.Player, out pageNumber))
                            return;
                        else
                        {
                            var uicInfo = new List<string>();
                            var time_1 = DateTime.Now.Subtract(DateTime.Parse(player.firstLogin));

                            uicInfo.Add(string.Format("Character info for {0}", args.Parameters[0]));

                            uicInfo.Add(string.Format("First login: {0} ({1} ago)",
                                player.firstLogin, sTools.timeSpanPlayed(time_1)));

                            uicInfo.Add("Last seen: Now");

                            uicInfo.Add(string.Format("Logged in {0} times since registering.  Overall play time: {1}",
                                player.loginCount, sTools.timePlayed(player.TimePlayed)));

                            PaginationTools.SendPage(args.Player, pageNumber, uicInfo, new PaginationTools.Settings
                            {
                                HeaderFormat = "Character Information [Page {0} of {1}]",
                                HeaderTextColor = Color.Lime,
                                LineTextColor = Color.White,
                                FooterFormat = string.Format("/uic {0} {1} for more", args.Parameters[0], pageNumber + 1),
                                FooterTextColor = Color.Lime
                            });
                        }
                    }
                    else
                    {
                        args.Player.SendErrorMessage("Something broke. Please try again later");
                    }
                }

                else
                {
                    string name = "";
                    if (args.Parameters.Count > 1)
                        name = string.Join(" ", args.Parameters).Substring(0, string.Join(" ", args.Parameters).Length - 2);
                    else
                        name = string.Join(" ", args.Parameters).Substring(0, string.Join(" ", args.Parameters).Length);

                    int pageNumber;
                    if (!PaginationTools.TryParsePageNumber(args.Parameters, string.Join(" ", args.Parameters).Length - 1,
                        args.Player, out pageNumber))
                        return;

                    IPAddress IP;
                    if (IPAddress.TryParse(name, out IP))
                    {
                        if (sTools.GetPlayerByIP(IP.ToString()).Count == 1)
                        {
                            sPlayer player = sTools.GetPlayerByIP(IP.ToString())[0];

                            var uicInfo = new List<string>();
                            var time_1 = DateTime.Now.Subtract(DateTime.Parse(player.firstLogin));

                            uicInfo.Add(string.Format("Character info for {0}", player.Name));

                            uicInfo.Add(string.Format("First login: {0} ({1} ago)",
                                player.firstLogin, sTools.timeSpanPlayed(time_1)));

                            uicInfo.Add("Last seen: Now");

                            uicInfo.Add(string.Format("Logged in {0} times since registering.  Overall play time: {1}",
                                    player.loginCount, sTools.timePlayed(player.TimePlayed)));

                            PaginationTools.SendPage(args.Player, pageNumber, uicInfo, new PaginationTools.Settings
                            {
                                HeaderFormat = "Extended User Information [Page {0} of {1}]",
                                HeaderTextColor = Color.Lime,
                                LineTextColor = Color.White,
                                FooterFormat = string.Format("/uic {0} {1} for more", player.Name, pageNumber + 1),
                                FooterTextColor = Color.Lime
                            });

                        }
                        else if (sTools.GetPlayerByIP(IP.ToString()).Count > 1)
                            TShock.Utils.SendMultipleMatchError(args.Player,
                                sTools.GetPlayerByIP(IP.ToString()).Select(p => p.Name));
                        else
                            if (sTools.GetstoredPlayerByIP(IP.ToString()).Count == 1)
                            {
                                storedPlayer storedplayer = sTools.GetstoredPlayerByIP(IP.ToString())[0];
                                var uicInfo = new List<string>();
                                var time_1 = DateTime.Now.Subtract(DateTime.Parse(storedplayer.firstLogin));
                                var time_2 = DateTime.Now.Subtract(DateTime.Parse(storedplayer.lastSeen));

                                uicInfo.Add(string.Format("Character info for {0}", storedplayer.name));

                                uicInfo.Add(string.Format("First login: {0} ({1} ago)",
                                    storedplayer.firstLogin, sTools.timeSpanPlayed(time_1)));

                                uicInfo.Add(string.Format("Last seen: {0} ({1} ago).", storedplayer.lastSeen,
                                    sTools.timeSpanPlayed(time_2)));

                                uicInfo.Add(string.Format("Logged in {0} times since registering.  Overall play time: {1}",
                                    storedplayer.loginCount, sTools.timePlayed(storedplayer.totalTime)));

                                PaginationTools.SendPage(args.Player, pageNumber, uicInfo, new PaginationTools.Settings
                                {
                                    HeaderFormat = "Character Information [Page {0} of {1}]",
                                    HeaderTextColor = Color.Lime,
                                    LineTextColor = Color.White,
                                    FooterFormat = string.Format("/uic {0} {1} for more", storedplayer.name, pageNumber + 1),
                                    FooterTextColor = Color.Lime
                                });
                            }

                            else if (sTools.GetstoredPlayerByIP(IP.ToString()).Count > 1)
                                TShock.Utils.SendMultipleMatchError(args.Player,
                                    sTools.GetstoredPlayerByIP(IP.ToString()).Select(p => p.name));

                            else
                                args.Player.SendErrorMessage("Invalid IP address. Try /check ip {0} to make sure you're using the right IP address",
                            name);
                    }
                    else
                    {
                        if (sTools.GetPlayer(name).Count == 1)
                        {
                            sPlayer player = sTools.GetPlayer(name)[0];

                            var uicInfo = new List<string>();
                            var time_1 = DateTime.Now.Subtract(DateTime.Parse(player.firstLogin));

                            uicInfo.Add(string.Format("Character info for {0}", player.Name));

                            uicInfo.Add(string.Format("First login: {0} ({1} ago)",
                                player.firstLogin, sTools.timeSpanPlayed(time_1)));

                            uicInfo.Add("Last seen: Now");

                            uicInfo.Add(string.Format("Logged in {0} times since registering.  Overall play time: {1}",
                                    player.loginCount, sTools.timePlayed(player.TimePlayed)));

                            PaginationTools.SendPage(args.Player, pageNumber, uicInfo, new PaginationTools.Settings
                            {
                                HeaderFormat = "Extended User Information [Page {0} of {1}]",
                                HeaderTextColor = Color.Lime,
                                LineTextColor = Color.White,
                                FooterFormat = string.Format("/uic {0} {1} for more", player.Name, pageNumber + 1),
                                FooterTextColor = Color.Lime
                            });
                        }
                        else if (sTools.GetPlayer(name).Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(args.Player, sTools.GetPlayer(name).Select(p => p.Name));
                        }
                        else
                        {
                            if (sTools.GetstoredPlayer(name).Count == 1)
                            {
                                storedPlayer storedplayer = sTools.GetstoredPlayer(name)[0];

                                var uicInfo = new List<string>();
                                var time_1 = DateTime.Now.Subtract(DateTime.Parse(storedplayer.firstLogin));
                                var time_2 = DateTime.Now.Subtract(DateTime.Parse(storedplayer.lastSeen));

                                uicInfo.Add(string.Format("Character info for {0}", storedplayer.name));

                                uicInfo.Add(string.Format("First login: {0} ({1} ago)",
                                    storedplayer.firstLogin, sTools.timeSpanPlayed(time_1)));

                                uicInfo.Add(string.Format("Last seen: {0} ({1} ago).", storedplayer.lastSeen,
                                    sTools.timeSpanPlayed(time_2)));

                                uicInfo.Add(string.Format("Logged in {0} times since registering.  Overall play time: {1}",
                                    storedplayer.loginCount, sTools.timePlayed(storedplayer.totalTime)));

                                PaginationTools.SendPage(args.Player, pageNumber, uicInfo, new PaginationTools.Settings
                                {
                                    HeaderFormat = "Character Information [Page {0} of {1}]",
                                    HeaderTextColor = Color.Lime,
                                    LineTextColor = Color.White,
                                    FooterFormat = string.Format("/uic {0} {1} for more", storedplayer.name, pageNumber + 1),
                                    FooterTextColor = Color.Lime
                                });
                            }
                            else if (sTools.GetstoredPlayer(name).Count > 1)
                            {
                                TShock.Utils.SendMultipleMatchError(args.Player, sTools.GetstoredPlayer(name).Select(
                                    p => p.name));
                            }
                            else
                                args.Player.SendErrorMessage("Invalid player. Try /check name {0} to make sure you're using the right username",
                                name);
                        }
                    }
                }
            }
            else
                args.Player.SendErrorMessage("Invalid syntax. Try /uic [playerName]");
        }
        #endregion

        /* ------------------ */


        #region check_Time
        public static void check_Time(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                if (args.Parameters[1] == "self")
                {
                    if (sTools.GetPlayer(args.Player.Index) != null)
                    {
                        sPlayer player = sTools.GetPlayer(args.Player.Index);
                        args.Player.SendInfoMessage("You have played for {0}", sTools.timePlayed(player.TimePlayed));
                    }
                    else
                        if (TSServerPlayer.Server.Name == args.Player.Name)
                            args.Player.SendErrorMessage("The console has no stats to check");
                        else
                            args.Player.SendErrorMessage("Something broke. Please try again later");
                }
                else
                {
                    args.Parameters.RemoveAt(0);
                    string name = string.Join(" ", args.Parameters);

                    if (sTools.GetPlayer(name).Count == 1)
                    {
                        sPlayer player = sTools.GetPlayer(name)[0];
                        args.Player.SendInfoMessage("{0} has played for {1}", player.TSPlayer.UserAccountName,
                            sTools.timePlayed(player.TimePlayed));
                    }
                    else if (sTools.GetPlayer(name).Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, sTools.GetPlayer(name).Select(
                            p => p.Name));
                    }
                    else
                    {
                        if (sTools.GetstoredPlayer(name).Count == 1)
                        {
                            storedPlayer storedplayer = sTools.GetstoredPlayer(name)[0];
                            args.Player.SendInfoMessage("{0} has played for {1}", storedplayer.name,
                                sTools.timePlayed(storedplayer.totalTime));
                        }
                        else if (sTools.GetstoredPlayer(name).Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(args.Player, sTools.GetstoredPlayer(name).Select(
                            p => p.name));
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Invalid player. Try /check name {0} to make sure you're using the right username",
                            name);
                        }
                    }
                }
            }
            else
                args.Player.SendErrorMessage("Invalid syntax. Try /check time [playerName]");
        }
        #endregion

        #region check_Name
        public static void check_Name(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                args.Parameters.RemoveAt(0);
                string name = string.Join(" ", args.Parameters);

                var player = TShock.Utils.FindPlayer(name);

                if (player.Count > 1)
                    TShock.Utils.SendMultipleMatchError(args.Player, player.Select(ply => ply.Name));
                else if (player.Count == 1)
                    if (player[0].IsLoggedIn)
                        args.Player.SendInfoMessage("User name of {0} is {1}", player[0].Name, player[0].UserAccountName);
                    else
                        args.Player.SendErrorMessage("{0} is not logged in", player[0].Name);
                else
                    args.Player.SendErrorMessage("No players matched your query '{0}'", name);
            }
            else
                args.Player.SendErrorMessage("Invalid syntax. Try /check name [playerName]");
        }
        #endregion

        #region check_Kills
        public static void check_Kills(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                if (args.Parameters[1] == "self")
                {
                    if (sTools.GetPlayer(args.Player.Index) != null)
                    {
                        sPlayer player = sTools.GetPlayer(args.Player.Index);

                        args.Player.SendInfoMessage("You have killed {0} player{4}, {1} mob{5}, {2} boss{6} and died {3} time{7}",
                            player.kills, player.mobkills, player.bosskills, player.deaths,
                            sTools.suffix(player.kills), sTools.suffix(player.mobkills),
                            sTools.suffix(player.bosskills), sTools.suffix(player.deaths));
                    }
                    else
                        if (TSServerPlayer.Server.Name == args.Player.Name)
                            args.Player.SendErrorMessage("The console has no stats to check");
                        else
                            args.Player.SendErrorMessage("Something broke. Please try again later");
                }
                else
                {
                    args.Parameters.RemoveAt(0);
                    string name = string.Join(" ", args.Parameters);
                    
                    if (sTools.GetPlayer(name).Count == 1)
                    {
                        sPlayer player = sTools.GetPlayer(args.Parameters[1])[0];
                        args.Player.SendInfoMessage("{0} has killed {1} player{5}, {2} mob{6}, {3} boss{7} and died {4} time{8}",
                            player.TSPlayer.UserAccountName, player.kills, player.mobkills, player.bosskills, player.deaths,
                            sTools.suffix(player.kills), sTools.suffix(player.mobkills),
                            sTools.suffix(player.bosskills), sTools.suffix(player.deaths));
                    }
                    else if (sTools.GetPlayer(name).Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(args.Player, sTools.GetPlayer(name).Select(
                                p => p.Name));
                        }
                    else
                    {
                        if (sTools.GetstoredPlayer(name).Count == 1)
                        {
                            storedPlayer storedplayer = sTools.GetstoredPlayer(name)[0];
                            args.Player.SendInfoMessage("{0} has killed {1} player{5}, {2} mob{6}, {3} boss{7} and died {4} time{8}",
                                storedplayer.name, storedplayer.kills, storedplayer.mobkills, storedplayer.bosskills, storedplayer.deaths,
                                sTools.suffix(storedplayer.kills), sTools.suffix(storedplayer.mobkills),
                                sTools.suffix(storedplayer.bosskills), sTools.suffix(storedplayer.deaths));
                        }
                        else if (sTools.GetstoredPlayer(name).Count > 1)
                        {
                            TShock.Utils.SendMultipleMatchError(args.Player, sTools.GetstoredPlayer(name).Select(
                                p => p.name));
                        }
                        else
                        {
                            args.Player.SendErrorMessage("Invalid player. Try /check name {0} to make sure you're using the right username",
                            name);
                        }
                    }
                }
            }
            else
                args.Player.SendErrorMessage("Invalid syntax. Try /check kills [playerName]");
        }
        #endregion

        #region check_Afk
        public static void check_Afk(CommandArgs args)
        {
            if (args.Parameters.Count > 1)
            {
                if (args.Parameters[1] == "self")
                {
                    if (sTools.GetPlayer(args.Player.Index) != null)
                    {
                        sPlayer player = sTools.GetPlayer(args.Player.Index);
                        if (player.AFK)
                            args.Player.SendInfoMessage("You have been away for {0} seconds", player.AFKcount);
                        else
                            args.Player.SendInfoMessage("You are not away");
                    }
                    else
                        args.Player.SendErrorMessage("Something broke. Please try again later");
                }
                else
                {
                    args.Parameters.RemoveAt(0);
                    string name = string.Join(" ", args.Parameters);
                    
                    if (sTools.GetPlayer(name).Count == 1)
                    {
                        sPlayer player = sTools.GetPlayer(name)[0];
                        if (player.AFK)
                            args.Player.SendInfoMessage("{0} has been away for {1} second{0}",
                                player.TSPlayer.UserAccountName, player.AFKcount,
                                sTools.suffix(player.AFKcount));
                        else
                            args.Player.SendInfoMessage("{0} is not away", player.TSPlayer.UserAccountName);
                    }
                    else if (sTools.GetPlayer(name).Count > 1)
                    {
                        TShock.Utils.SendMultipleMatchError(args.Player, sTools.GetPlayer(name).Select(p => p.Name));
                    }
                    else
                    {
                        args.Player.SendErrorMessage("Invalid player. Try /check name {0} to make sure you're using the right username",
                            args.Parameters[1]);
                    }
                }
            }
            else
                args.Player.SendErrorMessage("Invalid syntax. Try /check afk [playerName\\self]");
        }
        #endregion
    }
}
