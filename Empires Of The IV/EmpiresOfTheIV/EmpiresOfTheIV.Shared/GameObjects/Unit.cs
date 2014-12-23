﻿using Anarian.DataStructures;
using Anarian.DataStructures.Components;
using Anarian.Interfaces;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmpiresOfTheIV.GameObjects
{
    public class Unit : AnimatedGameObject,
                        IUpdatable, IRenderable, ISelectableEntity
    {
        #region Fields/Properties
        bool m_selectable;
        public bool Selectable
        {
            get { return m_selectable; }
            set { m_selectable = value; }
        }

        bool m_selected;
        public bool Selected
        {
            get { return m_selected; }
            set { m_selected = value; }
        }

        public Health Health { get { return GetComponent(typeof(Health)) as Health; } }
        public Mana Mana { get { return GetComponent(typeof(Mana)) as Mana; } }
        #endregion

        public Unit()
            : base()
        {
            Selectable = true;
            Selected = false;

            // Add Unit Specific Components
            AddComponent(typeof(Health));
            AddComponent(typeof(Mana));
        }

        #region Interface Implimentations
        void IUpdatable.Update(GameTime gameTime) { Update(gameTime); }
        void IRenderable.Draw(GameTime gameTime, Camera camera, GraphicsDeviceManager graphics) { Draw(gameTime, camera, graphics); }
        
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

        public override void Draw(GameTime gameTime, Camera camera, GraphicsDeviceManager graphics)
        {
            base.Draw(gameTime, camera, graphics);
        }
    }
}
