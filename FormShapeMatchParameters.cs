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
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
namespace EmguCVDemoApp
{

    

    public partial class FormShapeMatchParameters : Form
    {
        public delegate void DelegateApplyShapeMatching(Image<Bgr, byte> imgTemplate, double threshold = 0.00001,
            double area = 1000, ContoursMatchType matchType = ContoursMatchType.I2);
        public event DelegateApplyShapeMatching OnShapeMatching;


        public FormShapeMatchParameters()
        {
            InitializeComponent();
            comboBox1.SelectedIndex = 1;
            StartPosition = FormStartPosition.Manual;
            int margin = 20;
            int x = Screen.PrimaryScreen.WorkingArea.Right - Width - margin;
            int y = Screen.PrimaryScreen.WorkingArea.Top + margin;
            Location = new Point(x, y);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                if (pictureBox1.Image==null)
                {
                    throw new Exception("Select a template image.");
                }
                var img = new Bitmap(pictureBox1.Image).ToImage<Bgr, byte>();
                ContoursMatchType matchType = ContoursMatchType.I2;

                switch(comboBox1.SelectedIndex)
                {
                    case 0:
                        matchType = ContoursMatchType.I1;
                        break;
                    case 1:
                        matchType = ContoursMatchType.I2;
                        break;
                    case 2:
                        matchType = ContoursMatchType.I3;
                        break;
                }

                double areaThreshold = 1000;
                double.TryParse(tbMinArea.Text, out areaThreshold);
                double threshold = 0.001;
                double.TryParse(tbDistanceThreshold.Text, out threshold);

                if(OnShapeMatching!=null)
                {
                    OnShapeMatching(img, threshold, areaThreshold, matchType);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void submenutoolStripMenuItemOpen_Click(object sender, EventArgs e)
        {
            try
            {
                OpenFileDialog dialog = new OpenFileDialog();
                dialog.Filter = "Image Files (*.jpg;*.png;*.bmp;)|*.jpg;*.png;*.bmp;|All Files (*.*)|*.*;";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    var img = new Image<Bgr, byte>(dialog.FileName);
                    pictureBox1.Image = img.ToBitmap();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void submenutoolStripMenuItemRotate_Click(object sender, EventArgs e)
        {
            try
            {
                var img = new Bitmap(pictureBox1.Image)
                    .ToImage<Bgr, byte>()
                    .Rotate(15, new Bgr(255, 255, 255));
                pictureBox1.Image = img.ToBitmap();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void submenutoolStripMenuItemScale_Click(object sender, EventArgs e)
        {
            try
            {
                var img = new Bitmap(pictureBox1.Image)
                    .ToImage<Bgr, byte>()
                    .Resize(1.25,Inter.Cubic);
                pictureBox1.Image = img.ToBitmap();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}
