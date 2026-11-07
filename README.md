# HelpDesk Flow

Central web de atendimento em português. A interface Angular 21 consome uma API ASP.NET Core 10 com Entity Framework Core e PostgreSQL 17. Em modo local, a API também serve o build do Angular na mesma origem.

## Rodar no Windows

Com **Docker Desktop** iniciado, **PowerShell 7.4+**, **.NET SDK 10** e **Node.js 24** instalados, rode na raiz do repositório:

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File .\scripts\start-local.ps1
```

O script gera credenciais locais em `.env.helpdesk`, inicia o PostgreSQL no Docker, instala as dependências do Angular, compila a interface e inicia a API. As migrações e a carga de demonstração são aplicadas automaticamente em desenvolvimento.

- Aplicação em [localhost:5031](http://localhost:5031)
- API interativa em [localhost:5031/swagger](http://localhost:5031/swagger)
- PostgreSQL em `127.0.0.1:55432`, com armazenamento persistente no volume `helpdesk-flow-local_pgdata`

### Contas de demonstração

| E-mail | Perfil |
| --- | --- |
| `usuario@helpdesk.test` | Solicitante |
| `tecnico@helpdesk.test` | Técnico |
| `admin@helpdesk.test` | Administrador |

As três contas usam a senha do campo `HELPDESK_DEMO_PASSWORD` no arquivo local `.env.helpdesk`. Guarde esse arquivo junto ao volume local para conservar o acesso entre execuções.

Comece com o solicitante, abra um chamado e anote o código exibido na fila. Entre como técnico, localize o chamado pelo código e clique em **Pegar chamado**. Mude a situação para **Em andamento**, **Resolvido** e **Fechado**. Apenas o administrador pode atribuir ou reatribuir chamados para outra pessoa e alterar os perfis na tela **Usuários**.

Encerre a API com **Ctrl+C**. Para parar o PostgreSQL e manter os dados:

```powershell
docker compose --env-file .env.helpdesk stop
```

## Desenvolvimento

Para editar o frontend com recarga automática, inicie a API com `-ApiOnly` e rode o Angular em outro terminal. O proxy encaminha `/api` para a porta 5031.

```powershell
pwsh -NoProfile -File .\scripts\start-local.ps1 -ApiOnly
```

```powershell
npm.cmd --prefix frontend ci
npm.cmd --prefix frontend start
```

O Angular estará disponível em [localhost:4200](http://localhost:4200).

A API lê `ConnectionStrings__DefaultConnection` e `Jwt__Key` (mínimo de 32 bytes) de variáveis de ambiente ou User Secrets. `Jwt__Issuer`, `Jwt__Audience` e `Jwt__ExpirationMinutes` têm valores padrão. Para clientes em outra origem, configure `AllowedOrigins__0` com a origem autorizada. Swagger e a carga de demonstração são recursos de desenvolvimento.

O solicitante enxerga os próprios chamados. Técnicos e administradores têm acesso à fila completa. O técnico pode editar a prioridade de chamados de terceiros, e o solicitante e o administrador podem editar o assunto e a descrição. Chamados fechados ou cancelados ficam disponíveis para consulta. Um chamado resolvido pode voltar para **Em andamento**.

## Testar a API

Com a instância local rodando:

```powershell
pwsh -NoProfile -File .\scripts\test-api.ps1
```

O teste cria uma conta e um chamado identificados pelo prefixo `teste-` na base utilizada. Exercita login, cadastro, autorização, edição parcial, atribuição, busca, comentários, resolução, reabertura, fechamento e mudança de perfil. O arquivo `HelpDesk-Flow.http` também tem exemplos de requisições manuais.
