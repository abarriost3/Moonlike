using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RLNET;

namespace MoonLike
{
    public static class MapGenerator
    {
        /// <summary>
        /// Generates a map with only ground
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static GameMap GenerateEmptyMap(int w, int h)
        {
            GameMap m = new GameMap(w, h);

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    m.tile[i, j] = new GroundTile();
                }
            }

            return m;
        }


        /// <summary>
        /// Generates a full walled map (Wall)
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static GameMap GenerateWallsMap(int w, int h)
        {
            GameMap m = new GameMap(w, h);

            for (int i = 0; i < w; i++)
            {
                for (int j = 0; j < h; j++)
                {
                    m.tile[i, j] = new WallTile();
                }
            }

            return m;
        }

        /// <summary>
        /// Generates a full empty map with walls outside
        /// </summary>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static GameMap GenerateEmptyWalledMap(int w, int h)
        {
            GameMap m = new GameMap(w, h);

            //Horizontal exterior walls
            for (int i = 0; i < w; i++)
            {
                m.tile[i, 0] = new WallTile();
                m.tile[i, h - 1] = new WallTile();
            }


            //Ground and vertical exterior walls
            for (int j = 1; j < (h - 1); j++)
            {
                m.tile[0, j] = new WallTile();
                m.tile[w - 1, j] = new WallTile();
                for (int i = 1; i < (w - 1); i++)
                {
                    m.tile[i, j] = new GroundTile();
                }
            }

            return m;
        }

        //Procedurally generates a dungeon map
        public static GameMap GenerateDungeon(int width, int height)
        {
            GameMap m;

            Console.WriteLine("Generating a " + width.ToString() + "x" + height.ToString() + " dungeon.");

            int minRooms;
            int maxRooms;
            int maxRoomWidth = 10 * 2;
            int maxRoomHeight = 10 * 2;
            int minRoomWidth = 3 * 2;
            int minRoomHeight = 3 * 2;
            int minDistanceRooms = 3; //Minimum of tiles beetween rooms
            int maxIterations = 150; //Maximum iterations for the generator to determine the rooms
            int minConectivity = 1; //Minimum connectivity for the rooms
            int maxConectivity = 3; //Maximum connectivity for the rooms
            bool stairsShareRoom = false; //If true, it wont matter if the upstairs are in the same room that downstairs

            bool[,] occupiedEntity = new bool[width, height];

            //Initializing occupiedEntity matrix
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    occupiedEntity[i, j] = false;
                }
            }

            //Fills the map with wall tiles
            m = GenerateWallsMap(width, height);

            //Determining the minimum rooms
            if ((width / (minRoomWidth + minDistanceRooms)) * 2 < (height / (minRoomHeight + minDistanceRooms)) * 2) maxRooms = (width / (minRoomWidth + minDistanceRooms)) * 2;
            else maxRooms = (height / (minRoomHeight + minDistanceRooms)) * 2;
            Console.WriteLine("Max rooms: " + maxRooms.ToString());

            //Determining the maximum rooms
            if ((width / (maxRoomWidth + minDistanceRooms)) * 2 < (height / (maxRoomHeight + minDistanceRooms)) * 2) minRooms = (width / (maxRoomWidth + minDistanceRooms)) * 2;
            else minRooms = (height / (maxRoomHeight + minDistanceRooms)) * 2;
            Console.WriteLine("Min rooms: " + minRooms.ToString() + "\n");


            //minRooms = 2;
            //maxRooms = 2;

            //Determining the rooms in the map
            int currentRooms = 0;
            int iterations = 0;
            int w;
            int h;
            int x;
            int y;
            List<Room> rooms = new List<Room>();
            //If there isnt enough rooms it will keep going infinitely
            while ((currentRooms < minRooms) || ((currentRooms < maxRooms) && (iterations < maxIterations)))
            {
                //Try to create a random room
                w = Engine.rng.Next(minRoomWidth, maxRoomWidth + 1);
                h = Engine.rng.Next(minRoomHeight, maxRoomHeight + 1);
                x = Engine.rng.Next(1, width);
                y = Engine.rng.Next(1, height);

                //Check if the conditions of the room are acceptable 
                //
                //Checks if the room can be contained in the map by just its size
                if (((x + w) < width) && ((y + h) < height))
                {
                    //Checks if the room is overlayed to another existing room and
                    //Special cases where the room can "break" the map
                    bool c = SpecialRoomConditionsChecker(w, h, x, y, minDistanceRooms, m);

                    //If its not overlayed or "breaks" the map, its created
                    if (c == false)
                    {
                        //Conditions met
                        //Creates room
                        Room r = new Room(w, h, x, y);
                        rooms.Add(r);
                        currentRooms++;
                        Console.WriteLine("\nRoom added! w: " + w.ToString() + " h: " + h.ToString() + " x: " + x.ToString() + " y: " + y.ToString());
                        Console.WriteLine("Current rooms created: " + currentRooms.ToString());
                        Console.WriteLine("Iteration: " + iterations.ToString() + "\n");

                        //Updates the map adding the room to it
                        m = AddRoomMap(w, h, x, y, m);

                        //Adds enemies to the room
                        //GenerateEnemiesSquare(ref rng, x, y, (x + w - 1), (y + h - 1), 3, 0, 999, ref occupiedEntity);

                        //Adds items to the room
                        //GenerateItemsSquare(x, y, (x + w - 1), (y + h - 1), 3, 0, 999);

                        int d = 9999999;
                        Room roomToConnect = null;
                        Tuple<int, EnumDirection> t = null;
                        EnumDirection dir = EnumDirection.NONE;
                        //Finds the nearest room in the map
                        for (int i = 0; i < rooms.Count; i++)
                        {
                            //Checks if the room from the list is the same room
                            if ((rooms[i].topLeftX != r.topLeftX) || (rooms[i].topLeftY != r.topLeftY))
                            {
                                t = r.RoomDistance(rooms[i]);
                                //Checks if its the nearest room connected
                                if (t.Item1 <= d)
                                {
                                    //Checks if it has surpassed the maximum connectivity allowed
                                    if (rooms[i].connectivity < maxConectivity)
                                    {
                                        dir = t.Item2;
                                        d = t.Item1;
                                        roomToConnect = rooms[i];
                                    }

                                }
                            }

                        }

                        //Connects the room to the nearest one
                        if (roomToConnect != null)
                        {
                            r.connectivity = r.connectivity + 1;
                            roomToConnect.connectivity = r.connectivity + 1;
                            m = ConnectRooms(r, roomToConnect, dir, m);
                        }

                    }





                }


                //Console.WriteLine("Iteration: " + iterations.ToString());
                iterations++;
            }
            //All rooms created and connected
            Console.WriteLine("Rooms placed and connected!. Total iterations: " + iterations.ToString() + "\n\n\n");

            //Determine in which room are located the staircases
            int[] roomIndex;
            roomIndex = new int[m.numberStairs];

            //Upstairs location
            roomIndex[0] = Engine.rng.Next(0, rooms.Count);
            //Downstairs location
            roomIndex[1] = Engine.rng.Next(0, rooms.Count);
            while ((stairsShareRoom == false) && (m.numberStairs <= rooms.Count) && (roomIndex[0] == roomIndex[1]))
            {
                roomIndex[1] = Engine.rng.Next(0, rooms.Count);
            }

            //Placing the stairs
            //Upstairs
            m.stairsX[0] = Engine.rng.Next(rooms[roomIndex[0]].topLeftX, (rooms[roomIndex[0]].topLeftX + rooms[roomIndex[0]].width));
            m.stairsY[0] = Engine.rng.Next(rooms[roomIndex[0]].topLeftY, (rooms[roomIndex[0]].topLeftY + rooms[roomIndex[0]].height));
            m.tile[m.stairsX[0], m.stairsY[0]] = new UpstairsTile();


            //Downstairs
            m.stairsX[1] = Engine.rng.Next(rooms[roomIndex[1]].topLeftX, (rooms[roomIndex[1]].topLeftX + rooms[roomIndex[1]].width));
            m.stairsY[1] = Engine.rng.Next(rooms[roomIndex[1]].topLeftY, (rooms[roomIndex[1]].topLeftY + rooms[roomIndex[1]].height));
            m.tile[m.stairsX[1], m.stairsY[1]] = new DownstairsTile();




            /*
			 * TESTER
			 */
            //Room rt2 = new Room(w, h, x, y);
            /*Room rt = new Room(5, 5, 10, 20);
			Room rt2 = new Room(5, 5, 15, 40);
			Console.WriteLine("Room 0| x: " + rt.X().ToString() + " y: " +  rt.Y().ToString() + " w: " +  rt.Width().ToString()+ " h: " +  rt.Height().ToString());
			Console.WriteLine("Room 1| x: " + rt2.X().ToString() + " y: " +  rt2.Y().ToString() + " w: " +  rt2.Width().ToString()+ " h: " +  rt2.Height().ToString() + "\n");
			Tuple<int, Direction> t5 = rt.RoomDistance(rt2);
			Console.WriteLine("Distance: " + t5.Item1.ToString());
			Console.WriteLine("Direction: " + t5.Item2.ToString());*/

            return m;
        }

        //Checks if the room is overlayed to another existing room and
        //Special cases where the room can "break" the map
        private static bool SpecialRoomConditionsChecker(int w, int h, int x, int y, int minDistanceRooms, GameMap m)
        {
            bool c = false;
            //Checks if the room is overlayed to another existing room and
            //Special cases where the room can "break" the map
            //
            // x is lesser than the map
            if ((x - minDistanceRooms) <= 0)
            {
                // x and y are lesser than the map
                if ((y - minDistanceRooms) <= 0)
                {
                    //Console.WriteLine("ROOM CHECKER: x<m and y<m");
                    c = SpaceChecker((w + minDistanceRooms), (h + minDistanceRooms), x, y, m);
                }
                // x is lesser than the map, but y is bigger than the map
                else if ((y + minDistanceRooms + h) >= (m.height - 1))
                {
                    //Console.WriteLine("ROOM CHECKER: x<m and y>m");
                    c = SpaceChecker((w + minDistanceRooms), (h + minDistanceRooms), x, (y - minDistanceRooms), m);
                }
                // x is lesser than the map, but y is inside the map
                else
                {
                    //Console.WriteLine("ROOM CHECKER: x<m and y=m");
                    c = SpaceChecker((w + minDistanceRooms), (h + (minDistanceRooms * 2)), x, (y - minDistanceRooms), m);
                }
            }
            // x is bigger than the map
            else if ((x + minDistanceRooms + w) >= (m.width - 1))
            {
                // x and y are bigger than the map
                if ((y + minDistanceRooms + h) >= (m.height - 1))
                {
                    //Console.WriteLine("ROOM CHECKER: x>m and y>m");
                    c = SpaceChecker((w + minDistanceRooms), (h + minDistanceRooms), (x - minDistanceRooms), (y - minDistanceRooms), m);
                }
                // x is bigger than the map, but y is lesser than the map
                else if ((y - minDistanceRooms) <= 0)
                {
                    //Console.WriteLine("ROOM CHECKER: x>m and y<m");
                    c = SpaceChecker((w + minDistanceRooms), (h + minDistanceRooms), (x - minDistanceRooms), y, m);
                }
                // x is bigger than the map, but y is inside the map
                else
                {
                    //Console.WriteLine("ROOM CHECKER: x>m and y=m");
                    c = SpaceChecker((w + minDistanceRooms), (h + (minDistanceRooms * 2)), (x - minDistanceRooms), (y - minDistanceRooms), m);
                }
            }
            // x is inside the map
            else
            {
                // x is inside the map but y is bigger than the map
                if ((y + minDistanceRooms + h) >= (m.height - 1))
                {
                    //Console.WriteLine("ROOM CHECKER: x=m and y>m");
                    c = SpaceChecker((w + minDistanceRooms * 2), (h + minDistanceRooms), (x - minDistanceRooms), (y - minDistanceRooms), m);
                }
                // x is inside the the map, but y is lesser than the map
                else if ((y - minDistanceRooms) <= 0)
                {
                    //Console.WriteLine("ROOM CHECKER: x=m and y<m");
                    c = SpaceChecker((w + minDistanceRooms * 2), (h + minDistanceRooms), (x - minDistanceRooms), y, m);
                }
                // x is inside the the map, but y is inside the map
                else
                {
                    //Console.WriteLine("ROOM CHECKER: x=m and y=m");
                    c = SpaceChecker((w + minDistanceRooms * 2), (h + (minDistanceRooms * 2)), (x - minDistanceRooms), (y - minDistanceRooms), m);
                }
            }

            return c;
        }


        //Checks if the space in those coordinates is occupied
        //
        //PRE: Map tiles must be accessible (exist)
        public static bool SpaceChecker(int w, int h, int x, int y, GameMap m)
        {
            int i = 0;
            int j = 0;
            bool occupied = false;

            while ((j < h) && (occupied == false))
            {
                while ((i < w) && (occupied == false))
                {
                    //Checks the coordinate if it has a ground tile
                    try
                    {
                        if (m.tile[(x + i), (y + j)].type == EnumGameTile.GROUND)
                        {
                            occupied = true;
                            //Console.WriteLine("Room occupied, trying again");
                        }



                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("SpaceChecker.ERROR: Accessing unexistent map location");
                        Console.WriteLine("Error in | w: " + w.ToString() + " h: " + h.ToString() + " x: " + x.ToString() + " y: " + y.ToString() + " |" + " x+i: " + (x + i).ToString() + " y+j: " + (y + j).ToString());
                        throw e;
                    }

                    i++;
                }

                i = 0;
                j++;
            }

            return occupied;
        }

        //Creates ground tiles on those coordinates
        //
        //PRE: Map tiles must be accessible (exist)
        public static GameMap AddRoomMap(int w, int h, int x, int y, GameMap m0)
        {
            GameMap m = m0;

            int i = 0;
            int j = 0;

            while (j < h)
            {
                while (i < w)
                {
                    //Checks the coordinate and place a ground tile
                    try
                    {
                        m.tile[(x + i), (y + j)] = new GroundTile();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("AddRoomMap.ERROR: Accessing unexistent map location");
                        throw e;
                    }

                    i++;
                }

                i = 0;
                j++;
            }

            return m;
        }

        //Connects the room B, to the room A. Using the Direction that the room B is relative to room A
        public static GameMap ConnectRooms(Room a, Room b, EnumDirection d, GameMap m0)
        {
            GameMap m = m0;

            int x1;
            int x2;
            int x3; //Random point
            int y1;
            int y2;
            int y3; //Random point

            int i;

            //Will contemplate all cases for the room
            switch (d)
            {
                case EnumDirection.NORTH:
                    //Finds the points that overlaps both rooms
                    //Minimum x point
                    if (b.topLeftX < a.topLeftX) x1 = a.topLeftX;
                    else x1 = b.topLeftX;

                    //Maximum x point
                    if ((b.topLeftX + b.width - 1) > (a.topLeftX + a.width - 1)) x2 = (a.topLeftX + a.width - 1);
                    else x2 = (b.topLeftX + b.width - 1);


                    //Picks a random point beetween those two points
                    x3 = Engine.rng.Next(x1, (x2 + 1));


                    //Creates the path
                    //Vertically
                    i = a.topLeftY;
                    while (i > (b.topLeftY + b.height - 1))
                    {
                        m.tile[x3, i] = new GroundTile();
                        i--;
                    }
                    break;
                case EnumDirection.NORTHEAST:
                    //A bit of randomness added to make the path different each time
                    //Path up and right
                    if (Engine.rng.Next(1, 3) == 1)
                    {
                        x3 = Engine.rng.Next(a.topLeftX, (a.topLeftX + a.width));
                        y3 = Engine.rng.Next(b.topLeftY, (b.topLeftY + b.height));

                        //Creates the path
                        //Vertically
                        i = a.topLeftY;
                        while (i > y3)
                        {
                            m.tile[x3, i] = new GroundTile();
                            i--;
                        }
                        //Horizontally
                        i = x3;
                        while (i < b.topLeftX)
                        {
                            m.tile[i, y3] = new GroundTile();
                            i++;
                        }
                    }
                    //Path right and up
                    else
                    {
                        x3 = Engine.rng.Next(b.topLeftX, (b.topLeftX + b.width));
                        y3 = Engine.rng.Next(a.topLeftY, (a.topLeftY + a.height));

                        //Creates the path
                        //Vertically
                        i = (b.topLeftY + b.height);
                        while (i < y3)
                        {
                            m.tile[x3, i] = new GroundTile();
                            i++;
                        }
                        //Horizontally
                        i = (a.topLeftX + a.width);
                        while (i <= x3)
                        {
                            m.tile[i, y3] = new GroundTile();
                            i++;
                        }
                    }
                    break;
                case EnumDirection.EAST:
                    //Finds the points that overlaps both rooms
                    //Minimum x point
                    if (b.topLeftY < a.topLeftY) y1 = a.topLeftY;
                    else y1 = b.topLeftY;

                    //Maximum x point
                    if ((b.topLeftY + b.height - 1) > (a.topLeftY + a.height - 1)) y2 = (a.topLeftY + a.height - 1);
                    else y2 = (b.topLeftY + b.height - 1);


                    //Picks a random point beetween those two points
                    y3 = Engine.rng.Next(y1, (y2 + 1));


                    //Creates the path
                    //Vertically
                    i = b.topLeftX;
                    while (i > (a.topLeftX + a.width - 1))
                    {
                        m.tile[i, y3] = new GroundTile();
                        i--;
                    }
                    break;
                case EnumDirection.SOUTHEAST:
                    //A bit of randomness added to make the path different each time
                    //Path down and right
                    if (Engine.rng.Next(1, 3) == 1)
                    {
                        x3 = Engine.rng.Next(a.topLeftX, (a.topLeftX + a.width));
                        y3 = Engine.rng.Next(b.topLeftY, (b.topLeftY + b.height));

                        //Creates the path
                        //Vertically
                        i = (a.topLeftY + a.height);
                        while (i <= y3)
                        {
                            m.tile[x3, i] = new GroundTile();
                            i++;
                        }
                        //Horizontally
                        i = x3;
                        while (i < b.topLeftX)
                        {
                            m.tile[i, y3] = new GroundTile();
                            i++;
                        }
                    }
                    //Path right and down
                    else
                    {
                        x3 = Engine.rng.Next(b.topLeftX, (b.topLeftX + b.width));
                        y3 = Engine.rng.Next(a.topLeftY, (a.topLeftY + a.height));

                        //Creates the path
                        //Vertically
                        i = b.topLeftY;
                        while (i > y3)
                        {
                            m.tile[x3, i] = new GroundTile();
                            i--;
                        }
                        //Horizontally
                        i = (a.topLeftX + a.width);
                        while (i <= x3)
                        {
                            m.tile[i, y3] = new GroundTile();
                            i++;
                        }
                    }
                    break;
                case EnumDirection.SOUTH:
                    //Finds the points that overlaps both rooms
                    //Minimum x point
                    if (b.topLeftX < a.topLeftX) x1 = a.topLeftX;
                    else x1 = b.topLeftX;

                    //Maximum x point
                    if ((b.topLeftX + b.width - 1) > (a.topLeftX + a.width - 1)) x2 = (a.topLeftX + a.width - 1);
                    else x2 = (b.topLeftX + b.width - 1);


                    //Picks a random point beetween those two points
                    x3 = Engine.rng.Next(x1, (x2 + 1));


                    //Creates the path
                    //Vertically
                    i = b.topLeftY;
                    while (i > (a.topLeftY + a.height - 1))
                    {
                        m.tile[x3, i] = new GroundTile();
                        i--;
                    }
                    break;
                case EnumDirection.SOUTHWEST:
                    //A bit of randomness added to make the path different each time
                    //Path up and right
                    if (Engine.rng.Next(1, 3) == 1)
                    {
                        x3 = Engine.rng.Next(b.topLeftX, (b.topLeftX + b.width));
                        y3 = Engine.rng.Next(a.topLeftY, (a.topLeftY + a.height));

                        //Creates the path
                        //Vertically
                        i = b.topLeftY;
                        while (i >= y3)
                        {
                            m.tile[x3, i] = new GroundTile();
                            i--;
                        }
                        //Horizontally
                        i = x3;
                        while (i < a.topLeftX)
                        {
                            m.tile[i, y3] = new GroundTile();
                            i++;
                        }
                    }
                    //Path right and up
                    else
                    {
                        x3 = Engine.rng.Next(a.topLeftX, (a.topLeftX + a.width));
                        y3 = Engine.rng.Next(b.topLeftX, (b.topLeftY + b.height));

                        //Creates the path
                        //Vertically
                        i = (a.topLeftY + a.height);
                        while (i < y3)
                        {
                            m.tile[x3, i] = new GroundTile();
                            i++;
                        }
                        //Horizontally
                        i = (b.topLeftX + b.width);
                        while (i <= x3)
                        {
                            m.tile[i, y3] = new GroundTile();
                            i++;
                        }
                    }
                    break;
                case EnumDirection.WEST:
                    //Finds the points that overlaps both rooms
                    //Minimum x point
                    if (b.topLeftY < a.topLeftY) y1 = a.topLeftY;
                    else y1 = b.topLeftY;

                    //Maximum x point
                    if ((b.topLeftY + b.height - 1) > (a.topLeftY + a.height - 1)) y2 = (a.topLeftY + a.height - 1);
                    else y2 = (b.topLeftY + b.height - 1);


                    //Picks a random point beetween those two points
                    y3 = Engine.rng.Next(y1, (y2 + 1));


                    //Creates the path
                    //Vertically
                    i = a.topLeftX;
                    while (i > (b.topLeftX + b.width - 1))
                    {
                        m.tile[i, y3] = new GroundTile();
                        i--;
                    }
                    break;
                case EnumDirection.NORTHWEST:
                    //A bit of randomness added to make the path different each time
                    //Path down and right
                    if (Engine.rng.Next(1, 3) == 1)
                    {
                        x3 = Engine.rng.Next(b.topLeftX, (b.topLeftX + b.width));
                        y3 = Engine.rng.Next(a.topLeftY, (a.topLeftY + a.height));

                        //Creates the path
                        //Vertically
                        i = (b.topLeftY + b.height);
                        while (i <= y3)
                        {
                            m.tile[x3, i] = new GroundTile();
                            i++;
                        }
                        //Horizontally
                        i = x3;
                        while (i < a.topLeftX)
                        {
                            m.tile[i, y3] = new GroundTile();
                            i++;
                        }
                    }
                    //Path right and down
                    else
                    {
                        x3 = Engine.rng.Next(a.topLeftX, (a.topLeftX + a.width));
                        y3 = Engine.rng.Next(b.topLeftY, (b.topLeftY + b.height));

                        //Creates the path
                        //Vertically
                        i = a.topLeftY;
                        while (i >= y3)
                        {
                            m.tile[x3, i] = new GroundTile();
                            i--;
                        }
                        //Horizontally
                        i = (b.topLeftX + b.width);
                        while (i < x3)
                        {
                            m.tile[i, y3] = new GroundTile();
                            i++;
                        }
                    }
                    break;

            }




            return m;
        }

        //Generates enemies inside of a square with definite parameters
        // n = number of generated enemies
        // minD = minimun dangerousity level
        // maxD = maximum dangerousity level
        /*public static List<Entity> GenerateEnemiesSquare(ref SuperRandom rng, int x1, int y1, int x2, int y2, int n, int minD, int maxD, ref bool[,] occupiedEntity)
        {
            int i = 0;
            int iteration = 0;
            int maxIterations = 100;
            int x = 0;
            int y = 0;
            //It generates all the enemies until the number of them is the specified or it reaches the maximum iterations possible
            while ((i < n) && (iteration < maxIterations))
            {
                //Tries to find a spot for the entity
                x = rng.Next(x1, x2 + 1);
                y = rng.Next(y1, y2 + 1);

                //If the spot isnt taken, then it generates the entity there
                if (occupiedEntity[x, y] == false)
                {
                    Console.WriteLine("\nEntity created! " + " x: " + x.ToString() + " y: " + y.ToString());
                    //Console.WriteLine("Current entities created: " + i.ToString());

                    Entity e = new Entity(ref rng, "Rat", Concept.RAT, Concept.FEMALE, Concept.YOUNGADULT, true);
                    e.ChangeX(x);
                    e.ChangeY(y);
                    entities.Add(e);
                    occupiedEntity[x, y] = true;
                    i++;
                }


                iteration++;
            }
            Console.WriteLine("Iterations: " + iteration.ToString() + "\n");
        }*/
    }
}
