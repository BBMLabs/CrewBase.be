namespace RowingClub.Identity.Application.Email;

public static class EmailTemplate
{
    public static string Render(string heading, string bodyHtml, string? ctaText = null, string? ctaUrl = null)
    {
        var ctaHtml = ctaText is not null && ctaUrl is not null
            ? $"""
              <div style="margin-top:28px;">
                <a href="{ctaUrl}" style="display:inline-block;background:#155e75;color:#ffffff;text-decoration:none;padding:13px 30px;border-radius:8px;font-weight:600;font-size:15px;font-family:Arial,Helvetica,sans-serif;">{ctaText}</a>
              </div>
              """
            : string.Empty;

        return $"""
            <!DOCTYPE html>
            <html lang="tr">
            <head>
            <meta charset="UTF-8" />
            <meta name="viewport" content="width=device-width, initial-scale=1.0" />
            <title>{heading}</title>
            </head>
            <body style="margin:0;padding:0;background:#f4f6f8;">
              <div style="background:#f4f6f8;padding:32px 16px;font-family:Arial,Helvetica,sans-serif;">
                <table role="presentation" width="100%" cellpadding="0" cellspacing="0" style="max-width:560px;margin:0 auto;background:#ffffff;border-radius:12px;overflow:hidden;border:1px solid #e5e9ec;">
                  <tr>
                    <td style="background:#155e75;padding:22px 32px;">
                      <span style="color:#ffffff;font-size:20px;font-weight:700;letter-spacing:0.3px;">&#x1F6A3; CrewBase</span>
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:32px;color:#1f2937;">
                      <h1 style="margin:0 0 18px;font-size:20px;line-height:1.35;color:#0f2733;">{heading}</h1>
                      <div style="font-size:15px;line-height:1.7;color:#374151;">{bodyHtml}</div>
                      {ctaHtml}
                    </td>
                  </tr>
                  <tr>
                    <td style="padding:18px 32px;background:#f4f6f8;border-top:1px solid #e5e9ec;">
                      <p style="margin:0;font-size:12px;line-height:1.6;color:#6b7280;">Bu e-postayı CrewBase hesabınızla ilgili bir işlem nedeniyle aldınız. Bu işlemi siz başlatmadıysanız güvenle yok sayabilirsiniz.</p>
                      <p style="margin:10px 0 0;font-size:12px;color:#9ca3af;">&copy; {DateTime.UtcNow.Year} CrewBase &mdash; Kürek kulüpleri için yönetim platformu.</p>
                    </td>
                  </tr>
                </table>
              </div>
            </body>
            </html>
            """;
    }
}
