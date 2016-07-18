using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using PwgTelegramBot.Models;

namespace PwgTelegramBot.Models.Tracker.Projects
{
    public class ProjectLabel
    {
        public string Kind { get; set; }
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public static ProjectLabel JsonToLabel(dynamic json)
        {
            ProjectLabel label = new ProjectLabel();
            label.Kind = json.kind;
            label.Id = json.id;
            label.ProjectId = json.project_id;
            label.Name = json.name;
            label.CreatedAt = json.created_at;
            label.UpdatedAt = json.updated_at;

            return label;
        }

        public static List<ProjectLabel> GetLabels(int projectId, string pivotalTrackerApiToken)
        {
            List<ProjectLabel> labels = new List<ProjectLabel>();

            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/labels";
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            foreach (var element in json)
            {
                ProjectLabel label = JsonToLabel(element);
                labels.Add(label);
            }

            return labels;
        }

        public static ProjectLabel GetLabel(int projectId, int labelId, string pivotalTrackerApiToken)
        {
            string url = "https://www.pivotaltracker.com/services/v5/projects/" + projectId + "/labels/" + labelId;
            dynamic json = WebRequestHelper.GetTrackerJson(url, pivotalTrackerApiToken);
            return JsonToLabel(json);
        }

        public static ProjectLabel GetLabel(int projectId, string labelName, string pivotalTrackerApiToken)
        {
            var labels = GetLabels(projectId, pivotalTrackerApiToken);
            return labels.FirstOrDefault(x => x.Name == labelName);
        }
    }
}