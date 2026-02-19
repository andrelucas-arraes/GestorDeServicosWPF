using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper.Contrib.Extensions;

namespace GestaoAulas.Models
{
    /// <summary>
    /// Modelo representando uma aula particular.
    /// Equivalente à tabela 'aulas' no SQLite.
    /// </summary>
    [Table("aulas")]
    public partial class Aula : ObservableObject
    {
        [property: Key]
        [ObservableProperty]
        private int _id;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DataFormatada))]
        private DateTime _data;

        [ObservableProperty]
        private string _diaSemana = string.Empty;

        [ObservableProperty]
        private string _nomeAula = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(DuracaoFormatada))]
        private double _duracao;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ValorFormatado))]
        private decimal _valor;

        [ObservableProperty]
        private string _status = "Pendente";

        [ObservableProperty]
        private decimal _valorHora;

        [ObservableProperty]
        private DateTime _dataCriacao;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsAula))]
        [NotifyPropertyChangedFor(nameof(DuracaoFormatada))]
        private string _categoria = "Aula";

        // Hooks (Triggers quando propriedades mudam)
        // REMOVIDO: RecalcularValor automático causava bug ao carregar do banco de dados (Dapper setando Duracao/ValorHora antes de Categoria)
        // partial void OnDuracaoChanged(double value) => RecalcularValor();
        // partial void OnValorHoraChanged(decimal value) => RecalcularValor();
        
        // REMOVIDO: Removemos também o hook de Categoria para evitar qualquer recálculo indesejado durante carga de dados.
        // A lógica de recálculo deve ser explícita na UI.
        // partial void OnCategoriaChanged(string value)
        // {
        //     if (value == "Aula")
        //     {
        //         RecalcularValor();
        //     }
        // }
        
        public bool IsAula => Categoria == "Aula";

        [ObservableProperty]
        private DateTime _dataAtualizacao;

        // Propriedades de exibição (somente leitura)
        
        [Write(false)] // Dapper ignora
        public string DataFormatada => Data.ToString("dd/MM/yyyy");

        [Write(false)]
        public string DuracaoFormatada
        {
            get
            {
                if (Categoria != "Aula") return "-";

                int horas = (int)Duracao;
                int minutos = (int)((Duracao - horas) * 60);
                return $"{horas}:{minutos:D2}";
            }
        }

        [Write(false)]
        public string ValorFormatado => Valor.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));

        // Configuração Estática (Default para novas aulas)
        public static decimal ValorHoraAula { get; set; } = 50.00m;

        // Hooks (Triggers quando propriedades mudam)
        // REMOVIDO: RecalcularValor automático causava bug ao carregar do banco de dados (Dapper setando Duracao/ValorHora antes de Categoria)
        // partial void OnDuracaoChanged(double value) => RecalcularValor();
        // partial void OnValorHoraChanged(decimal value) => RecalcularValor();

        partial void OnDataChanged(DateTime value)
        {
            DiaSemana = ObterDiaSemana(value);
        }

        /// <summary>
        /// Recalcula o valor da aula com base na duração atual e no valor da hora registrado nesta aula.
        /// </summary>
        public void RecalcularValor()
        {
            // Se não for aula, o valor é definido manualmente
            // Verificação extra para garantir que não recalcule se a categoria for "Serviço", "Freelance", etc.
            if (Categoria != "Aula") return;

            try
            {
                // Se o valor da hora desta aula for zero (caso de legado), usa o global atual
                if (ValorHora <= 0) ValorHora = ValorHoraAula;
                
                // Sanitização para evitar OverflowException em casos extremos de digitação
                if (Duracao > 1000000 || ValorHora > 1000000000) 
                {
                    Valor = 0;
                    return;
                }

                // Correção de precisão financeira: arredonda para 2 casas
                // Cast explicito de double para decimal para cálculo monetário
                Valor = Math.Round((decimal)Duracao * ValorHora, 2);
            }
            catch (OverflowException)
            {
                Valor = 0;
            }
        }

        private static string ObterDiaSemana(DateTime data)
        {
            return data.DayOfWeek switch
            {
                DayOfWeek.Sunday => "Dom",
                DayOfWeek.Monday => "Seg",
                DayOfWeek.Tuesday => "Ter",
                DayOfWeek.Wednesday => "Qua",
                DayOfWeek.Thursday => "Qui",
                DayOfWeek.Friday => "Sex",
                DayOfWeek.Saturday => "Sáb",
                _ => ""
            };
        }

        public static Aula CriarNova()
        {
            var aula = new Aula
            {
                Data = DateTime.Today,
                NomeAula = "",
                Duracao = 1.0,
                ValorHora = ValorHoraAula, // Inicializa com o valor atual das configurações
                Status = "Pendente",
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };
            
            // Garante que DiaSemana seja preenchido (Fix #2)
            aula.DiaSemana = ObterDiaSemana(aula.Data);
            
            // Calcula valor inicial
            aula.RecalcularValor();
            
            return aula;
        }

        /// <summary>
        /// Cria uma cópia profunda da aula, sem compartilhar estado do ObservableObject.
        /// </summary>
        public Aula Clone()
        {
            return new Aula
            {
                Id = this.Id,
                Data = this.Data,
                DiaSemana = this.DiaSemana,
                NomeAula = this.NomeAula,
                Duracao = this.Duracao,
                Valor = this.Valor,
                ValorHora = this.ValorHora,
                Status = this.Status,
                Categoria = this.Categoria,
                DataCriacao = this.DataCriacao,
                DataAtualizacao = this.DataAtualizacao
            };
        }
    }
}
