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
using Emgu.CV.Dnn;

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

        private void toolStripMenuItemPregnancyTest_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IMGDict.ContainsKey("input"))
                {
                    throw new Exception("Read an image");
                }

                double threshold = 300;

                var img = IMGDict["input"].Clone().SmoothGaussian(3);
                var binary = img.Convert<Gray, byte>()
                    .ThresholdBinaryInv(new Gray(240), new Gray(255));

                VectorOfVectorOfPoint contours = new VectorOfVectorOfPoint();
                VectorOfVectorOfPoint filteredContours = new VectorOfVectorOfPoint();
                Mat hierarchy = new Mat();

                CvInvoke.FindContours(binary, contours, hierarchy, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                var output = binary.CopyBlank();

                for (int i = 0; i < contours.Size; i++)
                {
                    var area = CvInvoke.ContourArea(contours[i]);
                    if (area>threshold)
                    {
                        filteredContours.Push(contours[i]);
                        //CvInvoke.DrawContours(output, contours, i, new MCvScalar(255), 2);
                    }
                }

                for (int i = 0; i < filteredContours.Size; i++)
                {
                    var bbox = CvInvoke.BoundingRectangle(filteredContours[i]);
                    binary.ROI = bbox;
                    var rects = ProcessParts(binary);
                    binary.ROI = Rectangle.Empty;

                    int count = rects.Count;

                    string msg = "";
                    int marin = 25;
                    MCvScalar color = new MCvScalar(0, 255, 0);

                    switch (count)
                    {
                        case 1:
                            msg = "Invalid";
                            color = new MCvScalar(0, 0, 255);
                            break;

                        case 2:
                            if (rects[0].Width*rects[0].Height < rects[1].Width * rects[1].Height)
                            {
                                msg = "Not Pregnant";
                            }
                            else
                            {
                                msg = "Invalid";
                            }
                            color = new MCvScalar(0, 0, 255);
                            break;
                        case 3:
                            msg = "Pregnant";
                            color = new MCvScalar(0, 255, 0);
                            break;

                        default:
                            msg = "Invalid/Not pregnant";
                            color = new MCvScalar(0, 0, 255);
                            break;
                    }
                    CvInvoke.PutText(img, msg, new Point(bbox.X + bbox.Width + marin, bbox.Y + marin),
                        FontFace.HersheyPlain, 1.5, color, 2);
                }
                // add Cv.Binary
                pictureBox1.Image = img.ToBitmap();
                

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private List<Rectangle> ProcessParts(Image<Gray, byte> img)
        {
            try
            {
                double areaThreshold = 200;
                Rectangle rectangle = Rectangle.Empty;
                img._Not();

                var contours = new VectorOfVectorOfPoint();
                var h = new Mat();
                CvInvoke.FindContours(img, contours, h, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                List<Rectangle> bboxes = new List<Rectangle>();

                for (int i = 0; i < contours.Size; i++)
                {
                    var area = CvInvoke.ContourArea(contours[i]);
                    if (area>areaThreshold)
                    {
                        bboxes.Add(CvInvoke.BoundingRectangle(contours[i]));
                    }
                }

                var sortedBboxes = bboxes.OrderBy(b => b.X).ToList();

                if (sortedBboxes.Count>2)
                {
                    sortedBboxes.RemoveRange(sortedBboxes.Count - 2, 2);
                }
                else
                {
                    sortedBboxes.Clear();
                }
                return sortedBboxes;
            }
            catch (Exception ex)
            {

                throw new Exception(ex.Message);
            }
        }

        private void TextLineSegmentation_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IMGDict.ContainsKey("input"))
                {
                    throw new Exception("Please read an image.");
                }
                var img = IMGDict["input"].Clone();
                var rotatedImage = deskewImage(img);

                (var img2, var bboxes) = detectTextSegments(rotatedImage);

                var kernel = Mat.Ones(3, 3, DepthType.Cv8U, 1);
                var mask = rotatedImage[0].CopyBlank();
                var imgoutput = rotatedImage.CopyBlank() + 255;
                int counter = 0;
                foreach (var box in bboxes)
                {
                    mask.Draw(box, new Gray(255), -1);
                    rotatedImage.ROI = box;
                    (var img3, var wordboxes) = detectTextSegments(rotatedImage, kernel);
                    img3.ROI = Rectangle.Empty;
                    rotatedImage.ROI = Rectangle.Empty;
                    imgoutput._And(img3);
                    counter += wordboxes.Count;
                }

                CvInvoke.PutText(imgoutput, "Words = " + counter.ToString(), new Point(10, 20), FontFace.HersheyPlain,
                    1.5, new MCvScalar(0, 255, 0), 2);
                pictureBox1.Image = imgoutput.ToBitmap();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private (Image<Bgr, byte>, List<Rectangle>) detectTextSegments(Image<Bgr, byte> img, Mat kernel = null)
        {
            try
            {
                if (kernel == null)
                {
                    kernel = Mat.Ones(3, 9, DepthType.Cv8U, 1);
                }

                var binary = img.Convert<Gray, byte>()
                    .ThresholdBinaryInv(new Gray(240), new Gray(255))
                    .MorphologyEx(MorphOp.Dilate, kernel, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(255));

                var temp = img.Clone();

                var conours = new VectorOfVectorOfPoint();
                var h = new Mat();
                CvInvoke.FindContours(binary, conours, h, RetrType.External, ChainApproxMethod.ChainApproxSimple);

                var bboxes = new List<Rectangle>();
                for (int i = 0; i < conours.Size; i++)
                {
                    var bbox = CvInvoke.BoundingRectangle(conours[i]);
                    bboxes.Add(bbox);
                    CvInvoke.Rectangle(temp, bbox, new MCvScalar(0, 0, 255), 2);
                }
                bboxes = bboxes.OrderBy(x => x.Y).ToList();
                return (temp, bboxes);
            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }

        }
        private Image<Bgr, byte> deskewImage(Image<Bgr, byte> img)
        {
            try
            {
                var SE = Mat.Ones(15, 15, DepthType.Cv8U, 1);
                var binary = img.Convert<Gray, byte>()
                    .SmoothGaussian(3)
                    .ThresholdBinaryInv(new Gray(240), new Gray(255))
                    .MorphologyEx(MorphOp.Dilate, SE, new Point(-1, -1), 1, BorderType.Default, new MCvScalar(0))
                    .Erode(1);

                var points = new VectorOfPoint();
                CvInvoke.FindNonZero(binary, points);
                var minAreaRect = CvInvoke.MinAreaRect(points);

                var rotationMatrix = new Mat(2, 3, DepthType.Cv32F, 1);
                var rotatedImage = img.CopyBlank();

                //var corners = CvInvoke.BoxPoints(minAreaRect).Select(x => new Point((int)x.X, (int)x.Y)).ToArray();

                //for (int i = 0; i < corners.Length; i++)
                //{
                //    CvInvoke.Line(img, corners[i], corners[(i + 1)%4], new MCvScalar(0, 255, 0), 2);
                //    CvInvoke.PutText(img, i.ToString(), corners[i], FontFace.HersheySimplex, 1.0, new MCvScalar(0, 0, 255), 3);
                //    CvInvoke.Circle(img, corners[i], 3, new MCvScalar(255, 0, 0), 5);
                //}

                var angle = minAreaRect.Angle < 45 ? minAreaRect.Angle : minAreaRect.Angle - 90;

                CvInvoke.GetRotationMatrix2D(minAreaRect.Center, angle, 1.0, rotationMatrix);
                CvInvoke.WarpAffine(img, rotatedImage, rotationMatrix, img.Size,borderMode:BorderType.Replicate);

                //pictureBox1.Image = rotatedImage.ToBitmap();
                return rotatedImage;

            }
            catch (Exception ex)
            {
                throw new Exception(ex.Message);
            }
        }

        private void PoseEstimationBody_25_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IMGDict.ContainsKey("input"))
                {
                    throw new Exception("Please read in image first.");
                }


                // for openopse
                int inWidth = 368;
                int inHeight = 368;
                float threshold = 0.1f;
                int nPoints = 25;

                var BODY_PARTS = new Dictionary<string, int>()
                {
                    { "Nose", 0 },
                    { "Neck", 1 },
                    { "RShoulder", 2 },
                    { "RElbow", 3 },
                    { "RWrist", 4 },
                    {"LShoulder",5},
                    { "LElbow", 6 },
                    { "LWrist", 7 },
                    { "MidHip", 8 },
                    { "RHip", 9 },
                    { "RKnee", 10 },
                    {"RAnkle",11},
                    { "LHip", 12 },
                    { "LKnee", 13 },
                    { "LAnkle", 14 },
                    { "REye", 15 },
                    { "LEye", 16 },
                    {"REar",17},
                    { "LEar", 18 },
                    { "LBigToe", 19 },
                    { "LSmallToe", 20 },
                    { "LHeel", 21 },
                    { "RBigToe", 22 },
                    {"RSmallToe",23},
                    { "RHeel", 24 },
                    { "Background", 25 }
                };

                int[,] point_pairs = new int[,]{
                            {1, 0}, {1, 2}, {1, 5},
                            {2, 3}, {3, 4}, {5, 6},
                            {6, 7}, {0, 15}, {15, 17},
                            {0, 16}, {16, 18}, {1, 8},
                            {8, 9}, {9, 10}, {10, 11},
                            {11, 22}, {22, 23}, {11, 24},
                            {8, 12}, {12, 13}, {13, 14},
                            {14, 19}, {19, 20}, {14, 21}};


                // Load the caffe Model
                string prototxt = @"F:\openpose\models\pose\body_25\pose_deploy.prototxt";
                string modelPath = @"F:\openpose\models\pose\body_25\pose_iter_584000.caffemodel";

                var net = DnnInvoke.ReadNetFromCaffe(prototxt, modelPath);

                var img = IMGDict["input"].Clone();
                var imgHeight = img.Height;
                var imgWidth = img.Width;

                var blob = DnnInvoke.BlobFromImage(img, 1.0 / 255.0, new Size(inWidth, inHeight), new MCvScalar(0, 0, 0));
                net.SetInput(blob);
                net.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);

                var output = net.Forward();

                var H = output.SizeOfDimension[2];
                var W = output.SizeOfDimension[3];
                var HeatMap = output.GetData();

                var points = new List<Point>();

                for (int i = 0; i < nPoints; i++)
                {
                    Matrix<float> matrix = new Matrix<float>(H, W);
                    for (int row = 0; row < H; row++)
                    {
                        for (int col = 0; col < W; col++)
                        {
                            matrix[row, col] = (float)HeatMap.GetValue(0, i, row, col);
                        }
                    }

                    double minVal = 0, maxVal = 0;
                    Point minLoc = default, maxLoc = default;

                    CvInvoke.MinMaxLoc(matrix, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                    var x = (img.Width * maxLoc.X) / W;
                    var y = (img.Height * maxLoc.Y) / H;

                    if (maxVal>threshold)
                    {
                        points.Add(new Point(x, y));
                    }
                    else
                    {
                        points.Add(Point.Empty);
                    }
                }

                // display points on image

                for (int i = 0; i < points.Count; i++)
                {
                    var p = points[i];
                    if (p!=Point.Empty)
                    {
                        CvInvoke.Circle(img, p, 5, new MCvScalar(0, 255, 0), -1);
                        CvInvoke.PutText(img, i.ToString(), p, FontFace.HersheySimplex, 0.8, new MCvScalar(0, 0, 255), 1, LineType.AntiAlias);
                    }
                }

                // draw skeleton

                for (int i = 0; i < point_pairs.GetLongLength(0); i++)
                {
                    var startIndex = point_pairs[i, 0];
                    var endIndex = point_pairs[i, 1];

                    if (points.Contains(points[startIndex]) && points.Contains(points[endIndex]))
                    {
                        CvInvoke.Line(img, points[startIndex], points[endIndex], new MCvScalar(255, 0, 0), 2);
                    }
                }
                pictureBox1.Image = img.ToBitmap();

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void HandPoseEstimation_Click(object sender, EventArgs e)
        {
            try
            {
                if (!IMGDict.ContainsKey("input"))
                {
                    throw new Exception("Read an image first.");
                }


                var img = IMGDict["input"].Clone().SmoothGaussian(3);
                var blob = DnnInvoke.BlobFromImage(img, 1.0 / 255.0, new Size(368, 368), new MCvScalar(0, 0, 0));

                string prototxt = @"F:\openpose\models\hand\pose_deploy.prototxt";
                string modelpath = @"F:\openpose\models\hand\pose_iter_102000.caffemodel";

                var net = DnnInvoke.ReadNetFromCaffe(prototxt, modelpath);

                net.SetInput(blob);
                net.SetPreferableBackend(Emgu.CV.Dnn.Backend.OpenCV);

                var output = net.Forward();

                var H = output.SizeOfDimension[2];
                var W = output.SizeOfDimension[3];

                var probMap = output.GetData();

                int nPoints = 22;
                int[,] POSE_PAIRS = new int[,] { { 0, 1 }, { 1, 2 }, { 2, 3 }, { 3, 4 }, { 0, 5 }, { 5, 6 }, { 6, 7 }, 
                    { 7, 8 }, { 0, 9 }, { 9, 10 }, { 10, 11 }, { 11, 12 }, { 0, 13 }, { 13, 14 }, { 14, 15 }, { 15, 16 }, 
                    { 0, 17 }, { 17, 18 }, { 18, 19 }, { 19, 20 } };

                var points = new List<Point>();

                for (int i = 0; i < nPoints; i++)
                {
                    Matrix<float> matrix = new Matrix<float>(H, W);
                    for (int row = 0; row < H; row++)
                    {
                        for (int col = 0; col < W; col++)
                        {
                            matrix[row, col] = (float)probMap.GetValue(0, i, row, col);
                        }
                    }


                    double minVal=0, maxVal=0;
                    Point minLoc = default, maxLoc = default;
                    CvInvoke.MinMaxLoc(matrix, ref minVal, ref maxVal, ref minLoc, ref maxLoc);

                    var x = (img.Width * maxLoc.X) / W;
                    var y = (img.Height * maxLoc.Y) / H;

                    var p = new Point(x, y);
                    points.Add(p);
                    CvInvoke.Circle(img, p, 5, new MCvScalar(0, 255, 0), -1);
                    CvInvoke.PutText(img, i.ToString(), p, FontFace.HersheySimplex, 0.5, new MCvScalar(0, 0, 255), 2);
                }


                // draw skeleton

                for (int i = 0; i < POSE_PAIRS.GetLongLength(0); i++)
                {
                    var startIndex = POSE_PAIRS[i, 0];
                    var endIndex = POSE_PAIRS[i, 1];

                    if (points.Contains(points[startIndex]) && points.Contains(points[endIndex]))
                    {
                        CvInvoke.Line(img, points[startIndex], points[endIndex], new MCvScalar(255, 0, 0), 2);
                    }
                }
                //CV.BitMap
                pictureBox1.Image = img.ToBitmap();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
