# Git Workflow

## 1. Branches

- Trabalhe sempre na branch atualmente ativa.
- Não crie, troque, renomeie ou exclua branches sem solicitação explícita.
- Não realize operações de merge ou rebase sem autorização.
- Preserve alterações locais existentes.
- Antes de modificar arquivos, verifique o estado do repositório utilizando `git status`.
- Caso existam alterações pendentes, preserve-as e evite sobrescrevê-las. Se houver risco de conflito, solicite orientação.

A organização e a identificação das alterações devem ocorrer preferencialmente por meio de Pull Requests, sem exigir uma branch específica para cada funcionalidade.

## 2. Commits

Não crie commits automaticamente. Somente realize commits quando solicitado explicitamente.

Utilize o padrão **Conventional Commits** para as mensagens:

`tipo: descrição curta`

Tipos recomendados:

| Tipo | Utilização |
|---|---|
| `feat` | Nova funcionalidade |
| `fix` | Correção de defeito |
| `refactor` | Refatoração sem alteração funcional |
| `docs` | Alterações na documentação |
| `chore` | Manutenção e configuração do projeto |
| `test` | Inclusão ou alteração de testes |
| `perf` | Melhorias de desempenho |

Exemplos:

- `feat: implementar leitura de fotos favoritas`
- `fix: corrigir navegação inicial`
- `refactor: simplificar serviço de configuração`
- `docs: atualizar arquitetura do projeto`

As mensagens devem ser escritas em português, de forma objetiva e no infinitivo.

Antes de criar um commit, revise os arquivos alterados e confirme que não existem credenciais, arquivos temporários ou alterações fora do escopo.

## 3. Pull Requests

- Não crie Pull Requests automaticamente.
- Não realize merge de Pull Requests sem autorização explícita.
- Utilize o PR para documentar o objetivo e as alterações realizadas.
- Mantenha cada PR focado em uma funcionalidade, correção ou conjunto de alterações relacionadas.
- Evite incluir refatorações não relacionadas à tarefa.

Quando solicitado, forneça uma descrição de PR seguindo o modelo:

### Resumo

Descrição objetiva do que foi implementado ou corrigido.

### Alterações

Principais modificações realizadas.

### Validação

Testes executados e resultados obtidos.

### Observações

Limitações, riscos ou pendências, quando existirem.

Não declare testes como executados sem que tenham sido efetivamente realizados.

## 4. Operações Git

Operações de leitura são permitidas, incluindo:

- `git status`
- `git diff`
- `git log`
- `git branch --show-current`

Operações que modificam o repositório ou seu histórico exigem autorização explícita, incluindo:

- `git checkout` ou `git switch` para outra branch.
- `git branch` para criação, renomeação ou exclusão.
- `git commit`.
- `git push`.
- `git merge`.
- `git rebase`.
- `git reset`.
- `git stash`.
- `git cherry-pick`.

Não utilize comandos destrutivos, como `git reset --hard`, `git clean -fd` ou push forçado, sem autorização explícita e específica.

## 5. Conclusão das tarefas

Ao finalizar uma implementação:

1. Informe os arquivos criados e modificados.
2. Resuma as alterações realizadas.
3. Informe os testes e validações executados.
4. Destaque eventuais pendências.
5. Aguarde instruções antes de realizar commit, push ou criar um Pull Request.