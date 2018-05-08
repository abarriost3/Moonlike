using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RLNET;

namespace MoonLike
{
    public class Engine
    {
        public const int generalMapWidth = 100;
        public const int generalMapHeight = 100;

        public static Screen actualScreen { get; set; }
        public static Player player { get; set; }
        public static SuperRandom rng { get; private set; }

        public static List<GameMap> mapList { get; private set; }
        public static int currentMapIndex { get; set; }


        public Engine()
        {
            //Setting up the Roguelike Console
            Program.rootConsole.Render += Render;
            Program.rootConsole.Update += Update;

            actualScreen = new MainMenuScreen();
            player = new Player("Player");
            rng = new SuperRandom();



            mapList = new List<GameMap>();
            mapList.Add(MapGenerator.GenerateDungeon(generalMapWidth, generalMapHeight));
            currentMapIndex = 0;
        }

        /// <summary>
        /// Function that renders each frame of the game each turn
        /// </summary>
        public void Render(object sender, UpdateEventArgs e)
        {
            //Clear things
            Program.rootConsole.Clear();

            //Display Screen
            actualScreen.Draw();

            //Materialise them
            Program.rootConsole.Draw();
        }

        /// <summary>
        /// Function that updates the game each turn
        /// </summary>
        public void Update(object sender, UpdateEventArgs e)
        {
            //Read keyboard
            actualScreen.ReadKeyboardInput();
        }


    }
}
