﻿using Anarian;
using Anarian.Particles;
using Anarian.Particles.Particle2D;
using Anarian.Particles.Particle2D.Modifiers;
using Anarian.Particles.Particle2D.Modifiers.Emission;
using Anarian.Particles.Particle2D.Modifiers.Lifespan;
using Anarian.Particles.Particle2D.Modifiers.Modifiers;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace EmpiresOfTheIV.Game.GameObjects.ParticleEmitters
{
    public class SmokePlumeParticleSystem : ParticleEmitter2D
    {
        public SmokePlumeParticleSystem(Vector2 position, uint maxNumberOfParticles)
            : base(maxNumberOfParticles, new ContinuousParticleEmitter(TimeSpan.FromSeconds(0.3)), new TimebasedParticleLifespan(5.0f, 10.0f))
        {
            Position = position;
            OnEmission += SmokePlumeParticleSystem_OnEmission;

            // Add one Time assets
            ParticleTextures.Add(ResourceManager.Instance.GetAsset(typeof(Texture2D), ParticleNames.SmokeParticleEffect.ToString()) as Texture2D);
            //ParticleTextures.Add(ResourceManager.Instance.GetAsset(typeof(Texture2D), ParticleNames.ExplosionParticleEffect.ToString()) as Texture2D);

            ParticleModifiersPostUpdate.Add(new OpacityLifespanParticleModifier());
            ParticleModifiersPostUpdate.Add(new ScaleLifespanParticleModifier(0.75f, 0.25f));
        }

        void SmokePlumeParticleSystem_OnEmission(object sender, Anarian.Events.AnarianEventArgs e)
        {
            Debug.WriteLine("Particles Emitted");
        }

        public override void Reset()
        {
            base.Reset();
        }

        protected override void InitializeConstants()
        {
            // Add the rest of the constraints
            minInitialSpeed = 20;
            maxInitialSpeed = 100;

            // we don't want the particles to accelerate at all, aside from what we
            // do in our overriden InitializeParticle.
            minAcceleration = 0;
            maxAcceleration = 0;

            minScale = .5f;
            maxScale = 1.0f;

            // rotate slowly, we want a fairly relaxed effect
            minRotationSpeed = -MathHelper.PiOver4 / 2.0f;
            maxRotationSpeed = MathHelper.PiOver4 / 2.0f;

            blendState = BlendState.AlphaBlend;
        }

        /// <summary>
        /// PickRandomDirection is overriden so that we can make the particles always 
        /// move have an initial velocity pointing up.
        /// </summary>
        /// <returns>a random direction which points basically up.</returns>
        protected override Vector2 PickRandomDirection()
        {
            // Point the particles somewhere between 80 and 100 degrees.
            // tweak this to make the smoke have more or less spread.
            float radians = ParticleHelpers.RandomBetween(
                MathHelper.ToRadians(80), MathHelper.ToRadians(100));

            Vector2 direction = Vector2.Zero;
            // from the unit circle, cosine is the x coordinate and sine is the
            // y coordinate. We're negating y because on the screen increasing y moves
            // down the monitor.
            direction.X = (float)Math.Cos(radians);
            direction.Y = -(float)Math.Sin(radians);
            return direction;
        }

        /// <summary>
        /// InitializeParticle is overridden to add the appearance of wind.
        /// </summary>
        /// <param name="gameTime">The gameTime</param>
        /// <param name="particle">the particle to set up</param>
        protected override void InitializeParticle(GameTime gameTime, Particle2D particle)
        {
            base.InitializeParticle(gameTime, particle);

            // the base is mostly good, but we want to simulate a little bit of wind
            // heading to the right.
            particle.Acceleration.X += ParticleHelpers.RandomBetween(10, 50);

            particle.Colour = Color.Transparent;
        }
    }
}
