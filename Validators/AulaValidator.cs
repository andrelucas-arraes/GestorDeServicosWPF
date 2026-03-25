using System;
using FluentValidation;
using GestaoAulas.Models;

namespace GestaoAulas.Validators
{
    public class AulaValidator : AbstractValidator<Aula>
    {
        public AulaValidator()
        {
            RuleFor(x => x.NomeAula)
                .NotEmpty().WithMessage("A descrição/aluno deve ser informada.")
                .MinimumLength(3).WithMessage("A descrição deve ter pelo menos 3 caracteres.");

            RuleFor(x => x.Data)
                .NotEmpty().WithMessage("A data é obrigatória.")
                .Must(ValidarAnoRazoavel).WithMessage("Ano inválido (muito distante).");

            RuleFor(x => x.Valor)
                .GreaterThanOrEqualTo(0)
                .WithMessage("O valor não pode ser negativo.");

            RuleFor(x => x.Status)
                .Must(x => x == "Pendente" || x == "Pago")
                .WithMessage("Status inválido. Use 'Pendente' ou 'Pago'.");
        }

        private bool ValidarAnoRazoavel(DateTime data)
        {
            return data.Year >= 2020 && data.Year <= DateTime.Now.Year + 5;
        }
    }
}
