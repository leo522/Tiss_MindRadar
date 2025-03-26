using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using static Tiss_MindRadar.Models.RadarChartVIewModel;
using System.Runtime.Remoting.Messaging;

namespace Tiss_MindRadar.Controllers
{
    public class ChartRadarController : Controller
    {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 心理狀態檢測雷達圖
        [HttpGet]
        public ActionResult MentalStateRadarChart(string date)
        {
            var form = new FormCollection { { "surveyDates", date } };
            return MentalStateRadarChart(form);
        }

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

                List<DateTime> surveyDatesList = _db.PsychologicalResponse
                    .Where(ur => ur.UserID == userId && ur.SurveyDate.HasValue)
                    .Select(ur => ur.SurveyDate.Value).Distinct().OrderBy(d => d).ToList();

                ViewBag.SurveyDates = surveyDatesList;

                string[] surveyDatesRaw = form.GetValues("surveyDates");

                List<DateTime> selectedDates = new List<DateTime>();

                if (surveyDatesRaw != null && surveyDatesRaw.Any())
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
 
                ViewBag.SelectedDates = selectedDates;

                // 查詢所有選擇的日期的數據
                List<RadarChartVIewModel> radarData = new List<RadarChartVIewModel>();
                foreach (var date in selectedDates)
                {
                    string query = @"SELECT d.CategoryName AS Dimension, d.SubCategory AS CategoryName, 
                                    COALESCE(AVG(pr.Score), 0) AS AverageScore 
                                    FROM PsychologicalResponse pr 
                                    INNER JOIN PsychologicalStateDescription d ON pr.CategoryID = d.ID 
                                    WHERE pr.UserID = @p0 AND pr.SurveyDate = @p1 
                                    GROUP BY d.CategoryName, d.SubCategory, d.ID, d.HeaderID
                                    ORDER BY CASE 
                                    WHEN d.CategoryName = '基礎心理技能' THEN 1 
                                    WHEN d.CategoryName = '身體心理技能' THEN 2 
                                    WHEN d.CategoryName = '認知技能' THEN 3 
                                    ELSE 4 END, d.ID";

                    object[] parameters = { userId, date };
                    var data = _db.Database.SqlQuery<RadarChartVIewModel>(query, parameters).ToList();

                    foreach (var item in data)
                    {
                        item.SurveyDate = date.ToString("yyyy-MM-dd"); // 存入日期以便前端區分數據
                    }

                    radarData.AddRange(data);
                }


                // 取得心理狀態技能描述
                var psy = _db.PsychologicalStateDescription.OrderBy(d => d.ID).ToList();
                var headers = _db.PsychologicalStateHeader.ToList();
                var mentalStates = _db.MentalState.OrderBy(m => m.QuestionNumber).ToList();

                ViewBag.PsychologicalDescriptions = psy;
                ViewBag.PsychologicalHeaders = headers;
                ViewBag.MentalStateQuestions = mentalStates;

                return View(radarData);
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"生成雷達圖失敗：{ex.Message}";
                return View(new List<RadarChartVIewModel>());
            }
        }
        #endregion

        #region 心理狀態檢測留言
        [HttpPost]
        public JsonResult AddRadarComment(string commentText, string surveyDate)
        {
            if (Session["UserID"] == null || string.IsNullOrWhiteSpace(commentText) || string.IsNullOrWhiteSpace(surveyDate))
            {
                return Json(new { success = false, message = "未登入或資料不完整" });
            }

            int userId = (int)Session["UserID"];
            string role = Session["UserRole"]?.ToString() ?? "Unknown";

            var comment = new RadarChartComment
            {
                UserID = userId,
                Role = role,
                RadarType = "心理狀態",
                SurveyDate = DateTime.ParseExact(surveyDate, "yyyy-MM-dd", CultureInfo.InvariantCulture),
                CommentText = commentText,
                CreatedAt = DateTime.Now
            };

            _db.RadarChartComment.Add(comment);
            _db.SaveChanges();

            // 紀錄 Log
            var log = new RadarCommentLog
            {
                ActionType = "Create",
                TargetType = "Comment",
                TargetID = comment.CommentID,
                PerformedBy = userId,
                PerformedRole = role,
                PerformedAt = DateTime.Now
            };
            _db.RadarCommentLog.Add(log);
            _db.SaveChanges();

            return Json(new { success = true });
        }

        [HttpGet]
        public JsonResult GetRadarComments(string surveyDate)
        {
            var comments = _db.RadarChartComment
                .Where(c => c.RadarType == "心理狀態" && c.SurveyDate.ToString() == surveyDate)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new RadarCommentViewModel
                {
                    CommentID = c.CommentID,
                    CommentText = c.CommentText,
                    CreatedAt = c.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                    SurveyDate = c.SurveyDate.ToString("yyyy-MM-dd"),
                    Role = c.Role,
                    UserName = c.Users.UserName,
                    Replies = c.RadarChartReply.Select(r => new RadarReplyViewModel
                    {
                        ReplyText = r.ReplyText,
                        CreatedAt = r.CreatedAt.ToString("yyyy-MM-dd HH:mm"),
                        PsychologistName = r.Users.UserName
                    }).ToList()
                }).ToList();

            return Json(new { success = true, comments }, JsonRequestBehavior.AllowGet);
        }

        #endregion

        #region 身心狀態檢測雷達圖
        [HttpGet]
        public ActionResult MentalPhysicalStateRadarChart(string date)
        {
            var form = new FormCollection { { "surveyDates", date } };
            return MentalPhysicalStateRadarChart(form);
        }

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

                // 改為由舊到新排序
                List<DateTime> surveyDatesList = _db.UserResponse
                    .Where(ur => ur.UserID == userId && ur.SurveyDate.HasValue)
                    .Select(ur => ur.SurveyDate.Value).Distinct().OrderBy(d => d).ToList();

                ViewBag.SurveyDates = surveyDatesList;  // 送到前端

                string[] surveyDatesRaw = form.GetValues("surveyDates");

                List<DateTime> selectedDates = new List<DateTime>();

                if (surveyDatesRaw != null && surveyDatesRaw.Any())
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
                else if (surveyDatesList.Any())
                {
                    selectedDates.Add(surveyDatesList.First()); // ✅ 預設最早或最新都可
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
    }
}