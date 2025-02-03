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

        #region 提交身心狀態檢測數據並保存到 MentalPhysicalStateUserResponse 表
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SubmitMentalPhysicalState(FormCollection form)
        {
            try
            {
                if (Session["UserID"] == null || Session["UserName"] == null)
                {
                    return RedirectToAction("Login", "Account");
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
            ViewBag.Age = Session["Age"];
            ViewBag.TeamName = Session["TeamName"];

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
                if (Session["UserID"] == null)
                {
                    return RedirectToAction("Login", "Account");
                }

                int userId = Convert.ToInt32(Session["UserID"]);
                var responses = new Dictionary<int, int>();

                foreach (var key in form.AllKeys) //解析表單數據
                {
                    if (key.StartsWith("responses[") && key.EndsWith("]"))
                    {
                        int questionId = int.Parse(key.Replace("responses[", "").Replace("]", ""));
                        int.TryParse(form[key], out int score); //未填寫時默認為 0

                        //**反向計分 (第15~18題)**
                        if (questionId >= 15 && questionId <= 18)
                        {
                            score = 6 - score; //反轉計分，例如 5 變成 1，4 變成 2，3 不變
                        }

                        responses[questionId] = score;
                    }
                }

                if (!responses.Any())
                {
                    ViewBag.ErrorMessage = "請至少選擇一個答案。";
                    var mentalStateItems = _db.MentalState.ToList();
                    return View("MentalState", mentalStateItems);
                }

                foreach (var response in responses) //保存數據
                {
                    var categoryId = _db.Database.SqlQuery<int>(
                        "SELECT CategoryID FROM PsychologicalStateQuestionCategory WHERE QuestionID = @p0", response.Key
                    ).FirstOrDefault();

                    var userResponse = new PsychologicalResponse
                    {
                        QuestionID = response.Key,
                        CategoryID = categoryId,
                        Score = response.Value,
                        UserID = userId,
                        BatchID = Guid.NewGuid(),
                        CreatedDate = DateTime.Now
                    };
                    _db.PsychologicalResponse.Add(userResponse);
                }

                _db.SaveChanges();
                return RedirectToAction("MentalStateRadarChart", "ChartRadar");
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