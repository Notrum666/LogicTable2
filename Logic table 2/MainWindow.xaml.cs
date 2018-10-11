﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Windows.Markup;
using System.Xml;
using System.Windows.Resources;
using System.Threading;

namespace Logic_table_2
{
    public static class CONFIG
    {
        public static float SCALE_COEF = 1.05f;
        public static float NODES_SIZE = 100;
        public static float CONNECTIONS_SIZE = 5;
        public static float CONNECTIONS_ARROWS_DROPOUT = 15;
        public static Cursor GRAB_CURSOR;
        public static Cursor GRABBING_CURSOR;
        public static SolidColorBrush BLACK = new SolidColorBrush(Colors.Black);
        public static SolidColorBrush WHITE = new SolidColorBrush(Colors.White);
        public static SolidColorBrush BLUE_TRANSPARENT = new SolidColorBrush(Color.FromArgb(150, 120, 120, 250));
    }
    public interface IChoosable
    {
        bool isChosen { get; set; }
        void select();
        void deselect();
        event MouseButtonEventHandler OnLeftMouseButtonDown, OnLeftMouseButtonUp;
    }
    public abstract class LogicNode
    {
        public delegate void ConnectionEventHandler(LogicNode from, LogicNode to);
        public event ConnectionEventHandler OnConnectionAdd, OnConnectionRemove;
        private List<LogicNode> inputs = new List<LogicNode>();
        protected bool state = false;
        protected bool nextState = false;
        protected int maxInputs = -1;

        public void addInput(LogicNode input)
        {
            if ((maxInputs != -1) && (inputs.Count() >= maxInputs))
            {
                OnConnectionRemove(inputs[0], this);
                inputs.RemoveAt(0);
                inputs.Insert(0, input);
                OnConnectionAdd(input, this);
                return;
            }
            inputs.Add(input);
            OnConnectionAdd(input, this);
        }
        public bool removeInput(LogicNode input)
        {
            if (inputs.Remove(input))
            {
                OnConnectionRemove(input, this);
                return true;
            }
            return false;
        }
        public void set_instantly(bool state)
        {
            nextState = state;
            this.state = state;
        }
        public void set(bool state)
        {
            nextState = state;
        }
        public bool get()
        {
            return state;
        }
        public void update_state()
        {
            state = nextState;
        }
        public void recount()
        {
            bool[] temp_inputs = new bool[inputs.Count()];
            for (int i = 0; i < temp_inputs.Length; i++)
                temp_inputs[i] = inputs[i].get();
            set(func(temp_inputs));
        }
        protected abstract bool func(bool[] inputs);
    }
    public abstract class VisualNode : LogicNode, IChoosable
    {
        public Grid view = new Grid();
        private Ellipse el = new Ellipse();
        private TextBlock funcText = new TextBlock();
        public float x, y;

        private UIElement selectionView { get; set; }
        public bool isChosen { get; set; }
        public event MouseButtonEventHandler OnLeftMouseButtonDown, OnLeftMouseButtonUp;

        public VisualNode(Point pos, string text)
        {
            this.x = (float)pos.X;
            this.y = (float)pos.Y;
            funcText.Text = text;
            construct();
        }
        private void construct()
        {
            view.HorizontalAlignment = HorizontalAlignment.Left;
            view.VerticalAlignment = VerticalAlignment.Top;
            view.MouseLeftButtonDown += delegate(object sender, MouseButtonEventArgs e) { OnLeftMouseButtonDown(this, e); };
            view.MouseLeftButtonUp += delegate (object sender, MouseButtonEventArgs e) { OnLeftMouseButtonUp(this, e); };
            Grid.SetZIndex(view, 1);

            el.HorizontalAlignment = HorizontalAlignment.Center;
            el.VerticalAlignment = VerticalAlignment.Center;
            el.Visibility = Visibility.Visible;
            view.Children.Add(el);

            funcText.HorizontalAlignment = HorizontalAlignment.Center;
            funcText.VerticalAlignment = VerticalAlignment.Center;
            funcText.Width = double.NaN;
            funcText.Height = double.NaN;
            funcText.FontWeight = FontWeights.Bold;
            funcText.Visibility = Visibility.Visible;
            view.Children.Add(funcText);

            Ellipse ch = new Ellipse();
            ch.HorizontalAlignment = HorizontalAlignment.Center;
            ch.VerticalAlignment = VerticalAlignment.Center;
            ch.Visibility = Visibility.Hidden;
            ch.Fill = null;
            ch.Stroke = CONFIG.BLUE_TRANSPARENT;
            ch.StrokeThickness = 5.0;
            selectionView = ch;
            view.Children.Add(selectionView);
        }
        public void select()
        {
            isChosen = true;
            selectionView.Visibility = Visibility.Visible;
        }
        public void deselect()
        {
            isChosen = false;
            selectionView.Visibility = Visibility.Hidden;
        }
        public void redraw(Camera camera)
        {
            view.Dispatcher.Invoke(delegate
            {
                ((Ellipse)selectionView).Height = CONFIG.NODES_SIZE * camera.scale * 1.2;
                ((Ellipse)selectionView).Width = CONFIG.NODES_SIZE * camera.scale * 1.2;
                ((Ellipse)selectionView).StrokeThickness = 5.0 * camera.scale;
                view.Height = CONFIG.NODES_SIZE * camera.scale * 2.0;
                view.Width = CONFIG.NODES_SIZE * camera.scale * 2.0;
                el.Height = CONFIG.NODES_SIZE * camera.scale;
                el.Width = CONFIG.NODES_SIZE * camera.scale;
                funcText.FontSize = view.Height / 10.0;
                view.Margin = new Thickness((x - camera.pos.X - CONFIG.NODES_SIZE) * camera.scale, (y - camera.pos.Y - CONFIG.NODES_SIZE) * camera.scale, 0, 0);
                el.Fill = get() ? CONFIG.WHITE : CONFIG.BLACK;
                funcText.Foreground = get() ? CONFIG.BLACK : CONFIG.WHITE;
            });
        }
    }
    public class LogicNode_And : VisualNode
    {
        public LogicNode_And(Point pos, string text) : base(pos, text)
        {
        }
        protected override bool func(bool[] inputs)
        {
            if (inputs.Length == 0)
                return false;
            foreach (bool a in inputs)
                if (!a)
                    return false;
            return true;
        }
    }
    public class LogicNode_Or : VisualNode
    {
        public LogicNode_Or(Point pos, string text) : base(pos, text)
        {
        }
        protected override bool func(bool[] inputs)
        {
            foreach (bool a in inputs)
                if (a)
                    return true;
            return false;
        }
    }
    public class LogicNode_Not : VisualNode
    {
        public LogicNode_Not(Point pos, string text) : base(pos, text)
        {
            state = true;
            nextState = true;
            maxInputs = 1;
        }
        protected override bool func(bool[] inputs)
        {
            return inputs.Length > 0 ? !inputs[0] : true;
        }
    }
    public class Connection : IChoosable
    {
        public VisualNode from, to;
        private Line view, leftArrow, rightArrow;
        public bool isChosen { get; set; }
        public event MouseButtonEventHandler OnLeftMouseButtonDown;
        public event MouseButtonEventHandler OnLeftMouseButtonUp;

        public Connection(VisualNode from, VisualNode to)
        {
            this.from = from;
            this.to = to;

            view = new Line();
            view.HorizontalAlignment = HorizontalAlignment.Left;
            view.VerticalAlignment = VerticalAlignment.Top;
            view.Stroke = CONFIG.BLACK;
            view.StrokeThickness = CONFIG.CONNECTIONS_SIZE;
            Grid.SetZIndex(view, -1);

            leftArrow = new Line();
            leftArrow.HorizontalAlignment = HorizontalAlignment.Left;
            leftArrow.VerticalAlignment = VerticalAlignment.Top;
            leftArrow.Stroke = CONFIG.BLACK;
            leftArrow.StrokeThickness = CONFIG.CONNECTIONS_SIZE;
            Grid.SetZIndex(leftArrow, -1);

            rightArrow = new Line();
            rightArrow.HorizontalAlignment = HorizontalAlignment.Left;
            rightArrow.VerticalAlignment = VerticalAlignment.Top;
            rightArrow.Stroke = CONFIG.BLACK;
            rightArrow.StrokeThickness = CONFIG.CONNECTIONS_SIZE;
            Grid.SetZIndex(rightArrow, -1);

            view.MouseLeftButtonDown += onLeftMouseButtonDown;
            leftArrow.MouseLeftButtonDown += onLeftMouseButtonDown;
            rightArrow.MouseLeftButtonDown += onLeftMouseButtonDown;
            view.MouseLeftButtonUp += onLeftMouseButtonUp;
            leftArrow.MouseLeftButtonUp += onLeftMouseButtonUp;
            rightArrow.MouseLeftButtonUp += onLeftMouseButtonUp;
        }
        private void onLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            OnLeftMouseButtonDown(this, e);
        }
        private void onLeftMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            OnLeftMouseButtonUp(this, e);
        }
        public void select()
        {
            isChosen = true;
            view.Stroke = CONFIG.BLUE_TRANSPARENT;
            leftArrow.Stroke = CONFIG.BLUE_TRANSPARENT;
            rightArrow.Stroke = CONFIG.BLUE_TRANSPARENT;
        }
        public void deselect()
        {
            isChosen = false;
            view.Stroke = CONFIG.BLACK;
            leftArrow.Stroke = CONFIG.BLACK;
            rightArrow.Stroke = CONFIG.BLACK;
        }
        public void addTo(Grid grid)
        {
            grid.Children.Add(view);
            grid.Children.Add(leftArrow);
            grid.Children.Add(rightArrow);
        }
        public void removeFrom(Grid grid)
        {
            grid.Children.Remove(view);
            grid.Children.Remove(leftArrow);
            grid.Children.Remove(rightArrow);
        }
        public void redraw(Camera camera)
        {
            view.X1 = (from.x - camera.pos.X) * camera.scale;
            view.Y1 = (from.y - camera.pos.Y) * camera.scale;
            view.X2 = (to.x - camera.pos.X) * camera.scale;
            view.Y2 = (to.y - camera.pos.Y) * camera.scale;

            Vector dir = (new Vector(view.X1, view.Y1) - new Vector(view.X2, view.Y2));
            dir.Normalize();
            leftArrow.X1 = view.X2 + dir.X * CONFIG.NODES_SIZE * camera.scale / 2.0;
            leftArrow.Y1 = view.Y2 + dir.Y * CONFIG.NODES_SIZE * camera.scale / 2.0;
            leftArrow.X2 = leftArrow.X1 + (dir.X + dir.Y) * camera.scale * CONFIG.CONNECTIONS_ARROWS_DROPOUT;
            leftArrow.Y2 = leftArrow.Y1 + (dir.Y - dir.X) * camera.scale * CONFIG.CONNECTIONS_ARROWS_DROPOUT;

            rightArrow.X1 = view.X2 + dir.X * CONFIG.NODES_SIZE * camera.scale / 2.0;
            rightArrow.Y1 = view.Y2 + dir.Y * CONFIG.NODES_SIZE * camera.scale / 2.0;
            rightArrow.X2 = rightArrow.X1 + (dir.X - dir.Y) * camera.scale * CONFIG.CONNECTIONS_ARROWS_DROPOUT;
            rightArrow.Y2 = rightArrow.Y1 + (dir.Y + dir.X) * camera.scale * CONFIG.CONNECTIONS_ARROWS_DROPOUT;

            view.StrokeThickness = CONFIG.CONNECTIONS_SIZE * camera.scale;
            leftArrow.StrokeThickness = CONFIG.CONNECTIONS_SIZE * camera.scale;
            rightArrow.StrokeThickness = CONFIG.CONNECTIONS_SIZE * camera.scale;
        }
    }
    public class Settings
    {
        public int gridSize = 0;
        public int frameDelay = 0;
    }
    public class Camera
    {
        public Point pos = new Point();
        public float scale = 1.0f;
        public Grid view;

        public void scaleUp()
        {
            Point mousePos = Mouse.GetPosition(view);
            pos.X += (mousePos.X - mousePos.X / CONFIG.SCALE_COEF) / scale;
            pos.Y += (mousePos.Y - mousePos.Y / CONFIG.SCALE_COEF) / scale;
            scale *= CONFIG.SCALE_COEF;
        }
        public void scaleDown()
        {
            Point mousePos = Mouse.GetPosition(view);
            pos.X += (mousePos.X - mousePos.X * CONFIG.SCALE_COEF) / scale;
            pos.Y += (mousePos.Y - mousePos.Y * CONFIG.SCALE_COEF) / scale;
            scale /= CONFIG.SCALE_COEF;
        }
        public Point screenToVirtual(Point point)
        {
            return new Point(point.X / scale + pos.X, point.Y / scale + pos.Y);
        }
        public Point virtualToScreen(Point point)
        {
            return new Point((point.X - pos.X) * scale, (point.Y - pos.Y) * scale);
        }
    }
    public class Document
    {
        private enum userState { CALM, CHOOSING, MOVING, GRABBING };

        public string name = "untitled";
        private List<LogicNode> nodes = new List<LogicNode>();
        private List<Connection> connections = new List<Connection>();
        private Grid view;
        private bool isUpdating = false;
        private bool needStopUpdating = false;
        
        private Point areaFrom = new Point(), areaTo = new Point();
        private Grid areaGrid;
        
        private Point prevMousePos;
        
        public Settings settings = new Settings();
        private Camera camera = new Camera();

        private Point grabbingStartPoint = new Point(-1, 0);
        private bool isGrabbing = false;
        private List<KeyValuePair<VisualNode, Point>> relativePoints = new List<KeyValuePair<VisualNode, Point>>();

        private userState state = userState.CALM;

        private Grid grid_grid = new Grid(); // Grid(UIelement), containing visual grid

        public Document(Grid grid)
        {
            setGrid();
            grid.Children.Add(view);
        }
        public void showGrid()
        {

        }
        public void hideGrid()
        {

        }
        public void removeFrom(Grid grid)
        {
            grid.Children.Remove(grid);
        }
        public void hide()
        {
            stopUpdating();
            view.Visibility = Visibility.Hidden;
        }
        public void show()
        {
            view.Visibility = Visibility.Visible;
        }
        private void setGrid()
        {
            view = new Grid();
            view.Width = Double.NaN;
            view.Height = Double.NaN;
            view.Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            view.HorizontalAlignment = HorizontalAlignment.Stretch;
            view.VerticalAlignment = VerticalAlignment.Stretch;
            view.MouseDown += onGridMouseDown;
            view.MouseUp += onGridMouseUp;
            view.MouseWheel += onGridWheelTurn;
            view.Children.Add(grid_grid);
            camera.view = view;
            
            view.MouseMove += onGridMouseMove;
            Grid.SetRow(view, 1);
        }
        public void startUpdating()
        {
            if (!isUpdating)
            {
                Thread updateThread = new Thread(new ThreadStart(cycleUpdating));
                updateThread.Start();
                isUpdating = true;
                needStopUpdating = false;
            }
        }
        public bool getIsUpdating()
        {
            return isUpdating;
        }
        public void stopUpdating()
        {
            if (isUpdating)
                needStopUpdating = true;
        }
        private void recountNodes()
        {
            foreach (LogicNode node in nodes)
                node.recount();
        }
        private void updateNodesStates()
        {
            foreach (LogicNode node in nodes)
                node.update_state();
        }
        public void updateTick()
        {
            if (!isUpdating)
            {
                Thread updateThread = new Thread(new ThreadStart(_updateTick));
                updateThread.Start();
            }
        }
        private void _updateTick()
        {
            try
            {
                recountNodes();
                updateNodesStates();
                reDraw();
            }
            catch
            {

            }
        }
        private void cycleUpdating()
        {
            while (!needStopUpdating)
            {
                _updateTick();
                if (settings.frameDelay != 0)
                    Thread.Sleep(settings.frameDelay);
            }
            isUpdating = false;
        }
        public void addNode(Point screenPos, string func)
        {
            LogicNode node = null;
            Point pos = camera.screenToVirtual(screenPos);
            switch (func)
            {
                case "AND":
                    node = new LogicNode_And(pos, func);
                    break;
                case "OR":
                    node = new LogicNode_Or(pos, func);
                    break;
                case "NOT":
                    node = new LogicNode_Not(pos, func);
                    break;
                default:
                    Environment.Exit(0);
                    break;
            }
            nodes.Add(node);
            
            ((VisualNode)node).redraw(camera);
            view.Children.Add(((VisualNode)node).view);
            ((VisualNode)node).OnLeftMouseButtonDown += onNodeLeftMouseButtonDown;
            ((VisualNode)node).OnLeftMouseButtonUp += onNodeLeftMouseButtonUp;
            node.OnConnectionAdd += onConnectionAdded;
            node.OnConnectionRemove += onConnectionRemoved;
        }
        public void removeNode(LogicNode node)
        {
            removeConnectionsTo(node);
            view.Children.Remove(((VisualNode)node).view);
            nodes.Remove(node);
        }
        private void reDraw()
        {
            foreach (VisualNode node in nodes)
                node.redraw(camera);
            foreach (Connection conn in connections)
                conn.redraw(camera);
        }

        private void onConnectionAdded(LogicNode from, LogicNode to)
        {

            Connection conn = new Connection(((VisualNode)from), ((VisualNode)to));
            conn.addTo(view);
            connections.Add(conn);
            conn.redraw(camera);
            conn.OnLeftMouseButtonDown += onConnectionLeftMouseButtonDown;
            conn.OnLeftMouseButtonUp += onConnectionLeftMouseButtonUp;
        }
        private void removeConnection(Connection conn)
        {
            conn.to.removeInput(conn.from);
        }
        private void onConnectionRemoved(LogicNode from, LogicNode to)
        {
            for (int i = 0; i < connections.Count(); i++)
                if (connections[i].from == from && connections[i].to == to)
                {
                    connections[i].removeFrom(view);
                    connections.RemoveAt(i);
                    i--;
                }
        }
        private void removeConnectionsTo(LogicNode node)
        {
            for (int i = 0; i < connections.Count(); i++)
                if (connections[i].from == node || connections[i].to == node)
                {
                    connections[i].removeFrom(view);
                    connections.RemoveAt(i);
                    i--;
                }

            foreach (LogicNode n in nodes)
                if (n != node)
                    n.removeInput(node);
        }
        private void onConnectionLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (isUpdating)
                return;
        }
        private void onConnectionLeftMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (isUpdating)
                return;
            if (isShiftPressed() && !isAltPressed())
            {
                ((Connection)sender).select();
                return;
            }
            if (!isShiftPressed() && !isAltPressed())
            {
                clearChoice();
                ((Connection)sender).select();
                return;
            }
        }

        public void onKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
                deleteChosen();
            if (isCtrlPressed() && e.Key == Key.A)
                foreach (VisualNode node in nodes)
                    node.select();
        }
        private void deleteChosen()
        {
            for (int i = 0; i < nodes.Count(); i++)
                if (((VisualNode)nodes[i]).isChosen)
                {
                    removeNode(nodes[i]);
                    i--;
                }
            for (int i = 0; i < connections.Count(); i++)
                if (connections[i].isChosen)
                {
                    removeConnection(connections[i]);
                    i--;
                }
        }

        private void onGridWheelTurn(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0)
                camera.scaleUp();
            else
                camera.scaleDown();
            reDraw();
        }
        private void onGridMouseDown(object sender, MouseEventArgs e)
        {
            if (state == userState.CALM && !isShiftPressed() && !isAltPressed() && e.MiddleButton == MouseButtonState.Pressed)
            {
                state = userState.MOVING;
                return;
            }
            if (isUpdating)
                return;
            if (state == userState.CALM && !isAltPressed() && e.LeftButton == MouseButtonState.Pressed)
            {
                if (e.OriginalSource == sender)
                {
                    state = userState.CHOOSING;

                    if (!isShiftPressed())
                        clearChoice();

                    areaGrid = new Grid();
                    areaGrid.Background = CONFIG.BLUE_TRANSPARENT;
                    areaGrid.HorizontalAlignment = HorizontalAlignment.Left;
                    areaGrid.VerticalAlignment = VerticalAlignment.Top;

                    Point mousePos = Mouse.GetPosition(view);
                    areaFrom = camera.screenToVirtual(mousePos);

                    areaTo.X = areaFrom.X;
                    areaTo.Y = areaFrom.Y;

                    areaGrid.Margin = new Thickness((areaFrom.X - camera.pos.X) * camera.scale, (areaFrom.Y - camera.pos.Y) * camera.scale, 0, 0);
                    areaGrid.Width = (areaTo.X - areaFrom.X) * camera.scale;
                    areaGrid.Height = (areaTo.Y - areaFrom.Y) * camera.scale;

                    Grid.SetZIndex(areaGrid, 5);

                    view.Children.Add(areaGrid);
                }
                return;
            }
        }
        private void onGridMouseUp(object sender, MouseEventArgs e)
        {
            if (state == userState.GRABBING)
            {
                isGrabbing = false;
                relativePoints.Clear();
                grabbingStartPoint.X = -1;
            }
            if (state == userState.CHOOSING)
            {
                if (e.LeftButton == MouseButtonState.Released)
                {
                    if (areaGrid != null)
                    {
                        if (isUpdating)
                        {
                            view.Children.Remove(areaGrid);
                            areaGrid = null;
                        }
                        if ((areaFrom.X - areaTo.X == 0) || (areaFrom.Y - areaTo.Y == 0))
                        {
                            view.Children.Remove(areaGrid);
                            areaGrid = null;
                        }
                        foreach (VisualNode node in nodes)
                        {
                            if (!node.isChosen)
                            {
                                double interpolatedX, interpolatedY;
                                interpolatedX = (((VisualNode)node).x - areaFrom.X) / (areaTo.X - areaFrom.X);
                                interpolatedY = (((VisualNode)node).y - areaFrom.Y) / (areaTo.Y - areaFrom.Y);
                                if ((interpolatedX >= 0) && (interpolatedX <= 1) && (interpolatedY >= 0) && (interpolatedY <= 1))
                                    node.select();
                            }
                        }

                        view.Children.Remove(areaGrid);
                        areaGrid = null;
                    }
                }
            }
            
            state = userState.CALM;
        }

        private void onGridMouseMove(object sender, MouseEventArgs e)
        {
            Point mousePos = Mouse.GetPosition(view);
            if (state == userState.GRABBING)
            {
                if (isGrabbing)
                {
                    Point mouseVirtualPos = camera.screenToVirtual(mousePos);
                    for (int i = 0; i < relativePoints.Count(); i++)
                    {
                        Point newPos = new Point(mouseVirtualPos.X + relativePoints[i].Value.X,
                                                 mouseVirtualPos.Y + relativePoints[i].Value.Y);
                        newPos = roundToGrid(newPos);
                        ((VisualNode)relativePoints[i].Key).x = (float)newPos.X;
                        ((VisualNode)relativePoints[i].Key).y = (float)newPos.Y;
                    }
                }
                else
                {
                    if (((Vector)grabbingStartPoint - (Vector)mousePos).LengthSquared > 9)
                        isGrabbing = true;
                }
            }
            if (state == userState.MOVING)
            {
                camera.pos -= (mousePos - prevMousePos) / camera.scale;
                if (areaGrid != null)
                {
                    areaTo = camera.screenToVirtual(mousePos);

                    double width = (areaTo.X - areaFrom.X) * camera.scale;
                    double height = (areaTo.Y - areaFrom.Y) * camera.scale;

                    areaGrid.Margin = new Thickness((areaFrom.X - camera.pos.X) * camera.scale + (width < 0 ? width : 0), (areaFrom.Y - camera.pos.Y) * camera.scale + (height < 0 ? height : 0), 0, 0);
                    areaGrid.Width = Math.Abs(width);
                    areaGrid.Height = Math.Abs(height);
                }
            }
            if (state == userState.CHOOSING)
            {
                areaTo = camera.screenToVirtual(mousePos);

                double width = (areaTo.X - areaFrom.X) * camera.scale;
                double height = (areaTo.Y - areaFrom.Y) * camera.scale;

                areaGrid.Margin = new Thickness((areaFrom.X - camera.pos.X) * camera.scale + (width < 0 ? width : 0), (areaFrom.Y - camera.pos.Y) * camera.scale + (height < 0 ? height : 0), 0, 0);
                areaGrid.Width = Math.Abs(width);
                areaGrid.Height = Math.Abs(height);
            }
            reDraw();
            prevMousePos = mousePos;
        }

        private void onNodeLeftMouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (state == userState.CALM && !isAltPressed() && !isShiftPressed() && ((VisualNode)sender).isChosen)
            {
                state = userState.GRABBING;
                Point mousePos = Mouse.GetPosition(view);
                grabbingStartPoint = mousePos;
                Point virtualMousePos = camera.screenToVirtual(mousePos);
                foreach (VisualNode node in nodes)
                {
                    if (node.isChosen)
                    {
                        relativePoints.Add(new KeyValuePair<VisualNode, Point>(node, new Point(node.x - virtualMousePos.X, node.y - virtualMousePos.Y)));
                    }
                }
            }

        }
        private void onNodeLeftMouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (state == userState.MOVING)
            {
                state = userState.CALM;
            }
            if (isUpdating)
            {
                state = userState.CALM;
                return;
            }
            if (state == userState.GRABBING)
            {
                if (!isGrabbing && !isAltPressed() && !isShiftPressed())
                {
                    clearChoice();
                    ((VisualNode)sender).select();
                }
                isGrabbing = false;
                relativePoints.Clear();
                grabbingStartPoint.X = -1;
            }
            if (state == userState.CALM && isAltPressed() && !isShiftPressed())
            {
                foreach (VisualNode node in nodes)
                    if (node.isChosen)
                        if (node != sender)
                            node.addInput((LogicNode)sender);
            }
            if (state == userState.CALM && !isAltPressed())
            {
                if (!isShiftPressed())
                {
                    clearChoice();
                    ((VisualNode)sender).select();
                }
                else
                {
                    if (((VisualNode)sender).isChosen)
                        ((VisualNode)sender).deselect();
                    else
                        ((VisualNode)sender).select();
                }
            }
            state = userState.CALM;
        }

        private void clearChoice()
        {
            foreach (VisualNode node in nodes)
                if (node.isChosen)
                    node.deselect();
            foreach (Connection conn in connections)
                if (conn.isChosen)
                    conn.deselect();
        }

        private bool isShiftPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
        }
        private bool isAltPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftAlt) || Keyboard.IsKeyDown(Key.RightAlt);
        }
        private bool isCtrlPressed()
        {
            return Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl);
        }

        private Point roundToGrid(Point point)
        {
            if (settings.gridSize == 0)
                return point;
            return new Point(point.X - point.X % settings.gridSize, point.Y - point.Y % settings.gridSize);
        }
        private void resetGrid()
        {
            if (Double.IsNaN(view.ActualWidth))
                return;
            grid_grid.Children.Clear();
            Point startPos = camera.screenToVirtual(new Point(-view.ActualWidth, -view.ActualHeight));
            startPos = camera.virtualToScreen(roundToGrid(startPos));
            grid_grid.Margin = new Thickness(startPos.X, startPos.Y, 0, 0);
            grid_grid.Width = view.ActualWidth * 3;
            grid_grid.Height = view.ActualHeight * 3;
            Grid.SetZIndex(grid_grid, -10);

            int countX, countY;
            countX = (int)(grid_grid.Width / camera.scale);
            countY = (int)(grid_grid.Height / camera.scale);
            for (int i = 0; i < countX; i++)
            {

            }
        }
    }


    public partial class MainWindow : Window
    {
        private List<Document> documents = new List<Document>();
        private int curDoc = -1;

        private Grid holdingGrid;

        public MainWindow()
        {
            InitializeComponent();
            StreamResourceInfo sriCurs = Application.GetResourceStream(new Uri("/Resources/Cursors/grab.cur", UriKind.Relative));
            CONFIG.GRAB_CURSOR = new Cursor(sriCurs.Stream);
            sriCurs = Application.GetResourceStream(new Uri("/Resources/Cursors/grabbing.cur", UriKind.Relative));
            CONFIG.GRABBING_CURSOR = new Cursor(sriCurs.Stream);

            NodeGrid_AND.Cursor = CONFIG.GRAB_CURSOR;
            NodeGrid_OR.Cursor = CONFIG.GRAB_CURSOR;
            NodeGrid_NOT.Cursor = CONFIG.GRAB_CURSOR;

            WindowState = WindowState.Maximized;
            StartGrid.Visibility = Visibility.Visible;
            ToolsScroll.Visibility = Visibility.Hidden;
            if ((bool)EnableGrid_CheckBox.IsChecked)
                GridSize_TextBox.IsEnabled = true;
            else
                GridSize_TextBox.IsEnabled = false;

            AdvancedSettingsGrid.Height = 0;
        }

        private void Button_New_Click(object sender, RoutedEventArgs e)
        {
            addDocument();
            StartGrid.Visibility = Visibility.Hidden;
            ToolsScroll.Visibility = Visibility.Visible;
        }

        private void Button_Open_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_AddNodesGrid_Click(object sender, RoutedEventArgs e)
        {
            if (_AddNodesGrid.Height == 0)
            {
                _AddNodesGrid.Height = Double.NaN;
                Button_AddNodesGrid.Content = "↑ Logic Nodes ↑";
            }
            else
            {
                _AddNodesGrid.Height = 0;
                Button_AddNodesGrid.Content = "↓ Logic Nodes ↓";
            }
        }
        private void Button_FlowControl_Click(object sender, RoutedEventArgs e)
        {
            if (_FlowControlGrid.Height == 0)
            {
                _FlowControlGrid.Height = Double.NaN;
                Button_FlowControl.Content = "↑ Flow Control ↑";
            }
            else
            {
                _FlowControlGrid.Height = 0;
                Button_FlowControl.Content = "↓ Flow Control ↓";
            }
        }
        private void Button_DocumentsControl_Click(object sender, RoutedEventArgs e)
        {
            if (_DocumentsControlGrid.Height == 0)
            {
                _DocumentsControlGrid.Height = Double.NaN;
                Button_DocumentsControl.Content = "↑ Documents Control ↑";
            }
            else
            {
                _DocumentsControlGrid.Height = 0;
                Button_DocumentsControl.Content = "↓ Documents Control ↓";
            }
        }
        private void Button_TableControl_Click(object sender, RoutedEventArgs e)
        {
            if (_TableControlGrid.Height == 0)
            {
                _TableControlGrid.Height = Double.NaN;
                Button_TableControl.Content = "↑ Table Control ↑";
            }
            else
            {
                _TableControlGrid.Height = 0;
                Button_TableControl.Content = "↓ Table Control ↓";
            }
        }

        private void NodeGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (holdingGrid == null)
            {
                Grid grid = new Grid();
                grid.Height = 90;
                grid.Width = 90;
                grid.HorizontalAlignment = HorizontalAlignment.Left;
                grid.VerticalAlignment = VerticalAlignment.Top;

                Ellipse el = new Ellipse();
                el.Width = 86;
                el.Height = 86;
                el.HorizontalAlignment = HorizontalAlignment.Left;
                el.VerticalAlignment = VerticalAlignment.Top;
                el.Margin = new Thickness(2, 2, 2, 2);
                el.Fill = new SolidColorBrush(Color.FromArgb(240, 100, 100, 100));
                el.Cursor = CONFIG.GRABBING_CURSOR;
                grid.Children.Add(el);

                TextBlock text = new TextBlock();
                text.HorizontalAlignment = HorizontalAlignment.Center;
                text.VerticalAlignment = VerticalAlignment.Center;
                text.Width = double.NaN;
                text.Height = double.NaN;
                text.FontWeight = FontWeights.Bold;
                text.Visibility = Visibility.Visible;
                text.FontSize = 20;
                text.Text = ((TextBlock)((Grid)sender).Children[1]).Text;
                text.Cursor = CONFIG.GRABBING_CURSOR;
                grid.Children.Add(text);

                WindowGrid.Children.Add(grid);
                Grid.SetZIndex(grid, 10);
                holdingGrid = grid;

                Point p = e.GetPosition(WindowGrid);
                holdingGrid.Margin = new Thickness(p.X - 45, p.Y - 45, 0, 0);
            }
        }

        private void addDocument()
        {
            documents.Add(new Document(MainGrid));

            StackPanel docPanel = new StackPanel();
            docPanel.HorizontalAlignment = HorizontalAlignment.Left;
            docPanel.VerticalAlignment = VerticalAlignment.Top;
            docPanel.Height = 25;
            docPanel.Width = Double.NaN;
            docPanel.Margin = new Thickness(0, 0, 5, 0);
            docPanel.Orientation = Orientation.Horizontal;
            docPanel.Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            docPanel.MouseLeftButtonUp += onDocumentClick;

            TextBlock name = new TextBlock();
            name.Text = "untitled";
            name.FontSize = 20;
            name.HorizontalAlignment = HorizontalAlignment.Left;
            name.VerticalAlignment = VerticalAlignment.Top;
            name.Margin = new Thickness(0, 0, 5, 0);
            name.Height = 25;
            name.Width = Double.NaN;
            name.Foreground = CONFIG.BLACK;
            docPanel.Children.Add(name);

            Button closeDoc = new Button();
            closeDoc.HorizontalAlignment = HorizontalAlignment.Left;
            closeDoc.VerticalAlignment = VerticalAlignment.Top;
            closeDoc.Height = 25;
            closeDoc.Width = 25;
            closeDoc.Background = new ImageBrush(new BitmapImage(new Uri("Resources/Icons/close.png", UriKind.Relative)));
            closeDoc.BorderBrush = null;
            closeDoc.Click += onCloseDocumentButtonClick;
            docPanel.Children.Add(closeDoc);

            DocumentsStack.Children.Add(docPanel);

            switchDocument(documents.Count() - 1);
        }

        private void onDocumentClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < DocumentsStack.Children.Count; i++)
                if (sender == DocumentsStack.Children[i])
                {
                    switchDocument(i);
                    return;
                }
        }
        private void onCloseDocumentButtonClick(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < documents.Count(); i++)
                if (((StackPanel)DocumentsStack.Children[i]).Children[1] == sender)
                    closeDocument(i);
        }
        private void closeDocument(int index)
        {
            // TODO: ask for save, if needed
            if (index == curDoc)
                if (index == documents.Count() - 1)
                {
                    if (index == 0)
                    {
                        ((ImageBrush)Button_StartSimulation.Background).ImageSource = new BitmapImage(new Uri("Resources/Icons/stop.png", UriKind.Relative));
                        StartGrid.Visibility = Visibility.Visible;
                        ToolsScroll.Visibility = Visibility.Hidden;
                    }
                    else
                    {
                        switchDocument(index - 1);
                    }
                    documents[index].removeFrom(MainGrid);
                    documents.RemoveAt(index);
                    DocumentsStack.Children.RemoveAt(index);
                }
                else
                {
                    documents[index].removeFrom(MainGrid);
                    documents.RemoveAt(index);
                    DocumentsStack.Children.RemoveAt(index);

                    switchDocument(index);
                }
            else
            {
                documents[index].removeFrom(MainGrid);
                documents.RemoveAt(index);
                DocumentsStack.Children.RemoveAt(index);
                if (index < curDoc)
                    curDoc--;
            }
        }

        private void switchDocument(int index)
        {
            if (curDoc != -1)
            {
                if (documents[curDoc].getIsUpdating())
                {
                    documents[curDoc].stopUpdating();
                    ((ImageBrush)Button_StartSimulation.Background).ImageSource = new BitmapImage(new Uri("Resources/Icons/play.png", UriKind.Relative));
                }
                documents[curDoc].hide();
            }
            documents[index].show();
            curDoc = index;

            for (int i = 0; i < documents.Count(); i++)
            {
                if (i != curDoc)
                    ((StackPanel)DocumentsStack.Children[i]).Background = new SolidColorBrush(Color.FromArgb(255, 160, 160, 160));
                else
                    ((StackPanel)DocumentsStack.Children[i]).Background = new SolidColorBrush(Color.FromArgb(255, 200, 200, 200));
            }
        }

        private void WindowGrid_MouseMove(object sender, MouseEventArgs e)
        {
            if (holdingGrid != null)
            {
                Point p = e.GetPosition(WindowGrid);
                holdingGrid.Margin = new Thickness(p.X - 45, p.Y - 45, 0, 0);
            }
        }

        private void WindowGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (holdingGrid != null)
            {
                Point mousePos = Mouse.GetPosition(ViewGrid_default);
                bool isMouseOverViewGrid = (mousePos.X >= 0) && (mousePos.Y >= 0);
                if ((!documents[curDoc].getIsUpdating()) && (isMouseOverViewGrid))
                {
                    WindowGrid.Children.Remove(holdingGrid);
                    documents[curDoc].addNode(mousePos, ((TextBlock)holdingGrid.Children[1]).Text);
                    holdingGrid = null;
                }
                else
                {
                    WindowGrid.Children.Remove(holdingGrid);
                    holdingGrid = null;
                }
            }
        }

        private void Button_ShowAdvancedSettings_Click(object sender, RoutedEventArgs e)
        {
            if (AdvancedSettingsGrid.Height == 0)
                AdvancedSettingsGrid.Height = Double.NaN;
            else
                AdvancedSettingsGrid.Height = 0;
        }

        private void Button_StartSimulation_Click(object sender, RoutedEventArgs e)
        {
            if (documents[curDoc].getIsUpdating())
            {
                documents[curDoc].stopUpdating();
                ((ImageBrush)Button_StartSimulation.Background).ImageSource = new BitmapImage(new Uri("Resources/Icons/play.png", UriKind.Relative));
            }
            else
            {
                documents[curDoc].startUpdating();
                ((ImageBrush)Button_StartSimulation.Background).ImageSource = new BitmapImage(new Uri("Resources/Icons/stop.png", UriKind.Relative));
            }
        }

        private void Button_SimulateFrame_Click(object sender, RoutedEventArgs e)
        {
            if (documents[curDoc].getIsUpdating())
            {
                documents[curDoc].stopUpdating();
                ((ImageBrush)Button_StartSimulation.Background).ImageSource = new BitmapImage(new Uri("Resources/Icons/play.png", UriKind.Relative));
            }
            else
                documents[curDoc].updateTick();
        }

        private void Button_NewFile_Click(object sender, RoutedEventArgs e)
        {
            addDocument();
        }

        private void Button_Load_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Save_Click(object sender, RoutedEventArgs e)
        {

        }

        private void Button_SavAs_Click(object sender, RoutedEventArgs e)
        {

        }

        private void TheWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (curDoc != -1)
                documents[curDoc].onKeyDown(sender, e);
        }

        private void FrameDelay_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            int res;
            bool success = Int32.TryParse(FrameDelay_TextBox.Text, out res);
            if (success && (res >= 0) && (res <= 10000))
                documents[curDoc].settings.frameDelay = res;
            else
            {
                MessageBox.Show("Value must be integer in range from 0 to 10000.", "Wrong value", MessageBoxButton.OK, MessageBoxImage.Error);
                FrameDelay_TextBox.Text = documents[curDoc].settings.frameDelay.ToString();
            }
        }

        private void ShowGrid_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            documents[curDoc].showGrid();
        }

        private void ShowGrid_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            documents[curDoc].hideGrid();
        }

        private void EnableGrid_CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            GridSize_TextBox.IsEnabled = true;
            documents[curDoc].settings.gridSize = Int32.Parse(GridSize_TextBox.Text);
        }

        private void EnableGrid_CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            GridSize_TextBox.IsEnabled = false;
            documents[curDoc].settings.gridSize = 0;
        }

        private void GridSize_TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            int res;
            bool success = Int32.TryParse(GridSize_TextBox.Text, out res);
            if (success && (res >= 10) && (res <= 500))
                documents[curDoc].settings.gridSize = res;
            else
            {
                MessageBox.Show("Value must be integer in range from 10 to 500.", "Wrong value", MessageBoxButton.OK, MessageBoxImage.Error);
                FrameDelay_TextBox.Text = documents[curDoc].settings.gridSize.ToString();
            }
        }
    }
}
