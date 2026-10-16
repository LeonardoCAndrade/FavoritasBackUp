# FavoritasBackUp

Aplicativo Android desenvolvido em **.NET MAUI**, com o objetivo de realizar backup automático ou manual das fotos marcadas como favoritas na galeria do dispositivo para um armazenamento pessoal em nuvem.

O projeto é destinado inicialmente ao uso pessoal, sem previsão de publicação em lojas de aplicativos.

## Objetivo

O FavoritasBackUp busca simplificar o backup das fotos favoritas do celular, permitindo preservar essas imagens em um armazenamento externo configurado pelo próprio usuário.

O aplicativo deve identificar as imagens marcadas como favoritas na galeria nativa do Android e enviá-las para um servidor pessoal por meio de uma API HTTP.

O foco inicial é a compatibilidade com dispositivos **Samsung Galaxy**, utilizando a **Galeria Samsung** como origem das fotos favoritas.

O aplicativo não tem como objetivo substituir a galeria do celular ou oferecer funcionalidades de gerenciamento de imagens.

## Funcionamento previsto

O fluxo principal do aplicativo consiste em:

1. **Configuração inicial:** o usuário informa o endereço do serviço de armazenamento, seu usuário e sua senha.
2. **Identificação das favoritas:** o aplicativo consulta as imagens marcadas como favoritas na galeria do dispositivo.
3. **Verificação do backup:** o aplicativo identifica quais imagens ainda precisam ser enviadas.
4. **Sincronização:** as imagens pendentes são enviadas para o armazenamento pessoal.
5. **Acompanhamento:** o aplicativo informa o resultado e o estado da última sincronização.

Inicialmente, a sincronização será iniciada manualmente pelo usuário. A execução automática em segundo plano poderá ser implementada futuramente.

## Escopo do aplicativo

O FavoritasBackUp é exclusivamente uma ferramenta de backup.

O aplicativo deve acessar as imagens somente para identificar as favoritas e realizar sua transferência para o armazenamento configurado.

Não fazem parte do escopo:

- Exibição de fotos ou miniaturas.
- Visualização ou pré-visualização de imagens.
- Navegação entre fotos.
- Edição ou exclusão de imagens.
- Organização de álbuns.
- Seleção manual de fotos para backup.

A identificação das favoritas deve ocorrer automaticamente, respeitando as permissões e limitações do Android.

## Tecnologias

| Tecnologia | Utilização |
|---|---|
| .NET MAUI | Desenvolvimento do aplicativo Android |
| C# | Linguagem principal |
| XAML | Construção das interfaces |
| .NET MAUI Shell | Navegação entre páginas |
| SecureStorage | Armazenamento seguro das configurações de acesso |
| Android MediaStore | Consulta das imagens e identificação das favoritas |
| HTTP API | Comunicação futura com o armazenamento pessoal |
| SQLite | Persistência futura do histórico e controle de sincronização |

O projeto é desenvolvido utilizando o Visual Studio 2026.

## Funcionalidades

### Implementadas

**Configuração da nuvem**

- Cadastro do endereço do serviço de armazenamento.
- Cadastro de usuário e senha.
- Armazenamento das configurações utilizando SecureStorage.
- Recuperação e edição das configurações salvas.

**Inicialização inteligente**

- Verificação da existência de configurações ao iniciar o aplicativo.
- Abertura da tela de configuração no primeiro acesso.
- Abertura direta da tela de sincronização quando já existe configuração válida.
- Navegação entre as telas de configuração e sincronização.

**Interface principal**

- Tela de sincronização.
- Botão para iniciar o processo de sincronização.
- Acesso às configurações por meio do ícone de engrenagem.
- Área destinada à apresentação do status da sincronização.

### Em desenvolvimento

**Leitura das fotos favoritas**

- Solicitação das permissões de leitura de imagens.
- Consulta às imagens disponíveis por meio do Android MediaStore.
- Identificação das imagens marcadas como favoritas.
- Exibição da quantidade de imagens encontradas para validar a leitura.

Nesta etapa, nenhuma imagem é enviada para a nuvem.

### Planejadas

**Backup para a nuvem**

- Comunicação com o serviço HTTP configurado.
- Autenticação utilizando as credenciais armazenadas.
- Envio das imagens favoritas.
- Tratamento de falhas de comunicação.

**Controle de sincronização**

- Identificação de imagens já enviadas.
- Prevenção de backups duplicados.
- Persistência local do histórico de sincronização.
- Registro de resultados e falhas.

**Sincronização automática**

- Execução em segundo plano.
- Identificação periódica de novas imagens favoritas.
- Envio automático de imagens pendentes, respeitando as restrições do Android.

## Estrutura do aplicativo

O aplicativo possui duas telas principais:

**ConfigPage**

Responsável pela configuração e manutenção dos dados de acesso ao armazenamento pessoal.

**SyncPage**

Tela principal utilizada para iniciar o backup e acompanhar o estado das sincronizações.

As responsabilidades técnicas dos serviços, models e componentes específicos do Android estão documentadas em `docs/ARCHITECTURE.md`.

## Segurança e privacidade

O FavoritasBackUp foi concebido para trabalhar com imagens pessoais.

As credenciais de acesso são armazenadas utilizando o SecureStorage do .NET MAUI.

O acesso às fotos depende das permissões concedidas pelo usuário e deve ser limitado ao necessário para identificar e realizar o backup das imagens favoritas.

As imagens não devem ser exibidas na interface nem compartilhadas com serviços externos diferentes do armazenamento configurado pelo usuário.

A comunicação com o servidor deverá utilizar HTTPS em ambientes reais para proteger credenciais e dados durante a transferência.

## Limitações conhecidas

A identificação de fotos favoritas depende das informações disponibilizadas pelo Android MediaStore.

Embora o Android ofereça um indicador público para imagens favoritas, nem todos os aplicativos de galeria necessariamente utilizam esse indicador.

Por esse motivo, a compatibilidade com a Galeria Samsung precisa ser validada em um dispositivo real.

O acesso às imagens também está sujeito às permissões e restrições da versão do Android instalada.

## Documentação

- `AGENTS.md` — Instruções gerais para agentes de desenvolvimento.
- `docs/ARCHITECTURE.md` — Arquitetura e organização técnica do projeto.
- `docs/GIT_WORKFLOW.md` — Regras de versionamento e fluxo de trabalho com Git.

## Estado do projeto

O FavoritasBackUp está em desenvolvimento.

A configuração inicial, o armazenamento seguro das credenciais e a navegação inteligente já estão implementados.

A próxima etapa é validar a identificação das fotos favoritas na Galeria Samsung antes de iniciar o desenvolvimento do processo de backup para a nuvem.