﻿using Anarian.DataStructures;
using Anarian.Interfaces;
using Anarian.Helpers;
using EmpiresOfTheIV.Game.Enumerators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Audio;
using Anarian;

namespace EmpiresOfTheIV.Game.GameObjects.Factories
{
    public class FactoryBase : AnarianObject, IUpdatable, IRenderable
    {

        #region Fields/Properties
        public uint FactoryBaseID { get; private set; }
        public uint PlayerID;

        public Building Base;
        public Factory Factory;

        public Vector3 DefaultRallyPoint { get; protected set; }
        public Vector3 CurrentRallyPoint;

        public AudioEmitter FactoryBaseAudioEmitter { get { return Base.BuildingAudioEmitter; } }

        public double DamageTakenThisFrame;
        public BoundingSphere Bounds;

        public SpriteFont FactoryBuildTimerSpriteFont;
        public Timer FactoryBuildTimer;

        public bool IsFactoryOnBase { get { return (Factory != null); } }
        public bool HasOwner { get { return (PlayerID != uint.MaxValue); } }
                //if (PlayerID == uint.MaxValue) return false;
                //return true;
        #endregion

        public FactoryBase(uint id, Vector3 rallypoint, BoundingSphere bounds)
            :base()
        {
            FactoryBaseID = id;
            PlayerID = uint.MaxValue;

            DefaultRallyPoint = rallypoint;
            CurrentRallyPoint = DefaultRallyPoint;

            Bounds = bounds;

            FactoryBuildTimer = new Timer(TimeSpan.FromSeconds(15.0));
            FactoryBuildTimerSpriteFont = ResourceManager.Instance.GetAsset(typeof(SpriteFont), "EmpiresOfTheIVFont") as SpriteFont;
        }

        public FactoryBaseRayIntersection CheckRayIntersection(Ray ray)
        {

            float? result = Bounds.Intersects(ray);
            if (result.HasValue)
            {
                if (HasOwner)
                    return FactoryBaseRayIntersection.Factory;
                else
                    return FactoryBaseRayIntersection.FactoryBase;
            }

            // No collision detected
            return FactoryBaseRayIntersection.None; 
        }

        public FactoryBaseRayIntersection CheckSphereIntersection(BoundingSphere sphere)
        {
            bool result = Bounds.Intersects(sphere);
            if (result)
            {
                if (HasOwner)
                    return FactoryBaseRayIntersection.Factory;
                else
                    return FactoryBaseRayIntersection.FactoryBase;
            }

            // No collision detected
            return FactoryBaseRayIntersection.None;
        }

        public bool TakeDamage(float damage)
        {
            if (!HasOwner) return false;
            if (Factory == null) return false;

            Factory.Health.DecreaseHealth(damage);
            return true;
        }

        void IUpdatable.Update(GameTime gameTime) { Update(gameTime); }
        public void Update(GameTime gameTime)
        {
            if (Base != null)
            {
                Base.Update(gameTime);
                Bounds.Center = Base.Transform.WorldPosition;
            }

            if (Factory != null)
                Factory.Update(gameTime);
            else
            {
                FactoryBuildTimer.Update(gameTime);
            }
        }
        void IRenderable.Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics, ICamera camera) { Draw(gameTime, spriteBatch, graphics, camera, false); }
        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics, ICamera camera, bool creatingShadowMap = false)
        {
            if (Base != null)
                Base.Draw(gameTime, spriteBatch, graphics, camera, creatingShadowMap);

            if (Factory != null)
                Factory.Draw(gameTime, spriteBatch, graphics, camera, creatingShadowMap);
            else
            {
                if (!creatingShadowMap)
                {
                    Vector2 screenPos = camera.ProjectToScreenCoordinates(Base.Transform.WorldPosition, graphics.Viewport);

                    var timeRemaining = FactoryBuildTimer.TimeRemaining;

                    if (FactoryBuildTimer.Progress == Anarian.Enumerators.ProgressStatus.InProgress)
                    {
                        string timeRemainingString = timeRemainingString = timeRemaining.Seconds + ":" + timeRemaining.Milliseconds;

                        spriteBatch.Begin();
                        spriteBatch.DrawString(FactoryBuildTimerSpriteFont,
                                               timeRemainingString,
                                               screenPos - new Vector2(50, 0.0f),
                                               Color.Blue);
                        spriteBatch.End();
                    }
                }
            }

            //Bounds.RenderBoundingSphere(graphics, camera.World, camera.View, camera.Projection, Color.Red);
        }
    }
}
