
# Fluxo de Trabalho Git & Pull Request

Sempre que eu solicitar a implementação de uma funcionalidade, bugfix ou refatoração:

1. **Nova Branch**: Crie uma branch descritiva antes de começar:
   `git checkout -b feature/<nome-da-tarefa>` ou `fix/<nome-da-tarefa>`.
2. **Implementação & Testes**: Faça as alterações necessárias e execute os testes/linters locais para garantir que tudo passa.
3. **Build & Test**: Antes de commitar, execute sempre `dotnet build` e `dotnet test` para validar a compilação.
4. **Commit**: Crie commits seguindo o padrão Conventional Commits (ex: `feat: ...`, `fix: ...`).
5. **Abrir Pull Request**:
   - Faça o push da branch: `git push -u origin HEAD`
   - **Draft PR (Opcional)**: Se a tarefa for complexa ou experimental, crie o PR com a flag `--draft`.
   - Abra o PR usando o GitHub CLI:
     `gh pr create --title "<título claro>" --body "<resumo das alterações, testes executados e checklist>"`
6. **Aguardar**: Encerre sua resposta me enviando o link do PR criado para que eu possa revisar. Não faça merge direto na branch principal (`main`/`master`).
