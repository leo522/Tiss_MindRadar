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
            var dtos = _db.SmoothExperience.Select(q => new SmoothExperienceViewModel
            { 
                QuestionID = q.QuestionID,
                QuestionText = q.QuestionText
            }).ToList();

            return View(dtos);
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

        #endregion
    }
}