﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PlayerAndEditorGUI;


namespace Playtime_Painter
{

    public abstract class BlitMode : IeditorDropdown
    {

        private static List<BlitMode> _allModes;

        protected PainterManager rt { get { return PainterManager.inst; } }

        protected PainterConfig cfg { get { return PainterConfig.inst; } }

        public int index;

        protected static PlaytimePainter painter;
        public static BrushConfig pegibrush;

        public static BlitMode getCurrentBlitModeForPainter(PlaytimePainter inspectedPainter)
        { painter = inspectedPainter; return PainterConfig.inst.brushConfig.blitMode; }

        public static List<BlitMode> allModes
        {
            get
            {
                if (_allModes == null)
                    InstantiateBrushes();
                return _allModes;
            }
        }

        public virtual bool showInDropdown()
        {
            if (painter == null)
                return (pegibrush.TargetIsTex2D ? supportedByTex2D : supportedByRenderTexturePair);

            imgData id = painter.curImgData;

            if (id == null)
                return false;

            return ((id.destination == texTarget.Texture2D) && (supportedByTex2D)) ||
                (
                    (id.destination == texTarget.RenderTexture) &&
                    ((supportedByRenderTexturePair && (id.renderTexture == null))
                        || (supportedBySingleBuffer && (id.renderTexture != null)))
                );
        }

        public BlitMode setKeyword()
        {

            foreach (BlitMode bs in allModes)
                if (bs != this)
                {
                    string name = bs.shaderKeyword;
                    if (name != null)
                        BlitModeExtensions.KeywordSet(name, false);
                }

            if (shaderKeyword != null)
                BlitModeExtensions.KeywordSet(shaderKeyword, true);

            return this;

        }

        protected virtual string shaderKeyword { get { return null; } }

        public virtual void SetGlobalShaderParameters() {}

        public BlitMode()
        {
            index = _allModes.Count;
        }

        protected static void InstantiateBrushes()
        {
            _allModes = new List<BlitMode>();
            
            _allModes.Add(new BlitModeAlphaBlit());
            _allModes.Add(new BlitModeAdd());
            _allModes.Add(new BlitModeSubtract());
            _allModes.Add(new BlitModeCopy());
            _allModes.Add(new BlitModeMin());
            _allModes.Add(new BlitModeMax());
            _allModes.Add(new BlitModeBlur());
            _allModes.Add(new BlitModeBloom());
            _allModes.Add(new BlitModeSamplingOffset());
            // The code below uses reflection to find all classes that are child classes of BlitMode.
            // The code above adds them manually to save some compilation time,
            // and if you add new BlitMode, just add _allModes.Add (new BlitModeMyStuff ());
            // Alternatively, in a far-far future, the code below may be reanabled if there will be like hundreds of fan-made brushes for my asset
            // Which would be cool, but I'm a realist so whatever happens its ok, I lived a good life and greatfull for every day.

            /*
			List<Type> allTypes = CsharpFuncs.GetAllChildTypesOf<BlitMode>();
			foreach (Type t in allTypes) {
				BlitMode tb = (BlitMode)Activator.CreateInstance(t);
				_allModes.Add(tb);
			}
            */
        }

        public virtual blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.AlphaBlit; } }

        public virtual bool supportedByTex2D { get { return true; } }
        public virtual bool supportedByRenderTexturePair { get { return true; } }
        public virtual bool supportedBySingleBuffer { get { return true; } }
        public virtual bool usingSourceTexture { get { return false; } }
        public virtual bool showColorSliders { get { return true; } }
        public virtual Shader shaderForDoubleBuffer { get { return rt.br_Multishade; } }
        public virtual Shader shaderForSingleBuffer { get { return rt.br_Blit; } }

        public virtual bool PEGI(BrushConfig brush, PlaytimePainter p)
        {

            imgData id = p == null ? null : p.curImgData;

            bool cpuBlit = id == null ? brush.TargetIsTex2D : id.destination == texTarget.Texture2D;
            BrushType brushType = brush.type;
            bool usingDecals = (!cpuBlit) && brushType.isUsingDecals;


            bool changed = false;

            pegi.newLine();

            if ((!cpuBlit) && (!usingDecals))
            {
                pegi.write("Hardness:", "Makes edges more rough.", 70);
                changed |= pegi.edit(ref brush.Hardness, 1f, 512f);
                pegi.newLine();
            }

            pegi.write(usingDecals ? "Tint alpha" : "Speed", usingDecals ? 70 : 40);
            changed |= pegi.edit(ref brush.speed, 0.01f, 20);
            pegi.newLine();
            pegi.write("Scale:", 40);



            if ((!cpuBlit) && brushType.isA3Dbrush)
            {

                Mesh m = painter.getMesh();

                float maxScale = (m != null ? m.bounds.max.magnitude : 1) * (painter == null ? 1 : painter.transform.lossyScale.magnitude);

                changed |= pegi.edit(ref brush.Brush3D_Radius, 0.001f * maxScale, maxScale * 0.5f);


            }
            else
            {
                if (!brushType.isPixelPerfect)
                    changed |= pegi.edit(ref brush.Brush2D_Radius, cpuBlit ? 1 : 0.1f, usingDecals ? 128 : (id != null ? id.width * 0.5f : 256));
                else
                {
                    int val = (int)brush.Brush2D_Radius;
                    changed |= pegi.edit(ref val, (int)(cpuBlit ? 1 : 0.1f), (int)(usingDecals ? 128 : (id != null ? id.width * 0.5f : 256)));
                    brush.Brush2D_Radius = val;
                }
            }

            pegi.newLine();

            return changed;
        }

        public virtual void PrePaint(PlaytimePainter pntr, BrushConfig br, StrokeVector st)
        {

            return;
        }
    }
        public class BlitModeAlphaBlit : BlitMode
        {
            public override string ToString() { return "Alpha Blit"; }
            protected override string shaderKeyword { get { return "BRUSH_NORMAL"; } }
        }

        public class BlitModeAdd : BlitMode
        {
            static BlitModeAdd _inst;
            public static BlitModeAdd inst { get { if (_inst == null) InstantiateBrushes(); return _inst; } }

            public override string ToString() { return "Add"; }
            protected override string shaderKeyword { get { return "BRUSH_ADD"; } }

            public override Shader shaderForSingleBuffer { get { return rt.br_Add; } }
            public override blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.AddBlit; } }

            public BlitModeAdd()
            {
                _inst = this;
            }
        }

        public class BlitModeSubtract : BlitMode
        {
            public override string ToString() { return "Subtract"; }
            protected override string shaderKeyword { get { return "BRUSH_SUBTRACT"; } }

            //public override Shader shaderForSingleBuffer { get { return rt.br_Add; } }
            public override bool supportedBySingleBuffer { get { return false; } }

            public override blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.SubtractBlit; } }
        }

        public class BlitModeCopy : BlitMode
        {
            public override string ToString() { return "Copy"; }
            protected override string shaderKeyword { get { return "BRUSH_COPY"; } }
            public override bool showColorSliders { get { return false; } }

            public override bool supportedByTex2D { get { return false; } }
            public override bool usingSourceTexture { get { return true; } }
            public override Shader shaderForSingleBuffer { get { return rt.br_Copy; } }
        }

        public class BlitModeMin : BlitMode
        {
            public override string ToString() { return "Min"; }
            public override bool supportedByRenderTexturePair { get { return false; } }
            public override bool supportedBySingleBuffer { get { return false; } }
            public override blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.MinBlit; } }
        }

        public class BlitModeMax : BlitMode
        {
            public override string ToString() { return "Max"; }
            public override bool supportedByRenderTexturePair { get { return false; } }
            public override bool supportedBySingleBuffer { get { return false; } }
            public override blitModeFunction BlitFunctionTex2D { get { return Blit_Functions.MaxBlit; } }
        }

        public class BlitModeBlur : BlitMode
        {
            public override string ToString() { return "Blur"; }
            protected override string shaderKeyword { get { return "BRUSH_BLUR"; } }
            public override bool showColorSliders { get { return false; } }
            public override bool supportedBySingleBuffer { get { return false; } }
            public override bool supportedByTex2D { get { return false; } }

            public override Shader shaderForDoubleBuffer { get { return rt.br_BlurN_SmudgeBrush; } }

            public override bool PEGI(BrushConfig brush, PlaytimePainter p)
            {

                bool brushChanged_RT = base.PEGI(brush, p);
                pegi.newLine();
                pegi.write("Blur Amount", 70);
                brushChanged_RT |= pegi.edit(ref brush.blurAmount, 1f, 8f);
                pegi.newLine();
                return brushChanged_RT;
            }
        }

        public class BlitModeSamplingOffset : BlitMode
        {
            protected override string shaderKeyword { get { return "BRUSH_SAMPLE_DISPLACE"; } }

            public enum ColorSetMethod { MDownPosition = 0, MDownColor = 1, Manual = 2 }

            public myIntVec2 currentPixel = new myIntVec2();

            public int method;

            public override bool supportedByTex2D { get { return false; } }

            public override string ToString() { return "Pixel Reshape"; }

            public void fromUV(Vector2 uv)  {
                currentPixel.x = (int)Mathf.Floor(uv.x * cfg.samplingMaskSize.x);
                currentPixel.y = (int)Mathf.Floor(uv.y * cfg.samplingMaskSize.y);
            }


            public void fromColor(BrushConfig brush, Vector2 uv) {
                var c = brush.colorLinear.ToColor();

                currentPixel.x = (int)Mathf.Floor((uv.x + (c.r - 0.5f) * 2) * cfg.samplingMaskSize.x);
                currentPixel.y = (int)Mathf.Floor((uv.y + (c.g - 0.5f) * 2) * cfg.samplingMaskSize.y);
            }

            public override bool PEGI(BrushConfig brush, PlaytimePainter p)
            {
                bool changed = base.PEGI(brush, p);

                if (p == null)
                    return changed;

                pegi.newLine();

                changed |= "Mask Size: ".edit(60, ref cfg.samplingMaskSize).nl();

                cfg.samplingMaskSize.Clamp(1, 512);

                changed |= "Color Set On".selectEnum(ref method, typeof(ColorSetMethod)).nl();

                if (method == 2)  {
                    changed |= "CurrentPixel".edit(80, ref currentPixel).nl();

                    currentPixel.Clamp(-cfg.samplingMaskSize.max, cfg.samplingMaskSize.max * 2);
                }

                if (p != null &&  "Set Tile/Offset".Click()) {
                    p.curImgData.tiling = Vector2.one * 1.5f;
                    p.curImgData.offset = -Vector2.one * 0.25f;
                    p.UpdateTylingToMaterial();
                    changed = true;
                }

                if (p != null && "Generate Default".Click().nl())
                {
                    var img = painter.curImgData;

                    var pix = img.pixels;

                    int dx = img.width / cfg.samplingMaskSize.x;
                    int dy = img.height / cfg.samplingMaskSize.y;

                    for (currentPixel.x = 0; currentPixel.x < cfg.samplingMaskSize.x; currentPixel.x++)
                        for (currentPixel.y = 0; currentPixel.y < cfg.samplingMaskSize.y; currentPixel.y++)
                        {

                        float center_uv_x = ((float)currentPixel.x + 0.5f) / (float)cfg.samplingMaskSize.x;
                        float center_uv_y = ((float)currentPixel.y + 0.5f) / (float)cfg.samplingMaskSize.y;

                        int startX = currentPixel.x * dx;

                            for (int suby = 0; suby < dy; suby++)
                            {

                                int y = (currentPixel.y * dy + suby);
                                int start = y * img.width + startX;

                            float offy = (center_uv_y - ((float)y / (float)img.height)) / 2f + 0.5f;

                            for (int subx = 0; subx < dx; subx++) {
                                int ind = start + subx;

                                float offx = (center_uv_x - ((float)(startX+ subx) / (float)img.width)) / 2f + 0.5f;
                                
                                pix[ind].r = offx;
                                pix[ind].g = offy;
                            }
                        }

                }

                    img.SetAndApply(true);
                    if (!img.TargetIsTexture2D())
                        img.Texture2D_To_RenderTexture();

                }

            pegi.newLine();

                return changed;
            }

            public override void PrePaint(PlaytimePainter pntr, BrushConfig br, StrokeVector st) {

            var v4 = new Vector4(st.unRepeatedUV.x, st.unRepeatedUV.y, Mathf.Floor(st.unRepeatedUV.x), Mathf.Floor(st.unRepeatedUV.y));

            Shader.SetGlobalVector("_brushPointedUV_Untiled", v4);

            if (st.firstStroke)
            {
                
                    if (method == ((int)ColorSetMethod.MDownColor))
                    {
                        pntr.SampleTexture(st.uvTo);
                        fromColor(br, st.unRepeatedUV);
                    }
                    else
                    if (method == ((int)ColorSetMethod.MDownPosition))
                        fromUV(st.uvTo);
              
                Shader.SetGlobalVector(PainterConfig.BRUSH_SAMPLING_DISPLACEMENT, new Vector4(
                    ((float)currentPixel.x + 0.5f) / ((float)cfg.samplingMaskSize.x),

                    ((float)currentPixel.y + 0.5f) / ((float)cfg.samplingMaskSize.y),
                    
                    cfg.samplingMaskSize.x, cfg.samplingMaskSize.y));

            }
        }
        }

        public class BlitModeBloom : BlitMode
        {
            public override string ToString() { return "Bloom"; }
            protected override string shaderKeyword { get { return "BRUSH_BLOOM"; } }

            public override bool showColorSliders { get { return false; } }
            public override bool supportedBySingleBuffer { get { return false; } }
            public override bool supportedByTex2D { get { return false; } }

            public override Shader shaderForDoubleBuffer { get { return rt.br_BlurN_SmudgeBrush; } }

            public override bool PEGI(BrushConfig brush, PlaytimePainter p)
            {

                bool brushChanged_RT = base.PEGI(brush, p);
                pegi.newLine();
                pegi.write("Bloom Radius", 70);
                brushChanged_RT |= pegi.edit(ref brush.blurAmount, 1f, 8f);
                pegi.newLine();
                return brushChanged_RT;
            }
        }

    }