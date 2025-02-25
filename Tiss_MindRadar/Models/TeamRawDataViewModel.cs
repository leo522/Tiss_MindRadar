using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Tiss_MindRadar.Models
{
	public class TeamRawDataViewModel
	{
        public string TeamName { get; set; }
        public string UserName { get; set; }
        public string Category { get; set; }
        public int Score { get; set; }
        public DateTime? SurveyDate { get; set; }
    }
}