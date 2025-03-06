using OfficeOpenXml.Style;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;
using System.Runtime.Remoting.Messaging;
using System.Runtime.InteropServices.ComTypes;

namespace Tiss_MindRadar.Controllers
{
    public class ReportRawDataController : Controller
    {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 產生各隊伍選手明細
        public ActionResult TeamReportData(int? teamId)
        {
            // 取得所有啟用的隊伍
            var teams = _db.Team.Where(t => t.isenable == true).ToList();
            ViewBag.Teams = new SelectList(teams, "TeamID", "TeamName", teamId);

            List<TeamReportViewModel> reportData = new List<TeamReportViewModel>();

            if (teamId.HasValue)
            {
                reportData = _db.PsychologicalResponse
                    .Join(_db.Users, pr => pr.UserID, u => u.UserID, (pr, u) => new { pr, u })
                    .Join(_db.MentalState, temp => temp.pr.CategoryID, ms => ms.QuestionNumber, (temp, ms) => new { temp, ms })
                    .Join(_db.UserProfile, temp => temp.temp.u.UserID, up => up.UserID, (temp, up) => new { temp, up })
                    .Where(result => result.temp.temp.u.TeamID == teamId)
                    .Select(result => new TeamReportViewModel
                    {
                        UserName = result.temp.temp.u.UserName,
                        Gender = result.up.Gender, // 取得性別欄位
                        Category = result.temp.ms.QuestionText,
                        Score = result.temp.temp.pr.Score,
                        SurveyDate = result.temp.temp.pr.SurveyDate
                    })
                    .OrderBy(r => r.UserName)
                    .ThenBy(r => r.SurveyDate)
                    .ToList();
            }

            return View(reportData);
        }
        #endregion

        #region 下載Excel報表
        [HttpPost]
        public ActionResult ExportTeamReportToExcel()
        {
            if (!int.TryParse(Request.Form["teamId"], out int teamId))
            {
                return new HttpStatusCodeResult(400, "缺少必要參數");
            }

            var team = _db.Team.FirstOrDefault(t => t.TeamID == teamId);
            if (team == null) return HttpNotFound("隊伍不存在");

            // 避免特殊字元影響檔名，移除可能不合法的字元
            string safeTeamName = string.Concat(team.TeamName.Split(Path.GetInvalidFileNameChars()));
            string fileName = $"{safeTeamName}_報表.xlsx";
            string encodedFileName = Uri.EscapeDataString(fileName);

            var reportData = _db.PsychologicalResponse
                .Join(_db.Users, pr => pr.UserID, u => u.UserID, (pr, u) => new { pr, u })
                .Where(x => x.u.TeamID == teamId)
                .Select(x => new
                {
                    x.u.UserName,
                    Gender = x.u.UserProfile.FirstOrDefault().Gender,
                    x.pr.SurveyDate,
                    x.pr.Score,
                    x.pr.QuestionID
                }).ToList();

            if (!reportData.Any()) return Content("沒有數據可下載");

            // 依選手與日期分組，確保每位選手每一題的分數都顯示
            var groupedData = reportData
                .GroupBy(r => new { r.UserName, r.Gender, r.SurveyDate })
                .Select(g => new
                {
                    g.Key.UserName,
                    g.Key.Gender,
                    SurveyDate = g.Key.SurveyDate?.ToString("yyyy/MM/dd"),
                    Scores = g.OrderBy(r => r.QuestionID).ToDictionary(r => r.QuestionID, r => r.Score)
                }).ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add($"{team.TeamName} 報表");

                // 建立標題列
                List<string> headers = new List<string> { "姓名", "性別", "填答日期" };
                for (int i = 1; i <= 24; i++) headers.Add($"Q{i}");
                headers.AddRange(new string[]
                {
                    "一、基礎心理技能", "1.目標設定", "2.自信心", "3.承諾",
                    "二、身體心理技能", "1.壓力反應", "2.害怕控制", "3.活化/激發", "4.放鬆",
                    "三、認知技能", "1.意象", "2.心智練習", "3.專注", "4.再專注", "5.競賽計畫"
                });

                for (int i = 0; i < headers.Count; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                    worksheet.Cells[1, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                int row = 2;
                foreach (var player in groupedData)
                {
                    worksheet.Cells[row, 1].Value = player.UserName;
                    worksheet.Cells[row, 2].Value = player.Gender;
                    worksheet.Cells[row, 3].Value = player.SurveyDate;

                    // 填入每題的分數
                    for (int q = 1; q <= 24; q++)
                    {
                        worksheet.Cells[row, q + 3].Value = player.Scores.ContainsKey(q)
                            ? Math.Round((double)player.Scores[q], 1): (double?)null;
                    }

                    // 安全計算平均數的方法，確保保留小數第一位
                    double? SafeAverage(IEnumerable<double?> scores) =>
                        scores.Any(s => s.HasValue) ? Math.Round(scores.Where(s => s.HasValue).Average(s => s.Value), 1) : (double?)null;

                    // 計算小類別平均數
                    double? avgGoalSetting = SafeAverage(player.Scores.Where(kv => kv.Key == 1 || kv.Key == 2).Select(kv => (double?)kv.Value));
                    double? avgConfidence = SafeAverage(player.Scores.Where(kv => kv.Key == 3 || kv.Key == 4).Select(kv => (double?)kv.Value));
                    double? avgCommitment = SafeAverage(player.Scores.Where(kv => kv.Key == 5 || kv.Key == 6).Select(kv => (double?)kv.Value));
                    double? avgStressResponse = SafeAverage(player.Scores.Where(kv => kv.Key == 7 || kv.Key == 8).Select(kv => (double?)kv.Value));
                    double? avgFearControl = SafeAverage(player.Scores.Where(kv => kv.Key == 9 || kv.Key == 10).Select(kv => (double?)kv.Value));
                    double? avgActivation = SafeAverage(player.Scores.Where(kv => kv.Key == 11 || kv.Key == 12).Select(kv => (double?)kv.Value));
                    double? avgRelaxation = SafeAverage(player.Scores.Where(kv => kv.Key == 13 || kv.Key == 14).Select(kv => (double?)kv.Value));
                    double? avgImagery = SafeAverage(player.Scores.Where(kv => kv.Key == 15 || kv.Key == 16).Select(kv => (double?)kv.Value));
                    double? avgMentalPractice = SafeAverage(player.Scores.Where(kv => kv.Key == 17 || kv.Key == 18).Select(kv => (double?)kv.Value));
                    double? avgFocus = SafeAverage(player.Scores.Where(kv => kv.Key == 19 || kv.Key == 20).Select(kv => (double?)kv.Value));
                    double? avgRefocus = SafeAverage(player.Scores.Where(kv => kv.Key == 21 || kv.Key == 22).Select(kv => (double?)kv.Value));
                    double? avgCompetitionPlan = SafeAverage(player.Scores.Where(kv => kv.Key == 23 || kv.Key == 24).Select(kv => (double?)kv.Value));


                    // 計算大類別平均數
                    double? avgBasicSkills = SafeAverage(new[] { avgGoalSetting, avgConfidence, avgCommitment });
                    double? avgPhysicalSkills = SafeAverage(new[] { avgStressResponse, avgFearControl, avgActivation, avgRelaxation });
                    double? avgCognitiveSkills = SafeAverage(new[] { avgImagery, avgMentalPractice, avgFocus, avgRefocus, avgCompetitionPlan });

                    // 填入大類別與小類別平均數，並四捨五入到小數點第一位
                    worksheet.Cells[row, 28].Value = avgBasicSkills.HasValue ? Math.Round(avgBasicSkills.Value, 1) : (double?)null;
                    worksheet.Cells[row, 29].Value = avgGoalSetting.HasValue ? Math.Round(avgGoalSetting.Value, 1) : (double?)null;
                    worksheet.Cells[row, 30].Value = avgConfidence.HasValue ? Math.Round(avgConfidence.Value, 1) : (double?)null;
                    worksheet.Cells[row, 31].Value = avgCommitment.HasValue ? Math.Round(avgCommitment.Value, 1) : (double?)null;
                    worksheet.Cells[row, 32].Value = avgPhysicalSkills.HasValue ? Math.Round(avgPhysicalSkills.Value, 1) : (double?)null;
                    worksheet.Cells[row, 33].Value = avgStressResponse.HasValue ? Math.Round(avgStressResponse.Value, 1) : (double?)null;
                    worksheet.Cells[row, 34].Value = avgFearControl.HasValue ? Math.Round(avgFearControl.Value, 1) : (double?)null;
                    worksheet.Cells[row, 35].Value = avgActivation.HasValue ? Math.Round(avgActivation.Value, 1) : (double?)null;
                    worksheet.Cells[row, 36].Value = avgRelaxation.HasValue ? Math.Round(avgRelaxation.Value, 1) : (double?)null;
                    worksheet.Cells[row, 37].Value = avgCognitiveSkills.HasValue ? Math.Round(avgCognitiveSkills.Value, 1) : (double?)null;
                    worksheet.Cells[row, 38].Value = avgImagery.HasValue ? Math.Round(avgImagery.Value, 1) : (double?)null;
                    worksheet.Cells[row, 39].Value = avgMentalPractice.HasValue ? Math.Round(avgMentalPractice.Value, 1) : (double?)null;
                    worksheet.Cells[row, 40].Value = avgFocus.HasValue ? Math.Round(avgFocus.Value, 1) : (double?)null;
                    worksheet.Cells[row, 41].Value = avgRefocus.HasValue ? Math.Round(avgRefocus.Value, 1) : (double?)null;
                    worksheet.Cells[row, 42].Value = avgCompetitionPlan.HasValue ? Math.Round(avgCompetitionPlan.Value, 1) : (double?)null;

                    row++;
                }

                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                var stream = new MemoryStream(package.GetAsByteArray());
                stream.Position = 0; // 修正檔案串流的位置

                Response.Headers.Remove("Content-Disposition"); // 先移除可能的舊值
                Response.Headers["Content-Disposition"] = $"attachment; filename=\"{HttpUtility.UrlEncode(fileName)}\"";
                Response.Headers["Content-Type"] = "application/octet-stream";

                return File(stream, "application/octet-stream", fileName);
            }
        }
        #endregion
    }
}