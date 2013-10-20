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
    public class Graph
    {
        public static List<string> validTypes = new List<string>()
        { "damage", "time" };

        public static void graphCommand(CommandArgs args)
        {
            bool validType = false;
            string type = "";

            if (args.Parameters.Count < 1)
            {
                args.Player.SendErrorMessage("Invalid syntax: /graph setpoint [type]");
                args.Player.SendErrorMessage("Valid types: damage, time");
            }

            else
            {
                if (args.Parameters[1].Length > 0)
                {
                    type = args.Parameters[1];

                    if (validTypes.Contains(type))
                    {
                        if (!getPoints(type))
                        {
                            validType = true;
                        }
                    }
                }

                if (args.Parameters[0] == "setpoint")
                {
                    if (validType)
                    {
                        if (Statistics.GetPlayer(args.Player.Index) != null)
                        {
                            var player = Statistics.GetPlayer(args.Player.Index);

                            player.posPoint.X = player.TSPlayer.X;
                            player.posPoint.Y = player.TSPlayer.Y;

                            //args.Player.SendSuccessMessage(string.Format("Set positioning point at ({0}, {1})",
                            //    player.posPoint.X, player.posPoint.Y));

                            args.Player.SendSuccessMessage(string.Format
                                ("{0} added a graph location for type {1} at point ({2}, {3}",
                                addPoints(player.posPoint, type) ? "Successfully" : "Unsuccessfully", type,
                                player.posPoint.X, player.posPoint.Y));
                        }
                    }
                    else
                    {
                        args.Player.SendErrorMessage((type.Length > 0 ? "Invalid" : "Non-existant") +
                            " type specified");
                    }
                }
            }
        }

        public static bool getPoints(string Type)
        {
            using (var reader = Statistics.db.QueryReader("SELECT * FROM Graphs WHERE Type = @0", Type.ToLower()))
            {
                if (reader.Read())
                {
                    return true;
                }
                else
                    return false;
            }
        }

        public static bool addPoints(Vector2 point, string type)
        {
            if (Statistics.db.Query("INSERT INTO Graphs (PointX, PointY, Type) VALUES (@0, @1, @2)",
                point.X, point.Y, type) == 1)
            {
                return true;
            }

            return false;
        }
    }

    public class GraphData
    {
        public Dictionary<string, Dictionary<int, int[]>> graphData
            = new Dictionary<string, Dictionary<int, int[]>>() { };
    }
}
