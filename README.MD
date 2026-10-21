# TracLabs — Gestão Metrológica e Emissão de Certificados de Calibração

O **TracLabs** é um sistema voltado para **laboratórios de metrologia e ensaio**, projetado para substituir controles manuais baseados em planilhas eletrônicas por uma plataforma rastreável, segura e automatizada, em conformidade com os requisitos técnicos da norma **ABNT NBR ISO/IEC 17025**.

O projeto contempla o controle de equipamentos de clientes e padrões de referência do laboratório, execução de cálculos metrológicos automatizados (incluindo balanço completo de incertezas de medição) e emissão padronizada de certificados de calibração.

---

## Sumário

- [Visão Geral e Motivação](#-visão-geral-e-motivação)
- [Principais Funcionalidades](#-principais-funcionalidades)
- [Motor de Cálculo Metrológico (Volume Gravimétrico)](#-motor-de-cálculo-metrológico-volume-gravimétrico)
- [Arquitetura da Solução](#-arquitetura-da-solução)
- [Tecnologias e Pacotes](#-tecnologias-e-pacotes)
- [Como Executar o Projeto](#-como-executar-o-projeto)
- [Executando os Testes](#-executando-os-testes)
- [Roadmap do Projeto](#-roadmap-do-projeto)
- [Autor](#-autor)

---

## Visão Geral e Motivação

Muitos laboratórios de calibração ainda dependem de planilhas manuais no Excel para registrar medições, controlar prazos de calibração de padrões e emitir certificados. Essa abordagem está sujeita a erros de transcrição, perda de rastreabilidade e atrasos operacionais.

O **TracLabs** resolve esse problema oferecendo:
- **Rastreabilidade Metrológica**: Vínculo rigoroso entre o equipamento calibrado, os padrões de referência utilizados (com seus respectivos erros, correções, resoluções e incertezas) e o técnico/supervisor responsável.
- **Automação de Cálculos**: Processamento no back-end de medições e estimativa de incerteza expandida sem intervenção manual em fórmulas.
- **Padronização e Segurança**: Controle de numeração sequencial concorrente de certificados, autenticação via JWT com controle de acesso baseado em perfis (`Tecnico` e `Supervisor`) e histórico auditável.

---

## Principais Funcionalidades

- **Autenticação e Controle de Acesso (RBAC)**:
  - Registro e login via **ASP.NET Core Identity** e **JWT Bearer**.
  - Perfis de acesso diferenciados: `Tecnico` e `Supervisor` (criados automaticamente via *seed* na inicialização da API).
- **Gestão de Ativos e Clientes**:
  - Cadastro de clientes (`Client`) com suporte a CPF/CNPJ.
  - Controle de equipamentos de clientes (`Equipment`) e ciclo de status (`Received`, `Approved`, `InCalibration`, `Calibrated`, `CertificateIssued`, `Returned` e `NonConforming`).
  - Cadastro de padrões de referência do laboratório (`StandardEquipment`), incluindo número de certificado, órgão emissor, resolução, correção, incerteza expandida ($U$), fator de abrangência ($k$) e validade.
- **Calibração Gravimétrica de Volume**:
  - Cálculo automatizado de volume corrigido a $20\text{ }^\circ\text{C}$ ($V_{20}$) e balanço completo de incerteza de medição.
- **Controle e Emissão de Certificados**:
  - Geração de numeração sequencial anual segura contra concorrência (`CertificateSequence`).
  - Suporte à geração de certificados em PDF (**QuestPDF**) e importação de dados de planilhas legadas (**EPPlus**).

---

## Motor de Cálculo Metrológico (Volume Gravimétrico)

O serviço `VolumeCalibrationService` implementa o método gravimétrico para calibração de vidrarias e instrumentos volumétricos de laboratório (balões volumétricos, pipetas de 1 ou 2 traços, pipetas graduadas, buretas e picnômetros), seguindo rigorosamente:

- **ABNT NBR ISO 4787:2013** — *Vidraria de laboratório — Instrumentos volumétricos de vidro — Métodos de teste de capacidade e uso*
- **DOC-CGCRE-027 (INMETRO)** — *Orientação para Acreditação de Laboratórios na Área de Medição de Volume*
- **JCGM 100:2008 (GUM)** — *Guia para a Expressão da Incerteza de Medição*

### Etapas processadas pelo motor:
1. **Compensação Ambiental**: Aplicação das correções de certificado para termômetros (água e ambiente), barômetro e termo-higrômetro.
2. **Massa Específica do Ar ($\rho_a$)**: Cálculo pela equação de Spieweck e estimativa de sua incerteza combinada $u(\rho_a)$.
3. **Massa Específica da Água ($\rho_w$)**: Cálculo pela equação de Tanaka compensada pela temperatura da água e incerteza $u(\rho_w)$.
4. **Volume Corrigido a $20\text{ }^\circ\text{C}$ ($V_{20,i}$)**: Suporte tanto a pesagens diretas da massa de água quanto a medições por diferença (recipiente vazio/tara e cheio ou escoamento), aplicando empuxo do ar e coeficiente de dilatação térmica volumétrica ($\gamma$) do material (vidro borossilicato 3.3, borossilicato 5.0, vidro soda-cal ou plástico/polipropileno).
5. **Balanço de Incertezas ($u_c$ e $U$)**:
   - Fontes avaliadas: massa/balança (resolução, excentricidade, repetibilidade e certificado), massa específica da água, massa específica do ar, coeficiente de dilatação térmica, temperatura da água, repetibilidade das medições (Tipo A) e leitura de menisco.
   - Cálculo dos graus de liberdade efetivos ($\nu_{\text{eff}}$) pela fórmula de **Welch-Satterthwaite**.
   - Determinação do fator de abrangência $k$ pela distribuição $t$ de Student para nível de confiança de $95,45\%$ e cálculo da incerteza expandida $U = k \cdot u_c$.

---

## Arquitetura da Solução

A solução segue os princípios da **Clean Architecture**, garantindo baixo acoplamento, alta testabilidade e separação clara de responsabilidades:

```text
TracLabs/
├── docs/                                # Documentação do projeto, normas de referência e planilhas de validação
├── src/
│   ├── TracLabs.Domain/                 # Núcleo do negócio: Entidades, Value Objects e Enums (sem dependências externas)
│   ├── TracLabs.Application/            # Casos de uso, DTOs, Interfaces de Serviços/Repositórios, FluentValidation e MediatR
│   ├── TracLabs.Infrastructure/         # EF Core (PostgreSQL), Migrations, Repositórios, Identity/JWT e Serviços Metrológicos
│   └── TracLabs.API/                    # Controllers REST, Middleware global de exceções, Configuração DI e Swagger
├── tests/
│   ├── TracLabs.Domain.Tests/           # Testes unitários de regras de domínio (ex.: sequência de certificados)
│   └── TracLabs.Application.Tests/      # Testes unitários do motor metrológico validados contra planilhas reais
├── compose.yaml                         # Ambiente Docker Compose (PostgreSQL)
├── Directory.Build.props                # Configurações globais de compilação MSBuild
└── TracLabs.slnx                        # Arquivo de solução .NET
```

---

## Tecnologias e Pacotes

| Camada / Categoria | Tecnologia / Biblioteca | Versão |
| :--- | :--- | :--- |
| **Plataforma & Linguagem** | .NET / C# | `.NET 10.0` |
| **API & Documentação** | ASP.NET Core Web API + Swashbuckle (Swagger) | `10.0.0` / `7.3.1` |
| **Banco de Dados** | PostgreSQL | `18.4-alpine` |
| **ORM & Acesso a Dados** | Entity Framework Core (`Npgsql`) + Dapper | `10.0.0` / `2.1.79` |
| **Autenticação & Segurança** | ASP.NET Core Identity + JWT Bearer | `10.0.0` |
| **Arquitetura & Validação** | MediatR, FluentValidation, AutoMapper | `12.4.1` / `12.1.1` / `16.1.1` |
| **Documentos & Planilhas** | QuestPDF (PDF) + EPPlus (Excel `.xlsx`) | `2025.4.0` / `7.5.2` |
| **Testes Automatizados** | xUnit + Coverlet | `2.9.3` |

---

## Como Executar o Projeto

### Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) instalado
- [Docker](https://www.docker.com/) e Docker Compose (ou instância local do PostgreSQL)

### 1. Clonar o repositório

```bash
git clone https://github.com/JaxPe/TracLabs.git
cd TracLabs
```

### 2. Subir o banco de dados PostgreSQL

Para iniciar o container do PostgreSQL com as variáveis definidas em `.env.development`:

```bash
docker compose up -d
```

> **Nota:** Verifique a *connection string* `DefaultConnection` em `src/TracLabs.API/appsettings.Development.json` para garantir que a porta e as credenciais correspondem à instância do PostgreSQL em execução.

### 3. Aplicar as Migrations do Banco de Dados

```bash
dotnet ef database update --project src/TracLabs.Infrastructure --startup-project src/TracLabs.API
```

### 4. Compilar e Executar a API

```bash
dotnet build
dotnet run --project src/TracLabs.API
```

Com a aplicação em execução no ambiente de desenvolvimento (`Development`), acesse a documentação interativa do **Swagger UI** em:
- `http://localhost:5269/swagger` (ou na porta HTTPS configurada em `Properties/launchSettings.json`).

---

## Executando os Testes

A suíte de testes automatizados valida tanto as regras de negócio de domínio quanto a precisão matemática do motor de calibração gravimétrica (comparado com planilhas de referência para balão volumétrico de $2000\text{ mL}$, picnômetro de $50\text{ mL}$ e pipeta de $1\text{ mL}$).

Para executar todos os testes:

```bash
dotnet test
```

---

## Roadmap do Projeto

- [x] **Fase 1 — Fundação**: Camadas `Domain`, `Application` e `Infrastructure`, entidades metrológicas, Value Objects e mapeamentos EF Core.
- [x] **Fase 2 — API Core**: Autenticação JWT + ASP.NET Core Identity (`Tecnico` e `Supervisor`), Migrations iniciais, Swagger com suporte a Bearer Token e `ExceptionHandlingMiddleware`.
- [ ] **Fase 3 — Núcleo do Sistema (Em andamento)**:
  - [x] Motor de cálculo metrológico gravimétrico de volume (`VolumeCalibrationService` — ISO 4787 / DOC-CGCRE-027 / GUM)
  - [x] Controle de sequência de certificados (`CertificateSequence`)
  - [ ] Ciclo de status para padrões de referência (`StandardEquipment`: `PendingCalibration`, `InCalibration`, `Calibrated`, `OutOfService`)
  - [ ] Casos de uso CQRS (Commands/Queries com MediatR) e Endpoints de Calibração/Equipamentos
  - [ ] Emissão de certificados em PDF (QuestPDF conforme ABNT NBR ISO/IEC 17025)
  - [ ] Importação de planilhas Excel (EPPlus) e alertas de vencimento de padrões
- [ ] **Fase 4 — Frontend**: Interface web em Blazor WebAssembly integrada à API.
- [ ] **Fase 5 — Finalização**: Testes de integração, containerização completa e entrega final.

---

## Autor

Desenvolvido por **João Pedro Barroco Barbosa** — Niterói, RJ.
