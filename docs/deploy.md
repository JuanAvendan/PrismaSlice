# Deploying PrismaSlice for $0

```mermaid
flowchart LR
    U[Browser] --> V["Vercel<br/>static page (web/)"]
    V -- "rewrite /api/*" --> A["Azure Container Apps<br/>API + EmbeddingGemma (ONNX)"]
    A --> N[("Neon<br/>Postgres + pgvector")]
    G["GitHub Actions"] -- "image" --> R["GitHub Container Registry"]
    R --> A
```

| Piece | Service | Why it stays free |
|---|---|---|
| API with the embedding model | Azure Container Apps (Consumption) | Scales to zero, so it only bills while it handles requests, within the monthly free grant. Log Analytics is disabled. |
| Database | Neon (Free) | pgvector is included. The compute suspends after 5 minutes idle. |
| Container images | GitHub Container Registry | Free for public images, which avoids the ~USD 5/month of Azure Container Registry. |
| Search page | Vercel (Hobby) | Static files, and the API calls go through a rewrite, so there is no CORS. |

Free tiers change. Check the current limits on each provider's pricing page.

## 1. Database (Neon)

1. Create a Neon project, and pick the region closest to the API (for example US East).
2. Neon gives you a connection URL in the form `postgresql://user:password@host/db`. Npgsql needs key/value format, so rewrite it as:

   ```
   Host=<host>;Database=<db>;Username=<user>;Password=<password>;SSL Mode=Require
   ```

3. Seed it from your machine. This applies the migrations, inserts the synthetic data and embeds every ticket with the same ONNX model the API uses:

   ```powershell
   ./scripts/download-model.ps1
   $env:ConnectionStrings__Default = "<Neon connection string>"
   dotnet run --project tools/PrismaSlice.SeedData
   Remove-Item Env:ConnectionStrings__Default
   ```

## 2. Container image (GHCR)

Merging to `master` runs the **Deploy** workflow. It builds the API image and pushes it to `ghcr.io/<owner>/prismaslice-api`. The Azure step is skipped until step 3 has run.

After the first run, make the package public, so Azure can pull it without credentials: **GitHub → your profile → Packages → prismaslice-api → Package settings → Change visibility → Public**.

## 3. Azure Container Apps

Requirements:
- [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli): `winget install Microsoft.AzureCLI`
- [GitHub CLI](https://cli.github.com/): `winget install GitHub.cli`
- PowerShell 7.3+: `winget install Microsoft.PowerShell`

```powershell
az login
gh auth login
pwsh ./infra/azure-setup.ps1 -AlertEmail you@example.com `
    -NeonConnectionString (Read-Host -AsSecureString "Neon connection string")
```

The script is safe to re-run. It does the following:

1. Creates the resource group and the Container Apps environment, without Log Analytics.
2. Creates the app: 1 vCPU / 2 GiB, 0 to 1 replicas, public HTTPS. The model needs about 1 GiB of RAM.
3. Stores the Neon connection string as an app secret.
4. Creates an Entra ID app with a federated credential for the GitHub `production` environment, with Contributor rights on the resource group only. Deployments use OIDC, so no Azure password is stored in GitHub.
5. Creates a monthly **budget** on the resource group, USD 1 by default (`-BudgetAmountUsd`). It sends an email when actual spend passes 50 % and when the forecast passes 100 %.
6. Sets the repository secrets and variables the Deploy workflow needs.
7. Writes the API URL into `web/vercel.json`.

From then on, every merge to `master` deploys automatically.

## 4. Search page (Vercel)

1. Commit the updated `web/vercel.json`.
2. In Vercel, go to **Add New → Project** and import the repository.
3. Set **Root Directory** to `web` and **Framework Preset** to `Other`. There is no build command.
4. Deploy. The page calls `/api/*` on its own domain, and Vercel forwards those calls to the Container App.

## What to expect

- **Cold start.** After a few minutes without traffic the API scales to zero. The next visit has to start the container and load the model, which takes several seconds. The page tells the visitor it is waking up.
- **Neon also suspends.** The first query after it has been idle takes a little longer.
- **Rate limits.** By default the public API allows 30 searches per minute and 5 ticket creations per minute per IP. Change them with `RateLimiting__SearchPerMinute` and `RateLimiting__WritesPerMinute`.
- **Health checks.** `/health` doesn't touch the database, so probes don't keep Neon awake.

## Cost controls

The demo relies on the **monthly free grant** of Container Apps, which renews every month. It does not use the trial credit, which expires after 30 days.

When the Azure trial ends, the subscription has to be upgraded to pay-as-you-go to keep running. After that you are only billed for usage above the free amounts, so normal portfolio traffic costs USD 0.

Two safeguards cover the rest:

- **Budget alerts.** The setup script creates them. Azure budgets only notify; they never stop resources, and alerts can arrive several hours late.
- **Demo power switch.** In GitHub, go to **Actions → Demo power → Run workflow** and pick `off`, `on` or `status`.
  - `off` disables the app's ingress. Nothing can reach the API, so it stays scaled to zero and costs nothing.
  - `on` restores the ingress. The URL stays the same, because it comes from the Container Apps environment, so the Vercel rewrite keeps working.
  - While the demo is off, the search page says so and points visitors to the recording in the README.

With `--max-replicas 1`, the worst case is one replica running all month. Check the current Container Apps prices before relying on any figure.

## Tear down

```powershell
az group delete --name prismaslice-rg
```

Then delete the Neon project, the Vercel project and the GHCR package if you no longer need them.
