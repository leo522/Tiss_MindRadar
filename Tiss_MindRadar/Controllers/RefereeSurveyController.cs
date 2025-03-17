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

        #region 選擇量表頁面
        public ActionResult ChooseRefereeSurvey()
        {
            return View();
        }
        #endregion

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

        [HttpPost]
        public ActionResult SubmitSmoothExperienceSurvey(List<SmoothExperienceResponseViewModel> responses)
        {
            if (responses == null || !responses.Any())
            {
                return RedirectToAction("SmoothExperienceSurvey");
            }

            foreach (var res in responses)
            {
                var newResponse = new SmoothExperienceResponse
                {
                    QuestionID = res.QuestionID,
                    Score = res.Score,
                    SubmittedAt = DateTime.Now
                };
                _db.SmoothExperienceResponse.Add(newResponse);
            }

            _db.SaveChanges();
            return RedirectToAction("SmoothExperienceSurvey");
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
    }
}