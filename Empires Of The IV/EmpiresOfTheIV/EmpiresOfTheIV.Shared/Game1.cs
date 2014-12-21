﻿using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using KillerrinStudiosToolkit;

using Anarian;
using Anarian.DataStructures;
using Anarian.DataStructures.Animation;
using Anarian.DataStructures.Animation.Aux;
//using AnimationAux;
using Anarian.Enumerators;
using Anarian.DataStructures.Input;
using Anarian.DataStructures.Rendering;
using Anarian.Helpers;
using Anarian.Interfaces;

namespace EmpiresOfTheIV
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : AnarianGameEngine
    {
        /// <summary>
        /// The animated model we are displaying
        /// </summary>
        private AnimatedGameObject dance = null;

        //Terrain
        Terrain m_terrain;

        public Game1()
            :base()
        {
            Content.RootDirectory = "Content";  
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            graphics.SupportedOrientations = DisplayOrientation.LandscapeLeft | DisplayOrientation.LandscapeRight;

#if WINDOWS_APP
            this.IsMouseVisible = true;
#endif

            // Subscribe to our Events
            // Mouse
            m_inputManager.Mouse.MouseDown += Mouse_MouseDown;
            m_inputManager.Mouse.MouseClicked += Mouse_MouseClicked;
            m_inputManager.Mouse.MouseMoved += Mouse_MouseMoved;
            // Keybaord
            m_inputManager.Keyboard.KeyboardDown += Keyboard_KeyboardDown;
            m_inputManager.Keyboard.KeyboardPressed += Keyboard_KeyboardPressed;
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            base.LoadContent();

            // Load the Assets
            m_resourceManager.LoadTexture(Content, "KillerrinStudiosLogo");
            m_resourceManager.LoadModel(Content, "t-pose_3_t");
            
            // Create the Game Objects
            StaticGameObject armyGuy = new StaticGameObject();
            armyGuy.Model3D = m_resourceManager.GetModel("t-pose_3_t");
            armyGuy.Transform.Scale = new Vector3(0.007f);
            armyGuy.Transform.Position = new Vector3(0.2f, -0.5f, 0.50f);
            //armyGuy.Visible = false;

            // Create the Game Objects
            StaticGameObject armyGuy2 = new StaticGameObject();
            armyGuy2.Model3D = m_resourceManager.GetModel("t-pose_3_t");
            armyGuy2.Transform.Scale = new Vector3(0.02f);
            armyGuy2.Transform.Position = new Vector3(0.0f, 0.5f, -1.5f);
            
            // Add to the Scene
            m_sceneManager.CurrentScene.SceneNode.Transform.AddChild(armyGuy.Transform);
            m_sceneManager.CurrentScene.SceneNode.Transform.AddChild(armyGuy2.Transform);

            // Load the Terrain
            Texture2D heightMap = Content.Load<Texture2D>("heightmap");
            Texture2D grassTexture = Content.Load<Texture2D>("grassTexture");
            m_terrain = new Terrain(graphics, heightMap, grassTexture);

            //// Load the Animated Model
            ////// Load the model we will display
            //model = new AnimatedModel("t-pose_3");
            //model.LoadContent(Content);
            ////// Load the model that has an animation clip it in
            //dance = new AnimatedModel("walk_t");
            //dance.LoadContent(Content);
            dance = new AnimatedGameObject();
            dance.Model3D = CustomContentLoader.LoadAnimatedModel(Content, "walk_t");
            
            System.Diagnostics.Debug.WriteLine(dance.Model3D.Clips.Count);
            AnimationClip clip = dance.Model3D.Clips[0];
            System.Diagnostics.Debug.WriteLine(dance.Model3D.Clips[0].Name);
            ////
            ////// And play the clip
            AnimationPlayer player = dance.Model3D.PlayClip(clip);
            player.Looping = true;
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            base.UnloadContent();
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            dance.Update(gameTime);

            if (rayPosOnTerrain.HasValue) {
                GameObject moving = m_sceneManager.CurrentScene.SceneNode.Transform.GetChild(0).GameObject;
                //GameObject moveTo = m_sceneManager.CurrentScene.SceneNode.GetChild(1);

                Vector3 direction = rayPosOnTerrain.Value - moving.Transform.Position;
                direction.Normalize();

                Vector3 speed = direction * 0.002f;

                moving.Transform.Position += speed * (float)gameTime.ElapsedGameTime.Milliseconds;
            }

            Vector3 p = m_sceneManager.CurrentScene.SceneNode.Transform.GetChild(1).Position; 
            p.X += 0.02f;
            m_sceneManager.CurrentScene.SceneNode.Transform.GetChild(1).Position = p;

            float height = m_terrain.GetHeightAtPoint(m_sceneManager.CurrentScene.SceneNode.Transform.GetChild(1).Position);

            if (height != float.MaxValue) {
                Vector3 pos = m_sceneManager.CurrentScene.SceneNode.Transform.GetChild(1).Position;
                pos.Y = height;
                m_sceneManager.CurrentScene.SceneNode.Transform.GetChild(1).Position = pos;
            }

            
            base.Update(gameTime);
        }
        

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            // First, call the PreDraw to prepare the screen for rendering
            base.PreDraw(gameTime);

            // Now we can begin our draw Here
            Texture2D logo = ResourceManager.Instance.GetTexture("KillerrinStudiosLogo");

            // Draw Texture
            spriteBatch.Begin();
            spriteBatch.Draw(logo, new Vector2(0.0f, 0.0f), Color.White);
            spriteBatch.End();

            //// Draw some Points
            //PrimitiveHelper2D.DrawSineWave(spriteBatch, Color.Red, 4, new Vector2(0.0f, 600.0f), 100.0f, 0.006f, GraphicsDevice.Viewport.Width, 0.0f);

            // Call Draw on the Anarian Game Engine to render the SceneGraph
            base.Draw(gameTime);

            //// Draw the Rays
            if (currentRay.HasValue) {
                currentRay.Value.DrawRay(graphics, Color.Red, m_sceneManager.CurrentScene.Camera, Matrix.Identity);
            }

            m_terrain.Draw(gameTime, m_sceneManager.CurrentScene.Camera, graphics);

            dance.Draw(gameTime, m_sceneManager.CurrentScene.Camera, graphics);

            // Lastly, Call the Monogame Draw Method
            base.PostDraw(gameTime);
        }


        #region Input Events
        void Mouse_MouseDown(object sender, Anarian.Events.MouseClickedEventArgs e)
        {
            if (e.ButtonClicked == MouseButtonClick.RightMouseButton) {
                //Ray ray = new Ray(Vector3.Zero, Vector3.One);
                //bool intersects = m_sceneManager.CurrentScene.SceneNode.Transform.GetChild(0).GameObject.CheckRayIntersection(ray);

                //Debug.WriteLine("GC: TOTAL MEMORY {0}", GC.GetTotalMemory(false));
            }
        }

        Ray? currentRay;
        Vector3? rayPosOnTerrain;
        void Mouse_MouseClicked(object sender, Anarian.Events.MouseClickedEventArgs e)
        {
            if (e.ButtonClicked == MouseButtonClick.LeftMouseButton) {
                Camera camera = m_sceneManager.CurrentScene.Camera;
                Ray ray = camera.GetMouseRay(
                    e.Position,
                    GraphicsDevice.Viewport
                    );

                bool intersects = m_sceneManager.CurrentScene.SceneNode.Transform.GetChild(0).GameObject.CheckRayIntersection(ray);
                Debug.WriteLine("Hit: {0}, Ray: {1}", intersects, ray.ToString());

                currentRay = ray;

                // Get the point on the terrain
                rayPosOnTerrain = m_terrain.Intersects(ray);
            }            
            if (e.ButtonClicked == MouseButtonClick.MiddleMouseButton) {
                //Debug.WriteLine("BEFORE GC: TOTAL MEMORY {0}", GC.GetTotalMemory(false));
                //GC.Collect();
                //Debug.WriteLine("AFTER GC: TOTAL MEMORY {0}", GC.GetTotalMemory(false));
                Debug.WriteLine("Middle Mouse Pressed");
                GamePage.PageFrame.Navigate(typeof(BlankPage));
                GamePage.PageFrame.Visibility = Windows.UI.Xaml.Visibility.Visible;
            }
            if (e.ButtonClicked == MouseButtonClick.RightMouseButton) {

            }
        }

        void Mouse_MouseMoved(object sender, Anarian.Events.MouseMovedEventArgs e)
        {
            //Debug.WriteLine("Mouse Moved To: {0}, Delta: {1}", e.Position.ToString(), e.DeltaPosition.ToString());
            //m_sceneManager.CurrentScene.Camera.AddYaw(e.DeltaPosition.X * 0.0005f);
            //m_sceneManager.CurrentScene.Camera.AddPitch(e.DeltaPosition.Y * 0.0005f);
        }

        void Keyboard_KeyboardDown(object sender, Anarian.Events.KeyboardPressedEventArgs e)
        {
            Camera cam = m_sceneManager.CurrentScene.Camera;
            
            switch (e.KeyClicked) {
                case Keys.W:
                    cam.MoveForward(2.0f);
                    break;
                case Keys.S:
                    cam.MoveForward(-2.0f);
                    break;
                case Keys.A:
                    cam.MoveHorizontal(-2.0f);
                    break;
                case Keys.D:
                    cam.MoveHorizontal(2.0f);
                    break;
                case Keys.Q:
                    cam.MoveVertical(-2.0f);
                    break;
                case Keys.E:
                    cam.MoveVertical(2.0f);
                    break;

                case Keys.O:
                    cam.MoveDepth(-2.0f);
                    break;
                case Keys.L:
                    cam.MoveDepth(2.0f);
                    break;

                case Keys.Up:
                    cam.AddPitch(MathHelper.ToRadians(2));
                    break;
                case Keys.Down:
                    cam.AddPitch(MathHelper.ToRadians(-2));
                    break;
                case Keys.Left:
                    cam.AddYaw(MathHelper.ToRadians(2));
                    break;
                case Keys.Right:
                    cam.AddYaw(MathHelper.ToRadians(-2));
                    break;
            }
            //Debug.WriteLine("{0}, Held Down", e.KeyClicked.ToString());
        }
        void Keyboard_KeyboardPressed(object sender, Anarian.Events.KeyboardPressedEventArgs e)
        {
            //Debug.WriteLine("{0}, Pressed", e.KeyClicked.ToString());
        }
        #endregion
    }
}
