using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.ViewModels
{
    public class GroupedPsychScoreViewModel
    {
        public string CategoryName { get; set; }
        public List<SubCategoryViewModel> Items { get; set; }
    }

    public class SubCategoryViewModel
    {
        public string SubCategory { get; set; }
        public string Description { get; set; }
        public List<ScoreItemViewModel> Scores { get; set; }
    }

    public class ScoreItemViewModel
    {
        public string UserName { get; set; }
        public decimal Score { get; set; }
        public string SurveyDate { get; set; }
    }
}