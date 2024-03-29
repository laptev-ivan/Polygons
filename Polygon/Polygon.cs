﻿using LibraryShapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Windows.Forms;
using ZedGraph;

namespace Polygon
{
    public partial class Polygon : Form
    {
        public List<Shape> shapes;
        bool flag;
        static Random rnd;
        RadiusChanger radiusChanger;
        bool graph;
        Stopwatch clock;
        bool saving;

        public Polygon()
        {
            InitializeComponent();
            shapes = new List<Shape>();
            rnd = new Random();
            DoubleBuffered = true;
        }

        private void Form_Paint(object sender, PaintEventArgs e)
        {
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            Pen pen = new Pen(Color.Black, 3);

            if (shapes.Count > 2)
            {
                #region STANDARD
                if (standardToolStripMenuItem.Checked)
                {
                    //Standard Algorithm
                    int cntL, cntR;
                    foreach (Shape i in shapes)
                        i.DrawLine = false;

                    for (int i = 0; i < shapes.Count; i++)
                    {
                        for (int j = i + 1; j < shapes.Count; j++)
                        {
                            cntL = cntR = 0;
                            float k = ((float)shapes[j].Y - shapes[i].Y) / ((float)shapes[j].X - shapes[i].X);
                            float b = shapes[i].Y - k * shapes[i].X;
                            for (int l = 0; l < shapes.Count; l++)
                            {
                                if (l != i && l != j)
                                {
                                    if (shapes[l].Y > k * shapes[l].X + b)
                                        cntR++;
                                    else
                                        cntL++;
                                }
                            }
                            if (cntR * cntL == 0 && shapes[i].X != shapes[j].X)
                            {
                                if (!graph)
                                    e.Graphics.DrawLine(pen, shapes[i].X, shapes[i].Y, shapes[j].X, shapes[j].Y);
                                shapes[i].DrawLine = true;
                                shapes[j].DrawLine = true;
                            }
                        }
                    }
                }

                #endregion

                #region JARVIS
                else if (jarvisToolStripMenuItem.Checked)
                {
                    // Jarvis Algorithm
                    foreach (Shape i in shapes)
                        i.DrawLine = false;

                    // First shape
                    int startShape = 0;
                    foreach (Shape i in shapes)
                    {
                        if (i.X <= shapes[startShape].X)
                            startShape = shapes.IndexOf(i);
                    }

                    // Second shape
                    double minCos = 1;
                    int index = -1;
                    foreach (Shape i in shapes)
                    {
                        if (i == shapes[startShape]) continue;
                        if (MinCos(0, i.X - shapes[startShape].X, -20000, i.Y - shapes[startShape].Y) < minCos)
                        {
                            minCos = MinCos(0, i.X - shapes[startShape].X, -20000, i.Y - shapes[startShape].Y);
                            index = shapes.IndexOf(i);
                        }
                    }
                    shapes[startShape].DrawLine = shapes[index].DrawLine = true;
                    if (!graph)
                        e.Graphics.DrawLine(pen, shapes[startShape].X, shapes[startShape].Y, shapes[index].X, shapes[index].Y);

                    // Last shapes
                    double vx = shapes[startShape].X - shapes[index].X;
                    double vy = shapes[startShape].Y - shapes[index].Y;
                    int curShape = index;
                    do
                    {
                        minCos = 1;
                        for (int i = 0; i < shapes.Count; i++)
                        {
                            if (shapes[i] == shapes[curShape] || shapes[i] == shapes[index]) continue;

                            if (MinCos(vx, shapes[i].X - shapes[curShape].X, vy, shapes[i].Y - shapes[curShape].Y) < minCos)
                            {
                                minCos = MinCos(vx, shapes[i].X - shapes[curShape].X, vy, shapes[i].Y - shapes[curShape].Y);
                                index = i;
                            }
                        }
                        if (!graph)
                            e.Graphics.DrawLine(pen, shapes[curShape].X, shapes[curShape].Y, shapes[index].X, shapes[index].Y);
                        shapes[curShape].DrawLine = shapes[index].DrawLine = true;
                        vx = shapes[curShape].X - shapes[index].X;
                        vy = shapes[curShape].Y - shapes[index].Y;
                        curShape = index;
                    } while (index != startShape);
                }
                #endregion
            }
            if (!graph)
                foreach (Shape i in shapes)
                    i.Draw(e.Graphics);
        }

        private double MinCos(double x1, double x2, double y1, double y2)
        {
            return (x1 * x2 + y1 * y2) / (Math.Sqrt(x1 * x1 + y1 * y1) * Math.Sqrt(x2 * x2 + y2 * y2));
        }

        private void Form_MouseDown(object sender, MouseEventArgs e)
        {
            saving = false;
            if (e.Button == MouseButtons.Left)
            {
                foreach (Shape i in shapes)
                {
                    if (i.IsInside(e.X, e.Y))
                    {
                        flag = true;
                        i.Dx = i.X - e.X;
                        i.Dy = i.Y - e.Y;
                        i.IsDragged = true;
                    }
                }
                if (!flag)
                {
                    if (circleToolStripMenuItem.Checked)
                    {
                        Circle circle = new Circle(e.X, e.Y);
                        shapes.Add(circle);
                    }
                    else if (squareToolStripMenuItem.Checked)
                    {
                        Square square = new Square(e.X, e.Y);
                        shapes.Add(square);
                    }
                    else if (triangleToolStripMenuItem.Checked)
                    {
                        Triangle triangle = new Triangle(e.X, e.Y);
                        shapes.Add(triangle);
                    }
                    if (shapes.Count >= 3)
                    {
                        Refresh();
                        if (!shapes[shapes.Count - 1].DrawLine)
                        {
                            shapes.RemoveAt(shapes.Count - 1);
                            flag = true;
                            foreach (Shape i in shapes)
                            {
                                i.IsDragged = true;
                                i.Dx = i.X - e.X;
                                i.Dy = i.Y - e.Y;
                            }
                        }
                        for (int i = 0; i < shapes.Count; i++)
                        {
                            if (!shapes[i].DrawLine)
                            {
                                shapes.RemoveAt(i);
                                i--;
                            }
                        }
                    }
                }
            }
            if (e.Button == MouseButtons.Right)
                for (int i = shapes.Count - 1; i >= 0; i--)
                    if (shapes[i].IsInside(e.X, e.Y))
                    {
                        shapes.RemoveAt(i);
                        break;
                    }
            Refresh();
        }

        private void Form_MouseMove(object sender, MouseEventArgs e)
        {
            if (flag)
            {
                foreach (Shape i in shapes)
                    if (i.IsDragged)
                    {
                        i.X = e.X + i.Dx;
                        i.Y = e.Y + i.Dy;
                    }
                Refresh();
            }
        }

        private void Form_MouseUp(object sender, MouseEventArgs e)
        {
            if (flag)
            {
                flag = false;
                foreach (Shape i in shapes)
                    i.IsDragged = false;
                if (shapes.Count >= 3)
                    for (int i = 0; i < shapes.Count; i++)
                        if (!shapes[i].DrawLine)
                        {
                            shapes.RemoveAt(i);
                            i--;
                        }
                Refresh();
            }
        }

        private void FigureToolStripMenuItem_Click(object sender, EventArgs e)
        {
            circleToolStripMenuItem.CheckState = CheckState.Checked;
            ((ToolStripMenuItem)sender).Checked = true;
            foreach (ToolStripMenuItem item in figureTypeToolStripMenuItem.DropDownItems)
                if (item != null && item != (ToolStripMenuItem)sender)
                    item.Checked = false;
        }

        private void AlgorithmToolStripMenuItem_Click(object sender, EventArgs e)
        {
            jarvisToolStripMenuItem.CheckState = CheckState.Checked;
            ((ToolStripMenuItem)sender).Checked = true;
            foreach (ToolStripMenuItem item in algorithmTypeToolStripMenuItem.DropDownItems)
                if (item != null && item != (ToolStripMenuItem)sender)
                    item.Checked = false;
        }

        private void changeColorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (shapes.Count > 0)
            {
                DialogResult result = colorDialog1.ShowDialog();
                if (result == DialogResult.OK)
                    Shape.C = colorDialog1.Color;
                Refresh();
            }
        }

        private void efficiencyGraphicToolStripMenuItem_Click(object sender, EventArgs e)
        {
            shapes.Clear();

            graph = true;

            for (int i = 0; i < 3; i++)
                shapes.Add(new Circle(rnd.Next(-100, 100), rnd.Next(-100, 100)));

            clock = new Stopwatch();

            jarvisToolStripMenuItem.Checked = false;
            standardToolStripMenuItem.Checked = true;

            PointPairList listStandard = new PointPairList();
            for (int i = 1; i <= 10; i++)
            {
                for (int j = 1; j <= 10 * i; j++)
                    shapes.Add(new Circle(rnd.Next(-100, 100), rnd.Next(-100, 100)));
                clock.Start();
                Refresh();
                clock.Stop();
                listStandard.Add(i * 10, clock.ElapsedTicks);
                clock.Reset();
            }

            shapes.Clear();

            for (int i = 0; i < 3; i++)
                shapes.Add(new Circle(rnd.Next(-100, 100), rnd.Next(-100, 100)));

            jarvisToolStripMenuItem.Checked = true;
            standardToolStripMenuItem.Checked = false;

            PointPairList listJar = new PointPairList();
            for (int i = 1; i <= 10; i++)
            {
                for (int j = 1; j <= 10 * i; j++)
                    shapes.Add(new Circle(rnd.Next(-100, 100), rnd.Next(-100, 100)));
                clock.Start();
                Refresh();
                clock.Stop();
                listJar.Add(i * 10, clock.ElapsedTicks);
                clock.Reset();
            }

            shapes.Clear();

            Form2 f2 = new Form2(listJar, listStandard);
            f2.ShowDialog();

            Refresh();
            graph = false;
        }

        private void UpdateRadius(object sender, RadiusEventArgs e)
        {
            Shape.R = e.Radius;
            Refresh();
        }

        private void changeRadiusToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (radiusChanger == null || radiusChanger.IsDisposed)
                radiusChanger = new RadiusChanger();
            if (radiusChanger.WindowState == FormWindowState.Minimized || !radiusChanger.TopMost)
            {
                radiusChanger.WindowState = FormWindowState.Normal;
                radiusChanger.Activate();
            }
            radiusChanger.RadiusChanged += new RadiusDelegate(UpdateRadius);
            radiusChanger.Show();
        }

        private void SaveAs()
        {
            BinaryFormatter bf = new BinaryFormatter();
            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create, FileAccess.Write);
                bf.Serialize(fs, shapes);
                bf.Serialize(fs, Shape.C);
                bf.Serialize(fs, Shape.R);
                fs.Close();
                saving = true;
            }
            else
            {
                DialogResult dialogSave = MessageBox.Show("Do you want to save changes to this file?", "Polygons", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogSave == DialogResult.Yes) Save();
            }
        }

        private void Open()
        {
            if (shapes.Count != 0 && !saving)
            {
                DialogResult dialogSave = MessageBox.Show("Do you want to save changes to this file?", "Polygons", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogSave == DialogResult.Yes) Save();
            }
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fs = new FileStream(openFileDialog1.FileName, FileMode.Open, FileAccess.Read);
                shapes = (List<Shape>)bf.Deserialize(fs);
                Shape.C = (Color)bf.Deserialize(fs);
                Shape.R = (int)bf.Deserialize(fs);
                Refresh();
                fs.Close();
                saving = true;
            }
        }

        private void Save()
        {
            if (saveFileDialog1.FileName == "") SaveAs();
            else
            {
                BinaryFormatter bf = new BinaryFormatter();
                FileStream fs = new FileStream(saveFileDialog1.FileName, FileMode.Create, FileAccess.Write);
                bf.Serialize(fs, shapes);
                bf.Serialize(fs, Shape.C);
                bf.Serialize(fs, Shape.R);
                fs.Close();
                saving = true;
            }
        }

        private void Polygon_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (shapes.Count != 0 && !saving)
            {
                DialogResult dialogSave = MessageBox.Show("Do you want to save changes to this file?", "Polygons", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogSave == DialogResult.Yes) Save();
            }
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (shapes.Count != 0 && !saving)
            {
                DialogResult dialogSave = MessageBox.Show("Do you want to save changes to this file?", "Polygons", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (dialogSave == DialogResult.Yes) Save();
            }
            shapes = new List<Shape>();
            Refresh();
            saving = false;
        }

        private void saveToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Open();
        }

        private void saveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveAs();
        }
    }
}
