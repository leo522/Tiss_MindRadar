using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.Models
{
	public class ReportDataModel
	{
        public string UserName { get; set; }
        public string Gender { get; set; }
        public string SurveyDate { get; set; }
        public Dictionary<int, int> Scores { get; set; }
    }
}