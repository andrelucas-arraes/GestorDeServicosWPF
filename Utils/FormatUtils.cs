using System;
using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace GestaoAulas.Utils
{
    public static class FormatUtils
    {
        public static bool TryParseData(string texto, out DateTime data)
        {
            data = DateTime.MinValue;
            if (string.IsNullOrWhiteSpace(texto)) return false;

            string[] formatos = { "dd/MM/yyyy", "d/M/yyyy", "dd-MM-yyyy", "d/M/yy" };
            return DateTime.TryParseExact(texto, formatos, 
                CultureInfo.InvariantCulture, DateTimeStyles.None, out data);
        }

        public static bool TryParseDuracao(string texto, out double duracao)
        {
            duracao = 0;
            if (string.IsNullOrWhiteSpace(texto)) return false;

            // Formato H:MM
            if (texto.Contains(':'))
            {
                var partes = texto.Split(':');
                if (partes.Length == 2 &&
                    int.TryParse(partes[0], out int horas) &&
                    int.TryParse(partes[1], out int minutos))
                {
                    duracao = horas + (minutos / 60.0);
                    return duracao > 0;
                }
            }

            // Formato decimal
            if (double.TryParse(texto.Replace(",", "."), 
                NumberStyles.Any, CultureInfo.InvariantCulture, out duracao))
            {
                return duracao > 0;
            }

            return false;
        }

        public static bool TryParseDecimal(string texto, out decimal valor)
        {
            valor = 0;
            if (string.IsNullOrWhiteSpace(texto)) return false;

            // Limpa caracteres de moeda e espaços extras
            string limpo = texto.Replace("R$", "").Replace(" ", "").Trim();

            // Lógica robusta para pt-BR:
            // No Brasil, vírgula é decimal. Ponto é milhar.
            // Se houver vírgula, tratamos o ponto como lixo (milhar).
            if (limpo.Contains(","))
            {
                limpo = limpo.Replace(".", "");
            }
            
            // Tenta conversão usando pt-BR explicitamente
            var culturePt = new CultureInfo("pt-BR");
            if (decimal.TryParse(limpo, NumberStyles.Any, culturePt, out valor))
                return true;

            // Fallback para Invariant (caso o usuário digite ponto como decimal)
            if (decimal.TryParse(limpo.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out valor))
                return true;

            return false;
        }

        private static bool _isFormattingData = false;
        
        public static void MascaraData(TextBox textBox, bool isBackspace = false)
        {
            // Evita recursão quando modificamos o texto
            if (_isFormattingData) return;
            
            try
            {
                _isFormattingData = true;
                
                string text = textBox.Text;
                if (string.IsNullOrEmpty(text)) return;

                // Extrai apenas dígitos
                string digits = new string(text.Where(char.IsDigit).ToArray());
                
                // Limita a 8 dígitos (DD/MM/AAAA)
                if (digits.Length > 8) digits = digits.Substring(0, 8);

                // Reconstrói o texto formatado
                string newText = "";
                
                for (int i = 0; i < digits.Length; i++)
                {
                    newText += digits[i];
                    
                    // Adiciona barra após o dia (posição 1) e mês (posição 3)
                    // Não adiciona se for o último dígito E estiver apagando
                    bool isLastDigit = (i == digits.Length - 1);
                    if (i == 1 || i == 3)
                    {
                        // Sempre adiciona a barra, exceto se for backspace no último dígito
                        if (!isBackspace || !isLastDigit)
                        {
                            newText += "/";
                        }
                    }
                }

                // Só atualiza se houver diferença
                if (newText != textBox.Text)
                {
                    int caretPos = textBox.CaretIndex;
                    int lengthDiff = newText.Length - textBox.Text.Length;
                    
                    textBox.Text = newText;
                    
                    // Ajusta a posição do cursor
                    int newCaret = caretPos + lengthDiff;
                    if (newCaret < 0) newCaret = 0;
                    if (newCaret > newText.Length) newCaret = newText.Length;
                    textBox.CaretIndex = newCaret;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro na formatação de data: {ex.Message}");
            }
            finally
            {
                _isFormattingData = false;
            }
        }
        
        private static bool _isFormattingDuracao = false;
        
        public static void MascaraDuracao(TextBox textBox)
        {
            // Evita recursão quando modificamos o texto
            if (_isFormattingDuracao) return;
            
            try
            {
                _isFormattingDuracao = true;
                
                string text = textBox.Text;
                if (string.IsNullOrEmpty(text)) return;

                string digits = new string(text.Where(char.IsDigit).ToArray());
                
                if (digits.Length > 4) digits = digits.Substring(0, 4);

                string newText = digits;
                
                // Se tiver 3 ou mais dígitos, formata como H:MM (ex: 130 -> 1:30)
                if (digits.Length >= 3)
                {
                    newText = digits.Substring(0, digits.Length - 2) + ":" + digits.Substring(digits.Length - 2);
                }
                
                if (newText != textBox.Text)
                {
                    int caretPos = textBox.CaretIndex;
                    int lengthDiff = newText.Length - textBox.Text.Length;

                    textBox.Text = newText;

                    int newCaret = caretPos + lengthDiff;
                    if (newCaret < 0) newCaret = 0;
                    if (newCaret > newText.Length) newCaret = newText.Length;
                    
                    textBox.CaretIndex = newCaret;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erro na formatação de duração: {ex.Message}");
            }
            finally
            {
                _isFormattingDuracao = false;
            }
        }
    }
}
