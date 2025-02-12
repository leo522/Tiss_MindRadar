using System;
using System.Collections.Generic;
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
        public ActionResult MentalPhysicalStateRadarChart(DateTime? surveyDate = null)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId = Convert.ToInt32(Session["UserID"]);
                ViewBag.UserName = Session["UserName"];
                ViewBag.Age = Session["Age"];
                ViewBag.TeamName = Session["TeamName"];

                // 取得該用戶所有有填寫問卷的日期，並按時間排序
                List<DateTime> surveyDates = _db.UserResponse.Where(ur => ur.UserID == userId && ur.SurveyDate.HasValue)
                    .Select(ur => ur.SurveyDate.Value).Distinct().OrderByDescending(d => d).ToList();


                ViewBag.SurveyDates = surveyDates;  // 送到前端
                
                if (!surveyDate.HasValue && surveyDates.Any()) //如果 surveyDate 沒有提供，則使用最新的檢測日期
                {
                    surveyDate = surveyDates.First();
                }

                ViewBag.SelectedDate = surveyDate;

                string query = @"SELECT c.CategoryName, CAST(ROUND(AVG(ur.Score), 0) AS INT) AS AverageScore
                                FROM UserResponse ur INNER JOIN QuestionCategory qc ON ur.QuestionID = qc.QuestionID
                                INNER JOIN Category c ON qc.CategoryID = c.ID WHERE ur.UserID = @p0 AND ur.SurveyDate = @p1
                                GROUP BY c.CategoryName";

                object[] parameters = { userId, surveyDate.Value };

                var data = _db.Database.SqlQuery<RadarChartVIewModel>(query, parameters).ToList();
                return View(data);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"生成雷達圖失敗：{ex.Message}";
                return View(new List<RadarChartVIewModel>());
            }
        }
        #endregion

        #region 心理狀態檢測雷達圖
        public ActionResult MentalStateRadarChart(DateTime? surveyDate = null)
        {
            try
            {
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId = Convert.ToInt32(Session["UserID"]);
                ViewBag.UserName = Session["UserName"];
                ViewBag.Age = Session["Age"];
                ViewBag.TeamName = Session["TeamName"];

                // 取得該用戶所有有填寫問卷的日期，並按時間排序
                List<DateTime> surveyDates = _db.PsychologicalResponse.Where(ur => ur.UserID == userId && ur.SurveyDate.HasValue).Select(ur => ur.SurveyDate.Value).Distinct()
                    .OrderByDescending(d => d).ToList();

                ViewBag.SurveyDates = surveyDates;

                // 如果 surveyDate 沒有提供，則使用最新的檢測日期
                if (!surveyDate.HasValue && surveyDates.Any())
                {
                    surveyDate = surveyDates.First();
                }

                ViewBag.SelectedDate = surveyDate;

                // 修正 SQL 查詢，確保 AVG 計算不受 NULL 影響
                string query = @"SELECT c.CategoryName, COALESCE(AVG(pr.Score), 0) AS AverageScore FROM PsychologicalStateCategory c
                                LEFT JOIN PsychologicalStateQuestionCategory qc ON qc.CategoryID = c.ID LEFT JOIN PsychologicalResponse pr ON pr.QuestionID = qc.QuestionID AND pr.UserID = @p0 AND pr.SurveyDate = @p1 GROUP BY c.CategoryName";

                object[] parameters = { userId, surveyDate.Value };

                var data = _db.Database.SqlQuery<RadarChartVIewModel>(query, parameters).ToList();

                return View(data);
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