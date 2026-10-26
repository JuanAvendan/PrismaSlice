#Requires -Version 7.3
<#
.SYNOPSIS
    Crea (una sola vez) la infraestructura de PrismaSlice en Azure Container Apps
    y conecta el workflow de GitHub Actions por OIDC.

.DESCRIPTION
    Requisitos: Azure CLI (az login hecho), GitHub CLI (gh auth login hecho), y la
    imagen ghcr.io/<owner>/prismaslice-api:latest ya publicada y PÚBLICA (la publica
    el workflow Deploy al hacer merge a master).

    Qué crea, todo dentro de la cuota gratuita del plan Consumption:
      - Grupo de recursos y entorno de Container Apps sin Log Analytics (evita costos de logs).
      - Container App: 1 vCPU / 2 GiB, escala de 0 a 1 réplica, ingress HTTPS público.
      - La cadena de conexión de Neon como secreto de la app (no queda en el repo).
      - Una app de Entra ID con credencial federada para el environment "production"
        de GitHub, con rol Contributor solo sobre el grupo de recursos.
      - Un presupuesto mensual sobre el grupo de recursos (USD 1 por defecto) que avisa
        por correo al 50 % real y al 100 % proyectado. Azure no corta el gasto por sí
        solo: para apagar la demo usa el workflow "Demo power" en GitHub Actions.
    Al final escribe la URL de la API en web/vercel.json.

.EXAMPLE
    ./infra/azure-setup.ps1 -AlertEmail tu@correo.com `
        -NeonConnectionString (Read-Host -AsSecureString "Cadena de Neon")
#>
param(
    [Parameter(Mandatory)]
    [securestring] $NeonConnectionString,

    [Parameter(Mandatory)]
    [string] $AlertEmail,

    [decimal] $BudgetAmountUsd = 1,
    [string] $ResourceGroup = "prismaslice-rg",
    [string] $Location = "eastus2",
    [string] $Environment = "prismaslice-env",
    [string] $AppName = "prismaslice-api",
    [string] $Repository = "JuanAvendan/PrismaSlice"
)

$ErrorActionPreference = "Stop"
$PSNativeCommandUseErrorActionPreference = $true

function Step($text) { Write-Host "`n==> $text" -ForegroundColor Cyan }

$owner = $Repository.Split("/")[0].ToLowerInvariant()
$image = "ghcr.io/$owner/prismaslice-api:latest"
$connection = [System.Net.NetworkCredential]::new("", $NeonConnectionString).Password
if ($connection -notmatch "Gss Encryption Mode") {
    $connection = $connection.TrimEnd(";") + ";Gss Encryption Mode=Disable"
}

Step "Comprobando sesiones de Azure y GitHub"
$subscriptionId = az account show --query id -o tsv
$tenantId = az account show --query tenantId -o tsv
gh auth status | Out-Null
az extension add --name containerapp --upgrade --only-show-errors
az provider register --namespace Microsoft.App --wait | Out-Null

Step "Grupo de recursos $ResourceGroup ($Location)"
az group create --name $ResourceGroup --location $Location --only-show-errors | Out-Null

Step "Entorno de Container Apps $Environment (sin Log Analytics)"
if (-not (az containerapp env list -g $ResourceGroup --query "[?name=='$Environment'].name" -o tsv)) {
    az containerapp env create --name $Environment --resource-group $ResourceGroup `
        --location $Location --logs-destination none --only-show-errors | Out-Null
}

Step "Container App $AppName con $image"
if (-not (az containerapp list -g $ResourceGroup --query "[?name=='$AppName'].name" -o tsv)) {
    az containerapp create --name $AppName --resource-group $ResourceGroup `
        --environment $Environment --image $image `
        --ingress external --target-port 8080 `
        --cpu 1 --memory 2Gi --min-replicas 0 --max-replicas 1 `
        --secrets "db-connection=$connection" `
        --env-vars "ConnectionStrings__Default=secretref:db-connection" "Swagger__Enabled=true" `
        --only-show-errors | Out-Null
}
else {
    az containerapp secret set --name $AppName --resource-group $ResourceGroup `
        --secrets "db-connection=$connection" --only-show-errors | Out-Null
}
$fqdn = az containerapp show --name $AppName --resource-group $ResourceGroup `
    --query properties.configuration.ingress.fqdn -o tsv

Step "Identidad de GitHub Actions (OIDC, sin secretos de larga duración)"
$appDisplayName = "github-$($Repository.Replace('/', '-'))-deploy"
$clientId = az ad app list --display-name $appDisplayName --query "[0].appId" -o tsv
if (-not $clientId) {
    $clientId = az ad app create --display-name $appDisplayName --query appId -o tsv
    az ad sp create --id $clientId --only-show-errors | Out-Null
}
$subject = "repo:${Repository}:environment:production"
$existing = az ad app federated-credential list --id $clientId --query "[?subject=='$subject'].name" -o tsv
if (-not $existing) {
    $credential = @{
        name      = "github-production"
        issuer    = "https://token.actions.githubusercontent.com"
        subject   = $subject
        audiences = @("api://AzureADTokenExchange")
    } | ConvertTo-Json -Compress
    $credentialFile = New-TemporaryFile
    Set-Content -Path $credentialFile -Value $credential -Encoding utf8
    az ad app federated-credential create --id $clientId --parameters "@$credentialFile" --only-show-errors | Out-Null
    Remove-Item $credentialFile
}
$scope = az group show --name $ResourceGroup --query id -o tsv
az role assignment create --assignee $clientId --role Contributor --scope $scope --only-show-errors | Out-Null

Step "Presupuesto de USD $BudgetAmountUsd/mes con alertas a $AlertEmail"
# API REST de Cost Management: permite fijar destinatarios de correo en todas las versiones de az.
# Las alertas llegan con algunas horas de retraso; son un aviso, no un tope de gasto.
$monthStart = (Get-Date).ToUniversalTime().ToString("yyyy-MM-01T00:00:00Z")
$budget = @{
    properties = @{
        category      = "Cost"
        amount        = $BudgetAmountUsd
        timeGrain     = "Monthly"
        timePeriod    = @{ startDate = $monthStart }
        notifications = @{
            actual_over_50 = @{
                enabled = $true; operator = "GreaterThan"; threshold = 50
                thresholdType = "Actual"; contactEmails = @($AlertEmail)
            }
            forecast_over_100 = @{
                enabled = $true; operator = "GreaterThan"; threshold = 100
                thresholdType = "Forecasted"; contactEmails = @($AlertEmail)
            }
        }
    }
} | ConvertTo-Json -Depth 6
$budgetFile = New-TemporaryFile
Set-Content -Path $budgetFile -Value $budget -Encoding utf8
try {
    az rest --method put `
        --url "https://management.azure.com$scope/providers/Microsoft.Consumption/budgets/prismaslice-monthly?api-version=2023-05-01" `
        --body "@$budgetFile" --only-show-errors | Out-Null
}
catch {
    # Algunas suscripciones (p. ej. de prueba) no admiten presupuestos: se avisa y se sigue.
    Write-Warning "No se pudo crear el presupuesto ($($_.Exception.Message)). Créalo en el portal: Cost Management → Budgets."
}
finally {
    Remove-Item $budgetFile
}

Step "Secretos y variables del repositorio $Repository"
gh secret set AZURE_CLIENT_ID --repo $Repository --body $clientId
gh secret set AZURE_TENANT_ID --repo $Repository --body $tenantId
gh secret set AZURE_SUBSCRIPTION_ID --repo $Repository --body $subscriptionId
gh variable set AZURE_RESOURCE_GROUP --repo $Repository --body $ResourceGroup
gh variable set AZURE_CONTAINER_APP --repo $Repository --body $AppName

Step "Apuntando web/vercel.json a https://$fqdn"
$vercelJson = Join-Path $PSScriptRoot "..\web\vercel.json"
(Get-Content $vercelJson -Raw) -replace "https://[^/`"]+\.azurecontainerapps\.io", "https://$fqdn" |
    Set-Content $vercelJson -Encoding utf8 -NoNewline

Write-Host "`nListo." -ForegroundColor Green
Write-Host "  API:     https://$fqdn/swagger"
Write-Host "  Health:  https://$fqdn/health"
Write-Host "  Siguiente: commitea web/vercel.json e importa el repo en Vercel (Root Directory: web)."
