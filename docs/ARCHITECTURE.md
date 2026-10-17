# Arquitetura — FavoritasBackUp

## 1. Visão geral

O FavoritasBackUp é um aplicativo Android desenvolvido em .NET MAUI, utilizando C# e XAML.

Sua finalidade é identificar fotos marcadas como favoritas na galeria do dispositivo e realizar o backup dessas imagens para um armazenamento pessoal em nuvem.

O aplicativo não possui funcionalidades de visualização, edição, seleção manual ou gerenciamento de fotos.

A arquitetura deve priorizar simplicidade, separação de responsabilidades, segurança e facilidade de manutenção.

## 2. Tecnologias

| Tecnologia | Responsabilidade |
|---|---|
| .NET MAUI | Plataforma de desenvolvimento |
| C# | Lógica da aplicação |
| XAML | Interface do usuário |
| MAUI Shell | Navegação entre páginas |
| Injeção de dependência | Gerenciamento de serviços e dependências |
| SecureStorage | Armazenamento seguro das configurações |
| Android MediaStore | Consulta das imagens do dispositivo |
| HttpClient | Comunicação WebDAV com o Nextcloud |
| SQLite | Persistência futura do controle de sincronização |

O Android é a plataforma principal do projeto.

## 3. Organização do projeto

A estrutura deve seguir a organização abaixo, podendo ser expandida conforme a necessidade:

```text
FavoritasBackUp/
├── Models/
│   ├── CloudConfiguration.cs
│   └── PhotoInfo.cs
│
├── Services/
│   ├── CloudConfigurationService.cs
│   └── IPhotoLibraryService.cs
│
├── Platforms/
│   └── Android/
│       ├── Services/
│       │   └── AndroidPhotoLibraryService.cs
│       └── AndroidManifest.xml
│
├── ConfigPage.xaml
├── ConfigPage.xaml.cs
├── SyncPage.xaml
├── SyncPage.xaml.cs
├── AppShell.xaml
├── AppShell.xaml.cs
└── MauiProgram.cs
```

Essa estrutura representa a organização de referência. Arquivos relacionados a funcionalidades ainda não implementadas podem não existir.

Novos diretórios devem ser criados somente quando houver necessidade real.

## 4. Responsabilidades das camadas

### Pages

As páginas são responsáveis pela apresentação e interação com o usuário.

Responsabilidades:

- Exibir informações e estados da aplicação.
- Receber ações do usuário.
- Executar validações simples de interface.
- Invocar serviços responsáveis pelas operações.
- Apresentar resultados e mensagens de erro.

As páginas não devem implementar diretamente consultas ao MediaStore, persistência de credenciais ou comunicação HTTP.

A lógica de negócio deve permanecer separada da interface.

### Models

Representam os dados utilizados pela aplicação.

Devem conter propriedades e estruturas necessárias para a transferência de informações entre componentes.

Evitar dependências de interface, navegação ou APIs específicas do Android.

Models não devem executar consultas, acessar armazenamento ou realizar chamadas HTTP.

### Services

Concentram as operações e regras da aplicação.

Responsabilidades:

- Persistência e recuperação de configurações.
- Consulta e identificação das fotos favoritas.
- Comunicação WebDAV com o Nextcloud por `INextcloudBackupService` e `NextcloudBackupService`.
- Futuramente, controle das imagens sincronizadas.

Serviços devem possuir responsabilidades bem definidas e evitar dependência direta das páginas.

Utilizar interfaces quando houver benefício para desacoplamento, testes ou isolamento de código específico de plataforma.

### Platforms/Android

Contém implementações que dependem diretamente das APIs do Android.

Exemplos:

- Acesso ao MediaStore.
- Consulta aos metadados das imagens.
- Integração com permissões específicas do Android.

Essas implementações não devem conter lógica de apresentação.

Sempre que possível, o acesso a recursos específicos da plataforma deve ocorrer por meio de interfaces definidas fora de `Platforms`.

## 5. Injeção de dependência

O registro das dependências deve ser centralizado em `MauiProgram.cs`.

Utilizar o contêiner de dependências nativo do .NET MAUI.

Preferir injeção por construtor.

Evitar instanciar serviços diretamente nas páginas quando eles puderem ser fornecidos pelo contêiner.

Não duplicar registros ou criar múltiplas instâncias desnecessárias de serviços que gerenciam o mesmo estado.

O tempo de vida de cada serviço deve ser escolhido conforme sua responsabilidade.

## 6. Navegação

A navegação é gerenciada pelo MAUI Shell.

O aplicativo possui duas páginas principais:

**ConfigPage**

Responsável pela configuração e manutenção das credenciais de acesso à nuvem.

**SyncPage**

Responsável por iniciar operações de leitura e sincronização e apresentar seus resultados.

### Inicialização inteligente

Ao iniciar o aplicativo:

1. Verificar a existência de uma configuração completa no SecureStorage.
2. Se não existir configuração, abrir `ConfigPage`.
3. Se existir configuração, abrir `SyncPage`.

A verificação deve ocorrer sem bloquear a interface ou apresentar páginas incorretas durante o carregamento.

### Regras de navegação

- Utilizar rotas nomeadas do Shell.
- Preferir `nameof()` para referências a páginas.
- Utilizar navegação absoluta entre os destinos principais quando apropriado.
- Evitar páginas duplicadas na pilha de navegação.
- Não implementar sistemas paralelos de navegação sem necessidade.

## 7. Configurações e credenciais

As configurações da nuvem são representadas por `CloudConfiguration`.

O serviço `CloudConfigurationService` é responsável por salvar, recuperar e verificar a existência dessas configurações.

As informações sensíveis devem ser armazenadas utilizando o SecureStorage do .NET MAUI.

Regras:

- Não armazenar senhas em texto puro em arquivos locais.
- Não utilizar SQLite para persistir credenciais.
- Não incluir credenciais no código-fonte.
- Não registrar senhas ou tokens em logs.
- Tratar falhas de leitura e gravação do armazenamento seguro.
- Não apagar configurações automaticamente em caso de erro.

A validação das configurações deve ser realizada antes de persistir os dados.

## 8. Acesso às fotos favoritas

O acesso às imagens deve utilizar as APIs públicas do Android.

### MediaStore

O MediaStore é a fonte principal para consultar imagens armazenadas no dispositivo.

A identificação das favoritas deve utilizar o indicador público `IS_FAVORITE`, quando disponível.

A compatibilidade desse indicador com a Galeria Samsung deve ser validada em dispositivo real.

Não presumir que todas as galerias armazenam o estado de favorita da mesma forma.

### PhotoInfo

O model `PhotoInfo` representa os metadados necessários para identificar uma imagem.

Pode incluir:

- Identificador da mídia.
- Nome do arquivo.
- URI de conteúdo.
- Indicador de favorita.
- Data de inclusão (`DateAddedUtc`) e captura (`DateTakenUtc`), separadas. A captura usa `DATE_TAKEN` do MediaStore em milissegundos Unix, sem substituição pela inclusão.

O model não deve armazenar o conteúdo binário da imagem.

### IPhotoLibraryService

Define a consulta das imagens e abertura de streams de leitura por URI, sem dependência direta do Android. O chamador deve liberar os streams retornados.

### AndroidPhotoLibraryService

Implementa o contrato utilizando MediaStore e ContentResolver.

Responsabilidades:

- Consultar imagens acessíveis.
- Identificar as favoritas disponíveis.
- Retornar metadados necessários ao backup.
- Tratar erros e limitações de acesso.
- Liberar corretamente os recursos nativos.

A leitura deve respeitar as permissões concedidas pelo usuário.

### Restrições

- Não utilizar APIs privadas da Samsung.
- Não acessar bancos de dados internos da galeria.
- Não exigir root.
- Não utilizar caminhos físicos como principal mecanismo de acesso.
- Não carregar imagens completas na memória durante a consulta de metadados.
- Não exibir fotos, miniaturas ou pré-visualizações.

## 9. Permissões Android

As permissões devem ser solicitadas conforme a versão do Android.

Considerar:

- `READ_EXTERNAL_STORAGE` nas versões aplicáveis anteriores ao Android 13.
- `READ_MEDIA_IMAGES` no Android 13 ou superior.
- Acesso parcial às imagens no Android 14 ou superior.

Regras:

- Solicitar somente as permissões necessárias.
- Tratar permissões negadas.
- Diferenciar acesso completo de acesso parcial.
- Não presumir acesso a toda a galeria quando a permissão for limitada.
- Não utilizar `MANAGE_EXTERNAL_STORAGE`.

O aplicativo deve continuar funcional mesmo quando o usuário não conceder acesso às imagens, apresentando uma mensagem adequada.

## 10. Backup manual para o Nextcloud

`NextcloudBackupService` implementa `INextcloudBackupService` e recebe `HttpClient`, `CloudConfigurationService` e `IPhotoLibraryService` por injeção de dependência. `BackupResult` contém total de favoritas de hoje, enviadas, ignoradas, falhas e mensagem segura de erro.

Fluxo:

1. Capturar o dia/fuso local no início e consultar as favoritas pelo serviço existente.
2. Selecionar favoritas cuja captura UTC convertida para o fuso local corresponda ao dia. Capturas ausentes, zero ou inválidas não são substituídas pela inclusão.
3. Recuperar configurações do SecureStorage. Exigir URL WebDAV HTTPS sem credenciais embutidas, query ou fragmento. Autenticar com HTTP Basic e senha de aplicativo.
4. Criar `FavoritasBackUp/` na raiz WebDAV por `MKCOL`. HTTP 405 requer `PROPFIND` com `Depth: 0` para confirmar uma coleção existente; um arquivo ocupando esse caminho é falha.
5. Calcular SHA-256 do conteúdo por streaming e usar o hash completo como nome seguro e estável, sem extensão. Abrir novamente a URI para enviar sequencialmente por streaming usando `PUT` e `If-None-Match: *`. HTTP 412 conta como ignorado; conteúdos idênticos compartilham o destino.
6. Contabilizar erros por imagem, mantendo os envios anteriores. Falhas na configuração/pasta contabilizam todas as candidatas como falhas. Uma queda de conexão após envio pode deixar o arquivo no servidor; a próxima tentativa o reconhecerá pela condição HTTP.

O `HttpClient` singleton é registrado no `MauiProgram.cs` com `Xamarin.Android.Net.AndroidMessageHandler`, utilizando o transporte nativo e a configuração de segurança de rede do Android, sem redirecionamentos nem cookies e limite de dez minutos por requisição. Credenciais são colocadas em cada requisição, sem cabeçalho global compartilhado. A validação TLS permanece ativa.

Na infraestrutura atual, o Nextcloud está em Docker Desktop no Windows e é acessado por `https://192.168.15.7:8443`, inicialmente apenas na rede doméstica. O Caddy roda em um contêiner Docker separado, termina o TLS na porta 8443 e encaminha as requisições ao Nextcloud por HTTP na porta interna 8080, com a seguinte configuração:

```caddyfile
https://192.168.15.7:8443 {
    tls internal
    reverse_proxy host.docker.internal:8080
}
```

O endpoint WebDAV é `https://192.168.15.7:8443/remote.php/dav/files/USUARIO/`, em que `USUARIO` representa o usuário configurado no Nextcloud. O IP local atual é `192.168.15.7`; a URL permanece configurável pelo usuário.

O certificado TLS é emitido pela autoridade certificadora local do Caddy (`tls internal`), não por uma autoridade certificadora pública. Essa autoridade precisa ser confiável nos dispositivos clientes, sem desabilitar a validação TLS. Conforme informado pelo usuário, a CA raiz já está instalada no Android e o navegador do Galaxy S26 acessa o Nextcloud corretamente por HTTPS. O envio de imagens pelo aplicativo via HTTPS ainda não foi validado. Futuramente, poderá ser implementado um certificado público ou outra solução de acesso remoto seguro.

O serviço bloqueia execuções simultâneas; a SyncPage também desabilita o botão e mostra “Enviando imagens...”. Sair da página cancela a operação e libera os streams. A interface apresenta o resultado e indica acesso parcial. O horário de última sincronização é atualizado somente para um conjunto não vazio sem falhas (envios ou arquivos existentes), permanecendo em memória na página. Nenhum original é alterado ou excluído.

Não há SQLite ou sincronização em segundo plano nesse fluxo. A referência preexistente a `sqlite-net-pcl` no projeto ainda não é utilizada.

## 11. Persistência local — Planejada

O SQLite poderá ser utilizado para controlar o histórico de sincronização.

Informações previstas:

- Identificação da imagem.
- Estado do backup.
- Data da última tentativa.
- Data do último envio bem-sucedido.
- Informações necessárias para evitar duplicidade.

O banco de dados não deve armazenar senhas ou cópias das imagens.

A estrutura definitiva das tabelas será definida durante a implementação dessa funcionalidade.

## 12. Tratamento de erros

Os serviços devem tratar erros de forma apropriada à sua responsabilidade.

Diretrizes:

- Não ocultar falhas que impeçam a execução da funcionalidade.
- Apresentar mensagens compreensíveis ao usuário.
- Evitar exposição de detalhes técnicos ou sensíveis na interface.
- Registrar informações úteis para diagnóstico, sem incluir credenciais.
- Garantir a liberação de recursos em caso de exceção.
- Não apresentar operações malsucedidas como concluídas.

## 13. Evolução da arquitetura

A arquitetura deve evoluir conforme as necessidades reais do projeto.

Diretrizes:

- Evitar abstrações prematuras.
- Não introduzir padrões complexos sem benefício concreto.
- Priorizar legibilidade e manutenção.
- Preservar a separação entre interface, regras da aplicação e integrações externas.
- Reutilizar componentes existentes.
- Manter a documentação sincronizada com alterações arquiteturais relevantes.

Não é obrigatório utilizar MVVM, Clean Architecture ou outros padrões adicionais enquanto a complexidade do projeto não justificar sua adoção.
## HTTPS no Android para o teste local

O aplicativo usa `Xamarin.Android.Net.AndroidMessageHandler` com o gerenciador de confiança e a verificação de hostname padrão do Android. O manifesto referencia `Platforms/Android/Resources/xml/network_security_config.xml`, que preserva as CAs do sistema e proíbe HTTP sem criptografia.

A seção `debug-overrides` adiciona confiança nas CAs instaladas pelo usuário somente quando `android:debuggable=true` (build Debug para desenvolvimento/teste). Essa confiança abrange todas as CAs do usuário: não há uma cópia verificada do certificado raiz específico do Caddy no repositório para incorporá-lo como recurso. Release mantém somente as CAs do sistema. Não há callback que aceite certificados inválidos. O certificado do servidor precisa conter o endereço configurado no SAN. As outras plataformas não são alteradas.

O tratamento de erros diferencia TLS/HTTPS, conexão/tempo limite, HTTP 401 (credenciais), 403 (permissões) e 404 (destino WebDAV). Falhas de leitura/envio continuam contabilizadas por imagem sem declarar sucesso.

### Primeiro teste no Samsung Galaxy S26

1. Instalar/executar em **Debug**, na mesma rede doméstica do servidor, com a CA raiz do Caddy instalada no Android.
2. Informar na ConfigPage `https://192.168.15.7:8443/remote.php/dav/files/USUARIO/`, o usuário correspondente e uma senha de aplicativo do Nextcloud.
3. Conceder acesso às imagens; acesso parcial limita os resultados às imagens autorizadas.
4. Marcar uma foto capturada hoje como favorita. Manter uma foto de outro dia favorita e uma de hoje não favorita para conferir que ficam de fora. Tocar em **Sincronizar**.
5. Conferir os contadores e os arquivos em `FavoritasBackUp` no Nextcloud (nomes SHA-256 sem extensão). Repetir para confirmar que arquivos existentes são ignorados sem sobrescrita.

A configuração está implementada; a validação em dispositivo e Nextcloud reais continua em desenvolvimento. Os testes simulados não comprovam a confiança TLS no celular nem a integração da Galeria Samsung com `IS_FAVORITE`.