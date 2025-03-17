using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;

namespace Tiss_MindRadar.Controllers
{
    public class SurveyController : Controller
    {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 身心狀態檢測
        public ActionResult MentalPhysicalState()
        {
            ViewBag.Title = "身心狀態檢測";
            ViewBag.UserName = Session["UserName"];
            ViewBag.Age = Session["Age"];
            ViewBag.TeamName = Session["TeamName"];

            int userId = Convert.ToInt32(Session["UserID"]);

            var MentalPhysicalStateItems = _db.MentalPhysicalState.ToList();
            return View("MentalPhysicalState", MentalPhysicalStateItems);
        }
        #endregion

        #region 提交身心狀態檢測數據並保存到 UserResponse 表
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitMentalPhysicalState(FormCollection form)
        {
            try
            {
                if (Session["UserID"] == null || Session["UserName"] == null)
                {
                    return RedirectToAction("Login", "UserAccount");
                }

                int userId = Convert.ToInt32(Session["UserID"]);
                DateTime surveyDate = DateTime.Parse(form["SurveyDate"]);
                var responses = new Dictionary<int, int>();

                foreach (var key in form.AllKeys)
                {
                    if (key.StartsWith("responses["))
                    {
                        int questionId = int.Parse(key.Replace("responses[", "").Replace("]", ""));
                        int.TryParse(form[key], out int score);
                        responses[questionId] = score;
                    }
                }

                foreach (var response in responses)
                {
                    var userResponse = new UserResponse
                    {
                        QuestionID = response.Key,
                        UserID = userId,
                        Score = response.Value,
                        CategoryID = response.Key,
                        BatchID = Guid.NewGuid(),
                        SurveyDate = surveyDate, // 儲存填寫日期
                        CreatedDate = DateTime.Now
                    };
                    _db.UserResponse.Add(userResponse);
                }

                _db.SaveChanges();
                return RedirectToAction("MentalPhysicalStateRadarChart", "ChartRadar", new { date = surveyDate.ToString("yyyy-MM-dd") });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"提交失敗：{ex.Message}";
                return View("MentalPhysicalState", _db.MentalPhysicalState.ToList());
            }
        }
        #endregion

        #region 心理狀態檢測
        public ActionResult MentalState()
        {
            ViewBag.Title = "心理狀態檢測";
            ViewBag.UserName = Session["UserName"];
            ViewBag.Age = Session["Age"];
            ViewBag.TeamName = Session["TeamName"];

            int userId = Convert.ToInt32(Session["UserID"]);

            var MentalStateItems = _db.MentalState.ToList();
            return View("MentalState", MentalStateItems);
        }
        #endregion

        #region 提交心理狀態檢測數據並保存到 MentalStateCategory 表
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitMentalState(FormCollection form)
        {
            try
            {
                if (Session["UserID"] == null || Session["UserName"] == null)
                {
                    return RedirectToAction("Login", "UserAccount");
                }

                int userId = Convert.ToInt32(Session["UserID"]);

                if (!DateTime.TryParse(form["SurveyDate"], out DateTime surveyDate))
                {
                    surveyDate = DateTime.Now; // 如果沒有選擇日期，預設當天
                }

                var responses = new Dictionary<int, int>();

                foreach (var key in form.AllKeys)
                {
                    if (key.StartsWith("responses[") && key.EndsWith("]"))
                    {
                        int questionId = int.Parse(key.Replace("responses[", "").Replace("]", ""));
                        int.TryParse(form[key], out int score);

                        // **將 7~10 題 & 15~18 題進行反向計分**
                        if ((questionId >= 7 && questionId <= 10) || (questionId >= 15 && questionId <= 18))
                        {
                            score = 6 - score;
                        }

                        responses[questionId] = score;
                    }
                }

                foreach (var response in responses)
                {
                    var userResponse = new PsychologicalResponse
                    {
                        QuestionID = response.Key,
                        UserID = userId,
                        Score = response.Value,
                        CategoryID = response.Key,
                        BatchID = Guid.NewGuid(),
                        SurveyDate = surveyDate, // 儲存選擇的填寫日期
                        CreatedDate = DateTime.Now
                    };
                    _db.PsychologicalResponse.Add(userResponse);
                }

                _db.SaveChanges();
                return RedirectToAction("MentalStateRadarChart", "ChartRadar", new { date = surveyDate.ToString("yyyy-MM-dd") });
            }
            catch (Exception ex)
            {
                ViewBag.ErrorMessage = $"提交失敗：{ex.Message}";
                var mentalStateItems = _db.MentalState.ToList();
                return View("MentalState", mentalStateItems);
            }
        }
        #endregion
    }
}