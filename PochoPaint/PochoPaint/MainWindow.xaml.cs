using Microsoft.Win32;
using PochoPaint.models;
using System;
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
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace PochoPaint;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    private int diameter = (int)BrushesSize.Small;
    private Brush brushColor = Brushes.Black;

    private bool isDrawing = false;
    private bool isDragging = false;

    private readonly List<UIElement> elementsWriting = new();
    private readonly Stack<Polyline> undoStack = new();
    private readonly Stack<Polyline> redoStack = new();

    private CursorMode cursorMode = CursorMode.Painting;
    private Polyline? selectedPolyline = null;

    private DateTime? clickDownTime;
    private int timeToDrag = 250;

    private int lastIndex = 0;

    private bool isMenuVisible = false;

    public MainWindow()
    {
        InitializeComponent();
    }

    #region Dragging
    private void EndDragginPolylineSelected(Point mouse)
    {
        if (selectedPolyline == null) return;
        var points = selectedPolyline.Points.ToArray();
        var last = points.Last();
        var dx = mouse.X - last.X;
        var dy = mouse.Y - last.Y;
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Point(points[i].X + dx, points[i].Y + dy);
        }
        selectedPolyline.Points.Clear();
        foreach (var point in points)
        {
            selectedPolyline.Points.Add(point);
        }
        isDragging = false;
    }

    private void MoveSelectedPolyline(Point mouse)
    {
        if (selectedPolyline == null) return;
        var points = selectedPolyline.Points.ToArray();
        var last = points.Last();
        var dx = mouse.X - last.X;
        var dy = mouse.Y - last.Y;
        for (int i = 0; i < points.Length; i++)
        {
            points[i] = new Point(points[i].X + dx, points[i].Y + dy);
        }
        selectedPolyline.Points.Clear();
        foreach (var point in points)
        {
            selectedPolyline.Points.Add(point);
        }   
    }

    private void SelectAt(Point point)
    {
        var elements = undoStack.ToArray();
        foreach (var element in elements)
        {
            foreach (var pointPolyline in element.Points)
            {
                if (Math.Abs(point.X - pointPolyline.X) < diameter / 2 && Math.Abs(point.Y - pointPolyline.Y) < diameter / 2)
                {
                    SelectPolyline(element);
                    return;
                }
            }
        }

        UnselectPolyline();
    }

    private void SelectPolyline(Polyline polyline)
    {
        UnselectPolyline();
        selectedPolyline = polyline;
        polyline.Stroke = Brushes.Yellow;
    }

    private void UnselectPolyline()
    {
        if (selectedPolyline != null)
        {
            selectedPolyline.Stroke = brushColor;
            selectedPolyline = null;
        }
    }
    #endregion

    #region Paint
    private void PaintCircle(Brush circleColor, Point point)
    {
        Ellipse circle = new Ellipse
        {
            Width = diameter,
            Height = diameter,
            Fill = circleColor
        };

        Canvas.SetLeft(circle, point.X - (diameter / 2));
        Canvas.SetTop(circle, point.Y - (diameter / 2));

        PaintCanvas.Children.Add(circle);
        elementsWriting.Add(circle); // Agregar a la lista de elementos dibujados
    }

    private void Canvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (isDrawing)
        {
            PaintCircle(brushColor, e.GetPosition(PaintCanvas));
        }
        if (isDragging && clickDownTime != null && DateTime.Now.Subtract(clickDownTime.Value).TotalMilliseconds > timeToDrag)
        {
            MoveSelectedPolyline(e.GetPosition(PaintCanvas));
        }
    }

    private void PaintCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        switch (cursorMode)
        {
            case CursorMode.Painting:
                isDrawing = true;
                lastIndex = PaintCanvas.Children.Count; // Guardar el índice del último elemento dibujado
                break;
            case CursorMode.Selecting:
                clickDownTime = DateTime.Now;
                isDragging = true;
                SelectAt(e.GetPosition(PaintCanvas));
                break;
        }
    }

    private void PaintCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        isDrawing = false;
        isDragging = false;
        switch (cursorMode)
        {
            case CursorMode.Painting:
                PaintPolyline(brushColor, e.GetPosition(PaintCanvas));
                break;
            case CursorMode.Selecting:
                if (clickDownTime == null || DateTime.Now.Subtract(clickDownTime.Value).TotalMilliseconds <= timeToDrag) return;
                EndDragginPolylineSelected(e.GetPosition(PaintCanvas));
                break;
        }   
    }

    private void PaintPolyline(Brush brushColor, Point mouse)
    {
        // Crear un Polyline con los puntos dibujados
        Polyline polyline = new Polyline
        {
            Stroke = brushColor,
            StrokeThickness = diameter
        };
        foreach (var element in elementsWriting)
        {
            polyline.Points.Add(new Point(Canvas.GetLeft(element) + (diameter / 2), Canvas.GetTop(element) + (diameter / 2)));
        }
        PaintCanvas.Children.RemoveRange(lastIndex, elementsWriting.Count); // Eliminar los elementos dibujados
        PaintCanvas.Children.Add(polyline);
        elementsWriting.Clear(); // Limpiar la lista de puntos dibujados
        undoStack.Push(polyline); // Agregar la lista actual al historial de deshacer
    }
    #endregion

    #region RadioButtons
    private void RadioBlack_Checked(object sender, RoutedEventArgs e)
    {
        brushColor = Brushes.Black;
    }

    private void RadioRed_Checked(object sender, RoutedEventArgs e)
    {
        brushColor = Brushes.Red;
    }

    private void RadioGreen_Checked(object sender, RoutedEventArgs e)
    {
        brushColor = Brushes.Green;
    }

    private void RadioBlue_Checked(object sender, RoutedEventArgs e)
    {
        brushColor = Brushes.Blue;
    }

    private void RadioSmall_Checked(object sender, RoutedEventArgs e)
    {
        diameter = (int)BrushesSize.Small;
    }

    private void RadioMedium_Checked(object sender, RoutedEventArgs e)
    {
        diameter = (int)BrushesSize.Medium;
    }

    private void RadioLarge_Checked(object sender, RoutedEventArgs e)
    {
        diameter = (int)BrushesSize.Large;
    }

    private void Painting_Checked(object sender, RoutedEventArgs e)
    {
        cursorMode = CursorMode.Painting;
        UnselectPolyline();
    }

    private void Selecting_Checked(object sender, RoutedEventArgs e)
    {
        cursorMode = CursorMode.Selecting;
        UnselectPolyline();
    }
    #endregion

    #region Open/Save/New
    private bool CheckChanges()
    {
        if (undoStack.Count > 0)
        {
            // Dialogo de confirmación
            MessageBoxResult result = MessageBox.Show("¿Hay cambios sin guardar, desea guardar los cambios?", "Confirmación", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes) // Guardar
            {
                SaveDialog();
            }
            else if (result != MessageBoxResult.No) // Cancelar
            {
                return false;
            }
        }
        return true;
    }

    private void Open_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckChanges()) return;
        OpenFileDialog fg = new OpenFileDialog();
        fg.Filter = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg|BMP (*.bmp)|*.bmp";
        if (fg.ShowDialog() == true)
        {
            // Pintar en el canvas PaintCanvas
            BitmapImage bitmap = new BitmapImage(new Uri(fg.FileName));
            Image image = new Image
            {
                Source = bitmap,
                Width = bitmap.Width,
                Height = bitmap.Height
            };
            PaintCanvas.Children.Clear();
            PaintCanvas.Children.Add(image);
        }
    }

    private void New_Click(object sender, RoutedEventArgs e)
    {
        if (!CheckChanges()) return;          
        PaintCanvas.Children.Clear();
        undoStack.Clear();
        redoStack.Clear();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (undoStack.Count == 0) return;
        SaveDialog();
    }

    private void SaveDialog()
    {
        SaveFileDialog fg = new SaveFileDialog();
        fg.Filter = "PNG (*.png)|*.png|JPEG (*.jpg)|*.jpg|BMP (*.bmp)|*.bmp";
        if (fg.ShowDialog() == true)
        {
            // Pintar en el canvas PaintCanvas
            RenderTargetBitmap rtb = new RenderTargetBitmap(
                (int)PaintCanvas.RenderSize.Width,
                (int)PaintCanvas.RenderSize.Height,
                96d, 96d, PixelFormats.Default);
            rtb.Render(PaintCanvas);
            BitmapEncoder pngEncoder = new PngBitmapEncoder();
            pngEncoder.Frames.Add(BitmapFrame.Create(rtb));
            using (var fs = System.IO.File.OpenWrite(fg.FileName))
            {
                pngEncoder.Save(fs);
            }
        }
    }

    private void SaveCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = undoStack.Count > 0;
    }
    #endregion

    #region Undo/Redo
    private void Undo_Click(object sender, RoutedEventArgs e)
    {
        if (undoStack.Count == 0) return;
        var last = undoStack.Pop();
        redoStack.Push(last);
        PaintCanvas.Children.Clear();
        if (undoStack.Count == 0) return;
        foreach (var element in undoStack.ToArray())
        {
            PaintCanvas.Children.Add(element);
        }
    }

    private void UndoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = undoStack.Count > 0;
    }

    private void Redo_Click(object sender, RoutedEventArgs e)
    {
        if (redoStack.Count == 0) return;
        var last = redoStack.Pop();
        undoStack.Push(last);
        PaintCanvas.Children.Add(undoStack.Peek());
    }
    private void RedoCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = redoStack.Count > 0;
    }
    #endregion

    #region Copy/Paste/Cut/Delete
    private void Copy_Click(object sender, RoutedEventArgs e)
    {
        if (selectedPolyline == null) return;
        var points = selectedPolyline.Points.ToArray();
        Clipboard.SetData("Polyline", points);
    }

    private void CopyCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = selectedPolyline != null;
    }

    private void Paste_Click(object sender, RoutedEventArgs e)
    {
        if (!Clipboard.ContainsData("Polyline")) return;
        PasteFromClipboard((Point[])Clipboard.GetData("Polyline"));
    }

    private void PasteFromClipboard(Point[] points)
    {
        var polyline = new Polyline
        {
            Stroke = brushColor,
            StrokeThickness = diameter
        };
        foreach (var point in points)
        {
            polyline.Points.Add(point);
        }
        SelectPolyline(polyline);
        isDragging = true;
        PaintCanvas.Children.Add(polyline);
        undoStack.Push(polyline);
    }

    private void PasteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = Clipboard.ContainsData("Polyline");
    }

    private void Cut_Click(object sender, RoutedEventArgs e)
    {
        if (selectedPolyline == null) return;
        var points = selectedPolyline.Points.ToArray();
        Clipboard.SetData("Polyline", points);
        PaintCanvas.Children.Remove(selectedPolyline);
        undoStack.Pop();
        UnselectPolyline();
    }

    private void CutCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = selectedPolyline != null;
    }

    private void Delete_Click(object sender, RoutedEventArgs e)
    {
        if (selectedPolyline == null) return;
        PaintCanvas.Children.Remove(selectedPolyline);
        redoStack.Push(undoStack.Pop());      
        UnselectPolyline();
    }

    private void DeleteCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
    {
        e.CanExecute = selectedPolyline != null;
    }
    #endregion

    #region Animation
    private void ToggleMenuVisibility(object sender, RoutedEventArgs e)
    {
        if (!isMenuVisible)
        {
            ShowMenu();
            isMenuVisible = true;
        }
        else
        {
            HideMenu();
            isMenuVisible = false;
        }
    }
    private void ShowMenu()
    {
        SideMenu.Visibility = Visibility.Visible;

        DoubleAnimation animation = new DoubleAnimation();
        animation.Duration = TimeSpan.FromSeconds(0.15);
        animation.To = 0;
        ((TranslateTransform)SideMenu.RenderTransform).BeginAnimation(TranslateTransform.XProperty, animation);
    }

    private void HideMenu()
    {
        DoubleAnimation animation = new DoubleAnimation();
        animation.Duration = TimeSpan.FromSeconds(0.15);
        animation.To = -90; // Ajusta este valor según la posición inicial del menú
        animation.Completed += (s, e) => SideMenu.Visibility = Visibility.Collapsed;
        ((TranslateTransform)SideMenu.RenderTransform).BeginAnimation(TranslateTransform.XProperty, animation);
    }

    #endregion
}
