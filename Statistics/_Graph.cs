/*
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

using System.Timers;

using Terraria;
using TerrariaApi;
using TerrariaApi.Server;

using TShockAPI;
using TShockAPI.DB;

using Mono.Data.Sqlite;
using MySql.Data.MySqlClient;

using Newtonsoft.Json;

namespace Statistics
{
    public class _Graph
    {
        public static List<string> validTypes = new List<string>() { "damage", "time" };

        #region GraphPointCommand
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
                        if (!getPoints(type, false))
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

                            player.posPoint.X = player.TSPlayer.TileX;
                            player.posPoint.Y = player.TSPlayer.TileY;

                            args.Player.SendSuccessMessage(string.Format
                                ("{0} added a graph location for type {1} at point ({2}, {3})",
                                addPoints(player.posPoint, type) ? "Successfully" : "Unsuccessfully", type,
                                (int)player.posPoint.X, (int)player.posPoint.Y));

                            if (type == "damage")
                            _GraphData.damageData.Add(DateTime.Now.Date.DayOfWeek.ToString().ToLower(), 
                                new Dictionary<int, int[]>() { { 0, new int[] { 0 } } });

                            else if (type == "time")
                                _GraphData.timeData.Add(DateTime.Now.Date.DayOfWeek.ToString().ToLower(),
                                new Dictionary<int, int[]>() { { 0, new int[] { 0 } } });
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
        #endregion

        #region CreateGraph
        public static void createGraph(CommandArgs args)
        {
            string type = "";
            if (args.Parameters.Count < 1)
                args.Player.SendErrorMessage("Invalid syntax: /cgraph <type>");

            else
            {
                if (args.Parameters[0].Length > 0)
                {
                    type = args.Parameters[0];

                    if (getPoints(type, false))
                    {
                        Vector2 startPoint = getPoints(type);

                        bool damage = false;

                        if (type == "damage")
                          damage = true;

                        for (int i = (int)startPoint.X; i < (int)(startPoint.X +
                            (damage ? _GraphData.damageData.Keys.Count : _GraphData.timeData.Keys.Count) * 6); i++)
                        {
                            for (int j = (int)startPoint.Y + 6; j > (int)startPoint.Y + 2; j--)
                            {
                                if (Main.tile[i, j].type != _GraphData.graphTile)
                                {
                                    WorldGen.KillTile(i, j, false, false, false);
                                    WorldGen.PlaceTile(i, j, _GraphData.graphTile, false, true, TSPlayer.Server.Index, 0);
                                }
                                else
                                {
                                    WorldGen.PlaceTile(i, j, _GraphData.graphTile, false, true, TSPlayer.Server.Index, 0);
                                }
                            }
                        }

                        updatePlayers(false);
                        args.Player.SendSuccessMessage("Attempted to create a graphing platform!");
                    }
                }
            }
        }
        #endregion

        #region getPoints(Bool value)
        public static bool getPoints(string Type, bool vector)
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
        #endregion

        #region getPoints(return vector)
        public static Vector2 getPoints(string Type)
        {
            using (var reader = Statistics.db.QueryReader("SELECT * FROM Graphs WHERE Type = @0", Type.ToLower()))
            {
                if (reader.Read())
                {
                    Vector2 vector = new Vector2(reader.Get<int>("PointX"), reader.Get<int>("PointY"));

                    return vector;
                }
                else
                {
                    Vector2 failedVector = new Vector2(0, 0);

                    return failedVector;
                }
            }
        }
        #endregion

        #region addPoints(to database, return true or false)
        public static bool addPoints(Vector2 point, string type)
        {
            if (Statistics.db.Query("INSERT INTO Graphs (PointX, PointY, Type) VALUES (@0, @1, @2)",
                (int)point.X, (int)point.Y, type) == 1)
            {
                return true;
            }

            return false;
        }
        #endregion

        #region updatePlayerTiles (thanks @k0rd and the world refill team!)
        public static void updatePlayers(bool hard = false)
        {
            foreach (TSPlayer player in TShock.Players)
            {
                if ((player != null) && (player.Active))
                {
                    for (int i = 0; i < 255; i++)
                    {
                        for (int j = 0; j < Main.maxSectionsX; j++)
                        {
                            for (int k = 0; k < Main.maxSectionsY; k++)
                            {
                                Netplay.serverSock[i].tileSection[j, k] = false;
                            }
                        }
                    }
                }
            }

        }
        #endregion
    }

    public class _GraphData
    {

        public static int graphTile = 148;
        public static int columnTile = 121;

        public string Warning = "PLEASE DO NOT EDIT GRAPH DATA";
        public static Dictionary<string, Dictionary<int, int[]>> damageData
            = new Dictionary<string, Dictionary<int, int[]>>() { };
        //Day name, average, numbers making up average

        public static Dictionary<string, Dictionary<int, int[]>> timeData
            = new Dictionary<string, Dictionary<int, int[]>>() { };

        //Start date, then check if a week has passed. If week has passed, new graph data for new graph
        //config stuff

     
    }
}
*/
