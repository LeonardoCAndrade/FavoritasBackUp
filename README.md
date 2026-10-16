# FavoritasBackUp

Aplicativo Android desenvolvido em **.NET MAUI**, com o objetivo de realizar backup automático ou manual das fotos marcadas como favoritas na galeria do dispositivo para um armazenamento pessoal em nuvem.

O projeto é destinado inicialmente ao uso pessoal, sem previsão de publicação em lojas de aplicativos.

## Objetivo

O FavoritasBackUp busca simplificar o backup das fotos favoritas do celular, permitindo preservar essas imagens em um armazenamento externo configurado pelo próprio usuário.

O aplicativo deve identificar as imagens marcadas como favoritas na galeria nativa do Android e enviá-las para um servidor pessoal por meio do WebDAV do Nextcloud utilizando HTTPS.

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
| HttpClient / WebDAV | Backup manual para o Nextcloud por HTTPS |
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

**Leitura e backup manual das favoritas de hoje**

- Permissões de leitura completas ou parciais conforme a versão do Android.
- Consulta de favoritas pelo indicador público `IS_FAVORITE` do MediaStore (Android 11 ou superior).
- Filtro pela captura `DATE_TAKEN`, em milissegundos Unix, convertida para o fuso local do celular. A inclusão (`DATE_ADDED`) não é usada como captura; imagens sem captura válida ficam fora do backup.
- Envio sequencial por streaming para `FavoritasBackUp` na raiz WebDAV configurada, preservando os originais.
- HTTP Basic com senha de aplicativo do Nextcloud, somente HTTPS, sem seguir redirecionamentos.
- Criação por `MKCOL`; se a pasta já existe, confirmação de que o destino é uma coleção WebDAV.
- Nomes compostos pelo SHA-256 completo do conteúdo, sem extensão ou nome original. Duas leituras por streaming (hash e envio), sem cópia integral em memória. Conteúdos idênticos compartilham o destino.
- `PUT` com `If-None-Match: *`: HTTP 412 conta como já existente/ignorado, sem sobrescrita.
- Resultado com favoritas de hoje, enviadas, ignoradas e falhas; prevenção de execuções simultâneas.
- Última sincronização atualizada na tela somente quando há imagens e todas foram enviadas ou confirmadas como existentes, sem falhas. Esse horário não é persistido entre reinicializações.
- Nenhuma foto ou miniatura é exibida.

### Em desenvolvimento

- Validação em dispositivo Samsung real e servidor Nextcloud.

### Planejadas

**Histórico de sincronização**

- Persistência local do histórico e registro de resultados.
- Controle de sincronização além da condição de existência no servidor.

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

A comunicação exige HTTPS para proteger credenciais e dados. Redirecionamentos são recusados; configure diretamente a URL WebDAV final e utilize um certificado confiável no celular.

## Limitações conhecidas

A identificação de fotos favoritas depende das informações disponibilizadas pelo Android MediaStore.

Embora o Android ofereça um indicador público para imagens favoritas, nem todos os aplicativos de galeria necessariamente utilizam esse indicador.

Por esse motivo, a compatibilidade com a Galeria Samsung precisa ser validada em um dispositivo real.

O acesso às imagens também está sujeito às permissões e restrições da versão do Android instalada.

## Configuração do Nextcloud

Na ConfigPage, informe a URL WebDAV completa da raiz de arquivos do usuário, por exemplo `https://nuvem.exemplo/remote.php/dav/files/usuario/`, o usuário e uma **senha de aplicativo** gerada no Nextcloud. Instalações em subdiretórios devem incluir esse prefixo na URL.

Sincronizar consulta as favoritas capturadas no dia local no início da operação. Permissões parciais limitam os resultados às imagens autorizadas. Em caso de interrupção, os arquivos já enviados permanecem no servidor; uma nova tentativa ignora os existentes.

## Validação local

- Android: `dotnet build FavoritasBackUp.csproj -f net10.0-android --no-restore -v minimal`.
- Verificações automatizadas sem dependências adicionais: `dotnet run --project tests/WebDavChecks/WebDavChecks.csproj`.
- Os testes usam transporte HTTP e biblioteca simulados; não substituem validação das permissões/MediaStore em Android nem integração com um Nextcloud real.

## Documentação

- `AGENTS.md` — Instruções gerais para agentes de desenvolvimento.
- `docs/ARCHITECTURE.md` — Arquitetura e organização técnica do projeto.
- `docs/GIT_WORKFLOW.md` — Regras de versionamento e fluxo de trabalho com Git.

## Estado do projeto

O FavoritasBackUp está em desenvolvimento.

A configuração inicial, o armazenamento seguro das credenciais e a navegação inteligente já estão implementados.

O primeiro backup manual das favoritas capturadas hoje para o Nextcloud está implementado. A compatibilidade com a Galeria Samsung e o envio para um servidor real ainda precisam de validação em dispositivo.