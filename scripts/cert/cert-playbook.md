# Cert / Key Operations Playbook

Scripts: `scripts/cert/`. All prompt for every value.

## Per-app values

| Name | ModVerify |
|---|---|
| `<CertsDir>` | `src/ModVerify.CliApp/Resources/Certs/` |
| `<TrustCer>` | `modverify-trust.cer` |
| `<AppEnvironmentClass>` | `ModVerifyAppEnvironment` |
| `<TrustResourceConst>` | `EmbeddedTrustCertResource` |

GitHub Actions secret names used by `release.yml`: **`UPDATER_SIGNING_PFX_B64`** (base64-encoded intermediate PFX) and **`UPDATER_SIGNING_PFX_PASSWORD`** (its password).

---

## 0. First-time app setup

Once per app, before any cert ops.

1. Create `<CertsDir>` in the repo.
2. csproj: add the trust cert as an embedded resource (use a `Condition` so the build doesn't fail before the cert exists):
   ```xml
   <EmbeddedResource Include="<CertsDir><TrustCer>"
                     LogicalName="<TrustResourceConst-value>"
                     Condition="Exists('<CertsDir><TrustCer>')" />
   ```
   ModVerify: `LogicalName="AET.ModVerify.App.Resources.Certs.modverify-trust.cer"`.
3. `Program.cs`: define the constant and override `RegisterTrustedCertificates`:
   ```csharp
   private const string <TrustResourceConst> = "<logical-name-from-csproj>";

   protected override void RegisterTrustedCertificates(IServiceProvider appServices)
   {
       if (!IsUpdateableApplication) return;
       appServices.GetRequiredService<CertificateManager>()
           .RegisterTrustedCertificates(typeof(Program).Assembly, [<TrustResourceConst>], devCertPath);
   }
   ```
4. `<AppEnvironmentClass>`: set `UpdateMirrors` to your CDN URL with a `<channel>` segment, e.g. `https://example.com/<app>/v2/`.
5. GitHub → Settings → Secrets and variables → Actions → New repository secret. Create both `UPDATER_SIGNING_PFX_B64` and `UPDATER_SIGNING_PFX_PASSWORD` with placeholder values — they get the real values after section 2.

Then proceed with section 1.

---

## 1. Generate root

```powershell
.\scripts\cert\New-RootCertificate.ps1
```

ModVerify: Subject `CN=ModVerify Root CA`, Output base `.\modverify-root`, Years `20`. Writes `modverify-root.pfx` (private) and `modverify-root.cer` (trust cert to commit).

After:
1. Strong passphrase. Memorize *and* record.
2. Back up the PFX in ≥2 independent failure modes (password manager + offline USB / YubiKey).
3. Commit `<TrustCer>` to `<CertsDir>`.
4. Delete the working-copy PFX once backups are confirmed.
5. Schedule the annual test (section 3).

---

## 2. Issue intermediate

```powershell
.\scripts\cert\New-IntermediateCertificate.ps1
```

ModVerify (substitute current YYYY-MM): Root PFX `.\modverify-root.pfx`, Subject `CN=ModVerify Signing 2026-05`, Output base `.\modverify-int-202605`, Months `12`.

After:
1. GitHub → Settings → Secrets → Actions: paste the base64 the script printed into `UPDATER_SIGNING_PFX_B64`; set `UPDATER_SIGNING_PFX_PASSWORD` to the intermediate password you entered.
2. `Remove-Item ".\modverify-int-202605.pfx" -Force`.
3. Lock the root PFX away.
4. Trigger a release; confirm CI signs and a fresh client verifies.

If compromise: also publish a version higher than anything an attacker might push, audit recent releases, rotate fast.

---

## 3. Annual root test

```powershell
.\scripts\cert\Test-RootCertificate.ps1
```

ModVerify: Root PFX `.\modverify-root.pfx`, Test subject `CN=Annual Test - DELETE ME`, Embedded cert `.\src\ModVerify.CliApp\Resources\Certs\modverify-trust.cer`.

Fails → section 4 path B.

---

## 4. Root rotation

- Custody change / expiry → **path A (bridge)**.
- Old key lost or compromised → **path B (catastrophic)**.

Shared first steps:
1. New root via `New-RootCertificate.ps1` with a distinct output base (e.g. `.\modverify-v2-root` → `modverify-v2-root.pfx` / `modverify-v2-root.cer`).
2. New intermediate under the new root via `New-IntermediateCertificate.ps1`.

### Path A — Bridge (old key intact)

3. Bridge release N, signed by the **old** intermediate:
   - Drop new CER next to `<TrustCer>` in `<CertsDir>`; add to csproj `<EmbeddedResource>`.
   - `RegisterTrustedCertificates` in `Program.cs`: `[<TrustResourceConst>, <TrustResourceConst>V2]`.
   - Bump `UpdateMirrors` in `<AppEnvironmentClass>` to next `<channel>` segment.
4. Sign release N with old intermediate.
5. Publish release N to both `<channel>/` (frozen at N) and `<channel+1>/`.
6. Swap GitHub Secrets to new intermediate.
7. Release N+1+ under new root → `<channel+1>/` only.

After (path A):
- After migration window (e.g. 6 months), drop old `<TrustCer>` from new releases and stop publishing to old channel.
- Keep old offline root PFX while you might ship fixups on old channel.

### Path B — Catastrophic (no working old key)

Auto-update is dead until users manually reinstall.

3. Replace `<CertsDir>/<TrustCer>` with the new root's public cert. Drop the old root entirely.
4. Update GitHub Secrets.
5. Cut a release.
6. Announce widely — users must reinstall manually from GitHub Releases.

### Shared after

1. Apply section 1 "After" custody to the new root.
2. Destroy all remaining copies of the old root (after old channel retired in path A; immediately in path B).
