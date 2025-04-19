using System.Drawing;
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
using System.Diagnostics;
using Newtonsoft.Json;
using System.Numerics;
using System.ComponentModel;
using Tiss_MindRadar.Utility;
using Tiss_MindRadar.ViewModels;

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
                    .ToList()
                    .Select(result => new TeamReportViewModel
                    {
                        UserName = MaskingHelper.MaskUserName(result.temp.temp.u.UserName),
                        Gender = result.up.Gender,
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

        #region 產生報表
        private void GenerateReportSheet(ExcelWorksheet sheet, List<ReportDataModel> data, Dictionary<string, (double maleAvg, double femaleAvg)> categoryAverages)
        {
            //標題
            sheet.Cells[1, 1].Value = "心理技能報表";
            sheet.Cells[1, 1].Style.Font.Bold = true;

            //標題列
            List<string> headers = new List<string> { "姓名", "性別", "填答日期" };
            var questionTexts = _db.MentalState.OrderBy(q => q.QuestionNumber).Select(q => q.QuestionText).ToList();
            headers.AddRange(questionTexts);

            //寫入標題
            for (int i = 0; i < headers.Count; i++)
            {
                sheet.Cells[2, i + 1].Value = headers[i];
                sheet.Cells[2, i + 1].Style.Font.Bold = true;
                sheet.Cells[2, i + 1].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            // 填入人員數據
            int row = 3;
            foreach (var player in data)
            {
                sheet.Cells[row, 1].Value = player.UserName;
                sheet.Cells[row, 2].Value = player.Gender;
                sheet.Cells[row, 3].Value = player.SurveyDate;

                int col = 4;
                foreach (var score in player.Scores)
                {
                    sheet.Cells[row, col].Value = score.Value;
                    col++;
                }
                row++;
            }

            //填入大類別向度 & 小類別向度的平均數
            row++; //在個人數據後面空一行
            sheet.Cells[row, 1].Value = "心理技能平均數";
            sheet.Cells[row, 1].Style.Font.Bold = true;
            sheet.Cells[row, 1].Style.Font.Size = 14;
            sheet.Cells[row, 2].Value = "男";
            sheet.Cells[row, 3].Value = "女";
            sheet.Cells[row, 2, row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

            row++; //空一行，避免壓縮標題與數據

            foreach (var category in categoryAverages)
            {
                //如果是大類別 (基礎心理技能、身體心理技能、認知技能)**
                if (category.Key.StartsWith("一、") || category.Key.StartsWith("二、") || category.Key.StartsWith("三、"))
                {
                    sheet.Cells[row, 1].Value = category.Key;
                    sheet.Cells[row, 1].Style.Font.Bold = true;
                    sheet.Cells[row, 1].Style.Font.Size = 12;
                    sheet.Cells[row, 2].Value = category.Value.maleAvg;
                    sheet.Cells[row, 3].Value = category.Value.femaleAvg;
                    sheet.Cells[row, 2, row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    row++; //空一行，區隔小類別
                }
                else
                {
                    //小類別
                    sheet.Cells[row, 1].Value = category.Key;
                    sheet.Cells[row, 2].Value = category.Value.maleAvg;
                    sheet.Cells[row, 3].Value = category.Value.femaleAvg;
                    sheet.Cells[row, 2, row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                    row++; //換行
                }
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns(); //自動調整欄位寬度
        }
        #endregion

        #region 產生報表（直向）
        private void GenerateReportSheetVertical(ExcelWorksheet sheet, List<ReportDataModel> data)
        {
            sheet.Cells[1, 1].Value = "心理技能報表（直向）";
            sheet.Cells[1, 1].Style.Font.Bold = true;

            var questionList = _db.MentalState.OrderBy(q => q.QuestionNumber).ToList();

            sheet.Cells[2, 1].Value = "題號";
            sheet.Cells[2, 2].Value = "題目";

            for (int i = 0; i < data.Count; i++)
            {
                sheet.Cells[2, i + 3].Value = data[i].UserName + "\n(" + data[i].SurveyDate + ")";
                sheet.Cells[2, i + 3].Style.WrapText = true;
                sheet.Cells[2, i + 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }

            int row = 3;
            foreach (var q in questionList)
            {
                sheet.Cells[row, 1].Value = q.QuestionNumber;
                sheet.Cells[row, 2].Value = q.QuestionText;

                for (int i = 0; i < data.Count; i++)
                {
                    var score = data[i].Scores.ContainsKey(q.QuestionNumber) ? data[i].Scores[q.QuestionNumber] : 0;
                    sheet.Cells[row, i + 3].Value = score;
                }

                row++;
            }

            sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
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

                string safeTeamName = string.Concat(team.TeamName.Split(Path.GetInvalidFileNameChars()));
                string fileName = $"{safeTeamName}_報表.xlsx";

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

                //性別排序(女 → 男)
                var groupedData = reportData
                    .GroupBy(r => new { r.UserName, r.Gender, r.SurveyDate })
                    .Select(g => new ReportDataModel
                    {
                        UserName = MaskingHelper.MaskUserName(g.Key.UserName),
                        Gender = g.Key.Gender,
                        SurveyDate = g.Key.SurveyDate?.ToString("yyyy/MM/dd"),
                        Scores = g.GroupBy(r => r.QuestionID).ToDictionary(q => q.Key, q => q.First().Score)
                    })
                    .OrderBy(g => g.Gender == "女" ? 0 : 1).ThenBy(g => g.UserName).ToList();

                //計算大類別向度 & 小類別向度的平均數
                var maleScores = reportData.Where(r => r.Gender == "男").GroupBy(r => r.QuestionID).ToDictionary(g => g.Key, g => g.Average(r => r.Score));

                var femaleScores = reportData.Where(r => r.Gender == "女").GroupBy(r => r.QuestionID).ToDictionary(g => g.Key, g => g.Average(r => r.Score));

                var categoryAverages = GetCategoryAverages(maleScores, femaleScores);

                //ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                ExcelPackage.License.SetNonCommercialPersonal("YourName");

                using (var package = new ExcelPackage())
                {
                    var worksheet = package.Workbook.Worksheets.Add($"{team.TeamName} 報表");
                    
                    //GenerateReportSheet(worksheet, groupedData, categoryAverages); //產生報表
                    GenerateReportSheetVertical(worksheet, groupedData); //使用直向格式
                    GenerateRadarChart(worksheet, categoryAverages);

                    var stream = new MemoryStream(package.GetAsByteArray());

                    return File(stream, "application/octet-stream", fileName);
                }
            }
            catch (Exception ex)
            {
                var errorBytes = System.Text.Encoding.UTF8.GetBytes("產生報表失敗：" + ex.Message);
                return File(errorBytes, "text/plain", "報表錯誤訊息.txt");
            }
        }
        #endregion

        #region 計算大類別向度 & 小類別向度的平均數
        private (double maleAvg, double femaleAvg) GetAverage(int[] questionIds, Dictionary<int, double> maleScores, Dictionary<int, double> femaleScores)
        {
            var maleCategoryScores = questionIds
                .Where(id => maleScores.ContainsKey(id))
                .Select(id => (double?)maleScores[id]);

            var femaleCategoryScores = questionIds
                .Where(id => femaleScores.ContainsKey(id))
                .Select(id => (double?)femaleScores[id]);

            return (SafeAverage(maleCategoryScores), SafeAverage(femaleCategoryScores));
        }

        private double SafeAverage(IEnumerable<double?> scores) =>
            scores.Any(s => s.HasValue) ? Math.Round(scores.Where(s => s.HasValue).Average(s => s.Value), 1) : 0.0;


        private Dictionary<string, (double maleAvg, double femaleAvg)> GetCategoryAverages(Dictionary<int, double> maleScores, Dictionary<int, double> femaleScores)
        {
            return new Dictionary<string, (double maleAvg, double femaleAvg)>
        {
            // **大類別平均數**
            { "一、基礎心理技能", GetAverage(new[] { 1, 2, 3, 4, 5, 6 }, maleScores, femaleScores) },
            { "二、身體心理技能", GetAverage(new[] { 7, 8, 9, 10, 11, 12, 13, 14 }, maleScores, femaleScores) },
            { "三、認知技能", GetAverage(new[] { 15, 16, 17, 18, 19, 20, 21, 22, 23, 24 }, maleScores, femaleScores) },

            // **小類別平均數**
            { "1.目標設定", GetAverage(new[] { 1, 2 }, maleScores, femaleScores) },
            { "2.自信心", GetAverage(new[] { 3, 4 }, maleScores, femaleScores) },
            { "3.承諾", GetAverage(new[] { 5, 6 }, maleScores, femaleScores) },

            { "1.壓力反應", GetAverage(new[] { 7, 8 }, maleScores, femaleScores) },
            { "2.害怕控制", GetAverage(new[] { 9, 10 }, maleScores, femaleScores) },
            { "3.活化/激發", GetAverage(new[] { 11, 12 }, maleScores, femaleScores) },
            { "4.放鬆", GetAverage(new[] { 13, 14 }, maleScores, femaleScores) },

            { "1.意象", GetAverage(new[] { 15, 16 }, maleScores, femaleScores) },
            { "2.心智練習", GetAverage(new[] { 17, 18 }, maleScores, femaleScores) },
            { "3.專注", GetAverage(new[] { 19, 20 }, maleScores, femaleScores) },
            { "4.再專注", GetAverage(new[] { 21, 22 }, maleScores, femaleScores) },
            { "5.競賽計畫", GetAverage(new[] { 23, 24 }, maleScores, femaleScores) }
        };
        }
        #endregion

        #region 產生報表內雷達圖_EPPlus版本
        private void GenerateRadarChart(ExcelWorksheet sheet, Dictionary<string, (double maleAvg, double femaleAvg)> categoryAverages)
        {
            var chart = sheet.Drawings.AddChart("RadarChart", eChartType.RadarFilled);
            chart.Title.Text = "向度大類別平均分數（男女區分）";
            chart.Title.Font.Size = 14;
            chart.Title.Font.Bold = true;

            var categories = new List<string> { "一、基礎心理技能", "二、身體心理技能", "三、認知技能" };
            var maleData = categories.Select(c => categoryAverages.ContainsKey(c) ? categoryAverages[c].maleAvg : 0).ToList();
            var femaleData = categories.Select(c => categoryAverages.ContainsKey(c) ? categoryAverages[c].femaleAvg : 0).ToList();

            int startRow = sheet.Dimension.End.Row + 2;
            int startCol = 1;

            // 寫入資料
            //sheet.Cells[startRow, startCol].Value = "類別";
            //sheet.Cells[startRow, startCol + 1].Value = "男性平均分數";
            //sheet.Cells[startRow, startCol + 2].Value = "女性平均分數";
            //sheet.Cells[startRow, startCol, startRow, startCol + 2].Style.Font.Bold = true;

            for (int i = 0; i < categories.Count; i++)
            {
                sheet.Cells[startRow + i + 1, startCol].Value = categories[i];
                sheet.Cells[startRow + i + 1, startCol + 1].Value = maleData[i];
                sheet.Cells[startRow + i + 1, startCol + 2].Value = femaleData[i];
            }

            var dataRange = sheet.Cells[startRow + 1, startCol, startRow + categories.Count, startCol];
            var maleRange = sheet.Cells[startRow + 1, startCol + 1, startRow + categories.Count, startCol + 1];
            var femaleRange = sheet.Cells[startRow + 1, startCol + 2, startRow + categories.Count, startCol + 2];

            var maleSeries = chart.Series.Add(maleRange, dataRange);
            maleSeries.Header = "男性平均分數";

            var femaleSeries = chart.Series.Add(femaleRange, dataRange);
            femaleSeries.Header = "女性平均分數";

            // 提高辨識度
            maleSeries.Border.Fill.Color = System.Drawing.Color.Blue;
            maleSeries.Border.Width = 2;

            femaleSeries.Border.Fill.Color = System.Drawing.Color.Orange;
            femaleSeries.Border.Width = 2;

            chart.Legend.Position = eLegendPosition.Bottom;
            chart.Legend.Font.Size = 12;
            chart.Legend.Font.Bold = true;

            chart.SetPosition(startRow - 1, 0, startCol + 4, 0);
            chart.SetSize(600, 600);
            chart.DisplayBlanksAs = eDisplayBlanksAs.Gap;

            // 補充數據表格在圖表下方
            int tableStartRow = startRow + categories.Count + 10;
            sheet.Cells[tableStartRow, 1].Value = "向度名稱";
            sheet.Cells[tableStartRow, 2].Value = "男性平均分數";
            sheet.Cells[tableStartRow, 3].Value = "女性平均分數";
            sheet.Cells[tableStartRow, 1, tableStartRow, 3].Style.Font.Bold = true;

            for (int i = 0; i < categories.Count; i++)
            {
                sheet.Cells[tableStartRow + i + 1, 1].Value = categories[i];
                sheet.Cells[tableStartRow + i + 1, 2].Value = maleData[i];
                sheet.Cells[tableStartRow + i + 1, 3].Value = femaleData[i];
            }
        }
        #endregion
    }
}