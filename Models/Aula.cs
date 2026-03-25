using System;
using CommunityToolkit.Mvvm.ComponentModel;
using Dapper.Contrib.Extensions;

namespace GestaoAulas.Models
{
    /// <summary>
    /// Modelo representando um serviço/aula particular.
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

        [ObservableProperty]
        private string _tag = string.Empty;

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
                if (Duracao <= 0) return "-";

                int horas = (int)Duracao;
                int minutos = (int)((Duracao - horas) * 60);
                return $"{horas}:{minutos:D2}";
            }
        }

        [Write(false)]
        public string ValorFormatado => Valor.ToString("C2", new System.Globalization.CultureInfo("pt-BR"));

        partial void OnDataChanged(DateTime value)
        {
            DiaSemana = ObterDiaSemana(value);
        }

        public static string ObterDiaSemana(DateTime data)
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
                Tag = "",
                Duracao = 0,
                Valor = 0,
                ValorHora = 0,
                Status = "Pendente",
                DataCriacao = DateTime.Now,
                DataAtualizacao = DateTime.Now
            };
            
            aula.DiaSemana = ObterDiaSemana(aula.Data);
            
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
                Tag = this.Tag,
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
