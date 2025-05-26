using SchoolAttendance.UI.Notifier;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
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
using System.Windows.Shapes;
using static System.Collections.Specialized.BitVector32;
using static System.Data.Entity.Infrastructure.Design.Executor;

namespace SchoolAttendance
{
    /// <summary>
    /// Interaction logic for AttendancingWindow.xaml
    /// </summary>
    public partial class AttendancingWindow : Window
    {
    
        private int rowCount = 0;
        private static string tablename;
        // private Grid studentRow;

        public AttendancingWindow()
        {
            InitializeComponent();
            LoadTableList();

            CBHS.Unchecked += CheckBox_Unchecked;
            CBT.Unchecked += CheckBox_Unchecked;
        }

        private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // If both checkboxes are unchecked, load the full table list
            if (CBHS.IsChecked == false && CBT.IsChecked == false)
            {
                LoadTableList();
            }
        }       

        private void CBHS_Checked(object sender, RoutedEventArgs e)
        {
            CBT.IsChecked = false;
            LoadTableListHighSchool();
        }

        private void CBT_Checked(object sender, RoutedEventArgs e)
        {
            CBHS.IsChecked = false;
            LoadTableListTertiary();
        }

        private void LoadTableList()
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SectionDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                var tables = new List<string>();

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Get all tables
                    using (SQLiteCommand command = new SQLiteCommand(
                        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'sqlite_%';",
                        connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader.GetString(0);
                            tables.Add(tableName);
                        }
                    }   
                }
                // Sort tables alphabetically and set as ComboBox items
                TableSelectionComboBox.ItemsSource = tables.OrderBy(t => t).ToList();

                // Make sure the ComboBox displays the values properly
                TableSelectionComboBox.DisplayMemberPath = ""; // Display the string directly
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table list: {ex.Message}", "Database Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTableListHighSchool()
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SectionDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                var tables = new List<string>();

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Get all tables
                    using (SQLiteCommand command = new SQLiteCommand(
                        "SELECT name FROM sqlite_master WHERE type='table' AND name NOT LIKE 'BS%' AND name NOT LIKE 'sqlite_%';",
                        connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader.GetString(0);
                            tables.Add(tableName);
                        }
                    }
                }

                TableSelectionComboBox.ItemsSource = tables.OrderBy(t => t).ToList();

                // Make sure the ComboBox displays the values properly
                TableSelectionComboBox.DisplayMemberPath = ""; // Display the string directly
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table list: {ex.Message}", "Database Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadTableListTertiary()
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SectionDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                var tables = new List<string>();

                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // Get all tables
                    using (SQLiteCommand command = new SQLiteCommand(
                        "SELECT name FROM sqlite_master WHERE type='table' AND name LIKE 'BS%';",
                        connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader.GetString(0);
                            tables.Add(tableName);
                        }
                    }
                }
                TableSelectionComboBox.ItemsSource = tables.OrderBy(t => t).ToList();

                // Make sure the ComboBox displays the values properly
                TableSelectionComboBox.DisplayMemberPath = ""; // Display the string directly
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading table list: {ex.Message}", "Database Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TableSelectionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TableSelectionComboBox.SelectedItem is string selectedTable)
            {
                StudentRowsPanel.Children.Clear();
                rowCount = 0;
                tablename = selectedTable;

                LoadStudents(selectedTable);
            }
        }

        private void LoadStudents(string tableName)
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SectionDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    string query = $@"
                        SELECT 
                            StudentNumber, 
                            LastName, 
                            FirstName, 
                            MiddleName
                        FROM `{tableName}` 
                        ORDER BY LastName, FirstName";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string studNum = reader["StudentNumber"].ToString();
                            string lastName = reader["LastName"].ToString();
                            string firstName = reader["FirstName"].ToString();
                            string middleName = reader["MiddleName"].ToString();

                            Grid studentRow = CreateStudentRow(studNum, lastName, firstName, middleName);

                            StudentRowsPanel.Children.Add(studentRow);
                            rowCount++;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading students: {ex.Message}", "Database Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private Grid CreateStudentRow(string studNum, string lastName, string firstName, string middleName)
        {
            var newRow = new Grid
            {
                Height = 32,
                Margin = new Thickness(0, 2, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Tag = rowCount
            };

            // Column definitions
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(10) });
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(130) }); // Student Number
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) }); // Last Name
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(160) }); // First Name
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Middle Name
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(56) }); // Present
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(52) }); // Absent
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(50) }); // Late

            // Student Number
            var studentNumText = new TextBlock
            {
                Text = studNum,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            Grid.SetColumn(studentNumText, 1);
            newRow.Children.Add(studentNumText);

            // Last Name
            var lastNameText = new TextBlock
            {
                Text = lastName,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            Grid.SetColumn(lastNameText, 2);
            newRow.Children.Add(lastNameText);

            // First Name
            var firstNameText = new TextBlock
            {
                Text = firstName,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            Grid.SetColumn(firstNameText, 3);
            newRow.Children.Add(firstNameText);

            // Middle Name
            var middleNameText = new TextBlock
            {
                Text = middleName,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            Grid.SetColumn(middleNameText, 4);
            newRow.Children.Add(middleNameText);

            // Present CheckBox
            var present = new CheckBox
            {
                Cursor = Cursors.Hand,
                Name = "PresentCheckBox",
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(72, 72, 72)),
                Template = (ControlTemplate)FindResource("CustomCheckBoxTemplate")
            };
            Grid.SetColumn(present, 5);
            newRow.Children.Add(present);

            // Absent CheckBox
            var absent = new CheckBox
            {
                Cursor = Cursors.Hand,
                Name = "AbsentCheckBox",
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(72, 72, 72)),
                Template = (ControlTemplate)FindResource("CustomCheckBoxTemplate")
            };
            Grid.SetColumn(absent, 6);
            newRow.Children.Add(absent);

            // Late CheckBox
            var late = new CheckBox
            {
                Cursor = Cursors.Hand,
                Name = "LateCheckBox",
                Margin = new Thickness(4, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(43, 43, 43)),
                BorderBrush = new SolidColorBrush(Color.FromRgb(72, 72, 72)),
                Template = (ControlTemplate)FindResource("CustomCheckBoxTemplate")
            };
            Grid.SetColumn(late, 7);
            newRow.Children.Add(late);

            present.Checked += (s, e) =>
            {
                absent.IsChecked = false;
                late.IsChecked = false;
            };

            absent.Checked += (s, e) =>
            {
                present.IsChecked = false;
                late.IsChecked = false;
            };

            late.Checked += (s, e) =>
            {
                present.IsChecked = false;
                absent.IsChecked = false;
            };

            return newRow;
        }

        private async void BTNAttendanceSection(object sender, RoutedEventArgs e)
        {
            string attendanceTableName = GenerateAttendanceTableName();
            if (string.IsNullOrEmpty(attendanceTableName)) return;

            // Show loading indicator
            BTNAttendance.IsEnabled = false;
            BTNAttendance.Content = "Saving Attendance...";

            try
            {
                bool saveSuccess = await Task.Run(() => SaveAttendanceData(attendanceTableName));

                if (saveSuccess)
                {
                    MessageBox.Show("Attendance data saved successfully!", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                // No else needed - WarningWindow is shown inside SaveAttendanceData
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving attendance: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Restore button state
                BTNAttendance.IsEnabled = true;
                BTNAttendance.Content = "Save Attendance";
            }
        }

        private string GenerateAttendanceTableName()
        {
            string subjectname = TBSubject.Text.Replace(" ", "");
            string currentTimeStamp = DateTime.Now.ToString("MM-dd-yyyy").ToUpper();
            return $"{subjectname}_{tablename}_{currentTimeStamp}";
        }

        private bool SaveAttendanceData(string attendanceTableName)
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AttendanceDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            // Collect attendance data from UI
            List<AttendanceRecord> records = GetAttendanceRecordsFromUI();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check if table exists
                        if (TableExists(connection, attendanceTableName))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                new WarningWindow("Duplicate Attendance",
                                    $"Attendance for {TBSubject.Text} - {tablename} already exists!\n" +
                                    "Please edit the existing record instead.").Show();
                            });
                            return false;
                        }

                        // Create table
                        string createTableSql = $@"
                CREATE TABLE `{attendanceTableName}` (
                    StudentNumber TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    FirstName TEXT NOT NULL,
                    MiddleName TEXT,
                    IsPresent BOOLEAN NOT NULL DEFAULT 0,
                    IsAbsent BOOLEAN NOT NULL DEFAULT 0,
                    IsLate BOOLEAN NOT NULL DEFAULT 0,
                    PRIMARY KEY (StudentNumber)
                )";

                        new SQLiteCommand(createTableSql, connection, transaction).ExecuteNonQuery();

                        // Insert records
                        InsertAttendanceRecords(connection, transaction, attendanceTableName, records);

                        transaction.Commit();
                        return true;
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        private List<AttendanceRecord> GetAttendanceRecordsFromUI()
        {
            var records = new List<AttendanceRecord>();
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Grid row in StudentRowsPanel.Children.OfType<Grid>())
                {
                    var textBlocks = row.Children.OfType<TextBlock>().ToList();
                    if (textBlocks.Count < 4 || string.IsNullOrWhiteSpace(textBlocks[0].Text))
                        continue;

                    var presentCheckBox = FindCheckBox(row, "PresentCheckBox");
                    var absentCheckBox = FindCheckBox(row, "AbsentCheckBox");
                    var lateCheckBox = FindCheckBox(row, "LateCheckBox");

                    records.Add(new AttendanceRecord
                    {
                        StudentNumber = textBlocks[0].Text,
                        LastName = textBlocks[1].Text,
                        FirstName = textBlocks[2].Text,
                        MiddleName = textBlocks.Count > 3 ? textBlocks[3].Text : null,
                        IsPresent = presentCheckBox?.IsChecked ?? false,
                        IsAbsent = absentCheckBox?.IsChecked ?? false,
                        IsLate = lateCheckBox?.IsChecked ?? false
                    });
                }
            });
            return records;
        }

        private void InsertAttendanceRecords(SQLiteConnection connection, SQLiteTransaction transaction,
                                           string tableName, List<AttendanceRecord> records)
        {
            string insertSql = $@"
        INSERT INTO `{tableName}` 
        (StudentNumber, LastName, FirstName, MiddleName, IsPresent, IsAbsent, IsLate) 
        VALUES (@sn, @ln, @fn, @mn, @present, @absent, @late)";

            var cmd = new SQLiteCommand(insertSql, connection, transaction);
            var snParam = cmd.Parameters.Add("@sn", DbType.String);
            var lnParam = cmd.Parameters.Add("@ln", DbType.String);
            var fnParam = cmd.Parameters.Add("@fn", DbType.String);
            var mnParam = cmd.Parameters.Add("@mn", DbType.String);
            var presentParam = cmd.Parameters.Add("@present", DbType.Boolean);
            var absentParam = cmd.Parameters.Add("@absent", DbType.Boolean);
            var lateParam = cmd.Parameters.Add("@late", DbType.Boolean);

            foreach (var record in records)
            {
                snParam.Value = record.StudentNumber;
                lnParam.Value = record.LastName;
                fnParam.Value = record.FirstName;
                mnParam.Value = string.IsNullOrEmpty(record.MiddleName) ? DBNull.Value : (object)record.MiddleName;
                presentParam.Value = record.IsPresent;
                absentParam.Value = record.IsAbsent;
                lateParam.Value = record.IsLate;
                cmd.ExecuteNonQuery();
            }
        }

        private CheckBox FindCheckBox(Grid row, string name)
        {
            return row.Children.OfType<CheckBox>().FirstOrDefault(cb => cb.Name == name);
        }

        private class AttendanceRecord
        {
            public string StudentNumber { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
            public bool IsPresent { get; set; }
            public bool IsAbsent { get; set; }
            public bool IsLate { get; set; }
        }

        private bool TableExists(SQLiteConnection connection, string tableName)
        {
            try
            {
                string query = $"SELECT name FROM sqlite_master WHERE type='table' AND name='{tableName}'";
                using (SQLiteCommand cmd = new SQLiteCommand(query, connection))
                {
                    object result = cmd.ExecuteScalar();
                    return result != null && result.ToString() == tableName;
                }
            }
            catch
            {   
                return false;
            }
        }

        /*
        private CheckBox FindCheckBox(Grid row, string name)
        {
            foreach (var child in row.Children)
            {
                if (child is CheckBox checkBox && checkBox.Name == name)
                    return checkBox;
            }
            return null;
        }
        */

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
