# AGENTS.md

## Documentação

Antes de implementar alterações, consulte a documentação relevante:

- `README.md` — Contexto, objetivos e escopo do projeto.
- `docs/ARCHITECTURE.md` — Arquitetura e padrões técnicos.
- `docs/GIT_WORKFLOW.md` — Regras de Git, commits e Pull Requests.

Em caso de divergência entre documentação e implementação, sinalize o problema.

## Desenvolvimento

- Implemente somente o que foi solicitado.
- Respeite a arquitetura e os padrões existentes.
- Evite alterações fora do escopo.
- Priorize soluções simples e reutilize componentes existentes.
- Não adicione dependências sem necessidade justificada.
- Preserve o funcionamento das funcionalidades existentes.
- Não antecipe funcionalidades futuras.
- Mantenha o `README.md` atualizado sempre que uma alteração impactar funcionalidades, escopo, tecnologias ou o estado de desenvolvimento do projeto.
- Ao concluir uma funcionalidade, atualize sua classificação no `README.md` entre implementada, em desenvolvimento ou planejada, quando aplicável.
- Evite alterações no `README.md` quando a mudança não afetar as informações documentadas.

## Git

- Trabalhe na branch atual.
- Não crie, troque, renomeie ou exclua branches sem autorização explícita.
- Não realize commits, pushes ou Pull Requests sem autorização explícita.
- Não descarte alterações existentes.
- Consulte `docs/GIT_WORKFLOW.md` para as demais regras.

## Segurança

- Não inclua credenciais ou tokens no código-fonte.
- Utilize armazenamento seguro para dados sensíveis.
- Não exponha dados sensíveis em logs.
- Solicite somente as permissões Android necessárias.
- Não utilize APIs privadas para contornar restrições do Android.

## Validação

- Compile e execute os testes aplicáveis quando o ambiente permitir.
- Corrija erros introduzidos pelas alterações.
- Considere cenários de falha e permissões negadas.
- Não declare testes como executados sem realmente executá-los.

## Entrega

Ao concluir, informe objetivamente:

- Alterações realizadas.
- Arquivos criados ou modificados.
- Testes e validações executados.
- Pendências ou limitações identificadas.