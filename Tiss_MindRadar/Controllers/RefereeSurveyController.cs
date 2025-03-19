using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Tiss_MindRadar.Models;
using static Tiss_MindRadar.Models.RefereeViewModel;

namespace Tiss_MindRadar.Controllers
{
    public class RefereeSurveyController : Controller
    {
        private TISS_MindRadarEntities _db = new TISS_MindRadarEntities(); //資料庫

        #region 流暢經驗_裁判版
        public ActionResult SmoothExperienceSurvey()
        {
            ViewBag.RefereeName = Session["UserName"];
            ViewBag.RefereeTeamName = Session["RefereeTeamName"];

            // 取得流暢經驗分類資料
            var categories = _db.SmoothExperienceCategory
                .Select(c => new SmoothExperienceCategoryViewModel
                {
                    CategoryID = c.CategoryID,
                    CategoryItem = c.CategoryItem,
                    Questions = _db.SmoothExperience
                        .Where(q => q.CategoryID == c.CategoryID) // 按流暢經驗分類篩選問題
                        .Select(q => new SmoothExperienceViewModel
                        {
                            QuestionID = q.QuestionID,
                            QuestionText = q.QuestionText
                        }).ToList()
                }).ToList();

            return View(categories);
        }
        #endregion

        #region 儲存_流暢經驗答案
        [HttpPost]
        public ActionResult SubmitSmoothExperienceSurvey(List<SmoothExperienceResponseViewModel> Responses)
        {
            if (Responses == null || !Responses.Any())
            {
                return Json(new { success = false, message = "請填寫所有問題" });
            }

            HashSet<int> reverseScoringQuestions = new HashSet<int> { 1, 3, 5, 6 }; //需要反向計分的題目

            foreach (var response in Responses)
            {
                int finalScore = reverseScoringQuestions.Contains(response.QuestionID)
                    ? ReverseScore(response.Score)
                    : response.Score;

                var newResponse = new SmoothExperienceResponse
                {
                    QuestionID = response.QuestionID,
                    Score = finalScore,
                    SubmittedAt = DateTime.Now
                };

                _db.SmoothExperienceResponse.Add(newResponse);
            }

            //TempData["SuccessMessage"] = "您的答案已成功提交！";

            _db.SaveChanges();
            return RedirectToAction("SmoothExperienceSurvey");
        }
        #endregion

        #region 反向計分函數
        private int ReverseScore(int score)
        {
            return 6 - score; // 5 -> 1, 4 -> 2, 3 -> 3, 2 -> 4, 1 -> 5
        }
        #endregion

        #region 專業能力_裁判版
        public ActionResult ProfessionalCapabilitiesSurvey()
        {
            ViewBag.RefereeName = Session["UserName"];
            ViewBag.RefereeTeamName = Session["RefereeTeamName"];

            // 取得專業能力分類資料
            var categories = _db.ProfessionalCapabilitiesCategory
                 .Select(c => new ProfessionalCapabilitiesCategoryViewModel
                 {
                     CategoryID = c.CategoryID,
                     CategoryItem = c.CategoryItem,
                     Questions = _db.ProfessionalCapabilities
                    .Where(q => q.CategoryID == c.CategoryID) // 按分類篩選問題
                    .Select(q => new ProfessionalCapabilitiesViewModel
                    {
                        QuestionID = q.QuestionID,
                        QuestionText = q.QuestionText
                    }).ToList()
                 }).ToList();
            return View(categories);
        }

        [HttpPost]
        public ActionResult SubmitProfessionalCapabilitiesSurvey(List<ProfessionalCapabilitiesResponseViewModel> responses)
        {
            if (responses == null || !responses.Any())
            {
                return RedirectToAction("ProfessionalCapabilitiesSurvey");
            }

            foreach (var res in responses)
            {
                var newResponse = new ProfessionalCapabilitiesResponse
                {
                    QuestionID = res.QuestionID,
                    Score = res.Score,
                    SubmittedAt = DateTime.Now
                };

                _db.ProfessionalCapabilitiesResponse.Add(newResponse);
            }

            _db.SaveChanges();

            return RedirectToAction("ProfessionalCapabilitiesSurvey");
        }
        #endregion

        #region 選擇量表頁面
        public ActionResult ChooseRefereeSurvey()
        {
            return View();
        }
        #endregion
    }
}