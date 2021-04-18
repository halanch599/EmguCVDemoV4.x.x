using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.Util;
using Emgu.CV.CvEnum;
namespace EmguCVDemoApp
{
    public partial class Form1 : Form
    {

        Dictionary<string, Image<Bgr, byte>> IMGDict;
        public Form1()
        {
            InitializeComponent();
            IMGDict = new Dictionary<string, Image<Bgr, byte>>();
        }

        private void toolStripMenuItemOpen_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "Image Files (*.jpg;*.png;*.bmp;)|*.jpg;*.png;*.bmp;|All Files (*.*)|*.*;";
                if (dialog.ShowDialog()==DialogResult.OK)
                {
                    var img = new Image<Bgr, byte>(dialog.FileName);
                    pictureBox1.Image = img.ToBitmap();
                    if (IMGDict.ContainsKey("input"))
                    {
                        IMGDict.Remove("input");
                    }
                    IMGDict.Add("input", img);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void toolStripMenuItemShapeMatching_Click(object sender, EventArgs e)
        {
            try
            {
                //Image<Bgr, byte> imgTemplate = new Image<Bgr, byte>(@"F:\AJ Data\img\stop.jpg");
                //ApplyShapeMatching(imgTemplate);

                if (IMGDict["input"] == null)
                {
                    throw new Exception("Select and image.");
                }

                FormShapeMatchParameters form = new FormShapeMatchParameters();
                form.OnShapeMatching += ApplyShapeMatching;
                form.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ApplyShapeMatching(Image<Bgr, byte> imgTemplate, double threshold = 0.00001,
            double area=1000, ContoursMatchType matchType = ContoursMatchType.I2)
        {
            try
            {
               
                var img = IMGDict["input"].Clone();
                var imgSource = img.Convert<Gray, byte>()
                    .SmoothGaussian(3)
                    .ThresholdBinaryInv(new Gray(240), new Gray(255));

                var imgTarget = imgTemplate.Convert<Gray, byte>()
                    .SmoothGaussian(3)
                    .ThresholdBinaryInv(new Gray(240), new Gray(255));

                var imgSourceContours = CalculateContours(imgSource, area);
                var imgTargetContours = CalculateContours(imgTarget, area);

                if(imgSourceContours.Size==0 || imgTargetContours.Size==0)
                {
                    throw new Exception("Not engough contours.");
                }

                for (int i = 0; i < imgSourceContours.Size; i++)
                {
                    var distance = CvInvoke.MatchShapes(imgSourceContours[i], imgTargetContours[0]
                        , matchType);

                    if(distance<=threshold)
                    {
                        var rect = CvInvoke.BoundingRectangle(imgSourceContours[i]);
                        img.Draw(rect, new Bgr(0, 255, 0), 4);
                        CvInvoke.PutText(img, distance.ToString("F6"), new Point(rect.X, rect.Y + 20),
                            Emgu.CV.CvEnum.FontFace.HersheyPlain, 3, new MCvScalar(255, 0, 0));
                    }
                }

                pictureBox1.Image = img.ToBitmap();

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private VectorOfVectorOfPoint CalculateContours(Image<Gray, byte> img, double thresholdarea = 1000)
        {
            try
            {
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                Mat h = new Mat();

                CvInvoke.FindContours(img, contours, h, Emgu.CV.CvEnum.RetrType.External,
                    Emgu.CV.CvEnum.ChainApproxMethod.ChainApproxSimple);

                VectorOfVectorOfPoint filteredContours = new VectorOfVectorOfPoint();
                for (int i = 0; i < contours.Size; i++)
                {
                    var area = CvInvoke.ContourArea(contours[i]);
                    if (area>=thresholdarea)
                    {
                        filteredContours.Push(contours[i]);
                    }
                }
                return filteredContours;
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void COVID19Test_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IMGDict.ContainsKey("input"))
                {
                    throw new Exception("Selct an image first.");
                }

                var img = IMGDict["input"].SmoothGaussian(3);
                var edges = img.Convert<Gray, byte>().Canny(150, 50);
                Mat morphology = new Mat();
                CvInvoke.MorphologyEx(edges, morphology, MorphOp.Close, Mat.Ones(5, 5, DepthType.Cv8U, 1),
                    new Point(-1, -1), 3, BorderType.Default, new MCvScalar(0));

                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                Mat h = new Mat();
                CvInvoke.FindContours(morphology, contours, h, RetrType.Tree, ChainApproxMethod.ChainApproxSimple);

                var preprocessed = edges.CopyBlank();
                var data = h.GetData();
                for (int r = 0; r < data.GetLength(0); r++)
                {
                    for (int c = 0; c < data.GetLength(1); c++)
                    {
                        if ((((int)data.GetValue(r, c, 2)))==-1
                            && (((int)data.GetValue(r, c, 3)) > -1))
                        {
                            var bbox = CvInvoke.BoundingRectangle(contours[c]);
                            var AR = bbox.Width / (float)bbox.Height;
                            if (AR<=2.0)
                            {
                                CvInvoke.DrawContours(preprocessed, contours, c, new MCvScalar(255), -1);
                            }
                        }
                    }
                }

                var output1 = edges.CopyBlank();
                CvInvoke.Dilate(preprocessed, output1, Mat.Ones(10, 1, DepthType.Cv8U, 1), new Point(-1, -1),
                    1, BorderType.Default, new MCvScalar(0));

                contours.Clear();
                CvInvoke.FindContours(output1, contours, h, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                var finaloutput = edges.CopyBlank();
                for (int i = 0; i < contours.Size; i++)
                {
                    var bbox = CvInvoke.BoundingRectangle(contours[i]);
                    if (bbox.Height>(bbox.Width*3))
                    {
                        CvInvoke.DrawContours(finaloutput, contours, i, new MCvScalar(255), -1);
                        preprocessed.ROI = bbox;
                        int count = CountContours(preprocessed);
                        preprocessed.ROI = Rectangle.Empty;
                        string msg = "";
                        MCvScalar color;
                        if (count==2)
                        {
                            msg = "Negative";
                            color = new MCvScalar(0, 255, 0);
                        }
                        else
                        {
                            msg = "Positive";
                            color = new MCvScalar(0, 0, 255);
                        }
                        int margin = 50;
                        CvInvoke.PutText(img, msg, new(bbox.X-margin, bbox.Y-margin), FontFace.HersheyPlain, 2.5, color, 3);
                    }
                }

                pictureBox1.Image = img.ToBitmap();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private int CountContours(Image<Gray, byte> img)
        {
            try
            {
                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                CvInvoke.FindContours(img, contours, null, RetrType.External, ChainApproxMethod.ChainApproxSimple);
                return contours.Size;
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }
    }
}
