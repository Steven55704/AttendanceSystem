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
using System.Windows.Navigation;
using System.Windows.Shapes;

using System.Data;
using System.Data.SQLite;
using System.Text.RegularExpressions;
using static System.Collections.Specialized.BitVector32;

namespace SchoolAttendance
{
    /// <summary>
    /// Interaction logic for Section_Page.xaml
    /// </summary>
    public partial class Section_Page : Page
    {
        

        public Section_Page()
        {
            InitializeComponent();
            this.IsVisibleChanged += SectionPage_IsVisibleChanged;

        }

        /*
        private string Ordinal(string number)
        {
            return number switch
            {
                "1" => "1st",
                "2" => "2nd",
                "3" => "3rd",
                _ => number + "th"
            };
        }
        */

        public void ReloadSections()
        {
            SectionWrapPanel.Children.Clear();
            LoadSections();
        }

        private void SectionPage_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (this.IsVisible)
            {
                SectionWrapPanel.Children.Clear(); // optional: clear old cards
                LoadSections();
            }
        }

        private void LoadSections()
        {
            string dbPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "SectionDatabase.db");
            string connectionString = $"Data Source={dbPath};Version=3;";

            try
            {
                using (SQLiteConnection connection = new SQLiteConnection(connectionString))
                {
                    connection.Open();

                    var sections = new List<SectionInfo>();

                    using (SQLiteCommand command = new SQLiteCommand("SELECT name FROM sqlite_master WHERE type='table';", connection))
                    using (SQLiteDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string tableName = reader.GetString(0);
                            if (tableName.StartsWith("sqlite_")) continue;

                            sections.Add(new SectionInfo(tableName));
                        }
                    }

                    // Sort sections with custom ordering
                    var sortedSections = sections
                        .OrderBy(s => s.IsCollege) // High School first (false), College second (true)
                        .ThenBy(s => s.SortKey1)   // Primary sort key
                        .ThenBy(s => s.SortKey2)   // Secondary sort key
                        .ThenBy(s => s.SortKey3)   // Tertiary sort key
                        .ThenBy(s => s.SortKey4);   // Quaternary sort key

                    SectionWrapPanel.Children.Clear();
                    foreach (var section in sortedSections)
                    {
                        Button card = CreateSectionCard(section.TableName);
                        SectionWrapPanel.Children.Add(card);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database error: {ex.Message}", "Error - LoadSections");
            }
        }

        private string GetCourseFromTableName(string tableName)
        {
            // Extract letters from beginning (e.g., "BSIT" from "BSIT401E", "WEB" from "WEB111-1A")
            // Check for High School format (WEB111-1B)
            string result;

            if (tableName.Contains("-"))
            {
                // High School format (WEB121-1A)
                string prefix = tableName.Split('-')[0];
                result = new string(prefix.TakeWhile(char.IsLetter).ToArray());
            }
            else
            {
                // College format (BSIT401E)
                result = new string(tableName.TakeWhile(char.IsLetter).ToArray());
            }

            return result;
        }

        private string GetYearAndSemester(string tableName)
        {
            // Handle High School format (WEB121-1A)
            if (tableName.Contains("-"))
            {
                try
                {
                    string[] parts = tableName.Split('-');
                    string prefix = parts[0];  // WEB121

                    // Extract grade (first 2 digits after letters - "12" from "WEB121")
                    string gradeDigits = new string(prefix.SkipWhile(char.IsLetter)
                                                       .Take(2)
                                                       .ToArray());

                    // Extract semester (last digit before hyphen - "1" from "WEB121")
                    string semester = new string(prefix.SkipWhile(char.IsLetter)
                                                     .Skip(2)  // Skip the grade digits
                                                     .Take(1)  // Take the semester digit
                                                     .ToArray());

                    return $"G{gradeDigits}S{semester}";

                }
                catch
                {
                    return "Unknown";
                }
            }

            // Rest of college format handling remains the same...
            string digits = new string(tableName.Where(char.IsDigit).ToArray());

            if (int.TryParse(digits, out int code))
            {
                int yearLevel = (code - 101) / 200 + 1;
                int semester = ((code / 100) % 2 == 0) ? 2 : 1;

                return $"{yearLevel}Y{semester}S";
            }

            return "Unknown";
        }

        private Button CreateSectionCard(string tableName)
        {
            string course = GetCourseFromTableName(tableName);      // BSIT
            string year = GetYearAndSemester(tableName);          // 4th Year
            string section = tableName;                           // BSIT401E

            // Create content elements
            var title = new TextBlock
            {
                Text = $"{course} - {year}",
                FontWeight = FontWeights.Bold,
                FontSize = 14,
                Foreground = Brushes.White,
                Margin = new Thickness(0, 0, 0, 5)
            };

            var subtitle = new TextBlock
            {
                Text = section,
                FontSize = 12,
                Foreground = Brushes.Gray
            };

            var panel = new StackPanel();
            panel.Children.Add(title);
            panel.Children.Add(subtitle);

            // Create the visual border (for styling)
            var buttonContent = new Border
            {
                Width = 150,
                Height = 100,
                Background = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                CornerRadius = new CornerRadius(8),
                Child = panel,
                Padding = new Thickness(10)
            };

            // Create the button
            var button = new Button
            {
                Width = 140,
                Height = 100,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Content = buttonContent,
                Cursor = Cursors.Hand,
                Margin = new Thickness(9.6),
                FocusVisualStyle = null
            };

            // Click event handler
            button.Click += (sender, e) =>
            {
                OpenSection(section);
            };

            // Visual feedback for mouse interaction
            button.MouseEnter += (sender, e) =>
            {
                buttonContent.Background = new SolidColorBrush(Color.FromRgb(20, 20, 20));
            };

            button.MouseLeave += (sender, e) =>
            {
                buttonContent.Background = new SolidColorBrush(Color.FromRgb(10, 10, 10));
            };

            return button;
        }

        private void OpenSection(string section)
        {
            WindowManager.ShowWindow<Section_Window>(
                () => new Section_Window(section),
                () =>
                {
                    NavigationService.Refresh();
                    ReloadSections();
                }
            );
        }

        private void CreateSection(object sender, RoutedEventArgs e)
        {
            WindowManager.ShowWindow<Create_Section_Window>(
                () => new Create_Section_Window(),
                () =>
                {
                    NavigationService.Refresh();
                    ReloadSections();
                }
            );
        }

        private class SectionInfo
        {
            public string TableName { get; }
            public bool IsCollege { get; }
            public string SortKey1 { get; } // Primary sort key
            public string SortKey2 { get; } // Secondary sort key
            public string SortKey3 { get; } // Tertiary sort key
            public string SortKey4 { get; } // Quaternary sort key

            public SectionInfo(string tableName)
            {
                TableName = tableName;
                IsCollege = tableName.StartsWith("BS");

                if (IsCollege)
                {
                    // College format: BSIT401E
                    // Sort by: 3rd letter (I in BSIT), then 1st number (4 in 401), then last letter (E)
                    SortKey1 = tableName.Length > 2 ? tableName[2].ToString() : "";  // 3rd letter (specialization)
                    SortKey2 = tableName.Length > 4 ? tableName[4].ToString() : "";  // 1st number in year code (4 in 401)
                    SortKey3 = tableName.Length > 6 ? tableName[6].ToString() : "";  // Last letter (section)
                    SortKey4 = ""; // Not used for college
                }
                else
                {
                    // High School format: WEB121-1A
                    // NEW SORT ORDER: course (WEB), grade (12), semester (1), section (A)
                    var match = Regex.Match(tableName, @"^([A-Z]+)(\d{2})(\d)-([A-Z])$");
                    if (match.Success)
                    {
                        SortKey1 = match.Groups[1].Value; // Course (WEB) - NOW PRIMARY
                        SortKey2 = match.Groups[2].Value; // Grade (12) - NOW SECONDARY
                        SortKey3 = match.Groups[3].Value; // Semester (1) - NOW TERTIARY
                        SortKey4 = match.Groups[4].Value; // Section (A) - NOW QUATERNARY
                    }
                    else
                    {
                        // Fallback for non-standard names
                        SortKey1 = SortKey2 = SortKey3 = SortKey4 = "";
                    }
                }
            }
        }
    }
}
