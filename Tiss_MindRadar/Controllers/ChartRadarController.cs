using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;
using static Tiss_MindRadar.Models.RadarChartVIewModel;

namespace Tiss_MindRadar.Controllers
{
    public class ChartRadarController : Controller
    {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 身心狀態檢測雷達圖
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MentalPhysicalStateRadarChart(FormCollection form)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "UserAccount");
                }

                if (!int.TryParse(Session["UserID"]?.ToString(), out int userId))
                {
                    return RedirectToAction("Login", "UserAccount");
                }

                ViewBag.UserName = Session["UserName"];
                ViewBag.Age = Session["Age"];
                ViewBag.TeamName = Session["TeamName"];

                // 取得該用戶所有有填寫問卷的日期，並按時間排序
                List<DateTime> surveyDatesList = _db.UserResponse.Where(ur => ur.UserID == userId && ur.SurveyDate.HasValue)
                    .Select(ur => ur.SurveyDate.Value).Distinct().OrderByDescending(d => d).ToList();

                ViewBag.SurveyDates = surveyDatesList;  // 送到前端

                string[] surveyDatesRaw = form.GetValues("surveyDates");

                List<DateTime> selectedDates = new List<DateTime>();

                if (surveyDatesRaw != null)
                {
                    try
                    {
                        selectedDates = surveyDatesRaw
                            .Select(d => DateTime.ParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture)).ToList();
                    }
                    catch (FormatException)
                    {
                        ViewBag.ErrorMessage = "提交失敗：日期格式錯誤";
                        return View("MentalPhysicalStateRadarChart", new List<RadarChartVIewModel>());
                    }
                }

                if (!selectedDates.Any() && surveyDatesList.Any())
                {
                    selectedDates.Add(surveyDatesList.First()); // 預設選擇最新日期
                }

                ViewBag.SelectedDates = selectedDates;

                // 查詢所有選擇的日期的數據
                List<RadarChartVIewModel> radarData = new List<RadarChartVIewModel>();

                foreach (var date in selectedDates)
                {
                    string query = @"SELECT c.CategoryName, CAST(ROUND(AVG(ur.Score), 0) AS INT) AS AverageScore
                                FROM UserResponse ur INNER JOIN QuestionCategory qc ON ur.QuestionID = qc.QuestionID
                                INNER JOIN Category c ON qc.CategoryID = c.ID WHERE ur.UserID = @p0 AND ur.SurveyDate = @p1
                                GROUP BY c.CategoryName";

                    object[] parameters = { userId, date };
                    var data = _db.Database.SqlQuery<RadarChartVIewModel>(query, parameters).ToList();

                    foreach (var item in data)
                    {
                        item.SurveyDate = date.ToString("yyyy-MM-dd"); // 存入日期以便前端區分數據
                    }

                    radarData.AddRange(data);
                }
              
                return View(radarData);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"生成雷達圖失敗：{ex.Message}";
                return View(new List<RadarChartVIewModel>());
            }
        }
        #endregion

        #region 心理狀態檢測雷達圖
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MentalStateRadarChart(FormCollection form)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "UserAccount");
                }

                if (!int.TryParse(Session["UserID"]?.ToString(), out int userId))
                {
                    return RedirectToAction("Login", "UserAccount");
                }

                ViewBag.UserName = Session["UserName"];
                ViewBag.Age = Session["Age"];
                ViewBag.TeamName = Session["TeamName"];

                // 取得該用戶所有有填寫問卷的日期，並按時間排序
                List<DateTime> surveyDatesList = _db.PsychologicalResponse.Where(ur => ur.UserID == userId && ur.SurveyDate.HasValue)
                .Select(ur => ur.SurveyDate.Value).Distinct().OrderByDescending(d => d).ToList();

                ViewBag.SurveyDates = surveyDatesList;

                string[] surveyDatesRaw = form.GetValues("surveyDates");

                List<DateTime> selectedDates = new List<DateTime>();

                if (surveyDatesRaw != null)
                {
                    try
                    {
                        selectedDates = surveyDatesRaw
                            .Select(d => DateTime.ParseExact(d, "yyyy-MM-dd", CultureInfo.InvariantCulture))
                            .ToList();
                    }
                    catch (FormatException)
                    {
                        ViewBag.ErrorMessage = "提交失敗：日期格式錯誤";
                        return View("MentalStateRadarChart", new List<RadarChartVIewModel>());
                    }
                }
                else if (surveyDatesList.Any())
                {
                    selectedDates.Add(surveyDatesList.First()); // 預設選擇最新日期
                }

                ViewBag.SelectedDates = selectedDates;

                // 查詢所有選擇的日期的數據
                List<RadarChartVIewModel> radarData = new List<RadarChartVIewModel>();

                foreach (var date in selectedDates)
                {
                    string query = @"SELECT c.CategoryName, COALESCE(AVG(pr.Score), 0) AS AverageScore FROM PsychologicalResponse pr
                                   INNER JOIN PsychologicalStateCategory c ON pr.CategoryID = c.ID WHERE pr.UserID = @p0 AND pr.SurveyDate = @p1 
                                   GROUP BY c.CategoryName";

                    object[] parameters = { userId, date };
                    var data = _db.Database.SqlQuery<RadarChartVIewModel>(query, parameters).ToList();

                    foreach (var item in data)
                    {
                        item.SurveyDate = date.ToString("yyyy-MM-dd"); // 存入日期以便前端區分數據
                    }

                    radarData.AddRange(data);
                }

                //取得心理狀態技能描述
                var psy = _db.PsychologicalStateDescription
                    .OrderBy(d => d.ID).ToList();

                ViewBag.PsychologicalDescriptions = psy;

                return View(radarData);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"生成雷達圖失敗：{ex.Message}";
                return View(new List<RadarChartVIewModel>());
            }
        }

        #endregion
    }
}