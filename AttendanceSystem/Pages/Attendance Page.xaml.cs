using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
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

namespace SchoolAttendance
{
    /// <summary>
    /// Interaction logic for Attendance_Page.xaml
    /// </summary>
    public partial class Attendance_Page : Page
    {

        public Attendance_Page()
        {
            InitializeComponent();
            this.IsVisibleChanged += AttendancePage_IsVisibleChanged;
        }

        public void ReloadAttendance()
        {
            AttendanceRowsPanel.Children.Clear();
            LoadAttendance();
        }

        private void AttendancePage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                AttendanceRowsPanel.Children.Clear();
                LoadAttendance();
            }
        }

        private void LoadAttendance()
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AttendanceDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Get all tables in the attendance database
                    List<string> attendanceTables = new List<string>();
                    using (SQLiteCommand command = new SQLiteCommand(
                        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';",
                        connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            attendanceTables.Add(reader.GetString(0));
                        }
                    }

                    // Process each attendance table
                    foreach (string tableName in attendanceTables)
                    {
                        // Parse the table name to extract section and date (format: SECTION_MM-dd-yyyy_hh-mmtt)
                        string[] parts = tableName.Split('_');
                        if (parts.Length < 3) continue; // Skip invalid table names

                        string subject = System.Text.RegularExpressions.Regex.Replace(
                            parts[0],
                            "([a-z])([A-Z0-9])",
                            "$1 $2"
                            );
                        string section = parts[1];
                        string datePart = parts[2];

                        // if (!DateTime.TryParse($"{datePart} {timePart.Replace("-", ":")}", out DateTime date))

                        if (!DateTime.TryParseExact(datePart, "MM-dd-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
                            continue;

                        // Get attendance counts from the table
                        string query = $@"
                            SELECT 
                                SUM(CASE WHEN IsPresent = 1 THEN 1 ELSE 0 END) as PresentCount,
                                SUM(CASE WHEN IsAbsent = 1 THEN 1 ELSE 0 END) as AbsentCount,
                                SUM(CASE WHEN IsLate = 1 THEN 1 ELSE 0 END) as LateCount,
                                COUNT(*) as TotalCount
                            FROM `{tableName}`";

                        using (SQLiteCommand countCommand = new SQLiteCommand(query, connection))
                        using (SQLiteDataReader countReader = countCommand.ExecuteReader())
                        {
                            if (countReader.Read())
                            {
                                int presentCount = countReader.IsDBNull(0) ? 0 : countReader.GetInt32(0);
                                int absentCount = countReader.IsDBNull(1) ? 0 : countReader.GetInt32(1);
                                int lateCount = countReader.IsDBNull(2) ? 0 : countReader.GetInt32(2);
                                int totalCount = countReader.IsDBNull(3) ? 0 : countReader.GetInt32(3);

                                // Create attendance card
                                Button card = CreateAttendanceCard(date, section, subject, presentCount, absentCount, lateCount, totalCount);
                                AttendanceRowsPanel.Children.Insert(0, card);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading attendance: {ex.Message}", "Database Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Button CreateAttendanceCard(DateTime date, string section, string subject, int presentCount, int absentCount, int lateCount, int totalCount)
        {
            var cardButton = new Button
            {
                Cursor = Cursors.Hand,
                Content = CreateCardContent(date, section, subject, presentCount, absentCount, lateCount, totalCount),
                Tag = new { Subject = subject, Date = date, Section = section }  // Store data for click handler
            };

            // Add click handler
            cardButton.Click += (sender, e) =>
            {
                var button = sender as Button;
                var data = button.Tag as dynamic;
                var attendanceWindow = new ShowAttendanceWindow(data.Subject, (DateTime)data.Date, (string)data.Section);
                WindowManager.ShowWindow<ShowAttendanceWindow>(
                    () => attendanceWindow,
                    () =>
                    {
                        NavigationService.Refresh();
                        ReloadAttendance();
                    }
                );
            };

            return cardButton;
        }

        private void CreateAttendance(object sender, RoutedEventArgs e)
        {
            WindowManager.ShowWindow<AttendancingWindow>(
                    () => new AttendancingWindow(),
                    () =>
                    {
                        NavigationService.Refresh();
                        ReloadAttendance();
                    }
                );
        }

        private Grid CreateCardContent(DateTime date, string section, string subject, int presentCount, int absentCount, int lateCount, int totalCount)
        {

            var grid = new Grid
            {
                Height = 40,
                Width = 840,
                Margin = new Thickness(0, 2, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Cursor = Cursors.Hand
            };

            // Column definitions
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(30) }); // Gap
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(100) }); // Date
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(110) }); // Section
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(210) });  // subject
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) }); // Gap
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });  // Present
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(40) }); // Gap
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });  // Absent
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) }); // Gap
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });  // Late
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) }); // Gap
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) });  // Total

            // Date TextBlock
            var dateText = new TextBlock
            {
                Text = date.ToString("MMM dd, yyyy"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold
            };
            Grid.SetColumn(dateText, 1);
            grid.Children.Add(dateText);

            // Section TextBlock
            var sectionText = new TextBlock
            {
                Text = section,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                Foreground = Brushes.White
            };
            Grid.SetColumn(sectionText, 2);
            grid.Children.Add(sectionText);

            // Subject TextBlock
            var subjectText = new TextBlock
            {
                Text = subject,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(10, 0, 0, 0),
                Foreground = Brushes.White
            };
            Grid.SetColumn(subjectText, 3);
            grid.Children.Add(subjectText);

            AddCountTextBlock(grid, 5, presentCount.ToString(), Brushes.LightGreen);
            AddCountTextBlock(grid, 7, absentCount.ToString(), Brushes.OrangeRed);
            AddCountTextBlock(grid, 9, lateCount.ToString(), Brushes.Yellow);
            AddCountTextBlock(grid, 11, totalCount.ToString(), Brushes.White);

            return grid;
        }

        private void AddCountTextBlock(Grid grid, int column, string text, Brush foreground)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = foreground,
                FontWeight = FontWeights.Medium
            };
            Grid.SetColumn(textBlock, column);
            grid.Children.Add(textBlock);
        }
    }
}
