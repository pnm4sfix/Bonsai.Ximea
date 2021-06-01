﻿using Bonsai;
using OpenCV.Net;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Reactive.Disposables;
using System.Threading.Tasks;
using xiApi.NET;


namespace Bonsai.Ximea
{
    [Description("Generates a sequence of images acquired from the specified PSEye camera.")]
    [Combinator(MethodName = nameof(Generate))]
    [WorkflowElementCategory(ElementCategory.Source)]
    public class XimeaSource: Source<IplImage>
    {
        readonly object captureLock = new object();
        IObservable<IplImage> source;
        IntPtr camera;
        xiCam myCam = new xiCam();
        IplImage image;
        IplImage output;

        int gain;
        int exposure;
        int whiteBalanceRed;
        int whiteBalanceGreen;
        int whiteBalanceBlue;
        bool autoGain;
        bool autoExposure;
        int autoWhiteBalance;

        public XimeaSource()
        {
            //ColorMode = CLEyeCameraColorMode.CLEYE_COLOR_RAW;
            //Resolution = CLEyeCameraResolution.CLEYE_VGA;
            FrameRate = 60;

            //AutoWhiteBalance = true;
            Exposure = 511;

            source = Observable.Create<IplImage>((observer, cancellationToken) =>
            {
                return Task.Factory.StartNew(() =>
                {
                    lock (captureLock)
                    {
                        Load();
                        try
                        {
                            while (!cancellationToken.IsCancellationRequested)
                            {

                                var frameBmp = frm.Bitmap; // this is the bitmap from that object

                                Frame = new IplImage(frameBmp.Width, frameBmp.Height, BitDepth.U8, 3);  //creates the OpenCvSharp IplImage;
                                Frame.CopyFrom(frameBmp); // copies the bitmap data to the IplImage
                                myCam.GetImage(out output, 100);
                                //if (CLEye.CLEyeCameraGetFrame(camera, image.ImageData, 500))
                                //{
                                //    if (image.Channels == 4)
                                //   {
                                //        CV.CvtColor(image, output, ColorConversion.Bgra2Bgr);
                                //    }

                                observer.OnNext(output.Clone());
                                
                            }
                        }
                        finally { Unload(); }
                    }
                },
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
            })
            .PublishReconnectable()
            .RefCount();
        }
        //[TypeConverter(typeof(CameraGuidConverter))]
        //[Description("The optional GUID used to uniquely identify the acquisition camera.")]
        //public Guid? CameraGuid { get; set; }

        [Description("The camera index used to find a camera, in case no GUID is specified.")]
        public int CameraIndex { get; set; }

        //[Description("The camera color processing mode.")]
        //public CLEyeCameraColorMode ColorMode { get; set; }

        //[Description("The camera acquisition resolution.")]
        //public CLEyeCameraResolution Resolution { get; set; }

        [Description("The frame rate at which to acquire image frames.")]
        public float FrameRate { get; set; }

        //[Description("Indicates whether auto gain calibration should be used.")]
        //public bool AutoGain
        //{
        //    get { return autoGain; }
        //    set
        //    {
        //        autoGain = value;
        //        if (camera != IntPtr.Zero)
        //        {
        //            CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_AUTO_GAIN, value ? 1 : 0);
        //            if (!autoGain) Gain = gain;
        //       }
        //    }
        //}

        //[Description("Indicates whether auto exposure should be used.")]
        //public bool AutoExposure
        //{
        //   get { return autoExposure; }
        //    set
        //    {
        //        autoExposure = value;
        //        if (camera != IntPtr.Zero)
        //        {
        //            CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_AUTO_EXPOSURE, value ? 1 : 0);
         //           if (!autoExposure) Exposure = exposure;
         //       }
         //   }
        //}

        [Description("Indicates whether auto white balance calibration should be used.")]
        public int AutoWhiteBalance
        {
            get { return autoWhiteBalance; }
            set
            {
                autoWhiteBalance = value;
                //if (camera != IntPtr.Zero)
                //{
                //    CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_AUTO_WHITEBALANCE, value ? 1 : 0);
                myCam.SetParam(PRM.AUTO_WB, value);
                if (autoWhiteBalance==0)
                {
                    WhiteBalanceRed = whiteBalanceRed;
                    WhiteBalanceGreen = whiteBalanceGreen;
                    WhiteBalanceBlue = whiteBalanceBlue;
                }
                
            }
        }

        [Range(0, 79)]
        [Description("The fixed gain value, used when auto gain is disabled.")]
        [Editor(DesignTypes.SliderEditor, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public int Gain
        {
            get { return gain; }
            set
            {
                gain = value;
                //if (camera != IntPtr.Zero)
                //{
                //    CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_GAIN, value);
                //}
                myCam.SetParam(PRM.GAIN, value);
            }
        }

        [Range(0, 5110)]
        [Description("The fixed exposure value, used when auto exposure is disabled.")]
        [Editor(DesignTypes.SliderEditor, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public int Exposure
        {
            get { return exposure; }
            set
            {
                exposure = value;
                //if (camera != IntPtr.Zero)
                //{
                //    CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_EXPOSURE, value);
                //}
                myCam.SetParam(PRM.EXPOSURE, value);
            }
        }

        [Range(0, 255)]
        [Description("The fixed white balance value for the red channel, used when auto white balance is disabled.")]
        [Editor(DesignTypes.SliderEditor, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public int WhiteBalanceRed
        {
            get { return whiteBalanceRed; }
            set
            {
                whiteBalanceRed = value;
                //if (camera != IntPtr.Zero)
                //{
                //    CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_RED, value);
                //}
                myCam.SetParam(PRM.WB_KR, value);
            }
        }

        [Range(0, 255)]
        [Description("The fixed white balance value for the green channel, used when auto white balance is disabled.")]
        [Editor(DesignTypes.SliderEditor, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public int WhiteBalanceGreen
        {
            get { return whiteBalanceGreen; }
            set
            {
                whiteBalanceGreen = value;
                //if (camera != IntPtr.Zero)
                //{
                //CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_GREEN, value);
                //}
                myCam.SetParam(PRM.WB_KG, value);
            }
        }

        [Range(0, 255)]
        [Description("The fixed white balance value for the blue channel, used when auto white balance is disabled.")]
        [Editor(DesignTypes.SliderEditor, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public int WhiteBalanceBlue
        {
            get { return whiteBalanceBlue; }
            set
            {
                whiteBalanceBlue = value;
                //if (camera != IntPtr.Zero)
                //{
                myCam.SetParam(PRM.WB_KB, value);
                //CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_BLUE, value);
                //}
            }
        }

        private void Load()
        {
            myCam.OpenDevice(CameraIndex);


            
            myCam.SetParam(PRM.EXPOSURE, exposure);
            // Set device gain to 5 decibels
            float gain_db = 5;
            myCam.SetParam(PRM.GAIN, gain_db);

            // Set image output format to monochrome 8 bit
            myCam.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.MONO8);

            //Start acquisition
            myCam.StartAcquisition();

            //var guid = CameraGuid;
            //if (!guid.HasValue)
            //{
            //   guid = myCam.OpenDevice(0);
            //    if (guid == Guid.Empty)
            //    {
            //        throw new InvalidOperationException("No camera found with the given index.");
            //    }
            //}

            //camera = CLEye.CLEyeCreateCamera(guid.Value, ColorMode, Resolution, FrameRate);
            //if (myCam == IntPtr.Zero) //change this to if xicam not found
            //{
            //    throw new InvalidOperationException("No camera found with the given GUID.");
            //}

            //AutoGain = autoGain;
            A//utoExposure = autoExposure;
            AutoWhiteBalance = autoWhiteBalance;
            Gain = gain;
            Exposure = exposure;
            WhiteBalanceRed = whiteBalanceRed;
            WhiteBalanceGreen = whiteBalanceGreen;
            WhiteBalanceBlue = whiteBalanceBlue;

            int width, height;
            myCam.GetParam(PRM.HEIGHT, out height);
            myCam.GetParam(PRM.WIDTH, out width);
            //CLEye.CLEyeCameraGetFrameDimensions(camera, out width, out height);

            //switch (ColorMode)
            //{
            //    case CLEyeCameraColorMode.CLEYE_COLOR_RAW:
            //    case CLEyeCameraColorMode.CLEYE_COLOR_PROCESSED:
            //        image = new IplImage(new Size(width, height), IplDepth.U8, 4);
            //        output = new IplImage(image.Size, IplDepth.U8, 3);
            //        break;
            //    case CLEyeCameraColorMode.CLEYE_MONO_RAW:
            //    case CLEyeCameraColorMode.CLEYE_MONO_PROCESSED:
            //        image = new IplImage(new Size(width, height), IplDepth.U8, 1);
            //        output = image;
            //        break;
            //}

            //if (!CLEye.CLEyeCameraStart(camera))
            //{
            //    throw new InvalidOperationException("Unable to start camera.");
            //}
        }

        private void Unload()
        {
            //CLEye.CLEyeCameraStop(camera);
            //CLEye.CLEyeDestroyCamera(camera);
            //camera = IntPtr.Zero;

            myCam.StopAcquisition();
            myCam.CloseDevice();
        }

        public override IObservable<IplImage> Generate()
        {
            return source;
        }

        //class CameraGuidConverter : GuidConverter
        //{
        //    public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        //    {
        //        return true;
        //    }
        //
        //    public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        //    {
        //        var cameraGuids = new List<Guid>();
        //        var cameraCount = CLEye.CLEyeGetCameraCount();
        //        for (int i = 0; i < cameraCount; i++)
        //        {
        //            cameraGuids.Add(CLEye.CLEyeGetCameraUUID(i));
        //        }
        //
        //        return new StandardValuesCollection(cameraGuids);
        //    }
        //}

    }
}