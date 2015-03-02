﻿using Anarian.DataStructures;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Text;

namespace EmpiresOfTheIV.Game.GameObjects.Factories
{
    public class Factory : Building
    {
        //List<FactoryPlot> m_factoryPlots;

        public Factory()
            :base()
        {

        }

        public override bool CheckRayIntersection(Ray ray)
        {
            return base.CheckRayIntersection(ray);
        }
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
        }
        public override void Draw(GameTime gameTime, SpriteBatch spriteBatch, GraphicsDevice graphics, Camera camera)
        {
            base.Draw(gameTime, spriteBatch, graphics, camera);
        }
    }
}