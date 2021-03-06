using Bonsai;
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
using System.Drawing;
using System.Windows.Media.Imaging;

using xiApi.NET;
 

namespace Bonsai.Ximea
{
    [Description("Generates a sequence of images acquired from the specified Ximea camera.")]
    [Combinator(MethodName = nameof(Generate))]
    [WorkflowElementCategory(ElementCategory.Source)]
    public class XimeaSource: Source<IplImage>
    {
        readonly object captureLock = new object();
        IObservable<IplImage> source;
        IntPtr camera;
        xiCam myCam = new xiCam();
        Bitmap frame;
        //IplImage image = new IplImage(frame.Width, frame.Height, frame.depth, 1);
        IplImage output;
        //Array array;
        //OpenCV.Net.Arr arr;
        public int width, height;
        int gain;
        float framerate;
        int exposure;
        int whiteBalanceRed;
        int whiteBalanceGreen;
        int whiteBalanceBlue;
        bool autoGain;
        bool autoExposure;
        int autoWhiteBalance;
        
        bool deviceOpen = false;

        public XimeaSource()
        {
            //ColorMode = CLEyeCameraColorMode.CLEYE_COLOR_RAW;
            //Resolution = CLEyeCameraResolution.CLEYE_VGA;
            FrameRate = 60;

            //AutoWhiteBalance = true;
            Exposure = 1226;

            OffsetX = 432;
            OffsetY = 40;

            ROIWidth = 1000;
            ROIHeight = 988;



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

                                
                                //var frameBmp = frm.Bitmap; // this is the bitmap from that object

                                
                                myCam.GetImage(out frame, 1000);
                                // Lock the bitmap's bits. 
                                Rectangle rect = new Rectangle(0, 0, frame.Width, frame.Height);
                                System.Drawing.Imaging.BitmapData imgData = frame.LockBits
                                (rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, frame.PixelFormat);

                                IntPtr ptr = imgData.Scan0;

                                

                                OpenCV.Net.Size outSize = new OpenCV.Net.Size(frame.Width, frame.Height);
                                output = new IplImage(outSize, OpenCV.Net.IplDepth.U8, 1, ptr);
                                //output = new IplImage(frame.Width, frame.Height, BitDepth.U8, 1);
                                //Frame = new IplImage(frame.Width, frame.Height, BitDepth.U8, 3);  //creates the OpenCvSharp IplImage;
                                //output.
                                //output.CopyFrom(frame); // copies the bitmap data to the IplImage
                                //if (CLEye.CLEyeCameraGetFrame(camera, image.ImageData, 500))
                                //{
                                //    if (image.Channels == 4)
                                //   {
                                //        CV.CvtColor(image, output, ColorConversion.Bgra2Bgr);
                                //    }
                                //frame.CopyPixels(array, 1, 0);
                                //OpenCV.Net.CV.ConvertImage(array, output);
                                observer.OnNext(output.Clone());
                                frame.UnlockBits(imgData);
                                //array.CopyTo(arr, 0);


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

        //[Description("The camera index used to find a camera, in case no GUID is specified.")]
        //public int CameraIndex { get; set; }


        [Range(0, 500)]
        [Description("The frame rate at which to acquire image frames.")]
        [Editor(DesignTypes.SliderEditor, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public float FrameRate 
        { 
            get { return framerate; }

            set 
            {
                framerate = value;
                if (deviceOpen)
                {
                    myCam.SetParam(PRM.FRAMERATE, value);
                }
            } }



        [Description("The ROI width at which to acquire image frames.")]
        public int ROIWidth { get; set; }

        [Description("The frame height at which to acquire image frames.")]
        public int ROIHeight { get; set; }

        [Description("The ROI offset in X axis at which to acquire image frames.")]
        public int OffsetX { get; set; }

        [Description("The ROI offset in Y axis at which to acquire image frames.")]
        public int OffsetY { get; set; }

        [Range(0, 79)]
        [Description("The fixed gain value, used when auto gain is disabled.")]
        [Editor(DesignTypes.SliderEditor, "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public int Gain
        {
            get { return gain; }
            set
            {
                gain = value;
                if (deviceOpen)
                {
                    myCam.SetParam(PRM.GAIN, value);
                    //CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_GAIN, value);
                }
                
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
              if (deviceOpen)
              {
                    //    CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_EXPOSURE, value);
                    myCam.SetParam(PRM.EXPOSURE, (Int32)value);
              }
                
                
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
                if (deviceOpen)
                {
                    //    CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_RED, value);
                    myCam.SetParam(PRM.WB_KR, value);
                }
                    
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
                if (deviceOpen)
                {
                    //CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_GREEN, value);
                    myCam.SetParam(PRM.WB_KG, value);
                }
                
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
                if (deviceOpen)
                {
                  myCam.SetParam(PRM.WB_KB, value);
                //CLEye.CLEyeSetCameraParameter(camera, CLEyeCameraParameter.CLEYE_WHITEBALANCE_BLUE, value);
                }
            }
        }

        private void Load()
        {
            int numDevices = 0;
            myCam.GetNumberDevices(out numDevices);

            if (0 == numDevices)
            {
                Console.WriteLine("No devices found");
                Thread.Sleep(3000);
                return;
            }
            else
            {
                Console.WriteLine("Found {0} connected devices.", numDevices);
            }
            myCam.OpenDevice(0);

            deviceOpen = true;
            

            myCam.SetParam(PRM.EXPOSURE, Exposure);
            // Set device gain to 5 decibels
            //float gain_db = 0;
            myCam.SetParam(PRM.GAIN, Gain);

            // Set image output format to monochrome 8 bit
            myCam.SetParam(PRM.IMAGE_DATA_FORMAT, IMG_FORMAT.MONO8);
            //myCam.SetParam(PRM.BUFFER_POLICY, BUFF_POLICY.SAFE);
            myCam.SetParam(PRM.ACQ_TIMING_MODE, 1);
            myCam.SetParam(PRM.FRAMERATE, FrameRate);
            myCam.SetParam(PRM.WIDTH, ROIWidth);
            myCam.SetParam(PRM.HEIGHT, ROIHeight);
            myCam.SetParam(PRM.OFFSET_X, OffsetX);
            myCam.SetParam(PRM.OFFSET_Y, OffsetY);
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
            //AutoExposure = autoExposure;
            //AutoWhiteBalance = autoWhiteBalance;
            //Gain = gain;
            //Exposure = exposure;
            //WhiteBalanceRed = whiteBalanceRed;
            //WhiteBalanceGreen = whiteBalanceGreen;
            WhiteBalanceBlue = whiteBalanceBlue;

            
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
            deviceOpen = false;
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
