param([uri]$BaseUrl = 'http://localhost:5031')

$ErrorActionPreference = 'Stop'
if (-not $BaseUrl.IsLoopback) { throw 'Execute o teste apenas contra uma instância local.' }
$root = Join-Path $PSScriptRoot '..'
$config = ConvertFrom-StringData (Get-Content -LiteralPath (Join-Path $root '.env.helpdesk') -Raw)
$demoPassword = $config.HELPDESK_DEMO_PASSWORD
$prefix = 'teste-' + [Guid]::NewGuid().ToString('N').Substring(0, 8)

function Assert-That($Condition, $Message) {
    if (-not $Condition) { throw $Message }
}

function Request($Method, $Path, $Expected, $Body = $null, $Token = $null) {
    $options = @{
        Uri = $BaseUrl.AbsoluteUri.TrimEnd('/') + '/api' + $Path
        Method = $Method
        SkipHttpErrorCheck = $true
        TimeoutSec = 15
    }
    if ($null -ne $Body) {
        $options.ContentType = 'application/json; charset=utf-8'
        $options.Body = ConvertTo-Json -InputObject $Body -Compress
    }
    if ($Token) { $options.Headers = @{ Authorization = "Bearer $Token" } }

    $response = Invoke-WebRequest @options
    Assert-That ($response.StatusCode -eq $Expected) "$Method $Path esperava HTTP $Expected e recebeu HTTP $($response.StatusCode)."
    if ($Expected -lt 400 -and $response.Content) {
        $json = $response.Content
        if ($json -is [byte[]]) { $json = [Text.Encoding]::UTF8.GetString($json) }
        ConvertFrom-Json -InputObject $json
    }
}

function Login($Email, $Password) {
    Request POST '/auth/login' 200 @{ email = $Email; password = $Password }
}

$null = Request GET '/tickets' 401
$null = Request POST '/auth/login' 401 @{ email = 'admin@helpdesk.test'; password = 'senha-incorreta' }
$admin = Login 'admin@helpdesk.test' $demoPassword
$tech = Login 'tecnico@helpdesk.test' $demoPassword
$other = Login 'usuario@helpdesk.test' $demoPassword

$email = "$prefix@helpdesk.test"
$account = @{ email = $email; name = 'Solicitante de teste'; password = $demoPassword; role = 'Admin' }
$owner = Request POST '/auth/register' 201 $account
$account.email = $email.ToUpperInvariant()
$null = Request POST '/auth/register' 409 $account
$me = Request GET '/auth/me' 200 -Token $owner.accessToken
Assert-That ($me.role -eq 'User' -and $me.email -eq $email) 'O cadastro deve sempre criar um solicitante.'
$null = Request GET '/users' 403 -Token $owner.accessToken

$input = @{ title = "Chamado $prefix"; description = 'Verificar o fluxo de atendimento.'; priority = 'Critical'; requesterId = $other.user.id }
$ticket = Request POST '/tickets' 201 $input $owner.accessToken
Assert-That ($ticket.status -eq 'Open' -and $ticket.requester.id -eq $owner.user.id) 'O chamado deve ser aberto em nome da sessão.'
$path = '/tickets/' + $ticket.id
$null = Request GET $path 200 -Token $owner.accessToken
$null = Request GET $path 404 -Token $other.accessToken
$null = Request GET "$path/comments" 404 -Token $other.accessToken
$null = Request POST "$path/comments" 404 @{ content = 'Sem acesso.' } $other.accessToken
$null = Request PUT $path 404 @{ title = 'Sem acesso' } $other.accessToken
$null = Request PATCH "$path/status" 403 @{ status = 'InProgress' } $owner.accessToken
$null = Request PATCH "$path/assign" 403 @{ technicianId = $tech.user.id } $owner.accessToken
$null = Request PATCH "$path/assign" 403 @{ technicianId = $other.user.id } $tech.accessToken
$null = Request GET '/users/technicians' 403 -Token $tech.accessToken
$null = Request PATCH "$path/status" 409 @{ status = 'Closed' } $tech.accessToken
$null = Request PUT $path 403 @{ title = 'Somente o solicitante pode editar' } $tech.accessToken
$edited = Request PUT $path 200 @{ title = "Revisado $prefix" } $owner.accessToken
Assert-That ($edited.title -eq "Revisado $prefix" -and $edited.priority -eq 'Critical') 'A edição parcial deve preservar a prioridade.'
$assigned = Request PATCH "$path/take" 200 @{} $tech.accessToken
Assert-That ($assigned.assignedTo.id -eq $tech.user.id) 'O técnico deve conseguir pegar um chamado sem responsável.'
$assignedToAdmin = Request PATCH "$path/assign" 200 @{ technicianId = $admin.user.id } $admin.accessToken
Assert-That ($assignedToAdmin.assignedTo.id -eq $admin.user.id) 'O administrador deve conseguir reatribuir um chamado.'
$null = Request PATCH "$path/take" 409 @{} $tech.accessToken
$assigned = Request PATCH "$path/assign" 200 @{ technicianId = $tech.user.id } $admin.accessToken
Assert-That ($assigned.assignedTo.id -eq $tech.user.id) 'O administrador deve conseguir atribuir o técnico responsável.'

$code = 'HD-' + $ticket.id.Substring($ticket.id.Length - 12).ToUpperInvariant()
$list = Request GET "/tickets?page=1&pageSize=1&status=Open&priority=Critical&search=$code" 200 -Token $owner.accessToken
Assert-That ($list.totalItems -eq 1 -and $list.items[0].id -eq $ticket.id) 'Busca por código, filtros e paginação devem funcionar em conjunto.'
$empty = Request GET "/tickets?search=$prefix" 200 -Token $other.accessToken
Assert-That ($empty.totalItems -eq 0) 'A fila de um solicitante deve excluir os chamados de outros usuários.'

$comment = Request POST "$path/comments" 201 @{ content = 'Reprodução confirmada.' } $tech.accessToken
$comments = @(Request GET "$path/comments" 200 -Token $owner.accessToken)
Assert-That ($comments.Count -eq 1 -and $comments[0].id -eq $comment.id) 'O solicitante deve ver o comentário persistido.'
foreach ($state in 'InProgress', 'Resolved', 'InProgress', 'Resolved', 'Closed') {
    $current = Request PATCH "$path/status" 200 @{ status = $state } $tech.accessToken
    Assert-That ($current.status -eq $state) "Situação inesperada após mover para $state."
    if ($state -eq 'Resolved') { Assert-That ($null -ne $current.resolvedAt) 'A resolução deve registrar data.' }
    if ($state -eq 'InProgress') { Assert-That ($null -eq $current.resolvedAt) 'A reabertura deve limpar a data da resolução.' }
}
Assert-That ($null -ne $current.closedAt) 'O fechamento deve registrar data.'
$null = Request POST "$path/comments" 409 @{ content = 'Chamado fechado.' } $owner.accessToken
$null = Request PUT $path 409 @{ priority = 'Low' } $owner.accessToken

$null = Request GET '/users' 200 -Token $admin.accessToken
$updated = Request PATCH "/users/$($owner.user.id)/role" 200 @{ role = 'Technician' } $admin.accessToken
Assert-That ($updated.role -eq 'Technician') 'O administrador deve conseguir alterar o perfil.'
$null = Request GET '/auth/me' 401 -Token $owner.accessToken
$promoted = Login $email $demoPassword
Assert-That ($promoted.user.role -eq 'Technician') 'O novo login deve refletir a alteração de perfil.'

Write-Host "Integração aprovada. Registro de teste $prefix."
