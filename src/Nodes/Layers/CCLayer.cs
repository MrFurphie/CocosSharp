/****************************************************************************
Copyright (c) 2010-2012 cocos2d-x.org
Copyright (c) 2008-2010 Ricardo Quesada
Copyright (c) 2011      Zynga Inc.
Copyright (c) 2011-2012 openxlive.com

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE.
****************************************************************************/

using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace CocosSharp
{
    public class CCLayer : CCNode
    {
        public static CCCameraProjection DefaultCameraProjection = CCCameraProjection.Projection3D;

        // A delegate type for hooking up Layer Visible Bounds change notifications.
        internal delegate void LayerVisibleBoundsChangedEventHandler(object sender, EventArgs e);
        internal event LayerVisibleBoundsChangedEventHandler LayerVisibleBoundsChanged;

        bool restoreScissor;
        bool noDrawChildren;

        CCCameraProjection initCameraProjection;
        bool visibleBoundsDirty;
        CCRect visibleBoundsWorldspace;

        CCRenderTexture renderTexture;
        CCClipMode childClippingMode;
        CCCamera camera;

        CCRenderCommand beforeDrawCommand;
        CCRenderCommand afterDrawCommand;


        #region Properties

        public override CCLayer Layer 
        {
            get { return this; }
            internal set 
            {
            }
        }

        public override CCCamera Camera
        {
            get { return camera; }
            set 
            {
                if (camera != value) 
                {
                    // Stop listening to previous camera's event
                    if (camera != null)
                    {
                        camera.OnCameraVisibleBoundsChanged -= 
                            new CocosSharp.CCCamera.CameraVisibleBoundsChangedEventHandler(OnCameraVisibleBoundsChanged);
                    }

                    camera = value;

                    if (camera != null)
                    {
                        camera.OnCameraVisibleBoundsChanged += 
                            new CocosSharp.CCCamera.CameraVisibleBoundsChangedEventHandler(OnCameraVisibleBoundsChanged);
                        OnCameraVisibleBoundsChanged(camera, null);
                    }
                }
            }
        }

        /// <summary>
        /// Set to true if the child drawing should be isolated in their own render target
        /// </summary>
        public CCClipMode ChildClippingMode
        {
            get { return childClippingMode; }
            set
            {
                if (childClippingMode != value)
                {
                    childClippingMode = value;
                    UpdateClipping();
                }
            }
        }

        public new CCRect VisibleBoundsWorldspace
        {
            get 
            { 
                if (visibleBoundsDirty) 
                    UpdateVisibleBoundsRect(); 
                
                return visibleBoundsWorldspace; 
            }
        }

        // Layer should have fixed size of content
        public override CCSize ContentSize
        {
            get { return VisibleBoundsWorldspace.Size; }
            set
            { }
        }

        public override CCAffineTransform AffineLocalTransform 
        {
            get 
            {
                return CCAffineTransform.Identity;
            }
        }

        #endregion Properties


        #region Constructors

        public CCLayer()
            : this(DefaultCameraProjection)
        { 
        }

        public CCLayer(CCSize visibleBoundsDimensions, 
            CCClipMode clipMode = CCClipMode.None)
            : this(visibleBoundsDimensions, DefaultCameraProjection, clipMode)
        {  
        }

        public CCLayer(CCSize visibleBoundsDimensions, 
            CCCameraProjection cameraProjection, 
            CCClipMode clipMode = CCClipMode.None)
            : this(new CCCamera(cameraProjection, visibleBoundsDimensions), clipMode)
        {  
        }

        public CCLayer(CCCamera camera, CCClipMode clipMode = CCClipMode.None) 
            : this(camera.Projection, clipMode)
        {
            Camera = camera;
        }

        public CCLayer(CCCameraProjection cameraProjection, CCClipMode clipMode = CCClipMode.None)
            : base()
        {
            beforeDrawCommand = new CCCustomCommand(BeforeDraw);
            afterDrawCommand = new CCCustomCommand(AfterDraw);

            ChildClippingMode = clipMode;
            IgnoreAnchorPointForPosition = true;
            AnchorPoint = CCPoint.AnchorMiddle;

            initCameraProjection = cameraProjection;
        }

        void UpdateClipping()
        {
            if (ChildClippingMode == CCClipMode.BoundsWithRenderTarget && Scene !=null)
            {
                CCRect bounds = VisibleBoundsWorldspace;
                CCRect viewportRect = new CCRect(Viewport.Bounds);

                renderTexture = new CCRenderTexture(bounds.Size, viewportRect.Size);
                renderTexture.Sprite.AnchorPoint = new CCPoint(0, 0);
            }
            else
            {
                renderTexture = null;
            }
        }

        #endregion Constructors


        #region Content layout

        protected override void AddedToScene()
        {
            base.AddedToScene();

            if(Camera == null)
            {
                Camera = new CCCamera (initCameraProjection, GameView.DesignResolution);
                visibleBoundsDirty = true;
            }
        }

        void OnCameraVisibleBoundsChanged(object sender, EventArgs e)
        {
            CCCamera camera = sender as CCCamera;

            if(camera != null && camera == Camera && Scene != null) 
            {
                visibleBoundsDirty = true;
                if (LayerVisibleBoundsChanged != null)
                    LayerVisibleBoundsChanged(this, null);
            }
        }

        internal void OnViewportChanged (CCGameView gameView)
        {
            if (Scene != null && Camera != null) 
            {
                ViewportChanged ();
                visibleBoundsDirty = true;
            }
        }


        protected override void VisibleBoundsChanged()
        {
            base.VisibleBoundsChanged();

            UpdateVisibleBoundsRect();
            UpdateClipping();
        }

        protected override void ViewportChanged()
        {
            base.ViewportChanged();

            visibleBoundsDirty = true;
            UpdateClipping();
        }

        void UpdateVisibleBoundsRect()
        {
            if(Camera == null)
                return;

            if (Camera.Projection == CCCameraProjection.Projection2D && Camera.OrthographicViewSizeWorldspace == CCSize.Zero)
                return;



            // Want to determine worldspace bounds relative to camera target
            // Need to first find z screenspace coord of target
            CCPoint3 target = Camera.TargetInWorldspace;
            Vector3 targetVec = new Vector3(0.0f, 0.0f, target.Z);
            targetVec = Viewport.Project(targetVec, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);

            Vector3 topLeft = new Vector3(Viewport.X, Viewport.Y, targetVec.Z);
            Vector3 topRight = new Vector3(Viewport.X + Viewport.Width, Viewport.Y, targetVec.Z);
            Vector3 bottomLeft = new Vector3(Viewport.X, Viewport.Y + Viewport.Height, targetVec.Z);
            Vector3 bottomRight = new Vector3(Viewport.X + Viewport.Width, Viewport.Y + Viewport.Height, targetVec.Z);

            // Convert screen space to worldspace. Note screenspace origin is in topleft part of viewport
            topLeft = Viewport.Unproject(topLeft, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);
            topRight = Viewport.Unproject(topRight, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);
            bottomLeft = Viewport.Unproject(bottomLeft, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);
            bottomRight = Viewport.Unproject(bottomRight, Camera.ProjectionMatrix, Camera.ViewMatrix, Matrix.Identity);

            CCPoint topLeftPoint = new CCPoint(topLeft.X, topLeft.Y);
            CCPoint bottomLeftPoint = new CCPoint(bottomLeft.X, bottomLeft.Y);
            CCPoint bottomRightPoint = new CCPoint(bottomRight.X, bottomRight.Y);

            visibleBoundsWorldspace = new CCRect(
                (float)Math.Round(bottomLeftPoint.X), (float)Math.Round(bottomLeftPoint.Y), 
                (float)Math.Round(bottomRightPoint.X - bottomLeftPoint.X), 
                (float)Math.Round(topLeftPoint.Y - bottomLeftPoint.Y));

            visibleBoundsDirty = false;
        }

        #endregion Content layout


        #region Visiting and drawing

        public override void Visit(ref CCAffineTransform parentWorldTransform)
        {
            if (!Visible || GameView == null)
                return;

            // Set camera view/proj matrix even if ChildClippingMode is None
            if(Camera != null)
            {
                var viewMatrix = Camera.ViewMatrix;
                var projMatrix = Camera.ProjectionMatrix;

                Renderer.PushLayerGroup(ref viewMatrix, ref projMatrix);
            }

            if (ChildClippingMode == CCClipMode.None)
            {
                base.Visit(ref parentWorldTransform);

                if(Camera != null)
                    Renderer.PopLayerGroup();

                return;
            }

            beforeDrawCommand.GlobalDepth = float.MinValue;
            beforeDrawCommand.WorldTransform = parentWorldTransform;
            Renderer.AddCommand(beforeDrawCommand);

            VisitRenderer(ref parentWorldTransform);

            if(!noDrawChildren && Children != null)
            {
                SortAllChildren();
                var elements = Children.Elements;
                for(int i = 0, N = Children.Count; i < N; ++i)
                {
                    var child = elements[i];
                    if (child.Visible)
                        child.Visit(ref parentWorldTransform);
                }
            }

            afterDrawCommand.GlobalDepth = float.MaxValue;
            afterDrawCommand.WorldTransform = parentWorldTransform;
            Renderer.AddCommand(afterDrawCommand);

            if(Camera != null)
                Renderer.PopLayerGroup();
        }

        void BeforeDraw()
        {
            noDrawChildren = false;
            CCRect visibleBounds = Layer.VisibleBoundsWorldspace;
            CCRect viewportRect = new CCRect(Viewport.Bounds);
            CCDrawManager drawManager = DrawManager;

            if (ChildClippingMode == CCClipMode.Bounds && GameView != null)
            {
                drawManager.ScissorRectInPixels = viewportRect;
            }

            else if (ChildClippingMode == CCClipMode.BoundsWithRenderTarget)
            {
                restoreScissor = DrawManager.ScissorRectEnabled;

                DrawManager.ScissorRectEnabled = false;

                DrawManager.PushMatrix();
                DrawManager.WorldMatrix = Matrix.Identity;

                renderTexture.BeginWithClear(0, 0, 0, 0);
            }
        }

        void AfterDraw()
        {
            if (ChildClippingMode != CCClipMode.None)
            {
                if (ChildClippingMode == CCClipMode.BoundsWithRenderTarget)
                {
                    renderTexture.End();

                    DrawManager.PopMatrix();
                }

                if (restoreScissor)
                {
                    DrawManager.ScissorRectEnabled = true;
                    restoreScissor = false;
                }
                else
                {
                    DrawManager.ScissorRectEnabled = false;
                }

                if (ChildClippingMode == CCClipMode.BoundsWithRenderTarget)
                {
                    renderTexture.Sprite.Visit();
                }
            }
        }

        #endregion Visiting and drawing


        #region Unit conversion

        public CCPoint ScreenToWorldspace(CCPoint point)
        {
            CCRect viewportRectInPixels = new CCRect(Viewport.Bounds);
            CCRect worldBounds = Layer.VisibleBoundsWorldspace;

            point -= viewportRectInPixels.Origin;

            // Note: Screen coordinates have origin in top left corner
            // but world coords have origin in bottom left corner
            // Therefore, Y world ratio is 1 minus Y viewport ratio
            CCPoint worldPointRatio 
            = new CCPoint(point.X / viewportRectInPixels.Size.Width, 1 - (point.Y / viewportRectInPixels.Size.Height));

            return new CCPoint (
                worldBounds.Origin.X + (worldBounds.Size.Width * worldPointRatio.X),
                worldBounds.Origin.Y + (worldBounds.Size.Height * worldPointRatio.Y));
        }

        public CCSize ScreenToWorldspace(CCSize size)
        {
            CCRect viewportRectInPixels = new CCRect(Viewport.Bounds);
            CCRect worldBounds = Layer.VisibleBoundsWorldspace;

            CCPoint worldSizeRatio = new CCPoint(size.Width / viewportRectInPixels.Size.Width, size.Height / viewportRectInPixels.Size.Height);

            return new CCSize(worldSizeRatio.X * worldBounds.Size.Width, worldSizeRatio.Y * worldBounds.Size.Height);
        }

        public CCSize WorldToScreenspace(CCSize size)
        {
            CCRect visibleBounds = VisibleBoundsWorldspace;
            CCRect viewportInPixels = new CCRect(Viewport.Bounds);

            CCPoint worldSizeRatio = new CCPoint(size.Width / visibleBounds.Size.Width, size.Height / visibleBounds.Size.Height);

            return new CCSize(worldSizeRatio.X * viewportInPixels.Size.Width, worldSizeRatio.Y * viewportInPixels.Size.Height);
        }

        public CCPoint WorldToScreenspace(CCPoint point)
        {
            CCRect worldBounds = VisibleBoundsWorldspace;
            CCRect viewportRectInPixels = new CCRect(Viewport.Bounds);

            point -= worldBounds.Origin;

            CCPoint worldPointRatio 
            = new CCPoint(point.X / worldBounds.Size.Width, (point.Y / worldBounds.Size.Height));

            return new CCPoint(viewportRectInPixels.Origin.X + viewportRectInPixels.Size.Width * worldPointRatio.X,
                viewportRectInPixels.Origin.Y + viewportRectInPixels.Size.Height * (1 - worldPointRatio.Y));
        }

        #endregion Unit conversion
    }
}