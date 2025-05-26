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

namespace SchoolAttendance
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {

        public MainWindow()
        {
            InitializeComponent();
            ContentFrame.Navigate(new Attendance_Page());
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            if (WindowManager.ActiveWindowCount > 0)
            {
                MessageBox.Show(
                    "Please close all section windows before exiting the application.",
                    "Cannot Exit",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return; // Prevent shutdown
            }

            Application.Current.Shutdown();
        }

        private void AttendanceButton_Click(object sender, RoutedEventArgs e)
        {
            AttendanceBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A"));
            SectionBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#090909"));
            ContentFrame.Navigate(new Attendance_Page());
        }

        private void SectionButton_Click(object sender, RoutedEventArgs e)
        {
            SectionBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1A1A1A"));
            AttendanceBtn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#090909"));
            ContentFrame.Navigate(new Section_Page());
        }
    }
}
