using System;
using System.Drawing;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace HitboxAssigner
{

    public partial class Form1 : Form
    {
        float spriteFrameWidth;
        float spriteFrameHeight;

        Image loadedSprite;
        int noFrames;
        int currentFrame;

        Bitmap croppedImage;

        Point startPos;      // mouse-down position
        Point currentPos;    // current mouse position
        bool drawing;        // busy drawing

        HitboxInformation information = new HitboxInformation();

        List<Box> hitboxFrameInfo = new List<Box>();
        List<Box> hurtboxFrameInfo = new List<Box>();

        List<Box> selectedHitboxes = new List<Box>();
        List<Box> selectedHurtboxes = new List<Box>();

        int zoomLevel;
        int zoomLev;

        string spriteName;

        FrameNo frameNoForm = new FrameNo();


        public Form1()
        {
            InitializeComponent();
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            saveFileDialog1.Filter = "Javascript Object Notation (*.json)|*.json";
            saveFileDialog1.DefaultExt = "json";
            saveFileDialog1.AddExtension = true;

            textBox1.ReadOnly = true;


            pictureBox1.MouseDown += new MouseEventHandler(MouseDownPictureBox);
            pictureBox1.MouseUp += new MouseEventHandler(MouseUpPictureBox);
            pictureBox1.MouseMove += new MouseEventHandler(MouseMovePictureBox);
            pictureBox1.Paint += new PaintEventHandler(PaintOnPictureBox);

            information.HitBoxes = new List<Box>();
            information.HurtBoxes = new List<Box>();
        }


        private Rectangle getRectangle()
        {
            return new Rectangle(
                Math.Min(startPos.X, currentPos.X),
                Math.Min(startPos.Y, currentPos.Y),
                Math.Abs(startPos.X - currentPos.X),
                Math.Abs(startPos.Y - currentPos.Y));
        }

        private Rectangle getImageRectangle()
        {
            PropertyInfo pInfo = pictureBox1.GetType().GetProperty("ImageRectangle",
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic |
            System.Reflection.BindingFlags.Instance);

           return (Rectangle)pInfo.GetValue(pictureBox1, null);
        }

        private void UpdatePictureBox()
        {
            Rectangle croppedSize = new Rectangle((int)spriteFrameWidth * currentFrame, 0, (int)spriteFrameWidth, (int)spriteFrameHeight);

            Bitmap bmpImage = new Bitmap(loadedSprite);
            croppedImage = (Bitmap)bmpImage.Clone(croppedSize, bmpImage.PixelFormat);

            pictureBox1.Image = croppedImage;
            pictureBox1.SizeMode = PictureBoxSizeMode.Normal;

            ZoomOnImage();
        }
    
       private void loadSprtesheetToolStripMenuItem_Click(object sender, EventArgs e)
       {
           hitboxFrameInfo.Clear();
           hurtboxFrameInfo.Clear();
           selectedHitboxes.Clear();
           selectedHurtboxes.Clear();

           openFileDialog1.Filter = "PNG Image|*.png|JPG Image|*.jpg";
           openFileDialog1.Multiselect = true;

           openFileDialog2.Filter = "Javascript Object Notation (*.json)|*.json";
           openFileDialog2.Multiselect = false;

           DialogResult result = openFileDialog1.ShowDialog();

           if (result == DialogResult.OK) 
           {
               frameNoForm.ShowDialog();
               loadedSprite = Image.FromFile(openFileDialog1.FileName);

               if (frameNoForm.frameNo > 0){
                   noFrames = frameNoForm.frameNo - 1;
               }
               else {
                   noFrames = (loadedSprite.Width / loadedSprite.Height) - 1;
               }

               spriteFrameWidth = loadedSprite.Width / (noFrames + 1);
               spriteFrameHeight = loadedSprite.Height;

               currentFrame = 0;
               textBox1.Text = (currentFrame + 1).ToString() + " : " + (noFrames + 1).ToString();

               spriteName = Path.GetFileNameWithoutExtension(openFileDialog1.FileName);

               UpdatePictureBox();
           }
       }

       private void MoveLeft()
       {
            currentFrame--;

           if (currentFrame < 0) {
               currentFrame = 0;
           }

            bool moveCheck = false;

            textBox1.Text = (currentFrame + 1).ToString() + " : " + (noFrames + 1).ToString();

            hitboxFrameInfo.Clear();
            foreach (var item in information.HitBoxes)
            {
                if(item.FrameNo == currentFrame) {
                    hitboxFrameInfo.Add(item);
                }

                if (item.NextMove == true && item.FrameNo == currentFrame) {
                    moveCheck = true;
                }
            }

            hurtboxFrameInfo.Clear();
            foreach (var item in information.HurtBoxes)
            {
                if(item.FrameNo == currentFrame) {
                    hurtboxFrameInfo.Add(item);
                }
            }

            if (moveCheck == true) {
                TransitionCheck.Checked = true;
            }
            else {
                TransitionCheck.Checked = false;
            }

            DeleteBoxes();
           UpdatePictureBox();
       }

       private void MoveRight()
       {
            currentFrame++;

           if (currentFrame > noFrames) {
               currentFrame = noFrames;
           }

            bool moveCheck = false;
            textBox1.Text = (currentFrame + 1).ToString() + " : " + (noFrames + 1).ToString();


            hitboxFrameInfo.Clear();
            foreach (var item in information.HitBoxes)
            {
                if (item.FrameNo == currentFrame) {
                    hitboxFrameInfo.Add(item);
                }

                if(item.NextMove == true && item.FrameNo == currentFrame) {
                    moveCheck = true;
                }
            }

            hurtboxFrameInfo.Clear();
            foreach (var item in information.HurtBoxes)
            {
                if (item.FrameNo == currentFrame) {
                    hurtboxFrameInfo.Add(item);
                }
            }

            if(moveCheck == true) {
                TransitionCheck.Checked = true;
            }
            else {
                TransitionCheck.Checked = false;
            }

           DeleteBoxes();
           UpdatePictureBox();
       }

        private void DeleteBoxes()
        {
            foreach (var item in selectedHitboxes) {
                information.HitBoxes.RemoveAll(h => h.Equals(item));
            }

            foreach (var item in selectedHurtboxes) {
                information.HurtBoxes.RemoveAll(h => h.Equals(item));
            }

           selectedHurtboxes.Clear();
           selectedHitboxes.Clear();

           pictureBox1.Invalidate();
        }

        private void ReduceToOriginalSize()
        {
            Rectangle currentRectangle;

            for (int i = 0; i < hitboxFrameInfo.Count; i++)
            {
                if (hitboxFrameInfo[i].ZoomLevel == 2)
                {
                    float value = 0.5f;
                    currentRectangle = HitBoxChange(i, value, 1, hitboxFrameInfo[i]);
                }

                if (hitboxFrameInfo[i].ZoomLevel == 3)
                {
                    float value = 0.35f;
                    currentRectangle = HitBoxChange(i, value, 1, hitboxFrameInfo[i]);
                }

                if (hitboxFrameInfo[i].ZoomLevel == 4)
                {
                    float value = 0.25f;
                    currentRectangle = HitBoxChange(i, value, 1, hitboxFrameInfo[i]);
                }

                if (hitboxFrameInfo[i].ZoomLevel == 5)
                {
                    float value = 0.2f;
                    currentRectangle = HitBoxChange(i, value, 1, hitboxFrameInfo[i]);
                }
            }

            for (int i = 0; i < hurtboxFrameInfo.Count; i++)
            {
                if (hurtboxFrameInfo[i].ZoomLevel == 2)
                {
                    float value = 0.5f;
                    currentRectangle = HurtboxChange(i, value, 1, hurtboxFrameInfo[i]);
                }

                if (hurtboxFrameInfo[i].ZoomLevel == 3)
                {
                    float value = 0.35f;
                    currentRectangle = HurtboxChange(i, value, 1, hurtboxFrameInfo[i]);
                }

                if (hurtboxFrameInfo[i].ZoomLevel == 4)
                {
                    float value = 0.25f;
                    currentRectangle = HurtboxChange(i, value, 1, hurtboxFrameInfo[i]);
                }

                if (hurtboxFrameInfo[i].ZoomLevel == 5)
                {
                    float value = 0.2f;
                    currentRectangle = HurtboxChange(i, value, 1, hurtboxFrameInfo[i]);
                }
            }

            pictureBox1.Invalidate();
        }

        private void ReduceSizeSave()
        {
            for (int i = 0; i < information.HitBoxes.Count; i++)
            {
                if (information.HitBoxes[i].ZoomLevel == 2)
                {
                    float value = 0.5f;
                    information.HitBoxes[i] = new Box((int)(information.HitBoxes[i].X * value), (int)(information.HitBoxes[i].Y * value),
                                                      (int)(information.HitBoxes[i].Width * value), (int)(information.HitBoxes[i].Height * value), 
                                                      information.HitBoxes[i].FrameNo, 1);
                }

                if (information.HitBoxes[i].ZoomLevel == 3)
                {
                    float value = 0.35f;
                    information.HitBoxes[i] = new Box((int)(information.HitBoxes[i].X * value), (int)(information.HitBoxes[i].Y * value),
                                                       (int)(information.HitBoxes[i].Width * value), (int)(information.HitBoxes[i].Height * value),
                                                       information.HitBoxes[i].FrameNo, 1);
                }

                if (information.HitBoxes[i].ZoomLevel == 4)
                {
                    float value = 0.25f;
                    information.HitBoxes[i] = new Box((int)(information.HitBoxes[i].X * value), (int)(information.HitBoxes[i].Y * value),
                                                      (int)(information.HitBoxes[i].Width * value), (int)(information.HitBoxes[i].Height * value),
                                                      information.HitBoxes[i].FrameNo, 1);
                }

                if (information.HitBoxes[i].ZoomLevel == 5)
                {
                    float value = 0.2f;
                    information.HitBoxes[i] = new Box((int)(information.HitBoxes[i].X * value), (int)(information.HitBoxes[i].Y * value),
                                                      (int)(information.HitBoxes[i].Width * value), (int)(information.HitBoxes[i].Height * value),
                                                      information.HitBoxes[i].FrameNo, 1);
                }
            }

            for (int i = 0; i < information.HurtBoxes.Count; i++)
            {
                if (information.HurtBoxes[i].ZoomLevel == 2)
                {
                    float value = 0.5f;
                    information.HurtBoxes[i] = new Box((int)(information.HurtBoxes[i].X * value), (int)(information.HurtBoxes[i].Y * value),
                                                       (int)(information.HurtBoxes[i].Width * value), (int)(information.HurtBoxes[i].Height * value), 
                                                             information.HurtBoxes[i].FrameNo, 1);
                }

                if (information.HurtBoxes[i].ZoomLevel == 3)
                {
                    float value = 0.35f;
                    information.HurtBoxes[i] = new Box((int)(information.HurtBoxes[i].X * value), (int)(information.HurtBoxes[i].Y * value),
                                                      (int)(information.HurtBoxes[i].Width * value), (int)(information.HurtBoxes[i].Height * value),
                                                            information.HurtBoxes[i].FrameNo, 1);
                }

                if (information.HurtBoxes[i].ZoomLevel == 4)
                {
                    float value = 0.25f;
                    information.HurtBoxes[i] = new Box((int)(information.HurtBoxes[i].X * value), (int)(information.HurtBoxes[i].Y * value),
                                                       (int)(information.HurtBoxes[i].Width * value), (int)(information.HurtBoxes[i].Height * value),
                                                             information.HurtBoxes[i].FrameNo, 1);
                }

                if (information.HurtBoxes[i].ZoomLevel == 5)
                {
                    float value = 0.2f;
                    information.HurtBoxes[i] = new Box((int)(information.HurtBoxes[i].X * value), (int)(information.HurtBoxes[i].Y * value),
                                                      (int)(information.HurtBoxes[i].Width * value), (int)(information.HurtBoxes[i].Height * value),
                                                            information.HurtBoxes[i].FrameNo, 1);
                }
            }
        }

       private void ZoomOnImage()
       {
           zoomLev = trackBar1.Value;

           Size newSize = new Size((int)(croppedImage.Width * trackBar1.Value), (int)(croppedImage.Height * trackBar1.Value));
           pictureBox1.Image = new Bitmap(croppedImage, newSize);

           if (hitboxFrameInfo.Count > 0)
           {
               for (int i = 0; i < hitboxFrameInfo.Count; i++)
               {
                   Rectangle currentRectangle;

                   if (hitboxFrameInfo[i].ZoomLevel == 1) {
                        currentRectangle = HitBoxChange(i, trackBar1.Value, zoomLev, hitboxFrameInfo[i]);
                    }

                   if (hitboxFrameInfo[i].ZoomLevel == 2)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.5f;
                           currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                        }

                       if (zoomLev >= 3)
                       {
                           float value = (trackBar1.Value / 2f);
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                        }
                   }

                   if (hitboxFrameInfo[i].ZoomLevel == 3)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.35f;
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.68f;
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }

                       if (zoomLev >= 4)
                       {
                           float value = (trackBar1.Value / 3f);
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }
                   }

                   if (hitboxFrameInfo[i].ZoomLevel == 4)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.25f;
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.5f;
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }

                       if (zoomLev == 3)
                       {
                           float value = 0.75f;
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }

                       if (zoomLev >= 5)
                       {
                           float value = (trackBar1.Value / 4f);
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }
                   }

                   if (hitboxFrameInfo[i].ZoomLevel == 5)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.2f;
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.4f;
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }

                       if (zoomLev == 3)
                       {
                           float value = 0.6f;
                            currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }

                       if (zoomLev == 4)
                       {
                           float value = 0.8f;
                           currentRectangle = HitBoxChange(i, value, zoomLev, hitboxFrameInfo[i]);
                       }
                   }
               }
           }

           

           if (hurtboxFrameInfo.Count > 0)
           {
               for (int i = 0; i < hurtboxFrameInfo.Count; i++)
               {
                   Rectangle currentRectangle;

                   if (hurtboxFrameInfo[i].ZoomLevel == 1) {
                        currentRectangle = HurtboxChange(i, trackBar1.Value, zoomLev, hurtboxFrameInfo[i]);
                   }

                   if (hurtboxFrameInfo[i].ZoomLevel == 2)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.5f;
                           currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }

                       if (zoomLev >= 3)
                       {
                           float value = (trackBar1.Value / 2f);
                            currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }
                   }

                   if (hurtboxFrameInfo[i].ZoomLevel == 3)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.35f;
                            currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.68f;
                           currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }

                       if (zoomLev >= 4)
                       {
                           float value = (trackBar1.Value / 3f);
                           currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }
                   }

                   if (hurtboxFrameInfo[i].ZoomLevel == 4)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.25f;
                            currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.5f;
                            currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }

                       if (zoomLev == 3)
                       {
                           float value = 0.75f;
                           currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }

                       if (zoomLev >= 5)
                       {
                           float value = (trackBar1.Value / 4f);
                            currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }
                   }

                   if (hurtboxFrameInfo[i].ZoomLevel == 5)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.2f;
                           currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.4f;
                            currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }

                       if (zoomLev == 3)
                       {
                           float value = 0.6f;
                            currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }

                       if (zoomLev == 4)
                       {
                           float value = 0.8f;
                           currentRectangle = HurtboxChange(i, value, zoomLev, hurtboxFrameInfo[i]);
                       }
                   }
               }
           }

           if (selectedHurtboxes.Count > 0)
           {
               for (int i = 0; i < selectedHurtboxes.Count; i++)
               {
                   Rectangle currentRectangle;

                   if (selectedHurtboxes[i].ZoomLevel == 1) {
                        currentRectangle = SelectedHurtboxChange(i, trackBar1.Value, zoomLev, selectedHurtboxes[i]);
                    }

                   if (selectedHurtboxes[i].ZoomLevel == 2)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.5f;
                           currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }

                       if (zoomLev >= 3)
                       {
                           float value = (trackBar1.Value / 2f);
                           currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }
                   }

                   if (selectedHurtboxes[i].ZoomLevel == 3)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.35f;
                            currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.68f;
                           currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }

                       if (zoomLev >= 4)
                       {
                           float value = (trackBar1.Value / 3f);
                           currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }
                   }

                   if (selectedHurtboxes[i].ZoomLevel == 4)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.25f;
                            currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.5f;
                           currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }

                       if (zoomLev == 3)
                       {
                           float value = 0.75f;
                           currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }

                       if (zoomLev >= 5)
                       {
                           float value = (trackBar1.Value / 4f);
                           currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }
                   }

                   if (selectedHurtboxes[i].ZoomLevel == 5)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.2f;
                           currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.4f;
                            currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }

                       if (zoomLev == 3)
                       {
                           float value = 0.6f;
                            currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }

                       if (zoomLev == 4)
                       {
                           float value = 0.8f;
                           currentRectangle = SelectedHurtboxChange(i, value, zoomLev, selectedHurtboxes[i]);
                       }
                   }
               }
           }

           if (selectedHitboxes.Count > 0)
           {
               for (int i = 0; i < selectedHitboxes.Count; i++)
               {
                   Rectangle currentRectangle;

                   if (selectedHitboxes[i].ZoomLevel == 1) {
                        currentRectangle = SelectedHitBoxChange(i, trackBar1.Value, zoomLev, selectedHitboxes[i]);
                   }

                   if (selectedHitboxes[i].ZoomLevel == 2)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.5f;
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }

                       if (zoomLev >= 3)
                       {
                           float value = (trackBar1.Value / 2f);
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }
                   }

                   if (selectedHitboxes[i].ZoomLevel == 3)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.35f;
                            currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.68f;
                            currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }

                       if (zoomLev >= 4)
                       {
                           float value = (trackBar1.Value / 3f);
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }
                   }

                   if (selectedHitboxes[i].ZoomLevel == 4)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.25f;
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.5f;
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }

                       if (zoomLev == 3)
                       {
                           float value = 0.75f;
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }

                       if (zoomLev >= 5)
                       {
                           float value = (trackBar1.Value / 4f);
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }
                   }

                   if (selectedHitboxes[i].ZoomLevel == 5)
                   {
                       if (zoomLev == 1)
                       {
                           float value = 0.2f;
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }

                       if (zoomLev == 2)
                       {
                           float value = 0.4f;
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }

                       if (zoomLev == 3)
                       {
                           float value = 0.6f;
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }

                       if (zoomLev == 4)
                       {
                           float value = 0.8f;
                           currentRectangle = SelectedHitBoxChange(i, value, zoomLev, selectedHitboxes[i]);
                       }
                   }
               }
           }

               //DebugInformation();
       }

        private Rectangle HitBoxChange(int selectedBox, float zoomVal, int zoomLevel, Box information)
       {
            Rectangle currentRectangle = new Rectangle();

            currentRectangle = new Rectangle((int)(hitboxFrameInfo[selectedBox].X * zoomVal), (int)(hitboxFrameInfo[selectedBox].Y * zoomVal),
                                             (int)(hitboxFrameInfo[selectedBox].Width * zoomVal), (int)(hitboxFrameInfo[selectedBox].Height * zoomVal));

            hitboxFrameInfo.RemoveAt(selectedBox);
            hitboxFrameInfo.Insert(selectedBox, new Box(currentRectangle.X, currentRectangle.Y, currentRectangle.Width, currentRectangle.Height, currentFrame, zoomLevel));
            //ReAttachedHitBoxes();

            return currentRectangle;
        }

        private Rectangle HurtboxChange(int selectedBox, float zoomVal, int zoomLevel, Box information)
        {
            Rectangle currentRectangle = new Rectangle();

            currentRectangle = new Rectangle((int)(hurtboxFrameInfo[selectedBox].X * zoomVal), (int)(hurtboxFrameInfo[selectedBox].Y * zoomVal),
                                             (int)(hurtboxFrameInfo[selectedBox].Width * zoomVal), (int)(hurtboxFrameInfo[selectedBox].Height * zoomVal));

            hurtboxFrameInfo.RemoveAt(selectedBox);
            hurtboxFrameInfo.Insert(selectedBox, new Box(currentRectangle.X, currentRectangle.Y, currentRectangle.Width, currentRectangle.Height, currentFrame, zoomLevel));

            return currentRectangle;
        }

         private Rectangle SelectedHitBoxChange(int selectedBox, float zoomVal, int zoomLevel, Box information)
        {
            Rectangle currentRectangle = new Rectangle();

            currentRectangle = new Rectangle((int)(hitboxFrameInfo[selectedBox].X * zoomVal), (int)(hitboxFrameInfo[selectedBox].Y * zoomVal),
                                             (int)(hitboxFrameInfo[selectedBox].Width * zoomVal), (int)(hitboxFrameInfo[selectedBox].Height * zoomVal));

            selectedHitboxes.RemoveAt(selectedBox);
            selectedHitboxes.Insert(selectedBox, new Box(currentRectangle.X, currentRectangle.Y, currentRectangle.Width, currentRectangle.Height, currentFrame, zoomLevel));

            return currentRectangle;
         }

        private Rectangle SelectedHurtboxChange(int selectedBox, float zoomVal, int zoomLevel, Box information)
        {
            Rectangle currentRectangle = new Rectangle();

            currentRectangle = new Rectangle((int)(hurtboxFrameInfo[selectedBox].X * zoomVal), (int)(hurtboxFrameInfo[selectedBox].Y * zoomVal),
                                             (int)(hurtboxFrameInfo[selectedBox].Width * zoomVal), (int)(hurtboxFrameInfo[selectedBox].Height * zoomVal));

            selectedHurtboxes.RemoveAt(selectedBox);
            selectedHitboxes.Insert(selectedBox, new Box(currentRectangle.X, currentRectangle.Y, currentRectangle.Width, currentRectangle.Height, currentFrame, zoomLevel));

            return currentRectangle;
        }

        private void SaveHitboxInformation()
        {
            string filename;

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                ReduceSizeSave();
                filename = saveFileDialog1.FileName;

                information.Spritename =  Path.GetFileNameWithoutExtension(saveFileDialog1.FileName);
                string output = JsonConvert.SerializeObject(information);
                File.WriteAllText(filename, output);
            }
       }

        private void LoadHitboxInformation()
        {
            if (openFileDialog2.ShowDialog() == DialogResult.OK)
            {
                hitboxFrameInfo.Clear();
                hurtboxFrameInfo.Clear();
                selectedHitboxes.Clear();
                selectedHurtboxes.Clear();


                string input = File.ReadAllText(openFileDialog2.FileName);
                information = JsonConvert.DeserializeObject<HitboxInformation>(input);

                hitboxFrameInfo.Clear();
                foreach (var item in information.HitBoxes)
                {
                    if (item.FrameNo == currentFrame) {
                        hitboxFrameInfo.Add(item);
                    }
                }

                hurtboxFrameInfo.Clear();
                foreach (var item in information.HurtBoxes)
                {
                    if (item.FrameNo == currentFrame) {
                        hurtboxFrameInfo.Add(item);
                    }
                }

                DeleteBoxes();
                UpdatePictureBox();
            }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
       {
           if (keyData == Keys.Left) {
               MoveLeft();
               return true;
           }

           if (keyData == Keys.Right) {
               MoveRight();
               return true;
           }

           if (keyData == Keys.Escape) {
               System.Windows.Forms.Application.Exit();
           }

           if (keyData == Keys.Delete) {
               DeleteBoxes();
           }

           return base.ProcessCmdKey(ref msg, keyData);
       }

       private void button1_Click(object sender, EventArgs e) {
           MoveLeft();
       }

       private void button2_Click(object sender, EventArgs e) {
           MoveRight();
       }

       private void trackBar1_Scroll(object sender, EventArgs e) {
           ZoomOnImage();
       }

       private void MouseDownPictureBox(object sender, MouseEventArgs e) {
           currentPos = startPos = e.Location;
           drawing = true;
       }

       private void MouseUpPictureBox(object sender, MouseEventArgs e) {
           if (drawing)
           {
               drawing = false;
               var rc = getRectangle();

               if (radioButton3.Checked == true) 
               {
                   bool boxselected = false;

                    for (int j = 0; j < hurtboxFrameInfo.Count; j++)
                    {
                        Rectangle box = new Rectangle(hurtboxFrameInfo[j].X, hurtboxFrameInfo[j].Y,
                                                      hurtboxFrameInfo[j].Width, hurtboxFrameInfo[j].Height);

                        if (box.Contains(e.Location) == true)
                        {
                            selectedHurtboxes.Add(hurtboxFrameInfo[j]);
                            hurtboxFrameInfo.RemoveAt(j);
                            boxselected = true;
                        }
                    }

                    for (int i = 0; i < hitboxFrameInfo.Count; i++)
                    {
                        Rectangle box = new Rectangle(hitboxFrameInfo[i].X, hitboxFrameInfo[i].Y,
                                                      hitboxFrameInfo[i].Width, hitboxFrameInfo[i].Height);

                        if (box.Contains(e.Location) == true)
                        {
                            selectedHitboxes.Add(hitboxFrameInfo[i]);
                            hitboxFrameInfo.RemoveAt(i);
                            boxselected = true;
                        }
                    }

                   if (boxselected == false)
                   {
                       foreach (var selectedHitbox in selectedHitboxes) {
                           hitboxFrameInfo.Add(selectedHitbox);
                       }

                       foreach (var selectedHurtbox in selectedHurtboxes){
                           hurtboxFrameInfo.Add(selectedHurtbox);
                       }

                       selectedHurtboxes.Clear();
                       selectedHitboxes.Clear();
                   }
               }

               if (rc.Width > 0 && rc.Height > 0) {
               
                    zoomLevel = trackBar1.Value;

                    if (radioButton1.Checked == true){
                        hitboxFrameInfo.Add(new Box(rc.X, rc.Y, rc.Width, rc.Height, currentFrame, zoomLevel));
                    }

                    if (radioButton2.Checked == true) {
                        hurtboxFrameInfo.Add(new Box(rc.X, rc.Y, rc.Width, rc.Height, currentFrame, zoomLevel));
                    }

                    ReAttachedHitBoxes();
                }

               pictureBox1.Invalidate();
           }
       }

        private void ReAttachedHitBoxes()
        {
            information.HitBoxes.RemoveAll(h => h.FrameNo == currentFrame);
            foreach (var item in hitboxFrameInfo)
            {
                information.HitBoxes.Add(item);
            }

            information.HurtBoxes.RemoveAll(h => h.FrameNo == currentFrame);
            foreach (var item in hurtboxFrameInfo)
            {
                information.HurtBoxes.Add(item);
            }
        }

       private void MouseMovePictureBox(object sender, MouseEventArgs e) {
           currentPos.X = Clamp(e.Location.X, 0, pictureBox1.Width - 1);
           currentPos.Y = Clamp(e.Location.Y, 0, pictureBox1.Height - 1);

           if (drawing) {
               pictureBox1.Invalidate();
           }
       }

       private void PaintOnPictureBox(object sender, PaintEventArgs e) {

           if (drawing) {
               SolidBrush mycolour = new SolidBrush(Color.FromArgb(100, Color.Black));
               e.Graphics.FillRectangle(mycolour, getRectangle());
           }

           if(hurtboxFrameInfo.Count > 0)
           {
               for (int j = 0; j < hurtboxFrameInfo.Count; j++) {
                   SolidBrush mycolour = new SolidBrush(Color.FromArgb(100, Color.Red));
                   e.Graphics.FillRectangle(mycolour, new Rectangle(hurtboxFrameInfo[j].X, hurtboxFrameInfo[j].Y,
                                                                    hurtboxFrameInfo[j].Width, hurtboxFrameInfo[j].Height));
               }
           }

             if (hitboxFrameInfo.Count > 0)
             {
               for (int i = 0; i < hitboxFrameInfo.Count; i++) {
                   SolidBrush mycolour = new SolidBrush(Color.FromArgb(100, Color.Blue));
                    e.Graphics.FillRectangle(mycolour, new Rectangle(hitboxFrameInfo[i].X, hitboxFrameInfo[i].Y,
                                                                     hitboxFrameInfo[i].Width, hitboxFrameInfo[i].Height));
                }
             }

             foreach (var selectedHitbox in selectedHitboxes) {
                 SolidBrush mycolour = new SolidBrush(Color.FromArgb(100, Color.Green));
                  e.Graphics.FillRectangle(mycolour, new Rectangle(selectedHitbox.X, selectedHitbox.Y,
                                                                   selectedHitbox.Width, selectedHitbox.Height));
             }

             foreach (var selectedHurtbox in selectedHurtboxes) {
                 SolidBrush mycolour = new SolidBrush(Color.FromArgb(100, Color.Green));
                  e.Graphics.FillRectangle(mycolour, new Rectangle(selectedHurtbox.X, selectedHurtbox.Y,
                                                                   selectedHurtbox.Width, selectedHurtbox.Height));
            }
       }

       public static int Clamp(int value, int min, int max) {
           return (value < min) ? min : (value > max) ? max : value;
       }

       private void button3_Click(object sender, EventArgs e)
       {
           DeleteBoxes();
       }

       private void saveToolStripMenuItem_Click(object sender, EventArgs e) {
           SaveHitboxInformation();
       }

       private void loadToolStripMenuItem_Click(object sender, EventArgs e){
           LoadHitboxInformation();
       }

        private void TransitionCheck_CheckedChanged(object sender, EventArgs e)
        {
            if(TransitionCheck.Checked == true) {
                hitboxFrameInfo.Add(new Box(0, 0, 0, 0, currentFrame, 1, true));
                ReAttachedHitBoxes();
            }

            if (TransitionCheck.Checked == false)
            {
                for (int i = 0; i < hitboxFrameInfo.Count; i++) {
                    if (hitboxFrameInfo[i].NextMove == true && currentFrame == hitboxFrameInfo[i].FrameNo) {
                        hitboxFrameInfo.RemoveAt(i);
                    }
                    if(hitboxFrameInfo[i].X == 0 && hitboxFrameInfo[i].Y == 0 && currentFrame == hitboxFrameInfo[i].FrameNo) {
                        hitboxFrameInfo.RemoveAt(i);
                    }
                }
            }
        }
    }
}