﻿using Anarian.DataStructures;
using Anarian.GUI;
using Anarian.Interfaces;
using EmpiresOfTheIV.Game.Enumerators;
using EmpiresOfTheIV.Game.GameObjects;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmpiresOfTheIV.Game.Menus
{
    public class PausedMenu : GameMenu,
                              IUpdatable, IRenderable
    {

        #region Fields/Properties
        EmpiresOfTheIVGame m_game;
        Level m_level;
        #endregion

        public PausedMenu(EmpiresOfTheIVGame game)
            : base(GameState.Paused)
        {
            m_game = game;
        }


        #region Interface Implimentations
        void IUpdatable.Update(GameTime gameTime) { Update(gameTime); }
        void IRenderable.Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics, Camera camera) { Draw(gameTime, spriteBatch, graphics, camera); }
        #endregion

        public void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }

        public void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics, Camera camera)
        {
            base.Draw(gameTime, spriteBatch, graphics);
        }
    }
}