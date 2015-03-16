﻿using Anarian;
using Anarian.DataStructures;
using Anarian.DataStructures.Animation.Aux;
using Anarian.DataStructures.Components;
using Anarian.Interfaces;
using EmpiresOfTheIV.Game.Enumerators;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmpiresOfTheIV.Game.GameObjects
{
    public class Unit : AnimatedGameObject,
                        IUpdatable, IRenderable, ISelectableEntity
    {
        #region Fields/Properties
        public uint UnitID { get; protected set; }
        public UnitType UnitType;

        public bool Selectable { get; set; }
        public bool Selected { get; set; }

        public Health Health { get { return GetComponent(typeof(Health)) as Health; } }
        public Mana Mana { get { return GetComponent(typeof(Mana)) as Mana; } }

        private Texture2D blankTexture;
        private Texture2D selectionBox;

        public AnimationClip MovementClip;
        public AnimationClip IdleClip;
        public AnimationClip AttackClip;

        public float HeightAboveTerrain;
        #endregion

        public Unit(uint unitID, UnitType unitType)
            : base()
        {
            UnitID = unitID;
            UnitType = unitType;

            // Setup base Selection rules
            Selectable = true;
            Selected = false;

            // Cash the refrences to textures
            blankTexture = ResourceManager.Instance.GetAsset(typeof(Texture2D), ResourceManager.EngineReservedAssetNames.blankTextureName) as Texture2D;
            selectionBox = ResourceManager.Instance.GetAsset(typeof(Texture2D), "SelectionBox") as Texture2D;

            // Add Unit Specific Components
            AddComponent(typeof(Health));
            AddComponent(typeof(Mana));

            // Other Variables
            MovementClip = null;
            IdleClip = null;
            AttackClip = null;

            HeightAboveTerrain = 0.0f;
        }

        public override void Reset()
        {
            UnitType = UnitType = Enumerators.UnitType.None;
            
            Selectable = true;
            Selected = false;

            MovementClip = null;
            IdleClip = null;
            AttackClip = null;

            HeightAboveTerrain = 0.0f;

            Health.Reset();
            Mana.Reset();
        }

        #region Interface Implimentations
        void IUpdatable.Update(GameTime gameTime) { Update(gameTime); }
        void IRenderable.Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics, ICamera camera) { Draw(gameTime, spriteBatch, graphics, camera); }
        
        bool ISelectableEntity.Selectable
        {
            get { return Selectable; }
            set { Selectable = value; }
        }

        bool ISelectableEntity.Selected
        {
            get { return Selected; }
            set { Selected = value; }
        }
        #endregion

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics, ICamera camera)
        {
            base.Draw(gameTime, spriteBatch, graphics, camera);

            #region Draw the Health
            Vector3 screenPos3D = graphics.Viewport.Project(m_transform.WorldPosition, camera.Projection, camera.View, camera.World);
            Vector2 screenPos2D = new Vector2(screenPos3D.X, screenPos3D.Y);
            Rectangle healthRectOutline = new Rectangle((int)(screenPos2D.X - graphics.Viewport.X) - 75,
                                                        (int)(screenPos2D.Y - graphics.Viewport.Y) + 25,
                                                        (int)Health.MaxHealth + 2,
                                                        5);
            Rectangle healthRect = new Rectangle(healthRectOutline.X + 1,
                                                 healthRectOutline.Y + 1,
                                                 (int)(MathHelper.Clamp(Health.CurrentHealth, 0, healthRectOutline.Width - 2)),
                                                 healthRectOutline.Height - 2);

            // Draw the selection rectangle
            spriteBatch.Begin();
            spriteBatch.Draw(blankTexture, healthRectOutline, Color.Black);
            spriteBatch.Draw(blankTexture, healthRect, Color.Red);
            spriteBatch.End();

            if (Selected)
            {
                float selectionBoxScale = 1.5f;
                spriteBatch.Begin();
                spriteBatch.Draw(selectionBox,
                                 screenPos2D - new Vector2(15,25) - new Vector2((selectionBox.Width * selectionBoxScale) / 2f, (selectionBox.Height * selectionBoxScale) / 2f),
                                 null, Color.Purple, 0, Vector2.Zero,
                                 selectionBoxScale, SpriteEffects.None, 1.0f);
                spriteBatch.End();
            }
            #endregion
        }

        protected override void SetupEffects(Effect effect, GraphicsDevice graphics, ICamera camera, GameTime gameTime)
        {
            base.SetupEffects(effect, graphics, camera, gameTime);
        }
    }
}
