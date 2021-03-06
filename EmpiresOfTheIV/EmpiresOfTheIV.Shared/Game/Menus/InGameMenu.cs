﻿using Anarian;
using Anarian.DataStructures;
using Anarian.DataStructures.Animation;
using Anarian.DataStructures.Animation.Aux;
using Anarian.DataStructures.Rendering;
using Anarian.DataStructures.ScreenEffects;
using Anarian.Enumerators;
using Anarian.Events;
using Anarian.GUI;
using Anarian.Helpers;
using Anarian.IDManagers;
using Anarian.Interfaces;
using EmpiresOfTheIV.Data_Models;
using EmpiresOfTheIV.Game.Commands;
using EmpiresOfTheIV.Game.Enumerators;
using EmpiresOfTheIV.Game.GameObjects;
using EmpiresOfTheIV.Game.Game_Tools;
using EmpiresOfTheIV.Game.Loading;
using EmpiresOfTheIV.Game.Menus.PageParameters;
using EmpiresOfTheIV.Game.Networking;
using EmpiresOfTheIV.Game.Players;
using KillerrinStudiosToolkit.Events;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using EmpiresOfTheIV.Game.GameObjects.Factories;
using KillerrinStudiosToolkit.Enumerators;
using Microsoft.Xna.Framework.Audio;
using EmpiresOfTheIV.Game.GameObjects.ParticleEmitters;
using Anarian.Particles.Particle2D;
using Anarian.DataStructures.Components;

namespace EmpiresOfTheIV.Game.Menus
{
    public class InGameMenu : GameMenu,
                              IUpdatable, IRenderable
    {
        #region Fields/Properties
        NetworkManager m_networkManager;
        GamePausedState m_pausedState;

        Overlay m_overlay;
        Vector2 centerOfScreen;
        
        #region Assets
        SpriteFont m_empiresOfTheIVFont;
        SpriteFont m_empiresOfTheIVFontSmall;

        Texture2D m_blankTexture;
        Texture2D m_selectionTexture;

        Color m_guiZoneColor = Color.Blue * 0.5f;

        GUIButton m_guiGestureButton;
        GUIButton m_guiSelectionButton;
        GUIButton m_guiCameraPanButton;
        GUIButton m_guiIssueCommandButton;


        Texture2D m_currencyTexture; 
        Texture2D m_metalTexture;
        Texture2D m_energyTexture;
        Texture2D m_unitCapTexture;
        #endregion

        #region Loading
        private object loadinglockObject = new object();

        LoadingProgress m_currentLoadingProgress;
        Progress<LoadingProgress> m_loadingProgress;
        Task<LoadingStatus> m_loadingContentTask;
        Texture2D m_loadingMiniMap;
        #endregion

        #region Networking
        private object opponentloadedLockObject = new object();
        bool opponentFullyLoaded = false;
        WaitingForDataState waitingForDataState = WaitingForDataState.WaitingForLoading;

        Timer m_networkTimer;
        #endregion

        #region Page Parameters
        InGamePageParameter m_pageParameter;
        Player m_me;
        Team m_team1;
        Team m_team2;

        ChatManager m_chatManager;
        #endregion

        public RenderPassMode CurrentRenderPassMode;

        public UniversalCamera m_gameCamera;

        public AudioListener m_audioListener;
        public AudioEmitter m_audioEmitter;

        public List<SoundEffectInstance> m_activeSoundEffectInstances;
        public List<ParticleEmitter2D> m_activeParticleEmitters;

        public InputMode m_inputMode;
        int m_cameraMovementScreenBuffer = 30;
        int m_guiDistanceFromSide = 0;
        float m_unitSightHeightIncrease = 0.4f;

        public SelectionManager m_selectionManager;
        public UnitPool m_unitPool;
        public CommandRelay m_commandRelay;
        public BuildMenuManager m_buildMenuManager;
        
        public Map m_map;

        public Timer m_gameOverTransition;
        #endregion

        #region Constructor, PageSettings
        public InGameMenu(EmpiresOfTheIVGame game, object parameter)
            :base(game, parameter, GameState.InGame)
        {
            m_networkManager = m_game.NetworkManager;
            m_networkTimer = new Timer(m_networkManager.ConnectionPreventTimeoutTick);
            m_networkTimer.Completed += m_networkTimer_Completed;

            m_gameOverTransition = new Timer(TimeSpan.FromSeconds(2.0));
            m_gameOverTransition.Completed += m_gameOverTransition_Completed;

            m_pausedState = GamePausedState.WaitingForData;
            m_inputMode = InputMode.Gesture;
            CurrentRenderPassMode = RenderPassMode.None;

            m_currentLoadingProgress = new LoadingProgress(0, "");
            m_loadingProgress = new Progress<LoadingProgress>();
            m_loadingProgress.ProgressChanged += m_loadingProgress_ProgressChanged;

            m_overlay = new Overlay(m_game.GraphicsDevice, Color.Black);
            m_overlay.FadePercentage = 0.75f;

            centerOfScreen = new Vector2(AnarianConsts.ScreenRectangle.Width / 2.0f, AnarianConsts.ScreenRectangle.Height / 2.0f);

            // Get basic Assets
            m_blankTexture = m_game.ResourceManager.GetAsset(typeof(Texture2D), ResourceManager.EngineReservedAssetNames.blankTextureName) as Texture2D;
            m_empiresOfTheIVFont = m_game.ResourceManager.GetAsset(typeof(SpriteFont), "EmpiresOfTheIVFont") as SpriteFont;
            m_empiresOfTheIVFontSmall = m_game.ResourceManager.GetAsset(typeof(SpriteFont), "EmpiresOfTheIVFont Small") as SpriteFont;

            // Subscribe to Events
            m_networkManager.OnConnected += NetworkManager_OnConnected;
            m_networkManager.OnDisconnected += NetworkManager_OnDisconnected;
            m_networkManager.OnMessageRecieved += NetworkManager_OnMessageRecieved;
            m_networkManager.OnSystemPacketRecieved += NetworkManager_OnSystemPacketRecieved;
            m_networkManager.OnGamePacketRecieved += NetworkManager_OnGamePacketRecieved;

            Consts.Game.StateManager.OnBackButtonPressed += StateManager_OnBackButtonPressed;
            Consts.Game.StateManager.HandleBackButtonPressed = false;

            // Save the parameters
            m_pageParameter = (InGamePageParameter)parameter;
            m_me = m_pageParameter.me;           Debug.WriteLine("Me: " + m_me.ToString());
            m_team1 = m_pageParameter.team1;     Debug.WriteLine(m_team1.ToString());
            m_team2 = m_pageParameter.team2;     Debug.WriteLine(m_team2.ToString());

            m_chatManager = m_pageParameter.chatManager;

            #region Load the Minimap
            switch (m_pageParameter.MapName)
            {
                case MapName.RadientValley:
                    if (GameConsts.Loading.Map_RadientFlatlands == LoadingStatus.Loaded) {
                        m_loadingMiniMap = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Radient Valley MiniMap") as Texture2D;
                    }
                    else {
                        m_loadingMiniMap = m_game.ResourceManager.LoadAsset(m_game.Content, typeof(Texture2D), "Textures/Maps/Radient Valley MiniMap") as Texture2D;
                    }
                    break;
                case MapName.Kalia:
                    if (GameConsts.Loading.Map_Kalia == LoadingStatus.Loaded) {
                        m_loadingMiniMap = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Kalia MiniMap") as Texture2D;
                    }
                    else {
                        m_loadingMiniMap = m_game.ResourceManager.LoadAsset(m_game.Content, typeof(Texture2D), "Textures/Maps/Kalia MiniMap") as Texture2D;
                    }
                    break;
                default: m_loadingMiniMap = null; break;
            }
            #endregion

            //// Begin the Asynchronous Loading
            //m_loadingContentTask = Task.Run(() => LoadContent(m_loadingProgress, m_game.Content, m_game.GraphicsDevice));
        }

        public override void MenuLoaded()
        {
            base.MenuLoaded();

            // Turn off the Unified Menu
            m_game.SceneManager.Active = false;

            //if (NavigationSaveState == Anarian.Enumerators.NavigationSaveState.KeepSate)
            //{
            //}

            // Begin the Asynchronous Loading
            m_loadingContentTask = Task.Run(() => LoadContent(m_loadingProgress, m_game.Content, m_game.GraphicsDevice));
        }

        public override void MenuExited()
        {
            base.MenuExited();

            m_game.InputManager.PointerDown -= InputManager_PointerDown;
            m_game.InputManager.PointerPressed -= InputManager_PointerClicked;
            m_game.InputManager.PointerMoved -= InputManager_PointerMoved;
            m_game.InputManager.Keyboard.KeyboardDown -= Keyboard_KeyboardDown;
            m_game.InputManager.Keyboard.KeyboardPressed -= Keyboard_KeyboardPressed;

            m_networkManager.OnConnected -= NetworkManager_OnConnected;
            m_networkManager.OnDisconnected -= NetworkManager_OnDisconnected;
            m_networkManager.OnMessageRecieved -= NetworkManager_OnMessageRecieved;
            m_networkManager.OnSystemPacketRecieved -= NetworkManager_OnSystemPacketRecieved;
            m_networkManager.OnGamePacketRecieved -= NetworkManager_OnGamePacketRecieved;

            Consts.Game.StateManager.OnBackButtonPressed -= StateManager_OnBackButtonPressed;

            // De-Subscribe to our Events
            // Pointer
            m_game.InputManager.PointerDown -= InputManager_PointerDown;
            m_game.InputManager.PointerPressed -= InputManager_PointerClicked;
            m_game.InputManager.PointerMoved -= InputManager_PointerMoved;

            // Keyboard
            m_game.InputManager.Keyboard.KeyboardDown -= Keyboard_KeyboardDown;
            m_game.InputManager.Keyboard.KeyboardPressed -= Keyboard_KeyboardPressed;
        }

        public override void SendMessage(object message)
        {
            base.SendMessage(message);
        }

        private void StateManager_OnBackButtonPressed(object sender, EventArgs e)
        {
            if (m_currentLoadingProgress.Progress < 100) { return; }

            if (m_buildMenuManager.Active)
                m_buildMenuManager.Disable();
        }


        void m_gameOverTransition_Completed(object sender, EventArgs e)
        {
            NavigateGameOver();
        }
        public void NavigateGameOver()
        {
            if (m_networkManager.HostSettings == KillerrinStudiosToolkit.Enumerators.HostType.Host)
            {
                SystemPacket sp = new SystemPacket(true, SystemPacketID.Gameover, "");
                m_networkManager.SendMessage(sp.ThisToJson());
            }

            GameOverPageParameter pageParam = new GameOverPageParameter();
            pageParam.GameConnectionType = m_pageParameter.GameConnectionType;
            pageParam.GameType = m_pageParameter.GameType;
            pageParam.MapName = m_pageParameter.MapName;
            pageParam.maxUnitsPerPlayer = m_pageParameter.maxUnitsPerPlayer;
            pageParam.team1 = m_pageParameter.team1;
            pageParam.team2 = m_pageParameter.team2;
            pageParam.me = m_pageParameter.me;

            // Describe from Events
            m_game.InputManager.PointerDown -= InputManager_PointerDown;
            m_game.InputManager.PointerPressed -= InputManager_PointerClicked;
            m_game.InputManager.PointerMoved -= InputManager_PointerMoved;
            m_game.InputManager.Keyboard.KeyboardDown -= Keyboard_KeyboardDown;
            m_game.InputManager.Keyboard.KeyboardPressed -= Keyboard_KeyboardPressed;

            m_networkManager.OnConnected -= NetworkManager_OnConnected;
            m_networkManager.OnDisconnected -= NetworkManager_OnDisconnected;
            m_networkManager.OnMessageRecieved -= NetworkManager_OnMessageRecieved;
            m_networkManager.OnSystemPacketRecieved -= NetworkManager_OnSystemPacketRecieved;
            m_networkManager.OnGamePacketRecieved -= NetworkManager_OnGamePacketRecieved;

            Consts.Game.StateManager.OnBackButtonPressed -= StateManager_OnBackButtonPressed;

            // Lets Go!
            m_game.StateManager.RemovePreviousOnCompleted = true;
            m_game.StateManager.Navigate(GameState.GameOver, pageParam);
        }
        #endregion

        #region Loading
        private async Task<LoadingStatus> LoadContent(IProgress<LoadingProgress> progress, ContentManager Content, GraphicsDevice graphics)
        {
            if (progress != null) progress.Report(new LoadingProgress(0, "Initial Setup"));

            m_selectionTexture = m_game.ResourceManager.GetAsset(typeof(Texture2D), "SelectionBox Icon") as Texture2D;
            m_currencyTexture = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Currency") as Texture2D;
            m_metalTexture = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Metal") as Texture2D;
            m_energyTexture = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Energy") as Texture2D;
            m_unitCapTexture = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Unit Cap") as Texture2D;

            #region Setup GUI
            Rectangle buttonRect = new Rectangle(10, 20, 100, 100);
            int yDistanceBetweenItems = 20;
            Color guiColor = Color.White;

            // Setup the UI Deadzone
            m_guiDistanceFromSide += buttonRect.Width + (buttonRect.X * 2);

            // Setup the Left Bar
            m_guiGestureButton = new GUIButton(m_game.ResourceManager.GetAsset(typeof(Texture2D), "Gesture UI Icon") as Texture2D, buttonRect, guiColor);

            buttonRect.Y = AnarianConsts.ScreenRectangle.Height - buttonRect.Height - yDistanceBetweenItems;
            m_guiCameraPanButton = new GUIButton(m_game.ResourceManager.GetAsset(typeof(Texture2D), "Camera Movement UI Icon") as Texture2D, buttonRect, guiColor);

            buttonRect.Y -= yDistanceBetweenItems + buttonRect.Height;
            m_guiSelectionButton = new GUIButton(m_game.ResourceManager.GetAsset(typeof(Texture2D), "Selection UI Icon") as Texture2D, buttonRect, guiColor);

            // Setup the Bottom Bar
            buttonRect.X = m_guiDistanceFromSide + yDistanceBetweenItems;
            buttonRect.Y += yDistanceBetweenItems + buttonRect.Height;
            m_guiIssueCommandButton = new GUIButton(m_game.ResourceManager.GetAsset(typeof(Texture2D), "Issue Command UI Icon") as Texture2D, buttonRect, Color.DarkRed);
            m_guiIssueCommandButton.Active = false;
            #endregion

            #region Setup Variables
            m_gameCamera = new UniversalCamera();
            m_gameCamera.AspectRatio = UniversalCamera.Aspect16x9;
            m_gameCamera.Near = 0.2f;
            m_gameCamera.Far = 1000.0f;
            m_gameCamera.Speed = 1.0f;

            //// Since Phone has a smaller screen and a lower TouchScreen input frequency, we double the speed
            //if (KillerrinStudiosToolkit.KillerrinApplicationData.OSType == KillerrinStudiosToolkit.Enumerators.ClientOSType.WindowsPhone81)
            //    m_gameCamera.Speed *= 2;

            //Camera Position: {X:4.199995 Y:55.02913 Z:15.78831}, 
            m_gameCamera.DefaultCameraPosition = new Vector3(4.20f, 50.03f, 15.79f);

            //Camera Rotation: {M11:1 M12:0 M13:0 M14:0} {M21:0 M22:0.2419228 M23:-0.9702981 M24:0} {M31:0 M32:0.9702981 M33:0.2419228 M34:0} {M41:0 M42:0 M43:0 M44:1}
            m_gameCamera.DefaultCameraRotation = new Matrix(1, 0, 0, 0,
                                                            0, 0.242f, -0.97f, 0,
                                                            0, 0.97f, 0.24f, 0,
                                                            0, 0, 0, 1);

            m_gameCamera.ResetViewToDefaults();

            m_audioListener = new AudioListener();
            m_audioEmitter = new AudioEmitter();
            m_audioListener.Position = m_gameCamera.Position;
            m_audioEmitter.Position = m_gameCamera.Position;

            m_activeSoundEffectInstances = new List<SoundEffectInstance>();
            m_activeParticleEmitters = new List<ParticleEmitter2D>();

            m_unitPool = new UnitPool((int)(m_pageParameter.maxUnitsPerPlayer * (m_team1.PlayerCount + m_team2.PlayerCount)));
            m_commandRelay = new CommandRelay();

            IDManager unitIDManager = new IDManager();
            IDManager factoryBaseIDManager = new IDManager();

            m_selectionManager = new SelectionManager();
            m_selectionManager.SelectionTexture = m_selectionTexture;
            #endregion

            if (progress != null) progress.Report(new LoadingProgress(20, "Loading Empires"));

            #region Load Empires
            #region Unanian Empire
            AnimatedModel unanianGroundSoldierTPose = null;
            AnimatedModel unanianGroundSoldierAnimation = null;
            AnimationClip unanianGroundSoldierWalkAnimClip = null;

            Model         unanianFactory = null;
            AnimatedModel unanianSpaceshipFighter = null;

            if (m_team1.IsPlayerEmpire(EmpireType.UnanianEmpire) || m_team2.IsPlayerEmpire(EmpireType.UnanianEmpire))
            {
                if (GameConsts.Loading.Empire_UnanianEmpireLoaded == LoadingStatus.Loaded)
                {
                    unanianGroundSoldierTPose = m_game.ResourceManager.GetAsset(typeof(AnimatedModel), UnitID.UnanianSoldier.ToString() + "|" + ModelType.AnimatedModel.ToString()) as AnimatedModel;
                    unanianGroundSoldierAnimation = m_game.ResourceManager.GetAsset(typeof(AnimatedModel), UnitID.UnanianSoldier.ToString() + "|" + ModelType.Animation.ToString()) as AnimatedModel;

                    unanianSpaceshipFighter = m_game.ResourceManager.GetAsset(typeof(AnimatedModel), UnitID.UnanianSpaceFighter.ToString() + "|" + ModelType.AnimatedModel.ToString()) as AnimatedModel;

                    unanianFactory = m_game.ResourceManager.GetAsset(typeof(Model), "Unanian Factory") as Model;
                }
                else
                {
                    GameConsts.Loading.Empire_UnanianEmpireLoaded = LoadingStatus.CurrentlyLoading;

                    unanianGroundSoldierTPose = m_game.ResourceManager.LoadAsset(Content, typeof(AnimatedModel), "Models/Units/Unanian Empire/t-pose_3", UnitID.UnanianSoldier.ToString() + "|" + ModelType.AnimatedModel.ToString()) as AnimatedModel;
                    unanianGroundSoldierAnimation = m_game.ResourceManager.LoadAsset(Content, typeof(AnimatedModel), "Models/Units/Unanian Empire/walk", UnitID.UnanianSoldier.ToString() + "|" + ModelType.Animation.ToString()) as AnimatedModel;

                    unanianSpaceshipFighter = m_game.ResourceManager.LoadAsset(Content, typeof(AnimatedModel), "Models/Units/Unanian Empire/Unanian Fighter", UnitID.UnanianSpaceFighter.ToString() + "|" + ModelType.AnimatedModel.ToString()) as AnimatedModel;

                    unanianFactory = m_game.ResourceManager.LoadAsset(Content, typeof(Model), "Models/Factories/Unanian Empire/Unanian Factory") as Model;
                    GameConsts.Loading.Empire_UnanianEmpireLoaded = LoadingStatus.Loaded;
                }

                unanianGroundSoldierWalkAnimClip = unanianGroundSoldierAnimation.Clips[0];

                foreach (ModelMesh mesh in unanianFactory.Meshes)
                {
                    var bs = mesh.BoundingSphere;
                    bs.Radius = 130.0f;
                    mesh.BoundingSphere = bs;
                }
            }
            #endregion

            #region Crescanian Confederacy
            //AnimatedModel crescanianGroundSoldierTPose = null;
            //AnimatedModel crescanianGroundSoldierAnimation = null;
            //AnimationClip crescanianGroundSoldierWalkAnimClip = null;
            //
            //Model         crescanianFactory = null;
            //AnimatedModel crescanianSpaceshipFighter = null;
            //
            //if (m_team1.IsPlayerEmpire(EmpireType.CrescanianConfederation) || m_team2.IsPlayerEmpire(EmpireType.CrescanianConfederation))
            //{
            //    if (GameConsts.Loading.Empire_CrescanianConfederationLoaded == LoadingStatus.Loaded)
            //    {
            //
            //    }
            //    else
            //    {
            //        GameConsts.Loading.Empire_CrescanianConfederationLoaded = LoadingStatus.CurrentlyLoading;
            //
            //        GameConsts.Loading.Empire_CrescanianConfederationLoaded = LoadingStatus.Loaded;
            //    }
            //}
            #endregion

            #region Kingdom of Edolas
            //AnimatedModel kingdomOfEdolasGroundSoldierTPose = null;
            //AnimatedModel kingdomOfEdolasGroundSoldierAnimation = null;
            //AnimationClip kingdomOfEdolasGroundSoldierWalkAnimClip = null;
            //
            //Model         kingdomOfEdolasFactory= null;
            //AnimatedModel kingdomOfEdolasSpaceshipFighter = null;
            //
            //if (m_team1.IsPlayerEmpire(EmpireType.TheKingdomOfEdolas) || m_team2.IsPlayerEmpire(EmpireType.TheKingdomOfEdolas))
            //{
            //    if (GameConsts.Loading.Empire_KingdomOfEdolasLoaded == LoadingStatus.Loaded)
            //    {
            //
            //    }
            //    else
            //    {
            //        GameConsts.Loading.Empire_KingdomOfEdolasLoaded = LoadingStatus.CurrentlyLoading;
            //
            //        GameConsts.Loading.Empire_KingdomOfEdolasLoaded = LoadingStatus.Loaded;
            //    }
            //}
            #endregion
            #endregion

            // We need the factory, so we load the BuildMenuManager Here
            m_buildMenuManager = new BuildMenuManager(m_guiZoneColor, m_me);

            if (progress != null) progress.Report(new LoadingProgress(40, "Loading Map"));

            #region Load the Map
            Texture2D heightMap = null;
            Texture2D mapTexture = null;
            Texture2D mapParallax = null;
            MapTerrain mapTerrain = null;

            Model factoryBaseModel = null;
            FactoryBase[] factoryBases;

            switch (m_pageParameter.MapName)
            {
                #region Radient Flatlands
                case MapName.RadientValley:
                    #region Load/Get the Data
                    if (GameConsts.Loading.Map_RadientFlatlands == LoadingStatus.Loaded)
                    {
                        heightMap = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Radient Valley HeightMap") as Texture2D;
                        mapTexture = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Radient Valley Texture") as Texture2D;
                        mapParallax = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Radient Valley Parallax") as Texture2D;

                        mapTerrain = m_game.PrefabManager.GetPrefab("Radient Valley Terrain") as MapTerrain;

                        factoryBaseModel = m_game.ResourceManager.GetAsset(typeof(Model), "Radient Valley FactoryBase") as Model;
                    }
                    else 
                    {
                        GameConsts.Loading.Map_RadientFlatlands = LoadingStatus.CurrentlyLoading;

                        heightMap = m_game.ResourceManager.LoadAsset(Content, typeof(Texture2D), "Textures/Maps/Radient Valley HeightMap") as Texture2D;
                        mapTexture = m_game.ResourceManager.LoadAsset(Content, typeof(Texture2D), "Textures/Maps/Radient Valley Texture") as Texture2D;
                        mapParallax = Color.Black.CreateTextureFromSolidColor(graphics, 1, 1); m_game.ResourceManager.AddAsset(mapParallax, "Radient Valley Parallax");

                        mapTerrain = new MapTerrain(graphics, heightMap, mapTexture);
                        m_game.PrefabManager.AddPrefab(mapTerrain, "Radient Valley Terrain");

                        factoryBaseModel = m_game.ResourceManager.LoadAsset(Content, typeof(Model), "Models/Factories/Factory Base", "Radient Valley FactoryBase") as Model;

                        GameConsts.Loading.Map_RadientFlatlands = LoadingStatus.Loaded;
                    }
                    #endregion

                    if (progress != null) progress.Report(new LoadingProgress(50, "Creating Factories"));

                    #region Create Factories
                    factoryBases = new FactoryBase[2];

                    // Because the Meshes Bounding Sphere is messed up, we fix it here
                    foreach (ModelMesh mesh in factoryBaseModel.Meshes)
                    {
                        var bs = mesh.BoundingSphere;
                        bs.Radius = 230.0f;
                        mesh.BoundingSphere = bs;
                    }

                    BoundingSphere bound = new BoundingSphere(Vector3.Zero, 11.0f);

                    // Factory 1
                    Vector3 factory1Spawn = new Vector3(-55.0f, 0.0f, -10.0f);
                    float f1Height = mapTerrain.GetHeightAtPoint(factory1Spawn);
                    factory1Spawn.Y = f1Height;

                    factoryBases[0] = new FactoryBase(factoryBaseIDManager.GetNewID(), factory1Spawn, bound);
                    factoryBases[0].Base = new Building();
                    factoryBases[0].Base.Transform.Position = new Vector3(-70.0f, 0.0f, -10.0f);
                    factoryBases[0].Base.Transform.Scale = new Vector3(0.05f);
                    factoryBases[0].Base.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(-90.0f), 0.0f, 0.0f);
                    factoryBases[0].Base.Transform.CreateAllMatrices();
                    factoryBases[0].Base.Model3D = factoryBaseModel;
                    factoryBases[0].Base.Active = true;
                    factoryBases[0].Base.CullDraw = false;
                    factoryBases[0].Base.UpdateBoundsEveryFrame = false;
                    factoryBases[0].Base.RenderBounds = false;
                
                    // Factory 2
                    Vector3 factory2Spawn = new Vector3(55.0f, 0.0f, 10.0f);
                    float f2Height = mapTerrain.GetHeightAtPoint(factory1Spawn);
                    factory2Spawn.Y = f2Height;

                    factoryBases[1] = new FactoryBase(factoryBaseIDManager.GetNewID(), factory2Spawn, bound);
                    factoryBases[1].Base = new Building();
                    factoryBases[1].Base.Transform.Position = new Vector3(70.0f, 0.0f, 10.0f);
                    factoryBases[1].Base.Transform.Scale = new Vector3(0.05f);
                    factoryBases[1].Base.Transform.Rotation = Quaternion.CreateFromYawPitchRoll(MathHelper.ToRadians(90.0f), 0.0f, 0.0f);
                    factoryBases[1].Base.Transform.CreateAllMatrices();
                    factoryBases[1].Base.Model3D = factoryBaseModel;
                    factoryBases[1].Base.Active = true;
                    factoryBases[1].Base.CullDraw = false;
                    factoryBases[1].Base.UpdateBoundsEveryFrame = false;
                    factoryBases[1].Base.RenderBounds = false;
                    #endregion
                    
                    #region Other
                    // Make the map
                    m_map = new Map(MapName.RadientValley, mapParallax, mapTerrain, factoryBases);
                    m_map.AddAvailableUnitType(UnitType.Soldier, UnitType.Vehicle, UnitType.Ship, UnitType.Air, UnitType.Space);

                    // Set GameCamera Starting Positions and Limits
                    var gameCameraPos = m_gameCamera.Position; 
                    if (m_team1.Exists(m_pageParameter.me.ID)) {
                        // Set Factory Owners
                        if (m_team2.PlayerCount > 0)
                            GameFactory.CreateFactoryOnFactoryBase(factoryBases[1], m_team2.Players[0]);
                        GameFactory.CreateFactoryOnFactoryBase(factoryBases[0], m_pageParameter.me);

                        switch (m_pageParameter.me.EmpireType)
                        {
                            case EmpireType.UnanianEmpire:                                break;
                            case EmpireType.CrescanianConfederation:                      break;
                            case EmpireType.TheKingdomOfEdolas:                           break;
                        }

                        // Set the Game Cameras Position
                        gameCameraPos.X = factoryBases[0].Base.Transform.Position.X;
                        gameCameraPos.Z = factoryBases[0].Base.Transform.Position.Z + 10;
                    }
                    else if (m_team2.Exists(m_pageParameter.me.ID)) {
                        // Set Factory Owners
                        if (m_team1.PlayerCount > 0)
                            GameFactory.CreateFactoryOnFactoryBase(factoryBases[0], m_team1.Players[0]);
                        GameFactory.CreateFactoryOnFactoryBase(factoryBases[1], m_pageParameter.me);

                        switch (m_pageParameter.me.EmpireType)
                        {
                            case EmpireType.UnanianEmpire: break;
                            case EmpireType.CrescanianConfederation: break;
                            case EmpireType.TheKingdomOfEdolas: break;
                        }

                        // Set the Game Cameras Position
                        gameCameraPos.X = factoryBases[1].Base.Transform.Position.X;
                        gameCameraPos.Z = factoryBases[1].Base.Transform.Position.Z + 10;                        
                    }
                    m_gameCamera.Position = gameCameraPos;
                    m_gameCamera.DefaultCameraPosition = m_gameCamera.Position;

                    // MathHelper.Clamp(gameCameraPosition.Y, 30.0f, 56.0f);
                    m_gameCamera.MinClamp = new Vector3(-92.60f, m_gameCamera.DefaultCameraPosition.Y - 10, -18.35f);
                    m_gameCamera.MaxClamp = new Vector3(85.80f, m_gameCamera.DefaultCameraPosition.Y + 10,  36.74f);

                    m_map.MinimumMapBounds = new Vector3(-94.64308f, 12.90819f, -34.61902f);
                    m_map.MaximumMapBounds = new Vector3(93.01296f, 12.60246f, 35.42957f);
                    #endregion
                    break;
                #endregion

                #region Kalia
                case MapName.Kalia:
                    #region Load/Get the Data
                    if (GameConsts.Loading.Map_Kalia == LoadingStatus.Loaded)
                    {
                        heightMap = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Kalia HeightMap") as Texture2D;
                        mapTexture = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Kalia Texture") as Texture2D;
                        mapParallax = m_game.ResourceManager.GetAsset(typeof(Texture2D), "Kalia Parallax") as Texture2D;

                        mapTerrain = mapTerrain = m_game.PrefabManager.GetPrefab("Kalia Terrain") as MapTerrain;

                        factoryBaseModel = m_game.ResourceManager.GetAsset(typeof(Model), "Kalia FactoryBase") as Model;
                    }
                    else
                    {
                        GameConsts.Loading.Map_Kalia = LoadingStatus.CurrentlyLoading;

                        mapTexture = Color.Black.CreateTextureFromSolidColor(graphics, 1, 1);  m_game.ResourceManager.AddAsset(mapParallax, "Kalia Texture");
                        mapParallax = Color.Black.CreateTextureFromSolidColor(graphics, 1, 1); m_game.ResourceManager.AddAsset(mapParallax, "Kalia Parallax");
                        
                        mapTerrain = MapTerrain.CreateFlatTerrain(graphics, 200, 128, mapTexture);
                        heightMap = mapTerrain.HeightData.HeightMap; m_game.ResourceManager.AddAsset(mapParallax, "Kalia HeightMap");

                        m_game.PrefabManager.AddPrefab(mapTerrain, "Kalia Terrain");

                        factoryBaseModel = m_game.ResourceManager.LoadAsset(Content, typeof(Model), "Models/Factories/Factory Base", "Kalia FactoryBase") as Model;
                        GameConsts.Loading.Map_Kalia = LoadingStatus.Loaded;
                    }
#endregion

                    if (progress != null) progress.Report(new LoadingProgress(50, "Creating Factories"));

                    #region Create Factories
                    factoryBases = new FactoryBase[2];
                    #endregion

                    // Make the map
                    m_map = new Map(MapName.Kalia, mapParallax, mapTerrain, factoryBases);
                    m_map.AddAvailableUnitType(UnitType.Space);
                    break;
                #endregion
            }
            #endregion

            if (progress != null) progress.Report(new LoadingProgress(60, "Setting up Units"));

            #region Create all the Units in the pool
            for (int i = 0; i < m_unitPool.TotalUnitsInPool; i++)
            {
                var unit = new Unit(unitIDManager.GetNewID(), UnitType.None);
                m_unitPool.m_inactiveUnits.Add(unit);
            }
            #endregion

            if (progress != null) progress.Report(new LoadingProgress(80, "Preforming Technical Magic"));

            #region Subscribe to input Events
            // Subscribe to our Events
            // Pointer
            m_game.InputManager.PointerDown += InputManager_PointerDown;
            m_game.InputManager.PointerPressed += InputManager_PointerClicked;

            // Because Phone will have different input interactions, we will not subscribe to PointerMoved to save processing
            if (KillerrinStudiosToolkit.KillerrinApplicationData.OSType == KillerrinStudiosToolkit.Enumerators.ClientOSType.WindowsPhone81) { }
            else
            {
                m_game.InputManager.PointerMoved += InputManager_PointerMoved;
            }

            // Keyboard
            m_game.InputManager.Keyboard.KeyboardDown += Keyboard_KeyboardDown;
            m_game.InputManager.Keyboard.KeyboardPressed += Keyboard_KeyboardPressed;
            #endregion 

            if (progress != null) progress.Report(new LoadingProgress(99, "Taking out Ninjas"));

            #region Set Default Player Values and Assign Units to Players
            foreach (var player in m_team1.Players)
            {
                int minValue = (int)(player.ID * m_pageParameter.maxUnitsPerPlayer);
                int maxValue = (int)(player.ID * m_pageParameter.maxUnitsPerPlayer + m_pageParameter.maxUnitsPerPlayer);

                foreach (var unit in m_unitPool.m_inactiveUnits)
                {
                    if (unit.UnitID >= minValue &&
                        unit.UnitID < maxValue)
                    {
                        unit.PlayerID = player.ID;

                    }
                }
            }
            foreach (var player in m_team2.Players)
            {
                int minValue = (int)(player.ID * m_pageParameter.maxUnitsPerPlayer);
                int maxValue = (int)(player.ID * m_pageParameter.maxUnitsPerPlayer + m_pageParameter.maxUnitsPerPlayer);

                foreach (var unit in m_unitPool.m_inactiveUnits)
                {
                    if (unit.UnitID >= minValue &&
                        unit.UnitID < maxValue)
                    {
                        unit.PlayerID = player.ID;
                    }
                }
            }
            #endregion

            if (m_pageParameter.GameConnectionType == GameConnectionType.Singleplayer)
                m_pausedState = GamePausedState.Unpaused;
            else
            {
                m_pausedState = GamePausedState.WaitingForData;
                if (m_networkManager.HostSettings == KillerrinStudiosToolkit.Enumerators.HostType.Client)
                {
                    SystemPacket sp = new SystemPacket(true, SystemPacketID.GameLoaded, "");
                    m_networkManager.SendMessage(sp.ThisToJson());
                }
            }

            // Send the final report and return loaded
            if (progress != null) progress.Report(new LoadingProgress(100, "Ready to Play!"));
            return LoadingStatus.Loaded;
        }
        void m_loadingProgress_ProgressChanged(object sender, LoadingProgress e)
        {
            Debug.WriteLine("m_loadingProgress_ProgressChanged: " + e.ToString());
            lock (loadinglockObject)
            {
                m_currentLoadingProgress = e;
            }
        }
        #endregion

        #region Networking
        void m_networkTimer_Completed(object sender, EventArgs e)
        {
            if (m_networkManager.HostSettings == KillerrinStudiosToolkit.Enumerators.HostType.Host)
            {
                Debug.WriteLine("Sending Connection Tick");

                // Send an ACK to keep the connection opened
                SystemPacket sp = new SystemPacket(true, SystemPacketID.ConnectionTick, "");
                m_networkManager.SendMessage(sp.ThisToJson());
            }

            // Reset the timer
            m_networkTimer.Reset();
        }


        private void NetworkManager_OnConnected(object sender, OnConnectedEventArgs e)
        {
        }

        private void NetworkManager_OnDisconnected(object sender, EventArgs e)
        {
        }

        void NetworkManager_OnSystemPacketRecieved(object sender, EotIVPacketRecievedEventArgs e)
        {
            Debug.WriteLine("System Packet Recieved");
            SystemPacket systemPacket = e.Packet as SystemPacket;

            if (systemPacket.ID == SystemPacketID.Chat)
            {
                Debug.WriteLine("Chat Packet Recieved");
                JObject jObject = JObject.Parse(systemPacket.Command);
                ChatMessage chatMessage = JsonConvert.DeserializeObject<ChatMessage>(jObject.ToString());

                try
                {
                    m_chatManager.AddMessage(chatMessage);
                }
                catch (Exception) { }
            }
            else if (systemPacket.ID == SystemPacketID.GameLoaded)
            {
                lock (opponentloadedLockObject)
                {
                    opponentFullyLoaded = true;
                }
            }
            else if (systemPacket.ID == SystemPacketID.GameBegin)
            {
                m_pausedState = GamePausedState.Unpaused;
                waitingForDataState = WaitingForDataState.InGame;
            }
            else if (systemPacket.ID == SystemPacketID.Gameover)
            {
                NavigateGameOver();
            }
        }
        void NetworkManager_OnGamePacketRecieved(object sender, EotIVPacketRecievedEventArgs e)
        {
            Debug.WriteLine("Game Packet Recieved");
            GamePacket gamePacket = e.Packet as GamePacket;

            if (gamePacket.ID == GamePacketID.Command)
            {
                if (gamePacket.Commands != null)
                {
                    foreach (var command in gamePacket.Commands)
                    {
                        m_commandRelay.AddCommand(command, NetworkTrafficDirection.Inbound);
                    }
                }
            }
            else if (gamePacket.ID == GamePacketID.GameSync)
            {

            }
        }

        private void NetworkManager_OnMessageRecieved(object sender, ReceivedMessageEventArgs e)
        {
        }
        #endregion

        #region Input
        #region Pointer
        public List<PointerPressedEventArgs> m_activePointerEventsThisFrame = new List<PointerPressedEventArgs>();
        public List<int> ignorePointerIDs = new List<int>();

        bool touchDown = false;
        bool leftMouseDown = false;
        bool middleMouseDown = false;
        bool rightMouseDown = false;
        void InputManager_PointerDown(object sender, Anarian.Events.PointerPressedEventArgs e)
        {
            if (m_pausedState != GamePausedState.Unpaused) return;
            foreach (var i in ignorePointerIDs)
                if (i == e.ID)
                    return;

            //Debug.WriteLine("{0}, Pressed", e.ToString());
            m_activePointerEventsThisFrame.Add(e);
            
            if (e.Pointer == PointerPress.Touch) { touchDown = true; }
            else if (e.Pointer == PointerPress.LeftMouseButton) { leftMouseDown = true; }
            else if (e.Pointer == PointerPress.MiddleMouseButton) { middleMouseDown = true; }
            else if (e.Pointer == PointerPress.RightMouseButton) { rightMouseDown = true; }
        }

        public List<PointerPressedEventArgs> m_activePointerClickedEventsThisFrame = new List<PointerPressedEventArgs>();
        bool selectionReleased = false;
        void InputManager_PointerClicked(object sender, Anarian.Events.PointerPressedEventArgs e)
        {
            if (m_pausedState != GamePausedState.Unpaused) return;
            foreach (var i in ignorePointerIDs)
                if (i == e.ID)
                    return;
            
            m_activePointerClickedEventsThisFrame.Add(e);

            if (e.Pointer == PointerPress.Touch) { touchDown = false; }
            else if (e.Pointer == PointerPress.LeftMouseButton) { leftMouseDown = false; }
            else if (e.Pointer == PointerPress.MiddleMouseButton) { middleMouseDown = false; }
            else if (e.Pointer == PointerPress.RightMouseButton) { rightMouseDown = false; }

            if (e.Pointer == PointerPress.LeftMouseButton || e.Pointer == PointerPress.Touch)
                selectionReleased = true;

            // If we are in the Selection Input Mode, make sure we set the selection to be released
            // so the system doesn't keep the indicators on screen
            if (m_inputMode == InputMode.Selection)
                selectionReleased = true;
        }


        PointerMovedEventArgs m_lastPointerMovedEventArgs = new PointerMovedEventArgs(new GameTime());
        void InputManager_PointerMoved(object sender, Anarian.Events.PointerMovedEventArgs e)
        {
            if (m_pausedState != GamePausedState.Unpaused) return;
            //Debug.WriteLine("{0}, Moved", e.ToString());

            m_lastPointerMovedEventArgs = e;
        }
        #endregion
        
        #region Keyboard
        void Keyboard_KeyboardDown(object sender, Anarian.Events.KeyboardPressedEventArgs e)
        {
            if (m_pausedState != GamePausedState.Unpaused) return;
            if (m_buildMenuManager.Active) return;

            switch (e.KeyClicked)
            {
                // Vertical
                case Keys.Up:
                case Keys.W: m_gameCamera.Move(e.GameTime, m_gameCamera.CameraRotation.Up); break;
                case Keys.Down:
                case Keys.S: m_gameCamera.Move(e.GameTime, -m_gameCamera.CameraRotation.Up); break;

                // Horizontal
                case Keys.Left:
                case Keys.A: m_gameCamera.Move(e.GameTime, -m_gameCamera.CameraRotation.Right); break;
                case Keys.Right:
                case Keys.D: m_gameCamera.Move(e.GameTime, m_gameCamera.CameraRotation.Right); break;

                // Zoom
                case Keys.PageUp:
                case Keys.Q: m_gameCamera.Move(e.GameTime, -m_gameCamera.CameraRotation.Forward); break;

                case Keys.PageDown:
                case Keys.E: m_gameCamera.Move(e.GameTime, m_gameCamera.CameraRotation.Forward); break;
            }
        }
        void Keyboard_KeyboardPressed(object sender, Anarian.Events.KeyboardPressedEventArgs e)
        {
            if (m_pausedState != GamePausedState.Unpaused) return;

            switch (e.KeyClicked)
            {
                case Keys.LeftControl: Debug.WriteLine("Camera Position: {0}, \n Camera Rotation: {1}", m_gameCamera.Position, m_gameCamera.CameraRotation); break;
                //case Keys.O: m_buildMenuManager.Active = !m_buildMenuManager.Active; break;
            }
        }
        #endregion

        public void HandleInput(GameTime gameTime)
        {
            #region First thing we do is cull out old stuck pointers
            if (m_activePointerClickedEventsThisFrame.Count >= 2)
            {
                // If the first ID + 5 is less than the second pointer, we can assume the touch is stuck and we can safely ignore it
                if ((m_activePointerClickedEventsThisFrame[0].ID + 5) < m_activePointerClickedEventsThisFrame[1].ID)
                {
                    // Ignore 0 as thats mouse and we can't stop it
                    if (m_activePointerClickedEventsThisFrame[0].ID != 0)
                        ignorePointerIDs.Add(m_activePointerClickedEventsThisFrame[0].ID);

                    // If its not the mouse though, parse out the old ID
                    m_activePointerClickedEventsThisFrame.RemoveAt(0);
                }
            }

            if (m_activePointerEventsThisFrame.Count >= 2)
            {
                // If the first ID + 5 is less than the second pointer, we can assume the touch is stuck and we can safely ignore it
                if ((m_activePointerEventsThisFrame[0].ID + 5) < m_activePointerEventsThisFrame[1].ID)
                {
                    // Ignore 0 as thats mouse and we can't stop it
                    if (m_activePointerEventsThisFrame[0].ID != 0)
                        ignorePointerIDs.Add(m_activePointerEventsThisFrame[0].ID);

                    // If its not the mouse though, parse out the old ID
                    m_activePointerEventsThisFrame.RemoveAt(0);
                }
            }
            #endregion

            if (m_buildMenuManager.Active)
            {
                #region Check Build Manager Code
                if (m_activePointerClickedEventsThisFrame.Count > 0)
                {
                    var pointer = m_activePointerClickedEventsThisFrame[0];
                    bool clickOnMenu = false;

                    if (pointer.Pointer == PointerPress.Touch ||
                        pointer.Pointer == PointerPress.LeftMouseButton)
                    {
                        clickOnMenu = Input_BuildMenu(pointer);
                    }

                    if (!clickOnMenu)
                        m_buildMenuManager.Disable();
                }
                #endregion
            }
            else
            {
                // Since Mouse Screen-Edge movement doesn't need a specific mode, we do it here
                #region Mouse Movement Camera Controls
#if WINDOWS_APP
                var previousCamY = m_gameCamera.Position.Y;
                if (m_lastPointerMovedEventArgs.InputType == InputType.Mouse)
                {
                    if (!middleMouseDown)
                    {
                        var deltaPos = m_lastPointerMovedEventArgs.DeltaPosition;

                        if (m_lastPointerMovedEventArgs.Position.X <= (AnarianConsts.ScreenRectangle.X + m_cameraMovementScreenBuffer))
                        {
                            m_gameCamera.Move(gameTime, -m_gameCamera.CameraRotation.Right);
                        }
                        else if (m_lastPointerMovedEventArgs.Position.X >= (AnarianConsts.ScreenRectangle.Width - m_cameraMovementScreenBuffer))
                        {
                            m_gameCamera.Move(gameTime, m_gameCamera.CameraRotation.Right);
                        }

                        if (m_lastPointerMovedEventArgs.Position.Y <= (AnarianConsts.ScreenRectangle.Y + m_cameraMovementScreenBuffer))
                        {
                            m_gameCamera.Move(gameTime, m_gameCamera.CameraRotation.Up);
                        }
                        else if (m_lastPointerMovedEventArgs.Position.Y >= (AnarianConsts.ScreenRectangle.Height - m_cameraMovementScreenBuffer))
                        {
                            m_gameCamera.Move(gameTime, -m_gameCamera.CameraRotation.Up);
                        }

                        var camPos = m_gameCamera.Position;
                        camPos.Y = previousCamY;
                        m_gameCamera.Position = camPos;

                        var mouseWheelDelta = m_game.InputManager.Mouse.GetMouseWheelDelta();

                        if (mouseWheelDelta > 0)
                        {
                            m_gameCamera.Move(gameTime, m_gameCamera.CameraRotation.Forward * 2.0f);
                        }
                        else if (mouseWheelDelta < 0)
                        {
                            m_gameCamera.Move(gameTime, -m_gameCamera.CameraRotation.Forward * 2.0f);
                        }
                    }
                }
#endif
                #endregion

                bool skipInputCode = false;
                #region UI: Check If Input Mode Changed
                #region Enable And Disable Dynamic Buttons
                // If any units are currently selected, we turn on the issue command button
                if (m_unitPool.AreAnyUnitsCurrentlySelected)
                    m_guiIssueCommandButton.Active = true;
                else
                    m_guiIssueCommandButton.Active = false;
                #endregion

                if (m_activePointerClickedEventsThisFrame.Count > 0)
                {
                    var pointer = m_activePointerClickedEventsThisFrame[0];

                    if (m_guiGestureButton.Intersects(pointer.Position))
                    {
                        if (m_inputMode != InputMode.Gesture)
                        {
                            m_inputMode = InputMode.Gesture;
                            skipInputCode = true;
                        }
                    }
                    else if (m_guiSelectionButton.Intersects(pointer.Position))
                    {
                        if (m_inputMode != InputMode.Selection)
                        {
                            m_inputMode = InputMode.Selection;
                            skipInputCode = true;
                        }
                    }
                    else if (m_guiCameraPanButton.Intersects(pointer.Position))
                    {
                        if (m_inputMode != InputMode.CameraPan)
                        {
                            m_inputMode = InputMode.CameraPan;
                            skipInputCode = true;
                        }
                    }
                    else if (m_guiIssueCommandButton.Intersects(pointer.Position))
                    {
                        if (m_inputMode != InputMode.IssueCommand)
                        {
                            m_inputMode = InputMode.IssueCommand;
                            skipInputCode = true;
                        }
                    }
                }

                if (m_activePointerEventsThisFrame.Count > 0)
                {
                    if (m_activePointerEventsThisFrame[0].Position.X < m_guiDistanceFromSide)
                    {
                        skipInputCode = true;
                    }
                }
                #endregion

                if (!skipInputCode)
                {
                    #region Inputs
                    #region Gesture Mode
                    if (m_inputMode == InputMode.Gesture)
                    {
                        bool skipSelection = false;

                        // Do Input Which only operates when the active pointers are clicked
                        if (m_activePointerClickedEventsThisFrame.Count > 0)
                        {
                            #region Issue Command
                            if ((m_activePointerClickedEventsThisFrame.Count == 1 && m_activePointerClickedEventsThisFrame[0].Pointer == PointerPress.Touch) ||
                                m_activePointerClickedEventsThisFrame[0].Pointer == PointerPress.RightMouseButton ||
                                m_activePointerClickedEventsThisFrame[0].Pointer == PointerPress.LeftMouseButton)
                            {
                                PointerPressedEventArgs e;
                                e = m_activePointerClickedEventsThisFrame[0];

                                if (m_activePointerClickedEventsThisFrame[0].Pointer == PointerPress.RightMouseButton)
                                {
                                    skipSelection = Input_IssueCommand(e);
                                }
                                else if (m_activePointerClickedEventsThisFrame[0].Pointer == PointerPress.LeftMouseButton)
                                {
                                    if (m_selectionManager.MinimumSelection.Contains(e.Position))
                                    {
                                        skipSelection = Input_SelectSingleUnitOrFactory(e);
                                    }
                                }
                                else // Touch
                                {
                                    if (m_selectionManager.MinimumSelection.Contains(e.Position))
                                    {
                                        // Since Touch will handle both the selection of a single unit or factory
                                        // We will run the code to try selecting a single one
                                        skipSelection = Input_SelectSingleUnitOrFactory(e);

                                        // If nothing is discovered, then we go direcly to Command Mode
                                        if (!skipSelection)
                                            skipSelection = Input_IssueCommand(e);
                                    }
                                }
                            }
                            #endregion
                        }

                        // Do Input which operates when the active pointers are currently down
                        if (m_activePointerEventsThisFrame.Count > 0)
                        {
                            #region Pointer Down Camera Movement
                            if (m_activePointerEventsThisFrame.Count == 2 ||
                                m_activePointerEventsThisFrame[0].Pointer == PointerPress.MiddleMouseButton)
                            {
                                PointerPressedEventArgs e;
                                if (m_activePointerEventsThisFrame[0].Pointer == PointerPress.MiddleMouseButton)
                                    e = m_activePointerEventsThisFrame[0];
                                else
                                    e = m_activePointerEventsThisFrame[1];

                                Input_PanCamera(e);
                            }
                            #endregion

                            #region Selection
                            if (!skipSelection)
                            {
                                if ((m_activePointerEventsThisFrame.Count == 1 && m_activePointerEventsThisFrame[0].Pointer == PointerPress.Touch) ||
                                    m_activePointerEventsThisFrame[0].Pointer == PointerPress.LeftMouseButton)
                                {
                                    PointerPressedEventArgs e;
                                    e = m_activePointerEventsThisFrame[0];

                                    Input_Selection(e);
                                }
                            }
                            #endregion
                        }
                    }
                    #endregion

                    #region Selection Only Mode
                    else if (m_inputMode == InputMode.Selection)
                    {
                        bool skipSelection = false;
                        if (m_activePointerClickedEventsThisFrame.Count > 0)
                        {
                            if ((m_activePointerClickedEventsThisFrame.Count == 1 && m_activePointerClickedEventsThisFrame[0].Pointer == PointerPress.Touch) ||
                                m_activePointerClickedEventsThisFrame[0].Pointer == PointerPress.LeftMouseButton)
                            {
                                PointerPressedEventArgs e;
                                e = m_activePointerClickedEventsThisFrame[0];

                                if (m_selectionManager.MinimumSelection.Contains(e.Position))
                                {
                                    skipSelection = Input_SelectSingleUnitOrFactory(e);
                                }
                            }
                        }


                        if (!skipSelection)
                        {
                            if (m_activePointerEventsThisFrame.Count > 0)
                                Input_Selection(m_activePointerEventsThisFrame[0]);
                        }
                    }
                    #endregion

                    else if (m_inputMode == InputMode.CameraPan)
                    {
                        if (m_activePointerEventsThisFrame.Count > 0)
                            Input_PanCamera(m_activePointerEventsThisFrame[0]);
                    }
                    else if (m_inputMode == InputMode.IssueCommand)
                    {
                        if (m_activePointerClickedEventsThisFrame.Count > 0)
                        {
                            Input_IssueCommand(m_activePointerClickedEventsThisFrame[0]);
                            m_inputMode = InputMode.Gesture;
                        }
                    }
                    #endregion
                }
            }

            // Since we are done with the touch input, we can clear the pointers
            m_activePointerEventsThisFrame.Clear();
            m_activePointerClickedEventsThisFrame.Clear();

            // Update the Camera and AudioListener
            m_gameCamera.Update(gameTime);
            m_audioListener.Position = m_gameCamera.Position;
            m_audioEmitter.Position = m_gameCamera.Position;
        }

        #region Standard Input Modes
        public bool Input_Selection(PointerPressedEventArgs e)
        {
            // If we're not in the game anymore, we can't issue any more commands
            if (!m_me.Alive) return false;

            if (!m_selectionManager.HasSelection)
                m_commandRelay.AddCommand(Command.StartSelectionCommand(e.Position), NetworkTrafficDirection.Local);
            m_commandRelay.AddCommand(Command.EndSelectionCommand(e.Position), NetworkTrafficDirection.Local);

            return true;
        }
        public bool Input_PanCamera(PointerPressedEventArgs e)
        {
            var delta = e.DeltaPosition;
            delta.Normalize();
            Vector2 deltaBuffer = new Vector2(0.2f, 0.2f);

            Vector3 xAccel;
            if (delta.X < 0 - deltaBuffer.X)
            {
                if (GameConsts.Settings.InverseCameraX) xAccel = m_gameCamera.CameraRotation.Right;
                else xAccel = -m_gameCamera.CameraRotation.Right;
                m_gameCamera.Move(e.GameTime, xAccel);
            }
            else if (delta.X > 0 + deltaBuffer.X)
            {
                if (GameConsts.Settings.InverseCameraX) xAccel = -m_gameCamera.CameraRotation.Right;
                else xAccel = m_gameCamera.CameraRotation.Right;
                m_gameCamera.Move(e.GameTime, xAccel);
            }

            Vector3 yAccel;
            if (delta.Y < 0 - deltaBuffer.Y)
            {
                if (GameConsts.Settings.InverseCameraY) yAccel = -m_gameCamera.CameraRotation.Up;
                else yAccel = m_gameCamera.CameraRotation.Up;
                m_gameCamera.Move(e.GameTime, yAccel);
            }
            else if (delta.Y > 0 + deltaBuffer.Y)
            {
                if (GameConsts.Settings.InverseCameraY) yAccel = m_gameCamera.CameraRotation.Up;
                else yAccel = -m_gameCamera.CameraRotation.Up;
                m_gameCamera.Move(e.GameTime, yAccel);
            }

            return true;
        }
        public bool Input_IssueCommand(PointerPressedEventArgs e)
        {
            // If we're not in the game anymore, we can't issue any more commands
            if (!m_me.Alive) return false;

            // Get where we clicked in worldspace
            Ray ray = m_gameCamera.GetMouseRay(
                e.Position,
                m_game.Graphics.GraphicsDevice.Viewport
            );

            #region Did we Click on Enemy Unit?
            for (int i = 0; i < m_unitPool.m_activeUnits.Count; i++)
            {
                if (m_unitPool.m_activeUnits[i].CheckRayIntersection(ray))
                {
                    // Check if it is an Enemy Unit
                    // If it is, issue the Attack Command
                    if (m_unitPool.m_activeUnits[i].PlayerID != m_me.ID) 
                    {                    
                    }
                    return true;
                }
            }
            #endregion

            #region Did we Click on Enemy Factory?
            // If a unit isn't intersected, then we check to see if we collided with a factoryBase
            FactoryBase intersectedFactoryBase = null;
            var factoryResult = m_map.IntersectFactoryBase(ray, out intersectedFactoryBase);
            if (factoryResult == FactoryBaseRayIntersection.Factory)
            {
                if (intersectedFactoryBase.PlayerID == m_me.ID)
                {
                    // Try to issue an attack command or move closer
                }
                return true;
            }
            #endregion

            #region Move Units
            // If we still haven't intersected, we finally move onto the terrain
            var terrainResult = m_map.IntersectTerrain(ray);

            // If we clicked on empty terrain...
            if (terrainResult.HasValue)
            {
                // Check if we are within bounds
                if (terrainResult.Value.Y < m_map.Terrain.HeightData.HighestHeight - 1)
                {
                    List<Unit> selectedUnits = m_unitPool.GetAllSelectedUnits();
                    if (selectedUnits.Count == 1)
                    {
                        m_commandRelay.AddCommand(Command.MoveCommand(selectedUnits[0].UnitID, terrainResult.Value), NetworkTrafficDirection.Outbound);
                    }
                    else if (selectedUnits.Count > 1)
                    {
                        // Get the Center of Mass from all selected units
                        // which will be used to move the units in their current formation
                        Vector3 centerOfMass = Vector3.Zero;
                        foreach (var unit in selectedUnits)
                        {
                            centerOfMass += unit.Transform.WorldPosition;
                        }
                        centerOfMass = centerOfMass / selectedUnits.Count;

                        // Then move all selected units
                        foreach (var unit in selectedUnits)
                        {
                            Vector3 difference = unit.Transform.Position - centerOfMass;
                            Vector3 movementPosition = terrainResult.Value + difference;
                            movementPosition = m_map.ResolveBounds(movementPosition);

                            float terrainHeightAtPosition = m_map.Terrain.GetHeightAtPoint(movementPosition);
                            if (terrainHeightAtPosition != float.MaxValue)
                            {
                                movementPosition.Y = terrainHeightAtPosition;
                                //else {
                                //    movementPosition = m_map.Terrain.NearestPoint(movementPosition);
                                //}

                                m_commandRelay.AddCommand(Command.MoveCommand(unit.UnitID, movementPosition), NetworkTrafficDirection.Outbound);
                            }
                        }
                    }
                }

                return true;
            }
            #endregion

            return false;
        }
        #endregion

        #region Special Input
        public bool Input_BuildMenu(PointerPressedEventArgs e)
        {
            var purchaseSlot = m_buildMenuManager.CheckPurchaseInput(e);

            if (purchaseSlot != BuildMenuPurchaseSlot.None)
            {
                bool purchasedSuccessfully = false;

                if (m_buildMenuManager.m_activeFactory.HasOwner)
                {
                    var unitID = m_buildMenuManager.PurchaseSlotToUnitID(purchaseSlot);
                    var unitType = GameFactory.UnitTypeFromUnitID(unitID);

                    if (m_map.IsUnitTypeBuildable(unitType))
                    {
                        var cost = GameFactory.CreateUnitCost(unitID);
                        if (m_me.Economy.SubtractCost(cost))
                        {
                            var unit = m_unitPool.FirstInactiveOfPlayer(m_me.ID);

                            if (unit != null)
                            {
                                // Randomize the Current Rallypoint
                                Vector3 movePosition = m_buildMenuManager.m_activeFactory.CurrentRallyPoint;
                                Vector3 randomizedPosition = new Vector3(Anarian.Particles.ParticleHelpers.RandomBetween(-5.0f, 5.0f),
                                                                         0.0f,
                                                                         Anarian.Particles.ParticleHelpers.RandomBetween(-5.0f, 5.0f));
                                movePosition += randomizedPosition;
                                movePosition = m_map.ResolveBounds(movePosition);
                                float moveHeight = m_map.Terrain.GetHeightAtPoint(movePosition);

                                if (moveHeight != float.MaxValue)
                                    movePosition.Y = moveHeight;
                                else
                                    movePosition = m_buildMenuManager.m_activeFactory.CurrentRallyPoint;

                                // Fire Off the Command
                                m_commandRelay.AddCommand(Command.BuildUnitCommand(unit.UnitID, unitID, m_buildMenuManager.m_activeFactory.FactoryBaseID, movePosition), NetworkTrafficDirection.Outbound);
                                purchasedSuccessfully = true;
                            }
                            // Something went wrong, so we refunded the cost
                            else
                                m_me.Economy.AddCost(cost);
                        }
                    }
                }
                else
                {
                    // Build a Factory
                    if (m_buildMenuManager.m_activeFactory.FactoryBuildTimer.Progress == ProgressStatus.Completed)
                    {
                        if (m_buildMenuManager.m_activeFactory != null)
                        {
                            var cost = GameFactory.CreateFactoryCost(m_me.EmpireType);

                            if (m_me.Economy.SubtractCost(cost))
                            {
                                m_commandRelay.AddCommand(Command.BuildFactoryCommand(m_buildMenuManager.m_activeFactory.FactoryBaseID, m_me.ID), NetworkTrafficDirection.Outbound);
                                purchasedSuccessfully = true;
                            }
                        }

                    }
                }

                if (!purchasedSuccessfully)
                {
                    m_game.AudioManager.PlaySoundEffect(SoundName.MenuError, 0.5f);
                }

                return true;
            }
            return false;
        }
        public bool Input_SelectSingleUnitOrFactory(PointerPressedEventArgs e)
        {
            // If we're not in the game anymore, we can't issue any more commands
            if (!m_me.Alive) return false;

            // Get where we clicked in worldspace
            Ray ray = m_gameCamera.GetMouseRay(
                e.Position,
                m_game.Graphics.GraphicsDevice.Viewport
            );

            // First we check if our ray intersects with a Unit
            foreach (var unit in m_unitPool.m_myActiveUnits)
            {
                if (unit.CheckRayIntersection(ray))
                {
                    if (unit.PlayerID == m_me.ID)
                    {
                        // Single Unit Selection
                        if (!unit.Selected)
                        {
                            foreach (var u in m_unitPool.m_myActiveUnits)
                            {
                                u.Selected = false;    
                            }

                            unit.Selected = true;
                            m_unitPool.AreAnyUnitsCurrentlySelected = true;
                            return true;
                        }
                    }
                }
            }

            FactoryBase intersectedFactoryBase = null;
            var result = m_map.IntersectFactoryBase(ray, out intersectedFactoryBase);

            if (result == FactoryBaseRayIntersection.FactoryBase)
            {
                if (intersectedFactoryBase.FactoryBuildTimer.Progress == ProgressStatus.Completed)
                {
                    // Enable UI To Build Factory
                    m_buildMenuManager.Enable(intersectedFactoryBase, BuildMenuType.BuildFactory);
                }
                else
                {
                    // The cooldown timer hasn't completed yet
                    m_game.AudioManager.PlaySoundEffect(SoundName.MenuError, 0.5f);
                }
                return true;
            }
            else if (result == FactoryBaseRayIntersection.Factory)
            {
                if (intersectedFactoryBase.PlayerID == m_me.ID)
                {
                    // Enable the UI To Build Unit
                    m_buildMenuManager.Enable(intersectedFactoryBase, BuildMenuType.BuildUnit);
                    return true;
                }
            }

            return false;
        }
        #endregion
        #endregion

        #region Update
        void IUpdatable.Update(GameTime gameTime) { Update(gameTime); }
        public override void Update(GameTime gameTime)
        {
            // Update the Network Timer so that we can continue to send out Network Tics
            m_networkTimer.Update(gameTime);

            // Check if the game is fully loaded
            if (m_currentLoadingProgress.Progress < 100) { base.Update(gameTime); return; }

            // Check if the game is paused
            switch (m_pausedState)
            {
                case GamePausedState.Paused: UpdatePaused(gameTime); base.Update(gameTime); return;
                case GamePausedState.WaitingForData: UpdateWaitingForData(gameTime); base.Update(gameTime); return;
                case GamePausedState.GameOver: m_gameOverTransition.Update(gameTime); break;
            }

            // Update our Input
            HandleInput(gameTime);

            // Then the Map
            m_map.Update(gameTime);

            // Then our Economy
            //m_pageParameter.me.Economy.Metal.Infinite = true;
            //m_pageParameter.me.Economy.Energy.Infinite = true;
            //m_pageParameter.me.Economy.Currency.Infinite = true;
            m_pageParameter.me.Update(gameTime);

            #region Do Commands
            foreach (var command in m_commandRelay.m_commands)
            {
                //Debug.WriteLine(command.ToString());
                switch (command.CommandType)
                {
                    #region Selection
                    case CommandType.StartSelection:
                        m_selectionManager.StartingPosition = new Vector2(command.Position.X, command.Position.Y);
                        m_commandRelay.Complete(command);
                        break;
                    case CommandType.EndSelection:
                        // First set the Ending Position
                        m_selectionManager.EndingPosition = new Vector2(command.Position.X, command.Position.Y);
                        m_commandRelay.Complete(command);

                        // If our selection is greater than the minimums required to select, then we will select
                        if (!m_selectionManager.MinimumSelection.Contains(m_selectionManager.EndingPosition.Value))
                        {
                            // Then select the Units
                            m_unitPool.AreAnyUnitsCurrentlySelected = false;

                            BoundingFrustum selectionFrustrum = m_gameCamera.UnprojectRectangle(m_selectionManager.GetSelection(), m_game.GraphicsDevice.Viewport);
                            foreach (var item in m_unitPool.m_myActiveUnits)
                            {
                                if (item.LifeState != GameObjectLifeState.Alive)
                                {
                                    item.Selected = false;
                                    continue;
                                }

                                if (item.PlayerID == m_me.ID &&
                                    item.CheckFrustumIntersection(selectionFrustrum))
                                {
                                    item.Selected = true;
                                    m_unitPool.AreAnyUnitsCurrentlySelected = true;
                                }
                                else
                                {
                                    item.Selected = false;
                                }
                            }
                        }
                        break;
                    #endregion

                    #region Move
                    case CommandType.Move:
                        Unit moveUnit = m_unitPool.FindUnit(PoolStatus.Active, command.ID1);
                        if (moveUnit != null)
                        {
                            if (moveUnit.LifeState != GameObjectLifeState.Alive)
                            {
                                moveUnit.IgnoreAttackRotation = false;
                                m_commandRelay.Complete(command);
                                continue;
                            }

                            bool smoothHeight = true;

                            var moveNewPos = (command.Position + new Vector3(0.0f, moveUnit.HeightAboveTerrain, 0.0f));
                            var moveResult = moveUnit.Transform.MoveToPosition(gameTime, moveNewPos, 1.2f);

                            if (moveResult)
                            {
                                if (moveUnit.CurrentAnimationPlayer != null)
                                    moveUnit.CurrentAnimationPlayer.Paused = true;
                                moveUnit.IgnoreAttackRotation = false;
                                m_commandRelay.Complete(command);
                                smoothHeight = false;
                            }
                            else
                            {
                                if (moveUnit.CurrentAnimationPlayer != null)
                                    moveUnit.CurrentAnimationPlayer.Paused = false;
                                moveUnit.IgnoreAttackRotation = true;
                            }

                            // Since this is the only spot where units will move
                            // We will set the unit to be on top of the terrain
                            float moveHeight = m_map.Terrain.GetHeightAtPoint(moveUnit.Transform.Position);
                            if (moveHeight != float.MaxValue)
                            {
                                Vector3 movePos = moveUnit.Transform.Position;
                                float targetHeight = moveHeight + moveUnit.HeightAboveTerrain;

                                if (smoothHeight)
                                {
                                    float currentHeight = MathHelper.SmoothStep(movePos.Y, targetHeight, (float)gameTime.ElapsedGameTime.TotalMilliseconds);
                                    movePos.Y = currentHeight;
                                }
                                else
                                {
                                    movePos.Y = targetHeight;
                                }
                                moveUnit.Transform.Position = movePos;
                            }
                        }
                        break;
                    #endregion
                    #region Attack
                    case CommandType.Attack:
                        bool attackInRange = false;

                        var attackingUnit = m_unitPool.FindUnit(PoolStatus.Active, command.ID1);
                        if (attackingUnit != null)
                        {
                            // Play Attacking Sound Effect and
                            int selection = Consts.random.Next((int)SoundName.SpaceGun06, (int)SoundName.SpaceGun09 + 1);
                            SoundName weaponSoundEffect = (SoundName)selection;
                            m_activeSoundEffectInstances.Add(AudioManager.Instance.Play3DSoundEffect(m_audioListener, attackingUnit.AudioEmitter, weaponSoundEffect));

                            Transform defendingTransform = null;

                            if (command.TargetType == TargetType.Unit)
                            {
                                var defendingUnit = m_unitPool.FindUnit(PoolStatus.Active, command.ID2);
                                if (defendingUnit != null)
                                {
                                    defendingTransform = defendingUnit.Transform;
                                }
                            }
                            else if (command.TargetType == TargetType.Factory)
                            {
                                var defendingBuilding = m_map.GetFactoryBase(command.ID2);
                                if (defendingBuilding != null)
                                {
                                    if (defendingBuilding.Base != null)
                                    {
                                        defendingTransform = defendingBuilding.Base.Transform;
                                    }
                                }
                            }

                            if (defendingTransform != null)
                            {
                                if (!attackingUnit.IgnoreAttackRotation)
                                {
                                    Vector3 direction = attackingUnit.Transform.WorldPosition - defendingTransform.WorldPosition;
                                    attackingUnit.Transform.RotateToPoint(gameTime, direction);
                                }

                                var bulletParticle = new BulletParticleSystem(Vector2.Zero, 1, Vector3.Zero);

                                Vector2 attackingProjected = m_gameCamera.ProjectToScreenCoordinates(attackingUnit.Transform.WorldPosition, m_game.GraphicsDevice.Viewport);
                                Vector2 defendingProjected = m_gameCamera.ProjectToScreenCoordinates(defendingTransform.WorldPosition, m_game.GraphicsDevice.Viewport);

                                bulletParticle.Position = attackingProjected;
                                bulletParticle.WorldPosition = attackingUnit.Transform.WorldPosition;
                                bulletParticle.TargetWorldPosition = defendingTransform.WorldPosition;

                                bulletParticle.DistanceLifespan.TargetPosition = defendingProjected;
                                bulletParticle.MoveToPositionModifier.TargetPosition = defendingProjected;

                                m_activeParticleEmitters.Add(bulletParticle);
                            }
                        }

                        // Since we are not in range, we can't complete this attack command
                        if (!attackInRange)
                        {
                            m_commandRelay.Complete(command);
                        }

                        break;
                    #endregion

                    #region Building Management
                    case CommandType.BuildFactory:
                        var buildFactory = m_map.GetFactoryBase(command.ID1);
                        if (buildFactory != null)
                        {
                            Player player = null;
                            if (m_team1.Exists(command.ID2))
                            {
                                player = m_team1[command.ID2];
                            }
                            else if (m_team2.Exists(command.ID2))
                            {
                                player = m_team2[command.ID2];
                            }

                            if (player != null)
                            {
                                GameFactory.CreateFactoryOnFactoryBase(buildFactory, player);
                                buildFactory.FactoryBuildTimer.Reset();
                                m_buildMenuManager.Disable();
                            }
                        }

                        m_commandRelay.Complete(command);
                        break;
                    case CommandType.BuildUnit:
                        Unit buildUnit;
                        bool buildUnitResult = m_unitPool.SwapPool(command.ID1, out buildUnit);
                        var buildUnitFactory = m_map.GetFactoryBase(command.ID2);

                        if (buildUnit != null)
                        {
                            if (buildUnitFactory.Factory != null)
                            {
                                // Create the Unit
                                GameFactory.CreateUnit(buildUnit, command.UnitID, buildUnitFactory.Factory.Transform.WorldPosition);

                                // Then have them move out to the Current Rally Point
                                m_commandRelay.AddCommand(Command.MoveCommand(buildUnit.UnitID, command.Position), NetworkTrafficDirection.Outbound);
                            }
                        }

                        m_commandRelay.Complete(command);
                        break;
                    case CommandType.SetFactoryRallyPoint:
                        break;
                    case CommandType.Cancel:
                        break;
                    #endregion

                    #region Damage/Kill
                    case CommandType.Damage:
                        if (command.TargetType == TargetType.Factory)
                        {
                            var damageFactoryBase = m_map.GetFactoryBase(command.ID1);
                            if (damageFactoryBase != null)
                            {
                                if (damageFactoryBase.Factory != null)
                                {
                                    damageFactoryBase.TakeDamage((float)command.Damage); //damageFactoryBase.Factory.Health.DecreaseHealth((float)command.Damage);
                                }
                            }
                        }
                        else if (command.TargetType == TargetType.Unit)
                        {
                            var damageUnit = m_unitPool.FindUnit(PoolStatus.Active, command.ID1);
                            if (damageUnit != null)
                            {
                                damageUnit.TakeDamage((float)command.Damage); //damageUnit.Health.DecreaseHealth((float)command.Damage);
                                //Debug.WriteLine("Unit {0} took {1} Damage and has {2} Health Left", damageUnit.UnitID, command.Damage, damageUnit.Health.CurrentHealth);
                            }
                        }

                        m_commandRelay.Complete(command);
                        break;
                    case CommandType.Kill:
                        bool killSafeToComplete = false;

                        #region Kill Factory
                        if (command.TargetType == TargetType.Factory)
                        {
                            var killFactoryBase = m_map.GetFactoryBase(command.ID1);
                            if (killFactoryBase != null)
                            {
                                // Play Explosion Sound Effect
                                if (killFactoryBase.Factory != null)
                                {
                                    if (killFactoryBase.Factory.LifeState == GameObjectLifeState.Alive)
                                    {
                                        m_activeSoundEffectInstances.Add(AudioManager.Instance.Play3DSoundEffect(m_audioListener, killFactoryBase.FactoryBaseAudioEmitter, SoundName.BuildingExplosion));
                                        killFactoryBase.Factory.LifeState = GameObjectLifeState.Dying;
                                    }

                                    #region Kill Code
                                    if (killFactoryBase.Factory.LifeState == GameObjectLifeState.Dead)
                                    {
                                        if (m_buildMenuManager.m_activeFactory != null)
                                        {
                                            if (m_buildMenuManager.m_activeFactory.FactoryBaseID == killFactoryBase.FactoryBaseID)
                                                m_buildMenuManager.Disable();
                                        }

                                        uint killPreviousOwner = killFactoryBase.PlayerID;

                                        // Disable the Smoke Emitter so that it will allow itself to complete
                                        killFactoryBase.Factory.SmokePlumeParticleEmitter.EmissionSettings.Active = false;
                                        m_activeParticleEmitters.Add(killFactoryBase.Factory.SmokePlumeParticleEmitter);

                                        killFactoryBase.PlayerID = uint.MaxValue;
                                        killFactoryBase.Factory = null;

                                        // Now we check if the other player still has a Factory left
                                        bool foundOtherFactoryForPlayer = false;
                                        foreach (var factory in m_map.FactoryBases)
                                        {
                                            if (factory.PlayerID == killPreviousOwner)
                                            {
                                                foundOtherFactoryForPlayer = true;
                                                break;
                                            }
                                        }

                                        if (!foundOtherFactoryForPlayer)
                                        {
                                            // Uh-oh, that was the players last factory!
                                            if (m_team1.Exists(killPreviousOwner))
                                            {
                                                var killPlayer = m_team1.GetPlayer(killPreviousOwner);
                                                killPlayer.Alive = false;
                                            }
                                            else if (m_team2.Exists(killPreviousOwner))
                                            {
                                                var killPlayer = m_team2.GetPlayer(killPreviousOwner);
                                                killPlayer.Alive = false;
                                            }
                                        }

                                        killSafeToComplete = true;
                                    }
                                    #endregion
                                }
                            }
                        }
                        #endregion

                        #region Kill Unit
                        else if (command.TargetType == TargetType.Unit)
                        {
                            var killUnit = m_unitPool.FindUnit(PoolStatus.Active, command.ID1);
                            if (killUnit != null)
                            {
                                if (killUnit.LifeState == GameObjectLifeState.Alive)
                                {
                                    m_activeSoundEffectInstances.Add(AudioManager.Instance.Play3DSoundEffect(m_audioListener, killUnit.AudioEmitter, GameFactory.SoundNameFromUnitID(killUnit.UnitName)));
                                    killUnit.LifeState = GameObjectLifeState.Dying;
                                }

                                if (killUnit.LifeState == GameObjectLifeState.Dead)
                                {
                                    if (killUnit.PlayerID == m_me.ID)
                                    {
                                        m_me.Economy.AddCost(killUnit.UnitCost.OnlyUnitCost());
                                    }

                                    Unit u;
                                    m_unitPool.SwapPool(command.ID1, out u);

                                    killSafeToComplete = true;
                                }
                            }
                        }
                        #endregion

                        if (killSafeToComplete)
                            m_commandRelay.Complete(command);
                        break;
                    #endregion

                    default:
                        break;
                }
            }
            #endregion

            // If the Selection was Released, reset the Selection Manager
            if (selectionReleased)
            {
                selectionReleased = false;
                m_selectionManager.Deselect();
            }

            #region Remove All Completed Instances of Objects
            for (int i = m_activeSoundEffectInstances.Count - 1; i > -1; i--)
            {
                if (m_activeSoundEffectInstances[i].State != SoundState.Playing)
                {
                    m_activeSoundEffectInstances[i].Dispose();
                    m_activeSoundEffectInstances.RemoveAt(i);
                }
            }
            for (int i = m_activeParticleEmitters.Count - 1; i > -1; i-- )
            {
                if (m_activeParticleEmitters[i].Progress == ProgressStatus.Completed)
                {
                    m_activeParticleEmitters.RemoveAt(i);
                }
                else
                {
                    m_activeParticleEmitters[i].Update(gameTime);
                }
            }
            #endregion

            // Update all the Active Units
            m_unitPool.Update(gameTime);
            m_unitPool.GetAllMyActiveUnits(m_me.ID);

            // Update the GUI
            m_buildMenuManager.Update(gameTime);

            // Run Game Logic
            UpdateGame(gameTime);

            #region Singleplayer and Host Only Processing
            if (m_pageParameter.GameConnectionType == GameConnectionType.Singleplayer ||
                m_networkManager.HostSettings == KillerrinStudiosToolkit.Enumerators.HostType.Host)
            { 
            }
            else
            {
            }
            #endregion

            // Lastly, Aggregate all the commands to the current Command Pool, then Send all Outgoing
            m_commandRelay.AggregateAndSendCommands();

            //-- Update the Menu
            base.Update(gameTime);
        }

        private void UpdateGame(GameTime gameTime)
        {
            if (m_pausedState != GamePausedState.Unpaused) return;

            #region Parse all Units
            bool breakUnit1 = false;
            foreach (var unit1 in m_unitPool.m_activeUnits)
            {
                if (unit1.LifeState != GameObjectLifeState.Alive) continue;
                float unitSightChangedBy = 0.0f;

                bool unitFoundInRange = false;
                foreach(var unit2 in m_unitPool.m_activeUnits)
                {
                    if (unit2.LifeState != GameObjectLifeState.Alive) continue;
                    if (unit1.UnitID == unit2.UnitID) continue;
                    if (unit1.PlayerID == unit2.PlayerID) continue;

                    // If Our Unit is higher than the unit we are checking, then we increase our line of sight
                    if (unit1.Transform.WorldPosition.Y > unit2.Transform.WorldPosition.Y)
                    {
                        var difference = unit1.Transform.WorldPosition.Y - unit2.Transform.WorldPosition.Y;
                        unitSightChangedBy = difference * m_unitSightHeightIncrease;
                        unit1.SightRange.Radius += unitSightChangedBy;
                    }

                    if (unit2.CheckSphereIntersection(unit1.SightRange))
                    {
                        if (unit1.Attack())
                        {
                            //Debug.WriteLine("Unit {0} is attacking Unit {1}", unit1.UnitID, unit2.UnitID);
                            m_commandRelay.AddCommand(Command.AttackCommand(unit1.UnitID, unit2.UnitID, TargetType.Unit), NetworkTrafficDirection.Local);
                            unit2.DamageTakenThisFrame += unit1.AttackDamage;
                        }

                        unitFoundInRange = true;
                    }

                    // Subtract the Unit Sight back to zero
                    unit1.SightRange.Radius -= unitSightChangedBy;
                    unitSightChangedBy = 0.0f;

                    // Check for exit
                    if (unitFoundInRange) break;
                }

                if (!unitFoundInRange)
                {
                    bool factoryFoundInRange = false;
                    foreach (var factory in m_map.FactoryBases)
                    {
                        if (!factory.HasOwner) continue;
                        if (unit1.PlayerID == factory.PlayerID) continue;
                        if (factory.Factory.LifeState != GameObjectLifeState.Alive) continue;

                        if (m_team1.Exists(unit1.PlayerID))
                        {
                            if (!m_team1[unit1.PlayerID].Alive)
                                continue;
                        }
                        else if (m_team2.Exists(unit1.PlayerID))
                        {
                            if (!m_team2[unit1.PlayerID].Alive)
                                continue;
                        }

                        // If Our Unit is higher than the factory we are checking, then we increase our line of sight
                        if (unit1.Transform.WorldPosition.Y > factory.Factory.Transform.WorldPosition.Y)
                        {
                            var difference = unit1.Transform.WorldPosition.Y - factory.Factory.Transform.WorldPosition.Y;
                            unitSightChangedBy = difference * m_unitSightHeightIncrease;
                            unit1.SightRange.Radius += unitSightChangedBy;
                        }

                        if (factory.CheckSphereIntersection(unit1.SightRange) == FactoryBaseRayIntersection.Factory)
                        {
                            // Unit Is automatically attacking Factory
                            //Debug.WriteLine("Unit {0} is attacking Factory {1}", unit1.UnitID, factory.FactoryBaseID);

                            if (unit1.Attack())
                            {
                                m_commandRelay.AddCommand(Command.AttackCommand(unit1.UnitID, factory.FactoryBaseID, TargetType.Factory), NetworkTrafficDirection.Local);
                                factory.DamageTakenThisFrame += unit1.AttackDamage;
                            }

                            factoryFoundInRange = true;
                        }

                        // Subtract the Unit Sight back to zero
                        unit1.SightRange.Radius -= unitSightChangedBy;
                        unitSightChangedBy = 0.0f;

                        if (factoryFoundInRange) break;
                    }
                }

                if (breakUnit1) break;
            }
            #endregion

            #region Damage/Kill Units/Factories
            foreach (var unit in m_unitPool.m_activeUnits)
            {
                if (unit.LifeState != GameObjectLifeState.Alive) continue;

                if (!unit.Health.Alive)
                {
                    if (m_pageParameter.GameConnectionType == GameConnectionType.Singleplayer ||
                        m_networkManager.HostSettings == KillerrinStudiosToolkit.Enumerators.HostType.Host)
                    {
                        m_commandRelay.AddCommand(Command.KillCommand(unit.UnitID, TargetType.Unit), NetworkTrafficDirection.Outbound);
                    }
                }
                else
                {
                    if (unit.DamageTakenThisFrame > 0.0)
                    {
                        m_commandRelay.AddCommand(Command.DamageCommand(unit.UnitID, TargetType.Unit, unit.DamageTakenThisFrame), NetworkTrafficDirection.Local);
                        unit.DamageTakenThisFrame = 0.0;
                    }
                }
            }
            foreach (var factory in m_map.FactoryBases)
            {
                if (!factory.HasOwner) continue;
                if (factory.Factory.LifeState != GameObjectLifeState.Alive) continue;

                if (!factory.Factory.Health.Alive) {
                    if (m_pageParameter.GameConnectionType == GameConnectionType.Singleplayer ||
                        m_networkManager.HostSettings == KillerrinStudiosToolkit.Enumerators.HostType.Host)
                    {
                        m_commandRelay.AddCommand(Command.KillCommand(factory.FactoryBaseID, TargetType.Factory), NetworkTrafficDirection.Outbound);
                    }
                }
                else
                {
                    if (factory.DamageTakenThisFrame > 0.0)
                    {
                        m_commandRelay.AddCommand(Command.DamageCommand(factory.FactoryBaseID, TargetType.Factory, factory.DamageTakenThisFrame), NetworkTrafficDirection.Local);
                        factory.DamageTakenThisFrame = 0.0;
                    }
                }
            }
            #endregion

            #region Check if a Team has Won yet
            m_team1.Dead = true;
            foreach (var player in m_team1.Players)
            {
                if (player.Alive)
                    m_team1.Dead = false;
            }

            m_team2.Dead = true;
            foreach (var player in m_team2.Players)
            {
                if (player.Alive)
                    m_team2.Dead = false;
            }

            // Set the Winners
            if (m_team1.Dead && m_team2.Dead)
            {
                m_team1.Winner = true;
                m_team2.Winner = true;
            }
            else if (m_team1.Dead && !m_team2.Dead)
            {
                m_team1.Winner = false;
                m_team2.Winner = true;
            }
            else if (!m_team1.Dead && m_team2.Dead)
            {
                m_team1.Winner = true;
                m_team2.Winner = false;
            }

            if (m_team1.Dead ||
                m_team2.Dead)
            {
                m_pausedState = GamePausedState.GameOver;
            }
            #endregion
        }

        #region Mini Menus
        private void UpdatePaused(GameTime gameTime)
        {
            m_overlay.ApplyEffect(gameTime);
        }

        private void UpdateWaitingForData(GameTime gameTime)
        {
            m_overlay.ApplyEffect(gameTime);

            if (m_networkManager.HostSettings == KillerrinStudiosToolkit.Enumerators.HostType.Host)
            {
                if (waitingForDataState == WaitingForDataState.WaitingForLoading)
                {
                    lock (opponentloadedLockObject)
                    {
                        // Send Ready to begin
                        if (opponentFullyLoaded)
                        {
                            SystemPacket sp = new SystemPacket(true, SystemPacketID.GameBegin, "");
                            m_networkManager.SendMessage(sp.ThisToJson());
                            m_pausedState = GamePausedState.Unpaused;
                            waitingForDataState = WaitingForDataState.InGame;
                        }
                    }
                }
            }
        }
        #endregion
        #endregion

        #region Draw
        void IRenderable.Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics, ICamera camera) { Draw(gameTime, spriteBatch, graphics); }
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics)
        {
            graphics.Clear(ClearOptions.Target | ClearOptions.DepthBuffer, Color.Black, 1.0f, 0);

            if (m_currentLoadingProgress.Progress < 100) { 
                DrawLoadingMenu(gameTime, spriteBatch, graphics);
                base.Draw(gameTime, spriteBatch, graphics); 
                return;
            }

            #region BuildMenu Render Target
            RenderTarget2D buildMenu3DModelsRenderTarget = null;
            if (m_buildMenuManager.Active)
            {
                buildMenu3DModelsRenderTarget = new RenderTarget2D(graphics, m_buildMenuManager.m_uiRectBackground.Width, m_buildMenuManager.m_uiRectBackground.Height);
                graphics.SetRenderTarget(buildMenu3DModelsRenderTarget);
                graphics.Clear(Color.Transparent);
                m_buildMenuManager.Draw3DModels(gameTime, spriteBatch, graphics, m_gameCamera);
                graphics.SetRenderTarget(null);
            }
            #endregion

            DrawScene(gameTime, spriteBatch, graphics, false);
            DrawGUI(gameTime, spriteBatch, graphics, ref buildMenu3DModelsRenderTarget, false);

            switch (m_pausedState)
            {
                case GamePausedState.Paused:            DrawPaused(gameTime, spriteBatch, graphics);           break;                             
                case GamePausedState.WaitingForData:    DrawWaitingForData(gameTime, spriteBatch, graphics);   break;
            }

            base.Draw(gameTime, spriteBatch, graphics);
        }

        public bool DrawScene(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics, bool creatingShadowMap)
        {
            // Draw The Map
            m_map.Draw(gameTime, spriteBatch, graphics, m_gameCamera, creatingShadowMap);

            // Draw the Units
            m_unitPool.Draw(gameTime, spriteBatch, graphics, m_gameCamera, creatingShadowMap);

            if (!creatingShadowMap)
            {
            }

            return true;
        }

        public bool DrawGUI(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics, ref RenderTarget2D buildMenu3DModelsRenderTarget, bool creatingShadowMap)
        {
            //-- Render before the GUI
            // Do conditional rendering which doens't use the Shadow Map
            if (!creatingShadowMap)
            {
                foreach (var particleEffect in m_activeParticleEmitters)
                {
                    particleEffect.Draw(gameTime, spriteBatch, graphics, m_gameCamera);
                }
            }

            //-- Now Render GUI
            // Draw SelectionBox
            m_selectionManager.Draw(gameTime, spriteBatch, graphics, m_gameCamera);

            spriteBatch.Begin();
            // Input Forbidden Zone Area
            spriteBatch.Draw(m_blankTexture, new Rectangle(0, 0, m_guiDistanceFromSide, AnarianConsts.ScreenRectangle.Height), m_guiZoneColor);

            // Selected GUI Item
            switch (m_inputMode)
            {
                case InputMode.Gesture: spriteBatch.Draw(m_blankTexture, m_guiGestureButton.Position, Color.Black * 0.5f); break;
                case InputMode.Selection: spriteBatch.Draw(m_blankTexture, m_guiSelectionButton.Position, Color.Black * 0.5f); break;
                case InputMode.CameraPan: spriteBatch.Draw(m_blankTexture, m_guiCameraPanButton.Position, Color.Black * 0.5f); break;
                case InputMode.IssueCommand: spriteBatch.Draw(m_blankTexture, m_guiIssueCommandButton.Position, Color.Black * 0.5f); break;
            }

            spriteBatch.End();

            // Individual GUI Buttons
            m_guiGestureButton.Draw(gameTime, spriteBatch, graphics, m_gameCamera);
            m_guiSelectionButton.Draw(gameTime, spriteBatch, graphics, m_gameCamera);
            m_guiCameraPanButton.Draw(gameTime, spriteBatch, graphics, m_gameCamera);
            m_guiIssueCommandButton.Draw(gameTime, spriteBatch, graphics, m_gameCamera);

            if (m_buildMenuManager.Active)
            {
                m_buildMenuManager.Draw(gameTime, spriteBatch, graphics, m_gameCamera);

                spriteBatch.Begin();
                spriteBatch.Draw((Texture2D)buildMenu3DModelsRenderTarget, m_buildMenuManager.m_uiRectBackground, Color.White);
                spriteBatch.End();

                // Now, dispose of the render target to get the memory back
                buildMenu3DModelsRenderTarget.Dispose();
            }

            #region Player Economy
            {
                int distanceBetweenElements = 160;
                int xOffset = (AnarianConsts.ScreenRectangle.Width) - distanceBetweenElements;
                int yOffset = 25;

                spriteBatch.Begin();
                spriteBatch.Draw(m_unitCapTexture, new Rectangle(xOffset, yOffset, 50, 50), Color.White);
                spriteBatch.DrawString(m_empiresOfTheIVFontSmall, m_pageParameter.me.Economy.UnitCap.CurrentAmountAsString, new Vector2(xOffset + 50, yOffset), Color.White);
                xOffset -= distanceBetweenElements;

                spriteBatch.Draw(m_energyTexture, new Rectangle(xOffset, yOffset, 50, 50), Color.White);
                spriteBatch.DrawString(m_empiresOfTheIVFontSmall, m_pageParameter.me.Economy.Energy.CurrentAmountAsString, new Vector2(xOffset + 50, yOffset), Color.White);
                xOffset -= distanceBetweenElements;

                spriteBatch.Draw(m_metalTexture, new Rectangle(xOffset, yOffset, 50, 50), Color.White);
                spriteBatch.DrawString(m_empiresOfTheIVFontSmall, m_pageParameter.me.Economy.Metal.CurrentAmountAsString, new Vector2(xOffset + 50, yOffset), Color.White);
                xOffset -= distanceBetweenElements;

                spriteBatch.Draw(m_currencyTexture, new Rectangle(xOffset, yOffset, 50, 50), Color.White);
                spriteBatch.DrawString(m_empiresOfTheIVFontSmall, m_pageParameter.me.Economy.Currency.CurrentAmountAsString, new Vector2(xOffset + 50, yOffset), Color.White);
                spriteBatch.End();
            }
            #endregion

            return true;
        }

        #region Mini Menus
        public void DrawLoadingMenu(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics)
        {
            //graphics.Clear(Color.Black);
            
            if (m_loadingMiniMap != null)
            {
                spriteBatch.Begin();
                spriteBatch.Draw(m_loadingMiniMap,
                                new Rectangle(0, 0, AnarianConsts.ScreenRectangle.Width, (int)(AnarianConsts.ScreenRectangle.Height * 0.80)),
                                Color.White);
                spriteBatch.End();
            }

            // Loading Outline
            var outlineRect = new Rectangle(0,
                                      (int)(AnarianConsts.ScreenRectangle.Height * 0.75),
                                            AnarianConsts.ScreenRectangle.Width,
                                      (int)(AnarianConsts.ScreenRectangle.Height * 0.10)); 
            PrimitiveHelper2D.DrawRect(spriteBatch, Color.Wheat, outlineRect);

            // Loading Bar
            lock (loadinglockObject)
            {
                int distanceFromTopBottom = 5;
                int distanceFromLeftRight = 0;
                var loadingBar = new Rectangle(0 + distanceFromLeftRight,
                                               outlineRect.Y + distanceFromTopBottom,
                                               m_currentLoadingProgress.Progress * ((outlineRect.Width - (distanceFromLeftRight * 2)) / 100),
                                               outlineRect.Height - (distanceFromTopBottom * 2));
                PrimitiveHelper2D.DrawRect(spriteBatch, Color.ForestGreen, loadingBar);

                // Text
                Vector2 loadingSize = m_empiresOfTheIVFont.MeasureString("Loading");

                spriteBatch.Begin();
                spriteBatch.DrawString(m_empiresOfTheIVFont, "Loading", new Vector2(centerOfScreen.X - (loadingSize.X * 0.3f), AnarianConsts.ScreenRectangle.Height * 0.15f), Color.Wheat);

                spriteBatch.DrawString(m_empiresOfTheIVFont, m_currentLoadingProgress.Status, new Vector2(25, outlineRect.Y - 50), Color.Wheat);
                spriteBatch.DrawString(m_empiresOfTheIVFont, m_currentLoadingProgress.Progress + "%", new Vector2(outlineRect.Width - 100, outlineRect.Y - 50), Color.Wheat);
                spriteBatch.End();
            }
        }

        public void DrawPaused(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics)
        {
            m_overlay.Draw(gameTime, spriteBatch);

            Vector2 pausedTextSize = m_empiresOfTheIVFont.MeasureString("Paused");

            spriteBatch.Begin();
            spriteBatch.DrawString(m_empiresOfTheIVFont, "Paused", new Vector2(centerOfScreen.X - (pausedTextSize.X * 0.3f), AnarianConsts.ScreenRectangle.Height * 0.15f), Color.Wheat);
            spriteBatch.End();
        }

        public void DrawWaitingForData(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics)
        {
            m_overlay.Draw(gameTime, spriteBatch);

            Vector2 waitingTextSize = m_empiresOfTheIVFont.MeasureString("Waiting for Data");

            spriteBatch.Begin();
            spriteBatch.DrawString(m_empiresOfTheIVFont, "Waiting for Data", new Vector2(centerOfScreen.X - (waitingTextSize.X * 0.3f), AnarianConsts.ScreenRectangle.Height * 0.15f), Color.Wheat);
            spriteBatch.End();
        }
        #endregion
        #endregion
    }
}
