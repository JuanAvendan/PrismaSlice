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
pwsh ./infra/azure-setup.ps1 -NeonConnectionString (Read-Host -AsSecureString "Neon connection string")
```

The script is safe to re-run. It does the following:

1. Creates the resource group and the Container Apps environment, without Log Analytics.
2. Creates the app: 1 vCPU / 2 GiB, 0 to 1 replicas, public HTTPS. The model needs about 1 GiB of RAM.
3. Stores the Neon connection string as an app secret.
4. Creates an Entra ID app with a federated credential for the GitHub `production` environment, with Contributor rights on the resource group only. Deployments use OIDC, so no Azure password is stored in GitHub.
5. Sets the repository secrets and variables the Deploy workflow needs.
6. Writes the API URL into `web/vercel.json`.

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

## Tear down

```powershell
az group delete --name prismaslice-rg
```

Then delete the Neon project, the Vercel project and the GHCR package if you no longer need them.
