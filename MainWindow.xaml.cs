using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace SnakeGame
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static int sep = 20;
        bool moveR = false;
        bool moveUp = false;
        bool moveL = false;
        bool moveD = false;
        bool gamestart = false;
        Ellipse ellipse = new Ellipse();
        Ellipse appleellipse = new Ellipse();
        DispatcherTimer timer = new DispatcherTimer();
        List<Ellipse> tailList = new List<Ellipse>();
        int score = 0;

        public MainWindow()
        {
            InitializeComponent();
            RecordLabel.Content = $"Record: {Properties.Settings.Default.record}";
            DrawField();
            Dispatcher.BeginInvoke(new Action(() => DrawSnakeHead()),System.Windows.Threading.DispatcherPriority.Render);
            Dispatcher.BeginInvoke(new Action(() => DrawApples()), System.Windows.Threading.DispatcherPriority.Render);
            timer.Interval = TimeSpan.FromMilliseconds(20);
            timer.Tick += Timer_Tick;
        }

        private bool CheckAppleEaten()
        {
            
            // Create Rect for both the snake and apple
            Rect snakeRect = new Rect(Canvas.GetLeft(ellipse), Canvas.GetTop(ellipse), ellipse.Width, ellipse.Height);
            Rect appleRect = new Rect(Canvas.GetLeft(appleellipse), Canvas.GetTop(appleellipse), appleellipse.Width, appleellipse.Height);
            // Check if the snake and apple intersect
            if (snakeRect.IntersectsWith(appleRect))
            {
                score++;
                if (score > Properties.Settings.Default.record)
                {
                    Properties.Settings.Default.record = score;
                    Properties.Settings.Default.Save();
                    RecordLabel.Content = $"Record: {Properties.Settings.Default.record}";
                }
                
                // If they intersect, remove the apple and return true
                FieldGrid.Children.Remove(appleellipse);
                Ellipse tail = new Ellipse()
                {
                    Width = sep - 5,
                    Height = sep - 5,
                    Fill = Brushes.LightBlue,
                };
                
                tailList.Add(tail);
                FieldGrid.Children.Add(tail);
                ScoreLabel.Content = $"Score: {score}";
                return true;
            }
            return false;

        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // check if snake eat the apple
            if (CheckAppleEaten())
            {
                DrawApples();
            }
            Dispatcher.BeginInvoke(new Action(() =>
            {
                // Queue to store previous head positions (global variable)
                Queue<(double X, double Y)> positionQueue = new Queue<(double, double)>();

                if (gamestart)
                {
                    double previousHeadX = Canvas.GetLeft(ellipse);
                    double previousHeadY = Canvas.GetTop(ellipse);

                    // Move the head first and be sure to be inside recangles in UI
                    if (moveR) Canvas.SetLeft(ellipse, previousHeadX + sep);
                    if (moveL) Canvas.SetLeft(ellipse, previousHeadX - sep);
                    if (moveUp) Canvas.SetTop(ellipse, previousHeadY - sep);
                    if (moveD) Canvas.SetTop(ellipse, previousHeadY + sep);

                    // Store the new head position in the queue
                    positionQueue.Enqueue((previousHeadX, previousHeadY));

                    // Keep the queue size equal to the tail size
                    while (positionQueue.Count > tailList.Count)
                    {
                        positionQueue.Dequeue(); // Remove the oldest position
                    }

                    // Convert the queue to an array for easier access
                    var positions = positionQueue.ToArray();

                    // Move each segment from LAST to FIRST
                    for (int i = tailList.Count - 1; i > 0; i--)
                    {
                        // Each segment takes the position of the one ahead of it
                        double prevX = Canvas.GetLeft(tailList[i - 1]);
                        double prevY = Canvas.GetTop(tailList[i - 1]);

                        Canvas.SetLeft(tailList[i], prevX);
                        Canvas.SetTop(tailList[i], prevY);
                    }

                    // The first tail segment follows the old head position
                    if (tailList.Count > 0 && positions.Length > 0)
                    {
                        Canvas.SetLeft(tailList[0], positions[0].X);
                        Canvas.SetTop(tailList[0], positions[0].Y);
                    }

                    // Check for boundary collision (Game Over)
                    double newLeft = Canvas.GetLeft(ellipse);
                    double newTop = Canvas.GetTop(ellipse);
                    if (newLeft < 0 || newLeft + ellipse.Width >= FieldGrid.ActualWidth ||
                        newTop < 0 || newTop + ellipse.Height >= FieldGrid.ActualHeight)
                    {
                        timer.Stop();
                        MessageBoxResult result = MessageBox.Show($"Game Over\nscore = {score}\nDo you want to restart the game?", "Snake", MessageBoxButton.YesNo);

                        if (result == MessageBoxResult.Yes)
                        {
                            if (Environment.ProcessPath != null)
                            {
                                Process.Start(Environment.ProcessPath);
                            }
                        }
                        Application.Current.Shutdown();
                    }

                    // Check for head hit the body (Game Over)
                    if (tailList.Count > 0)
                    {
                        Rect snakeRect = new Rect(Canvas.GetLeft(ellipse), Canvas.GetTop(ellipse), ellipse.Width, ellipse.Height);
                        foreach (Ellipse ellipse in tailList)
                        {
                            Rect body = new Rect(Canvas.GetLeft(ellipse), Canvas.GetTop(ellipse), ellipse.Width, ellipse.Height);
                            if (snakeRect.IntersectsWith(body))
                            {
                                timer.Stop();
                                MessageBoxResult result = MessageBox.Show($"Game Over\nscore = {score}\nDo you want to restart the game?", "Snake", MessageBoxButton.YesNo);

                                if (result == MessageBoxResult.Yes)
                                {
                                    if (Environment.ProcessPath != null)
                                    {
                                        Process.Start(Environment.ProcessPath);
                                    }
                                }
                                Application.Current.Shutdown();
                            }
                        }
                    }
                }

            }), DispatcherPriority.Render);
        }

        private void DrawApples()
        {
            Random random = new Random();
            List<int> separatorsX = new List<int>();
            List<int> separatorsY = new List<int>();
            for (int i = 0; i < FieldGrid.ActualHeight - sep; i += sep) // <= to ensure full coverage
            {
                if (tailList.Count > 0)
                {
                    foreach (Ellipse body in tailList)
                    {
                        //Prohibide apples X coordinate to not show up where is tail or head of snake
                        if ((Canvas.GetTop(body) != i + 3) || (Canvas.GetTop(ellipse) != i + 3))
                        {
                            separatorsY.Add(i + 3);
                        }
                    }
                }
                else
                {
                    //Prohibide apples X coordinate to not show up where is head of snake
                    if (Canvas.GetTop(ellipse) != i + 3)
                    {
                        separatorsY.Add(i + 3);
                    }
                }
            }
            for (int i = 0; i < FieldGrid.ActualWidth - sep; i += sep)
            {
                if (tailList.Count > 0)
                {
                    foreach (Ellipse body in tailList)
                    {
                        //Prohibide apples Y coordinate to not show up where is tail or head of snake
                        if ((Canvas.GetLeft(body) != i + 3) || (Canvas.GetTop(ellipse) != i + 3))
                        {
                            separatorsX.Add(i + 3);
                        }
                    }
                }
                else
                {
                    //Prohibide apples Y coordinate to not show up where is head of snake
                    if (Canvas.GetTop(ellipse) != i + 3)
                    {
                        separatorsX.Add(i + 3);
                    }
                }
            }

            // Get random row and column
            int row = random.Next(0, separatorsX.Count); // Random column index
            int col = random.Next(0, separatorsY.Count); // Random row index

            int randomX = separatorsX[row];
            int randomY = separatorsY[col];
            appleellipse = new Ellipse
            {
                Width = sep - 5,
                Height = sep - 5,
                Fill = Brushes.LightGreen,
            };
            Canvas.SetLeft(appleellipse, randomX);
            Canvas.SetTop(appleellipse, randomY);
            FieldGrid.Children.Add(appleellipse);
        }

        private void DrawField()
        {
            Dispatcher.BeginInvoke(new Action(() =>
            {
                FieldGrid.UpdateLayout();

                // Draw horizontal lines
                for (int i = 0; i < FieldGrid.ActualHeight; i+=sep) // <= to ensure full coverage
                {
                    double y = i;
                    Line line = new Line
                    {
                        X1 = 0,
                        X2 = FieldGrid.ActualWidth,
                        Y1 = y,
                        Y2 = y,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    FieldGrid.Children.Add(line);
                }
                //Draw vertical Lines
                for (int i = 0; i < FieldGrid.ActualWidth; i+=sep)
                {
                    double x = i;
                    Line line = new Line
                    {
                        Y1 = 0,
                        Y2 = FieldGrid.ActualHeight,
                        X1 = x,
                        X2 = x,
                        Stroke = Brushes.Black,
                        StrokeThickness = 1
                    };
                    FieldGrid.Children.Add(line);
                }
            }), System.Windows.Threading.DispatcherPriority.Render);
        }
        private void DrawSnakeHead()
        {
            ellipse = new Ellipse
            {
                Width = sep - 5,
                Height = sep - 5,
                Fill = Brushes.Red,
            };
            //make sure the head of snake is inside rectangles 
            Canvas.SetLeft(ellipse, FieldGrid.ActualWidth / 2 - ellipse.Width / 2);
            Canvas.SetTop(ellipse, FieldGrid.ActualHeight / 2 - ellipse.Width / 2);
            FieldGrid.Children.Add(ellipse);
        }

        private void FieldGrid_PreviewKeyUp(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (!gamestart)
            {
                gamestart = true;
                timer.Start();
                timer.Interval = TimeSpan.FromMilliseconds(200);
            }
            switch (e.Key)
            {
                case (Key.Right):
                    if (!moveL)
                    {
                        moveR = true;
                        moveUp = false;
                        moveD = false;
                    }
                    break;
                case (Key.Left):
                    if (!moveR)
                    {
                        moveL = true;
                        moveUp = false;
                        moveD = false;
                    }
                    break;
                case (Key.Up):
                    if (!moveD)
                    {
                        moveUp = true;
                        moveL =
                            moveR = false;
                    }
                    break;
                case (Key.Down):
                    if (!moveUp)
                    {
                        moveD = true;
                        moveL =
                            moveR = false;
                    }
                    break;
            }
        }
    }
}