﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;

using Anarian.Interfaces;
using Anarian.Helpers;

namespace Anarian.DataStructures
{
    public class GameObject : IUpdatable, IRenderable
    {
        #region Properties
        bool    m_active;
        bool    m_visible;
        Model   m_model;

        public bool Active
        {
            get { return m_active; }
            set { m_active = value; }
        }
        public bool Visible
        {
            get { return m_visible; }
            set { m_visible = value; }
        }
        public Model Model3D
        {
            get { return m_model; }
            set { 
                m_model = value;
            }
        }
        #endregion

        #region Translations
        Vector3 m_orbitalRotation;
        public Vector3 OrbitalRotation
        {
            get { return m_orbitalRotation; }
            set { m_orbitalRotation = value; }
        }
        
        Vector3 m_rotation;
        public Vector3 Rotation
        {
            get { return m_rotation; }
            set { m_rotation = value; }
        }

        Vector3 m_scale;
        public Vector3 Scale {
            get { return m_scale; }
            set { m_scale = value; }
        }

        Vector3 m_position;
        public Vector3 Position {
            get { return m_position; }
            set { m_position = value; }
        }

        public Matrix WorldMatrix
        {
            get
            {
                Matrix scale = Matrix.CreateScale(WorldScale);

                Vector3 worldRotation = WorldRotation;
                Matrix rotX = Matrix.CreateRotationX(worldRotation.X);
                Matrix rotY = Matrix.CreateRotationY(worldRotation.Y);
                Matrix rotZ = Matrix.CreateRotationZ(worldRotation.Z);
                Matrix rotation = rotX * rotY * rotZ;

                Matrix translation = Matrix.CreateTranslation(WorldPosition);

                Vector3 worldOrbitalRotation = WorldOrbitalRotation;
                Matrix rotOX = Matrix.CreateRotationX(worldOrbitalRotation.X);
                Matrix rotOY = Matrix.CreateRotationY(worldOrbitalRotation.Y);
                Matrix rotOZ = Matrix.CreateRotationZ(worldOrbitalRotation.Z);
                Matrix orbitalRotation = rotOX * rotOY * rotOZ;

                return scale * rotation * translation * orbitalRotation;
            }
        }

        public Vector3 WorldPosition
        {
            get
            {
                Vector3 pos = m_position;

                if (m_parent != null) {
                    pos += m_parent.WorldPosition;
                }
                return pos;
            }
        }

        public Vector3 WorldRotation
        {
            get
            {
                Vector3 rot = m_rotation;

                if (m_parent != null) {
                    rot += m_parent.WorldRotation;
                }
                return rot;
            }
        }

        public Vector3 WorldOrbitalRotation
        {
            get
            {
                Vector3 rot = m_orbitalRotation;

                if (m_parent != null) {
                    rot += m_parent.WorldOrbitalRotation;
                }
                return rot;
            }
        }

        public Vector3 WorldScale
        {
            get
            {
                Vector3 sca = m_scale;

                if (m_parent != null) {
                    sca += m_parent.WorldScale;
                }
                return sca;
            }
        }
        #endregion


        List<BoundingBox> m_boundingBoxes;
        public GameObject()
        {
            m_parent    = null;
            m_active    = true;
            m_visible   = true;

            m_orbitalRotation = Vector3.Zero;

            m_position  = Vector3.Zero;
            m_rotation  = Vector3.Zero;
            m_scale     = Vector3.One;

            m_children  = new List<GameObject>();
            m_boundingBoxes = new List<BoundingBox>();
        }

        public bool CheckRayIntersection(Ray ray)
        {
            // Generate the bounding boxes
            m_boundingBoxes = new List<BoundingBox>();
            
            // Create the ModelTransforms
            Matrix[] modelTransforms = new Matrix[Model3D.Bones.Count];
            Model3D.CopyAbsoluteBoneTransformsTo(modelTransforms);

            // Check intersection
            foreach (ModelMesh mesh in Model3D.Meshes) {
                //BoundingSphere boundingSphere = mesh.BoundingSphere.Transform(modelTransforms[mesh.ParentBone.Index] * WorldMatrix);
                BoundingBox boundingBox = mesh.GenerateBoundingBox(WorldMatrix);
                m_boundingBoxes.Add(boundingBox);

                if (ray.Intersects(boundingBox) != null) return true;
            }
            return false;
        }

        #region Interface Implimentations
        void IUpdatable.Update(GameTime gameTime) { Update(gameTime); }
        void IRenderable.Draw(GameTime gameTime, Camera camera, GraphicsDeviceManager graphics) { Draw(gameTime, camera, graphics); }
        #endregion

        #region Update/Draw
        public void Update(GameTime gameTime)
        {
            if (!m_active) return;
            foreach (GameObject gO in m_children) {
                gO.Update(gameTime);
            }
        }

        public void Draw(GameTime gameTime, Camera camera, GraphicsDeviceManager graphics)
        {
            if (!m_active) return;


            // Render the Children
            foreach (GameObject gO in m_children) {
                if (gO != null) gO.Draw(gameTime, camera, graphics);
            }


            // Now that the children have been rendered...
            // We check if we are visible on the screen,
            // We check if we have a model,
            // Then we render it
            if (!m_visible) return;
            if (m_model == null) return;

            // Render This Object
            //Debug.WriteLine("Rendering Model Pos:{0}, Sca:{1}, Rot:{2}", WorldPosition, WorldScale, WorldRotation);
            
            // Since we are also using 2D, Reset the
            // Graphics Device to Render 3D Models properly
            GraphicsDevice graphicsDevice = graphics.GraphicsDevice;
            graphicsDevice.BlendState = BlendState.Opaque;
            graphicsDevice.DepthStencilState = DepthStencilState.Default;
            graphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            // Copy any parent transforms.
            Matrix[] transforms = new Matrix[m_model.Bones.Count];
            m_model.CopyAbsoluteBoneTransformsTo(transforms);

            // Draw the model. A model can have multiple meshes, so loop.
            foreach (ModelMesh mesh in m_model.Meshes) {
                // This is where the mesh orientation is set, as well 
                // as our camera and projection.
                foreach (BasicEffect effect in mesh.Effects) {
                    effect.EnableDefaultLighting();
                    //effect.LightingEnabled = false;// EnableDefaultLighting();
                    effect.DiffuseColor = new Vector3(1, 1, 1);
                    effect.PreferPerPixelLighting = true;
                    effect.World = transforms[mesh.ParentBone.Index]
                        * WorldMatrix;
                    //* Matrix.CreateScale(20f);
                    effect.View = camera.View;
                    effect.Projection = camera.Projection;
                }
                // Draw the mesh, using the effects set above.
                mesh.Draw();

                for (int i = 0; i < m_boundingBoxes.Count; i++) {
                    m_boundingBoxes[i].DrawBoundingBox(graphics, Color.Red, camera, Matrix.Identity);
                }
            }
        }
        #endregion

        #region Parent/Children
        GameObject m_parent;
        GameObject Parent { 
            get { return m_parent; }
            set { m_parent = value; }
        }

        List<GameObject> m_children;
        public List<GameObject> GetChildren() { return m_children; }
        public void AddChild(GameObject child) {
            child.Parent = this;
            m_children.Add(child); 
        }
        public void RemoveChild(int index) { m_children.RemoveAt(index); }
        public GameObject GetChild(int index) { return m_children[index]; }
        #endregion
    }
}
