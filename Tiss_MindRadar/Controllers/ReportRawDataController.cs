using OfficeOpenXml.Style;
using OfficeOpenXml;
using OfficeOpenXml.Drawing.Chart;
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
            try
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

                var groupedData = reportData
                    .GroupBy(r => new { r.UserName, r.Gender, r.SurveyDate })
                    .Select(g => new
                    {
                        g.Key.UserName,
                        g.Key.Gender,
                        SurveyDate = g.Key.SurveyDate?.ToString("yyyy/MM/dd"),
                        Scores = g.GroupBy(r => r.QuestionID).ToDictionary(q => q.Key, q => q.First().Score)
                    }).ToList();

                // **計算男女大類別平均數**
                var maleData = groupedData.Where(p => p.Gender == "男");
                var femaleData = groupedData.Where(p => p.Gender == "女");

                var questionTexts = _db.MentalState.OrderBy(q => q.QuestionNumber).Select(q => q.QuestionText).ToList();

                var maleScores = reportData.Where(r => r.Gender == "男").GroupBy(r => r.QuestionID).ToDictionary(g => g.Key, g => g.Average(r => r.Score));

                var femaleScores = reportData.Where(r => r.Gender == "女").GroupBy(r => r.QuestionID).ToDictionary(g => g.Key, g => g.Average(r => r.Score));

                // 計算大類別的平均分數
                var categoryScores = new Dictionary<string, (double maleAvg, double femaleAvg)>
                {
                    { "一、基礎心理技能", GetCategoryAverage(new[] { 1, 2, 3 }, maleScores, femaleScores) },
                    { "二、身體心理技能", GetCategoryAverage(new[] { 4, 5, 6, 7 }, maleScores, femaleScores) },
                    { "三、認知技能", GetCategoryAverage(new[] { 8, 9, 10, 11, 12 }, maleScores, femaleScores) }
                };

                ExcelPackage.LicenseContext = LicenseContext.NonCommercial; //**設定 LicenseContext 避免錯誤**

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add($"{team.TeamName} 報表");

                    // 建立標題列
                    List<string> headers = new List<string> { "姓名", "性別", "填答日期" };

                    headers.AddRange(questionTexts);

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
                                ? Math.Round((double)player.Scores[q], 1) : (double?)null;
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

                    // 在此處插入雷達圖
                    var radarChart = worksheet.Drawings.AddChart("RadarChart", eChartType.Radar);

                    //// 設定雷達圖數據範圍 (大類別的男、女性別平均分數)
                    //var maleAverageRange = worksheet.Cells[2, 28, row - 1, 28]; // 假設這是男性大類別平均數的範圍
                    //var femaleAverageRange = worksheet.Cells[2, 29, row - 1, 29]; // 假設這是女性大類別平均數的範圍

                    //radarChart.Series.Add(maleAverageRange, worksheet.Cells[2, 1, row - 1, 1]);
                    //radarChart.Series.Add(femaleAverageRange, worksheet.Cells[2, 1, row - 1, 1]);

                    //radarChart.Title.Text = "隊伍雷達圖";

                    //// 設置雷達圖大小
                    //radarChart.SetSize(500, 500);

                    //// 使用 SetPosition 設置圖表位置 (設置圖表的起始列和行)
                    //radarChart.SetPosition(row + 1, 0, 1, 0); // 在 row + 1 行，1 列的位置顯示
                    // 設定雷達圖標題
                    radarChart.Title.Text = $"{team.TeamName} 向度大類別平均分數（男女區分）";

                    // **大類別名稱**
                    string[] categories = { "基礎心理技能", "身體心理技能", "認知技能" };

                    // **對應的男女性別平均數**
                    double[] maleAverages = {
                                categoryScores["一、基礎心理技能"].maleAvg,
                                categoryScores["二、身體心理技能"].maleAvg,
                                categoryScores["三、認知技能"].maleAvg
                        };

                    double[] femaleAverages = {
                                categoryScores["一、基礎心理技能"].femaleAvg,
                                categoryScores["二、身體心理技能"].femaleAvg,
                                categoryScores["三、認知技能"].femaleAvg
                        };

                    // **填入數據到 Excel**
                    int chartStartRow = row + 2;
                    int chartStartCol = 1;

                    worksheet.Cells[chartStartRow, chartStartCol].Value = "向度大類別";
                    worksheet.Cells[chartStartRow, chartStartCol + 1].Value = "男";
                    worksheet.Cells[chartStartRow, chartStartCol + 2].Value = "女";

                    for (int i = 0; i < categories.Length; i++)
                    {
                        worksheet.Cells[chartStartRow + i + 1, chartStartCol].Value = categories[i];
                        worksheet.Cells[chartStartRow + i + 1, chartStartCol + 1].Value = maleAverages[i];
                        worksheet.Cells[chartStartRow + i + 1, chartStartCol + 2].Value = femaleAverages[i];
                    }

                    // 設定圖表範圍
                    var seriesMale = radarChart.Series.Add(
                        worksheet.Cells[chartStartRow + 1, chartStartCol + 1, chartStartRow + categories.Length, chartStartCol + 1],
                        worksheet.Cells[chartStartRow + 1, chartStartCol, chartStartRow + categories.Length, chartStartCol]
                    );
                    seriesMale.Header = "男性平均分數";

                    var seriesFemale = radarChart.Series.Add(
                        worksheet.Cells[chartStartRow + 1, chartStartCol + 2, chartStartRow + categories.Length, chartStartCol + 2],
                        worksheet.Cells[chartStartRow + 1, chartStartCol, chartStartRow + categories.Length, chartStartCol]
                    );
                    seriesFemale.Header = "女性平均分數";

                    // **調整雷達圖大小與位置**
                    radarChart.SetPosition(chartStartRow - 2, 0, chartStartCol + 4, 0);
                    radarChart.SetSize(600, 400);

                    var stream = new MemoryStream(package.GetAsByteArray());
                    stream.Position = 0; // 修正檔案串流的位置

                    Response.Headers.Remove("Content-Disposition"); // 先移除可能的舊值
                    Response.Headers["Content-Disposition"] = $"attachment; filename=\"{HttpUtility.UrlEncode(fileName)}\"";
                    Response.Headers["Content-Type"] = "application/octet-stream";

                    return File(stream, "application/octet-stream", fileName);
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        #endregion

        #region 計算每個大類別的平均分數
        private (double maleAvg, double femaleAvg) GetCategoryAverage(int[] questionIds, Dictionary<int, double> maleScores, Dictionary<int, double> femaleScores)
        {
            double SafeAverage(IEnumerable<double> scores) => scores.Any() ? Math.Round(scores.Average(), 1) : 0.0;

            var maleCategoryAvg = SafeAverage(questionIds.Where(id => maleScores.ContainsKey(id)).Select(id => maleScores[id]));
            var femaleCategoryAvg = SafeAverage(questionIds.Where(id => femaleScores.ContainsKey(id)).Select(id => femaleScores[id]));

            return (maleCategoryAvg, femaleCategoryAvg);
            //var maleCategoryAvg = questionIds
            //    .Where(id => maleScores.ContainsKey(id))
            //    .Average(id => maleScores[id]);

            //var femaleCategoryAvg = questionIds
            //    .Where(id => femaleScores.ContainsKey(id))
            //    .Average(id => femaleScores[id]);

            //return (Math.Round(maleCategoryAvg, 1), Math.Round(femaleCategoryAvg, 1));
        }
        #endregion
   }
}