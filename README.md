# 💼 Gestor de Serviços e Aulas (WPF)

![Versão](https://img.shields.io/badge/versão-2.1.x-blue)
![.NET](https://img.shields.io/badge/.NET-8.0--windows-purple)
![Licença](https://img.shields.io/badge/licença-MIT-green)
![Plataforma](https://img.shields.io/badge/plataforma-Windows-blue)
![SQLite](https://img.shields.io/badge/banco-SQLite-informational)
![LiveCharts](https://img.shields.io/badge/gráficos-LiveCharts2-orange)

O **Gestor de Serviços e Aulas** é uma aplicação desktop completa, desenvolvida em **C# com WPF** (Windows Presentation Foundation), projetada para facilitar o **controle financeiro** e a **organização de serviços prestados** — como aulas particulares, consultorias, freelances, manutenções e outros tipos de atendimentos.

O sistema oferece uma interface moderna com **tema escuro (dark mode)**, dashboard em tempo real, controle de pagamentos, categorias personalizáveis, sistema de tags, painel de análises com gráficos interativos, exportação de dados, backup integrado e atualização automática.

---

## 📸 Visão Geral

A tela principal do aplicativo é dividida em:

1. **Header** — Título da aplicação com botões de acesso rápido (Análises, Configurações e Exportar Dados).
2. **Dashboard** — 4 cards com estatísticas em tempo real do mês selecionado.
3. **Filtros** — Seleção de período por mês e ano.
4. **Formulário Rápido** — Área para cadastro direto de novos serviços/aulas sem abrir diálogos.
5. **Tabela de Dados** — DataGrid interativa com todos os registros filtrados.

---

## 🚀 Funcionalidades

### 📋 Gestão de Lançamentos
- **Cadastro completo** de aulas/serviços com data, dia da semana (automático), descrição, duração, valor e status.
- **Formulário rápido** na tela principal para adicionar serviços sem abrir janelas extras.
- **Diálogo de edição** detalhado com campos específicos por categoria.
- **Edição inline** via duplo clique na tabela para acesso rápido.
- **Exclusão** com confirmação de segurança via diálogo customizado.
- **Clonagem de registros** para facilitar lançamentos repetitivos.

### 🏷️ Categorias Personalizáveis e Tags
- **Categorias padrão** incluídas: `Aula` e `Serviço` (protegidas contra remoção).
- **Categorias customizáveis** — Adicione e remova categorias livremente via Configurações.
- **Persistência no banco** — As categorias são armazenadas em tabela dedicada no SQLite.
- **Sistema de Tags** para classificação livre (ex: "Cliente X", "Projeto Y").
- **Campos dinâmicos**: quando a categoria é "Aula", exibe campos de duração e valor/hora; para outras categorias, exibe campo de valor manual.

### 💰 Controle de Pagamento
- **Status de pagamento** visual com badges coloridas: 🟢 Pago | 🟠 Pendente.
- **Alteração rápida** de status via menu de contexto (clique direito).
- **Cálculo automático** do valor para aulas baseado em `Duração × Valor/Hora`.
- **Valor manual** para categorias que não são aulas.

### 📊 Dashboard & Estatísticas
- **Total de Horas** — Soma das horas de aulas no período selecionado (formato `Xh:MM`).
- **Valor Pendente** — Total financeiro de serviços com status "Pendente" (em R$).
- **Total Recebido** — Total financeiro de serviços com status "Pago" (em R$).
- **Qtd. Serviços** — Quantidade total de registros no período.
- Atualização **automática em tempo real** ao filtrar ou modificar dados.

### 📈 Painel de Análises (Gráficos)
- **Receita Mensal** — Gráfico de barras com a receita total por mês (últimos 12 meses).
- **Receita por Categoria** — Gráfico de pizza mostrando distribuição de receita por categoria.
- **Status de Pagamento** — Gráfico de pizza comparando valores Pago vs. Pendente.
- **Top 10 Clientes/Serviços** — Gráfico de barras horizontais com os maiores faturamentos por descrição.
- **Resumo consolidado** — Cards com total de registros, receita total, pendente e ticket médio.
- Gráficos interativos com tooltips e labels de dados via **LiveCharts2 + SkiaSharp**.

### 🔍 Filtragem Inteligente
- **Filtro por Mês** — Seleção via dropdown com opção "Todos".
- **Filtro por Ano** — Dropdown dinâmico que lista apenas anos com registros.
- **Busca em tempo real** com debounce de 300ms para performance.
- **Filtragem no SQL** — Consultas otimizadas no banco de dados para máxima performance.

### 📤 Exportação de Dados
- **Exportação para Excel** (.xlsx) com formatação profissional:
  - Cabeçalho com título e período.
  - Colunas formatadas (datas, moedas, durações).
  - Estilização automática (bordas, cores, largura de colunas).
  - Linha de totais com soma automática.
- **Exportação para CSV** (.csv) com encoding UTF-8 e escape correto.
- **Diálogo de exportação** com seleção de formato, caminho personalizado e opção de abrir o arquivo após exportar.
- **Nomes de arquivo inteligentes** gerados automaticamente com base no período (ex: `Servicos_Fevereiro_2026.xlsx`).

### 💾 Sistema de Backup
- **Backup manual** do banco de dados com um clique.
- **Backup automático** na inicialização da aplicação.
- **Restauração de backup** com seleção de arquivo e confirmação.
- **Backup externo** — Configuração de pasta sincronizada (OneDrive, Google Drive, Dropbox) para cópia automática.
- **Limpeza automática** de backups antigos para gerenciamento de espaço.
- **Informações do banco** — Tamanho do arquivo e data do último backup exibidos nas configurações.

### 🔄 Atualização Automática
- **Verificação de versão** — Compara a versão do executável em uso com a versão publicada.
- **Atualização com um clique** — Aplica a atualização via script `.bat` temporário.
- **Reinicialização automática** — O app reinicia após a atualização e exibe mensagem de sucesso.
- **Feedback inteligente** — Informa quando já está na versão mais recente.

### ⚙️ Configurações
- **Valor da hora-aula** — Configurável globalmente com formatação monetária em tempo real.
- **Gerenciamento de categorias** — Adicionar e remover categorias de serviço.
- **Gerenciamento de backup** — Backup, restauração e configuração de pasta externa.
- **Atualizar aplicativo** — Verificar e aplicar atualizações disponíveis.
- **Sobre** — Informações da versão e tecnologias.

### 🖥️ Interface e UX
- **Tema escuro (Dark Mode)** moderno com paleta cuidadosa (`#0a0b10`, `#111827`, `#162c46`).
- **Material Design** via biblioteca MaterialDesignInXAML para componentes polidos.
- **Caixas de mensagem customizadas** (`CustomMessageBox`) com estilo consistente.
- **Menu de contexto** (clique direito) com ações rápidas: Editar, Excluir, Marcar Pago/Pendente.
- **Máscara de entrada** automática para datas (`DD/MM/AAAA`) e durações (`H:MM`).
- **Converters XAML** para formatação automática de datas, moeda (R$), duração e status.
- **Janela responsiva** com tamanho mínimo de 900×600 e inicial de 1200×850.
- **Seleção de data via calendário** no diálogo de edição (DatePicker).
- **Instância única** — Impede múltiplas execuções simultâneas via Mutex.

### 🛡️ Robustez e Qualidade
- **Validação de dados** com FluentValidation antes de salvar.
- **Tratamento global de exceções** — Erros não tratados são capturados e logados.
- **Logging estruturado** com Serilog em arquivo CSV, incluindo rotação automática (7 dias, 10 MB).
- **Proteção contra race conditions** no carregamento de dados (sistema de `loadId`).
- **Debounce** na busca por texto para evitar consultas excessivas.
- **Rollback de status** em caso de falha ao atualizar no banco.
- **Sanitização numérica** para evitar OverflowException em cálculos.
- **Migração automática de dados** — Detecta e migra banco de versões anteriores.

---

## 🛠️ Tecnologias Utilizadas

| Tecnologia | Versão | Uso |
|---|---|---|
| **C#** | 12 | Linguagem principal |
| **.NET** | 8.0-windows | Framework (WPF) |
| **CommunityToolkit.Mvvm** | 8.2.2 | Padrão MVVM (ObservableObject, RelayCommand) |
| **SQLite** | — | Banco de dados local (portátil) |
| **Microsoft.Data.Sqlite** | 8.0.0 | Driver SQLite |
| **Dapper** | 2.1.35 | Micro-ORM para acesso a dados |
| **Dapper.Contrib** | 2.0.78 | Extensões CRUD para Dapper |
| **ClosedXML** | 0.102.2 | Geração de planilhas Excel (.xlsx) |
| **LiveChartsCore.SkiaSharpView.WPF** | 2.0.0-rc3.3 | Gráficos interativos (barras, pizza, etc.) |
| **MaterialDesignThemes** | 4.9.0 | Componentes UI Material Design |
| **MaterialDesignColors** | 2.1.4 | Paleta de cores Material Design |
| **FluentValidation** | 11.9.0 | Validação de regras de negócio |
| **Serilog** | 3.1.1 | Logging estruturado |
| **Serilog.Sinks.File** | 5.0.0 | Log em arquivo com rotação |
| **Serilog.Extensions.Hosting** | 8.0.0 | Integração Serilog com Host |
| **Microsoft.Extensions.DependencyInjection** | 8.0.0 | Injeção de dependência |
| **Microsoft.Extensions.Hosting** | 8.0.0 | Host genérico .NET |

---

## 🏗️ Arquitetura

O projeto segue o padrão **MVVM (Model-View-ViewModel)** com **Injeção de Dependência**, garantindo separação clara de responsabilidades e testabilidade.

```
GestaoAulas/
│
├── 📁 Models/
│   └── Aula.cs                       # Entidade principal (ObservableObject)
│
├── 📁 ViewModels/
│   └── MainViewModel.cs              # Lógica de interface e comandos
│
├── 📁 Views/
│   ├── AnalisesDialog.xaml/.cs        # Painel de análises com gráficos (LiveCharts2)
│   ├── AulaDialog.xaml/.cs            # Diálogo de edição de serviços/aulas
│   ├── ConfiguracoesDialog.xaml/.cs    # Diálogo de configurações (backup, categorias, atualização)
│   ├── ExportarDialog.xaml/.cs        # Diálogo de exportação (Excel/CSV)
│   └── CustomMessageBox.xaml/.cs      # Caixa de mensagem personalizada
│
├── 📁 Repositories/
│   ├── IAulaRepository.cs             # Interface do repositório
│   └── AulaRepository.cs             # Implementação com Dapper + SQLite
│
├── 📁 Services/
│   ├── DialogService.cs               # Serviço de diálogos (abstração UI)
│   └── BackupManager.cs              # Gerenciador de backups
│
├── 📁 Export/
│   └── ExportManager.cs              # Exportação Excel/CSV
│
├── 📁 Validators/
│   └── AulaValidator.cs              # Regras de validação (FluentValidation)
│
├── 📁 Infrastructure/
│   └── DecimalTypeHandler.cs         # Handler para tipos decimais no Dapper
│
├── 📁 Utils/
│   ├── Converters.cs                 # Value Converters XAML (Status, Moeda, Data, etc.)
│   └── FormatUtils.cs                # Utilitários de formatação e máscaras
│
├── 📁 Assets/
│   └── icon.ico                      # Ícone da aplicação
│
├── MainWindow.xaml/.cs               # Janela principal
├── App.xaml/.cs                      # Startup, DI, migração e configuração global
└── GestaoAulas.csproj                # Definição do projeto
```

---

## 📦 Modelo de Dados

A entidade principal `Aula` contém os seguintes campos:

| Campo | Tipo | Descrição |
|---|---|---|
| `Id` | `int` | Identificador único (PK, auto-increment) |
| `Data` | `DateTime` | Data do serviço/aula |
| `DiaSemana` | `string` | Dia da semana abreviado (automático: Seg, Ter, Qua...) |
| `NomeAula` | `string` | Descrição ou nome do aluno/cliente |
| `Categoria` | `string` | Tipo de serviço (personalizável via Configurações) |
| `Tag` | `string` | Tag livre para classificação |
| `Duracao` | `double` | Duração em horas (apenas para aulas) |
| `ValorHora` | `decimal` | Valor cobrado por hora (apenas para aulas) |
| `Valor` | `decimal` | Valor total do serviço (R$) |
| `Status` | `string` | Status do pagamento: Pendente ou Pago |
| `DataCriacao` | `DateTime` | Data de criação do registro |
| `DataAtualizacao` | `DateTime` | Data da última atualização |

---

## ⚙️ Como Executar

### Pré-requisitos
- **Visual Studio 2022** ou posterior (com workload ".NET Desktop Development").
- **SDK do .NET 8.0** ou superior.

### Passos

1. **Clone o repositório**:
   ```bash
   git clone https://github.com/andrelucas-arraes/GestorDeServicosWPF.git
   ```

2. **Abra o projeto** no Visual Studio:
   ```
   GestaoAulas.csproj
   ```

3. **Restaure os pacotes NuGet**:
   ```bash
   dotnet restore
   ```

4. **Execute o projeto**:
   - Pressione `F5` no Visual Studio, ou:
   ```bash
   dotnet run
   ```

> **📝 Nota**: O sistema criará automaticamente o banco de dados `aulas.db` na pasta `%APPDATA%/GestorDeServicos/` caso ele não exista. A estrutura das tabelas é criada via migration automática no primeiro uso. Se houver um banco legado na pasta antiga (`GestaoAulas`), ele será migrado automaticamente.

### Publicação (Build de Produção)

Para criar um executável standalone:

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

O executável será gerado na pasta `publish/`.

---

## 📂 Estrutura de Pastas em Execução

Ao executar, a aplicação gerencia os seguintes diretórios:

```
📁 %APPDATA%/GestorDeServicos/
├── aulas.db                     # Banco de dados SQLite
├── backup_config.txt            # Configuração de backup externo
└── 📁 backups/                  # Backups automáticos do banco

📁 Diretório de execução/
├── GestorDeServicos.exe         # Executável
├── 📁 logs/                     # Logs da aplicação (Serilog, CSV rotativo)
└── 📁 exports/                  # Arquivos exportados (Excel/CSV)
```

---

## 🔧 Configurações Disponíveis

Acessíveis pelo botão **"Configurações"** no header da aplicação:

| Configuração | Descrição |
|---|---|
| **Valor da Hora-Aula** | Define o valor padrão (R$) para novas aulas. Utilizado no cálculo automático `Duração × Valor/Hora`. |
| **Gerenciar Categorias** | Adicionar e remover categorias de serviço. Categorias "Aula" e "Serviço" são protegidas. |
| **Backup Manual** | Cria uma cópia do banco `aulas.db` na pasta de backups. |
| **Restaurar Backup** | Restaura o banco a partir de um arquivo de backup selecionado. |
| **Pasta de Backup Externo** | Define uma pasta sincronizada (nuvem) para cópia secundária. |
| **Atualizar Aplicativo** | Verifica e aplica atualizações disponíveis a partir da pasta de publicação. |

---

## 🤝 Contribuindo

Contribuições são bem-vindas! Siga os passos:

1. Faça um **fork** do projeto.
2. Crie uma branch para sua feature: `git checkout -b feature/minha-feature`.
3. Commit suas alterações: `git commit -m 'feat: adiciona minha feature'`.
4. Push para a branch: `git push origin feature/minha-feature`.
5. Abra um **Pull Request**.

---

## 📝 Licença

Este projeto está sob a licença [MIT](LICENSE).

---

## 👤 Autor

Desenvolvido por **André Lucas Arraes**.

- 🔗 GitHub: [@andrelucas-arraes](https://github.com/andrelucas-arraes)
