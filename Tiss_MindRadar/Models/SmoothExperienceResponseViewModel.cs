using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.Models
{
    #region 流暢經驗
    public class SmoothExperienceSurveyRequest
    {
        public List<SmoothExperienceResponseViewModel> Responses { get; set; }
    }

    public class SmoothExperienceResponseViewModel
    {
        public int QuestionID { get; set; }
        public int Score { get; set; }
    }
    #endregion

    //#region 專業能力
    //public class ProfessionalCapabilitiesSurveyRequest
    //{
    //    public List<ProfessionalCapabilitiesResponseViewModel> Responses { get; set; }
    //}

    //public class ProfessionalCapabilitiesResponseViewModel
    //{
    //    public int QuestionID { get; set; }
    //    public int Score { get; set; }
    //}
    //#endregion
}