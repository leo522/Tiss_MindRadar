using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.Models
{
    #region 專業能力
    public class ProfessionalCapabilitiesCategoryViewModel
    {
        public int CategoryID { get; set; }
        public string CategoryItem { get; set; }
        public List<ProfessionalCapabilitiesViewModel> Questions { get; set; }
    }

    public class ProfessionalCapabilitiesViewModel
    {
        public int QuestionID { get; set; }
        public string QuestionText { get; set; }
        public int Score { get; set; }
    }

    public class ProfessionalCapabilitiesResponseViewModel
    {
        public int QuestionID { get; set; }
        public int Score { get; set; }
    }
    #endregion
}