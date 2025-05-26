using SchoolAttendance.UI.Notifier;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace SchoolAttendance
{
    public static class WindowManager
    {
        private static readonly Dictionary<Type, Window> _activeWindows = new Dictionary<Type, Window>();
        private static bool _isAppClosing = false;
        public static int ActiveWindowCount => _activeWindows.Count;

        static WindowManager()
        {
            Application.Current.Exit += (s, e) => _isAppClosing = true;
        }

        public static void ShowWindow<T>(Func<T> createWindow, Action onClosed = null) where T : Window
        {
            if (_isAppClosing) return;

            if (_activeWindows.TryGetValue(typeof(T), out var existingWindow))
            {
                existingWindow.Focus();
                return;
            }

            var window = createWindow();
            _activeWindows[typeof(T)] = window;

            void Cleanup()
            {
                if (_isAppClosing) return;

                window.Closed -= OnClosed;
                _activeWindows.Remove(typeof(T));
                onClosed?.Invoke();
            }

            void OnClosed(object sender, EventArgs e) => SafeInvoke(Cleanup);

            window.Closed += OnClosed;
            window.Show();
        }

        public static void CloseAllWindows()
        {
            foreach (var window in _activeWindows.Values.ToList())
            {
                window.Close();
            }
            _activeWindows.Clear();
        }

        private static void SafeInvoke(Action action)
        {
            try
            {
                if (!_isAppClosing)
                {
                    Application.Current.Dispatcher.Invoke(action);
                }
            }
            catch (Exception ex)
            {
                new WarningWindow("Error", ex.Message);
            }
        }
    }
}
