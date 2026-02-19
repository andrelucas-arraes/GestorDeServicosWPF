using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace GestaoAulas.Views
{
    public partial class CustomMessageBox : Window
    {
        public MessageBoxResult Result { get; private set; } = MessageBoxResult.None;

        public CustomMessageBox()
        {
            InitializeComponent();
            // Permite arrastar a janela WindowStyle=None (Fix #13)
            MouseLeftButtonDown += (s, e) => { if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
        }

        public static MessageBoxResult Show(Window owner, string message, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var msgBox = new CustomMessageBox();
            msgBox.Owner = owner;
            msgBox.txtMessage.Text = message;
            msgBox.txtTitle.Text = caption;
            
            // Configurar ícone
            msgBox.txtIcon.Text = icon switch
            {
                MessageBoxImage.Error => "[X]",
                MessageBoxImage.Warning => "[!]",
                MessageBoxImage.Question => "[?]",
                MessageBoxImage.Information => "[i]",
                _ => "[i]"
            };

            // Configurar botões
            switch (button)
            {
                case MessageBoxButton.OK:
                    msgBox.AddButton("OK", MessageBoxResult.OK, true);
                    break;
                case MessageBoxButton.OKCancel:
                    msgBox.AddButton("OK", MessageBoxResult.OK, true);
                    msgBox.AddButton("Cancelar", MessageBoxResult.Cancel, false, true);
                    break;
                case MessageBoxButton.YesNo:
                    msgBox.AddButton("Sim", MessageBoxResult.Yes, true);
                    msgBox.AddButton("Não", MessageBoxResult.No, false, true);
                    break;
                case MessageBoxButton.YesNoCancel:
                    msgBox.AddButton("Sim", MessageBoxResult.Yes, true);
                    msgBox.AddButton("Não", MessageBoxResult.No);
                    msgBox.AddButton("Cancelar", MessageBoxResult.Cancel, false, true);
                    break;
            }

            msgBox.ShowDialog();
            return msgBox.Result;
        }

        public static MessageBoxResult Show(string message, string caption, MessageBoxButton button, MessageBoxImage icon)
        {
            var owner = Application.Current.MainWindow;
            if (owner != null && owner.IsVisible)
                return Show(owner, message, caption, button, icon);
                
            var msgBox = new CustomMessageBox();
            msgBox.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            msgBox.txtMessage.Text = message;
            msgBox.txtTitle.Text = caption;
            
            msgBox.txtIcon.Text = icon switch { MessageBoxImage.Error => "[X]", MessageBoxImage.Warning => "[!]", MessageBoxImage.Question => "[?]", _ => "[i]" };
            
            // Configura TODOS os tipos de botão (Fix #14)
            switch (button) {
                case MessageBoxButton.OK: 
                    msgBox.AddButton("OK", MessageBoxResult.OK, true); 
                    break;
                case MessageBoxButton.OKCancel:
                    msgBox.AddButton("OK", MessageBoxResult.OK, true);
                    msgBox.AddButton("Cancelar", MessageBoxResult.Cancel, false, true);
                    break;
                case MessageBoxButton.YesNo: 
                    msgBox.AddButton("Sim", MessageBoxResult.Yes, true); 
                    msgBox.AddButton("Não", MessageBoxResult.No, false, true); 
                    break;
                case MessageBoxButton.YesNoCancel:
                    msgBox.AddButton("Sim", MessageBoxResult.Yes, true);
                    msgBox.AddButton("Não", MessageBoxResult.No);
                    msgBox.AddButton("Cancelar", MessageBoxResult.Cancel, false, true);
                    break;
            }

            msgBox.ShowDialog();
            return msgBox.Result;
        }

        public static MessageBoxResult Show(string message, string caption = "Mensagem", MessageBoxButton button = MessageBoxButton.OK)
        {
            return Show(message, caption, button, MessageBoxImage.Information);
        }

        private void AddButton(string text, MessageBoxResult result, bool isDefault = false, bool isCancel = false)
        {
            var btn = new Button
            {
                Content = text,
                MinWidth = 80,
                Height = 30,
                Margin = new Thickness(10, 0, 0, 0),
                Cursor = System.Windows.Input.Cursors.Hand,
                IsDefault = isDefault,
                IsCancel = isCancel
            };

            // Estilização básica (ideal seria usar Styles do App.xaml, mas vamos hardcodar por segurança)
            btn.Background = isDefault ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#238636")) : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#21262d"));
            btn.Foreground = Brushes.White;
            btn.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#30363d"));
            btn.BorderThickness = new Thickness(1);
            
            // Tratamento de hover (simples via evento, já que não estamos no XAML com setters complexos)
            btn.MouseEnter += (s, e) => btn.Opacity = 0.9;
            btn.MouseLeave += (s, e) => btn.Opacity = 1.0;

            btn.Click += (s, e) =>
            {
                Result = result;
                Close();
            };

            pnlButtons.Children.Add(btn);
        }
    }
}
