/*
 * Simple Metric/Imperial Speedometer
 * Author: libertylocked
 * Version: 2.0.4a
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using GTA;
using GTA.Math;
using GTA.Native;

namespace GTAVMod_Speedometer
{
    public class Metric_Speedometer : Script
    {
        // Constants
        const string SCRIPT_VERSION = "2.0.4a";
        const string URL_VERSIONFILE = @"https://raw.githubusercontent.com/LibertyLocked/GTAVMods/release/GTAVMod_Speedometer/version.txt"; // it's just a txt, chill out people
        const int NUM_FONTS = 8;

        #region Fields
        bool creditsShown = false;
        UIContainer speedContainer, odometerContainer;
        UIText speedText, odometerText;
        SpeedoMode speedoMode;
        float distanceKm = 0;

        ScriptSettings settings;
        bool enableMenu;
        Keys menuKey;
        bool enableSaving;
        bool useMph;

        // Fields for menus
        MySettingsMenu mainMenu;
        GTA.Menu coreMenu, dispMenu, colorMenu;
        GTA.MenuItem[] mainMenuItems, coreMenuItems, dispMenuItems, colorMenuItems;
        bool isChangingBackcolor;

        // Fields for UI settings
        VerticalAlignment vAlign;
        HorizontalAlign hAlign;
        Point posOffset;
        int pWidth, pHeight;
        float fontSize;
        int fontStyle;
        Color backcolor, forecolor;

        #endregion

        public Metric_Speedometer()
        {
            ParseSettings();
            SetupUIElements();
            SetupMenus();

            UpKey = Keys.NumPad8;
            DownKey = Keys.NumPad2;
            LeftKey = Keys.NumPad4;
            RightKey = Keys.NumPad6;
            ActivateKey = Keys.NumPad5;
            BackKey = Keys.NumPad0;

            this.View.MenuTransitions = false; // because transition looksnice/doesnotlooknice
            this.Tick += OnTick;
            this.KeyDown += OnKeyDown;
        }

        #region Event handles

        void OnTick(object sender, EventArgs e)
        {
            if (enableSaving)
            {
                bool isPausePressed = Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 2, 199) ||
                    Function.Call<bool>(Hash.IS_DISABLED_CONTROL_JUST_PRESSED, 2, 200); // pause or pause alternate button
                if (isPausePressed) SaveStats();
            }

            Player player = Game.Player;
            if (player != null && player.CanControlCharacter && player.IsAlive && player.Character != null)
            {
                if (player.Character.IsInVehicle())
                {
                    Update(player.Character.CurrentVehicle.Speed);
                    Draw();
                }
                else if (IsPlayerRidingDeer(player.Character))
                {
                    Update(GetEntitySpeed(player.Character));
                    Draw();
                }
            }
        }

        void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (enableMenu && e.KeyCode == menuKey)
            {
                this.View.CloseAllMenus();
                this.View.AddMenu(mainMenu);
                if (!creditsShown)
                {
                    ShowCredits();
                    creditsShown = true;
                }
            }
        }

        #endregion

        #region Private methods

        void Update(float speedThisFrame)
        {
            float speedKph = speedThisFrame * 3600 / 1000; // convert from m/s to km/h
            float distanceLastFrame = speedThisFrame * Game.LastFrameTime / 1000; // increment odometer counter
            distanceKm += distanceLastFrame;

            if (useMph)
            {
                float speedMph = KmToMiles(speedKph);
                float distanceMiles = KmToMiles(distanceKm);
                speedText.Caption = Math.Floor(speedMph).ToString("0") + " mph"; // floor speed mph
                if (speedoMode == SpeedoMode.Detailed)
                {
                    double truncated = Math.Floor(distanceMiles * 10) / 10.0;
                    odometerText.Caption = truncated.ToString("0.0") + " mi";
                }
            }
            else
            {
                speedText.Caption = Math.Floor(speedKph).ToString("0") + " km/h"; // floor speed km/h
                if (speedoMode == SpeedoMode.Detailed)
                {
                    double truncated = Math.Floor(distanceKm * 10) / 10.0;
                    odometerText.Caption = truncated.ToString("0.0") + " km";
                }
            }
        }

        void Draw()
        {
            if (speedoMode != SpeedoMode.Off) speedContainer.Draw();
            if (speedoMode == SpeedoMode.Detailed) // draw these widgets in detailed mode only
                odometerContainer.Draw();
        }

        void ParseSettings()
        {
            try
            {
                settings = ScriptSettings.Load(@".\scripts\Metric_Speedometer.ini");

                // Parse Core settings
                this.useMph = settings.GetValue("Core", "UseMph", false);
                this.speedoMode = (SpeedoMode)settings.GetValue<int>("Core", "DisplayMode", 1);
                this.enableMenu = settings.GetValue("Core", "EnableMenu", true);
                if (enableMenu)
                    this.menuKey = (Keys)Enum.Parse(typeof(Keys), settings.GetValue("Core", "MenuKey"), true);
                this.enableSaving = settings.GetValue("Core", "EnableSaving", true);

                // Parse UI settings
                this.vAlign = (VerticalAlignment)Enum.Parse(typeof(VerticalAlignment), settings.GetValue("UI", "VertAlign"), true);
                this.hAlign = (HorizontalAlign)Enum.Parse(typeof(HorizontalAlign), settings.GetValue("UI", "HorzAlign"), true);
                this.posOffset = new Point(settings.GetValue<int>("UI", "OffsetX", 0), settings.GetValue<int>("UI", "OffsetY", 0));
                this.pWidth = settings.GetValue("UI", "PanelWidth", 66);
                this.pHeight = settings.GetValue("UI", "PanelHeight", 24);
                this.fontSize = float.Parse(settings.GetValue("UI", "FontSize"), CultureInfo.InvariantCulture.NumberFormat);
                this.fontStyle = settings.GetValue("UI", "FontStyle", 4);
                this.backcolor = Color.FromArgb(settings.GetValue<int>("UI", "BackcolorA", 200), settings.GetValue<int>("UI", "BackcolorR", 237),
                    settings.GetValue<int>("UI", "BackcolorG", 239), settings.GetValue<int>("UI", "BackcolorB", 241));
                this.forecolor = Color.FromArgb(settings.GetValue<int>("UI", "ForecolorA", 255), settings.GetValue<int>("UI", "ForecolorR", 0),
                    settings.GetValue<int>("UI", "ForecolorG", 0), settings.GetValue<int>("UI", "ForecolorB", 0));

                // Load stats
                if (enableSaving) LoadStats();
            }
            catch { UI.Notify("~r~failed to load speedometer config"); }
        }

        void SetupUIElements()
        {
            Point pos = new Point(posOffset.X, posOffset.Y);
            Point odometerPos = new Point(0, 0);
            switch (vAlign)
            {
                case VerticalAlignment.Top:
                    pos.Y += 0;
                    odometerPos.Y += pHeight; // below speed counter
                    break;
                case VerticalAlignment.Center:
                    pos.Y += UI.HEIGHT / 2 - pHeight / 2;
                    odometerPos.Y += pHeight; // below speed counter
                    break;
                case VerticalAlignment.Bottom:
                    pos.Y += UI.HEIGHT - pHeight;
                    odometerPos.Y -= pHeight; // above speed counter
                    break;
            }
            switch (hAlign)
            {
                case HorizontalAlign.Left:
                    pos.X += 0;
                    break;
                case HorizontalAlign.Center:
                    pos.X += UI.WIDTH / 2 - pWidth / 2;
                    break;
                case HorizontalAlign.Right:
                    pos.X += UI.WIDTH - pWidth;
                    break;
            }
            odometerPos.X += pos.X;
            odometerPos.Y += pos.Y;

            this.speedContainer = new UIContainer(pos, new Size(pWidth, pHeight), backcolor);
            this.speedText = new UIText(String.Empty, new Point(pWidth / 2, 0), fontSize, forecolor, fontStyle, true);
            this.speedContainer.Items.Add(speedText);
            this.odometerContainer = new UIContainer(odometerPos, new Size(pWidth, pHeight), backcolor);
            this.odometerText = new UIText(String.Empty, new Point(pWidth / 2, 0), fontSize, forecolor, fontStyle, true);
            this.odometerContainer.Items.Add(odometerText);
        }

        void ResetUIToDefault()
        {
            vAlign = VerticalAlignment.Bottom;
            hAlign = HorizontalAlign.Center;
            posOffset = new Point(0, 0);
            pWidth = 66;
            pHeight = 24;
            fontSize = 0.5f;
            fontStyle = 4;
            backcolor = Color.FromArgb(150, 237, 239, 241);
            forecolor = Color.FromArgb(255, 0, 0, 0);
            SetupUIElements();
        }

        void SetupMenus()
        {
            // Create main menu
            MenuButton btnToggle = new MenuButton("", delegate { speedoMode = (SpeedoMode)(((int)speedoMode + 1) % Enum.GetNames(typeof(SpeedoMode)).Length); UpdateMainButtons(0); });
            MenuButton btnClear = new MenuButton("Reset Odometer", delegate { distanceKm = 0; UI.Notify("Odometer reset"); });
            MenuButton btnCore = new MenuButton("Core Settings >", delegate { View.AddMenu(coreMenu); });
            MenuButton btnDisp = new MenuButton("Display Settings >", delegate { View.AddMenu(dispMenu); });
            MenuButton btnExtra = new MenuButton("Extras >", delegate
                {
                    MenuButton btnShowCredits = new MenuButton("Show Credits", ShowCredits);
                    MenuButton btnUpdates = new MenuButton("Check for Updates", CheckForUpdates);
                    GTA.Menu extraMenu = new GTA.Menu("Extras", new GTA.MenuItem[] { btnShowCredits, btnUpdates });
                    extraMenu.HasFooter = false;
                    View.AddMenu(extraMenu);
                });
            MenuButton btnReload = new MenuButton("Reload", delegate { ParseSettings(); SetupUIElements(); UI.Notify("Speedometer reloaded"); 
                UpdateMainButtons(5); UpdateCoreButtons(0); UpdateDispButtons(0); UpdateColorButtons(0); });
            MenuButton btnBack = new MenuButton("Save & Exit", delegate { View.CloseAllMenus(); });
            mainMenuItems = new GTA.MenuItem[] { btnToggle, btnClear, btnCore, btnDisp, btnExtra, btnReload, btnBack };
            this.mainMenu = new MySettingsMenu("Speedometer v" + SCRIPT_VERSION, mainMenuItems, this);
            this.mainMenu.HasFooter = false;

            // Create core menu
            MenuButton btnUseMph = new MenuButton("", delegate { useMph = !useMph; UpdateCoreButtons(0); });
            MenuButton btnEnableSaving = new MenuButton("", delegate { enableSaving = !enableSaving; UpdateCoreButtons(1); });
            //MenuButton btnEnableMenu = new MenuButton("Disable Menu Key", delegate { enableMenu = !enableMenu; UpdateCoreButtons(2); });
            coreMenuItems = new GTA.MenuItem[] { btnUseMph, btnEnableSaving };
            this.coreMenu = new GTA.Menu("Core Settings", coreMenuItems);
            this.coreMenu.HasFooter = false;

            // Create display menu
            MenuButton btnVAlign = new MenuButton("", delegate { vAlign = (VerticalAlignment)(((int)vAlign + 1) % 3); posOffset.Y = 0; SetupUIElements(); UpdateDispButtons(0); });
            MenuButton btnHAlign = new MenuButton("", delegate { hAlign = (HorizontalAlign)(((int)hAlign + 1) % 3); posOffset.X = 0; SetupUIElements(); UpdateDispButtons(1); });
            MenuButton btnFontStyle = new MenuButton("", delegate { fontStyle = ++fontStyle % NUM_FONTS; SetupUIElements(); UpdateDispButtons(2); });
            MenuButton btnFontSize = new MenuButton("Font Size >", delegate
                {
                    MenuButton btnAddSize = new MenuButton("+ Font Size", delegate { fontSize += 0.02f; SetupUIElements(); });
                    MenuButton btnSubSize = new MenuButton("- Font Size", delegate { fontSize -= 0.02f; SetupUIElements(); });
                    GTA.Menu sizeMenu = new GTA.Menu("Font Size", new GTA.MenuItem[] { btnAddSize, btnSubSize });
                    sizeMenu.HasFooter = false;
                    View.AddMenu(sizeMenu);
                });
            MenuButton btnPanelSize = new MenuButton("Panel Size >", delegate
                {
                    MenuButton btnAddWidth = new MenuButton("+ Panel Width", delegate { pWidth += 2; SetupUIElements(); });
                    MenuButton btnSubWidth = new MenuButton("- Panel Width", delegate { pWidth -= 2; SetupUIElements(); });
                    MenuButton btnAddHeight = new MenuButton("+ Panel Height", delegate { pHeight += 2; SetupUIElements(); });
                    MenuButton btnSubHeight = new MenuButton("- Panel Height", delegate { pHeight -= 2; SetupUIElements(); });
                    GTA.Menu panelSizeMenu = new GTA.Menu("Panel Size", new GTA.MenuItem[] { btnAddWidth, btnSubWidth, btnAddHeight, btnSubHeight });
                    panelSizeMenu.HasFooter = false;
                    View.AddMenu(panelSizeMenu);
                });
            MenuButton btnAplyOffset = new MenuButton("Set Offset >", delegate
                {
                    MenuButton btnOffsetUp = new MenuButton("Move Up", delegate { posOffset.Y += -2; SetupUIElements(); });
                    MenuButton btnOffsetDown = new MenuButton("Move Down", delegate { posOffset.Y += 2; SetupUIElements(); });
                    MenuButton btnOffsetLeft = new MenuButton("Move Left", delegate { posOffset.X += -2; SetupUIElements(); });
                    MenuButton btnOffsetRight = new MenuButton("Move Right", delegate { posOffset.X += 2; SetupUIElements(); });
                    MenuButton btnOffsetClr = new MenuButton("Clear Offset", delegate { posOffset.X = 0; posOffset.Y = 0; SetupUIElements(); });
                    GTA.Menu offsetMenu = new GTA.Menu("Set Offset", new GTA.MenuItem[] { btnOffsetUp, btnOffsetDown, btnOffsetLeft, btnOffsetRight, btnOffsetClr });
                    offsetMenu.HasFooter = false;
                    View.AddMenu(offsetMenu);
                });
            MenuButton btnBackcolor = new MenuButton("Back Color >", delegate { isChangingBackcolor = true; UpdateColorButtons(0); View.AddMenu(colorMenu); });
            MenuButton btnForecolor = new MenuButton("Fore Color >", delegate { isChangingBackcolor = false; UpdateColorButtons(0); View.AddMenu(colorMenu); });
            MenuButton btnRstDefault = new MenuButton("Restore to Default", delegate { ResetUIToDefault(); UpdateDispButtons(8); });
            dispMenuItems = new GTA.MenuItem[] { btnVAlign, btnHAlign, btnFontStyle, btnAplyOffset, btnFontSize, btnPanelSize, btnBackcolor, btnForecolor, btnRstDefault };
            this.dispMenu = new GTA.Menu("Display Settings", dispMenuItems);
            this.dispMenu.HasFooter = false;

            // Create color menu
            MenuButton btnAddR = new MenuButton("+ R", delegate 
                {
                    if (isChangingBackcolor) backcolor = IncrementARGB(backcolor, 0, 5, 0, 0);
                    else forecolor = IncrementARGB(forecolor, 0, 5, 0, 0);
                    SetupUIElements(); UpdateColorButtons(0);
                });
            MenuButton btnSubR = new MenuButton("- R", delegate
                {
                    if (isChangingBackcolor) backcolor = IncrementARGB(backcolor, 0, -5, 0, 0);
                    else forecolor = IncrementARGB(forecolor, 0, -5, 0, 0);
                    SetupUIElements(); UpdateColorButtons(1);
                });
            MenuButton btnAddG = new MenuButton("+ G", delegate
                {
                    if (isChangingBackcolor) backcolor = IncrementARGB(backcolor, 0, 0, 5, 0);
                    else forecolor = IncrementARGB(forecolor, 0, 0, 5, 0);
                    SetupUIElements(); UpdateColorButtons(2);
                });
            MenuButton btnSubG = new MenuButton("- G", delegate
                {
                    if (isChangingBackcolor) backcolor = IncrementARGB(backcolor, 0, 0, -5, 0);
                    else forecolor = IncrementARGB(forecolor, 0, 0, -5, 0);
                    SetupUIElements(); UpdateColorButtons(3);
                });
            MenuButton btnAddB = new MenuButton("+ B", delegate
                {
                    if (isChangingBackcolor) backcolor = IncrementARGB(backcolor, 0, 0, 0, 5);
                    else forecolor = IncrementARGB(forecolor, 0, 0, 0, 5);
                    SetupUIElements(); UpdateColorButtons(4);
                });
            MenuButton btnSubB = new MenuButton("- B", delegate
                {
                    if (isChangingBackcolor) backcolor = IncrementARGB(backcolor, 0, 0, 0, -5);
                    else forecolor = IncrementARGB(forecolor, 0, 0, 0, -5);
                    SetupUIElements(); UpdateColorButtons(5);
                });
            MenuButton btnAddA = new MenuButton("+ Opacity", delegate
                {
                    if (isChangingBackcolor) backcolor = IncrementARGB(backcolor, 5, 0, 0, 0);
                    else forecolor = IncrementARGB(forecolor, 5, 0, 0, 0);
                    SetupUIElements(); UpdateColorButtons(6);
                });
            MenuButton btnSubA = new MenuButton("- Opacity", delegate
                {
                    if (isChangingBackcolor) backcolor = IncrementARGB(backcolor, -5, 0, 0, 0);
                    else forecolor = IncrementARGB(forecolor, -5, 0, 0, 0);
                    SetupUIElements(); UpdateColorButtons(7);
                });
            colorMenuItems = new GTA.MenuItem[] { btnAddR, btnSubR, btnAddG, btnSubG, btnAddB, btnSubB, btnAddA, btnSubA };
            this.colorMenu = new GTA.Menu("", colorMenuItems);
            this.colorMenu.HasFooter = false;
            this.colorMenu.HeaderHeight += 20;

            UpdateMainButtons(0);
            UpdateCoreButtons(0);
            UpdateDispButtons(0);
            UpdateColorButtons(0);
        }

        void UpdateMainButtons(int selectedIndex)
        {
            mainMenuItems[0].Caption = "Toggle Display: " + speedoMode.ToString(); // toggle button's caption
            mainMenu.Initialize(); // reinit main menu
            for (int i = 0; i < selectedIndex; i++)
                mainMenu.OnChangeSelection(true);
        }

        void UpdateCoreButtons(int selectedIndex)
        {
            coreMenuItems[0].Caption = "Speed Unit: " + (useMph ? "Imperial" : "Metric");
            coreMenuItems[1].Caption = "Save Odometer: " + (enableSaving ? "On" : "Off");
            //coreMenuItems[2].Caption = "Enable Menu Key: " + enableMenu;
            coreMenu.Initialize(); // reinit core menu
            for (int i = 0; i < selectedIndex; i++)
                coreMenu.OnChangeSelection(true);
        }

        void UpdateDispButtons(int selectedIndex)
        {
            dispMenuItems[0].Caption = "Vertical: " + System.Enum.GetName(typeof(VerticalAlignment), vAlign);
            dispMenuItems[1].Caption = "Horizontal: " + System.Enum.GetName(typeof(HorizontalAlign), hAlign);
            dispMenuItems[2].Caption = "Font Style: " + fontStyle;
            dispMenu.Initialize(); // reinit disp menu
            for (int i = 0; i < selectedIndex; i++)
                dispMenu.OnChangeSelection(true);
        }

        void UpdateColorButtons(int selectedIndex)
        {
            Color color = isChangingBackcolor ? backcolor : forecolor;
            colorMenu.Caption = (isChangingBackcolor ? "Back Color" : "Fore Color") 
                + "\nR: " + color.R + " G: " + color.G + " B: " + color.B + " A: " + color.A;
            colorMenu.Initialize(); // reinit color menu
            for (int i = 0; i < selectedIndex; i++)
                colorMenu.OnChangeSelection(true);
        }

        void LoadStats()
        {
            try
            {
                using (StreamReader sr = new StreamReader(@".\scripts\Metric_Speedometer_Stats.txt"))
                {
                    bool distanceParsed = float.TryParse(sr.ReadLine(), out distanceKm);
                }
            }
            catch { }
        }

        void SaveStats()
        {
            try
            {
                Thread thread = new Thread(ThreadProc_DoSaveStats);
                thread.Start();
            }
            catch { }
        }

        void ThreadProc_DoSaveStats()
        {
            using (StreamWriter sw = new StreamWriter((@".\scripts\Metric_Speedometer_Stats.txt"), false))
            {
                sw.WriteLine(distanceKm);
            }
        }

        float GetEntitySpeed(Entity entity)
        {
            try
            {
                float speed = Function.Call<float>(Hash.GET_ENTITY_SPEED, entity);
                return speed;
            }
            catch
            {
                return 0;
            }
        }

        bool IsPlayerRidingDeer(Ped playerPed)
        {
            try
            {
                Ped attached = Function.Call<Ped>(Hash.GET_ENTITY_ATTACHED_TO, playerPed);
                if (attached != null)
                {
                    PedHash attachedHash = (PedHash)attached.Model.Hash;
                    return (attachedHash == PedHash.Deer);
                }
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }

        float KmToMiles(float km)
        {
            return km * 0.6213711916666667f;
        }

        Color IncrementARGB(Color color, int da, int dr, int dg, int db)
        {
            return Color.FromArgb(Math.Max(Math.Min(color.A + da, 255), 0), Math.Max(Math.Min(color.R + dr, 255), 0),
                Math.Max(Math.Min(color.G + dg, 255), 0), Math.Max(Math.Min(color.B + db, 255), 0));
        }

        void ShowCredits()
        {
            UI.Notify("Speedometer ~r~v" + SCRIPT_VERSION + " ~s~by ~b~libertylocked");
        }

        void CheckForUpdates()
        {
            try
            {
                WebClient client = new WebClient();
                client.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                Stream stream = client.OpenRead(URL_VERSIONFILE);
                StreamReader reader = new StreamReader(stream);
                string latestVer = reader.ReadToEnd();
                if (SCRIPT_VERSION == latestVer) UI.Notify("~g~Speedometer is up to date");
                else UI.Notify("~b~New version is available on gta5-mods.com");
            }
            catch { UI.Notify("~r~Failed to check for updates!"); }
        }

        #endregion

        public void SaveSettings()
        {
            try
            {
                INIFile settings = new INIFile(@".\scripts\Metric_Speedometer.ini", true, true);
                settings.SetValue("Core", "UseMph", useMph.ToString());
                settings.SetValue("Core", "DisplayMode", (int)speedoMode);
                settings.SetValue("Core", "EnableSaving", enableSaving.ToString());
                settings.SetValue("UI", "VertAlign", Enum.GetName(typeof(VerticalAlignment), vAlign));
                settings.SetValue("UI", "HorzAlign", Enum.GetName(typeof(HorizontalAlign), hAlign));
                settings.SetValue("UI", "OffsetX", posOffset.X);
                settings.SetValue("UI", "OffsetY", posOffset.Y);
                settings.SetValue("UI", "PanelWidth", pWidth);
                settings.SetValue("UI", "PanelHeight", pHeight);
                settings.SetValue("UI", "FontSize", fontSize.ToString(CultureInfo.InvariantCulture));
                settings.SetValue("UI", "FontStyle", fontStyle.ToString());
                settings.SetValue("UI", "BackcolorR", backcolor.R);
                settings.SetValue("UI", "BackcolorG", backcolor.G);
                settings.SetValue("UI", "BackcolorB", backcolor.B);
                settings.SetValue("UI", "BackcolorA", backcolor.A);
                settings.SetValue("UI", "ForecolorR", forecolor.R);
                settings.SetValue("UI", "ForecolorG", forecolor.G);
                settings.SetValue("UI", "ForecolorB", forecolor.B);
                settings.SetValue("UI", "ForecolorA", forecolor.A);

                UI.Notify("Speedometer config saved");
            }
            catch { UI.Notify("~r~failed to save speedometer config"); }
        }
    }

    class MySettingsMenu : GTA.Menu
    {
        Metric_Speedometer script;

        public MySettingsMenu(string caption, GTA.MenuItem[] items, Metric_Speedometer script)
            : base(caption, items)
        {
            this.script = script;
        }

        public override void OnClose()
        {
            script.SaveSettings();
            base.OnClose();
        }
    }

    enum SpeedoMode
    {
        Off = 0,
        Simple = 1,
        Detailed = 2,
    }

    #region INI File class

    internal class INIFile
    {

        #region "Declarations"

        // *** Lock for thread-safe access to file and local cache ***
        private object m_Lock = new object();

        // *** File name ***
        private string m_FileName = null;
        internal string FileName
        {
            get
            {
                return m_FileName;
            }
        }

        // *** Lazy loading flag ***
        private bool m_Lazy = false;

        // *** Automatic flushing flag ***
        private bool m_AutoFlush = false;

        // *** Local cache ***
        private Dictionary<string, Dictionary<string, string>> m_Sections = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, Dictionary<string, string>> m_Modified = new Dictionary<string, Dictionary<string, string>>();

        // *** Local cache modified flag ***
        private bool m_CacheModified = false;

        #endregion

        #region "Methods"

        // *** Constructor ***
        public INIFile(string FileName)
        {
            Initialize(FileName, false, false);
        }

        public INIFile(string FileName, bool Lazy, bool AutoFlush)
        {
            Initialize(FileName, Lazy, AutoFlush);
        }

        // *** Initialization ***
        private void Initialize(string FileName, bool Lazy, bool AutoFlush)
        {
            m_FileName = FileName;
            m_Lazy = Lazy;
            m_AutoFlush = AutoFlush;
            if (!m_Lazy) Refresh();
        }

        // *** Parse section name ***
        private string ParseSectionName(string Line)
        {
            if (!Line.StartsWith("[")) return null;
            if (!Line.EndsWith("]")) return null;
            if (Line.Length < 3) return null;
            return Line.Substring(1, Line.Length - 2);
        }

        // *** Parse key+value pair ***
        private bool ParseKeyValuePair(string Line, ref string Key, ref string Value)
        {
            // *** Check for key+value pair ***
            int i;
            if ((i = Line.IndexOf('=')) <= 0) return false;

            int j = Line.Length - i - 1;
            Key = Line.Substring(0, i).Trim();
            if (Key.Length <= 0) return false;

            Value = (j > 0) ? (Line.Substring(i + 1, j).Trim()) : ("");
            return true;
        }

        // *** Read file contents into local cache ***
        internal void Refresh()
        {
            lock (m_Lock)
            {
                StreamReader sr = null;
                try
                {
                    // *** Clear local cache ***
                    m_Sections.Clear();
                    m_Modified.Clear();

                    // *** Open the INI file ***
                    try
                    {
                        sr = new StreamReader(m_FileName);
                    }
                    catch (FileNotFoundException)
                    {
                        return;
                    }

                    // *** Read up the file content ***
                    Dictionary<string, string> CurrentSection = null;
                    string s;
                    string SectionName;
                    string Key = null;
                    string Value = null;
                    while ((s = sr.ReadLine()) != null)
                    {
                        s = s.Trim();

                        // *** Check for section names ***
                        SectionName = ParseSectionName(s);
                        if (SectionName != null)
                        {
                            // *** Only first occurrence of a section is loaded ***
                            if (m_Sections.ContainsKey(SectionName))
                            {
                                CurrentSection = null;
                            }
                            else
                            {
                                CurrentSection = new Dictionary<string, string>();
                                m_Sections.Add(SectionName, CurrentSection);
                            }
                        }
                        else if (CurrentSection != null)
                        {
                            // *** Check for key+value pair ***
                            if (ParseKeyValuePair(s, ref Key, ref Value))
                            {
                                // *** Only first occurrence of a key is loaded ***
                                if (!CurrentSection.ContainsKey(Key))
                                {
                                    CurrentSection.Add(Key, Value);
                                }
                            }
                        }
                    }
                }
                finally
                {
                    // *** Cleanup: close file ***
                    if (sr != null) sr.Close();
                    sr = null;
                }
            }
        }

        // *** Flush local cache content ***
        internal void Flush()
        {
            lock (m_Lock)
            {
                PerformFlush();
            }
        }

        private void PerformFlush()
        {
            // *** If local cache was not modified, exit ***
            if (!m_CacheModified) return;
            m_CacheModified = false;

            // *** Check if original file exists ***
            bool OriginalFileExists = File.Exists(m_FileName);

            // *** Get temporary file name ***
            string TmpFileName = Path.ChangeExtension(m_FileName, "$n$");

            // *** Copy content of original file to temporary file, replace modified values ***
            StreamWriter sw = null;

            // *** Create the temporary file ***
            sw = new StreamWriter(TmpFileName);

            try
            {
                Dictionary<string, string> CurrentSection = null;
                if (OriginalFileExists)
                {
                    StreamReader sr = null;
                    try
                    {
                        // *** Open the original file ***
                        sr = new StreamReader(m_FileName);

                        // *** Read the file original content, replace changes with local cache values ***
                        string s;
                        string SectionName;
                        string Key = null;
                        string Value = null;
                        bool Unmodified;
                        bool Reading = true;
                        while (Reading)
                        {
                            s = sr.ReadLine();
                            Reading = (s != null);

                            // *** Check for end of file ***
                            if (Reading)
                            {
                                Unmodified = true;
                                s = s.Trim();
                                SectionName = ParseSectionName(s);
                            }
                            else
                            {
                                Unmodified = false;
                                SectionName = null;
                            }

                            // *** Check for section names ***
                            if ((SectionName != null) || (!Reading))
                            {
                                if (CurrentSection != null)
                                {
                                    // *** Write all remaining modified values before leaving a section ****
                                    if (CurrentSection.Count > 0)
                                    {
                                        foreach (string fkey in CurrentSection.Keys)
                                        {
                                            if (CurrentSection.TryGetValue(fkey, out Value))
                                            {
                                                sw.Write(fkey);
                                                sw.Write('=');
                                                sw.WriteLine(Value);
                                            }
                                        }
                                        sw.WriteLine();
                                        CurrentSection.Clear();
                                    }
                                }

                                if (Reading)
                                {
                                    // *** Check if current section is in local modified cache ***
                                    if (!m_Modified.TryGetValue(SectionName, out CurrentSection))
                                    {
                                        CurrentSection = null;
                                    }
                                }
                            }
                            else if (CurrentSection != null)
                            {
                                // *** Check for key+value pair ***
                                if (ParseKeyValuePair(s, ref Key, ref Value))
                                {
                                    if (CurrentSection.TryGetValue(Key, out Value))
                                    {
                                        // *** Write modified value to temporary file ***
                                        Unmodified = false;
                                        CurrentSection.Remove(Key);

                                        sw.Write(Key);
                                        sw.Write('=');
                                        sw.WriteLine(Value);
                                    }
                                }
                            }

                            // *** Write unmodified lines from the original file ***
                            if (Unmodified)
                            {
                                sw.WriteLine(s);
                            }
                        }

                        // *** Close the original file ***
                        sr.Close();
                        sr = null;
                    }
                    finally
                    {
                        // *** Cleanup: close files ***                  
                        if (sr != null) sr.Close();
                        sr = null;
                    }
                }

                // *** Cycle on all remaining modified values ***
                foreach (KeyValuePair<string, Dictionary<string, string>> SectionPair in m_Modified)
                {
                    CurrentSection = SectionPair.Value;
                    if (CurrentSection.Count > 0)
                    {
                        sw.WriteLine();

                        // *** Write the section name ***
                        sw.Write('[');
                        sw.Write(SectionPair.Key);
                        sw.WriteLine(']');

                        // *** Cycle on all key+value pairs in the section ***
                        foreach (KeyValuePair<string, string> ValuePair in CurrentSection)
                        {
                            // *** Write the key+value pair ***
                            sw.Write(ValuePair.Key);
                            sw.Write('=');
                            sw.WriteLine(ValuePair.Value);
                        }
                        CurrentSection.Clear();
                    }
                }
                m_Modified.Clear();

                // *** Close the temporary file ***
                sw.Close();
                sw = null;

                // *** Rename the temporary file ***
                File.Copy(TmpFileName, m_FileName, true);

                // *** Delete the temporary file ***
                File.Delete(TmpFileName);
            }
            finally
            {
                // *** Cleanup: close files ***                  
                if (sw != null) sw.Close();
                sw = null;
            }
        }

        // *** Read a value from local cache ***
        internal string GetValue(string SectionName, string Key, string DefaultValue)
        {
            // *** Lazy loading ***
            if (m_Lazy)
            {
                m_Lazy = false;
                Refresh();
            }

            lock (m_Lock)
            {
                // *** Check if the section exists ***
                Dictionary<string, string> Section;
                if (!m_Sections.TryGetValue(SectionName, out Section)) return DefaultValue;

                // *** Check if the key exists ***
                string Value;
                if (!Section.TryGetValue(Key, out Value)) return DefaultValue;

                // *** Return the found value ***
                return Value;
            }
        }

        // *** Insert or modify a value in local cache ***
        internal void SetValue(string SectionName, string Key, string Value)
        {
            // *** Lazy loading ***
            if (m_Lazy)
            {
                m_Lazy = false;
                Refresh();
            }

            lock (m_Lock)
            {
                // *** Flag local cache modification ***
                m_CacheModified = true;

                // *** Check if the section exists ***
                Dictionary<string, string> Section;
                if (!m_Sections.TryGetValue(SectionName, out Section))
                {
                    // *** If it doesn't, add it ***
                    Section = new Dictionary<string, string>();
                    m_Sections.Add(SectionName, Section);
                }

                // *** Modify the value ***
                if (Section.ContainsKey(Key)) Section.Remove(Key);
                Section.Add(Key, Value);

                // *** Add the modified value to local modified values cache ***
                if (!m_Modified.TryGetValue(SectionName, out Section))
                {
                    Section = new Dictionary<string, string>();
                    m_Modified.Add(SectionName, Section);
                }

                if (Section.ContainsKey(Key)) Section.Remove(Key);
                Section.Add(Key, Value);

                // *** Automatic flushing : immediately write any modification to the file ***
                if (m_AutoFlush) PerformFlush();
            }
        }

        // *** Encode byte array ***
        private string EncodeByteArray(byte[] Value)
        {
            if (Value == null) return null;

            StringBuilder sb = new StringBuilder();
            foreach (byte b in Value)
            {
                string hex = Convert.ToString(b, 16);
                int l = hex.Length;
                if (l > 2)
                {
                    sb.Append(hex.Substring(l - 2, 2));
                }
                else
                {
                    if (l < 2) sb.Append("0");
                    sb.Append(hex);
                }
            }
            return sb.ToString();
        }

        // *** Decode byte array ***
        private byte[] DecodeByteArray(string Value)
        {
            if (Value == null) return null;

            int l = Value.Length;
            if (l < 2) return new byte[] { };

            l /= 2;
            byte[] Result = new byte[l];
            for (int i = 0; i < l; i++) Result[i] = Convert.ToByte(Value.Substring(i * 2, 2), 16);
            return Result;
        }

        // *** Getters for various types ***
        internal bool GetValue(string SectionName, string Key, bool DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, DefaultValue.ToString(System.Globalization.CultureInfo.InvariantCulture));
            bool Value;
            if (bool.TryParse(StringValue, out Value)) return (Value);
            return DefaultValue;
        }

        internal int GetValue(string SectionName, string Key, int DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            int Value;
            if (int.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out Value)) return Value;
            return DefaultValue;
        }

        internal long GetValue(string SectionName, string Key, long DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            long Value;
            if (long.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out Value)) return Value;
            return DefaultValue;
        }

        internal double GetValue(string SectionName, string Key, double DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            double Value;
            if (double.TryParse(StringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out Value)) return Value;
            return DefaultValue;
        }

        internal byte[] GetValue(string SectionName, string Key, byte[] DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, EncodeByteArray(DefaultValue));
            try
            {
                return DecodeByteArray(StringValue);
            }
            catch (FormatException)
            {
                return DefaultValue;
            }
        }

        internal DateTime GetValue(string SectionName, string Key, DateTime DefaultValue)
        {
            string StringValue = GetValue(SectionName, Key, DefaultValue.ToString(CultureInfo.InvariantCulture));
            DateTime Value;
            if (DateTime.TryParse(StringValue, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.NoCurrentDateDefault | DateTimeStyles.AssumeLocal, out Value)) return Value;
            return DefaultValue;
        }

        // *** Setters for various types ***
        internal void SetValue(string SectionName, string Key, bool Value)
        {
            SetValue(SectionName, Key, (Value).ToString(CultureInfo.InvariantCulture));
        }

        internal void SetValue(string SectionName, string Key, int Value)
        {
            SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        internal void SetValue(string SectionName, string Key, long Value)
        {
            SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        internal void SetValue(string SectionName, string Key, double Value)
        {
            SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        internal void SetValue(string SectionName, string Key, byte[] Value)
        {
            SetValue(SectionName, Key, EncodeByteArray(Value));
        }

        internal void SetValue(string SectionName, string Key, DateTime Value)
        {
            SetValue(SectionName, Key, Value.ToString(CultureInfo.InvariantCulture));
        }

        #endregion

    }
#endregion
}
