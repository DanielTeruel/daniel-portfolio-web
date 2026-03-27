using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Microsoft.Data.SqlClient;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

var kvUri = "https://kv-daniellab.vault.azure.net/";
var kvClient = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
var secret = await kvClient.GetSecretAsync("ConnectionString-DanielDB");
string connString = secret.Value.Value;

async Task<SqlConnection> GetConnectionWithRetry()
{
    int attempts = 0;
    while (true)
    {
        try
        {
            var conn = new SqlConnection(connString);
            await conn.OpenAsync();
            return conn;
        }
        catch
        {
            attempts++;
            if (attempts >= 5) throw;
            await Task.Delay(3000);
        }
    }
}

app.MapGet("/", async () =>
{
    var proyectos = new System.Text.StringBuilder();
    var certs = new System.Text.StringBuilder();

    using (var conn = await GetConnectionWithRetry())
    {
        var cmd1 = new SqlCommand("SELECT Titulo, Tags, Descripcion FROM Proyectos", conn);
        using var reader1 = await cmd1.ExecuteReaderAsync();
        while (await reader1.ReadAsync())
        {
            var tags = reader1["Tags"].ToString()!.Split(',');
            var tagsHtml = string.Join("", tags.Select(t => $"<span class='proj-tag'>{t.Trim()}</span>"));
            proyectos.Append($@"
            <div class='proj-card fade-in'>
                <p class='proj-title'>{reader1["Titulo"]}</p>
                <div class='proj-tags'>{tagsHtml}</div>
                <p class='proj-body'>{reader1["Descripcion"]}</p>
            </div>");
        }
        await reader1.CloseAsync();

        var cmd2 = new SqlCommand("SELECT Nombre, Entidad, Skills FROM Certificaciones", conn);
        using var reader2 = await cmd2.ExecuteReaderAsync();
        while (await reader2.ReadAsync())
        {
            certs.Append($@"
            <div class='edu-card fade-in'>
                <div>
                    <p class='edu-title'>{reader2["Nombre"]}</p>
                    <p class='edu-place'>{reader2["Entidad"]}</p>
                    <p class='edu-skills'>{reader2["Skills"]}</p>
                </div>
            </div>");
        }
    }

    return Results.Content(HtmlPage(proyectos.ToString(), certs.ToString()), "text/html");
});

app.Run();

string HtmlPage(string proyectos, string certs) => $@"<!DOCTYPE html>
<html lang='es'>
<head>
<meta charset='UTF-8'>
<meta name='viewport' content='width=device-width, initial-scale=1.0'>
<title>Daniel Teruel Tirado – Cloud Engineer</title>
<link href='https://fonts.googleapis.com/css2?family=Inter:wght@300;400;500&family=Syne:wght@700;800&display=swap' rel='stylesheet'>
<style>
  *, *::before, *::after {{ box-sizing: border-box; margin: 0; padding: 0; }}
  :root {{
    --bg: #060b14; --surface: #0d1526; --surface2: #111d35; --border: #1a2a45;
    --azure: #0078d4; --azure-light: #50a8ff; --cyan: #00d4ff; --purple: #7c3aed;
    --green: #10b981; --text: #e2e8f0; --muted: #5a7a9a;
    --sans: 'Inter', sans-serif; --display: 'Syne', sans-serif;
  }}
  html {{ scroll-behavior: smooth; }}
  body {{ background: var(--bg); color: var(--text); font-family: var(--sans); font-weight: 300; line-height: 1.7; }}
  .container {{ position: relative; z-index: 1; max-width: 900px; margin: 0 auto; padding: 0 2rem 8rem; }}
  .hero {{ padding: 6rem 0 4rem; border-bottom: 1px solid var(--border); margin-bottom: 4rem; }}
  .hero-eyebrow {{ display: flex; align-items: center; gap: 10px; font-size: 12px; letter-spacing: .18em; text-transform: uppercase; color: var(--azure-light); margin-bottom: 1.5rem; font-weight: 400; }}
  .hero-eyebrow span {{ display: block; width: 32px; height: 1px; background: var(--azure-light); }}
  h1 {{ font-family: var(--display); font-size: clamp(2.4rem, 6vw, 4rem); font-weight: 800; line-height: 1.05; color: #fff; margin-bottom: .4rem; }}
  .hero-title {{ font-family: var(--display); font-size: clamp(1rem, 2.5vw, 1.4rem); font-weight: 700; color: var(--azure-light); margin-bottom: 1.8rem; }}
  .hero-bio {{ max-width: 620px; font-size: .95rem; color: var(--muted); line-height: 1.9; margin-bottom: 2.5rem; }}
  .hero-bio strong {{ color: #94b8d8; font-weight: 500; }}
  .badge-row {{ display: flex; flex-wrap: wrap; gap: 10px; }}
  .badge {{ font-size: 11px; font-weight: 500; letter-spacing: .06em; padding: 5px 14px; border-radius: 2px; border: 1px solid; }}
  .badge-azure {{ color: var(--azure-light); border-color: rgba(80,168,255,.35); background: rgba(0,120,212,.1); }}
  .badge-cert {{ color: #fbbf24; border-color: rgba(251,191,36,.35); background: rgba(251,191,36,.08); }}
  .badge-cloud {{ color: #a78bfa; border-color: rgba(167,139,250,.35); background: rgba(124,58,237,.08); }}
  .badge-sec {{ color: #34d399; border-color: rgba(52,211,153,.35); background: rgba(16,185,129,.08); }}
  .cert-hero {{ display: inline-flex; align-items: center; gap: 14px; padding: 14px 20px; border: 1px solid rgba(251,191,36,.3); background: linear-gradient(135deg, rgba(251,191,36,.07), rgba(0,120,212,.07)); border-radius: 6px; margin-bottom: 2rem; }}
  .cert-hero-icon {{ font-size: 10px; font-weight: 500; letter-spacing: .08em; color: #fbbf24; background: rgba(251,191,36,.12); border: 1px solid rgba(251,191,36,.4); padding: 6px 10px; border-radius: 3px; text-transform: uppercase; }}
  .cert-hero-text p:first-child {{ font-size: .88rem; font-weight: 500; color: #fff; }}
  .cert-hero-text p:last-child {{ font-size: .78rem; color: var(--muted); }}
  section {{ margin-bottom: 4.5rem; }}
  .section-label {{ font-size: 10px; letter-spacing: .22em; text-transform: uppercase; color: var(--azure-light); margin-bottom: 1.8rem; display: flex; align-items: center; gap: 12px; }}
  .section-label::after {{ content: ''; flex: 1; height: 1px; background: var(--border); }}
  .proj-card {{ padding: 1.5rem; border: 1px solid var(--border); background: var(--surface); margin-bottom: .75rem; border-radius: 6px; position: relative; overflow: hidden; transition: background .2s; }}
  .proj-card:hover {{ background: var(--surface2); }}
  .proj-card::before {{ content: ''; position: absolute; top: 0; left: 0; right: 0; height: 2px; background: linear-gradient(90deg, var(--azure), var(--purple)); }}
  .proj-title {{ font-family: var(--display); font-size: .92rem; font-weight: 700; color: #fff; margin-bottom: .6rem; }}
  .proj-tags {{ display: flex; flex-wrap: wrap; gap: 6px; margin-bottom: .8rem; }}
  .proj-tag {{ font-size: 10px; letter-spacing: .06em; color: var(--azure-light); background: rgba(0,120,212,.1); border: 1px solid rgba(80,168,255,.2); padding: 2px 8px; border-radius: 2px; }}
  .proj-body {{ font-size: .86rem; color: var(--muted); line-height: 1.8; }}
  .edu-card {{ display: flex; justify-content: space-between; align-items: flex-start; flex-wrap: wrap; gap: .5rem; padding: 1.2rem 1.5rem; border: 1px solid var(--border); background: var(--surface); margin-bottom: .75rem; border-radius: 6px; }}
  .edu-title {{ font-family: var(--display); font-size: .88rem; font-weight: 700; color: #fff; margin-bottom: .2rem; }}
  .edu-place {{ font-size: .8rem; color: var(--muted); margin-bottom: .3rem; }}
  .edu-skills {{ font-size: .78rem; color: #3a5a7a; font-style: italic; }}
  .fade-in {{ opacity: 0; transform: translateY(14px); animation: up .5s ease forwards; }}
  @keyframes up {{ to {{ opacity: 1; transform: none; }} }}
</style>
</head>
<body>
<div class='container'>
  <header class='hero'>
    <div class='hero-eyebrow'><span></span>Disponible para nuevas oportunidades</div>
    <h1>Daniel Teruel<br>Tirado</h1>
    <p class='hero-title'>Junior Cloud &amp; Systems Engineer</p>
    <div class='cert-hero'>
      <span class='cert-hero-icon'>AZ-104</span>
      <div class='cert-hero-text'>
        <p>Microsoft Certified: Azure Administrator Associate</p>
        <p>Microsoft · Identidades, VMs, RBAC, Storage, Backup, ARM</p>
      </div>
    </div>
    <p class='hero-bio'>Especializado en <strong>Microsoft Azure y entornos híbridos</strong>, con experiencia en administración de identidades, securización de infraestructuras corporativas y migración de dominios. Construyo y documento laboratorios cloud orientados a <strong>RBAC, Azure Policy, recuperación ante desastres y despliegues ARM</strong>. Motivado por crecer en <strong>Cloud Computing y Cloud Security</strong> dentro del ecosistema Microsoft.</p>
    <div class='badge-row'>
      <span class='badge badge-azure'>Microsoft Azure</span>
      <span class='badge badge-cert'>AZ-104 Certified</span>
      <span class='badge badge-cloud'>Hybrid Cloud</span>
      <span class='badge badge-cloud'>AWS</span>
      <span class='badge badge-sec'>Ciberseguridad</span>
      <span class='badge badge-azure'>Identity &amp; Access</span>
      <span class='badge badge-azure'>Active Directory</span>
      <span class='badge badge-sec'>Inglés C1</span>
    </div>
  </header>

  <section>
    <div class='section-label'>Proyectos cloud &amp; laboratorios</div>
    {proyectos}
  </section>

  <section>
    <div class='section-label'>Formación &amp; Certificaciones</div>
    {certs}
  </section>
</div>
</body>
</html>";
