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
- Botão **Testar conexão**: GET autenticado ao endpoint OCS de usuário, sem acessar fotos, criar pastas ou enviar arquivos. Valida HTTP, `ocs.meta` e a identificação do usuário autenticado. Exibe carregamento e mensagens específicas para falhas de autenticação, conexão, certificado e tempo limite.

**Leitura e backup manual das favoritas de hoje**

- Permissões de leitura completas ou parciais conforme a versão do Android.
- Consulta de favoritas pelo indicador público `IS_FAVORITE` do MediaStore (Android 11 ou superior).
- Filtro pela captura `DATE_TAKEN`, em milissegundos Unix, convertida para o fuso local do celular. A inclusão (`DATE_ADDED`) não é usada como captura; imagens sem captura válida ficam fora do backup.
- Envio sequencial por streaming via PUT diretamente à pasta WebDAV configurada, que deve ser criada manualmente no Nextcloud, preservando os originais.
- HTTP Basic com senha de aplicativo do Nextcloud, somente HTTPS, sem seguir redirecionamentos.
- Nenhuma criação ou verificação remota de diretórios. HTTP 404/409 orienta criar a pasta manualmente e conferir a URL.
- Nomes originais do MediaStore (`DISPLAY_NAME`), incluindo extensão, codificados como segmento da URL. Envio por streaming sem cópia integral em memória; o identificador interno permanece inalterado.
- `PUT` com `If-None-Match: *`: HTTP 412 conta como já existente/ignorado, sem sobrescrita. Fotos diferentes com o mesmo nome também são ignoradas, sem comparação do conteúdo.
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

O servidor Nextcloud está em Docker Desktop no Windows, com acesso inicialmente restrito à rede doméstica. O endereço HTTPS é `https://192.168.15.7:8443`. O IP local atual é `192.168.15.7`; a URL permanece configurável pelo usuário.

O Caddy, em um contêiner Docker separado, realiza a terminação TLS na porta HTTPS 8443 e encaminha as requisições ao Nextcloud pela porta HTTP interna 8080:

```caddyfile
https://192.168.15.7:8443 {
    tls internal
    reverse_proxy host.docker.internal:8080
}
```

O certificado TLS é emitido pela autoridade certificadora local do Caddy (`tls internal`), não por uma autoridade certificadora pública. Essa autoridade local precisa ser confiável nos dispositivos clientes, mantendo a validação de certificados TLS ativa.

Na ConfigPage, informe a URL WebDAV completa `https://192.168.15.7:8443/remote.php/dav/files/USUARIO/FavoritasBackUp/`, substituindo `USUARIO` pelo usuário configurado no Nextcloud, o usuário e uma **senha de aplicativo** gerada no Nextcloud. Crie a pasta FavoritasBackUp manualmente no Nextcloud antes de sincronizar. A URL salva é o diretório final de envio: nenhuma pasta adicional será acrescentada. Configurações antigas que apontam à raiz do usuário devem ser editadas para incluir a pasta desejada. Instalações em subdiretórios devem incluir esse prefixo na URL.

Conforme informado pelo usuário, a CA raiz já está instalada no Android e o navegador do Galaxy S26 acessa o Nextcloud corretamente por HTTPS. O envio de imagens pelo aplicativo via HTTPS ainda não foi validado. Futuramente, poderá ser implementado um certificado público ou outra solução de acesso remoto seguro.

Sincronizar consulta as favoritas capturadas no dia local no início da operação. Permissões parciais limitam os resultados às imagens autorizadas. Em caso de interrupção, os arquivos já enviados permanecem no servidor; uma nova tentativa ignora os existentes.

## Validação local

- Android: `dotnet build FavoritasBackUp.csproj -f net10.0-android --no-restore -v minimal`.
- Verificações automatizadas sem dependências adicionais: `dotnet run --project tests/WebDavChecks/WebDavChecks.csproj`.
- Somente o teste OCS: `dotnet run --project tests/WebDavChecks/WebDavChecks.csproj -- --connection-only`.
- Os testes usam transporte HTTP e biblioteca simulados; não substituem validação das permissões/MediaStore em Android nem integração com um Nextcloud real.

## Documentação

- `AGENTS.md` — Instruções gerais para agentes de desenvolvimento.
- `docs/ARCHITECTURE.md` — Arquitetura e organização técnica do projeto.
- `docs/GIT_WORKFLOW.md` — Regras de versionamento e fluxo de trabalho com Git.

## Estado do projeto

O FavoritasBackUp está em desenvolvimento.

A configuração inicial, o armazenamento seguro das credenciais e a navegação inteligente já estão implementados.

O primeiro backup manual das favoritas capturadas hoje para o Nextcloud está implementado. A compatibilidade com a Galeria Samsung e o envio para um servidor real ainda precisam de validação em dispositivo.
## HTTPS no Android para o teste local

O handler HTTPS validado no dispositivo foi preservado. A sincronização usa exclusivamente PUT, sem MKCOL ou PROPFIND; **Testar conexão** utiliza somente `GET` e não comprova o funcionamento do backup.

Para testar a conexão no Galaxy S26, execute em **Debug**, na mesma rede do Caddy/Nextcloud e com a CA raiz local instalada. Salve a URL WebDAV, usuário e senha de aplicativo nas configurações e toque em **Testar conexão** na tela de sincronização. Para a URL atual, a requisição será `GET https://192.168.15.7:8443/ocs/v2.php/cloud/user?format=json`. Não são necessárias permissões de fotos. O sucesso exige HTTP 2xx, `ocs.meta.status=ok`, `statuscode=200` e `ocs.data.id` preenchido. Falhas mostram mensagens compreensíveis; o diagnóstico completo com stack traces e credenciais ocultadas aparece na saída Debug do Visual Studio. O teste tem limite de 30 segundos e é cancelado ao sair da tela. Conforme informado pelo usuário, o teste de conexão foi validado com sucesso no Samsung Galaxy S26 com HTTPS e autenticação no Nextcloud. As melhorias posteriores precisam de nova conferência visual no dispositivo.

O aplicativo usa `Xamarin.Android.Net.AndroidMessageHandler` com o gerenciador de confiança e a verificação de hostname padrão do Android. O manifesto referencia `Platforms/Android/Resources/xml/network_security_config.xml`, que preserva as CAs do sistema e proíbe HTTP sem criptografia.

A seção `debug-overrides` adiciona confiança nas CAs instaladas pelo usuário somente quando `android:debuggable=true` (build Debug para desenvolvimento/teste). Essa confiança abrange todas as CAs do usuário: não há uma cópia verificada do certificado raiz específico do Caddy no repositório para incorporá-lo como recurso. Release mantém somente as CAs do sistema. Não há callback que aceite certificados inválidos. O certificado do servidor precisa conter o endereço configurado no SAN. As outras plataformas não são alteradas.

O tratamento de erros diferencia TLS/HTTPS, conexão/tempo limite, HTTP 401 (credenciais), 403 (permissões) e 404 (destino WebDAV). Falhas de leitura/envio continuam contabilizadas por imagem sem declarar sucesso.

### Primeiro teste no Samsung Galaxy S26

1. Instalar/executar em **Debug**, na mesma rede doméstica do servidor, com a CA raiz do Caddy instalada no Android.
2. Informar na ConfigPage `https://192.168.15.7:8443/remote.php/dav/files/USUARIO/FavoritasBackUp/`, o usuário correspondente e uma senha de aplicativo do Nextcloud.
3. Conceder acesso às imagens; acesso parcial limita os resultados às imagens autorizadas.
4. Marcar uma foto capturada hoje como favorita. Manter uma foto de outro dia favorita e uma de hoje não favorita para conferir que ficam de fora. Tocar em **Sincronizar**.
5. Conferir os contadores e os arquivos em `FavoritasBackUp` no Nextcloud (nomes originais com extensão). Repetir para confirmar que arquivos existentes são ignorados sem sobrescrita.

A configuração está implementada; a validação em dispositivo e Nextcloud reais continua em desenvolvimento. Os testes simulados não comprovam a confiança TLS no celular nem a integração da Galeria Samsung com `IS_FAVORITE`.
