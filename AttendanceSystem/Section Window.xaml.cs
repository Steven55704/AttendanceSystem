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

namespace SchoolAttendance
{
    /// <summary>
    /// Interaction logic for Section_Window.xaml
    /// </summary>
    public partial class Section_Window : Window
    {

        private int rowCount = 0;
        private Grid selectedRow = null;
        private const string DeleteConfirmationText = "confirm";
        private static string tablename;
        private static string originalTableName;

        public Section_Window(string tableName)
        {
            InitializeComponent();
            Title = $"Viewing Table: {tableName}";
            LoadStudents(tableName);
            ParseTableName(tableName);
            tablename = tableName;
            originalTableName = tablename;
            CheckBoxConfirmDelete.IsEnabled = false;
            NBTNDeleteSection.Background = new SolidColorBrush(Color.FromArgb(255, 80, 0, 0));
            NBTNDeleteSection.Foreground = new SolidColorBrush(Color.FromArgb(255, 180, 180, 180));
            TableHeader.PreviewMouseDown += NewPreviewMouseDown;
            StudentRowsPanelContainer.PreviewMouseDown += ContainerPreviewMouseDown;
        }

        private void DragWindow(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
                this.DragMove();
        }

        private void NewPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (selectedRow != null)
            {
                DeselectCurrentRow();
                e.Handled = true;
            }
        }

        private void ContainerPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (selectedRow != null && e.OriginalSource is ScrollViewer)
            {
                DeselectCurrentRow();
                e.Handled = true;
            }
        }

        private void DeselectCurrentRow()
        {
            selectedRow.Background = new SolidColorBrush(Color.FromRgb(30, 30, 30));
            selectedRow = null;
        }

        private void Row_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {

            if (selectedRow != null)
            {
                selectedRow.Background =  new SolidColorBrush(Color.FromRgb(30, 30, 30));
            }

            selectedRow = sender as Grid;
            if (selectedRow != null)
            {
                selectedRow.Background = new SolidColorBrush(Color.FromRgb(0, 128, 0));
            }

            e.Handled = true;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void RBHS_Checked(object sender, RoutedEventArgs e)
        {
            RBT.IsChecked = false;
            TxtBlkYear.Visibility = Visibility.Hidden;
            TxtBlkGrade.Visibility = Visibility.Visible;
        }

        private void RBT_Checked(object sender, RoutedEventArgs e)
        {
            RBHS.IsChecked = false;
            TxtBlkYear.Visibility = Visibility.Visible;
            TxtBlkGrade.Visibility = Visibility.Hidden;
        }

        private void BTNAddStudent(object sender, RoutedEventArgs e)
        {
            // Regex: only allows letters (upper/lowercase)
            Regex letterOnly = new Regex(@"^[a-zA-ZñÑ ]+$");
            Regex numOnly = new Regex(@"^[0-9]+$");

            // Validate Student Number (required, numbers only)
            if (string.IsNullOrWhiteSpace(TBSN.Text) || !numOnly.IsMatch(TBSN.Text))
            {
                MessageBox.Show("Please enter a valid student number (numbers only, cannot be empty).",
                               "Invalid Student Number",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            foreach (Grid row in StudentRowsPanel.Children.OfType<Grid>())
            {
                var textBlocks = row.Children.OfType<TextBlock>().ToList();
                if (textBlocks.Count > 0 && textBlocks[0].Text == TBSN.Text)
                {
                    MessageBox.Show("A student with this student number already exists.",
                                   "Duplicate Student Number",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                    return;
                }
            }

            // Validate Last Name (required)
            if (string.IsNullOrWhiteSpace(TBLN.Text) || !letterOnly.IsMatch(TBLN.Text))
            {
                MessageBox.Show("Please enter a valid last name (letters A-Z only, cannot be empty).",
                               "Invalid Last Name",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            // Validate First Name (required)
            if (string.IsNullOrWhiteSpace(TBFN.Text) || !letterOnly.IsMatch(TBFN.Text))
            {
                MessageBox.Show("Please enter a valid first name (letters A-Z only, cannot be empty).",
                               "Invalid First Name",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            // Validate Middle Name (optional but must be valid if provided)
            if (!string.IsNullOrWhiteSpace(TBMN.Text) && !letterOnly.IsMatch(TBMN.Text))
            {
                MessageBox.Show("Middle name must contain only letters (A-Z) if provided.",
                               "Invalid Middle Name",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            // Create a list to hold all current students with the new one
            List<StudentEntry> studentEntries = new List<StudentEntry>();

            // Add existing students to the list
            foreach (Grid row in StudentRowsPanel.Children.OfType<Grid>())
            {
                var textBlocks = row.Children.OfType<TextBlock>().ToList();
                studentEntries.Add(new StudentEntry
                {
                    Grid = row,
                    StudNum = textBlocks[0].Text,  // First TextBlock is student number
                    LastName = textBlocks[1].Text,  // Second is last name
                    FirstName = textBlocks[2].Text, // Third is first name
                    MiddleName = textBlocks[3].Text // Fourth is middle name
                });
            }

            // Add the new student
            studentEntries.Add(new StudentEntry
            {
                StudNum = TBSN.Text,  // Use TBSN.Text for student number
                LastName = TBLN.Text,
                FirstName = TBFN.Text,
                MiddleName = TBMN.Text
            });

            // Sort the list by last name only
            var sortedStudents = studentEntries
                .OrderBy(s => s.LastName)
                .ToList();

            // Clear the panel and rebuild it in sorted order
            StudentRowsPanel.Children.Clear();

            foreach (var student in sortedStudents)
            {
                // If this is the new student, create a new row
                if (student.Grid == null)
                {
                    student.Grid = CreateStudentRow(student.StudNum, student.LastName, student.FirstName, student.MiddleName);
                }

                StudentRowsPanel.Children.Add(student.Grid);
            }

            rowCount++;
        }

        private Grid CreateStudentRow(string studNum, string lastName, string firstName, string middleName)
        {
            var newRow = new Grid
            {
                Height = 32,
                Margin = new Thickness(0, 2, 0, 0),
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Cursor = Cursors.Hand,
                Tag = rowCount
            };

            // Column definitions - ensure these match your actual UI layout
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(22) });
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(170) }); // Student Number
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Last Name
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // First Name
            newRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(150) }); // Middle Name

            // Student Number TextBlock (Column 0)
            var studentNumText = new TextBlock
            {
                Text = studNum,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            Grid.SetColumn(studentNumText, 1);
            newRow.Children.Add(studentNumText);

            // Last Name TextBlock (Column 1)
            var lastNameText = new TextBlock
            {
                Text = lastName,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            Grid.SetColumn(lastNameText, 2);
            newRow.Children.Add(lastNameText);

            // First Name TextBlock (Column 2)
            var firstNameText = new TextBlock
            {
                Text = firstName,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            Grid.SetColumn(firstNameText, 3);
            newRow.Children.Add(firstNameText);

            // Middle Name TextBlock (Column 3)
            var middleNameText = new TextBlock
            {
                Text = middleName,
                Margin = new Thickness(10, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = Brushes.White
            };
            Grid.SetColumn(middleNameText, 4);
            newRow.Children.Add(middleNameText);

            newRow.MouseLeftButtonDown += Row_MouseLeftButtonDown;

            return newRow;
        }

        private void ParseTableName(string tableName)
        {
            try
            {
                // Auto-detect school type based on prefix
                if (tableName.StartsWith("BS"))
                {
                    RBT.IsChecked = true;
                    ParseCollegeTableName(tableName);
                }
                else
                {
                    RBHS.IsChecked = true;
                    ParseHighSchoolTableName(tableName);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error parsing table name: {ex.Message}", "Parse Error",
                               MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ParseHighSchoolTableName(string tableName)
        {
            // Format: WEB121-1A (Course+Grade+Semester+Section)
            Regex hsRegex = new Regex(@"^([A-Z]+)(\d{2})(\d)-(\d*[A-Z]|[A-Z]\d*)$");
            Match match = hsRegex.Match(tableName);

            if (!match.Success)
                throw new FormatException("Invalid High School table name format");

            TBCourse.Text = match.Groups[1].Value;  // Course (WEB)
            TBYear.Text = match.Groups[2].Value;    // Grade (12)
            TBSemester.Text = match.Groups[3].Value; // Semester (1)
            TBSection.Text = match.Groups[4].Value;  // Section (A)
        }

        private void ParseCollegeTableName(string tableName)
        {
            // Format: BSIT401A (Course+YearCode+Section)
            Regex collegeRegex = new Regex(@"^([A-Z]+)(\d{3})([A-Z])$");
            Match match = collegeRegex.Match(tableName);

            if (!match.Success)
                throw new FormatException("Invalid College table name format");

            string course = match.Groups[1].Value;  // Course (BSIT)
            int yearCode = int.Parse(match.Groups[2].Value); // Year code (401)
            string section = match.Groups[3].Value;  // Section (A)

            // Convert year code back to year/semester
            int year = (int)Math.Round((double)yearCode / 200);
            int semesterCode = yearCode % 200;
            string semester = semesterCode == 101 ? "1" : "2";

            TBCourse.Text = course;
            TBYear.Text = year.ToString();
            TBSemester.Text = semester;
            TBSection.Text = section;
        }

        private void LoadStudents(string sqlTableName)
        {
            // Clear existing rows and reset counter
            StudentRowsPanel.Children.Clear();
            rowCount = 0;

            // SQLite connection
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SectionDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    // First verify the table exists
                    if (!TableExists(connection, sqlTableName))
                    {
                        MessageBox.Show($"Table '{sqlTableName}' does not exist in the database.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    string query = $"SELECT StudentNumber, LastName, FirstName, MiddleName FROM `{sqlTableName}` ORDER BY LastName";

                    using (SQLiteCommand command = new SQLiteCommand(query, connection))
                    {
                        using (SQLiteDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                // Get student data from SQLite
                                string studNum = reader["StudentNumber"].ToString();
                                string lastName = reader["LastName"].ToString();
                                string firstName = reader["FirstName"].ToString();
                                string middleName = reader["MiddleName"].ToString();

                                // Create and add the row
                                Grid studentRow = CreateStudentRow(studNum, lastName, firstName, middleName);
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

        private class StudentEntry
        {
            public Grid Grid { get; set; }
            public string StudNum { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
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
                string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SectionDatabase.db");
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

        private void BTNRemoveStudent(object sender, RoutedEventArgs e)
        {
            if (selectedRow != null)
            {
                StudentRowsPanel.Children.Remove(selectedRow);
                selectedRow = null;
            }
        }

        private void BTNUpdateStudent(object sender, RoutedEventArgs e)
        {
            // Regex: only allows letters (upper/lowercase)
            Regex letterOnly = new Regex(@"^[a-zA-ZñÑ ]+$");
            Regex numOnly = new Regex(@"^[0-9]+$");

            // Validate Student Number (required, numbers only)
            if (string.IsNullOrWhiteSpace(TBSN.Text) || !numOnly.IsMatch(TBSN.Text))
            {
                MessageBox.Show("Please enter a valid student number (numbers only, cannot be empty).",
                               "Invalid Student Number",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            foreach (Grid row in StudentRowsPanel.Children.OfType<Grid>())
            {
                var textBlocks = row.Children.OfType<TextBlock>().ToList();
                if (textBlocks.Count > 0 && textBlocks[0].Text == TBSN.Text)
                {
                    MessageBox.Show("A student with this student number already exists.",
                                   "Duplicate Student Number",
                                   MessageBoxButton.OK,
                                   MessageBoxImage.Warning);
                    return;
                }
            }

            // Validate Last Name (required)
            if (string.IsNullOrWhiteSpace(TBLN.Text) || !letterOnly.IsMatch(TBLN.Text))
            {
                MessageBox.Show("Please enter a valid last name (letters A-Z only, cannot be empty).",
                               "Invalid Last Name",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            // Validate First Name (required)
            if (string.IsNullOrWhiteSpace(TBFN.Text) || !letterOnly.IsMatch(TBFN.Text))
            {
                MessageBox.Show("Please enter a valid first name (letters A-Z only, cannot be empty).",
                               "Invalid First Name",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            // Validate Middle Name (optional but must be valid if provided)
            if (!string.IsNullOrWhiteSpace(TBMN.Text) && !letterOnly.IsMatch(TBMN.Text))
            {
                MessageBox.Show("Middle name must contain only letters (A-Z) if provided.",
                               "Invalid Middle Name",
                               MessageBoxButton.OK,
                               MessageBoxImage.Warning);
                return;
            }

            if (selectedRow == null)
            {
                MessageBox.Show("Please select a row to update.");
                return;
            }

            // Get the children of the selected row
            var children = selectedRow.Children.OfType<TextBlock>().ToList();

            if (children.Count >= 3)
            {
                children[0].Text = TBSN.Text;
                children[1].Text = TBLN.Text;
                children[2].Text = TBFN.Text;
                children[3].Text = TBMN.Text;
            }
        }

        private async void BTNEditSection(object sender, RoutedEventArgs e)
        {
            string newTableName = GenerateTableName();
            if (string.IsNullOrEmpty(newTableName)) return;

            // Show loading indicator
            BTNEdit.IsEnabled = false;
            BTNEdit.Content = "Saving Changes...";

            try
            {
                bool updateSuccess = await Task.Run(() => ProperUpdateSection(originalTableName, newTableName));

                if (updateSuccess)
                {
                    MessageBox.Show($"Section updated successfully!{(originalTableName != newTableName ? $" (Renamed to {newTableName})" : "")}",
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    new Section_Page().ReloadSections();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error updating section: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Restore button state
                BTNEdit.IsEnabled = true;
                BTNEdit.Content = "Edit";
            }
        }

        private bool ProperUpdateSection(string originalTableName, string newTableName)
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SectionDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            // Collect current UI data
            var currentStudents = GetStudentsFromUI();

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
                                MessageBox.Show("Original section not found!", "Error",
                                              MessageBoxButton.OK, MessageBoxImage.Error);
                            });
                            return false;
                        }

                        // If table name changed, rename the table first
                        if (originalTableName != newTableName)
                        {
                            // Check if new table name already exists
                            if (TableExists(connection, newTableName))
                            {
                                Application.Current.Dispatcher.Invoke(() =>
                                {
                                    MessageBox.Show("A section with the new name already exists!", "Error",
                                                  MessageBoxButton.OK, MessageBoxImage.Error);
                                });
                                return false;
                            }

                            // Rename the table
                            string renameSql = $"ALTER TABLE `{originalTableName}` RENAME TO `{newTableName}`";
                            new SQLiteCommand(renameSql, connection, transaction).ExecuteNonQuery();
                        }

                        // Update student records in the (possibly renamed) table
                        UpdateStudentRecords(connection, transaction, newTableName, currentStudents);

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

        private List<StudentData> GetStudentsFromUI()
        {
            var students = new List<StudentData>();
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (Grid row in StudentRowsPanel.Children.OfType<Grid>())
                {
                    var textBlocks = row.Children.OfType<TextBlock>().ToList();
                    if (textBlocks.Count < 4 || string.IsNullOrWhiteSpace(textBlocks[0].Text))
                        continue;

                    students.Add(new StudentData
                    {
                        StudentNumber = textBlocks[0].Text,
                        LastName = textBlocks[1].Text,
                        FirstName = textBlocks[2].Text,
                        MiddleName = textBlocks.Count > 3 ? textBlocks[3].Text : null
                    });
                }
            });
            return students;
        }

        private void UpdateStudentRecords(SQLiteConnection connection, SQLiteTransaction transaction,
                                        string tableName, List<StudentData> currentStudents)
        {
            // Get existing students from database
            var dbStudents = new Dictionary<string, StudentData>();
            using (var cmd = new SQLiteCommand($"SELECT * FROM `{tableName}`", connection, transaction))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    dbStudents[reader["StudentNumber"].ToString()] = new StudentData
                    {
                        StudentNumber = reader["StudentNumber"].ToString(),
                        LastName = reader["LastName"].ToString(),
                        FirstName = reader["FirstName"].ToString(),
                        MiddleName = reader["MiddleName"] is DBNull ? null : reader["MiddleName"].ToString()
                    };
                }
            }

            // Prepare commands
            var updateCmd = new SQLiteCommand(
                $"UPDATE `{tableName}` SET LastName=@ln, FirstName=@fn, MiddleName=@mn WHERE StudentNumber=@sn",
                connection, transaction);

            var insertCmd = new SQLiteCommand(
                $"INSERT INTO `{tableName}` (StudentNumber, LastName, FirstName, MiddleName) VALUES (@sn, @ln, @fn, @mn)",
                connection, transaction);

            var deleteCmd = new SQLiteCommand(
                $"DELETE FROM `{tableName}` WHERE StudentNumber=@sn",
                connection, transaction);

            // Process changes
            foreach (var current in currentStudents)
            {
                if (dbStudents.TryGetValue(current.StudentNumber, out var existing))
                {
                    // Update if changed
                    if (existing.LastName != current.LastName ||
                        existing.FirstName != current.FirstName ||
                        existing.MiddleName != current.MiddleName)
                    {
                        updateCmd.Parameters.Clear();
                        updateCmd.Parameters.AddWithValue("@ln", current.LastName);
                        updateCmd.Parameters.AddWithValue("@fn", current.FirstName);
                        updateCmd.Parameters.AddWithValue("@mn", string.IsNullOrEmpty(current.MiddleName) ? DBNull.Value : (object)current.MiddleName);
                        updateCmd.Parameters.AddWithValue("@sn", current.StudentNumber);
                        updateCmd.ExecuteNonQuery();
                    }
                    dbStudents.Remove(current.StudentNumber);
                }
                else
                {
                    // Insert new student
                    insertCmd.Parameters.Clear();
                    insertCmd.Parameters.AddWithValue("@sn", current.StudentNumber);
                    insertCmd.Parameters.AddWithValue("@ln", current.LastName);
                    insertCmd.Parameters.AddWithValue("@fn", current.FirstName);
                    insertCmd.Parameters.AddWithValue("@mn", string.IsNullOrEmpty(current.MiddleName) ? DBNull.Value : (object)current.MiddleName);
                    insertCmd.ExecuteNonQuery();
                }
            }

            // Delete removed students
            foreach (var removed in dbStudents.Values)
            {
                deleteCmd.Parameters.Clear();
                deleteCmd.Parameters.AddWithValue("@sn", removed.StudentNumber);
                deleteCmd.ExecuteNonQuery();
            }
        }

        private string GenerateTableName()
        {
            if (RBHS.IsChecked == true) // High School
            {
                string course = TBCourse.Text.Trim().ToUpper();
                string grade = TBYear.Text.Trim();
                string semester = TBSemester.Text.Trim();
                string section = TBSection.Text.Trim().ToUpper();

                if (string.IsNullOrEmpty(course) || string.IsNullOrEmpty(grade) ||
                    string.IsNullOrEmpty(semester) || string.IsNullOrEmpty(section))
                {
                    MessageBox.Show("Please fill all High School fields");
                    return null;
                }

                if (course.StartsWith("BS"))
                {
                    MessageBox.Show("High School courses cannot start with 'BS'");
                    return null;
                }

                return $"{course}{grade}{semester}-{section}";
            }
            else if (RBT.IsChecked == true) // College
            {
                string course = TBCourse.Text.Trim().ToUpper();
                string year = TBYear.Text.Trim();
                string semester = TBSemester.Text.Trim();
                string section = TBSection.Text.Trim().ToUpper();

                if (string.IsNullOrEmpty(course) || string.IsNullOrEmpty(year) ||
                    string.IsNullOrEmpty(semester) || string.IsNullOrEmpty(section))
                {
                    MessageBox.Show("Please fill all College fields");
                    return null;
                }

                if (!int.TryParse(year, out int yearNum))
                {
                    MessageBox.Show("Invalid year number");
                    return null;
                }

                if (!course.StartsWith("BS"))
                {
                    MessageBox.Show("College courses must start with 'BS'");
                    return null;
                }

                int semesterNum = semester == "1" ? 1 : 2;
                int yearCode = (yearNum - 1) * 200 + (semesterNum == 1 ? 101 : 201);

                return $"{course}{yearCode}{section}";
            }
            else
            {
                MessageBox.Show("Please select either High School or College");
                return null;
            }
        }

        private class StudentData
        {
            public string StudentNumber { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
        }

        private void TextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                var currentTextBox = sender as TextBox;
                
                if (currentTextBox != TBMN && string.IsNullOrWhiteSpace(currentTextBox.Text))
                {
                    MessageBox.Show("This field cannot be empty!");
                    return;
                }
                
                var request = new TraversalRequest(FocusNavigationDirection.Next);
                currentTextBox.MoveFocus(request);
            }
        }
    }
}
