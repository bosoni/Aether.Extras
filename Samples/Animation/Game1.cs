using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using tainicom.Aether.Animation;

namespace Samples.Animation
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;

        Model _model_GPU;
        Animations _animations;

        KeyboardState prevKeyboardState;
        Clip[] animations = new Clip[10];

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("font");

            /*
            _model_GPU = Content.Load<Model>("Dude/dude_GPU");
            _animations = _model_GPU.GetAnimations(); // Animation Data are the same between the two models
            var clip = _animations.Clips["Take 001"];
            */

            _model_GPU = Content.Load<Model>("stickman_nla");
            _animations = _model_GPU.GetAnimations(); // Animation Data are the same between the two models

            int c = 0;
            foreach (KeyValuePair<string, Clip> kvp in _animations.Clips)
            {
                Console.WriteLine("Key = {0}, Value = {1}", kvp.Key, kvp.Value);
                animations[c++] = kvp.Value;
            }
            //_animations.SetClip(_animations.Clips["Idle.001"]);
            _animations.SetClip(animations[0]);
        }

        protected override void UnloadContent()
        {
        }

        int curAnim = 0;
        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var gamePadState = GamePad.GetState(PlayerIndex.One);

            // Allows the game to exit
            if (keyboardState.IsKeyDown(Keys.Escape) || gamePadState.Buttons.Back == ButtonState.Pressed)
                this.Exit();

            if (keyboardState.IsKeyDown(Keys.Space))
            {
                curAnim++;
                if (curAnim >= 4) curAnim = 0;
                _animations.SetClip(animations[curAnim]);
            }

            if (keyboardState.IsKeyDown(Keys.Up))
                Zoom += 5;

            if (keyboardState.IsKeyDown(Keys.Down))
                Zoom -= 5;

            prevKeyboardState = keyboardState;

            _animations.Update(gameTime.ElapsedGameTime, true, Matrix.Identity);

            base.Update(gameTime);
        }

        private Vector3 Position = Vector3.Zero;
        private float Zoom = 500;
        private float RotationY = 0.0f;
        private float RotationX = 0.0f;
        private Matrix gameWorldRotation = Matrix.Identity;


        Stopwatch sw = new Stopwatch();
        double msecMin = double.MaxValue;
        double msecMax = 0;
        double avg = 0;
        double acc = 0;
        int c;

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Blue);

            float aspectRatio = graphics.GraphicsDevice.Viewport.AspectRatio;
            Matrix projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(45.0f), aspectRatio, 0.01f, 10000.0f);
            Matrix view = Matrix.CreateLookAt(
                            new Vector3(0.0f, 60, -Zoom),
                            new Vector3(0.0f, 60.0f, 0),
                            Vector3.Up);

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;

            Matrix[] transforms = new Matrix[_model_GPU.Bones.Count];
            _model_GPU.CopyAbsoluteBoneTransformsTo(transforms);

            sw.Reset();
            sw.Start();

            foreach (ModelMesh mesh in _model_GPU.Meshes)
            {
                foreach (var part in mesh.MeshParts)
                {
                    ((SkinnedEffect)part.Effect).Alpha = 1.0f;
                    //((SkinnedEffect)part.Effect).SpecularColor = Vector3.One;
                    ConfigureEffectMatrices((IEffectMatrices)part.Effect, Matrix.Identity, view, projection);
                    ConfigureEffectLighting((IEffectLights)part.Effect);
                    ((SkinnedEffect)part.Effect).SetBoneTransforms(_animations.AnimationTransforms);// animate vertices on GPU
                }
                mesh.Draw();
            }
            sw.Stop();

            double msec = sw.Elapsed.TotalMilliseconds;
            msecMin = Math.Min(msecMin, msec);
            if (avg != 0)
                msecMax = Math.Max(msecMax, msec);
            acc += msec; c++;
            if (c > 60 * 2)
            {
                avg = acc / c;
                acc = c = 0;
            }

            spriteBatch.Begin();
            spriteBatch.DrawString(font, "Anim: " + curAnim, new Vector2(32, GraphicsDevice.Viewport.Height - 190), Color.White);
            spriteBatch.DrawString(font, "Zoom: " + Zoom, new Vector2(32, GraphicsDevice.Viewport.Height - 160), Color.White);
            spriteBatch.DrawString(font, msec.ToString("#0.000", CultureInfo.InvariantCulture) + "ms", new Vector2(32, GraphicsDevice.Viewport.Height - 130), Color.White);
            spriteBatch.DrawString(font, avg.ToString("#0.000", CultureInfo.InvariantCulture) + "ms (avg)", new Vector2(32, GraphicsDevice.Viewport.Height - 100), Color.White);
            spriteBatch.DrawString(font, msecMin.ToString("#0.000", CultureInfo.InvariantCulture) + "ms (min)", new Vector2(32, GraphicsDevice.Viewport.Height - 70), Color.White);
            spriteBatch.DrawString(font, msecMax.ToString("#0.000", CultureInfo.InvariantCulture) + "ms (max)", new Vector2(32, GraphicsDevice.Viewport.Height - 40), Color.White);
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void ConfigureEffectMatrices(IEffectMatrices effect, Matrix world, Matrix view, Matrix projection)
        {
            effect.World = world;
            effect.View = view;
            effect.Projection = projection;
        }

        private void ConfigureEffectLighting(IEffectLights effect)
        {
            effect.EnableDefaultLighting();
            effect.DirectionalLight0.Direction = Vector3.Backward;
            effect.DirectionalLight0.Enabled = true;
            effect.DirectionalLight1.Enabled = false;
            effect.DirectionalLight2.Enabled = false;
        }

    }
}
