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
                ViewBag.SelectedDate = surveyDate ?? DateTime.Now;

                string query = @"SELECT c.CategoryName, AVG(ur.Score) AS AverageScore FROM UserResponse ur
                          INNER JOIN QuestionCategory qc ON ur.QuestionID = qc.QuestionID
                          INNER JOIN Category c ON qc.CategoryID = c.ID WHERE ur.UserID = @p0 {0}
                          GROUP BY c.CategoryName";

                object[] parameters;

                if (surveyDate.HasValue)
                {
                    query = string.Format(query, "AND ur.SurveyDate = @p1");
                    parameters = new object[] { userId, surveyDate.Value };
                }
                else
                {
                    query = string.Format(query, "");
                    parameters = new object[] { userId };
                }

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
        public ActionResult MentalStateRadarChart(int? batchId = null)
        {
            try
            {
                if (Session["UserID"] == null) //確保 Session 存在
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId = Convert.ToInt32(Session["UserID"]);

                ViewBag.UserName = Session["UserName"];
                ViewBag.Age = Session["Age"];
                ViewBag.TeamName = Session["TeamName"];

                // **修正 SQL，確保結果是整數**
                string query = @"SELECT c.CategoryName, ROUND(AVG(pr.Score), 0) AS AverageScore
                                FROM PsychologicalResponse pr INNER JOIN PsychologicalStateQuestionCategory qc ON     pr.QuestionID = qc.QuestionID INNER JOIN PsychologicalStateCategory c ON   
                                qc.CategoryID = c.ID WHERE pr.UserID = @p0 {0} GROUP BY c.CategoryName";

                object[] parameters;
                if (batchId.HasValue)
                {
                    query = string.Format(query, "AND pr.BatchID = @p1");
                    parameters = new object[] { userId, batchId.Value };
                }
                else
                {
                    query = string.Format(query, "");
                    parameters = new object[] { userId };
                }

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