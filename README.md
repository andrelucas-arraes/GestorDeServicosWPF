# Gestor de Serviços e Aulas (WPF)

![Versão](https://img.shields.io/badge/versão-2.1.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0--windows-purple)
![Licença](https://img.shields.io/badge/licença-MIT-green)

O **Gestor de Serviços e Aulas** é uma aplicação desktop desenvolvida em C# com WPF (Windows Presentation Foundation), projetada para facilitar o controle financeiro e a organização de serviços prestados, como aulas particulares, consultorias ou outros tipos de atendimentos.

---

## 🚀 Funcionalidades

- **Gestão de Lançamentos**: Cadastro completo de aulas/serviços com descrição, data, duração e valores.
- **Controle de Pagamento**: Status visual para identificar rapidamente quais serviços estão pagos ou pendentes.
- **Filtragem Inteligente**: Filtros por mês, ano e termo de busca (pesquisa em tempo real).
- **Relatórios e Estatísticas**: Cálculo automático de totais mensais e status de ganhos.
- **Exportação para Excel**: Geração de planilhas detalhadas utilizando a biblioteca ClosedXML.
- **Interface Moderna**: Utiliza o Material Design para uma experiência de usuário limpa e intuitiva.
- **Banco de Dados Local**: Armazenamento seguro via SQLite (Dapper), garantindo portabilidade.

---

## 🛠️ Tecnologias Utilizadas

- **Linguagem**: C#
- **Framework**: .NET 8.0 Windows (WPF)
- **Arquitetura**: MVVM (Model-View-ViewModel) com CommunityToolkit.Mvvm
- **Persistência**: SQLite & Dapper/Dapper.Contrib
- **UI/UX**: Material Design In XAML
- **Exportação**: ClosedXML (Excel SDK)
- **Validação**: FluentValidation
- **Logs**: Serilog (Sink para arquivo)
- **Injeção de Dependência**: Microsoft.Extensions.DependencyInjection

---

## 📁 Estrutura do Projeto

- **Assets**: Ícones e recursos visuais.
- **Models**: Definição das entidades (ex: `Aula`).
- **ViewModels**: Lógica de interface e comando (MainViewModel).
- **Views**: Telas e diálogos em XAML.
- **Repositories**: Acesso a dados (SQLite).
- **Services**: Lógica de negócio e serviços externos (Diálogos, Exportação).
- **Infrastructure**: Configurações de banco de dados e handlers.
- **Validators**: Regras de validação de dados.
- **Utils**: Classes utilitárias e ajudantes de formatação.

---

## ⚙️ Como Executar

### Pré-requisitos
- Visual Studio 2022 ou posterior.
- SDK do .NET 8.0.

### Passos
1. Clone o repositório ou baixe os arquivos.
2. Abra o arquivo `GestaoAulas.csproj` no Visual Studio.
3. Restaure os pacotes NuGet (`dotnet restore`).
4. Execute o projeto (F5).

> **Nota**: O sistema criará automaticamente o banco de dados `aulas.db` no diretório de execução caso ele não exista.

---

## 📝 Licença

Este projeto está sob a licença [MIT](LICENSE).

---

Desenvolvido por **Gestor de Serviços**.
