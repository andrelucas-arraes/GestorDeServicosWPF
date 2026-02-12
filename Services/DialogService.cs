using System.Threading.Tasks;

namespace GestaoAulas.Services
{
    public interface IDialogService
    {
        void ShowMessage(string message, string title);
        bool Confirm(string message, string title);
        
        // Métodos específicos para janelas da aplicação
        bool ShowAulaDialog(GestaoAulas.Models.Aula aula);
        void ShowExportDialog(System.Collections.Generic.List<GestaoAulas.Models.Aula> aulas, int? mes, int? ano);
        void ShowConfiguracoesDialog();
    }

    public class DialogService : IDialogService
    {
        private System.Windows.Window? GetMainWindow() => System.Windows.Application.Current.MainWindow;

        public void ShowMessage(string message, string title)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var owner = GetMainWindow();
                if (owner != null && owner.IsVisible)
                    GestaoAulas.Views.CustomMessageBox.Show(owner, message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                else
                    GestaoAulas.Views.CustomMessageBox.Show(message, title, System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
            });
        }

        public bool Confirm(string message, string title)
        {
            return System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var owner = GetMainWindow();
                System.Windows.MessageBoxResult result;
                
                if (owner != null && owner.IsVisible)
                    result = GestaoAulas.Views.CustomMessageBox.Show(owner, message, title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                else
                    result = GestaoAulas.Views.CustomMessageBox.Show(message, title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Question);
                    
                return result == System.Windows.MessageBoxResult.Yes;
            });
        }

        public bool ShowAulaDialog(GestaoAulas.Models.Aula aula)
        {
            return System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new GestaoAulas.Views.AulaDialog(aula);
                dialog.Owner = GetMainWindow();
                return dialog.ShowDialog() == true;
            });
        }

        public void ShowExportDialog(System.Collections.Generic.List<GestaoAulas.Models.Aula> aulas, int? mes, int? ano)
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new GestaoAulas.Views.ExportarDialog(aulas, mes, ano);
                dialog.Owner = GetMainWindow();
                dialog.ShowDialog();
            });
        }

        public void ShowConfiguracoesDialog()
        {
            System.Windows.Application.Current.Dispatcher.Invoke(() =>
            {
                var dialog = new GestaoAulas.Views.ConfiguracoesDialog();
                dialog.Owner = GetMainWindow();
                dialog.ShowDialog();
            });
        }
    }
}
