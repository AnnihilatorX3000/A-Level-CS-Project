using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.IO;
using System.Drawing.Imaging;

namespace CSharp_NEAPathfinding
{
    public partial class Form1 : Form
    {
        /* EXTENSIONS:
         * -Save/Load Project files
         * -Loading screens on importing map (maybe @ start as well - Splash screen)
         * -Settings tab
         * 
         * -Background Thread Worker to handle pathfinding
         *      -Pathfinding speed slider
         *      -Step-by-step mode
         *      
         * -Mouse movement interpolation - USE MOUSE HIGHLIGHT
         * 
         * -Scope of Travel with Boundary Mode
         * -Bidirectional Search Mode
         * -Random Maze Creation
         * -Pathfind through muliple points - Complete graph and Minimum spanning tree
         * 
         * -LAYERS - just have viewable/not viewable - Also grid lines as layer - Need to be able to shift orders
         *      -Checkbox to show path, show shaded region
         *      -Moving tiles around by CTRL + M1 select region to drag, then move around with M1 hold while highlighted. Unhighlight by clicking M1  
         * 
         * 
         * 
         * For faster processing:
         *      Tile map edit mode - ONLY colours (or assign name and colour) - THEN when start pathfinding, BUILD GRAPH
         *      
         * Minigame idea:
         *      Pathfinding tag?
         *      
         * How to allow for even larger maps?? 
         * 
         * 
         */

        /* Bug List:
         *     Max allowable weight: 2147483647, which = max int. value, will cause overflow when finding costs over multiple tiles and crash
         *    
         *
         */

        public Form1()
        {
            InitializeComponent();
        }

        #region Classes
        public static class Global       //GLOBAL VARIABLES
        {
            //CONSTS-------------------------------------------------------------------------------------------------
            public const int tabCtrlWidth = 340;
            public const int borderLength = 18;
            public const int screenOptionsHeight = 85;

            public const int maxTileTypes = 2147483647;        //Excludes default tileTypes
            public const int maxScreens = 20;
            public const int maxPathPoints = 5;

            public const int minTileLength = 4;            //Includes +1 from gridline

            public static Color colourDefault = Color.BurlyWood;
            public static Color colourValid = Color.FromArgb(70, Color.LawnGreen);
            public static Color colourWorking = Color.FromArgb(50, Color.Yellow);
            public static Color colourInvalid = Color.FromArgb(50, Color.Red);

            //MAP----------------------------------------------------------------------------------------------------
            public static List<List<Tile>> lstMapRow = new List<List<Tile>>();            //[row][col] or [Y][X]

            //For drawing tiles
            public static int tileLength = 0;
            public static int noOfTilesX = 20;          //Default noOfTilesXY
            public static int noOfTilesY = 20;



            //SCREENS------------------------------------------------------------------------------------------------
            //List of pbx screen names
            public static List<string> lstScreens = new List<string>();

            public static int maxNoOfTilesX = 0;
            public static int maxNoOfTilesY = 0;


            //TILETYPES----------------------------------------------------------------------------------------------
            //Keeps track of dicTileType keys (As Dictionaries are Hashtables)
            public static List<string> lstDicTileTypeKeys = new List<string>();
            //Holds info on each tileType
            public static Dictionary<string, Tuple<Color, int>> dicTileTypeInfo = new Dictionary<string, Tuple<Color, int>>();
            //Holds list of tiles in map of certain tileTypes
            public static Dictionary<string, HashSet<string>> dicTileTypeTiles = new Dictionary<string, HashSet<string>>();

            //Unset custom tileType panels in tabTiles
            public static bool mapReadyToEdit = true;



            //MOUSE_HIGHLIGHTS---------------------------------------------------------------------------------------
            //Holds dictionary key
            public static string selectedTileTypeM1 = "";
            public static string selectedTileTypeM2 = "";

            //For tile highlight - px. where 0,0 on top left of overlay
            public static int newTileHighlightX = 0;
            public static int newTileHighlightY = 0;
            public static int curTileHighlightX = 0;
            public static int curTileHighlightY = 0;



            //PATHFINDING--------------------------------------------------------------------------------------------
            public static Color pathColour = Color.DarkRed;
            public static bool readyToPathfind = true;
            public static bool donePathfinding = false;

            //For adding points to find shortest path between
            public static bool allowEditPoints = false;
            public static List<string> lstPointsToPathfind = new List<string>();

            public static bool fastSearch = true;

            //TEXT OUTPUT--------------------------------------------------------------------------------------------
            public static int noOfIndents = 0;
            public static int indentFactor = 6;
        }

        public class Tile
        {
            //Name, TileType
            string tileCoords;
            string tileType;

            //Constructor
            public Tile(string _tileCoords, string _tileType)
            {
                tileCoords = _tileCoords;
                tileType = _tileType;
            }

            #region tileGetters
            public string getCoords()
            {
                return tileCoords;
            }

            public string getTileType()
            {
                return tileType;
            }

            //Refers to tileType dictionary
            public Color getColour()
            {
                Color tileColour = Global.dicTileTypeInfo[tileType].Item1;
                return tileColour;
            }

            public int getWeight()
            {
                int tileWeight = Global.dicTileTypeInfo[tileType].Item2;
                return tileWeight;
            }
            #endregion

            #region tileSetters
            public void setTileType(string _tileType)
            {
                tileType = _tileType;
            }
            #endregion

        }

        //For use in pathfinding algorithms that require sorting the openSet
        public class PriorityListDijkstra
        {
            //Tentative distance, Coords
            List<Tuple<int, string>> lst;

            public PriorityListDijkstra()
            {
                lst = new List<Tuple<int, string>>();
            }

            public void Add(Tuple<int, string> input)
            {
                lst.Add(input);
                int indexNo = lst.Count - 1;
                bool loopEnd = false;

                //Sort
                while (loopEnd == false && indexNo > 0)
                {
                    if (lst[indexNo].Item1 >= lst[indexNo - 1].Item1)
                    {
                        //Swap
                        Tuple<int, string> temp = lst[indexNo - 1];
                        lst[indexNo - 1] = lst[indexNo];
                        lst[indexNo] = temp;

                        indexNo--;
                    }
                    else
                    {
                        loopEnd = true;
                    }
                }
            }

            public Tuple<int, string> dequeue()
            {
                Tuple<int, string> output = lst[lst.Count - 1];
                lst.RemoveAt(lst.Count - 1);
                return output;
            }

            public bool Contains(string coords)
            {
                bool contains = false;

                for (int i = 0; i < lst.Count(); i++)
                {
                    if (lst[i].Item2 == coords)
                    {
                        contains = true;
                        i = lst.Count() - 1;
                    }
                }

                return contains;
            }

            public int Count()
            {
                return lst.Count();
            }

            public void Clear()
            {
                lst.Clear();
            }
        }

        public class PriorityListAStar
        {
            //G-Score, F-Score, Coords
            List<Tuple<int, int, string>> lst;

            public PriorityListAStar()
            {
                lst = new List<Tuple<int, int, string>>();
            }


            public void Add(Tuple<int, int, string> input)
            {
                lst.Add(input);
                int indexNo = lst.Count - 1;
                bool loopEnd = false;

                //Sort by f-score
                while (loopEnd == false && indexNo > 0)
                {
                    if (lst[indexNo].Item2 >= lst[indexNo - 1].Item2)
                    {
                        //Swap
                        Tuple<int, int, string> temp = lst[indexNo - 1];
                        lst[indexNo - 1] = lst[indexNo];
                        lst[indexNo] = temp;

                        indexNo--;
                    }
                    else
                    {
                        loopEnd = true;
                    }
                }
                loopEnd = false;
            }

            public Tuple<int, int, string> dequeue()
            {
                Tuple<int, int, string> output = lst[lst.Count - 1];
                lst.RemoveAt(lst.Count - 1);
                return output;
            }

            public bool Contains(string coords)
            {
                bool contains = false;

                for (int i = 0; i < lst.Count(); i++)
                {
                    if (lst[i].Item3 == coords)
                    {
                        contains = true;
                        i = lst.Count() - 1;
                    }
                }

                return contains;
            }

            public int Count()
            {
                return lst.Count();
            }

            public void Clear()
            {
                lst.Clear();
            }
        }

        #endregion

        #region Initialisation

        static void Main()
        {
            
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Maximized;

            InitialiseTileTypes();
            InitialiseMap();

            InitialiseControls();
            AddHandlers();

            //Draws default grid
            OutputMap();
        }

        private void InitialiseTileTypes()
        {
            Global.lstDicTileTypeKeys.Add("Empty");
            Global.lstDicTileTypeKeys.Add("Wall");

            Global.dicTileTypeInfo.Add(Global.lstDicTileTypeKeys[0], new Tuple<Color, int>(Color.White, 1));
            Global.dicTileTypeInfo.Add(Global.lstDicTileTypeKeys[1], new Tuple<Color, int>(Color.Black, -1));

            Global.dicTileTypeTiles.Add(Global.lstDicTileTypeKeys[0], new HashSet<string>());
            Global.dicTileTypeTiles.Add(Global.lstDicTileTypeKeys[1], new HashSet<string>());
        }

        private void InitialiseMap()
        {
            //Default starting map vals
            Global.noOfTilesX = 20;
            Global.noOfTilesY = 20;

            //Fill one row at a time - Values are colours
            for (int i = 0; i < Global.noOfTilesY; i++)
            {
                List<Tile> MapColumn = new List<Tile>();     //New column list for each row
                Tile newTile;

                //Fill current row with columns
                for (int j = 0; j < Global.noOfTilesX; j++)
                {
                    newTile = new Tile(j + "," + i, "Empty");
                    MapColumn.Add(newTile);
                    Global.dicTileTypeTiles["Empty"].Add(newTile.getCoords());
                }
                Global.lstMapRow.Add(MapColumn);
            }
        }

        #region initTabCtrl

        private void InitialiseControls()
        {
            pnlGrid.Location = new Point(30, 30);
            int pnlGridWidth = Screen.FromControl(this).WorkingArea.Width - pnlGrid.Location.X - Global.tabCtrlWidth - 31;
            int pnlGridHeight = Screen.FromControl(this).WorkingArea.Height - 15;
            pnlGrid.Size = new Size(pnlGridWidth, pnlGridHeight);

            tabCtrl.Location = new Point(Screen.FromControl(this).WorkingArea.Width - Global.tabCtrlWidth - 30, pnlGrid.Location.Y);
            tabCtrl.Size = new Size(Global.tabCtrlWidth, pnlGridHeight - 40);

            //Start pathfinding button
            Button btnStartPathfind = new Button();
            btnStartPathfind.Name = "btnStartPathfind";
            btnStartPathfind.Location = new Point(tabCtrl.Location.X, tabCtrl.Location.Y + tabCtrl.Height + 10);
            btnStartPathfind.Size = new Size(tabCtrl.Width, 30);
            btnStartPathfind.Text = "Start pathfinding";
            btnStartPathfind.Click += btnStartPathfind_Click;
            this.Controls.Add(btnStartPathfind);

            //tabScreen controls
            tabScreen.AutoScroll = true;
            initialiseTabScreen();

            //tabTiles controls
            tabTiles.AutoScroll = true;
            initialiseTabTiles();

            //tabOutput controls
            initialiseTabOutput();
        }

        private void addPnlAddSetCtrls(Panel pnl, string source)
        {
            //Button to add new tileTypes / Screens
            Button btnAdd = new Button();
            btnAdd.Font = new Font("Microsoft Sans Serif", 13.0f, FontStyle.Bold);
            btnAdd.Text = "+";
            btnAdd.Location = new Point(0, 0);
            btnAdd.Size = new Size(30, 30);

            pnl.Controls.Add(btnAdd);

            if (source == "SCREENS")
            {
                btnAdd.Name = "btnAddScreen";
                btnAdd.Click += btnAddScreen_Click;
            }
            else if (source == "TILETYPES")
            {
                //Button to set custom tileType options - Will only have one (initially invisible)
                Button btnSet = new Button();
                btnSet.Name = "btnSetTileTypes";       //Needed so we can refer to it when making it visible/invisible
                btnSet.Text = "Set";
                btnSet.Size = new Size(50, 30);
                btnSet.Location = new Point(226 - btnSet.Width, 0);       //Line up end of btn with end of textbox

                pnl.Controls.Add(btnSet);

                btnAdd.Name = "btnAddTileType";

                btnSet.Visible = false;        //Visible only when at least one custom tileType exists

                btnAdd.Click += btnAddTileType_Click;
                btnSet.Click += btnSetTileTypes_Click;
            }
        }

        #region initTabScreen
        private void initialiseTabScreen()
        {
            //Panel to set noOfTilesXY
            Panel pnlScreenSize = new Panel();
            initPnlScreenSize(pnlScreenSize);

            //Clear map button
            Button btnClearMap = new Button();
            btnClearMap.Text = "Clear Map";
            btnClearMap.AutoSize = true;
            btnClearMap.Location = new Point(tabCtrl.Width - btnClearMap.Width - 30, pnlScreenSize.Location.Y);
            tabScreen.Controls.Add(btnClearMap);
            btnClearMap.Click += btnClearMap_Click;

            //Maximise number of tiles button
            Button btnMaximise = new Button();
            btnMaximise.Text = "Maximise";
            btnMaximise.AutoSize = true;
            btnMaximise.Location = new Point(btnClearMap.Location.X, btnClearMap.Location.Y + 30);
            tabScreen.Controls.Add(btnMaximise);
            btnMaximise.Click += btnMaximiseNoOfTiles_Click;

            //For adding nodes to find shortest path between
            Panel pnlAddPoints = new Panel();
            pnlAddPoints.Name = "pnlAddPoints";
            pnlAddPoints.Location = new Point(pnlScreenSize.Location.X, pnlScreenSize.Location.Y + pnlScreenSize.Height + 10);
            initPnlAddPathPoints(pnlAddPoints);

            //Line to separate pnlScreenSize and Screen Options
            Label lblSeparator = new Label();
            lblSeparator.Name = "lblSeparator";
            lblSeparator.Location = new Point(30, pnlAddPoints.Location.Y + pnlAddPoints.Height);
            string strDashes = "";
            for (int i = 0; i < (tabCtrl.Width - 120) / 3; i++)
            {
                strDashes += "-";
            }
            lblSeparator.Text = strDashes;
            lblSeparator.AutoSize = true;
            tabScreen.Controls.Add(lblSeparator);

            //First screen - Default
            Panel pnlDefScreen = new Panel();
            pnlDefScreen.Name = "Screen0";
            pnlDefScreen.Location = new Point(30, lblSeparator.Location.Y + lblSeparator.Height + 10);
            pnlDefScreen.Size = new Size(tabCtrl.Width - 60, Global.screenOptionsHeight);
            pnlDefScreen.BackColor = Global.colourValid;

            //NOTE: For all screens, since all options are selected via combobox, there is no need to set algorithms
            addScreenPanelCtrls(pnlDefScreen);
            tabScreen.Controls.Add(pnlDefScreen);

            //Controls for adding/setting new tile types
            Panel pnlAddSet = new Panel();
            pnlAddSet.Name = "pnlAddSet";
            pnlAddSet.Location = new Point(30, pnlDefScreen.Location.Y + pnlDefScreen.Height);
            pnlAddSet.Size = new Size(tabCtrl.Width - 60, 30);
            addPnlAddSetCtrls(pnlAddSet, "SCREENS");
            tabScreen.Controls.Add(pnlAddSet);
        }

        private void initPnlScreenSize(Panel pnlScreenSize)
        {
            pnlScreenSize.Name = "pnlScreenSize";
            pnlScreenSize.Location = new Point(10, 10);
            pnlScreenSize.Size = new Size(200, 110);

            Label lblWidth = new Label();
            lblWidth.Text = "Width:";
            lblWidth.Location = new Point(20, 20);
            lblWidth.AutoSize = true;
            pnlScreenSize.Controls.Add(lblWidth);

            TextBox tbxWidth = new TextBox();
            tbxWidth.Name = "tbxWidth";
            tbxWidth.Text = Global.noOfTilesX.ToString();
            tbxWidth.Location = new Point(70, lblWidth.Location.Y - 2);
            pnlScreenSize.Controls.Add(tbxWidth);

            Label lblHeight = new Label();
            lblHeight.Text = "Height:";
            lblHeight.Location = new Point(20, lblWidth.Location.Y + 30);
            lblHeight.AutoSize = true;
            pnlScreenSize.Controls.Add(lblHeight);

            TextBox tbxHeight = new TextBox();
            tbxHeight.Name = "tbxHeight";
            tbxHeight.Text = Global.noOfTilesY.ToString();
            tbxHeight.Location = new Point(70, lblHeight.Location.Y - 2);
            pnlScreenSize.Controls.Add(tbxHeight);

            Button btnSetNoOfTiles = new Button();
            btnSetNoOfTiles.Text = "Set";
            btnSetNoOfTiles.Location = new Point(tbxHeight.Location.X, tbxHeight.Location.Y + 30);
            btnSetNoOfTiles.Size = new Size(tbxHeight.Width, 25);
            pnlScreenSize.Controls.Add(btnSetNoOfTiles);
            btnSetNoOfTiles.Click += btnSetNoOfTiles_Click;

            tabScreen.Controls.Add(pnlScreenSize);
        }

        private void initPnlAddPathPoints(Panel pnlAddPoints)
        {
            pnlAddPoints.Size = new Size(300, 60);

            Label lblAddPoints = new Label();
            lblAddPoints.Name = "lblAddPoints";
            lblAddPoints.Location = new Point(20, 0);
            lblAddPoints.Text = "Path points edit mode: OFF";
            lblAddPoints.AutoSize = true;
            pnlAddPoints.Controls.Add(lblAddPoints);

            Button btnAddPoints = new Button();
            btnAddPoints.Text = "Toggle add points";
            btnAddPoints.AutoSize = true;
            btnAddPoints.Location = new Point(20, 20);
            pnlAddPoints.Controls.Add(btnAddPoints);
            btnAddPoints.Click += btnAddPathPoints_Click;

            tabScreen.Controls.Add(pnlAddPoints);
        }

        private void addAlgorithmsToCmbBox(ComboBox cmb)
        {
            cmb.Items.Add("BFS");
            cmb.Items.Add("Dijkstra");
            cmb.Items.Add("A*");

            cmb.Text = "BFS";      //Default
        }

        #endregion

        #region initTabTiles
        private void initialiseTabTiles()
        {
            /*NOTES:
             * Each tile topLeft 70 px. apart in y (= customPanelHeight)
             * Each tile size (51, 51) px.
             * Labels and tile topLeft 70 px. apart in x
             * Label and textbox 6 spaces apart in x (for 'Name'): '      '
             * Name and weight label topLeft 25 px. apart in y
             * Panel's x ends at (-120 from tabCtrl.Width) +30 Location.X
             * TopLefts: pnlDefTileTypes = 30 px.
             *              lblSeparator = 169 px.
             *                 pnlAddSet = 199 px.
             *
             *POSSIBILE IMPROVEMENTS:
             * Have locations and sizes of controls in tabTiles as variables with default vals, but allow user to customise (MINIMUM SCREEN RES?)
             */

            //Default tile types
            addDefTileTypes();

            //Line to separate default and custom tileTypes
            Label lblSeparator = new Label();
            lblSeparator.Name = "lblSeparator";
            lblSeparator.Location = new Point(30, 168);
            string strDashes = "";
            for (int i = 0; i < (tabCtrl.Width - 120) / 3; i++)
            {
                strDashes += "-";
            }
            lblSeparator.Text = strDashes;
            lblSeparator.AutoSize = true;
            tabTiles.Controls.Add(lblSeparator);

            //Controls for adding/setting new tile types
            Panel pnlAddSet = new Panel();
            pnlAddSet.Name = "pnlAddSet";
            pnlAddSet.Location = new Point(30, lblSeparator.Location.Y + 30);
            pnlAddSet.Size = new Size(tabCtrl.Width - 60, 30);
            addPnlAddSetCtrls(pnlAddSet, "TILETYPES");
            tabTiles.Controls.Add(pnlAddSet);
        }

        private void addDefTileTypes()
        {
            //pnlEmpty
            Panel pnlEmpty = new Panel();
            pnlEmpty.Name = "Empty";
            pnlEmpty.Location = new Point(30, 30);
            pnlEmpty.Size = new Size(tabCtrl.Width - 60, 70);

            PictureBox pbxEmpty = new PictureBox();
            pbxEmpty.Name = "pbxEmpty";
            pbxEmpty.Size = new Size(51, 51);
            pbxEmpty.Location = new Point(0, 0);
            drawTileTypePbx(pbxEmpty, Global.dicTileTypeInfo["Empty"].Item1);
            pbxEmpty.MouseClick += tileTypeColour_Click;          //Move tileType selector on pbx click
            pnlEmpty.Controls.Add(pbxEmpty);

            Label lblEmptyName = new Label();
            lblEmptyName.AutoSize = true;
            lblEmptyName.Text = "Name:      Empty";
            lblEmptyName.Location = new Point(70, pbxEmpty.Location.Y);
            pnlEmpty.Controls.Add(lblEmptyName);

            Label lblEmptyWeight = new Label();
            lblEmptyWeight.AutoSize = true;
            lblEmptyWeight.Text = "Weight:    1";
            lblEmptyWeight.Location = new Point(70, pbxEmpty.Location.Y + 25);
            pnlEmpty.Controls.Add(lblEmptyWeight);

            tabTiles.Controls.Add(pnlEmpty);

            //pnlWall
            Panel pnlWall = new Panel();
            pnlWall.Name = "Wall";
            pnlWall.Location = new Point(30, 100);
            pnlWall.Size = new Size(tabCtrl.Width - 60, 70);

            PictureBox pbxWall = new PictureBox();
            pbxWall.Name = "pbxWall";
            pbxWall.Size = new Size(51, 51);
            pbxWall.Location = new Point(0, 0);
            drawTileTypePbx(pbxWall, Color.Black);
            pbxWall.MouseClick += tileTypeColour_Click;          //Move tileType selector on pbx click
            pnlWall.Controls.Add(pbxWall);

            Label lblWallName = new Label();
            lblWallName.AutoSize = true;
            lblWallName.Text = "Name:      Wall";
            lblWallName.Location = new Point(70, pbxWall.Location.Y);
            pnlWall.Controls.Add(lblWallName);

            Label lblWallWeight = new Label();
            lblWallWeight.AutoSize = true;
            lblWallWeight.Text = "Weight:    -1";
            lblWallWeight.Location = new Point(70, pbxWall.Location.Y + 25);
            pnlWall.Controls.Add(lblWallWeight);

            tabTiles.Controls.Add(pnlWall);

            initTileTypeSelector();
        }

        private void initTileTypeSelector()
        {
            //tileSelector M1 - Def: Wall
            Panel pnlWall = (Panel)tabTiles.Controls.Find("Wall", false).FirstOrDefault();

            Label lblTileSelectM1 = new Label();
            lblTileSelectM1.Name = "lblTileSelectM1";
            lblTileSelectM1.Font = new Font("Microsoft Sans Serif", 11.0f, FontStyle.Bold);
            lblTileSelectM1.ForeColor = Color.Red;
            lblTileSelectM1.Text = "1";
            lblTileSelectM1.AutoSize = true;
            lblTileSelectM1.Location = new Point(pnlWall.Location.X - 22, pnlWall.Location.Y + 5);

            Global.selectedTileTypeM1 = "Wall";
            tabTiles.Controls.Add(lblTileSelectM1);

            //tileSelector M2 - Def: Empty
            Panel pnlEmpty = (Panel)tabTiles.Controls.Find("Empty", false).FirstOrDefault();

            Label lblTileSelectM2 = new Label();
            lblTileSelectM2.Name = "lblTileSelectM2";
            lblTileSelectM2.Font = new Font("Microsoft Sans Serif", 11.0f, FontStyle.Bold);
            lblTileSelectM2.ForeColor = Color.Blue;
            lblTileSelectM2.Text = "2";
            lblTileSelectM2.AutoSize = true;
            lblTileSelectM2.Location = new Point(pnlEmpty.Location.X - 22, pnlEmpty.Location.Y + 25);

            Global.selectedTileTypeM2 = "Empty";
            tabTiles.Controls.Add(lblTileSelectM2);
        }

        private void drawTileTypePbx(PictureBox pbx, Color fillClr)
        {
            Bitmap bmp = new Bitmap(pbx.Width, pbx.Height);
            Graphics gfx = Graphics.FromImage(bmp);

            gfx.FillRectangle(new SolidBrush(fillClr), 0, 0, bmp.Width - 1, bmp.Height - 1);
            gfx.DrawRectangle(Pens.Black, 0, 0, bmp.Width - 1, bmp.Height - 1);
            pbx.Image = bmp;
        }

        #endregion

        #region initTabOutput
        private void initialiseTabOutput()
        {
            ListBox lstOutput = new ListBox();
            lstOutput.Enabled = false;
            lstOutput.Location = new Point(5, 5);
            lstOutput.Size = new Size(tabCtrl.Width - 20, tabCtrl.Height - 36);
            tabOutput.Controls.Add(lstOutput);
        }

        private void outputToTbxOutput(string output)
        {
            ListBox lstOutput = (ListBox)tabOutput.Controls[0];
            bool addExtraLine = false;

            if (output.StartsWith("..."))
            {
                Global.noOfIndents--;
                addExtraLine = (Global.noOfIndents == 0) ? true : false;
            }

            //Add indents to output
            int noOfSpaces = Global.indentFactor * Global.noOfIndents;
            string spaces = "";
            for (int i = 0; i < noOfSpaces; i++)
            {
                spaces += " ";
            }

            //Add items to listBox
            lstOutput.Items.Add(spaces + output);
            if (addExtraLine) { lstOutput.Items.Add(Environment.NewLine); }

            if (output.EndsWith("..."))
            {
                Global.noOfIndents++;
            }

            //Remove excess lines - Prevents vertical scrollbar from appearing
            while (lstOutput.Items.Count * lstOutput.ItemHeight > lstOutput.Height)
            {
                lstOutput.Items.RemoveAt(0);
            }
        }
        #endregion

        #endregion

        private void AddHandlers()
        {
            //Menu Bar - NEW
            menuBarNew.Click += menuBarNew_Click;
            menuBarImportImage.Click += menuBarImportImage_Click;
            menuBarExportBmp.Click += menuBarExportBmp_Click;
            menuBarExit.Click += menuBarExit_Click;

            //Menu Bar - EDIT
            menuBarClearMap.Click += btnClearMap_Click;
        }

        #endregion

        #region tabCtrlMethods

        #region tabScreenMethods
        private void btnSetNoOfTiles_Click(object sender, EventArgs e)
        {
            Button btnSet = (Button)sender;
            Panel pnlScreenSize = (Panel)btnSet.Parent;
            TextBox tbxWidth = (TextBox)pnlScreenSize.Controls.Find("tbxWidth", false).FirstOrDefault();
            TextBox tbxHeight = (TextBox)pnlScreenSize.Controls.Find("tbxHeight", false).FirstOrDefault();

            int noOfTilesX, noOfTilesY;

            string tbxOutput = string.Format("Attempting to change map size to {0} x {1}...", tbxWidth.Text, tbxHeight.Text);
            outputToTbxOutput(tbxOutput);

            //Checks if user inputs are integers first
            if (int.TryParse(tbxWidth.Text, out noOfTilesX))
            {
                if (int.TryParse(tbxHeight.Text, out noOfTilesY))
                {
                    noOfTilesValidation(noOfTilesX, noOfTilesY);
                }
                else
                {
                    tbxOutput = string.Format("Height '{0}' is not an integer", tbxHeight.Text);
                    outputToTbxOutput(tbxOutput);
                    tbxOutput = string.Format("...map size of {0} x {1} rejected", tbxWidth.Text, tbxHeight.Text);
                    outputToTbxOutput(tbxOutput);
                    MessageBox.Show("Invalid height");
                }
            }
            else
            {
                tbxOutput = string.Format("Width '{0}' is not an integer", tbxWidth.Text);
                outputToTbxOutput(tbxOutput);
                tbxOutput = string.Format("...map size of {0} x {1} rejected", tbxWidth.Text, tbxHeight.Text);
                outputToTbxOutput(tbxOutput);
                MessageBox.Show("Invalid width");
            }
        }

        private void noOfTilesValidation(int noOfTilesX, int noOfTilesY)
        {
            string tbxOutput = "";

            if (noOfTilesX != 0 && noOfTilesY != 0)
            {
                int oldNoOfTilesX = Global.noOfTilesX;
                int oldNoOfTilesY = Global.noOfTilesY;

                //Update current noOfTiles on map for trial fit
                Global.noOfTilesX = noOfTilesX;
                Global.noOfTilesY = noOfTilesY;
                Tuple<int, string> tileLengthStatus = trialFitScreen();
                int testTileLength = tileLengthStatus.Item1;
                string lastAction = tileLengthStatus.Item2;

                //Note: Revert must be made for 'RebuildMap' to work correctly
                Global.noOfTilesX = oldNoOfTilesX;
                Global.noOfTilesY = oldNoOfTilesY;

                if (testTileLength == -1)
                {
                    //Inform user what caused error by testing whether X/Y/Both caused tileLength to go below min
                    if (Global.lstScreens.Count == 1)
                    {
                        if (lastAction == "X")
                        {
                            tbxOutput = string.Format("Width '{0}' too large", noOfTilesX);
                            outputToTbxOutput(tbxOutput);
                            MessageBox.Show("Width too large");
                        }
                        else if (lastAction == "Y")
                        {
                            tbxOutput = string.Format("Width '{0}' too large", noOfTilesY);
                            outputToTbxOutput(tbxOutput);
                            MessageBox.Show("Height too large");
                        }
                        else if (lastAction == "XY")
                        {
                            tbxOutput = string.Format("Both width '{0}' and height '{1}' too large", noOfTilesX, noOfTilesY);
                            outputToTbxOutput(tbxOutput);
                            MessageBox.Show("Both width and height too large");
                        }
                    }
                    else
                    {
                        //Note: For multiple screens, it's too difficult to tell whether it's width/height/both that caused tileLength to go below minimum
                        tbxOutput = string.Format("Both width '{0}' and height '{1}' too large", noOfTilesX, noOfTilesY);
                        outputToTbxOutput(tbxOutput);
                        MessageBox.Show("Unable to change number of tiles");
                    }

                    tbxOutput = string.Format("...map size of {0} x {1} rejected", noOfTilesX, noOfTilesY);
                    outputToTbxOutput(tbxOutput);
                }
                else
                {
                    tbxOutput = string.Format("...map size of {0} x {1} accepted", noOfTilesX, noOfTilesY);
                    outputToTbxOutput(tbxOutput);

                    //Passed fitting
                    rebuildMap(noOfTilesX, noOfTilesY);

                    //Update current noOfTiles on map
                    Global.noOfTilesX = noOfTilesX;
                    Global.noOfTilesY = noOfTilesY;

                    //Accept testTileLength
                    Global.tileLength = testTileLength;

                    resetPathfinding();
                    placeScreens();
                    DrawFullGrid();
                }
            }
            else
            {
                tbxOutput = string.Format("Can't have width/height of 0");
                outputToTbxOutput(tbxOutput);
                tbxOutput = string.Format("...map size of {0} x {1} rejected", noOfTilesX, noOfTilesY);
                outputToTbxOutput(tbxOutput);
                MessageBox.Show("Width/Height cannot be 0");
            }
        }

        private void rebuildMap(int noOfColumns, int noOfRows)
        {
            //MAP REBUILD PSEUDOCODE - Row = Y, Column = X
            /* If more rows than original
             *     For each new row
             *          Add new no. of columns (tiles)
             *          Add each new tile to set of tiles of some tileType    
             * Else if less rows than original
             *     For each extra row
             *          Delete each tile of this row from dicTileTypeTiles
             *          Delete extra row
             * End If
             *
             * If more columns than original
             *     Add new columns (tiles) to original rows that are still within map
             *     Add each new tile to set of tiles of some tileType
             * Else if less columns than original
             *     For each original row that are still within map
             *          Delete extra columns (tiles)
             *          Delete each of these tiles from dicTileTypeTiles
             */

            string tbxOutput = "";
            tbxOutput = string.Format("Rebuilding map from {0} x {1} to {2} x {3}...", Global.noOfTilesX, Global.noOfTilesY, noOfColumns, noOfRows);
            outputToTbxOutput(tbxOutput);

            //Needed for columns if rows deleted, not all original rows will still be on map
            int originalRows = Global.noOfTilesY;

            //If more rows than original
            if (noOfRows > Global.noOfTilesY)
            {
                tbxOutput = string.Format("Too little rows, adding more");
                outputToTbxOutput(tbxOutput);
                addMapRows(noOfColumns, noOfRows);
            }
            //If less rows than original
            else if (noOfRows < Global.noOfTilesY)
            {
                tbxOutput = string.Format("Too many rows, removing more");
                outputToTbxOutput(tbxOutput);
                deleteMapRows(noOfColumns, noOfRows, ref originalRows);
            }

            //If more columns than original
            if (noOfColumns > Global.noOfTilesX)
            {
                tbxOutput = string.Format("Too little columns, adding more");
                outputToTbxOutput(tbxOutput);
                addMapColumns(noOfColumns, noOfRows, originalRows);
            }
            //If less columns than original
            else if (noOfColumns < Global.noOfTilesX)
            {
                tbxOutput = string.Format("Too many columns, removing some");
                outputToTbxOutput(tbxOutput);
                deleteMapColumns(noOfColumns, noOfRows, originalRows);
            }

            tbxOutput = string.Format("...done rebuilding map");
            outputToTbxOutput(tbxOutput);
        }

        private void addMapRows(int noOfColumns, int noOfRows)
        {
            //Add new rows with new no. of columns
            for (int row = Global.noOfTilesY; row < noOfRows; row++)
            {
                List<Tile> MapColumn = new List<Tile>();
                Tile newTile;

                for (int col = 0; col < noOfColumns; col++)
                {
                    newTile = new Tile(col + "," + row, "Empty");
                    MapColumn.Add(newTile);
                    Global.dicTileTypeTiles["Empty"].Add(newTile.getCoords());
                }

                Global.lstMapRow.Add(MapColumn);
            }
        }

        private void deleteMapRows(int noOfColumns, int noOfRows, ref int originalRows)
        {
            Tile tileDelete;

            //Delete extra rows
            for (int row = Global.noOfTilesY - 1; row >= noOfRows; row--)
            {
                //Delete tiles from dictTileTypeTiles
                for (int col = 0; col < Global.noOfTilesX; col++)
                {
                    tileDelete = Global.lstMapRow[row][col];
                    Global.dicTileTypeTiles[tileDelete.getTileType()].Remove(tileDelete.getCoords());
                }

                Global.lstMapRow.RemoveAt(row);
            }

            bool loopFinish = false;
            int coordsIndex = 0;

            //If there are points to pathfind
            if (Global.lstPointsToPathfind.Count() > 0)
            {
                //Delete path points that sat on deleted rows
                while (loopFinish == false)
                {
                    int row = int.Parse(Global.lstPointsToPathfind[coordsIndex].Split(',')[1]);
                    if (row > noOfRows - 1)
                    {
                        reorderStringList(ref Global.lstPointsToPathfind, coordsIndex);
                    }
                    else
                    {
                        coordsIndex++;
                    }

                    //If next index is beyond list bounds
                    if (coordsIndex == Global.lstPointsToPathfind.Count())
                    {
                        loopFinish = true;
                    }
                }
            }

            //Update original rows to original rows that still exit on map
            originalRows = noOfRows;
        }

        private void addMapColumns(int noOfColumns, int noOfRows, int originalRows)
        {
            //Add new columns to original rows
            for (int row = 0; row < originalRows; row++)
            {
                List<Tile> MapColumn = Global.lstMapRow[row];
                Tile newTile;

                for (int col = Global.noOfTilesX; col < noOfColumns; col++)
                {
                    newTile = new Tile(col + "," + row, "Empty");
                    MapColumn.Add(newTile);
                    Global.dicTileTypeTiles["Empty"].Add(newTile.getCoords());
                }
            }
        }

        private void deleteMapColumns(int noOfColumns, int noOfRows, int originalRows)
        {
            //Delete extra columns for each original row
            for (int row = 0; row < originalRows; row++)
            {
                List<Tile> MapColumn = Global.lstMapRow[row];
                Tile tileDelete;

                for (int col = Global.noOfTilesX - 1; col >= noOfColumns; col--)
                {
                    tileDelete = Global.lstMapRow[row][col];
                    MapColumn.RemoveAt(col);
                    Global.dicTileTypeTiles[tileDelete.getTileType()].Remove(tileDelete.getCoords());
                }
            }

            //If there are points to pathfind
            if (Global.lstPointsToPathfind.Count() > 0)
            {
                bool loopFinish = false;
                int coordsIndex = 0;

                //Delete path points that sat on deleted columns
                while (loopFinish == false)
                {
                    int col = int.Parse(Global.lstPointsToPathfind[coordsIndex].Split(',')[0]);
                    if (col > noOfColumns - 1)
                    {
                        reorderStringList(ref Global.lstPointsToPathfind, coordsIndex);
                    }
                    else
                    {
                        coordsIndex++;
                    }

                    //If next index is beyond list bounds
                    if (coordsIndex == Global.lstPointsToPathfind.Count())
                    {
                        loopFinish = true;
                    }
                }
            }
        }

        private void btnClearMap_Click(object sender, EventArgs e)
        {
            DialogResult newMapResult = MessageBox.Show("Clear the map?", "Clear Map", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (newMapResult == DialogResult.Yes)
            {
                string tbxOutput = "";

                tbxOutput = string.Format("Clearing map...");
                outputToTbxOutput(tbxOutput);

                //Holds coords to update on grid
                HashSet<string> hsetTilesToUpdate = new HashSet<string>();

                //Transfer coords from non-empty tiles to empty tile, create copy of transferred coords to hsetCoordsToUpdate, clear non-empty tile HashSet
                foreach (string tileType in Global.lstDicTileTypeKeys)
                {
                    if (tileType != "Empty")
                    {
                        foreach (string coords in Global.dicTileTypeTiles[tileType])
                        {
                            Tile tileToEdit = coordsToTile(coords);
                            tileToEdit.setTileType("Empty");

                            Global.dicTileTypeTiles["Empty"].Add(coords);
                            hsetTilesToUpdate.Add(coords);
                        }
                        Global.dicTileTypeTiles[tileType].Clear();
                    }
                }
                tbxOutput = string.Format("Set all tileTypes on map to \"Empty\"");
                outputToTbxOutput(tbxOutput);

                resetPathfinding();
                clearAllPathPoints();
                updateTilesOnMap(hsetTilesToUpdate);

                //Reset overlays
                foreach (Panel pnlScreen in pnlGrid.Controls)
                {
                    PictureBox pbxGrid = (PictureBox)pnlScreen.Controls[0];
                    initPbxGridOverlay(pbxGrid);
                }

                tbxOutput = string.Format("Reset all overlays of all screens");
                outputToTbxOutput(tbxOutput);

                tbxOutput = string.Format("...map cleared");
                outputToTbxOutput(tbxOutput);
            }
        }

        private void btnMaximiseNoOfTiles_Click(object sender, EventArgs e)
        {
            string tbxOutput = "";

            tbxOutput = string.Format("Maximising map size for {0} screens...", Global.lstScreens.Count());
            outputToTbxOutput(tbxOutput);

            rebuildMap(Global.maxNoOfTilesX, Global.maxNoOfTilesY);

            Global.noOfTilesX = Global.maxNoOfTilesX;
            Global.noOfTilesY = Global.maxNoOfTilesY;

            Global.tileLength = Global.minTileLength;

            //Change width and height textbox text
            Panel pnlScreenSize = (Panel)tabScreen.Controls.Find("pnlScreenSize", false).FirstOrDefault();
            TextBox tbxWidth = (TextBox)pnlScreenSize.Controls.Find("tbxWidth", false).FirstOrDefault();
            TextBox tbxHeight = (TextBox)pnlScreenSize.Controls.Find("tbxHeight", false).FirstOrDefault();

            tbxWidth.Text = Global.maxNoOfTilesX.ToString();
            tbxHeight.Text = Global.maxNoOfTilesY.ToString();

            resetPathfinding();
            placeScreens();
            DrawFullGrid();

            tbxOutput = string.Format("...map size maximised");
            outputToTbxOutput(tbxOutput);
        }

        private void btnAddPathPoints_Click(object sender, EventArgs e)
        {
            if (!Global.donePathfinding)
            {
                Button btnAddPoints = (Button)sender;
                Panel pnlAddPoints = (Panel)btnAddPoints.Parent;
                Label lblAddPoints = (Label)pnlAddPoints.Controls.Find("lblAddPoints", false).FirstOrDefault();

                string tbxOutput = "";

                //Pathfinding point placement mode indicator
                if (lblAddPoints.ForeColor != Color.Red)
                {
                    lblAddPoints.Text = "Path points edit mode: ON";
                    lblAddPoints.ForeColor = Color.Red;
                    Global.allowEditPoints = true;

                    tbxOutput = string.Format("Path points edit mode set to ON");
                    outputToTbxOutput(tbxOutput);
                }
                else
                {
                    lblAddPoints.Text = "Path points edit mode: OFF";
                    lblAddPoints.ForeColor = Color.Black;
                    Global.allowEditPoints = false;

                    tbxOutput = string.Format("Path points edit mode set to OFF");
                    outputToTbxOutput(tbxOutput);
                }
            }
        }

        private void btnAddScreen_Click(object sender, EventArgs e)
        {
            /*ADDING PROCEDURE:
             * -Create new screen and show on pnlGrid
             * -Create new panel for screen list
             * -Base new panel name off new screen name (Just hold list of names with a string list)
             * -Add controls to this panel
             */

            string tbxOutput = "";

            tbxOutput = string.Format("Attempting to add new screen...");
            outputToTbxOutput(tbxOutput);

            //Required for trialFitScreen - Reverted later on
            Global.lstScreens.Add("TEMP");

            Tuple<int, string> tileLengthStatus = trialFitScreen();
            int testTileLength = tileLengthStatus.Item1;

            Global.lstScreens.RemoveAt(Global.lstScreens.Count - 1);

            if (testTileLength != -1)
            {
                Button btnAdd = (Button)sender;
                Panel pnlAddSet = (Panel)btnAdd.Parent;

                Panel pnlScreenTab = new Panel();
                pnlScreenTab.Location = new Point(30, pnlAddSet.Location.Y);
                pnlScreenTab.Size = new Size(tabCtrl.Width - 60, Global.screenOptionsHeight);
                pnlScreenTab.BackColor = Global.colourValid;
                addScreenPanelCtrls(pnlScreenTab);
                tabScreen.Controls.Add(pnlScreenTab);

                //Moves add/set panel down
                pnlAddSet.Location = new Point(30, pnlAddSet.Location.Y + pnlScreenTab.Height);

                //Hide add button if max no. of screens reached
                if (Global.lstScreens.Count() == Global.maxScreens)
                {
                    tbxOutput = string.Format("Max no. of screens reached");
                    outputToTbxOutput(tbxOutput);

                    btnAdd.Visible = false;
                }

                Global.tileLength = testTileLength;

                resetPathfinding();
                placeScreens();
                DrawFullGrid();

                tbxOutput = string.Format("...new screen successfully added");
                outputToTbxOutput(tbxOutput);
            }
            else
            {
                tbxOutput = string.Format("...could not add new screen");
                outputToTbxOutput(tbxOutput);

                MessageBox.Show("Unable to add new screen");
            }
        }

        private void addScreenPanelCtrls(Panel pnl)
        {
            pnl.Name = "Screen" + Global.lstScreens.Count().ToString();

            Label lblScreenName = new Label();
            lblScreenName.Name = "lblScreenName";
            lblScreenName.Text = pnl.Name;
            lblScreenName.Font = new Font("Microsoft Sans Serif", 11.0f);
            lblScreenName.AutoSize = true;
            lblScreenName.Location = new Point(0, 0);
            lblScreenName.BackColor = Color.Transparent;
            pnl.Controls.Add(lblScreenName);

            Global.lstScreens.Add(lblScreenName.Text);

            Label lblAlgorithm = new Label();
            lblAlgorithm.Text = "Algorithm:";
            lblAlgorithm.AutoSize = true;
            lblAlgorithm.Location = new Point(30, lblScreenName.Height + 5);
            lblAlgorithm.BackColor = Color.Transparent;
            pnl.Controls.Add(lblAlgorithm);

            ComboBox cmbAlgorithm = new ComboBox();
            cmbAlgorithm.Name = "cmbAlgorithm";
            addAlgorithmsToCmbBox(cmbAlgorithm);
            cmbAlgorithm.AutoSize = true;
            cmbAlgorithm.Location = new Point(lblAlgorithm.Location.X + lblAlgorithm.Width + 10, lblAlgorithm.Location.Y);
            pnl.Controls.Add(cmbAlgorithm);
            cmbAlgorithm.SelectedIndexChanged += cmbAlgorithmChange;        //Handles changes to algorithm selection

            //If not default screen
            if (Global.lstScreens.Count() > 1)
            {
                //Delete button
                Button btnDeleteScreen = new Button();
                btnDeleteScreen.Font = new Font("Microsoft Sans Serif", 13.0f, FontStyle.Bold);
                btnDeleteScreen.Text = "x";
                btnDeleteScreen.Size = new Size(30, 30);
                btnDeleteScreen.Location = new Point(pnl.Width - btnDeleteScreen.Width, 0);
                btnDeleteScreen.BackColor = Color.Transparent;
                pnl.Controls.Add(btnDeleteScreen);
                btnDeleteScreen.Click += btnDeleteScreen_Click;
            }
        }

        private void btnDeleteScreen_Click(object sender, EventArgs e)
        {
            /*DELETION PROCEDURE:
             * -Delete panel from pnlGrid
             * -Delete panel from screen list, keeping track of index to delete
             * -Reorganise panels screen
             */
            Button btnDelete = (Button)sender;
            Panel pnlToDelete = (Panel)btnDelete.Parent;
            Panel pnlAddSet = (Panel)tabScreen.Controls.Find("pnlAddSet", false).FirstOrDefault();

            string tbxOutput = "";
            tbxOutput = string.Format("Deleting {0}...", pnlToDelete.Name);
            outputToTbxOutput(tbxOutput);

            //Make btnAdd in pnlAddSet to be visible again
            Button btnAdd = (Button)pnlAddSet.Controls.Find("btnAddScreen", false).FirstOrDefault();
            btnAdd.Visible = true;

            //"Screen" = 6 char
            int indexToDelete = int.Parse(pnlToDelete.Name.Substring(6, pnlToDelete.Name.Count() - 6));

            //Moves add/set panel up
            pnlAddSet.Location = new Point(30, pnlAddSet.Location.Y - pnlToDelete.Height);

            //Delete panel
            tabScreen.Controls.Remove(pnlToDelete);

            //Reorganise panel and screen list
            for (int i = indexToDelete; i < Global.lstScreens.Count() - 1; i++)
            {
                //Moves next panel up one slot
                Panel pnlToMove = (Panel)tabScreen.Controls.Find(Global.lstScreens[i + 1], false).FirstOrDefault();
                pnlToMove.Location = new Point(pnlToMove.Location.X, pnlToMove.Location.Y - pnlToMove.Height);

                //Renames screen based on new position
                string newScreenName = "Screen" + i.ToString();
                pnlToMove.Name = newScreenName;

                Label lblScreenName = (Label)pnlToMove.Controls.Find("lblScreenName", false).FirstOrDefault();
                lblScreenName.Text = newScreenName;

                Global.lstScreens[i] = newScreenName;
            }

            tbxOutput = string.Format("Screens reorganised");
            outputToTbxOutput(tbxOutput);

            //Remove last element in screen list
            Global.lstScreens.RemoveAt(Global.lstScreens.Count() - 1);

            //Refreshes pnlGrid and resets pathfinding
            OutputMap();
            resetPathfinding();

            tbxOutput = string.Format("...screen{0} deleted", indexToDelete);
            outputToTbxOutput(tbxOutput);
        }

        private void cmbAlgorithmChange(object sender, EventArgs e)
        {
            ComboBox cmbAlgorithm = (ComboBox)sender;
            Panel pnlScreenOptions = (Panel)cmbAlgorithm.Parent;

            //If A*, display heuristic options
            if (cmbAlgorithm.Text == "A*")
            {
                addHeuristicsOptions(pnlScreenOptions, cmbAlgorithm);
            }
            else
            {
                //Delete heuristic options, if available
                if (pnlScreenOptions.Controls.Find("cmbHeuristic", false).FirstOrDefault() != null)
                {
                    pnlScreenOptions.Controls.RemoveByKey("lblHeuristic");
                    pnlScreenOptions.Controls.RemoveByKey("cmbHeuristic");
                }
            }
        }

        private void addHeuristicsOptions(Panel pnlScreenOptions, ComboBox cmbAlgorithm)
        {
            Label lblHeuristic = new Label();
            lblHeuristic.Name = "lblHeuristic";
            lblHeuristic.Text = "Heuristic:";
            lblHeuristic.AutoSize = true;
            lblHeuristic.Location = new Point(30, cmbAlgorithm.Location.Y + 30);
            lblHeuristic.BackColor = Color.Transparent;
            pnlScreenOptions.Controls.Add(lblHeuristic);

            ComboBox cmbHeuristic = new ComboBox();
            cmbHeuristic.Name = "cmbHeuristic";
            addAStarHeuristicsToCmbBox(cmbHeuristic);
            cmbHeuristic.AutoSize = true;
            cmbHeuristic.Location = new Point(cmbAlgorithm.Location.X, lblHeuristic.Location.Y);
            pnlScreenOptions.Controls.Add(cmbHeuristic);
        }

        private void addAStarHeuristicsToCmbBox(ComboBox cmb)
        {
            cmb.Items.Add("Manhattan");
            cmb.Items.Add("Euclidean");
            cmb.Items.Add("Chebyshev");

            cmb.Text = "Manhattan";      //Default
        }

        #endregion

        #region tabTilesMethods
        private void btnAddTileType_Click(object sender, EventArgs e)
        {
            /* ALGORITHM:
             * Create new panel, put in correct location, and add required controls into it       
             * Create new tileType                                                                 
             * Increment panel counter by 1 (and also for unset panels)                            
             * Move pnlAddSet down to prepare for next new panel                                   
             */
            //Note: Click on panel (not on its controls) to select a tileType to place
            //      Click on pbx in panel to change tile colour

            string tbxOutput = "";

            Button btnAdd = (Button)sender;
            Panel pnlAddSet = (Panel)btnAdd.Parent;

            Panel pnlTileType = new Panel();
            pnlTileType.Size = new Size(tabCtrl.Width - 60, 70);
            pnlTileType.Location = new Point(30, pnlAddSet.Location.Y);

            pnlAddSet.Location = new Point(30, pnlAddSet.Location.Y + 70);     //Moves add/set panel down
            addTileTypePanelCtrls(pnlTileType);

            pnlTileType.BackColor = Global.colourWorking;

            //Create new tileType
            string pnlNumber = (Global.lstDicTileTypeKeys.Count).ToString();      //Includes Empty and Wall - Direct comparison to list index
            pnlTileType.Name = "pnlTileType" + pnlNumber;                         //Placeholder name
            Global.lstDicTileTypeKeys.Add(pnlTileType.Name);
            Global.dicTileTypeInfo.Add(pnlTileType.Name, new Tuple<Color, int>(Global.colourDefault, 0));    //Weight = 0 means tileType is not set

            //If one or more custom tileTypes exists, then btnSet is visible
            Button btnSet = (Button)pnlAddSet.Controls.Find("btnSetTileTypes", false).FirstOrDefault();
            btnSet.Visible = isBtnSetVisible();

            tabTiles.Controls.Add(pnlTileType);

            tbxOutput = string.Format("Added new tileType");
            outputToTbxOutput(tbxOutput);

            Global.mapReadyToEdit = false;

            //Hide add button if max tile types reached
            if (Global.lstDicTileTypeKeys.Count() - 2 == Global.maxTileTypes)
            {
                btnAdd.Visible = false;

                tbxOutput = string.Format("Max tileTypes reached");
                outputToTbxOutput(tbxOutput);
            }
        }

        private void moveTileTypeSelector(PictureBox pbx, string lblToMove)
        {
            Panel pnlClick = (Panel)pbx.Parent;
            Label lblTileSelect = (Label)tabTiles.Controls.Find(lblToMove, false).FirstOrDefault();

            if (lblToMove == "lblTileSelectM1")
            {
                Global.selectedTileTypeM1 = pnlClick.Name;

                lblTileSelect.Location = new Point(pnlClick.Location.X - 22, pnlClick.Location.Y + 5);

                string tbxOutput = "";
                tbxOutput = string.Format("Selected tileType 1 changed to tileType '{0}'", pnlClick.Name);
                outputToTbxOutput(tbxOutput);
            }
            else if (lblToMove == "lblTileSelectM2")
            {
                Global.selectedTileTypeM2 = pnlClick.Name;

                lblTileSelect.Location = new Point(pnlClick.Location.X - 22, pnlClick.Location.Y + 25);

                string tbxOutput = "";
                tbxOutput = string.Format("Selected tileType 2 changed to tileType '{0}'", pnlClick.Name);
                outputToTbxOutput(tbxOutput);
            }
        }

        private bool isBtnSetVisible()
        {
            bool isVisible = false;

            if (Global.lstDicTileTypeKeys.Count() > 2)
            {
                isVisible = true;
            }

            return isVisible;
        }

        private void addTileTypePanelCtrls(Panel pnl)
        {
            //Draw new tile type
            PictureBox pbxNewTile = new PictureBox();
            pbxNewTile.Name = "pbxColour";
            pbxNewTile.Location = new Point(0, 0);
            pbxNewTile.Size = new Size(51, 51);     //FIXED - Includes grid line
            pbxNewTile.Tag = Global.colourDefault;       //Default colour
            drawTileTypePbx(pbxNewTile, (Color)pbxNewTile.Tag);
            pbxNewTile.MouseClick += tileTypeColour_Click;          //Select tileType / Change tileType colour
            pnl.Controls.Add(pbxNewTile);

            //Draw labels and textboxes - Note: Setting backColor so changes to backColor in panel doesn't affect those in child controls
            Label lblNewName = new Label();
            lblNewName.Text = "Name:      ";
            lblNewName.AutoSize = true;
            lblNewName.Location = new Point(70, 0);
            lblNewName.BackColor = Color.Transparent;
            pnl.Controls.Add(lblNewName);

            TextBox tbxNewName = new TextBox();
            tbxNewName.Name = "tbxName";
            tbxNewName.AutoSize = true;
            tbxNewName.Location = new Point(70 + lblNewName.Width, 0);
            tbxNewName.TextChanged += tbxTileType_TextChanged;       //To update status of tileType to unset
            pnl.Controls.Add(tbxNewName);


            Label lblNewWeight = new Label();
            lblNewWeight.Text = "Weight:    ";
            lblNewWeight.AutoSize = true;
            lblNewWeight.Location = new Point(70, 25);
            lblNewWeight.BackColor = Color.Transparent;
            pnl.Controls.Add(lblNewWeight);

            TextBox tbxNewWeight = new TextBox();
            tbxNewWeight.Name = "tbxWeight";
            tbxNewWeight.AutoSize = true;
            tbxNewWeight.Location = new Point(70 + lblNewWeight.Width, 25);
            tbxNewWeight.TextChanged += tbxTileType_TextChanged;       //To update status of tileType to unset
            pnl.Controls.Add(tbxNewWeight);

            //Delete button
            Button btnDeleteTileType = new Button();
            btnDeleteTileType.Font = new Font("Microsoft Sans Serif", 13.0f, FontStyle.Bold);
            btnDeleteTileType.Text = "x";
            btnDeleteTileType.Size = new Size(30, 30);
            btnDeleteTileType.Location = new Point(pnl.Width - btnDeleteTileType.Width, 0);
            btnDeleteTileType.BackColor = Color.Transparent;
            pnl.Controls.Add(btnDeleteTileType);
            btnDeleteTileType.Click += btnDeleteTileType_Click;
        }

        private void tbxTileType_TextChanged(object sender, EventArgs e)
        {
            TextBox tbx = (TextBox)sender;
            Panel pnl = (Panel)tbx.Parent;

            if (pnl.BackColor != Global.colourWorking)
            {
                pnl.BackColor = Global.colourWorking;

                Global.mapReadyToEdit = false;

                string tbxOutput = "";
                tbxOutput = string.Format("Text change detected in tileType '{0}'. Unsetting tileType", pnl.Name);
                outputToTbxOutput(tbxOutput);
            }
        }

        private void tileTypeColour_Click(object sender, MouseEventArgs e)
        {
            PictureBox pbx = (PictureBox)sender;

            //M1 Click - Select tileType M1
            if (e.Button == MouseButtons.Left)
            {
                Panel pnl = (Panel)pbx.Parent;
                if (pnl.BackColor == Global.colourValid || pnl.Name == "Empty" || pnl.Name == "Wall")
                {
                    moveTileTypeSelector(pbx, "lblTileSelectM1");
                }
            }

            //M2 Click - Select tileType M2
            else if (e.Button == MouseButtons.Right)
            {
                Panel pnl = (Panel)pbx.Parent;
                if (pnl.BackColor == Global.colourValid || pnl.Name == "Empty" || pnl.Name == "Wall")
                {
                    moveTileTypeSelector(pbx, "lblTileSelectM2");
                }
            }

            //M3 Click - Change tileType colour
            //TEMPORARY: ONLY CUSTOM TILE TYPES CAN HAVE COLOUR CHANGED
            else if (e.Button == MouseButtons.Middle && pbx.Name != "pbxEmpty" && pbx.Name != "pbxWall")
            {
                Color pbxOriginalColour = (Color)pbx.Tag;

                //Sets tileType colour based on chosen colour in clrPicker
                Color pbxNewColour;
                ColorDialog clrPicker = new ColorDialog();

                if (clrPicker.ShowDialog() == DialogResult.OK)
                {
                    pbxNewColour = clrPicker.Color;
                    pbx.Tag = pbxNewColour;        //Tag colour on pbx so we can refer to it when updating
                    drawTileTypePbx(pbx, pbxNewColour);

                    //Updates panel status
                    if (pbxNewColour != pbxOriginalColour)
                    {
                        Panel pnl = (Panel)pbx.Parent;

                        string tbxOutput = "";
                        tbxOutput = string.Format("TileType '{0}' colour changed to '{1}'", pnl.Name, pbxNewColour);
                        outputToTbxOutput(tbxOutput);

                        //If previously set and just updating tile colour, allow insta-update
                        if (pnl.BackColor == Global.colourValid)
                        {
                            updateTileType(Global.lstDicTileTypeKeys.IndexOf(pnl.Name), pnl.Name);
                            tbxOutput = string.Format("Updated colour of all tiles with tileType {0} on map", pnl.Name);
                            outputToTbxOutput(tbxOutput);
                        }
                    }
                }
            }
        }

        private void btnSetTileTypes_Click(object sender, EventArgs e)
        {
            /* ALGORITHM:
             * For each custom panel in list
             *     If pass (Validation Check)
             *         Recreate dictionaries
             *         Update list key name
             *         Update panel name  
             *         Change panel backcolour to green
             *         Recolour tiles of updated tileType
             *     Else
             *         Change panel backcolour to red
             *       
             * If all panels green
             *      Map ready to edit
             */

            //RED = INVALID, YELLOW = PENDING, GREEN = VALID, SET  For back colours
            //(STILL NEED WEIGHTS = 0 THOUGH)

            string pnlKey = "";
            int pnlIndex = 2;       //Starts loop from start of custom tile types (Note: No need to check if custom tileTypes exist as btnSet won't be visible)
            bool loopFinish = false;

            string tbxOutput = "";
            tbxOutput = string.Format("Setting unset tileTypes...");
            outputToTbxOutput(tbxOutput);

            //Need this as upper bound of loop will be dynamic
            while (loopFinish == false)
            {
                pnlKey = Global.lstDicTileTypeKeys[pnlIndex];
                updateTileType(pnlIndex, pnlKey);
                pnlIndex++;

                //If next index is beyond list bounds
                if (pnlIndex == Global.lstDicTileTypeKeys.Count)
                {
                    loopFinish = true;
                }
            }

            tbxOutput = string.Format("...valid unset tileTypes set");
            outputToTbxOutput(tbxOutput);

            checkMapReadyToEdit();
        }

        private void updateTileType(int pnlIndex, string pnlKey)
        {
            Panel pnl = (Panel)tabTiles.Controls.Find(pnlKey, false).FirstOrDefault();
            string tbxOutput = "";

            //If not set and inputs are valid
            if (isTileTypeInputValid(pnlKey) == true)
            {
                tbxOutput = string.Format("Updating tileType '{0}'...", pnlKey);
                outputToTbxOutput(tbxOutput);

                TextBox tbxName = (TextBox)pnl.Controls.Find("tbxName", false).FirstOrDefault();
                TextBox tbxWeight = (TextBox)pnl.Controls.Find("tbxWeight", false).FirstOrDefault();
                PictureBox pbxColour = (PictureBox)pnl.Controls.Find("pbxColour", false).FirstOrDefault();

                Color prevColour = Global.dicTileTypeInfo[pnlKey].Item1;
                Color newColour = (Color)pbxColour.Tag;

                HashSet<string> hsetTiles = new HashSet<string>();

                //If tile to be updated is selected tile
                if (Global.selectedTileTypeM1 == pnlKey)
                {
                    Global.selectedTileTypeM1 = tbxName.Text;
                }

                //Update list of keys
                Global.lstDicTileTypeKeys[pnlIndex] = tbxName.Text;

                //If there is previous entry in dicTileTypeTiles - Move HashSet out, delete dictionary entry
                if (Global.dicTileTypeTiles.ContainsKey(pnlKey))
                {
                    hsetTiles = Global.dicTileTypeTiles[pnlKey];
                    Global.dicTileTypeTiles.Remove(pnlKey);
                    //Update tileType values of existing tiles in map
                    foreach (string coords in hsetTiles)
                    {
                        Tile tileToUpdate = coordsToTile(coords);
                        tileToUpdate.setTileType(tbxName.Text);
                    }
                }

                //Create new dicTileTypeTiles entry
                Global.dicTileTypeTiles.Add(tbxName.Text, hsetTiles);

                //Recreate dicTileTypeInfo entry
                Global.dicTileTypeInfo.Remove(pnlKey);
                Global.dicTileTypeInfo.Add(tbxName.Text, new Tuple<Color, int>((Color)pbxColour.Tag, int.Parse(tbxWeight.Text)));

                tbxOutput = string.Format("TileType '{0}' name changed to '{1}'", pnlKey, tbxName.Text);
                outputToTbxOutput(tbxOutput);
                tbxOutput = string.Format("TileType '{0}' weight set to {1}", tbxName.Text, tbxWeight.Text);
                outputToTbxOutput(tbxOutput);

                if (prevColour != newColour)
                {
                    //Update pbx colour
                    drawTileTypePbx(pbxColour, (Color)pbxColour.Tag);

                    tbxOutput = string.Format("TileType '{0}' colour set to '{1}'", tbxName.Text, pbxColour.Tag);
                    outputToTbxOutput(tbxOutput);

                    //Redraw existing tiles on grid
                    if (hsetTiles.Count() > 0)
                    {
                        recolourTiles(pnl.Name);
                    }
                }

                //Update panel
                pnl.Name = tbxName.Text;
                pnl.BackColor = Global.colourValid;

                tbxOutput = string.Format("...tileType updated");
                outputToTbxOutput(tbxOutput);
            }
            else
            {
                pnl.BackColor = Global.colourInvalid;

                //If tileTypeSelectorM1 is on updated invalid tileType, move tileTypeSelector to 'Wall' tileType
                int indexSelectedTile = Global.lstDicTileTypeKeys.IndexOf(Global.selectedTileTypeM1);
                if (indexSelectedTile == pnlIndex)
                {
                    resetTileTypeSelectorM1();
                }

                //If tileTypeSelectorM2 is on updated invalid tileType, move tileTypeSelector to 'Empty' tileType
                indexSelectedTile = Global.lstDicTileTypeKeys.IndexOf(Global.selectedTileTypeM2);
                if (indexSelectedTile == pnlIndex)
                {
                    resetTileTypeSelectorM2();
                }
            }
        }

        private bool isTileTypeInputValid(string pnlKey)
        {
            string tbxOutput = "";
            tbxOutput = string.Format("Validating tileType '{0}'...", pnlKey);
            outputToTbxOutput(tbxOutput);

            Panel pnl = (Panel)tabTiles.Controls.Find(pnlKey, false).FirstOrDefault();
            TextBox tbxName = (TextBox)pnl.Controls.Find("tbxName", false).FirstOrDefault();
            TextBox tbxWeight = (TextBox)pnl.Controls.Find("tbxWeight", false).FirstOrDefault();

            //Check name
            bool nameValid = checkTileTypeName(tbxName.Text, pnl.Name);

            //Check weight
            bool weightValid = checkTileTypeWeight(tbxWeight.Text, pnl.Name);

            //Check if all inputs valid
            bool isValid = false;
            if (nameValid && weightValid)
            {
                isValid = true;
            }

            //Output to log

            if (isValid)
            {
                tbxOutput = string.Format("...tileType '{0}' is valid", pnl.Name);
            }
            else
            {
                tbxOutput = string.Format("...tileType '{0}' is not valid", pnl.Name);
            }
            outputToTbxOutput(tbxOutput);

            return isValid;
        }

        private bool checkTileTypeName(string tileTypeName, string pnlName)
        {
            bool nameValid = false;
            string tbxOutput = "";

            if (tileTypeName.ToUpper() != "EMPTY" && tileTypeName.ToUpper() != "WALL")          //Disallow name to be default names
            {
                //If updating weight only
                if (tileTypeName == pnlName)
                {
                    nameValid = true;
                }
                //If new name and tbx is blank
                else if (tileTypeName.Trim() != "")
                {
                    //If new name not taken
                    if (Global.lstDicTileTypeKeys.Contains(tileTypeName) == false)
                    {
                        nameValid = true;
                    }
                    else
                    {
                        tbxOutput = string.Format("tileType '{0}' has an already taken name", pnlName);
                        outputToTbxOutput(tbxOutput);
                    }
                }
                else
                {
                    tbxOutput = string.Format("tileType '{0}' has blank name", pnlName);
                    outputToTbxOutput(tbxOutput);
                }
            }
            else
            {
                tbxOutput = string.Format("tileType '{0}' has reserved name", pnlName);
                outputToTbxOutput(tbxOutput);
            }

            return nameValid;
        }

        private bool checkTileTypeWeight(string tileTypeWeight, string pnlName)
        {
            bool weightValid = false;
            string tbxOutput = "";

            //Check if numeric
            int parseResult = 0;
            if (int.TryParse(tileTypeWeight, out parseResult))
            {
                //Check if within bounds
                if (parseResult > 0)
                {
                    weightValid = true;
                }
                else
                {
                    tbxOutput = string.Format("tileType '{0}' has negative weight", pnlName);
                    outputToTbxOutput(tbxOutput);
                }
            }
            else
            {
                tbxOutput = string.Format("tileType '{0}' has non-integer weight", pnlName);
                outputToTbxOutput(tbxOutput);
            }

            return weightValid;
        }

        private void recolourTiles(string tileTypeKey)
        {
            HashSet<string> tileCoords = Global.dicTileTypeTiles[tileTypeKey];
            updateTilesOnMap(tileCoords);
        }

        private void btnDeleteTileType_Click(object sender, EventArgs e)
        {
            /* ALGORITHM:
            *  Notify user that tiles of this type on grid will be deleted
            *If 'OK',
            *   Delete tiles from map (if any) - Set to default
            *   Obtain indexToDelete by [(btnDelete.Y - 1stpnl.TopLeft.Y) / pnlHeight]      {1st pnlTopLeft = 190}     
            *   Move pnlAddSet up                                                                                      
            *   Delete panel (parent of btnDelete) from tabTiles                                                       
            *   Decrement panel counter by 1                                                                           
            *   Use indexToDelete to delete key from dictionary                                                        
            *   For all index higher than (index to delete) in list                                                    
            *         Move panel upwards in tabTiles                                                                   
            *         Rename unset panels that have moved and recreate in dictionary                                   
            *         Move panel index in list by 1 towards 0                                                          
            *   Delete last element of list of keys (lst.Count - 1)                                                    
            *   Determine if btnSet will be visible or not                                                             
            */
            Button btnDelete = (Button)sender;
            Panel pnlToDelete = (Panel)btnDelete.Parent;
            Panel pnlAddSet = (Panel)tabTiles.Controls.Find("pnlAddSet", false).FirstOrDefault();

            string tbxOutput = "";
            tbxOutput = string.Format("Deleting tileType '{0}'...", pnlToDelete.Name);
            outputToTbxOutput(tbxOutput);

            //Make btnAdd in pnlAddSet to be visible again
            Button btnAdd = (Button)pnlAddSet.Controls.Find("btnAddTileType", false).FirstOrDefault();
            btnAdd.Visible = true;

            string keyToDelete = pnlToDelete.Name;
            int indexToDelete = Global.lstDicTileTypeKeys.IndexOf(pnlToDelete.Name);

            //Moves add/set panel up
            pnlAddSet.Location = new Point(30, pnlAddSet.Location.Y - 70);

            //If tileTypeSelectorM1 is on tileTypes to move, move tileTypeSelector to 'Wall' tileType
            int indexSelectedTile = Global.lstDicTileTypeKeys.IndexOf(Global.selectedTileTypeM1);
            if (indexSelectedTile >= indexToDelete)
            {
                resetTileTypeSelectorM1();
            }

            //If tileTypeSelectorM2 is on tileTypes to move, move tileTypeSelector to 'Empty' tileType
            indexSelectedTile = Global.lstDicTileTypeKeys.IndexOf(Global.selectedTileTypeM2);
            if (indexSelectedTile >= indexToDelete)
            {
                resetTileTypeSelectorM2();
            }

            //Delete panel
            tabTiles.Controls.Remove(pnlToDelete);

            //If tileType previously set
            if (Global.dicTileTypeTiles.ContainsKey(keyToDelete))
            {
                //Set tiles of to-be-deleted tileType to "Empty" tileType
                foreach (string tileCoords in Global.dicTileTypeTiles[keyToDelete])
                {
                    Tile tileToDelete = coordsToTile(tileCoords);
                    tileToDelete.setTileType("Empty");
                }

                recolourTiles(keyToDelete);
                Global.dicTileTypeTiles.Remove(keyToDelete);
            }

            //Delete tileTypeInfo from dictionary
            Global.dicTileTypeInfo.Remove(keyToDelete);

            //Reorganise list, dict, and panels in tabTiles
            reorganiseTileTypes(indexToDelete);

            //If one or more custom tileTypes exists, then btnSet is visible
            Button btnSet = (Button)pnlAddSet.Controls.Find("btnSetTileTypes", false).FirstOrDefault();
            btnSet.Visible = isBtnSetVisible();

            tbxOutput = string.Format("...tileType '{0}' deleted", keyToDelete);
            outputToTbxOutput(tbxOutput);

            checkMapReadyToEdit();
        }

        private void reorganiseTileTypes(int indexToDelete)
        {
            for (int i = indexToDelete; i < Global.lstDicTileTypeKeys.Count() - 1; i++)
            {
                Panel pnlToMove = (Panel)tabTiles.Controls.Find(Global.lstDicTileTypeKeys[i + 1], false).FirstOrDefault();

                //Recreating key for dictionary - If unset
                if (Global.dicTileTypeInfo[pnlToMove.Name].Item2 == 0)
                {
                    //Deletes old name
                    Global.dicTileTypeInfo.Remove(pnlToMove.Name);

                    //Generates new name based on new index in list and recreates dictionary key
                    string pnlNumber = i.ToString();
                    pnlToMove.Name = "pnlTileType" + pnlNumber;
                    Global.dicTileTypeInfo.Add(pnlToMove.Name, new Tuple<Color, int>(Global.colourDefault, 0));
                }

                //Shifting index in list
                Global.lstDicTileTypeKeys[i] = pnlToMove.Name;

                //Moves panel up 1 slot
                pnlToMove.Location = new Point(30, pnlToMove.Location.Y - 70);
            }

            //Remove last element in list
            Global.lstDicTileTypeKeys.RemoveAt(Global.lstDicTileTypeKeys.Count() - 1);

            string tbxOutput = "";
            tbxOutput = string.Format("Tiletypes reorganised");
            outputToTbxOutput(tbxOutput);
        }

        private void checkMapReadyToEdit()
        {
            if (!Global.donePathfinding)
            {
                //Assumes ready to edit until non-green panel found
                Global.mapReadyToEdit = true;
                string tbxOutput = "";

                //Loops through all tileTypes (excluding defaults)
                for (int i = 2; i < Global.lstDicTileTypeKeys.Count(); i++)
                {
                    string pnlName = Global.lstDicTileTypeKeys[i];
                    Panel pnl = (Panel)tabTiles.Controls.Find(pnlName, false).FirstOrDefault();

                    if (pnl.BackColor != Global.colourValid)
                    {
                        Global.mapReadyToEdit = false;
                        i = Global.lstDicTileTypeKeys.Count() - 1;
                    }
                }

                if (Global.mapReadyToEdit)
                {
                    tbxOutput = string.Format("Map ready to edit");
                }
                else
                {
                    tbxOutput = string.Format("Map not ready to edit");
                }
                outputToTbxOutput(tbxOutput);
            }
        }
        #endregion

        #endregion

        #region BestFitScreen
        private void BestFitPbx()
        {
            //Note: This method fits screens, regardless of tileLength       
            Tuple<int, string> tileLengthStatus = trialFitScreen();
            Global.tileLength = tileLengthStatus.Item1;
            placeScreens();
        }

        private Tuple<int, string> trialFitScreen()
        {
            string tbxOutput = "";
            tbxOutput = string.Format("Trial fitting screens by attempting to create enough slots...");
            outputToTbxOutput(tbxOutput);

            //Note: 'lastAction' is used to determine whether width/height/both caused tileLength to go below minimum - For 1 screen only

            //To stop 'fitting' if tileLength goes below minimum
            bool belowMinTilelength = false;

            Tuple<int, string> tileLengthStatus = getInitTileLength();
            int testTileLength = tileLengthStatus.Item1;
            string lastAction = tileLengthStatus.Item2;

            //Initial tileLength checker
            if (testTileLength < Global.minTileLength)
            {
                belowMinTilelength = true;

                tbxOutput = string.Format("Trial tileLength has gone less than minimum tileLength");
                outputToTbxOutput(tbxOutput);
            }
            else
            {
                //Initial total number of slots
                int slotLength = (Global.noOfTilesX * testTileLength) + Global.borderLength + 1;
                int layerLength = (Global.noOfTilesY * testTileLength) + Global.borderLength + 1;
                int slotsPerLayer = (int)Math.Floor((decimal)pnlGrid.Width / slotLength);
                int noOfLayers = (int)Math.Floor((decimal)pnlGrid.Height / layerLength);

                int totNoOfSlots = slotsPerLayer * noOfLayers;

                //Gradually adds slots to pnlGrid until enough to fit all screens in
                while (totNoOfSlots < Global.lstScreens.Count() && belowMinTilelength == false)
                {
                    int slotTileLength = (int)Math.Floor((1 / (decimal)Global.noOfTilesX) * (((decimal)pnlGrid.Width / (slotsPerLayer + 1)) - Global.borderLength - 1));
                    int layerTileLength = (int)Math.Floor((1 / (decimal)Global.noOfTilesY) * (((decimal)pnlGrid.Height / (noOfLayers + 1)) - Global.borderLength - 1));

                    //Finds out whether new slot or new layer comes first as tileLength decreases and sets tileLength this value
                    if (slotTileLength > layerTileLength)
                    {
                        testTileLength = slotTileLength;
                        totNoOfSlots += noOfLayers;
                        slotsPerLayer++;
                    }
                    else if (slotTileLength < layerTileLength)
                    {
                        testTileLength = layerTileLength;
                        totNoOfSlots += slotsPerLayer;
                        noOfLayers++;
                    }
                    else if (slotTileLength == layerTileLength)
                    {
                        testTileLength = slotTileLength;
                        slotsPerLayer++;
                        totNoOfSlots += noOfLayers;
                        noOfLayers++;
                        totNoOfSlots += slotsPerLayer;
                    }

                    //TileLength checker
                    if (testTileLength < Global.minTileLength)
                    {
                        belowMinTilelength = true;

                        tbxOutput = string.Format("Trial tileLength has gone less than minimum tileLength");
                        outputToTbxOutput(tbxOutput);
                    }
                }
            }

            if (belowMinTilelength)
            {
                //Failed = -1, Pass = Some +ve val > minTileLength
                testTileLength = -1;

                tbxOutput = string.Format("...trial fitting failed");
                outputToTbxOutput(tbxOutput);
            }
            else
            {
                tbxOutput = string.Format("...trial fitting succeeded");
                outputToTbxOutput(tbxOutput);
            }

            tileLengthStatus = new Tuple<int, string>(testTileLength, lastAction);
            return tileLengthStatus;
        }

        private void placeScreens()
        {
            //Creates and places screens into slots
            int slotLength = (Global.noOfTilesX * Global.tileLength) + Global.borderLength + 1;
            int layerLength = (Global.noOfTilesY * Global.tileLength) + Global.borderLength + 1;
            int slotsPerLayer = (int)Math.Floor((decimal)pnlGrid.Width / slotLength);
            int slotX = 0;
            int slotY = 0;

            //Clears existing screens
            pnlGrid.Controls.Clear();
            for (int i = 0; i < Global.lstScreens.Count(); i++)
            {
                Point screenLocation = new Point(slotX * slotLength, slotY * layerLength);
                Size screenSize = new Size(slotLength, layerLength);

                Panel pnlScreen = createScreen("Screen" + i, screenLocation, screenSize);
                pnlGrid.Controls.Add(pnlScreen);

                //Moves to next slot if not last screen
                if (i < Global.lstScreens.Count() - 1)
                {
                    slotX++;
                    if (slotX == slotsPerLayer)
                    {
                        slotY++;
                        slotX = 0;
                    }
                }
            }

            //Update maxNoOfTilesXY for current screen state
            //Note: Equations derived from noOfScreensX = pnlWidth / screenWidth
            int noOfLayers = (int)Math.Ceiling((decimal)Global.lstScreens.Count() / slotsPerLayer);
            Global.maxNoOfTilesX = (int)Math.Floor(((decimal)1 / Global.minTileLength) * (((decimal)pnlGrid.Width / slotsPerLayer) - Global.borderLength - 1));
            Global.maxNoOfTilesY = (int)Math.Floor(((decimal)1 / Global.minTileLength) * (((decimal)pnlGrid.Height / noOfLayers) - Global.borderLength - 1));

            string tbxOutput = "";
            tbxOutput = string.Format("Screens placed into slots");
            outputToTbxOutput(tbxOutput);
        }
        #endregion

        #region DrawGrid
        private void DrawFullGrid()
        {
            string tbxOutput = "";
            tbxOutput = string.Format("Drawing full grid to screen...");
            outputToTbxOutput(tbxOutput);

            Bitmap bmp = new Bitmap((Global.noOfTilesX * Global.tileLength) + 1, (Global.noOfTilesY * Global.tileLength) + 1);
            Graphics gfx = Graphics.FromImage(bmp);

            //For each tile to draw
            for (int row = 0; row < Global.noOfTilesY; row++)
            {
                for (int column = 0; column < Global.noOfTilesX; column++)
                {
                    Rectangle rect = new Rectangle((column * Global.tileLength), (row * Global.tileLength), Global.tileLength, Global.tileLength);

                    //Obtain colour
                    Tile tempTile = Global.lstMapRow[row][column];
                    Color tileColour = tempTile.getColour();

                    //Colour rectangle
                    gfx.FillRectangle(new SolidBrush(tileColour), rect);

                    //Grid Lines
                    gfx.DrawRectangle(Pens.Black, rect);
                }
            }

            //Output bmp to all screens
            foreach (Panel pnlScreen in pnlGrid.Controls)
            {
                PictureBox pbxGrid = (PictureBox)pnlScreen.Controls[0];
                pbxGrid.Image = bmp;

                //Creates overlay - Map resize
                initPbxGridOverlay(pbxGrid);
            }

            tbxOutput = string.Format("...full grid drawn to screen");
            outputToTbxOutput(tbxOutput);
        }

        private void initPbxGridOverlay(PictureBox pbxGrid)
        {
            Panel pnl = (Panel)pbxGrid.Parent;

            string tbxOutput = "";
            tbxOutput = string.Format("Initialising {0}'s overlay...", pnl.Name);
            outputToTbxOutput(tbxOutput);

            PictureBox pbxOverlay = new PictureBox();
            pbxOverlay.Name = "pbxOverlay";
            pbxOverlay.BackColor = Color.Transparent;
            pbxOverlay.Location = new Point(0, 0);
            pbxOverlay.Size = pbxGrid.Size;

            pbxOverlayAddHandlers(pbxOverlay);

            //If overlay exists, remove before adding new one
            if (pbxGrid.Controls.Find("pbxOverlay", false).FirstOrDefault() != null)
            {
                //Note: pbxGrid will always only have one child control
                pbxGrid.Controls.RemoveAt(0);

                tbxOutput = string.Format("Removed existing overlay");
                outputToTbxOutput(tbxOutput);
            }
            pbxGrid.Controls.Add(pbxOverlay);
            tbxOutput = string.Format("Added overlay to screen");
            outputToTbxOutput(tbxOutput);

            //Initialises bitmap that pbxOverlay will hold
            Bitmap bmpOverlay = new Bitmap(pbxGrid.Width, pbxGrid.Height);
            tbxOutput = string.Format("Created empty bitmap for overlay");
            outputToTbxOutput(tbxOutput);

            //Add 'X's to overlay
            fillPbxOverlay(bmpOverlay);

            //Initial values for tile highlight - Off-overlay
            int offsetX = pnlGrid.Location.X + pbxGrid.Location.X;
            int offsetY = pnlGrid.Location.Y + pbxGrid.Location.Y;
            Global.curTileHighlightX = MousePosition.X - offsetX;
            Global.curTileHighlightY = MousePosition.Y - offsetY;

            pbxOverlay.Image = bmpOverlay;
            tbxOutput = string.Format("...overlay initialised");
            outputToTbxOutput(tbxOutput);
        }

        private void pbxOverlayAddHandlers(PictureBox pbxOverlay)
        {
            pbxOverlay.MouseEnter += overlay_MouseEnter;
            pbxOverlay.MouseLeave += overlay_MouseLeave;
            pbxOverlay.MouseMove += overlay_MouseMove;
            pbxOverlay.MouseClick += overlay_MouseClick;    //For clicking but no mouse movement
        }

        private void fillPbxOverlay(Bitmap bmpOverlay)
        {
            foreach (string coords in Global.lstPointsToPathfind)
            {
                drawXOnOverlay(bmpOverlay, coords);
            }

            string tbxOutput = "";
            tbxOutput = string.Format("Drawn path points on new overlay");
            outputToTbxOutput(tbxOutput);
        }

        private void drawXOnOverlay(Bitmap bmp, string coords)
        {
            //Lock bmp bits to sys mem
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            //32-bits - Each mem location holds 1 byte
            int Bpp = 4;

            drawXOnTile(ref bmpData, Bpp, coords);

            //NOTE: UPDATING METHOD (IGNORING GRID LINES) MUST BE UPDATED IF OPTION TO DISCLUDE GRID LINES ADDED

            bmp.UnlockBits(bmpData);
        }

        private void drawXOnTile(ref BitmapData bmpData, int Bpp, string coords)
        {
            Point tileCoords = coordsStringToPoint(coords);

            unsafe
            {
                //Get address of first line
                byte* ptrStart = (byte*)bmpData.Scan0;

                //Location of tile in memory - Note: Think very long 1D array of bytes
                byte* tileRow = ptrStart + (tileCoords.Y * Global.tileLength * bmpData.Stride);
                int tileTopLeft = tileCoords.X * Global.tileLength * Bpp;       //Relative to tileRow

                // Drawing '\' part of cross
                for (int i = 1; i < Global.tileLength; i++)
                {
                    int offsetY = i * bmpData.Stride;
                    int offsetX = i * Bpp;
                    int totOffset = offsetX + offsetY;

                    //Big endian v.s. Little endian byte order
                    if (BitConverter.IsLittleEndian)
                    {
                        tileRow[(tileTopLeft) + totOffset] = Color.Black.B;
                        tileRow[(tileTopLeft + 1) + totOffset] = Color.Black.G;
                        tileRow[(tileTopLeft + 2) + totOffset] = Color.Black.R;
                        tileRow[(tileTopLeft + 3) + totOffset] = Color.Black.A;
                    }
                    else
                    {
                        tileRow[(tileTopLeft) + totOffset] = Color.Black.A;
                        tileRow[(tileTopLeft + 1) + totOffset] = Color.Black.R;
                        tileRow[(tileTopLeft + 2) + totOffset] = Color.Black.G;
                        tileRow[(tileTopLeft + 3) + totOffset] = Color.Black.B;
                    }
                }

                // Drawing '/' part of cross
                for (int i = 1; i < Global.tileLength; i++)
                {
                    int offsetY = i * bmpData.Stride;
                    int offsetX = (Global.tileLength - i) * Bpp;
                    int totOffset = offsetX + offsetY;

                    //Big endian v.s. Little endian byte order
                    if (BitConverter.IsLittleEndian)
                    {
                        tileRow[(tileTopLeft) + totOffset] = Color.Black.B;
                        tileRow[(tileTopLeft + 1) + totOffset] = Color.Black.G;
                        tileRow[(tileTopLeft + 2) + totOffset] = Color.Black.R;
                        tileRow[(tileTopLeft + 3) + totOffset] = Color.Black.A;
                    }
                    else
                    {
                        tileRow[(tileTopLeft) + totOffset] = Color.Black.A;
                        tileRow[(tileTopLeft + 1) + totOffset] = Color.Black.R;
                        tileRow[(tileTopLeft + 2) + totOffset] = Color.Black.G;
                        tileRow[(tileTopLeft + 3) + totOffset] = Color.Black.B;
                    }
                }
            }
        }

        private void updateTileOnMap(string coords)
        {
            //Take first pbx
            Panel pnlScreen = (Panel)pnlGrid.Controls[0];
            PictureBox pbxGrid = (PictureBox)pnlScreen.Controls[0];
            Bitmap bmp = (Bitmap)pbxGrid.Image;

            //Lock bmp bits to sys mem
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            //24-bits - Each mem location holds 1 byte
            int Bpp = 3;

            fillTileOnMap(ref bmpData, Bpp, coords);

            //NOTE: UPDATING METHOD (IGNORING GRID LINES) MUST BE UPDATED IF OPTION TO DISCLUDE GRID LINES ADDED

            bmp.UnlockBits(bmpData);
            pnlGrid.Refresh();
        }

        private void fillTileOnMap(ref BitmapData bmpData, int Bpp, string coords)
        {
            Point tileCoords = coordsStringToPoint(coords);

            //Get tile colour - Colour depends on status
            Tile tileToUpdate = coordsToTile(coords);
            Color tileColour = tileToUpdate.getColour();

            unsafe
            {
                //Get address of first line
                byte* ptrStart = (byte*)bmpData.Scan0;

                //Location of tile in memory - Note: Think very long 1D array of bytes
                byte* tileRow = ptrStart + (tileCoords.Y * Global.tileLength * bmpData.Stride);
                int tileTopLeft = tileCoords.X * Global.tileLength * Bpp;       //Relative to tileRow

                //Row
                for (int i = 0; i < Global.tileLength; i++)
                {
                    //Column
                    for (int j = 0; j < Global.tileLength; j++)
                    {
                        int offsetY = i * bmpData.Stride;
                        int offsetX = j * Bpp;
                        int totOffset = offsetX + offsetY;

                        //Tile Colour - Only draw inside grid lines
                        if (i != 0 && i != Global.tileLength && j != 0 && j != Global.tileLength)
                        {
                            //Big endian v.s. Little endian byte order
                            if (BitConverter.IsLittleEndian)
                            {
                                tileRow[(tileTopLeft) + totOffset] = tileColour.B;
                                tileRow[(tileTopLeft + 1) + totOffset] = tileColour.G;
                                tileRow[(tileTopLeft + 2) + totOffset] = tileColour.R;
                            }
                            else
                            {
                                tileRow[(tileTopLeft) + totOffset] = tileColour.R;
                                tileRow[(tileTopLeft + 1) + totOffset] = tileColour.G;
                                tileRow[(tileTopLeft + 2) + totOffset] = tileColour.R;
                            }
                        }
                    }
                }
            }
        }

        private void updateTilesOnMap(HashSet<string> hsetTilesToUpdate)
        {
            //Take first pbx
            Panel pnlScreen = (Panel)pnlGrid.Controls[0];
            PictureBox pbxGrid = (PictureBox)pnlScreen.Controls[0];
            Bitmap bmp = (Bitmap)pbxGrid.Image;

            //Lock bmp bits to sys mem
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);

            //24-bits - Each mem location holds 1 byte
            int Bpp = 3;

            //Looping through all tiles to update
            foreach (string coords in hsetTilesToUpdate)
            {
                fillTileOnMap(ref bmpData, Bpp, coords);
            }

            //NOTE: UPDATING METHOD (IGNORING GRID LINES) MUST BE UPDATED IF OPTION TO DISCLUDE GRID LINES ADDED

            bmp.UnlockBits(bmpData);
            pnlGrid.Refresh();
        }

        //Draws pathfinding status on overlay
        private void setOverlayTileStatus(PictureBox pbxOverlay, string coords, Color tileColour)
        {
            Bitmap bmp = (Bitmap)pbxOverlay.Image;

            //Lock bmp bits to sys mem
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            //32-bits - Each mem location holds 1 byte
            int Bpp = 4;

            Point tileCoords = coordsStringToPoint(coords);

            unsafe
            {
                //Get address of first line
                byte* ptrStart = (byte*)bmpData.Scan0;

                //Location of tile in memory - Note: Think very long 1D array of bytes
                byte* tileRow = ptrStart + (tileCoords.Y * Global.tileLength * bmpData.Stride);
                int tileTopLeft = tileCoords.X * Global.tileLength * Bpp;       //Relative to tileRow

                //Row
                for (int i = 0; i < Global.tileLength; i++)
                {
                    //Column
                    for (int j = 0; j < Global.tileLength; j++)
                    {
                        int offsetY = i * bmpData.Stride;
                        int offsetX = j * Bpp;
                        int totOffset = offsetX + offsetY;

                        //Tile Colour - Only draw inside grid lines
                        if (i != 0 && i != Global.tileLength && j != 0 && j != Global.tileLength)
                        {
                            //Big endian v.s. Little endian byte order
                            if (BitConverter.IsLittleEndian)
                            {
                                tileRow[(tileTopLeft) + totOffset] = tileColour.B;
                                tileRow[(tileTopLeft + 1) + totOffset] = tileColour.G;
                                tileRow[(tileTopLeft + 2) + totOffset] = tileColour.R;
                                tileRow[(tileTopLeft + 3) + totOffset] = tileColour.A;
                            }
                            else
                            {
                                tileRow[(tileTopLeft) + totOffset] = tileColour.A;
                                tileRow[(tileTopLeft + 1) + totOffset] = tileColour.R;
                                tileRow[(tileTopLeft + 2) + totOffset] = tileColour.G;
                                tileRow[(tileTopLeft + 3) + totOffset] = tileColour.B;
                            }
                        }
                    }
                }

                //NOTE: UPDATING METHOD (IGNORING GRID LINES) MUST BE UPDATED IF OPTION TO DISCLUDE GRID LINES ADDED

                bmp.UnlockBits(bmpData);
                pbxOverlay.Image = bmp;

                if (Global.fastSearch == false)
                {
                    pbxOverlay.Update();            //Goes really fast if this is disabled
                    System.Threading.Thread.Sleep(500);           //Slo-mo
                }
            }
        }

        private Color getOverlayTileStatus(PictureBox pbxOverlay, string coords)
        {
            Bitmap bmp = (Bitmap)pbxOverlay.Image;

            //Lock bmp bits to sys mem
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);

            //32-bits - Each mem location holds 1 byte
            int Bpp = 4;

            Point tileCoords = coordsStringToPoint(coords);

            unsafe
            {
                //Get address of first line
                byte* ptrStart = (byte*)bmpData.Scan0;

                //Location of tile in memory - Note: Think very long 1D array of bytes
                byte* tileRow = ptrStart + (tileCoords.Y * Global.tileLength * bmpData.Stride);
                int tileTopLeft = tileCoords.X * Global.tileLength * Bpp;       //Relative to tileRow

                //Take tile colour @ (1, 2) from grid line top left - Will always contian colour incase of X
                int offsetY = 2 * bmpData.Stride;
                int offsetX = Bpp;
                int totOffset = offsetX + offsetY;
                byte A, R, G, B;

                //Big endian v.s. Little endian byte order
                if (BitConverter.IsLittleEndian)
                {
                    B = tileRow[(tileTopLeft) + totOffset];
                    G = tileRow[(tileTopLeft + 1) + totOffset];
                    R = tileRow[(tileTopLeft + 2) + totOffset];
                    A = tileRow[(tileTopLeft + 3) + totOffset];
                }
                else
                {
                    A = tileRow[(tileTopLeft) + totOffset];
                    R = tileRow[(tileTopLeft + 1) + totOffset];
                    G = tileRow[(tileTopLeft + 2) + totOffset];
                    B = tileRow[(tileTopLeft + 3) + totOffset];
                }

                bmp.UnlockBits(bmpData);

                Color tileColour = Color.FromArgb(A, R, G, B);
                return tileColour;
            }
        }

        #endregion

        #region MouseFunctions

        private void replaceTileHighLight(PictureBox pbxOverlay)
        {
            Bitmap bmpOverlay = (Bitmap)pbxOverlay.Image;
            Graphics gfx = Graphics.FromImage(bmpOverlay);
            gfx.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;      //Replaces pixels rather than blend

            //Get highlighted old tile map coordinates
            int tileX = Global.curTileHighlightX / Global.tileLength;
            int tileY = Global.curTileHighlightY / Global.tileLength;
            string tileCoords = tileX + "," + tileY;

            //Delete previous highlight
            gfx.DrawRectangle(Pens.Transparent, Global.curTileHighlightX, Global.curTileHighlightY, Global.tileLength, Global.tileLength);

            Global.curTileHighlightX = Global.newTileHighlightX;
            Global.curTileHighlightY = Global.newTileHighlightY;

            //Only draw new tile highlight if within map bounds
            if (Global.curTileHighlightX <= pbxOverlay.Width && Global.curTileHighlightY <= pbxOverlay.Height)
            {
                //Get highlighted new tile map coordinates
                tileX = Global.curTileHighlightX / Global.tileLength;
                tileY = Global.curTileHighlightY / Global.tileLength;
                tileCoords = tileX + "," + tileY;

                //Draw over with highlight
                gfx.DrawRectangle(Pens.Red, Global.curTileHighlightX, Global.curTileHighlightY, Global.tileLength, Global.tileLength);
            }

            pbxOverlay.Image = bmpOverlay;
        }

        private void doMouseButtonPressedEvents(MouseEventArgs e)
        {
            //tabScreen open
            if (tabCtrl.SelectedIndex == 0 && Global.allowEditPoints)
            {
                //M1 pressed
                if (e.Button == MouseButtons.Left)
                {
                    addPathfindingPoint();
                }
                //M2 pressed
                else if (e.Button == MouseButtons.Right)
                {
                    deletePathfindingPoint();
                }
            }
            //tabTiles open
            else if (tabCtrl.SelectedIndex == 1 && Global.mapReadyToEdit)
            {
                //M1 pressed
                if (e.Button == MouseButtons.Left)
                {
                    setHighlightedTileType(Global.selectedTileTypeM1);
                }
                //M2 pressed
                else if (e.Button == MouseButtons.Right)
                {
                    setHighlightedTileType(Global.selectedTileTypeM2);
                }
            }
        }

        private void addPathfindingPoint()
        {
            //PBXGRID = PBX THAT MOUSE IS ABOVE - BASE ON OVERLAY THAT MOUSE IS ABOVE, AND GET PARENT
            Panel pnlScreen = (Panel)pnlGrid.Controls[0];
            PictureBox pbxGrid = (PictureBox)pnlScreen.Controls[0];
            PictureBox pbxOverlay = (PictureBox)pbxGrid.Controls[0];

            //Set location = Highlighted tile
            int tileX = Global.curTileHighlightX / Global.tileLength;
            int tileY = Global.curTileHighlightY / Global.tileLength;

            Tile tileSelected = Global.lstMapRow[tileY][tileX];

            //Disallows pathfinding to wall - Impassable
            if (tileSelected.getTileType() != "Wall")
            {
                string tileCoords = tileSelected.getCoords();

                //Limits number of points to pathfind between
                if (Global.lstPointsToPathfind.Count() < Global.maxPathPoints && !(Global.lstPointsToPathfind.Contains(tileCoords)))
                {
                    Global.lstPointsToPathfind.Add(tileCoords);

                    //Draw X on all overlays
                    foreach (Panel screen in pnlGrid.Controls)
                    {
                        PictureBox grid = (PictureBox)screen.Controls[0];
                        PictureBox overlay = (PictureBox)grid.Controls[0];
                        Bitmap bmpOverlay = (Bitmap)overlay.Image;
                        drawXOnOverlay(bmpOverlay, tileCoords);
                    }
                    pnlGrid.Refresh();

                    string tbxOutput = "";
                    string[] tempOutput = tileCoords.Split(',');
                    tbxOutput = string.Format("Path point drawn on coordinate ({0}, {1})", tileCoords[0], tileCoords[1]);
                    outputToTbxOutput(tbxOutput);
                }
            }
        }

        private void deletePathfindingPoint()
        {
            //PBXGRID = PBX THAT MOUSE IS ABOVE - BASE ON OVERLAY THAT MOUSE IS ABOVE, AND GET PARENT
            Panel pnlScreen = (Panel)pnlGrid.Controls[0];
            PictureBox pbxGrid = (PictureBox)pnlScreen.Controls[0];
            PictureBox pbxOverlay = (PictureBox)pbxGrid.Controls[0];
            Bitmap bmpOverlay = (Bitmap)pbxOverlay.Image;

            //Set location = Highlighted tile
            int tileX = Global.curTileHighlightX / Global.tileLength;
            int tileY = Global.curTileHighlightY / Global.tileLength;

            Tile tileSelected = Global.lstMapRow[tileY][tileX];
            string tileCoords = tileSelected.getCoords();

            //Check if tile has path point
            if (Global.lstPointsToPathfind.Contains(tileCoords))
            {
                int deletedIndex = Global.lstPointsToPathfind.IndexOf(tileCoords);
                reorderStringList(ref Global.lstPointsToPathfind, deletedIndex);

                //Erases X on all overlays
                foreach (Panel screen in pnlGrid.Controls)
                {
                    PictureBox grid = (PictureBox)screen.Controls[0];
                    PictureBox overlay = (PictureBox)grid.Controls[0];
                    Bitmap bmp = (Bitmap)overlay.Image;

                    //Erases cross
                    Graphics gfx = Graphics.FromImage(bmp);
                    gfx.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;      //Replaces pixels rather than blend
                    gfx.FillRectangle(Brushes.Transparent, tileX * Global.tileLength + 1, tileY * Global.tileLength + 1, Global.tileLength - 1, Global.tileLength - 1);
                }
                pnlGrid.Refresh();

                string tbxOutput = "";
                string[] tempOutput = tileCoords.Split(',');
                tbxOutput = string.Format("Path point at coordinate ({0}, {1}) deleted", tempOutput[0], tempOutput[1]);
                outputToTbxOutput(tbxOutput);
            }
        }

        private void setHighlightedTileType(string targetTileType)
        {
            //Tile to set = Highlighted tile
            int tileX = Global.curTileHighlightX / Global.tileLength;
            int tileY = Global.curTileHighlightY / Global.tileLength;

            Tile tileToUpdate = Global.lstMapRow[tileY][tileX];
            setTileType(tileToUpdate, targetTileType);

            //Update selected tile on map
            updateTileOnMap(tileToUpdate.getCoords());
        }

        private void setTileType(Tile tileToUpdate, string targetTileType)
        {
            if (tileToUpdate.getTileType() != targetTileType)
            {
                //Don't update if trying to write Wall into 'X'
                if (!(Global.lstPointsToPathfind.Contains(tileToUpdate.getCoords()) && targetTileType == "Wall"))
                {
                    //Remove old tile int dicTileTypeTiles
                    string tileCoords = tileToUpdate.getCoords();
                    string currTileType = tileToUpdate.getTileType();
                    Global.dicTileTypeTiles[currTileType].Remove(tileCoords);

                    //Add new tile in dicTileTypeTiles
                    tileToUpdate.setTileType(targetTileType);
                    Global.dicTileTypeTiles[targetTileType].Add(tileCoords);

                    string tbxOutput = "";
                    string[] tempOutput = tileToUpdate.getCoords().Split(',');
                    tbxOutput = string.Format("Tile at coordinate ({0}, {1}) set to tileType '{2}'", tempOutput[0], tempOutput[1], targetTileType);
                    outputToTbxOutput(tbxOutput);
                }
            }
        }

        private void boundaryFill(PictureBox pbxOverlay, Tile startTile)
        {
            Queue<string> tilesToCheck = new Queue<string>();        //Holds coordinates of tiles to be checked and the layer it belongs to
            HashSet<string> tilesSeen = new HashSet<string>();

            string startTileType = startTile.getTileType();
            Point startPointCoords = coordsStringToPoint(startTile.getCoords());

            //Enqueue Start Pos.
            tilesToCheck.Enqueue(startTile.getCoords());
            tilesSeen.Add(startTile.getCoords());

            while (tilesToCheck.Count > 0)
            {
                //Index: 0 = x, 1 = y
                string currTileCoords = tilesToCheck.Dequeue();
                Point currCoords = coordsStringToPoint(currTileCoords);

                //Look at adjacent tiles - dir = Checking direction (up/left/down/right) = (0/1/2/3)
                for (int dir = 0; dir <= 3; dir++)
                {
                    int nextX = 0, nextY = 0;

                    switch (dir)
                    {
                        case 0:     //UP - (X, Y - 1)
                            nextX = currCoords.X;
                            nextY = currCoords.Y - 1;
                            break;
                        case 1:     //LEFT - (X - 1, Y)
                            nextX = currCoords.X - 1;
                            nextY = currCoords.Y;
                            break;
                        case 2:     //DOWN - (X, Y + 1)
                            nextX = currCoords.X;
                            nextY = currCoords.Y + 1;
                            break;
                        case 3:     //RIGHT - (X + 1, Y)
                            nextX = currCoords.X + 1;
                            nextY = currCoords.Y;
                            break;
                    }

                    //If within bounds
                    if (nextX >= 0 && nextX < Global.noOfTilesX && nextY >= 0 && nextY < Global.noOfTilesY)
                    {
                        Tile nextTile = Global.lstMapRow[nextY][nextX];

                        //If next tile NOT seen AND equal to start tile tileType
                        if (!tilesSeen.Contains(nextTile.getCoords()) &&
                            nextTile.getTileType() == startTile.getTileType())
                        {
                            //Don't update if trying to write Wall into 'X'
                            if (!(Global.lstPointsToPathfind.Contains(nextTile.getCoords()) && (Global.selectedTileTypeM1 == "Wall")))
                            {
                                tilesToCheck.Enqueue(nextTile.getCoords());
                                tilesSeen.Add(nextTile.getCoords());
                            }
                        }
                    }
                }
            }

            //Update tileTypes
            foreach (string coords in tilesSeen)
            {
                Tile tileToUpdate = coordsToTile(coords);
                setTileType(tileToUpdate, Global.selectedTileTypeM1);
            }

            updateTilesOnMap(tilesSeen);

            string tbxOutput = "";
            tbxOutput = string.Format("Shape around coordinate ({0}, {1}) set to tileType '{2}'", startPointCoords.X, startPointCoords.Y, Global.selectedTileTypeM1);
            outputToTbxOutput(tbxOutput);
        }

        #region OverlayMouseFunctions
        private void overlay_MouseEnter(object sender, EventArgs e)
        {
            PictureBox pbxOverlay = (PictureBox)sender;
            PictureBox pbxGrid = (PictureBox)pbxOverlay.Parent;
            Panel pnlScreen = (Panel)pbxGrid.Parent;

            string tbxOutput = "";
            tbxOutput = string.Format("Mouse entered {0}", pnlScreen.Name);
            outputToTbxOutput(tbxOutput);
        }

        private void overlay_MouseLeave(object sender, EventArgs e)
        {
            //Remove pbxOverlay from pbxGrid
            PictureBox pbxOverlay = (PictureBox)sender;
            PictureBox pbxGrid = (PictureBox)pbxOverlay.Parent;
            Panel pnlScreen = (Panel)pbxGrid.Parent;
            Point cursPos = pnlScreen.PointToClient(Cursor.Position);

            //Updates new tile highlight position based on last tile mouse hovered above before leaving
            if (cursPos.X < Global.borderLength || cursPos.Y < Global.borderLength
                || cursPos.X >= pbxGrid.Width || cursPos.Y >= pbxGrid.Height)
            {
                //Makes tileHighlight offscreen
                Global.newTileHighlightX = pnlScreen.Width;
                Global.newTileHighlightY = pnlScreen.Height;
            }

            //USELESS??
            //else
            //{
            //    //Gets tile coordinates on leaving grid
            //    int offsetX = pnlGrid.Location.X + pbxGrid.Location.X;
            //    int offsetY = pnlGrid.Location.Y + pbxGrid.Location.Y;

            //    Global.newTileHighlightX = cursPos.X - offsetX;
            //    Global.newTileHighlightY = cursPos.Y - offsetY;
            //}

            replaceTileHighLight(pbxOverlay);

            string tbxOutput = "";
            tbxOutput = string.Format("Mouse left {0}", pnlScreen.Name);
            outputToTbxOutput(tbxOutput);
        }

        private void overlay_MouseMove(object sender, MouseEventArgs e)
        {
            PictureBox pbxOverlay = (PictureBox)sender;
            Point cursPos = pbxOverlay.PointToClient(Cursor.Position);

            //If not out of range
            if (!(cursPos.X < 0 || cursPos.X > pbxOverlay.Width || cursPos.Y < 0 || cursPos.Y > pbxOverlay.Height))
            {
                //Snap tile highlight to tile
                //Get tile no. first
                Global.newTileHighlightX = (int)(Math.Floor((decimal)cursPos.X / Global.tileLength));
                Global.newTileHighlightY = (int)(Math.Floor((decimal)cursPos.Y / Global.tileLength));

                //Corrects for mouse on bottom-most / right-most border drawing tile highlights outside of pbxGrid
                if (Global.newTileHighlightX == Global.noOfTilesX)
                {
                    Global.newTileHighlightX -= 1;
                }
                if (Global.newTileHighlightY == Global.noOfTilesY)
                {
                    Global.newTileHighlightY -= 1;
                }

                //Convert to screen coordinates
                Global.newTileHighlightX *= Global.tileLength;
                Global.newTileHighlightY *= Global.tileLength;

                //Only draw new tile highlight if different from current location
                if (Global.newTileHighlightX != Global.curTileHighlightX || Global.newTileHighlightY != Global.curTileHighlightY)
                {
                    replaceTileHighLight(pbxOverlay);
                }

                //If any mouse button pressed
                doMouseButtonPressedEvents(e);
            }
        }

        private void overlay_MouseClick(object sender, MouseEventArgs e)
        {
            //M3 click, tabTilesOpen, map ready to edit - Boundary Fill
            if (e.Button == MouseButtons.Middle && tabCtrl.SelectedIndex == 1 && Global.mapReadyToEdit)
            {
                PictureBox pbxOverlay = (PictureBox)sender;

                //Get selected tile
                int tileX = Global.curTileHighlightX / Global.tileLength;
                int tileY = Global.curTileHighlightY / Global.tileLength;
                Tile tileSelected = Global.lstMapRow[tileY][tileX];

                boundaryFill(pbxOverlay, tileSelected);
            }
            else
            {
                doMouseButtonPressedEvents(e);
            }
        }

        #endregion

        #endregion

        #region UsefulFunctions

        private void OutputMap()
        {
            string tbxOutput = string.Format("Outputting {0} x {1} map to screen...", Global.noOfTilesX, Global.noOfTilesY);
            outputToTbxOutput(tbxOutput);

            BestFitPbx();
            DrawFullGrid();

            tbxOutput = string.Format("...done outputting {0} x {1} map to screen", Global.noOfTilesX, Global.noOfTilesY);
            outputToTbxOutput(tbxOutput);
        }

        private Point coordsStringToPoint(string tileCoords)
        {
            string[] arrCoords = tileCoords.Split(',');
            Point coords = new Point(int.Parse(arrCoords[0]), int.Parse(arrCoords[1]));

            return coords;
        }

        private Tile coordsToTile(string tileCoords)
        {
            Point coords = coordsStringToPoint(tileCoords);

            Tile tile = Global.lstMapRow[coords.Y][coords.X];
            return tile;
        }

        private void reorderStringList(ref List<string> lst, int indexToDelete)
        {
            //Shifting index in list downwards
            for (int i = indexToDelete; i < lst.Count() - 1; i++)
            {
                lst[i] = lst[i + 1];
            }

            //Remove last element in list
            lst.RemoveAt(lst.Count() - 1);
        }

        private Tuple<int, string> getInitTileLength()
        {
            int testLengthX = (int)Math.Floor((decimal)(pnlGrid.Width - 1 - Global.borderLength) / Global.noOfTilesX);
            int testLengthY = (int)Math.Floor((decimal)(pnlGrid.Height - 1 - Global.borderLength) / Global.noOfTilesY);
            int initTileLength = 0;
            string lastAction = "";

            //Determines whether width/height/both caused tileLength to go less than minimum
            if (testLengthX < Global.minTileLength && testLengthY >= Global.minTileLength)
            {
                lastAction = "X";
            }
            else if (testLengthY < Global.minTileLength && testLengthX >= Global.minTileLength)
            {
                lastAction = "Y";
            }
            else if (testLengthX < Global.minTileLength && testLengthY < Global.minTileLength)
            {
                lastAction = "XY";
            }

            //Sets initTileLength
            if (testLengthX > testLengthY)
            {
                initTileLength = testLengthY;
            }
            else if (testLengthX < testLengthY)
            {
                initTileLength = testLengthX;
            }
            else if (testLengthX == testLengthY)
            {
                initTileLength = testLengthX;
            }

            Tuple<int, string> result = new Tuple<int, string>(initTileLength, lastAction);
            return result;
        }

        private Panel createScreen(string screenName, Point screenLocation, Size screenSize)
        {
            Panel pnlScreen = new Panel();
            pnlScreen.Name = screenName;
            pnlScreen.Location = screenLocation;
            pnlScreen.Size = screenSize;
            pnlScreen.BackColor = Color.White;

            Label lblScreen = new Label();
            lblScreen.Name = "lblScreen";
            lblScreen.Text = pnlScreen.Name;
            lblScreen.AutoSize = true;
            lblScreen.Location = new Point(0, 0);

            PictureBox pbxGrid = new PictureBox();
            pbxGrid.Name = "pbxGrid";
            pbxGrid.Location = new Point(Global.borderLength, Global.borderLength);
            pbxGrid.Size = new Size(pnlScreen.Width - Global.borderLength, pnlScreen.Height - Global.borderLength);

            //NOTE: pnlScreen.Controls[0] = pbxGrid
            //      pnlScreen.Controls[1] = lblScreen

            pnlScreen.Controls.Add(pbxGrid);
            pnlScreen.Controls.Add(lblScreen);

            return pnlScreen;
        }

        private void resetTileTypeSelectorM1()
        {
            Panel pnlWall = (Panel)tabTiles.Controls.Find("Wall", false).FirstOrDefault();
            PictureBox pbxWall = (PictureBox)pnlWall.Controls.Find("pbxWall", false).FirstOrDefault();

            moveTileTypeSelector(pbxWall, "lblTileSelectM1");
        }

        private void resetTileTypeSelectorM2()
        {
            Panel pnlEmpty = (Panel)tabTiles.Controls.Find("Empty", false).FirstOrDefault();
            PictureBox pbxEmpty = (PictureBox)pnlEmpty.Controls.Find("pbxEmpty", false).FirstOrDefault();

            moveTileTypeSelector(pbxEmpty, "lblTileSelectM2");
        }

        #endregion

        #region menuBarMethods
        private void menuBarNew_Click(object sender, EventArgs e)
        {
            DialogResult newProjectResult = MessageBox.Show("Start new project?\n(All unsaved changes will be deleted)", "New Project", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (newProjectResult == DialogResult.Yes)
            {
                ResetProject();
            }
        }

        private void ResetProject()
        {
            string tbxOutput = "";
            tbxOutput = string.Format("Resetting project...");
            outputToTbxOutput(tbxOutput);

            resetTabScreen();
            resetTabTiles();
            resetPathfinding();

            OutputMap();

            tbxOutput = string.Format("...project reset");
            outputToTbxOutput(tbxOutput);
        }

        private void resetTabScreen()
        {
            string tbxOutput = "";
            tbxOutput = string.Format("Resetting tabScreen...");
            outputToTbxOutput(tbxOutput);

            //Clear all path points
            Global.lstPointsToPathfind.Clear();
            tbxOutput = string.Format("All path points cleared");
            outputToTbxOutput(tbxOutput);

            //Delete all tiles from map
            Global.lstMapRow.Clear();
            tbxOutput = string.Format("Map cleared");
            outputToTbxOutput(tbxOutput);

            deleteAllCustomScreens();

            Panel pnlAddSet = (Panel)tabScreen.Controls.Find("pnlAddSet", false).FirstOrDefault();
            Panel pnlDefault = (Panel)tabScreen.Controls.Find("Screen0", false).FirstOrDefault();
            Button btnAdd = (Button)pnlAddSet.Controls.Find("btnAddScreen", false).FirstOrDefault();

            Label lblSeparator = (Label)tabScreen.Controls.Find("lblSeparator", false).FirstOrDefault();
            pnlAddSet.Location = new Point(30, pnlDefault.Location.Y + pnlDefault.Height);
            btnAdd.Visible = true;

            //Reinitialises map to default size
            InitialiseMap();
            tbxOutput = string.Format("Map reinitialised to default size");
            outputToTbxOutput(tbxOutput);

            //Reset width and height textbox values in tabScreen
            Panel pnlScreenSize = (Panel)tabScreen.Controls.Find("pnlScreenSize", false).FirstOrDefault();
            TextBox tbxWidth = (TextBox)pnlScreenSize.Controls.Find("tbxWidth", false).FirstOrDefault();
            TextBox tbxHeight = (TextBox)pnlScreenSize.Controls.Find("tbxHeight", false).FirstOrDefault();
            tbxWidth.Text = Global.noOfTilesX.ToString();
            tbxHeight.Text = Global.noOfTilesY.ToString();

            tbxOutput = string.Format("...tabScreen reset");
            outputToTbxOutput(tbxOutput);
        }

        private void deleteAllCustomScreens()
        {
            //Deletes all screen panels but default
            for (int i = Global.lstScreens.Count() - 1; i > 0; i--)
            {
                Panel pnlToDelete = (Panel)tabScreen.Controls.Find("Screen" + i, false).FirstOrDefault();
                tabScreen.Controls.Remove(pnlToDelete);
                Global.lstScreens.RemoveAt(i);
                pnlGrid.Controls.RemoveAt(i);
            }

            //Resets default screen panel
            Panel pnlDefault = (Panel)tabScreen.Controls.Find("Screen0", false).FirstOrDefault();
            ComboBox cmbAlgorithm = (ComboBox)pnlDefault.Controls.Find("cmbAlgorithm", false).FirstOrDefault();
            cmbAlgorithm.Text = "BFS";

            string tbxOutput = "";
            tbxOutput = string.Format("All custom screens deleted");
            outputToTbxOutput(tbxOutput);
        }

        private void resetTabTiles()
        {
            string tbxOutput = "";
            tbxOutput = string.Format("Resetting tabTiles...");
            outputToTbxOutput(tbxOutput);

            deleteAllCustomTileTypes();

            //Reset tileTypeSelector location
            resetTileTypeSelectorM1();
            resetTileTypeSelectorM2();

            tbxOutput = string.Format("...tabTiles reset");
            outputToTbxOutput(tbxOutput);
        }

        private void deleteAllCustomTileTypes()
        {
            //Delete all custom panels
            List<Panel> pnlsToDelete = tabTiles.Controls.OfType<Panel>().ToList();
            int pnlToDeleteIndex = pnlsToDelete.Count() - 1;

            while (pnlToDeleteIndex > 2)        //Excludes first 3 panels  
            {
                Panel pnlToDelete = pnlsToDelete[pnlToDeleteIndex];
                tabTiles.Controls.Remove(pnlToDelete);
                pnlToDeleteIndex--;
            }

            //Moves pnlAddSet to starting position and makes the '+' button visible and the 'Set' button invisible
            Panel pnlAddSet = pnlsToDelete[2];
            Button btnAdd = (Button)pnlAddSet.Controls.Find("btnAddTileType", false).FirstOrDefault();
            Button btnSet = (Button)pnlAddSet.Controls.Find("btnSetTileTypes", false).FirstOrDefault();

            Label lblSeparator = (Label)tabTiles.Controls.Find("lblSeparator", false).FirstOrDefault();
            pnlAddSet.Location = new Point(30, lblSeparator.Location.Y + 30);
            btnAdd.Visible = true;
            btnSet.Visible = false;

            pnlsToDelete.Clear();

            //Reset list of dictionary keys
            Global.lstDicTileTypeKeys.Clear();
            Global.lstDicTileTypeKeys.Add("Empty");
            Global.lstDicTileTypeKeys.Add("Wall");

            //Reset tile type info dictionary
            Global.dicTileTypeInfo.Clear();
            Global.dicTileTypeInfo.Add("Empty", new Tuple<Color, int>(Color.White, 1));
            Global.dicTileTypeInfo.Add("Wall", new Tuple<Color, int>(Color.Black, -1));

            //Reset tiles of certain tileType dictionary
            Global.dicTileTypeTiles.Clear();
            Global.dicTileTypeTiles.Add("Empty", new HashSet<string>());
            Global.dicTileTypeTiles.Add("Wall", new HashSet<string>());

            string tbxOutput = "";
            tbxOutput = string.Format("All custom tileTypes deleted");
            outputToTbxOutput(tbxOutput);
        }

        private void menuBarImportImage_Click(object sender, EventArgs e)
        {
            OpenFileDialog dlgOpen = new OpenFileDialog();
            DialogResult result = dlgOpen.ShowDialog();
            if (result == DialogResult.OK)
            {
                Form frmLoad = new Form();
                createLoadingScreen(frmLoad);

                string tbxOutput = "";
                tbxOutput = string.Format("Attempting to import image...");
                outputToTbxOutput(tbxOutput);

                string fileDir = dlgOpen.FileName;
                bool isImage = false;

                //Check if file is an image
                try
                {
                    Bitmap bmpImport = (Bitmap)Image.FromFile(fileDir);
                    isImage = true;
                }
                catch
                {
                    frmLoad.Close();

                    tbxOutput = string.Format("...could not import image");
                    outputToTbxOutput(tbxOutput);

                    MessageBox.Show("Error: Can't import " + fileDir.Substring(fileDir.LastIndexOf('.')));
                }

                //Proceed only if imported file is an image
                if (isImage)
                {
                    Bitmap bmpImport = (Bitmap)Image.FromFile(fileDir);

                    tbxOutput = string.Format("Testing if image fits into all screens...");
                    outputToTbxOutput(tbxOutput);

                    //Trial fit to test if image will fit into current number of screens
                    int oldNoOfTilesX = Global.noOfTilesX;
                    int oldNoOfTilesY = Global.noOfTilesY;
                    Global.noOfTilesX = bmpImport.Width;
                    Global.noOfTilesY = bmpImport.Height;

                    Tuple<int, string> tileLengthOptions = trialFitScreen();

                    Global.noOfTilesX = oldNoOfTilesX;
                    Global.noOfTilesY = oldNoOfTilesY;
                    int testTileLength = tileLengthOptions.Item1;

                    //If image is not too large
                    if (testTileLength != -1)
                    {
                        tbxOutput = string.Format("...image fits");
                        outputToTbxOutput(tbxOutput);

                        //Goes through bitmap and creates list of unique colours
                        List<Color> lstColour = new List<Color>();
                        Rectangle rect = new Rectangle(0, 0, bmpImport.Width, bmpImport.Height);
                        BitmapData bmpData = bmpImport.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                        fillColourList(ref lstColour, ref bmpData);

                        tbxOutput = string.Format("Testing if there are enough tileType slots for each unique colour in image...");
                        outputToTbxOutput(tbxOutput);

                        //A tileType will be created for each unique colour present in the bmp
                        //Check to see if there are enough spaces for these tileTypes
                        if (lstColour.Count() <= Global.maxTileTypes)
                        {
                            tbxOutput = string.Format("...there are enough tileType slots");
                            outputToTbxOutput(tbxOutput);

                            //Notify user that any unsaved changes will be erased
                            DialogResult newProjectResult = MessageBox.Show("All unsaved changes will be deleted. Continue?", "Import bitmap", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                            if (newProjectResult == DialogResult.Yes)
                            {
                                Global.noOfTilesX = bmpImport.Width;
                                Global.noOfTilesY = bmpImport.Height;
                                Global.tileLength = testTileLength;
                                prepareImport();

                                createTileTypes(ref lstColour);
                                outputBmpToMap(ref bmpData);
                                bmpImport.UnlockBits(bmpData);

                                placeScreens();
                                DrawFullGrid();

                                frmLoad.Close();

                                tbxOutput = string.Format("...image successfully imported");
                                outputToTbxOutput(tbxOutput);
                            }
                            else
                            {
                                frmLoad.Close();

                                tbxOutput = string.Format("...image importing cancelled");
                                outputToTbxOutput(tbxOutput);
                            }
                        }
                        else
                        {
                            bmpImport.UnlockBits(bmpData);

                            frmLoad.Close();

                            tbxOutput = string.Format("...too many colours");
                            outputToTbxOutput(tbxOutput);
                            tbxOutput = string.Format("...could not import image");
                            outputToTbxOutput(tbxOutput);

                            MessageBox.Show("Error: Too many colours in image. Maximum: " + Global.maxTileTypes + " excluding black and white. Your image has " + lstColour.Count());
                        }
                    }
                    else
                    {
                        frmLoad.Close();

                        tbxOutput = string.Format("...image too large");
                        outputToTbxOutput(tbxOutput);
                        tbxOutput = string.Format("...could not import image");
                        outputToTbxOutput(tbxOutput);

                        MessageBox.Show("Error: Image too large. Maximum size for " + Global.lstScreens.Count() + " " + ((Global.lstScreens.Count() > 1) ? "screens" : "screen") + ": " +
                                        +Global.maxNoOfTilesX + " x " + Global.maxNoOfTilesY + ". Your image is " + bmpImport.Width + " x " + bmpImport.Height);
                    }
                }
            }
        }

        private void createLoadingScreen(Form frmLoad)
        {
            frmLoad.FormBorderStyle = FormBorderStyle.None;
            frmLoad.Size = new Size(320, 110);
            frmLoad.StartPosition = FormStartPosition.Manual;
            frmLoad.SetDesktopLocation(this.Width - frmLoad.Size.Width - 40, 40);

            //For form border
            Panel pnlLoading = new Panel();
            pnlLoading.Size = frmLoad.Size;
            pnlLoading.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            pnlLoading.BackColor = Color.SlateGray;
            frmLoad.Controls.Add(pnlLoading);

            //Loading...
            Label lblLoading = new Label();
            lblLoading.Text = "Loading...";
            lblLoading.BackColor = Color.Transparent;
            lblLoading.Font = new Font("Microsoft Sans Serif", 13.0f, FontStyle.Bold);
            lblLoading.AutoSize = true;
            lblLoading.Location = new Point((int)((frmLoad.Width / 2) - (lblLoading.Width / 2)), (int)((frmLoad.Height / 2) - (lblLoading.Height / 2)));
            pnlLoading.Controls.Add(lblLoading);

            //Status text


            frmLoad.Show();

        }

        private void fillColourList(ref List<Color> lstColour, ref BitmapData bmpData)
        {
            unsafe
            {
                //Get address of first line
                byte* ptrStart = (byte*)bmpData.Scan0;
                int Bpp = 4;
                byte A, R, G, B;

                if (BitConverter.IsLittleEndian)
                {
                    for (int row = 0; row < bmpData.Height; row++)
                    {
                        byte* ptrRow = ptrStart + (row * (Bpp * bmpData.Width));

                        for (int col = 0; col < bmpData.Width; col++)
                        {
                            int bmpX = col * Bpp;

                            B = ptrRow[bmpX];
                            G = ptrRow[bmpX + 1];
                            R = ptrRow[bmpX + 2];
                            A = ptrRow[bmpX + 3];

                            Color pixColour = Color.FromArgb(A, R, G, B);
                            if (!lstColour.Contains(pixColour))
                            {
                                //Add if not default tileType colour
                                if (!(pixColour.ToArgb() == Color.White.ToArgb() || pixColour.ToArgb() == Color.Black.ToArgb()))
                                {
                                    lstColour.Add(pixColour);
                                }
                            }
                        }
                    }
                }
                //For big endian
                else
                {
                    for (int row = 0; row < bmpData.Height; row++)
                    {
                        byte* ptrRow = ptrStart + (row * (Bpp * bmpData.Width));

                        for (int col = 0; col < bmpData.Width; col++)
                        {
                            Color tileColour = Global.lstMapRow[row][col].getColour();

                            int bmpX = col * Bpp;

                            A = ptrRow[bmpX];
                            R = ptrRow[bmpX + 1];
                            G = ptrRow[bmpX + 2];
                            B = ptrRow[bmpX + 3];

                            Color pixColour = Color.FromArgb(A, R, G, B);
                            if (!lstColour.Contains(pixColour))
                            {
                                //Add if not default tileType colour
                                if (!(pixColour.ToArgb() == Color.White.ToArgb() || pixColour.ToArgb() == Color.Black.ToArgb()))
                                {
                                    lstColour.Add(pixColour);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void prepareImport()
        {
            string tbxOutput = "";
            tbxOutput = string.Format("Preparing to import image...");
            outputToTbxOutput(tbxOutput);

            //Clear all path points
            Global.lstPointsToPathfind.Clear();
            tbxOutput = string.Format("All path points cleared");
            outputToTbxOutput(tbxOutput);

            //Reset tile map
            Global.lstMapRow.Clear();
            tbxOutput = string.Format("Map cleared");
            outputToTbxOutput(tbxOutput);

            //Rebuilding map - Need to change Global.noOfTilesXY temporarily
            int noOfTilesX = Global.noOfTilesX;
            int noOfTilesY = Global.noOfTilesY;
            Global.noOfTilesX = 0;
            Global.noOfTilesY = 0;

            rebuildMap(noOfTilesX, noOfTilesY);

            Global.noOfTilesX = noOfTilesX;
            Global.noOfTilesY = noOfTilesY;

            //Clear empty hashSet to prevent excess tiles being stored later on
            Global.dicTileTypeTiles["Empty"].Clear();

            //Reset width and height textbox values in tabScreen
            Panel pnlScreenSize = (Panel)tabScreen.Controls.Find("pnlScreenSize", false).FirstOrDefault();
            TextBox tbxWidth = (TextBox)pnlScreenSize.Controls.Find("tbxWidth", false).FirstOrDefault();
            TextBox tbxHeight = (TextBox)pnlScreenSize.Controls.Find("tbxHeight", false).FirstOrDefault();
            tbxWidth.Text = Global.noOfTilesX.ToString();
            tbxHeight.Text = Global.noOfTilesY.ToString();

            resetTabTiles();
            resetPathfinding();

            tbxOutput = string.Format("...preparations complete");
            outputToTbxOutput(tbxOutput);
        }

        private void createTileTypes(ref List<Color> lstColour)
        {
            Panel pnlAddSet = (Panel)tabTiles.Controls.Find("pnlAddSet", false).FirstOrDefault();
            Button btnAdd = (Button)pnlAddSet.Controls.Find("btnAddTileType", false).First();

            //Create tileTypes for each unique colour
            for (int i = 0; i < lstColour.Count(); i++)
            {
                Color tileColour = lstColour[i];

                //Add tileType panel
                btnAddTileType_Click(btnAdd, EventArgs.Empty);

                //Change colour of tileType in panel that was just added
                Panel pnlTileType = (Panel)tabTiles.Controls[tabTiles.Controls.Count - 1];
                PictureBox pbxTileTypeColour = (PictureBox)pnlTileType.Controls.Find("pbxColour", false).FirstOrDefault();
                pbxTileTypeColour.Tag = tileColour;         //Tag colour on pbx so we can refer to it when updating
                drawTileTypePbx(pbxTileTypeColour, tileColour);

                //Update dictionary entries (Created in btnAddTileType_Click)
                string newTileTypeName = tileColour.ToString();

                //Recreate dicTileTypeTiles entry
                Global.dicTileTypeTiles.Remove(pnlTileType.Name);
                Global.dicTileTypeTiles.Add(newTileTypeName, new HashSet<string>());

                //Recreate dicTileTypeInfo entry (Note: Weight = 1)
                Global.dicTileTypeInfo.Remove(pnlTileType.Name);
                Global.dicTileTypeInfo.Add(newTileTypeName, new Tuple<Color, int>(tileColour, 1));

                //Update list of keys
                int indexToChange = Global.lstDicTileTypeKeys.IndexOf(pnlTileType.Name);
                Global.lstDicTileTypeKeys[indexToChange] = newTileTypeName;

                //Note: tileType name = Colour of tileType (argb), since on import, there are no two tileTypes with same colour
                pnlTileType.Name = newTileTypeName;
            }
        }

        private void outputBmpToMap(ref BitmapData bmpData)
        {
            unsafe
            {
                //Get address of first line
                byte* ptrStart = (byte*)bmpData.Scan0;
                int Bpp = 4;
                byte A, R, G, B;

                if (BitConverter.IsLittleEndian)
                {
                    for (int row = 0; row < Global.noOfTilesY; row++)
                    {
                        byte* ptrRow = ptrStart + (row * (Bpp * Global.noOfTilesX));

                        for (int col = 0; col < Global.noOfTilesX; col++)
                        {
                            int bmpX = col * Bpp;

                            B = ptrRow[bmpX];
                            G = ptrRow[bmpX + 1];
                            R = ptrRow[bmpX + 2];
                            A = ptrRow[bmpX + 3];

                            Color pixColour = Color.FromArgb(A, R, G, B);
                            string tileTypeName = "";

                            //Add tiles to hashSets
                            if (pixColour.ToArgb() == Color.White.ToArgb())
                            {
                                tileTypeName = "Empty";
                            }
                            else if (pixColour.ToArgb() == Color.Black.ToArgb())
                            {
                                tileTypeName = "Wall";
                            }
                            else
                            {
                                tileTypeName = pixColour.ToString();
                            }

                            //Edit map data with new colour
                            Global.lstMapRow[row][col].setTileType(tileTypeName);

                            //Add new coordinate to hashset
                            string coords = col.ToString() + "," + row.ToString();
                            Global.dicTileTypeTiles[tileTypeName].Add(coords);
                        }
                    }
                }
                //For big endian
                else
                {
                    for (int row = 0; row < Global.noOfTilesY; row++)
                    {
                        byte* ptrRow = ptrStart + (row * (Bpp * Global.noOfTilesX));

                        for (int col = 0; col < Global.noOfTilesX; col++)
                        {
                            Color tileColour = Global.lstMapRow[row][col].getColour();

                            int bmpX = col * Bpp;

                            A = ptrRow[bmpX];
                            R = ptrRow[bmpX + 1];
                            G = ptrRow[bmpX + 2];
                            B = ptrRow[bmpX + 3];

                            Color pixColour = Color.FromArgb(A, R, G, B);
                            string tileTypeName = "";

                            //Add tiles to hashSets
                            if (pixColour.ToArgb() == Color.White.ToArgb())
                            {
                                tileTypeName = "Empty";
                            }
                            else if (pixColour.ToArgb() == Color.Black.ToArgb())
                            {
                                tileTypeName = "Wall";
                            }
                            else
                            {
                                tileTypeName = pixColour.ToString();
                            }

                            //Edit map data with new colour
                            Global.lstMapRow[row][col].setTileType(tileTypeName);

                            //Add new coordinate to hashset
                            string coords = col.ToString() + "," + row.ToString();
                            Global.dicTileTypeTiles[tileTypeName].Add(coords);
                        }
                    }
                }
            }
        }

        private void menuBarExportBmp_Click(object sender, EventArgs e)
        {
            SaveFileDialog dlgSave = new SaveFileDialog();
            dlgSave.DefaultExt = ".bmp";
            dlgSave.ShowDialog();
            string fileDir = "";
            fileDir = dlgSave.FileName;

            //If name is valid
            if (fileDir != "")
            {
                Bitmap bmp = new Bitmap(Global.noOfTilesX, Global.noOfTilesY);

                //Lock bmp bits to sys mem
                Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                BitmapData bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, PixelFormat.Format32bppArgb);
                int Bpp = 4;

                unsafe
                {
                    //Get address of first line
                    byte* ptrStart = (byte*)bmpData.Scan0;

                    //Fill bmp
                    if (BitConverter.IsLittleEndian)
                    {
                        for (int row = 0; row < Global.noOfTilesY; row++)
                        {
                            byte* ptrRow = ptrStart + (row * (Bpp * Global.noOfTilesX));

                            for (int col = 0; col < Global.noOfTilesX; col++)
                            {
                                Color tileColour = Global.lstMapRow[row][col].getColour();

                                int bmpX = col * Bpp;

                                ptrRow[bmpX] = tileColour.B;
                                ptrRow[bmpX + 1] = tileColour.G;
                                ptrRow[bmpX + 2] = tileColour.R;
                                ptrRow[bmpX + 3] = tileColour.A;
                            }
                        }
                    }
                    //For big endian
                    else
                    {
                        for (int row = 0; row < Global.noOfTilesY; row++)
                        {
                            byte* ptrRow = ptrStart + (row * (Bpp * Global.noOfTilesX));

                            for (int col = 0; col < Global.noOfTilesX; col++)
                            {
                                Color tileColour = Global.lstMapRow[row][col].getColour();

                                int bmpX = col * Bpp;

                                ptrRow[bmpX] = tileColour.A;
                                ptrRow[bmpX + 1] = tileColour.R;
                                ptrRow[bmpX + 2] = tileColour.G;
                                ptrRow[bmpX + 3] = tileColour.B;
                            }
                        }
                    }

                    bmp.UnlockBits(bmpData);
                }

                bmp.Save(fileDir);

                string tbxOutput = "";
                tbxOutput = string.Format("Map exported as bitmap");
                outputToTbxOutput(tbxOutput);

                MessageBox.Show("Map successfully exported as bitmap");
            }
        }

        private void menuBarExit_Click(object sender, EventArgs e)
        {
            DialogResult exitResult = MessageBox.Show("Exit the program?", "Exit", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (exitResult == DialogResult.Yes)
            {
                Application.Exit();
            }
        }
        #endregion

        #region Pathfinding

        //SPLIT INTO MORE METHODS, MAKE EASIER TO READ
        private void btnStartPathfind_Click(object sender, EventArgs e)
        {
            Button btnStartPathfind = (Button)sender;

            //Starting pathfinding
            if (btnStartPathfind.Text == "Start pathfinding")
            {
                string tbxOutput = "";
                tbxOutput = string.Format("");
                outputToTbxOutput(tbxOutput);
                tbxOutput = string.Format("Pathfinding started...");
                outputToTbxOutput(tbxOutput);

                //Multiple destinations
                if (Global.lstPointsToPathfind.Count > 2)
                {
                    tbxOutput = string.Format("...too many path points");
                    outputToTbxOutput(tbxOutput);

                    MessageBox.Show("Multiple destinations (with Minimum spanning tree, Complete Graphs) not yet available", "Coming soon!", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                //Single destination
                else if (Global.lstPointsToPathfind.Count == 2)
                {
                    //Switch to tabOutput screen
                    tabCtrl.SelectedIndex = 2;
                    tabOutput.Refresh();

                    //Pathfind for each overlay
                    for (int i = 0; i < pnlGrid.Controls.Count; i++)
                    {
                        Panel pnlScreen = (Panel)pnlGrid.Controls[i];
                        PictureBox pbxGrid = (PictureBox)pnlScreen.Controls[0];
                        PictureBox pbxOverlay = (PictureBox)pbxGrid.Controls[0];

                        Panel pnlScreenOptions = (Panel)tabScreen.Controls.Find(pnlScreen.Name, false).FirstOrDefault();
                        ComboBox cmbAlgorithm = (ComboBox)pnlScreenOptions.Controls.Find("cmbAlgorithm", false).FirstOrDefault();

                        //Find chosen algorithm and its options then start pathfinding
                        startPathfinding(pnlScreenOptions, pbxOverlay, cmbAlgorithm);

                        //Stops pathfinding if no path    
                        if (btnStartPathfind.Text == "Reset pathfinding")
                        {
                            tbxOutput = string.Format("No path found. Pathfinding stopped");
                            outputToTbxOutput(tbxOutput);

                            i = pnlGrid.Controls.Count - 1;
                        }

                        tabOutput.Refresh();
                    }

                    btnStartPathfind.Text = "Reset pathfinding";

                    Global.mapReadyToEdit = false;
                    Global.allowEditPoints = false;
                    Global.donePathfinding = true;

                    tbxOutput = string.Format("...pathfinding ended");
                    outputToTbxOutput(tbxOutput);
                }
                //Too little path points
                else
                {
                    tbxOutput = string.Format("...too little path points on map");
                    outputToTbxOutput(tbxOutput);

                    MessageBox.Show("No start and end point defined", "Not enough path points", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            //Resetting pathfinding
            else if (btnStartPathfind.Text == "Reset pathfinding")
            {
                foreach (Panel pnlScreen in pnlGrid.Controls)
                {
                    PictureBox pbxGrid = (PictureBox)pnlScreen.Controls[0];
                    initPbxGridOverlay(pbxGrid);
                }

                resetPathfinding();
            }

            //SAVE BELOW FOR CONSTRUCTING COMPLETE GRAPHS
            //if (Global.lstPointsToPathfind.Count > 2)
            //{
            //    //Finding paths to all points
            //    for (int i = 0; i < Global.lstPointsToPathfind.Count; i++)
            //    {
            //        for (int j = i + 1; j < Global.lstPointsToPathfind.Count; j++)
            //        {
            //            //NEED TO REFRESH BITMAP FOR OLD GREY TILES
            //            pathfindBFS(pbxOverlay, Global.lstPointsToPathfind[i], Global.lstPointsToPathfind[j]);
            //        }
            //    }
            //}
            //else if (Global.lstPointsToPathfind.Count == 2)
            //{
            //    pathfindBFS(pbxOverlay, Global.lstPointsToPathfind[0], Global.lstPointsToPathfind[1]);
            //}
            //else
            //{
            //    MessageBox.Show("No start and end point defined", "Not enough path points", MessageBoxButtons.OK, MessageBoxIcon.Information);
            //}         
        }

        private void startPathfinding(Panel pnlScreenOptions, PictureBox pbxOverlay, ComboBox cmbAlgorithm)
        {
            if (cmbAlgorithm.Text == "BFS")
            {
                pathfindBFS(pbxOverlay, Global.lstPointsToPathfind[0], Global.lstPointsToPathfind[1]);
            }
            else if (cmbAlgorithm.Text == "Dijkstra")
            {
                pathfindDijkstra(pbxOverlay, Global.lstPointsToPathfind[0], Global.lstPointsToPathfind[1]);
            }
            else if (cmbAlgorithm.Text == "A*")
            {
                ComboBox cmbHeuristic = (ComboBox)pnlScreenOptions.Controls.Find("cmbHeuristic", false).FirstOrDefault();
                string heuristic = cmbHeuristic.Text.ToUpper();
                pathfindAStar(pbxOverlay, Global.lstPointsToPathfind[0], Global.lstPointsToPathfind[1], heuristic);
            }
        }

        private void resetPathfinding()
        {
            Button btnStartPathfind = (Button)this.Controls.Find("btnStartPathfind", false).FirstOrDefault();
            btnStartPathfind.Text = "Start pathfinding";

            Panel pnlAddPoints = (Panel)tabScreen.Controls.Find("pnlAddPoints", false).FirstOrDefault();
            Label lblAddPoints = (Label)pnlAddPoints.Controls.Find("lblAddPoints", false).FirstOrDefault();
            lblAddPoints.ForeColor = Color.Black;
            lblAddPoints.Text = "Path points edit mode: OFF";
            Global.allowEditPoints = false;

            Global.mapReadyToEdit = true;
            Global.donePathfinding = false;

            string tbxOutput = "";
            tbxOutput = string.Format("Pathfinding reset");
            outputToTbxOutput(tbxOutput);
        }

        private void clearAllPathPoints()
        {
            Panel pnlScreen = (Panel)pnlGrid.Controls[0];
            PictureBox pbxGrid = (PictureBox)pnlScreen.Controls[0];
            PictureBox pbxOverlay = (PictureBox)pbxGrid.Controls[0];
            Bitmap bmpOverlay = (Bitmap)pbxOverlay.Image;

            foreach (string strCoords in Global.lstPointsToPathfind)
            {
                Point coords = coordsStringToPoint(strCoords);

                //Erases X on all overlays
                foreach (Panel screen in pnlGrid.Controls)
                {
                    PictureBox grid = (PictureBox)screen.Controls[0];
                    PictureBox overlay = (PictureBox)grid.Controls[0];
                    Bitmap bmp = (Bitmap)overlay.Image;

                    //Erases cross
                    Graphics gfx = Graphics.FromImage(bmp);
                    gfx.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;      //Replaces pixels rather than blend
                    gfx.FillRectangle(Brushes.Transparent, coords.X * Global.tileLength + 1, coords.Y * Global.tileLength + 1, Global.tileLength - 1, Global.tileLength - 1);
                }
            }
            pnlGrid.Refresh();

            Global.lstPointsToPathfind.Clear();

            string tbxOutput = "";
            tbxOutput = string.Format("All path points cleared");
            outputToTbxOutput(tbxOutput);
        }



        #region PathfindingAlgorithms

        //NOTE: BFS can only handle black and white - If coloured, treat as impassable (wall)
        //(Colour key: Yellow = Working, Grey = Seen)
        private void pathfindBFS(PictureBox pbxOverlay, string startPoint, string endPoint)
        {
            /* BFS Algorithm:
             *
             * Add start point to queue tilesToCheck
             * Add start point with layerNo = 0 to dictionary tilesSeen
             *
             * While tilesToCheck NOT empty
             *     currTile = tilesToCheck.Dequeue
             *     Mark currTile as 'Working' and display on overlay
             *   
             *     For each nextTile adjacent to currTile
             *          If nextTile within bounds, not marked as seen, and has 'Empty' tileType
             *              If nextTile = endPoint
             *                  Clear tilesToCheck
             *                  pathFound = True
             *              Else
             *                  tilesToCheck.Enqueue(nextTile)
             *                  Mark nextTile as 'Seen' and display on overlay
             *                
             *          Add nextTile with its layerNo into tilesSeen
             *        
             *     Mark currTile as 'Seen' and display on overlay      
             *
             * If pathFound
             *      Trace Shortest Path
             * Else
             *      Output "Could not find path to destination" 
             */

            PictureBox pbxGrid = (PictureBox)pbxOverlay.Parent;
            Panel pnlScreen = (Panel)pbxGrid.Parent;
            string tbxOutput = "";
            tbxOutput = string.Format("Pathfinding through {0} with BFS...", pnlScreen.Name);
            outputToTbxOutput(tbxOutput);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            Queue<string> tilesToCheck = new Queue<string>();        //Holds coordinates of tiles to be checked
            Dictionary<string, int> tilesSeen = new Dictionary<string, int>();      //Holds tiles seen and layer no.
            bool pathFound = false;

            //Enqueue Start Pos
            Point startPointCoords = coordsStringToPoint(startPoint);
            tilesToCheck.Enqueue(startPoint);
            tilesSeen.Add(startPoint, 0);

            while (tilesToCheck.Count > 0)
            {
                string currTileCoords = tilesToCheck.Dequeue();

                //Index: 0 = x, 1 = y
                Point currCoords = coordsStringToPoint(currTileCoords);

                //Recolour current tile status
                setOverlayTileStatus(pbxOverlay, currTileCoords, Color.LightYellow);         //Yellow - Working tile

                //Look at adjacent tiles - dir = Checking direction (up/left/down/right) = (0/1/2/3)
                for (int dir = 0; dir <= 3; dir++)
                {
                    int nextX = 0, nextY = 0;

                    switch (dir)
                    {
                        case 0:     //UP - (X, Y - 1)
                            nextX = currCoords.X;
                            nextY = currCoords.Y - 1;
                            break;
                        case 1:     //LEFT - (X - 1, Y)
                            nextX = currCoords.X - 1;
                            nextY = currCoords.Y;
                            break;
                        case 2:     //DOWN - (X, Y + 1)
                            nextX = currCoords.X;
                            nextY = currCoords.Y + 1;
                            break;
                        case 3:     //RIGHT - (X + 1, Y)
                            nextX = currCoords.X + 1;
                            nextY = currCoords.Y;
                            break;
                    }

                    //If within bounds
                    if (nextX >= 0 && nextX < Global.noOfTilesX && nextY >= 0 && nextY < Global.noOfTilesY)
                    {
                        Tile nextTile = Global.lstMapRow[nextY][nextX];

                        //If next tile NOT seen AND empty
                        if (!tilesSeen.ContainsKey(nextTile.getCoords()) && nextTile.getTileType() == "Empty")
                        {
                            //If next tile is DESTINATION
                            if (nextTile.getCoords() == endPoint)
                            {
                                //End Search
                                tilesToCheck.Clear();
                                dir = 3;
                                pathFound = true;
                            }
                            else
                            {
                                //Enqueue next tile, mark as seen
                                tilesToCheck.Enqueue(nextTile.getCoords());

                                setOverlayTileStatus(pbxOverlay, nextTile.getCoords(), Color.Orange);      //Orange - Looking
                                revertTileOverlayColour(ref pbxOverlay, nextTile.getCoords());
                            }

                            tilesSeen.Add(nextTile.getCoords(), tilesSeen[currTileCoords] + 1);
                        }
                    }
                }

                revertTileOverlayColour(ref pbxOverlay, currTileCoords);
            }

            stopWatch.Stop();

            if (pathFound)
            {
                tbxOutput = string.Format("Path found!");
                outputToTbxOutput(tbxOutput);

                BFSOutputPath(pbxOverlay, ref tilesSeen, endPoint);
                tbxOutput = string.Format("Shortest path output to screen");
                outputToTbxOutput(tbxOutput);
            }
            else
            {
                //How to solve for multiple nodes? - To SOME destinations
                MessageBox.Show("Could not find path to destination", "No path", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //Change 'start pathfinding' button to 'reset'
                Button btnStartPathfind = (Button)this.Controls.Find("btnStartPathfind", false).FirstOrDefault();
                btnStartPathfind.Text = "Reset pathfinding";
            }

            tbxOutput = string.Format("...pathfinding complete in {0} ms", stopWatch.ElapsedMilliseconds);
            outputToTbxOutput(tbxOutput);
        }

        private bool isNextTilePath(PictureBox pbxOverlay, string tileCoords)
        {
            bool isTilePath = true;

            Color tileColour = getOverlayTileStatus(pbxOverlay, tileCoords);
            if (tileColour.ToArgb() != Global.pathColour.ToArgb())
            {
                isTilePath = false;
            }

            return isTilePath;
        }

        //Trace shortest path from end to start -Note: There may be multiple shortest paths, this is one of them
        private void BFSOutputPath(PictureBox pbxOverlay, ref Dictionary<string, int> tilesSeen, string endPoint)
        {
            /* Tracing shortest path [BFS]:
             *
             * layerNo = endPoint's layerNo
             * Push endPoint into shortestPath Stack
             * currTile = endPoint
             *
             * While layerNo > 0
             *      For each nextTile adjacent to currTile
             *          If dictionary tilesSeen contains currTile as key
             *              If layerNo of currTile = layerNo - 1
             *                  Push currTile into shortestPath      
             *
             * While shortestPath NOT empty
             *      pathTile = shortestPath.Pop
             *      Mark pathTile as 'Path' and display on overlay
             */

            Stack<string> shortestPath = new Stack<string>();
            int layerNo = tilesSeen[endPoint];
            string currCoords = endPoint;
            int nextX = 0, nextY = 0;

            shortestPath.Push(endPoint);

            while (layerNo > 0)
            {
                Point coords = coordsStringToPoint(currCoords);

                //Look at adjacent tiles - dir = Checking direction (up/left/down/right) = (0/1/2/3)
                for (int dir = 0; dir <= 3; dir++)
                {
                    switch (dir)
                    {
                        case 0:     //UP - (X, Y - 1)
                            nextX = coords.X;
                            nextY = coords.Y - 1;
                            break;
                        case 1:     //LEFT - (X - 1, Y)
                            nextX = coords.X - 1;
                            nextY = coords.Y;
                            break;
                        case 2:     //DOWN - (X, Y + 1)
                            nextX = coords.X;
                            nextY = coords.Y + 1;
                            break;
                        case 3:     //RIGHT - (X + 1, Y)
                            nextX = coords.X + 1;
                            nextY = coords.Y;
                            break;
                    }

                    //If adjacent tile is on last layer, add to stack. Continue searching for previous layer
                    currCoords = nextX + "," + nextY;
                    if (tilesSeen.ContainsKey(currCoords))
                    {
                        if (tilesSeen[currCoords] == layerNo - 1)
                        {
                            shortestPath.Push(currCoords);
                            dir = 3;
                            layerNo--;
                        }
                    }
                }
            }

            //Output shortest path
            while (shortestPath.Count > 0)
            {
                string pathCoords = shortestPath.Pop();
                setOverlayTileStatus(pbxOverlay, pathCoords, Color.DarkRed);

                //Retains 'X's on overlay
                if (tilesSeen[pathCoords] == 0 || pathCoords == endPoint)
                {
                    drawXOnOverlay((Bitmap)pbxOverlay.Image, pathCoords);
                    pbxOverlay.Refresh();
                }
            }
        }

        //BEWARE OF OBTAINING DISTANCES THROUGH VERY LARGE WEIGHTED NODES - OVERFLOW
        private void pathfindDijkstra(PictureBox pbxOverlay, string startPoint, string endPoint)
        {
            /* Dijkstra's Algorithm:
             *
             * Add start point (key) with tentative distance of 0 (val) to dictionary tilesToCheck
             * Add start point (key) with tentative distance of 0 and previous connected tile of "" (val) dictionary tilesSeen
             *
             * While tilesToCheck NOT empty
             *     currTile = Node with least tentative distance
             *     Remove this node from dictionary tilesToCheck
             *     Mark currTile as 'Working' and display on overlay
             *   
             *     For each nextTile adjacent to currTile
             *          If nextTile within bounds, not marked as seen
             *              If nextTile = endPoint
             *                  Clear tilesToCheck
             *                  pathFound = True
             *              Else
             *                  nextTileDist = currTile's tentDist + nextTile's weight
             *                  tilesToCheck.Add(nextTileCoords, nextTileDist);
             *                  Mark nextTile as 'Seen' and display on overlay
             *                
             *                  Add nextTile with nextTileDist and currTile (as previous connected tile) into tilesSeen
             *                
             *          Else If tile previously seen
             *              Mark nextTile as 'Updating' and display on overlay
             *              Compare nextTile's current tentative distance with nextTileDist
             *              Update nextTile's tentative distance if nextTileDist is less than the current value
             *              Mark nextTile as 'Seen' and display on overlay
             *                      
             *     Mark currTile as 'Seen' and display on overlay      
             *
             * If pathFound
             *      Trace Shortest Path
             * Else
             *      Output "Could not find path to destination"  
             */

            PictureBox pbxGrid = (PictureBox)pbxOverlay.Parent;
            Panel pnlScreen = (Panel)pbxGrid.Parent;
            string tbxOutput = "";
            tbxOutput = string.Format("Pathfinding through {0} with Dijkstra...", pnlScreen.Name);
            outputToTbxOutput(tbxOutput);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //Holds coordinates of tiles to check (key) and tentative distance (val)
            PriorityListDijkstra tilesToCheck = new PriorityListDijkstra();

            //Holds tilesSeen (key) and (tentative distance, previous connected tile) (val)
            Dictionary<string, Tuple<int, string>> tilesSeen = new Dictionary<string, Tuple<int, string>>();
            bool pathFound = false;

            //Enqueue Start Pos
            tilesToCheck.Add(new Tuple<int, string>(0, startPoint));
            tilesSeen.Add(startPoint, new Tuple<int, string>(0, ""));

            while (tilesToCheck.Count() > 0)
            {
                //Dequeue tile 
                Tuple<int, string> currTileInfo = tilesToCheck.dequeue();
                int currTileDist = currTileInfo.Item1;
                string currTileCoords = currTileInfo.Item2;

                //Index: 0 = x, 1 = y
                Point currCoords = coordsStringToPoint(currTileCoords);

                //Recolour current tile status
                setOverlayTileStatus(pbxOverlay, currTileCoords, Color.LightYellow);        //Yellow - Working tile

                //Look at adjacent tiles - dir = Checking direction (up/left/down/right) = (0/1/2/3)
                for (int dir = 0; dir <= 3; dir++)
                {
                    int nextX = 0, nextY = 0;

                    switch (dir)
                    {
                        case 0:     //UP - (X, Y - 1)
                            nextX = currCoords.X;
                            nextY = currCoords.Y - 1;
                            break;
                        case 1:     //LEFT - (X - 1, Y)
                            nextX = currCoords.X - 1;
                            nextY = currCoords.Y;
                            break;
                        case 2:     //DOWN - (X, Y + 1)
                            nextX = currCoords.X;
                            nextY = currCoords.Y + 1;
                            break;
                        case 3:     //RIGHT - (X + 1, Y)
                            nextX = currCoords.X + 1;
                            nextY = currCoords.Y;
                            break;
                    }

                    //If within bounds
                    if (nextX >= 0 && nextX < Global.noOfTilesX && nextY >= 0 && nextY < Global.noOfTilesY)
                    {
                        Tile nextTile = Global.lstMapRow[nextY][nextX];
                        int nextTileDist = currTileDist + nextTile.getWeight();

                        //If next tile NOT wall
                        if (nextTile.getTileType() != "Wall")
                        {
                            //If next tile NOT seen
                            if (!tilesSeen.ContainsKey(nextTile.getCoords()))
                            {
                                //If next tile is DESTINATION
                                if (nextTile.getCoords() == endPoint)
                                {
                                    //End Search
                                    tilesToCheck.Clear();
                                    dir = 3;
                                    pathFound = true;
                                }
                                else
                                {
                                    //Enqueue next tile, mark as seen
                                    tilesToCheck.Add(new Tuple<int, string>(nextTileDist, nextTile.getCoords()));

                                    setOverlayTileStatus(pbxOverlay, nextTile.getCoords(), Color.Orange);      //Orange - Looking
                                    revertTileOverlayColour(ref pbxOverlay, nextTile.getCoords());
                                }

                                tilesSeen.Add(nextTile.getCoords(), new Tuple<int, string>(nextTileDist, currTileCoords));
                            }

                            //If tile previously seen, compare existing tentative distance with new one
                            else
                            {
                                //If tile not yet checked, update tentative distance if new one is less than current
                                if (tilesToCheck.Contains(nextTile.getCoords()) && nextTileDist < tilesSeen[nextTile.getCoords()].Item1)
                                {
                                    //MARK nextTile AS UPDATING
                                    setOverlayTileStatus(pbxOverlay, nextTile.getCoords(), Color.Blue);         //Blue - Updating
                                    tilesSeen[nextTile.getCoords()] = new Tuple<int, string>(nextTileDist, currTileCoords);
                                    revertTileOverlayColour(ref pbxOverlay, nextTile.getCoords());
                                }
                            }
                        }
                    }
                }

                revertTileOverlayColour(ref pbxOverlay, currTileCoords);
            }

            stopWatch.Stop();

            if (pathFound)
            {
                tbxOutput = string.Format("Path found!");
                outputToTbxOutput(tbxOutput);

                DijkstraOutputPath(pbxOverlay, ref tilesSeen, startPoint, endPoint);
                tbxOutput = string.Format("Shortest path output to screen");
                outputToTbxOutput(tbxOutput);
            }
            else
            {
                //How to solve for multiple nodes? - To SOME destinations
                MessageBox.Show("Could not find path to destination", "No path", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //Change 'start pathfinding' button to 'reset'
                Button btnStartPathfind = (Button)this.Controls.Find("btnStartPathfind", false).FirstOrDefault();
                btnStartPathfind.Text = "Reset pathfinding";
            }

            tbxOutput = string.Format("...pathfinding complete in {0} ms", stopWatch.ElapsedMilliseconds);
            outputToTbxOutput(tbxOutput);
        }

        //Trace shortest path from end to start -Note: There may be multiple shortest paths, this is one of them
        private void DijkstraOutputPath(PictureBox pbxOverlay, ref Dictionary<string, Tuple<int, string>> tilesSeen, string startPoint, string endPoint)
        {
            /* Tracing shortest path [Dijkstra]:
             *
             * Push endPoint into shortestPath Stack
             * currCoords = endPoint
             *
             * While currCoords != startPoint
             *      prevCoords = currCoords' previous coordinates from tilesSeen
             *      Push prevCoords into shortestPath
             *      currCoords = prevCoords
             *
             * While shortestPath NOT empty
             *      pathTile = shortestPath.Pop
             *      Mark pathTile as 'Path' and display on overlay
             */

            Stack<string> shortestPath = new Stack<string>();
            string currCoords = endPoint;

            shortestPath.Push(endPoint);

            //Backtrack to startPoint by going through previously connected tiles from endPoint
            while (currCoords != startPoint)
            {
                string prevCoords = tilesSeen[currCoords].Item2;
                shortestPath.Push(prevCoords);
                currCoords = prevCoords;
            }

            //Output shortest path
            while (shortestPath.Count > 0)
            {
                string pathCoords = shortestPath.Pop();
                setOverlayTileStatus(pbxOverlay, pathCoords, Color.DarkRed);

                //Retains 'X's on overlay
                if (tilesSeen[pathCoords].Item1 == 0 || pathCoords == endPoint)
                {
                    drawXOnOverlay((Bitmap)pbxOverlay.Image, pathCoords);
                    pbxOverlay.Refresh();
                }
            }
        }



        //BEWARE OF OBTAINING DISTANCES THROUGH VERY LARGE WEIGHTED NODES - OVERFLOW
        private void pathfindAStar(PictureBox pbxOverlay, string startPoint, string endPoint, string heuristic)
        {
            /* A* Algorithm:
             *
             * Add start point (key) with G-score of 0, and F-score of 0 (val) to dictionary tilesToCheck
             * Add start point (key) with G-score of 0, F-score of 0 and previous connected tile of "" (val) dictionary tilesSeen
             *
             * While tilesToCheck NOT empty
             *     currTile = Node with least F-score
             *     Remove this node from dictionary tilesToCheck
             *     Mark currTile as 'Working' and display on overlay
             *   
             *     For each nextTile adjacent to currTile
             *          If nextTile within bounds, not marked as seen
             *              If nextTile = endPoint
             *                  Clear tilesToCheck
             *                  pathFound = True
             *              Else
             *                  newGscore = currTile's tentDist + nextTile's weight
             *                
             *                  x = Magnitude of x-dist of nextTile from endPoint
             *                  y = Magnitude of y-dist of nextTile from endPoint
             *                  If heuristic = Manhattan
             *                      newHscore = x + y
             *                  Else If heuristic = Euclidean
             *                      newHscore = sqrt(x^2 + y^2)
             *                  Else if heuristic = Chebyshev
             *                      If x >= y
             *                          newHscore = x
             *                      Else
             *                          newHscore = y                                           
             *                  newFscore = newGscore + newHscore
             *                
             *                  tilesToCheck.Add(nextTileCoords, (newGscore, newFscore));
             *                  Mark nextTile as 'Seen' and display on overlay
             *                
             *                  Add nextTile with newGscore, newFscore, and currTile (as previous connected tile) into tilesSeen
             *                
             *          Else If tile previously seen
             *              Mark nextTile as 'Updating' and display on overlay
             *              Compare nextTile's current F-Score with newFscore
             *              Update nextTile's G-score and F-score if newFscore is less than the current value
             *              Mark nextTile as 'Seen' and display on overlay
             *                      
             *     Mark currTile as 'Seen' and display on overlay      
             *
             * If pathFound
             *      Trace Shortest Path
             * Else
             *      Output "Could not find path to destination"
             */

            PictureBox pbxGrid = (PictureBox)pbxOverlay.Parent;
            Panel pnlScreen = (Panel)pbxGrid.Parent;
            string tbxOutput = "";
            tbxOutput = string.Format("Pathfinding through {0} with A* - {1}...", pnlScreen.Name, heuristic);
            outputToTbxOutput(tbxOutput);

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //List to hold G-Score, F-Score, Coords - Acts as priority queue and used to refer to tilesToCheck dictionary
            PriorityListAStar tilesToCheck = new PriorityListAStar();

            //Holds coordinates of tiles seen (key) and (G-Score, F-score, previous connected tile) (val)
            Dictionary<string, Tuple<int, int, string>> tilesSeen = new Dictionary<string, Tuple<int, int, string>>();
            bool pathFound = false;

            //Enqueue Start Pos
            tilesToCheck.Add(new Tuple<int, int, string>(0, 0, startPoint));
            tilesSeen.Add(startPoint, new Tuple<int, int, string>(0, 0, ""));

            while (tilesToCheck.Count() > 0)
            {
                //Dequeue tile 
                Tuple<int, int, string> currTile = tilesToCheck.dequeue();
                int currTileG = currTile.Item1;
                int currTileF = currTile.Item2;
                string currTileCoords = currTile.Item3;

                //Index: 0 = x, 1 = y
                Point currCoords = coordsStringToPoint(currTileCoords);

                //Recolour current tile status
                setOverlayTileStatus(pbxOverlay, currTileCoords, Color.LightYellow);        //Yellow - Working tile

                //Look at adjacent tiles - dir = Checking direction (up/left/down/right) = (0/1/2/3)
                for (int dir = 0; dir <= 3; dir++)
                {
                    int nextX = 0, nextY = 0;

                    switch (dir)
                    {
                        case 0:     //UP - (X, Y - 1)
                            nextX = currCoords.X;
                            nextY = currCoords.Y - 1;
                            break;
                        case 1:     //LEFT - (X - 1, Y)
                            nextX = currCoords.X - 1;
                            nextY = currCoords.Y;
                            break;
                        case 2:     //DOWN - (X, Y + 1)
                            nextX = currCoords.X;
                            nextY = currCoords.Y + 1;
                            break;
                        case 3:     //RIGHT - (X + 1, Y)
                            nextX = currCoords.X + 1;
                            nextY = currCoords.Y;
                            break;
                    }

                    //If within bounds
                    if (nextX >= 0 && nextX < Global.noOfTilesX && nextY >= 0 && nextY < Global.noOfTilesY)
                    {
                        Tile nextTile = Global.lstMapRow[nextY][nextX];

                        //If next tile NOT wall
                        if (nextTile.getTileType() != "Wall")
                        {
                            //Calculate F-Score
                            int nextTileG = currTileG + nextTile.getWeight();

                            //Heuristic calculation
                            Point nextTileCoords = coordsStringToPoint(nextTile.getCoords());
                            Point endPointCoords = coordsStringToPoint(endPoint);
                            int nextTileH = getHScore(nextTileCoords, endPointCoords, heuristic);

                            int nextTileF = nextTileG + nextTileH;

                            //If next tile NOT seen
                            if (!tilesSeen.ContainsKey(nextTile.getCoords()))
                            {
                                //If next tile is DESTINATION
                                if (nextTile.getCoords() == endPoint)
                                {
                                    //End Search
                                    tilesToCheck.Clear();
                                    dir = 3;
                                    pathFound = true;
                                }
                                else
                                {
                                    //Enqueue next tile, mark as seen
                                    tilesToCheck.Add(new Tuple<int, int, string>(nextTileG, nextTileF, nextTile.getCoords()));

                                    setOverlayTileStatus(pbxOverlay, nextTile.getCoords(), Color.Orange);      //Orange - Looking
                                    revertTileOverlayColour(ref pbxOverlay, nextTile.getCoords());
                                }

                                tilesSeen.Add(nextTile.getCoords(), new Tuple<int, int, string>(nextTileG, nextTileF, currTileCoords));
                            }

                            //If tile previously seen, compare existing F-score with new one
                            else
                            {
                                //If tile not yet checked, update F-Score if new one is less than current
                                if (tilesToCheck.Contains(nextTile.getCoords()) && nextTileF < tilesSeen[nextTile.getCoords()].Item2)
                                {
                                    //MARK nextTile AS UPDATING
                                    setOverlayTileStatus(pbxOverlay, nextTile.getCoords(), Color.Blue);         //Blue - Updating
                                    tilesSeen[nextTile.getCoords()] = new Tuple<int, int, string>(nextTileG, nextTileF, currTileCoords);
                                    revertTileOverlayColour(ref pbxOverlay, nextTile.getCoords());
                                }
                            }
                        }
                    }
                }

                revertTileOverlayColour(ref pbxOverlay, currTileCoords);
            }

            stopWatch.Stop();

            if (pathFound)
            {
                tbxOutput = string.Format("Path found!");
                outputToTbxOutput(tbxOutput);

                AStarOutputPath(pbxOverlay, ref tilesSeen, startPoint, endPoint);
                tbxOutput = string.Format("Shortest path output to screen");
                outputToTbxOutput(tbxOutput);
            }
            else
            {
                //How to solve for multiple nodes? - To SOME destinations
                MessageBox.Show("Could not find path to destination", "No path", MessageBoxButtons.OK, MessageBoxIcon.Information);

                //Change 'start pathfinding' button to 'reset'
                Button btnStartPathfind = (Button)this.Controls.Find("btnStartPathfind", false).FirstOrDefault();
                btnStartPathfind.Text = "Reset pathfinding";
            }

            tbxOutput = string.Format("...pathfinding complete in {0} ms", stopWatch.ElapsedMilliseconds);
            outputToTbxOutput(tbxOutput);
        }

        private int getHScore(Point nextTileCoords, Point endPointCoords, string heuristic)
        {
            int nextTileH = 0;
            int distToEndX = Math.Abs(endPointCoords.X - nextTileCoords.X);
            int distToEndY = Math.Abs(endPointCoords.Y - nextTileCoords.Y);

            if (heuristic == "MANHATTAN")
            {
                nextTileH = distToEndX + distToEndY;
            }
            else if (heuristic == "EUCLIDEAN")
            {
                nextTileH = (int)Math.Round(Math.Sqrt((distToEndX * distToEndX) + (distToEndY * distToEndY)));
            }
            else if (heuristic == "CHEBYSHEV")
            {
                if (distToEndX >= distToEndY)
                {
                    nextTileH = distToEndX;
                }
                else
                {
                    nextTileH = distToEndY;
                }
            }

            return nextTileH;
        }

        private void AStarOutputPath(PictureBox pbxOverlay, ref Dictionary<string, Tuple<int, int, string>> tilesSeen, string startPoint, string endPoint)
        {
            /* Tracing shortest path [A*]:
             *
             * Push endPoint into shortestPath Stack
             * currCoords = endPoint
             *
             * While currCoords != startPoint
             *      prevCoords = currCoords' previous coordinates from tilesSeen
             *      Push prevCoords into shortestPath
             *      currCoords = prevCoords
             *
             * While shortestPath NOT empty
             *      pathTile = shortestPath.Pop
             *      Mark pathTile as 'Path' and display on overlay
             */

            Stack<string> shortestPath = new Stack<string>();
            string currCoords = endPoint;

            shortestPath.Push(endPoint);

            //Backtrack to startPoint by going through previously connected tiles from endPoint
            while (currCoords != startPoint)
            {
                string prevCoords = tilesSeen[currCoords].Item3;
                shortestPath.Push(prevCoords);
                currCoords = prevCoords;
            }

            //Output shortest path
            while (shortestPath.Count > 0)
            {
                string pathCoords = shortestPath.Pop();
                setOverlayTileStatus(pbxOverlay, pathCoords, Color.DarkRed);

                //Retains 'X's on overlay
                if (tilesSeen[pathCoords].Item1 == 0 || pathCoords == endPoint)
                {
                    drawXOnOverlay((Bitmap)pbxOverlay.Image, pathCoords);
                    pbxOverlay.Refresh();
                }
            }
        }

        private void revertTileOverlayColour(ref PictureBox pbxOverlay, string tileCoords)
        {
            //Mark nextTile AS SEEN unless path
            if (isNextTilePath(pbxOverlay, tileCoords))
            {
                setOverlayTileStatus(pbxOverlay, tileCoords, Color.DarkRed);            //DarkRed - Path
            }
            else
            {
                setOverlayTileStatus(pbxOverlay, tileCoords, Color.LightGray);      //Grey - Seen
            }

            //Retains 'X's on overlay
            if (Global.lstPointsToPathfind.Contains(tileCoords))
            {
                drawXOnOverlay((Bitmap)pbxOverlay.Image, tileCoords);
                pbxOverlay.Refresh();
            }
        }

        #endregion

        #endregion



        #region testers
        //Output tileTypes
        private void outputPanelListDict()
        {
            Console.WriteLine("Panel--------------");
            foreach (Panel pnl in tabTiles.Controls.OfType<Panel>())
            {
                Console.WriteLine(pnl.Name);
            }

            Console.WriteLine("List of keys--------------");
            foreach (string pnlKey in Global.lstDicTileTypeKeys)
            {
                Console.WriteLine(pnlKey);
            }

            Console.WriteLine("Dictionary Info entries--------------");
            foreach (KeyValuePair<string, Tuple<Color, int>> dicEntry in Global.dicTileTypeInfo)
            {
                Console.WriteLine(dicEntry);
            }

            Console.WriteLine("Dictionary Tiles entries--------------");
            foreach (KeyValuePair<string, HashSet<string>> dicEntry in Global.dicTileTypeTiles)
            {
                Console.WriteLine(dicEntry);
                foreach (string coords in dicEntry.Value)
                {
                    Console.WriteLine(coords);
                }
            }

            Console.WriteLine("List of Path Points--------------");
            foreach (string pathPoints in Global.lstPointsToPathfind)
            {
                Console.WriteLine(pathPoints);
            }
        }

        //OUTPUT MAP - To text file - Keep for saving and opening
        private void testOutputMap()
        {
            StreamWriter strmWrite = new StreamWriter(@"C:\Users\AnnihilatorX3000\Desktop\PathfindingOutput.txt", false);

            for (int row = 0; row < Global.noOfTilesY; row++)
            {
                for (int column = 0; column < Global.noOfTilesX; column++)
                {
                    Rectangle rect = new Rectangle((column * Global.tileLength), (row * Global.tileLength), Global.tileLength, Global.tileLength);

                    //Obtain coords
                    Tile tempTile = Global.lstMapRow[row][column];
                    string tileCoords = tempTile.getCoords();

                    strmWrite.Write(tileCoords + " ");
                }
                strmWrite.WriteLine();
            }

            strmWrite.WriteLine("\n");

            for (int row = 0; row < Global.noOfTilesY; row++)
            {
                for (int column = 0; column < Global.noOfTilesX; column++)
                {
                    Rectangle rect = new Rectangle((column * Global.tileLength), (row * Global.tileLength), Global.tileLength, Global.tileLength);

                    //Obtain coords
                    Tile tempTile = Global.lstMapRow[row][column];
                    string tileType = tempTile.getTileType();

                    strmWrite.Write(tileType + " ");
                }
                strmWrite.WriteLine();
            }

            strmWrite.Close();
        }

        private void screenOverlayTester(PictureBox pbxGrid, PictureBox pbxOverlay)
        {
            Bitmap bmpOverlay = (Bitmap)pbxOverlay.Image;
            if (pbxGrid.Width != pbxOverlay.Width)
            {
                Console.WriteLine("pbxGridWidth != pbxOverlayWidth");
            }
            if (pbxOverlay.Width != bmpOverlay.Width)
            {
                Console.WriteLine("pbxOverlayWidth != bmpOverlayWidth");
            }
            if (pbxGrid.Height != pbxOverlay.Height)
            {
                Console.WriteLine("pbxGridHeight != pbxOverlayHeight");
            }
            if (pbxOverlay.Height != bmpOverlay.Height)
            {
                Console.WriteLine("pbxOverlayHeight != bmpOverlayHeight");
            }
        }

        private void outputScreenList()
        {
            Console.WriteLine("Screen List--------------");
            foreach (string str in Global.lstScreens)
            {
                Console.WriteLine(str);
            }
        }

        private void outputTilesSeen(ref Dictionary<string, int> tilesSeen)
        {
            Console.WriteLine("Tiles Seen---------------------");
            foreach (KeyValuePair<string, int> tile in tilesSeen)
            {
                Console.WriteLine(tile.Key + " : " + tile.Value);
            }
        }

        private void outputTilesSeen(ref Dictionary<string, Tuple<int, string>> tilesSeen)
        {
            Console.WriteLine("Tiles Seen---------------------");
            foreach (KeyValuePair<string, Tuple<int, string>> tile in tilesSeen)
            {
                Console.WriteLine(tile.Key + " : " + tile.Value.Item1);
            }
        }

        private void outputTilesSeen(ref Dictionary<string, Tuple<int, int, string>> tilesSeen)
        {
            Console.WriteLine("Tiles Seen---------------------");
            foreach (KeyValuePair<string, Tuple<int, int, string>> tile in tilesSeen)
            {
                Console.WriteLine(tile.Key + ", G: " + tile.Value.Item1 + ", F: " + tile.Value.Item2 + ", Prev: " + tile.Value.Item3);
            }
        }


        #endregion
    }
}























//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.Data;
//using System.Drawing;
//using System.Linq;
//using System.Text;
//using System.Threading.Tasks;
//using System.Windows.Forms;
//using System.Diagnostics;

//*BUG LIST(*) / ADDITIONS(.)
// * 
// * ZOOMING, PANNING, HOVERING, DRAWING
// *  1a. Start program maximized. Position components and resize according to design of form (Comes in later)
// *  1b. Implement some algorithm to determine pbxGrid dimensions based on screen size and number of tiles
// *  1c. mapTileLength - What if not all fits? (< 1 px.)
// *  1d. If grid display is off, final grid line will display white line on right and bottom. Colour these empty pixels with form background colour
// *  1e. Smoothen movement of camera centre to cursor pos on map - Linear interpolation?
// *  1f. Use different method to allow multi-key panning support, and also at no delay
// *  1g* 'Out of memory' exception on bitmaps - Cropping out of bounds?
// *  1h. Possible solution to out of memory: Replace timer to detect mouse hover with 'Mouse Move' create global mouse state and use to highlight square
// *
// *  
// *  
// */

//namespace VB_CSharpPathfindingTester
//{
//    public partial class Form1 : Form
//    {
//        public class Tile
//        {
//            //Name, TileType 
//            public string tileCoords;
//            public string tileType;

//            //Constructor
//            public Tile(string _tileCoords, string _tileType)
//            {
//                tileCoords = _tileCoords;
//                tileType = _tileType;
//            }

//            public string getCoords()
//            {
//                return tileCoords;
//            }

//            public string getType()
//            {
//                return tileType;
//            }

//            //Refers to tileType dictionary
//            public Color getColour()
//            {
//                Color tileColour =  Global.dicTileType[tileType].Item1;
//                return tileColour;
//            }

//            public int getWeight()
//            {
//                int tileWeight = Global.dicTileType[tileType].Item2;
//                return tileWeight;
//            }
//        }

//        public static class Global      //GLOBAL VARIABLES
//        {
//            //TileType Dictionary
//            public static Dictionary<string, Tuple<Color, int>> dicTileType = new Dictionary<string, Tuple<Color, int>>();

//            //Map - Original variables
//            public static List<List<Tile>> MapRow = new List<List<Tile>>();   //Contains entire map
//            public static int mapNoOfTilesX, mapNoOfTilesY;   //Determined by user
//            public static int mapTileLength;          //pbxGridXY (-1) / noOfTiles

//            //Grid
//            public static Bitmap bmpGridFull;

//            //Zooming
//            public static decimal zoomSens;

//            public static decimal gridNoOfTilesX, gridNoOfTilesY;
//            public static decimal gridTileLength;       //Pixels per tile
//            public static Point gridOffset;          //Determines top left part (starting pos) to display bitmap

//            //Panning
//            public static int panSpeed;     //Defined for 'Map Level'

//            //Zooming (camera style)
//            public static Point cameraInitCentre;     //Centre of viewport
//            public static decimal cameraCurrCentreX, cameraCurrCentreY;
//            public static decimal cameraWidth, cameraHeight;        //Distance (x or y) from camera to edge of viewport
//            public static int cameraMinWidth, cameraMinHeight, cameraMaxWidth, cameraMaxHeight;
//        }

//        public Form1()
//        {
//            InitializeComponent();

//            //INITIALISATION
//            MapCreateUserInput();

//            MakePbxGridOdd();

//            InitMaxVars();
//            ResetGridVars();
//            ResetCameraVars();

//            RegisterHandlers();

//            FillTileTypeDict();
//            CreateMap();
//            DrawGrid();
//        }

//        #region Initialisation
//        private void MapCreateUserInput()
//        {
//            //USER DEFINED VALUES
//            //      When creating new map
//            Global.mapNoOfTilesX = 20;      //int, >1
//            Global.mapNoOfTilesY = 20;      //int, >1

//            Global.zoomSens = 1.05M;        //dec, >1
//            Global.panSpeed = 5;            //int, >0
//        }

//        private void MakePbxGridOdd()
//        {
//            //Add some algorithm to determine pbxDimensions based on mapNoOfTiles and screen res.
//            pbxGrid.Width = 600;
//            pbxGrid.Height = 600;

//            if (pbxGrid.Width % 2 == 0)       //If even
//            {
//                pbxGrid.Width += 1;         //If width is even, then height is even as tileLength is equal in both dir. and initial noOfTiles is int
//                pbxGrid.Height += 1;
//            }
//        }

//        private void InitMaxVars()
//        {
//            Global.mapTileLength = (pbxGrid.Width - 1) / Global.mapNoOfTilesX;       //Must come after setting pbxGrid dimensions - What if not all fits? Resize necessary

//            //Note: '-1' excludes centre. Also, integer division
//            Global.cameraInitCentre.X = (pbxGrid.Width - 1) / 2;      //pbxGrid and Height should be odd for true centre
//            Global.cameraInitCentre.Y = (pbxGrid.Height - 1) / 2;     //Which may be perfect since we need an additional pixel column for final grid line

//            Global.cameraMaxWidth = Global.cameraInitCentre.X;
//            Global.cameraMaxHeight = Global.cameraInitCentre.Y;

//            decimal widthRatio = Global.cameraMaxHeight / Global.cameraMaxWidth;
//            decimal heightRatio = Global.cameraMaxWidth / Global.cameraMaxHeight;

//            //Determining min dimensions:
//            //At certain values on zooming in, zoom out is not possible due to flooring the result
//            //Therefore min dimensions is the furthest you can zoom in whilst being able to zoom out - Based on smaller dimension
//            //(zoomSens * minWidth) - minWidth = 1
//            //minWidth(zoomSens - 1) = 1
//            //minWidth = 1/(zoomSens - 1)
//            if (pbxGrid.Width >= pbxGrid.Height)
//            {
//                Global.cameraMinHeight = (int)Math.Ceiling(1 / (Global.zoomSens - 1));
//                Global.cameraMinWidth = (int)Math.Floor(heightRatio * Global.cameraMinHeight);
//            }
//            else
//            {
//                Global.cameraMinWidth = (int)Math.Ceiling(1 / (Global.zoomSens - 1));
//                Global.cameraMinHeight = (int)Math.Floor(widthRatio * Global.cameraMinWidth);
//            }
//        }

//        private void ResetGridVars()
//        {
//            Global.gridNoOfTilesX = Global.mapNoOfTilesX;
//            Global.gridNoOfTilesY = Global.mapNoOfTilesY;
//            Global.gridTileLength = Global.mapTileLength;
//            Global.gridOffset = new Point(0, 0);
//        }

//        private void ResetCameraVars()
//        {
//            Global.cameraCurrCentreX = Global.cameraInitCentre.X;
//            Global.cameraCurrCentreY = Global.cameraInitCentre.Y;
//            Global.cameraWidth = Global.cameraMaxWidth;
//            Global.cameraHeight = Global.cameraMaxHeight;
//        }

//        private void RegisterHandlers()
//        {
//            //Mouse
//            pbxGrid.MouseWheel += pbxGrid_MouseWheel;
//            pbxGrid.MouseEnter += pbxGrid_MouseEnter;
//            pbxGrid.MouseLeave += pbxGrid_MouseLeave;
//            pbxGrid.MouseClick += pbxGrid_MouseClick;

//            //Note: If mouse outside of pbx, draw non-highlighted bmp. Also, redraw mouse highlight when zooming or panning
//            pbxGrid.MouseMove += pbxGrid_MouseMove;

//            //Keyboard
//            this.KeyPreview = true;   //Receives key event before passing onto controls - Prevents having to add handlers to all controls
//            this.KeyDown += KeyPressed;

//            //Timer
//            TMR.Tick += TMR_Tick;
//            TMR.Enabled = true;
//        }

//        //Need this one to get MouseWheel to work
//        private void pbxGrid_MouseEnter(object sender, EventArgs e)
//        {
//            pbxGrid.Focus();
//        }
//        private void pbxGrid_MouseLeave(object sender, EventArgs e)
//        {
//            //Resets focus
//            pbxGrid.Enabled = false;
//            pbxGrid.Enabled = true;
//        }

//        private void FillTileTypeDict()
//        {
//            Global.dicTileType.Clear();

//            Global.dicTileType.Add("Empty", new Tuple<Color, int>(Global.colourDefault, 1));
//            Global.dicTileType.Add("Grass", new Tuple<Color, int>(Color.GreenYellow, 2));
//            Global.dicTileType.Add("Water", new Tuple<Color, int>(Color.CornflowerBlue, 4));
//            Global.dicTileType.Add("Idk", new Tuple<Color, int>(Color.MediumPurple, 10));
//        }

//        private void CreateMap()
//        {
//            //Fill one row at a time - Values are colours
//            for (int i = 0; i < Global.mapNoOfTilesY; i++)
//            {
//                List<Tile> MapColumn = new List<Tile>();     //New column list every time

//                //Fill current row with columns
//                for (int j = 0; j < Global.mapNoOfTilesX; j++)
//                {
//                    //Some random map
//                    if (i == 3)
//                    {
//                        MapColumn.Add(new Tile(j + ", " + i, "Grass"));
//                    }
//                    else if (j == 4)
//                    {
//                        MapColumn.Add(new Tile(j + ", " + i, "Water"));
//                    }
//                    else if (j == 9 || i == 5)
//                    {
//                        MapColumn.Add(new Tile(j + ", " + i, "Idk"));
//                    }
//                    else
//                    {
//                        MapColumn.Add(new Tile(j + ", " + i, "Empty"));
//                    }
//                }
//                Global.MapRow.Add(MapColumn);
//            }
//       }
//        #endregion

//        #region DrawGrid
//        private void DrawGrid()
//        {
//            //No of tiles to draw
//            decimal mapTopLeftX = Global.cameraCurrCentreX - Global.cameraWidth;
//            decimal mapTopLeftY = Global.cameraCurrCentreY - Global.cameraHeight;
//            decimal mapBtmRightX = Global.cameraCurrCentreX + Global.cameraWidth;
//            decimal mapBtmRightY = Global.cameraCurrCentreY + Global.cameraHeight;

//            //TopLeft tile - Offset will be applied to this tile
//            int tileTopLeftX = (int)Math.Floor(mapTopLeftX / Global.mapTileLength);
//            int tileTopLeftY = (int)Math.Floor(mapTopLeftY / Global.mapTileLength);

//            //BtmRight tile
//            int furthestColumn = (int)Math.Ceiling(mapBtmRightX / Global.mapTileLength);
//            int furthestRow = (int)Math.Ceiling(mapBtmRightY / Global.mapTileLength);
//            //Special case - When gridBtmRightXY falls on outer gridLine, it is part of a new tile, outside the map. This gets corrected
//            if (furthestColumn >= Global.mapNoOfTilesX) { furthestColumn = Global.mapNoOfTilesX - 1; }
//            if (furthestRow >= Global.mapNoOfTilesY) { furthestRow = Global.mapNoOfTilesY - 1; }

//            int tilesToDrawX = furthestColumn - tileTopLeftX + 1;          //+1 as furthest and nearest values are from 0, so we want to include 0
//            int tilesToDrawY = furthestRow - tileTopLeftY + 1;

//            //Bitmap file for grid - Persistent graphics
//            Global.bmpGridFull = new Bitmap(pbxGrid.Width + Global.gridOffset.X, pbxGrid.Height + Global.gridOffset.Y);
//            //^Parameter not valid issue
//            Graphics gfx = Graphics.FromImage(Global.bmpGridFull);   //Graphics object to fill bmpGrid

//            //Debug.WriteLine("mapTileLength: {0}", Global.mapTileLength);
//            //Debug.WriteLine("mapNoOfTiles: {0}, {1}", Global.mapNoOfTilesX, Global.mapNoOfTilesY);
//            //Debug.WriteLine("gridNoOfTiles: {0}, {1}", Global.gridNoOfTilesX, Global.gridNoOfTilesY);
//            //Debug.WriteLine("mapTopLeft: {0}, {1}", mapTopLeftX, mapTopLeftY);
//            //Debug.WriteLine("GridBtmRight: {0}, {1}", mapBtmRightX, mapBtmRightY);
//            //Debug.WriteLine("tileTopLeft: {0}, {1}", tileTopLeftX, tileTopLeftY);
//            //Debug.WriteLine("tileBtmRight: {0}, {1}", furthestColumn, furthestRow);
//            //Debug.WriteLine("Tiles to draw: {0}, {1}", tilesToDrawX, tilesToDrawY);
//            //Debug.WriteLine("Global.GridTileLength: {0}", Global.gridTileLength);
//            //Debug.WriteLine("gridTileLength * noOfTiles = {0}", Global.gridTileLength * tilesToDrawX);

//            //Overestimate TileLength
//            int gridTileLength = (int)Math.Ceiling(Global.gridTileLength);

//            //Drawing grid
//            for (int row = 0; row < tilesToDrawY; row++)
//            {
//                for (int column = 0; column < tilesToDrawX; column++)
//                {
//                    Rectangle rect = new Rectangle((int)Math.Ceiling(column * Global.gridTileLength), (int)Math.Ceiling(row * Global.gridTileLength), gridTileLength, gridTileLength);

//                    //Extract colour
//                    Tile tempTile = Global.MapRow[row + tileTopLeftY][column + tileTopLeftX];
//                    Color tileColour = tempTile.getColour();

//                    //Colour rectangle
//                    gfx.FillRectangle(new SolidBrush(tileColour), rect);

//                    //Grid Lines
//                    gfx.DrawRectangle(Pens.Black, rect);
//                }
//            }

//            Rectangle rectCrop = new Rectangle(Global.gridOffset.X, Global.gridOffset.Y, pbxGrid.Width, pbxGrid.Height);
//            pbxGrid.Image = (Bitmap)Global.bmpGridFull.Clone(rectCrop, Global.bmpGridFull.PixelFormat);
//        }
//        #endregion

//        #region Zooming
//        private void pbxGrid_MouseWheel(object sender, MouseEventArgs e)
//        {
//            string status = "";
//            if (e.Delta < 0) { status = "OUT"; }
//            else if (e.Delta > 0) { status = "IN"; }

//            //Camera Zooming
//            CameraZoom(status);
//            CameraReposition(e.X, e.Y);

//            //Update current NoOfTiles and TileLength
//            GetNewNoOfTiles(status);
//            GetNewTileLength();

//            UpdateOffsets();
//            DrawGrid();
//        }

//        private void CameraZoom(string status)
//        {
//            if (status == "IN")
//            {
//                Global.cameraWidth /= Global.zoomSens;
//                Global.cameraHeight /= Global.zoomSens;

//                //Min zoom
//                if (Global.cameraWidth < Global.cameraMinWidth || Global.cameraHeight < Global.cameraMinHeight)
//                {
//                    Global.cameraWidth = Global.cameraMinWidth;
//                    Global.cameraHeight = Global.cameraMinHeight;
//                }
//            }
//            else if (status == "OUT")
//            {
//                Global.cameraWidth *= Global.zoomSens;
//                Global.cameraHeight *= Global.zoomSens;

//                //Max zoom
//                if (Global.cameraWidth > Global.cameraMaxWidth || Global.cameraHeight > Global.cameraMaxHeight)
//                {
//                    Global.cameraWidth = Global.cameraMaxWidth;
//                    Global.cameraHeight = Global.cameraMaxHeight;
//                }
//            }
//        }

//        private void CameraReposition(decimal cursorX, decimal cursorY)
//        {
//            //Converts from camera position (pbxGrid) to map position
//            decimal widthRatio = cursorX / pbxGrid.Width;
//            decimal heightRatio = cursorY / pbxGrid.Height;
//            decimal cameraViewportWidth = 2 * Global.cameraWidth + 1;
//            decimal cameraViewportHeight = 2 * Global.cameraHeight + 1;
//            decimal cameraViewportTopLeftX = Global.cameraCurrCentreX - Global.cameraWidth;
//            decimal cameraViewportTopLeftY = Global.cameraCurrCentreY - Global.cameraHeight;

//            decimal mapCursX = (widthRatio * cameraViewportWidth) + cameraViewportTopLeftX;
//            decimal mapCursY = (heightRatio * cameraViewportHeight) + cameraViewportTopLeftY;

//            //Smoothen movement later on - Requires decimal type to work with
//            if (mapCursX > pbxGrid.Width - Global.cameraWidth)
//            {
//                Global.cameraCurrCentreX = pbxGrid.Width - Global.cameraWidth;
//            }
//            else if (mapCursX < Global.cameraWidth)
//            {
//                Global.cameraCurrCentreX = Global.cameraWidth;
//            }
//            else
//            {
//                Global.cameraCurrCentreX = mapCursX;
//            }

//            if (mapCursY > pbxGrid.Height - Global.cameraHeight)
//            {
//                Global.cameraCurrCentreY = pbxGrid.Height - Global.cameraWidth;
//            }
//            else if (mapCursY < Global.cameraHeight)
//            {
//                Global.cameraCurrCentreY = Global.cameraHeight;
//            }
//            else
//            {
//                Global.cameraCurrCentreY = mapCursY;
//            }
//        }

//        private void GetNewNoOfTiles(string status)
//        {
//            decimal widthRatio = Global.cameraWidth / Global.cameraMaxWidth;
//            decimal heightRatio = Global.cameraHeight / Global.cameraMaxHeight;

//            Global.gridNoOfTilesX = Global.mapNoOfTilesX * widthRatio;
//            Global.gridNoOfTilesY = Global.mapNoOfTilesY * heightRatio;
//        }

//        private void GetNewTileLength()
//        {
//            Global.gridTileLength = (pbxGrid.Width - 1) / Global.gridNoOfTilesX;
//        }
//        #endregion

//        #region Panning
//        private void KeyPressed(object sender, KeyEventArgs e)
//        {
//            //NOTE: Must be scaled according to zoom level - More zoomed in, slower panning speed as original pan speed increases topLeft by more pixels
//            //Want speed to remain constant so tileLength / panSpeed has to be constant (panSpeed defined for initialTileLength)
//            //new panSpeed = scaleFactor x Global.panSpeed = (gridTileLength / mapTileLength) x panSpeed
//            decimal scaleFactor = Global.gridTileLength / Global.mapTileLength;
//            decimal centreChange = scaleFactor * Global.panSpeed;
//            Keys keyPressed = e.KeyCode;

//            //Only accessible if WASD pressed
//            if (keyPressed == Keys.W || keyPressed == Keys.D || keyPressed == Keys.S || keyPressed == Keys.A)
//            {
//                //Panning - Note annoying delay and no multi-key support
//                if (keyPressed == Keys.W)        //UP
//                {
//                    if (Global.cameraCurrCentreY - centreChange > Global.cameraHeight)
//                    {
//                        Global.cameraCurrCentreY -= centreChange;
//                    }
//                    else
//                    {
//                        Global.cameraCurrCentreY = Global.cameraHeight;
//                    }
//                }

//                else if (keyPressed == Keys.D)       //RIGHT
//                {
//                    if (Global.cameraCurrCentreX + centreChange < pbxGrid.Width - Global.cameraWidth)
//                    {
//                        Global.cameraCurrCentreX += centreChange;
//                    }
//                    else
//                    {
//                        Global.cameraCurrCentreX = pbxGrid.Width - Global.cameraWidth;
//                    }
//                }

//                else if (keyPressed == Keys.S)       //DOWN
//                {
//                    if (Global.cameraCurrCentreY + centreChange < pbxGrid.Height - Global.cameraHeight)
//                    {
//                        Global.cameraCurrCentreY += centreChange;
//                    }
//                    else
//                    {
//                        Global.cameraCurrCentreY = pbxGrid.Height - Global.cameraHeight;
//                    }
//                }

//                else if (keyPressed == Keys.A)       //LEFT
//                {
//                    if (Global.cameraCurrCentreX - centreChange > Global.cameraWidth)
//                    {
//                        Global.cameraCurrCentreX -= centreChange;
//                    }
//                    else
//                    {
//                        Global.cameraCurrCentreX = Global.cameraWidth;
//                    }
//                }

//                UpdateOffsets();
//                DrawGrid();
//            }
//        }
//        #endregion

//        private void UpdateOffsets()
//        {
//            //Convert tile at map level to tile at grid level
//            decimal tileTopLeftX = (Global.cameraCurrCentreX - Global.cameraWidth) / Global.mapTileLength;
//            decimal tileTopLeftY = (Global.cameraCurrCentreY - Global.cameraHeight) / Global.mapTileLength;

//            Global.gridOffset.X = (int)Math.Floor((tileTopLeftX * Global.gridTileLength) % Global.gridTileLength);
//            Global.gridOffset.Y = (int)Math.Floor((tileTopLeftY * Global.gridTileLength) % Global.gridTileLength);

//            Debug.WriteLine("");
//            Debug.WriteLine("Offset: {0}, {1}", Global.gridOffset.X, Global.gridOffset.Y);
//            Debug.WriteLine("gridTileLength: {0}", Global.gridTileLength);
//        }

//        private void TMR_Tick(object sender, EventArgs e)
//        {
//            //MouseHoverHighlight();
//        }

//        private void MouseHoverHighlight()
//        {
//            Point cursPos = pbxGrid.PointToClient(Cursor.Position);

//            //Display unmouse-hovered bitmap
//            Rectangle rectCrop = new Rectangle(Global.gridOffset.X, Global.gridOffset.Y, pbxGrid.Width, pbxGrid.Height);
//            pbxGrid.Image = (Bitmap)Global.bmpGridFull.Clone(rectCrop, Global.bmpGridFull.PixelFormat);

//            //If mouse within pbxGrid
//            if ((cursPos.X >= 0 && cursPos.X <= pbxGrid.Width) &&
//                (cursPos.Y >= 0 && cursPos.Y <= pbxGrid.Height))
//            {
//                decimal mapTopLeftX = Global.cameraCurrCentreX - Global.cameraWidth;
//                decimal mapTopLeftY = Global.cameraCurrCentreY - Global.cameraHeight;
//                int tileTopLeftX = (int)Math.Floor(mapTopLeftX / Global.mapTileLength);
//                int tileTopLeftY = (int)Math.Floor(mapTopLeftY / Global.mapTileLength);

//                int gridTileX = (int)Math.Floor((decimal)(cursPos.X + Global.gridOffset.X) / Global.gridTileLength);
//                int gridTileY = (int)Math.Floor((decimal)(cursPos.Y + Global.gridOffset.Y) / Global.gridTileLength);

//                int mapTileX = gridTileX + tileTopLeftX;
//                int mapTileY = gridTileY + tileTopLeftY;

//                //If not hovering beyond tile map, overwrite unhovered image
//                if (mapTileX != Global.mapNoOfTilesX && mapTileY != Global.mapNoOfTilesY)
//                {
//                    int tileX = (int)Math.Floor((decimal)(cursPos.X + Global.gridOffset.X) / Global.gridTileLength);
//                    int tileY = (int)Math.Floor((decimal)(cursPos.Y + Global.gridOffset.Y) / Global.gridTileLength);
//                    int gridTileLength = (int)Math.Ceiling(Global.gridTileLength);

//                    //Use temp bitmap (DOES NOT EDIT bmpGridFull)
//                    Bitmap bmpGridHover = new Bitmap(pbxGrid.Width + Global.gridOffset.X, pbxGrid.Height + Global.gridOffset.Y);
//                    //^Parameter invalid error by pressing WASD simultaneously
//                    bmpGridHover = (Bitmap)Global.bmpGridFull.Clone();
//                    Graphics gfx = Graphics.FromImage(bmpGridHover);

//                    //Highlights tile that cursor is over
//                    Rectangle rect = new Rectangle((int)Math.Ceiling(tileX * Global.gridTileLength), (int)Math.Ceiling(tileY * Global.gridTileLength), gridTileLength, gridTileLength);
//                    gfx.DrawRectangle(Pens.Red, rect);

//                    //Cropping
//                    rectCrop = new Rectangle(Global.gridOffset.X, Global.gridOffset.Y, pbxGrid.Width, pbxGrid.Height);
//                    pbxGrid.Image = (Bitmap)bmpGridHover.Clone(rectCrop, bmpGridHover.PixelFormat);      //Out of memory can occur here
//                }
//            }
//        }

//        private void pbxGrid_MouseClick(object sender, MouseEventArgs e)
//        {
//            //M1 Click
//            if (e.Button == MouseButtons.Left)
//            {
//                ObtainTileInfo(e.X, e.Y);
//            }
//        }

//        private void ObtainTileInfo(int cursPosX, int cursPosY)
//        {
//            //Obtains tile coordinates and colour
//            decimal mapTopLeftX = Global.cameraCurrCentreX - Global.cameraWidth;
//            decimal mapTopLeftY = Global.cameraCurrCentreY S- Global.cameraHeight;
//            int tileTopLeftX = (int)Math.Floor(mapTopLeftX / Global.mapTileLength);
//            int tileTopLeftY = (int)Math.Floor(mapTopLeftY / Global.mapTileLength);

//            int gridTileX = (int)Math.Floor((decimal)(cursPosX + Global.gridOffset.X) / Global.gridTileLength);
//            int gridTileY = (int)Math.Floor((decimal)(cursPosY + Global.gridOffset.Y) / Global.gridTileLength);

//            int mapTileX = gridTileX + tileTopLeftX;
//            int mapTileY = gridTileY + tileTopLeftY;

//            //If some guy (Yuki) decides to click on the last grid , do not use
//            if (mapTileX != Global.mapNoOfTilesX && mapTileY != Global.mapNoOfTilesY)
//            {
//                //Extract colour
//                Tile tempTile = Global.MapRow[mapTileY][mapTileX];
//                Color tileColour = tempTile.getColour();

//                MessageBox.Show(string.Format("Tile No: {0}, {1} | {2}", mapTileX, mapTileY, tileColour.ToString()));
//            }
//        }

//        private void pbxGrid_MouseMove(object sender, MouseEventArgs e)
//        {
//            MouseHoverHighlight();
//        }
//    }
//}