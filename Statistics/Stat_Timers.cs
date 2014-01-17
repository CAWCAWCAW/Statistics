using System;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Collections.Generic;

using TShockAPI;

namespace Statistics
{
    public class Stat_Timers
    {
        static Timer aTimer = new Timer(5 * 1000);
        static Timer uTimer = new Timer(60 * 1000);
        static Timer databaseSaver = new Timer(600 * 1000);

        public static void Start(EventArgs args)
        {
            aTimer.Enabled = true;
            aTimer.Elapsed += new ElapsedEventHandler(afkTimer);

            uTimer.Enabled = true;
            uTimer.Elapsed += new ElapsedEventHandler(updateTimer);

            databaseSaver.Enabled = true;
            databaseSaver.Elapsed += new ElapsedEventHandler(databaseTimer);
        }

        static void databaseTimer(object sender, ElapsedEventArgs args)
        {
            sTools.saveDatabase();
        }

        static void afkTimer(object sender, ElapsedEventArgs args)
        {
            foreach (sPlayer player in sTools.splayers)
            {
                if (player.TSPlayer.X == player.lastPosX && player.TSPlayer.Y == player.lastPosY)
                {
                    player.AFKcount += 5;
                    if (player.AFKcount > 300)
                    {
                        if (!player.AFK)
                        {
                            TSPlayer.All.SendInfoMessage("{0} is now away", player.TSPlayer.Name);
                            player.TSPlayer.SendWarningMessage("You are now marked as away.");
                            player.TSPlayer.SendWarningMessage("This time is not being counted towards your statistics");
                            player.AFK = true;
                        }
                    }
                }
                else
                {
                    player.TimePlayed += 5;
                    if (player.AFK)
                    {
                        player.AFK = false;
                    }

                    if (player.AFKcount > 0)
                        player.AFKcount = 0;
                }

                player.lastPosX = player.TSPlayer.X;
                player.lastPosY = player.TSPlayer.Y;
            }
        }

        static void updateTimer(object sender, ElapsedEventArgs args)
        {
            foreach (sPlayer player in sTools.splayers)
            {
                if (!player.AFK && player.TSPlayer.IsLoggedIn)
                {
                    sTools.UpdatePlayer(player);
                }
            }
        }
    }
}
