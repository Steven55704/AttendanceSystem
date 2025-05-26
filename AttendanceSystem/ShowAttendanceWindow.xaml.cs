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
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using System.Text.RegularExpressions;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Data;
using static System.Collections.Specialized.BitVector32;

namespace SchoolAttendance
{
    /// <summary>
    /// Interaction logic for Section_Window.xaml
    /// </summary>
    public partial class ShowAttendanceWindow : Window
    {

        private int rowCount = 0;
        private const string DeleteConfirmationText = "confirm";
        private static string tablename;
        private static string originalTableName;

        public ShowAttendanceWindow(string subject, DateTime date, string tableName)
        {
            InitializeComponent();

            string subjectname = subject.Replace(" ", "");

            string dateString = date.ToString("MM-dd-yyyy");
            tablename = $"{subjectname}_{tableName}_{dateString}";

            Title = $"Viewing Table: {tableName}";
            originalTableName = tablename;
            LoadStudents(tablename);

            // UI initialization
            CheckBoxConfirmDelete.IsEnabled = false;
            NBTNDeleteSection.Background = new SolidColorBrush(Color.FromArgb(255, 80, 0, 0));
            NBTNDeleteSection.Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private Grid CreateStudentRow(string studNum, string lastName, string firstName, string middleName,
            bool isPresent = false, bool isAbsent = false, bool isLate = false)
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
                IsChecked = isPresent,
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
                IsChecked = isAbsent,
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
                IsChecked = isLate,
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



        private void LoadStudents(string sqlTableName)
        {
            StudentRowsPanel.Children.Clear();
            rowCount = 0;

            // Parse table name into course and date components
            string[] tableParts = sqlTableName.Split('_');
            string subject = tableParts[0];
            string section = tableParts[1];
            string dateString = tableParts[2];

            // Convert subject to display format with spaces
            string displaySubject = Regex.Replace(subject, "([a-z])([A-Z0-9])", "$1 $2");

            // Update UI textboxes
            TBSubject.Text = displaySubject; 
            TBSection.Text = section;
            TBDate.Text = dateString;

            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AttendanceDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    if (!TableExists(connection, sqlTableName))
                    {
                        MessageBox.Show($"Table '{sqlTableName}' does not exist in the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        this.Close();
                        return;
                    }

                    // Modified query to include attendance status
                    string query = $@"
                        SELECT 
                            StudentNumber, 
                            LastName, 
                            FirstName, 
                            MiddleName,
                            IsPresent,
                            IsAbsent,
                            IsLate
                        FROM `{sqlTableName}` 
                        ORDER BY LastName";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string studNum = reader["StudentNumber"].ToString();
                                string lastName = reader["LastName"].ToString();
                                string firstName = reader["FirstName"].ToString();
                                string middleName = reader["MiddleName"].ToString();

                                // Get attendance status
                                bool isPresent = Convert.ToBoolean(reader["IsPresent"]);
                                bool isAbsent = Convert.ToBoolean(reader["IsAbsent"]);
                                bool isLate = Convert.ToBoolean(reader["IsLate"]);
                                
                                // Create row with attendance status
                                Grid studentRow = CreateStudentRow(
                                    studNum, lastName, firstName, middleName,
                                    isPresent, isAbsent, isLate);

                                StudentRowsPanel.Children.Add(studentRow);
                                rowCount++;
                            }
                        }
                    }
                }
            }
            catch (SQLiteException sqlEx)
            {
                MessageBox.Show($"SQL Error loading students: {sqlEx.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading students: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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

        private void TBDeleteConfirmation_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Enable checkbox only if text matches exactly
            bool textMatches = TBDeleteConfirmation.Text.Trim().ToLower() == DeleteConfirmationText;
            CheckBoxConfirmDelete.IsEnabled = textMatches;

            // If text doesn't match, uncheck and disable button
            if (!textMatches)
            {
                CheckBoxConfirmDelete.IsChecked = false;
                NBTNDeleteSection.IsEnabled = false;
            }

            // Visual feedback
            TBDeleteConfirmation.Foreground = textMatches ? Brushes.LimeGreen : Brushes.White;
        }

        private void CBConfirmDelete_Checked(object sender, RoutedEventArgs e)
        {
            // Only enable button when checkbox is checked AND text matches
            NBTNDeleteSection.IsEnabled = CheckBoxConfirmDelete.IsEnabled && CheckBoxConfirmDelete.IsChecked == true;
            NBTNDeleteSection.Background = new SolidColorBrush(Color.FromArgb(255, 160, 0, 0));
            NBTNDeleteSection.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));
        }

        private void CBConfirmDelete_Unchecked(object sender, RoutedEventArgs e)
        {
            // Disable button when checkbox is unchecked
            NBTNDeleteSection.IsEnabled = false;
            NBTNDeleteSection.Background = new SolidColorBrush(Color.FromArgb(255, 80, 0, 0));
            NBTNDeleteSection.Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
        }

        private async void BTNDeleteSection(object sender, RoutedEventArgs e)
        {
            try
            {
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AttendanceDatabase.db");
                string connectionString = $"Data Source={dbPath};Version=3;";

                await Task.Run(() =>
                {
                    using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                    {
                        connection.Open();
                        string dropTableSql = $"DROP TABLE IF EXISTS `{tablename}`";

                        using (SQLiteCommand command = new SQLiteCommand(dropTableSql, connection))
                        {
                            command.ExecuteNonQuery();
                        }
                    }
                });

                // UI updates must be on the main thread
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Table '{tablename}' has been permanently deleted.",
                                  "Success",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Information);
                    this.Close();
                });
            }
            catch (Exception ex)
            {
                this.Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Failed to delete table: {ex.Message}",
                                  "Database Error",
                                  MessageBoxButton.OK,
                                  MessageBoxImage.Error);
                });
            }
        }

        private async void BTNEditAttendanceSection(object sender, RoutedEventArgs e)
        {
            string newTableName = GenerateAttendanceTableName();
            if (string.IsNullOrEmpty(newTableName)) return;

            // Show loading indicator
            BTNEditAttendance.IsEnabled = false;
            BTNEditAttendance.Content = "Saving Changes...";

            try
            {
                bool updateSuccess = await Task.Run(() => ProperUpdateAttendance(originalTableName, newTableName));

                if (updateSuccess)
                {
                    MessageBox.Show("Attendance updated successfully!", "Success",
                                  MessageBoxButton.OK, MessageBoxImage.Information);
                    this.Close();
                }
                // No else needed - error messages are shown inside ProperUpdateAttendance
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating attendance: {ex.Message}", "Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Restore button state
                BTNEditAttendance.IsEnabled = true;
                BTNEditAttendance.Content = "Edit";
            }
        }

        private bool ProperUpdateAttendance(string originalTableName, string newTableName)
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AttendanceDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            // Collect current attendance data from UI
            List<AttendanceRecord> currentRecords = GetAttendanceRecordsFromUI();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check if original table exists
                        if (!TableExists(connection, originalTableName))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                MessageBox.Show("Original attendance record not found!", "Error",
                                              MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                            return false;
                        }

                        // If the table name changed, rename the table first
                        if (originalTableName != newTableName)
                        {
                            // Check if new table name already exists
                            if (TableExists(connection, newTableName))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show("An attendance record with the new name already exists!", "Error",
                                                  MessageBoxButton.OK, MessageBoxImage.Error);
                                });
                                return false;
                            }

                            // Rename the table
                            string renameSql = $"ALTER TABLE `{originalTableName}` RENAME TO `{newTableName}`";
                            new SQLiteCommand(renameSql, connection, transaction).ExecuteNonQuery();
                        }

                        // Update records in the (possibly renamed) table
                        UpdateAttendanceRecords(connection, transaction, newTableName, currentRecords);

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

        private string GenerateAttendanceTableName()
        {
            string section = TBSection.Text;
            string subjectname = TBSubject.Text.Replace(" ", "");
            string currentTimeStamp = TBDate.Text;
            return $"{subjectname}_{section}_{currentTimeStamp}";
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

        private void UpdateAttendanceRecords(SQLiteConnection connection, SQLiteTransaction transaction,
                                           string tableName, List<AttendanceRecord> currentRecords)
        {
            // Get existing records
            var existingRecords = new Dictionary<string, AttendanceRecord>();
            using (var cmd = new SQLiteCommand($"SELECT * FROM `{tableName}`", connection, transaction))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    existingRecords[reader["StudentNumber"].ToString()] = new AttendanceRecord
                    {
                        StudentNumber = reader["StudentNumber"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        FirstName = reader["FirstName"].ToString(),
                        MiddleName = reader["MiddleName"] is DBNull ? null : reader["MiddleName"].ToString(),
                        IsPresent = Convert.ToBoolean(reader["IsPresent"]),
                        IsAbsent = Convert.ToBoolean(reader["IsAbsent"]),
                        IsLate = Convert.ToBoolean(reader["IsLate"])
                    };
                }
            }

            // Prepare commands
            var updateCmd = new SQLiteCommand(
                $@"UPDATE `{tableName}` 
        SET LastName=@ln, FirstName=@fn, MiddleName=@mn, 
            IsPresent=@present, IsAbsent=@absent, IsLate=@late
        WHERE StudentNumber=@sn",
                connection, transaction);

            var insertCmd = new SQLiteCommand(
                $@"INSERT INTO `{tableName}` 
        (StudentNumber, LastName, FirstName, MiddleName, IsPresent, IsAbsent, IsLate) 
        VALUES (@sn, @ln, @fn, @mn, @present, @absent, @late)",
                connection, transaction);

            var deleteCmd = new SQLiteCommand(
                $"DELETE FROM `{tableName}` WHERE StudentNumber=@sn",
                connection, transaction);

            // Process changes
            foreach (var current in currentRecords)
            {
                if (existingRecords.TryGetValue(current.StudentNumber, out var existing))
                {
                    // Update if changed
                    if (existing.LastName != current.LastName ||
                        existing.FirstName != current.FirstName ||
                        existing.MiddleName != current.MiddleName ||
                        existing.IsPresent != current.IsPresent ||
                        existing.IsAbsent != current.IsAbsent ||
                        existing.IsLate != current.IsLate)
                    {
                        updateCmd.Parameters.Clear();
                        updateCmd.Parameters.AddWithValue("@ln", current.LastName);
                        updateCmd.Parameters.AddWithValue("@fn", current.FirstName);
                        updateCmd.Parameters.AddWithValue("@mn", string.IsNullOrEmpty(current.MiddleName) ? DBNull.Value : (object)current.MiddleName);
                        updateCmd.Parameters.AddWithValue("@present", current.IsPresent);
                        updateCmd.Parameters.AddWithValue("@absent", current.IsAbsent);
                        updateCmd.Parameters.AddWithValue("@late", current.IsLate);
                        updateCmd.Parameters.AddWithValue("@sn", current.StudentNumber);
                        updateCmd.ExecuteNonQuery();
                    }
                    existingRecords.Remove(current.StudentNumber);
                }
                else
                {
                    // Insert new record
                    insertCmd.Parameters.Clear();
                    insertCmd.Parameters.AddWithValue("@sn", current.StudentNumber);
                    insertCmd.Parameters.AddWithValue("@ln", current.LastName);
                    insertCmd.Parameters.AddWithValue("@fn", current.FirstName);
                    insertCmd.Parameters.AddWithValue("@mn", string.IsNullOrEmpty(current.MiddleName) ? DBNull.Value : (object)current.MiddleName);
                    insertCmd.Parameters.AddWithValue("@present", current.IsPresent);
                    insertCmd.Parameters.AddWithValue("@absent", current.IsAbsent);
                    insertCmd.Parameters.AddWithValue("@late", current.IsLate);
                    insertCmd.ExecuteNonQuery();
                }
            }

            // Delete removed records
            foreach (var removed in existingRecords.Values)
            {
                deleteCmd.Parameters.Clear();
                deleteCmd.Parameters.AddWithValue("@sn", removed.StudentNumber);
                deleteCmd.ExecuteNonQuery();
            }
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

        private void ExportToExcel(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("This feature need license for 40$ but good to have a placeholder button!\n" + "Added this button incase teacher will ask if we have an idea of exporting attendance to excel :D", "Placeholder", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private CheckBox FindCheckBox(Grid row, string name)
        {
            foreach (var child in row.Children)
            {
                if (child is CheckBox checkBox && checkBox.Name == name)
                    return checkBox;
            }
            return null;
        }
    }
}
