using ClosedXML.Excel;
using RowingClub.Scheduling.Application.Panel;

namespace RowingClub.Api.Endpoints;

/// <summary>
/// Bir şubenin üye listesini XLSX olarak üretir - şube silme akışında (aktarım kapasite yetersizse
/// veya kullanıcı doğrudan istediğinde) üyeleri kaybetmeden dışa aktarma imkânı verir.
/// </summary>
internal static class BranchMemberExcelBuilder
{
    public static byte[] Build(BranchDetailDto detail)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Üyeler");
        string[] headers = ["Ad Soyad", "Telefon", "E-posta", "Derece", "Üye Kodu", "Kayıt Tarihi"];
        for (var i = 0; i < headers.Length; i++)
            sheet.Cell(1, i + 1).Value = headers[i];

        var row = 2;
        foreach (var m in detail.Members)
        {
            sheet.Cell(row, 1).Value = m.FullName;
            sheet.Cell(row, 2).Value = m.Phone;
            sheet.Cell(row, 3).Value = m.Email ?? "";
            sheet.Cell(row, 4).Value = m.Level;
            sheet.Cell(row, 5).Value = m.MemberCode ?? "";
            sheet.Cell(row, 6).Value = m.CreatedAtUtc.ToString("yyyy-MM-dd");
            row++;
        }
        sheet.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
