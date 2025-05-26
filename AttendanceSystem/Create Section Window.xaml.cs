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
using SchoolAttendance.UI.Notifier;
using System.IO;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using System.Data;

namespace SchoolAttendance
{
    /// <summary>
    /// Interaction logic for Create_Section_Window.xaml
    /// </summary>
    public partial class Create_Section_Window : Window
    {

        private int rowCount = 0;
        private Grid selectedRow = null;

        public Create_Section_Window()
        {
            InitializeComponent();
            RBHS.IsChecked = true;
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
            TBCourse.Visibility = Visibility.Hidden;
            TBYear.Visibility = Visibility.Hidden;
            TBSection.Visibility = Visibility.Hidden;
            TxtBlkYear.Visibility = Visibility.Hidden;
            TxtBlkGrade.Visibility = Visibility.Visible;
            TBHSCourse.Visibility = Visibility.Visible;
            TBHSSection.Visibility = Visibility.Visible;
            CBxHSYear.Visibility = Visibility.Visible;
        }

        private void RBT_Checked(object sender, RoutedEventArgs e)
        {
            RBHS.IsChecked = false;
            TBHSCourse.Visibility = Visibility.Hidden;
            TBHSSection.Visibility = Visibility.Hidden;
            CBxHSYear.Visibility = Visibility.Hidden;
            TxtBlkGrade.Visibility = Visibility.Hidden;
            TxtBlkYear.Visibility = Visibility.Visible; 
            TBCourse.Visibility = Visibility.Visible;
            TBYear.Visibility = Visibility.Visible;
            TBSection.Visibility = Visibility.Visible;
        }

        private void BTNImportStudent(object sender, RoutedEventArgs e)
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "YAML Files (*.yaml;*.yml)|*.yaml;*.yml|All files (*.*)|*.*",
                    Title = "Select Student YAML File"
                };

                if (openFileDialog.ShowDialog() != true)
                {
                    return;
                }

                string yamlFilePath = openFileDialog.FileName;
                string yamlContent = File.ReadAllText(yamlFilePath);

                var deserializer = new DeserializerBuilder()
                    .IgnoreUnmatchedProperties()  // Ignores extra YAML fields
                    .Build();

                var yamlData = deserializer.Deserialize<StudentYaml>(yamlContent);

                // Get existing student IDs for duplicate checking
                var existingStudentIds = new HashSet<string>();
                foreach (Grid row in StudentRowsPanel.Children.OfType<Grid>())
                {
                    var textBlocks = row.Children.OfType<TextBlock>().ToList();
                    existingStudentIds.Add(textBlocks[0].Text); // Student ID is first TextBlock
                }

                int newStudentsAdded = 0;
                int duplicatesSkipped = 0;

                // Get existing students
                List<StudentEntry> studentEntries = new List<StudentEntry>();
                foreach (var student in yamlData.students)
                {
                    // Skip if student already exists
                    if (existingStudentIds.Contains(student.StudentId))
                    {
                        duplicatesSkipped++;
                        continue;
                    }

                    // Add new student
                    var newRow = CreateStudentRow(
                        student.StudentId,
                        student.surname,
                        student.name,
                        student.middlename
                    );

                    StudentRowsPanel.Children.Add(newRow);
                    existingStudentIds.Add(student.StudentId); // Add to check for future duplicates
                    newStudentsAdded++;
                }

                // Sort and rebuild UI
                SortStudentRows();

                string message = $"{newStudentsAdded} new student(s) added.";
                if (duplicatesSkipped > 0)
                {
                    message += $"\n{duplicatesSkipped} duplicate(s) skipped.";
                }

                MessageBox.Show(message, "Import Complete", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error importing students: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void SortStudentRows()
        {
            // Get all rows with their student data
            var rowsWithData = StudentRowsPanel.Children.OfType<Grid>()
                .Select(row => new
                {
                    Row = row,
                    LastName = row.Children.OfType<TextBlock>().ElementAt(1).Text
                })
                .OrderBy(x => x.LastName)
                .ToList();

            // Clear and re-add in sorted order
            StudentRowsPanel.Children.Clear();
            foreach (var item in rowsWithData)
            {
                StudentRowsPanel.Children.Add(item.Row);
            }
        }

        // YAML data structure class
        public class Student
        {
            [YamlMember(Alias = "student id")]
            public string StudentId { get; set; }

            public string name { get; set; }
            public string middlename { get; set; }
            public string surname { get; set; }
        }

        public class StudentYaml
        {
            public List<Student> students { get; set; }
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

        private class StudentEntry
        {
            public Grid Grid { get; set; }
            public string StudNum { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
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

        private async void BTNCreateSection(object sender, RoutedEventArgs e)
        {
            string tableName = GenerateTableName();
            if (string.IsNullOrEmpty(tableName)) return;

            // Show loading indicator
            BTNCreate.IsEnabled = false;
            BTNCreate.Content = "Creating...";

            try
            {
                bool creationSuccess = await Task.Run(() => CreateSectionInDatabase(tableName));

                if (creationSuccess)
                {
                    MessageBox.Show($"Section {tableName} created successfully with {StudentRowsPanel.Children.Count} students!",
                                  "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    new Section_Page().ReloadSections();
                    this.Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving to database: {ex.Message}");
            }
            finally
            {
                // Restore button state
                BTNCreate.IsEnabled = true;
                BTNCreate.Content = "Create";
            }
        }

        private string GenerateTableName()
        {
            if (RBHS.IsChecked == true) // High School
            {
                string course = TBHSCourse.Text.Trim().ToUpper();
                string grade = (CBxHSYear.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? "";
                string semester = (CBxSemester.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? "";
                string section = TBHSSection.Text.Trim().ToUpper();

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
                string semester = (CBxSemester.SelectedItem as ComboBoxItem)?.Content?.ToString()?.Trim() ?? "";
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

        private bool CreateSectionInDatabase(string tableName)
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SectionDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            // Collect student data on the UI thread first
            List<StudentData> students = GetStudentsFromUI();

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    try
                    {
                        // Check if table already exists
                        if (TableExists(connection, tableName))
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                new WarningWindow("Duplicate Section",
                                    $"Section {tableName} already exists!\n" +
                                    "Please edit the existing record instead.").Show();
                            });
                            return false; // Return false to indicate duplicate
                        }

                        // Create table if it doesn't exist
                        string createTableSql = $@"
                CREATE TABLE `{tableName}` (
                    StudentNumber TEXT NOT NULL,
                    LastName TEXT NOT NULL,
                    FirstName TEXT NOT NULL,
                    MiddleName TEXT,
                    PRIMARY KEY (StudentNumber)
                )";

                        new SQLiteCommand(createTableSql, connection, transaction).ExecuteNonQuery();

                        // Insert all students
                        InsertStudents(connection, transaction, tableName, students);

                        transaction.Commit();
                        return true; // Return true for success
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

        private void InsertStudents(SQLiteConnection connection, SQLiteTransaction transaction,
                                  string tableName, List<StudentData> students)
        {
            string insertSql = $@"
        INSERT INTO `{tableName}` 
        (StudentNumber, LastName, FirstName, MiddleName) 
        VALUES (@sn, @ln, @fn, @mn)";

            var cmd = new SQLiteCommand(insertSql, connection, transaction);
            var snParam = cmd.Parameters.Add("@sn", DbType.String);
            var lnParam = cmd.Parameters.Add("@ln", DbType.String);
            var fnParam = cmd.Parameters.Add("@fn", DbType.String);
            var mnParam = cmd.Parameters.Add("@mn", DbType.String);

            foreach (var student in students)
            {
                snParam.Value = student.StudentNumber;
                lnParam.Value = student.LastName;
                fnParam.Value = student.FirstName;
                mnParam.Value = string.IsNullOrEmpty(student.MiddleName) ? DBNull.Value : (object)student.MiddleName;
                cmd.ExecuteNonQuery();
            }
        }

        // Helper class to store student data
        private class StudentData
        {
            public string StudentNumber { get; set; }
            public string LastName { get; set; }
            public string FirstName { get; set; }
            public string MiddleName { get; set; }
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
